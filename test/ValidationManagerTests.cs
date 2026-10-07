using System.Net;
using com.janoserdelyi.EmailValidation;
using com.janoserdelyi.Validation;

namespace Test;

public class ValidationManagerTests
{
	[Fact]
	public void ConstructorThrowsWhenBothAllowAndDisallowListsAreConfigured () {
		ValidationManagerOptions options = new () {
			DisallowedDomains = ["blocked.test"],
			AllowedDomains = ["allowed.test"]
		};

		Assert.Throws<InvalidOperationException> (() => new ValidationManager (options));
	}

	[Fact]
	public void ConstructorAllowsEitherDomainListModeOnItsOwn () {
		using ValidationManager disallowOnly = new (new ValidationManagerOptions { DisallowedDomains = ["blocked.test"] });
		using ValidationManager allowOnly = new (new ValidationManagerOptions { AllowedDomains = ["allowed.test"] });
	}

	[Fact]
	public void DisposeDoesNotThrow () {
		ValidationManager manager = new ();
		manager.Dispose ();
	}

	[Fact]
	public async Task ValidateEmailAsyncSucceedsWithDefaultOptionsWhenSkippingFullLookup () {
		using ValidationManager manager = new ();

		Result<Email> result = await manager.ValidateEmailAsync (" User@Example.com ", fullLookup: false);

		Assert.True (result.IsSuccess);
		Assert.Equal ("user@example.com", result.Value!.Address);
	}

	[Fact]
	public async Task ValidateEmailAsyncFailsFastOnInvalidFormat () {
		using ValidationManager manager = new ();

		Result<Email> result = await manager.ValidateEmailAsync ("not-an-email", fullLookup: false);

		Assert.True (result.IsFailure);
	}

	[Fact]
	public async Task ValidateEmailAsyncUsesConfiguredTypoMatches () {
		ValidationManagerOptions options = new () {
			TypoMatches = new Dictionary<string, TypoMatch> {
				["typo.test"] = new TypoMatch ("typo.test", "fixed.test")
			}
		};
		using ValidationManager manager = new (options);

		Result<Email> result = await manager.ValidateEmailAsync ("user@typo.test", fullLookup: false);

		Assert.True (result.IsFailure);
		Assert.Contains ("fixed.test", result.ErrorMessage);
	}

	[Fact]
	public async Task ValidateEmailAsyncBlocksConfiguredDisallowedDomains () {
		using ValidationManager manager = new (new ValidationManagerOptions {
			DisallowedDomains = ["blocked.test"]
		});

		Result<Email> blocked = await manager.ValidateEmailAsync ("user@blocked.test", fullLookup: false);
		Assert.True (blocked.IsFailure);

		Result<Email> allowed = await manager.ValidateEmailAsync ("user@other.test", fullLookup: false);
		Assert.True (allowed.IsSuccess);
	}

	[Fact]
	public async Task ValidateEmailAsyncOnlyAllowsConfiguredAllowedDomains () {
		using ValidationManager manager = new (new ValidationManagerOptions {
			AllowedDomains = ["allowed.test"]
		});

		Result<Email> notAllowed = await manager.ValidateEmailAsync ("user@other.test", fullLookup: false);
		Assert.True (notAllowed.IsFailure);

		Result<Email> allowed = await manager.ValidateEmailAsync ("user@allowed.test", fullLookup: false);
		Assert.True (allowed.IsSuccess);
	}

	[Fact]
	public async Task ValidateEmailAsyncBlocksTemporaryServiceDomains () {
		TemporaryServiceConfig tempConfig = new ("https://validationmanager-list.test") {
			ForceRefresh = true,
			HttpMessageHandler = new StubHttpMessageHandler (HttpStatusCode.OK, "vm-disposable.test\n")
		};

		using ValidationManager manager = new (new ValidationManagerOptions {
			TemporaryServiceConfig = tempConfig
		});

		Result<Email> result = await manager.ValidateEmailAsync ("user@vm-disposable.test", fullLookup: false);

		Assert.True (result.IsFailure);
	}

	[Fact]
	public async Task ValidateEmailAsyncSkipsMxCheckWhenFullLookupIsFalse () {
		using ValidationManager manager = new ();

		// this domain has no real MX records - if the MX check ran, this would fail
		Result<Email> result = await manager.ValidateEmailAsync ("user@no-mx-records.invalid", fullLookup: false);

		Assert.True (result.IsSuccess);
	}

	[Fact]
	public async Task ValidateEmailAsyncFullLookupSucceedsForConfiguredBypassDomain () {
		using ValidationManager manager = new (new ValidationManagerOptions {
			BypassDomains = ["bypassed.test"]
		});

		Result<Email> result = await manager.ValidateEmailAsync ("user@bypassed.test", fullLookup: true);

		Assert.True (result.IsSuccess);
	}

