using com.janoserdelyi.Validation;

namespace com.janoserdelyi.EmailValidation;

// holds all of the state email validation needs (dns servers, bypass/disallow domain lists, the optional
// domain repo) so that ValidateEmailAsync itself only ever needs an address and a bool - no service injection
// per call. construct one of these (ideally as a long-lived singleton) and reuse it
public class ValidationManager : IDisposable
{
	public ValidationManager (
		ValidationManagerOptions? options = null
	) {
		_options = options ?? new ValidationManagerOptions ();

		if (_options.DisallowedDomains.Count > 0 && _options.AllowedDomains.Count > 0) {
			throw new InvalidOperationException ("ValidationManagerOptions cannot specify both DisallowedDomains (block list) and AllowedDomains (allow list) - they are mutually exclusive");
		}

		_mxConfig = new MxConfig {
			DnsServers = [.. _options.DnsServers],
			BypassDomains = [.. _options.BypassDomains]
		};

		_disallowedDomains = [.. _options.DisallowedDomains];
		_allowedDomains = [.. _options.AllowedDomains];
	}

	private readonly ValidationManagerOptions _options;
	private readonly MxConfig _mxConfig;
	private readonly List<string> _disallowedDomains;
	private readonly List<string> _allowedDomains;
	private readonly Lock _stateLock = new ();
	private readonly SemaphoreSlim _initLock = new (1, 1);
	private bool _initialized;

	public void Dispose () {
		_initLock.Dispose ();
		GC.SuppressFinalize (this);
	}

	// loads known good/bad domains from the optional repo. safe to call more than once - only does the work once.
	// ValidateEmailAsync calls this itself on first use, so you don't have to
	public async Task InitializeAsync (
		CancellationToken cancellationToken = default
	) {
		if (_initialized) {
			return;
		}

		await _initLock.WaitAsync (cancellationToken);
		try {
			if (_initialized) {
				return;
			}

			if (_options.EmailDomainRepo != null) {
				var badDomains = await _options.EmailDomainRepo.SelectBadDomains (cancellationToken);
				if (badDomains != null) {
					foreach (var badDomain in badDomains) {
						addDisallowedDomain (badDomain.Domain);
					}
				}

				var goodDomains = await _options.EmailDomainRepo.SelectGoodDomains (cancellationToken);
				if (goodDomains != null) {
					foreach (var goodDomain in goodDomains) {
						addBypassDomain (goodDomain.Domain);
					}
				}
			}

			_initialized = true;
		}
		finally {
			_initLock.Release ();
		}
	}

	// the one call site - no services to pass in, just the address and whether to go all the way out to a
	// real MX/network check (slower) or stop at the local, format, typo, and domain-list checks (fast)
	public async Task<Result<Email>> ValidateEmailAsync (
		string emailAddress,
		bool fullLookup = true,
		CancellationToken cancellationToken = default
	) {
		if (!_initialized) {
			await InitializeAsync (cancellationToken);
		}

		var result = Email.Validator (emailAddress)
			.Lower ()
			.Trim ()
			.ValidateFormat ()
			.Parse ();

		if (result.IsFailure) {
			return result;
		}

		result = result
			.LocalIsValid ()
			.CommonTypos (_options.TypoMatches);

		if (result.IsFailure) {
			return result;
		}

		lock (_stateLock) {
			if (_allowedDomains.Count > 0) {
				result = result.AllowDomains (_allowedDomains);
			} else if (_disallowedDomains.Count > 0) {
				result = result.DisallowDomains (_disallowedDomains);
			}
		}

		if (result.IsFailure) {
			return result;
		}

		if (_options.TemporaryServiceConfig != null) {
			result = await result.DisallowTemporaryServiceDomains (_options.TemporaryServiceConfig, cancellationToken);

			if (result.IsFailure) {
				return result;
			}
		}

		if (!fullLookup) {
			return result;
		}

		// snapshot so a concurrent UpdateDomainCache/RemoveDomainFromCache can't mutate the lists mid-iteration
		MxConfig mxConfigSnapshot;
		lock (_stateLock) {
			mxConfigSnapshot = new MxConfig {
				DnsServers = [.. _mxConfig.DnsServers],
				BypassDomains = [.. _mxConfig.BypassDomains]
			};
		}

		result = await result.VerifyMxRecords (mxConfigSnapshot, cancellationToken);

		await recordResultAsync (result, cancellationToken);

		return result;
	}

	// manually mark a domain as good or bad, eg. in response to something learned outside of a validation call
	public void UpdateDomainCache (
		string domain,
		bool isGood
	) {
		domain = domain.Trim ().ToLowerInvariant ();

		lock (_stateLock) {
			if (isGood) {
				_ = _disallowedDomains.Remove (domain);
				addBypassDomain (domain);
			} else {
				_ = _mxConfig.BypassDomains.Remove (domain);
				addDisallowedDomain (domain);
			}
		}
	}

	public void RemoveDomainFromCache (
		string domain
	) {
		domain = domain.Trim ().ToLowerInvariant ();

		lock (_stateLock) {
			_ = _disallowedDomains.Remove (domain);
			_ = _mxConfig.BypassDomains.Remove (domain);
		}
	}

	// best effort - save the outcome of a full lookup so future calls (here or wherever else reads the repo)
	// can skip the network check. failures here should never fail the validation itself
	private async Task recordResultAsync (
		Result<Email> result,
		CancellationToken cancellationToken
	) {
		string? domain = result.Value?.Domain;

		if (domain == null) {
			return;
		}

		if (result.IsSuccess) {
			addBypassDomain (domain);
		} else {
			addDisallowedDomain (domain);
		}

		if (_options.EmailDomainRepo == null) {
			return;
		}

		var dto = new EmailDomainDto {
			Domain = domain,
			IsGood = result.IsSuccess,
			Reason = result.IsSuccess ? "validated" : result.ErrorMessage ?? "unknown reason",
			CreatedDate = DateTime.UtcNow
		};

		try {
			await _options.EmailDomainRepo.Upsert (dto, cancellationToken);
		} catch (Exception oops) {
			_options.Logger?.Invoke ($"error saving {(dto.IsGood ? "good" : "bad")} domain '{domain}' to repo - {oops}");
		}
	}

	private void addBypassDomain (
		string domain
	) {
		lock (_stateLock) {
			if (_mxConfig.BypassDomains.Contains (domain) == false) {
				_mxConfig.BypassDomains.Add (domain);
			}
		}
	}

	private void addDisallowedDomain (
		string domain
	) {
		lock (_stateLock) {
			if (_disallowedDomains.Contains (domain) == false) {
				_disallowedDomains.Add (domain);
			}
		}
	}
}