	[Fact]
	public async Task ValidateEmailAsyncLoadsBadDomainsFromRepoOnFirstUse () {
		FakeEmailDomainRepo repo = new ();
		repo.BadDomains.Add (new EmailDomainDto { Domain = "repo-bad.test", IsGood = false });

		using ValidationManager manager = new (new ValidationManagerOptions { EmailDomainRepo = repo });

		Result<Email> result = await manager.ValidateEmailAsync ("user@repo-bad.test", fullLookup: false);

		Assert.True (result.IsFailure);
	}

	[Fact]
	public async Task ValidateEmailAsyncLoadsGoodDomainsFromRepoAsMxBypass () {
		FakeEmailDomainRepo repo = new ();
		repo.GoodDomains.Add (new EmailDomainDto { Domain = "repo-good.test", IsGood = true });

		using ValidationManager manager = new (new ValidationManagerOptions { EmailDomainRepo = repo });

		Result<Email> result = await manager.ValidateEmailAsync ("user@repo-good.test", fullLookup: true);

		Assert.True (result.IsSuccess);
	}

	[Fact]
	public async Task InitializeAsyncIsIdempotentAndOptionalToCallExplicitly () {
		FakeEmailDomainRepo repo = new ();
		repo.BadDomains.Add (new EmailDomainDto { Domain = "explicit-bad.test", IsGood = false });

		using ValidationManager manager = new (new ValidationManagerOptions { EmailDomainRepo = repo });

		await manager.InitializeAsync ();
		await manager.InitializeAsync (); // second call should be a no-op

		Result<Email> result = await manager.ValidateEmailAsync ("user@explicit-bad.test", fullLookup: false);

		Assert.True (result.IsFailure);
	}

	[Fact]
	public async Task ValidateEmailAsyncPersistsSuccessfulFullLookupToRepo () {
		FakeEmailDomainRepo repo = new ();
		using ValidationManager manager = new (new ValidationManagerOptions {
			EmailDomainRepo = repo,
			BypassDomains = ["persisted.test"]
		});

		Result<Email> result = await manager.ValidateEmailAsync ("user@persisted.test", fullLookup: true);

		Assert.True (result.IsSuccess);
		Assert.Contains (repo.Upserted, d => d.Domain == "persisted.test" && d.IsGood);
	}

	[Fact]
	public async Task ValidateEmailAsyncLogsButDoesNotFailWhenRepoUpsertThrows () {
		FakeEmailDomainRepo repo = new () { ThrowOnUpsert = true };
		List<string> logs = [];

		using ValidationManager manager = new (new ValidationManagerOptions {
			EmailDomainRepo = repo,
			BypassDomains = ["logged.test"],
			Logger = logs.Add
		});

		Result<Email> result = await manager.ValidateEmailAsync ("user@logged.test", fullLookup: true);

		Assert.True (result.IsSuccess);
		Assert.Single (logs);
	}

	[Fact]
	public async Task UpdateAndRemoveDomainCacheAdjustRuntimeBehavior () {
		using ValidationManager manager = new ();

		manager.UpdateDomainCache ("Dynamic.Test", isGood: false);
		Result<Email> blocked = await manager.ValidateEmailAsync ("user@dynamic.test", fullLookup: false);
		Assert.True (blocked.IsFailure);

		manager.RemoveDomainFromCache ("dynamic.test");
		Result<Email> unblocked = await manager.ValidateEmailAsync ("user@dynamic.test", fullLookup: false);
		Assert.True (unblocked.IsSuccess);
	}

	private sealed class StubHttpMessageHandler (
		HttpStatusCode statusCode,
		string content
	) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync (
			HttpRequestMessage request,
			CancellationToken cancellationToken
		) {
			return Task.FromResult (new HttpResponseMessage (statusCode) {
				Content = new StringContent (content)
			});
		}
	}

	private sealed class FakeEmailDomainRepo : IEmailDomainRepo
	{
		public List<EmailDomainDto> GoodDomains { get; } = [];
		public List<EmailDomainDto> BadDomains { get; } = [];
		public List<EmailDomainDto> Upserted { get; } = [];
		public bool ThrowOnUpsert { get; set; }

		public Task<IEnumerable<EmailDomainDto>?> SelectGoodDomains (CancellationToken cancellationToken = default) {
			return Task.FromResult<IEnumerable<EmailDomainDto>?> (GoodDomains);
		}

		public Task<IEnumerable<EmailDomainDto>?> SelectBadDomains (CancellationToken cancellationToken = default) {
			return Task.FromResult<IEnumerable<EmailDomainDto>?> (BadDomains);
		}

		public Task Upsert (EmailDomainDto domain, CancellationToken cancellationToken = default) {
			if (ThrowOnUpsert) {
				throw new InvalidOperationException ("upsert failed");
			}

			Upserted.Add (domain);
			return Task.CompletedTask;
		}
	}
}
