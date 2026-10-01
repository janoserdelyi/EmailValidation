using System.Net;
using com.janoserdelyi.EmailValidation;
using com.janoserdelyi.Validation;

namespace Test;

public class EmailValidationCoverageTests
{
	[Fact]
	public void EmailConstructionAndPrimitiveChecks () {
		Email empty = new ();
		Email email = new ("User@example.com");

		Assert.Null (empty.ToString ());
		Assert.Equal ("User@example.com", email.ToString ());
		Assert.True (Email.AddressIsNotEmpty ("x"));
		Assert.False (Email.AddressIsNotEmpty (null));
		Assert.False (Email.AddressIsNotEmpty (""));
		Assert.True (Email.IsLongEnough ("abcde", 5));
		Assert.True (Email.IsLongEnough ("abcde", -5));
		Assert.False (Email.IsLongEnough ("abcd", 5));
		Assert.False (Email.IsLongEnough (null, 0));
		Assert.False (Email.IsLongEnough ("", 0));
		Assert.True (Email.IsLength ("abc", 3));
		Assert.True (Email.IsLength ("abc", -3));
		Assert.False (Email.IsLength ("ab", 3));
		Assert.False (Email.IsLength (null, 0));
		Assert.False (Email.IsLength ("", 0));
	}

	[Fact]
	public void FormatValidationCoversMalformedAndValidAddresses () {
		Assert.False (Email.IsValidFormat (null));
		Assert.False (Email.IsValidFormat (new Email (null!)));
		Assert.False (Email.IsValidFormat (new Email ("")));
		Assert.False (Email.IsValidFormat (new Email ("missing-at.example.com")));
		Assert.False (Email.IsValidFormat (new Email ("a@@example.com")));
		Assert.False (Email.IsValidFormat (new Email ("a@examplecom")));
		Assert.False (Email.IsValidFormat (new Email ("a..b@example.com")));
		Assert.False (Email.IsValidFormat (new Email ("a@example.com.")));
		Assert.False (Email.IsValidFormat (new Email (".a@example.com")));
		Assert.False (Email.IsValidFormat (new Email ("a b@example.com")));
		Assert.True (Email.IsValidFormat (new Email ("a.b@example.com")));
	}

	[Fact]
	public void ParsePartsHandlesMissingAndNormalizesAddress () {
		Assert.Null (Email.ParseParts (null!));
		Assert.Null (Email.ParseParts (new Email (null!)));
		Assert.Null (Email.ParseParts (new Email ("")));

		Email parsed = Email.ParseParts (new Email (" USER@Example.COM "))!;

		Assert.Equal ("user@example.com", parsed.Address);
		Assert.Equal (" USER", parsed.LocalPart);
		Assert.Equal ("Example.COM ", parsed.Domain);
	}

	[Fact]
	public void ValidateReturnsFailuresAndParsesValidEmails () {
		Assert.True (new Email (null!).Validate ().IsFailure);
		Assert.True (new Email (" ").Validate ().IsFailure);
		Assert.True (new Email ("a@b").Validate ().IsFailure);

		Result<Email> valid = new Email ("User@example.com").Validate ();

		Assert.True (valid.IsSuccess);
		Assert.Equal ("User", valid.Value!.LocalPart);
		Assert.Equal ("example.com", valid.Value.Domain);
	}

	[Fact]
	public void FormatAndParseExtensionsCoverPassThroughAndStateBranches () {
		Result<Email> failed = failure ();
		Assert.True (failed.ValidateFormat ().IsFailure);
		Assert.True (failed.Parse ().IsFailure);

		Result<Email> nullValue = success (null);
		Assert.True (nullValue.ValidateFormat ().IsFailure);
		Assert.True (nullValue.Parse ().IsFailure);

		Email alreadyValid = new ("user@example.com") { ValidFormat = true };
		Assert.Same (alreadyValid, success (alreadyValid).ValidateFormat ().Value);

		Result<Email> invalidFormat = success (new Email ("not-an-email")).ValidateFormat ();
		Assert.True (invalidFormat.IsFailure);

		Email formatOk = new ("user@example.com");
		Result<Email> validated = success (formatOk).ValidateFormat ();
		Assert.True (validated.IsSuccess);
		Assert.True (formatOk.ValidFormat);

		Email alreadyParsed = new ("user@example.com") { Parsed = true };
		Assert.Same (alreadyParsed, success (alreadyParsed).Parse ().Value);

		Email missingAddress = new (null!);
		Assert.True (success (missingAddress).Parse ().IsFailure);

		Email parsed = new ("User@example.com");
		Result<Email> parseResult = success (parsed).Parse ();
		Assert.True (parseResult.IsSuccess);
		Assert.True (parsed.Parsed);
		Assert.Equal ("User", parsed.LocalPart);
		Assert.Equal ("example.com", parsed.Domain);
	}

	[Fact]
	public void LocalPartValidationCoversPrerequisitesAndCharacterRules () {
		Assert.False (Email.LocalPartIsValid (null!));
		Assert.False (Email.LocalPartIsValid (""));
		Assert.True (Email.LocalPartIsValid ("user.name+tag"));
		Assert.False (Email.LocalPartIsValid ("user name"));

		Result<Email> failed = failure ();
		Assert.True (failed.LocalIsValid ().IsFailure);
		Assert.True (success (null).LocalIsValid ().IsFailure);
		Assert.True (success (new Email ("user@example.com")).LocalIsValid ().IsFailure);

		Email notParsed = new ("user@example.com") { ValidFormat = true };
		Assert.True (success (notParsed).LocalIsValid ().IsFailure);

		Email missingLocal = new ("user@example.com") { ValidFormat = true, Parsed = true };
		Assert.True (success (missingLocal).LocalIsValid ().IsFailure);

		Email trailingDot = new ("user.@example.com") {
			ValidFormat = true,
			Parsed = true,
			LocalPart = "user."
		};
		Assert.True (success (trailingDot).LocalIsValid ().IsFailure);

		Email invalidLocal = new ("bad local@example.com") {
			ValidFormat = true,
			Parsed = true,
			LocalPart = "bad local"
		};
		Assert.True (success (invalidLocal).LocalIsValid ().IsFailure);

		Email validLocal = new ("user@example.com") {
			ValidFormat = true,
			Parsed = true,
			LocalPart = "user"
		};
		Assert.True (success (validLocal).LocalIsValid ().IsSuccess);
		Assert.True (validLocal.ValidFormat);
	}

	[Fact]
	public void LowerTrimAndRankHandleMissingValuesAndSuccess () {
		Assert.True (failure ().Lower ().IsFailure);
		Assert.True (failure ().Trim ().IsFailure);
		Assert.True (failure ().Rank ().IsFailure);
		Assert.True (success (null).Lower ().IsFailure);
		Assert.True (success (null).Trim ().IsFailure);
		Assert.True (success (null).Rank ().IsFailure);

		Assert.True (success (new Email (null!)).Lower ().IsFailure);
		Assert.True (success (new Email (null!)).Trim ().IsFailure);
		Assert.True (success (new Email (null!)).Rank ().IsFailure);

		Email normalized = new ("  USER@EXAMPLE.COM  ") {
			LocalPart = " USER ",
			Domain = " EXAMPLE.COM "
		};
		Assert.True (success (normalized).Lower ().IsSuccess);
		Assert.Equal ("  user@example.com  ", normalized.Address);
		Assert.Equal (" user ", normalized.LocalPart);
		Assert.Equal (" example.com ", normalized.Domain);
		Assert.True (success (normalized).Trim ().IsSuccess);
		Assert.Equal ("user@example.com", normalized.Address);
		Assert.Equal ("user", normalized.LocalPart);
		Assert.Equal ("example.com", normalized.Domain);

		Email noParts = new (" USER@EXAMPLE.COM ");
		Assert.True (success (noParts).Lower ().IsSuccess);
		Assert.True (success (noParts).Trim ().IsSuccess);

		StubRanker ranker = new (7);
		Email ranked = new ("user@example.com");
		Assert.True (success (ranked).Rank (ranker).IsSuccess);
		Assert.Equal (7, ranked.StaticRank);
		Assert.Equal ("user@example.com", ranker.LastAddress);
	}

	[Fact]
	public void CommonTyposAndDomainAllowListsCoverAllOutcomes () {
		Assert.True (failure ().CommonTypos ().IsFailure);
		Assert.True (success (null).CommonTypos ().IsFailure);
		Assert.True (success (new Email ("user@example.com")).CommonTypos ().IsFailure);

		Email noLocal = new ("user@example.com") { Domain = "example.com" };
		Assert.True (success (noLocal).CommonTypos ().IsFailure);

		Email typo = new ("person@gmial.com") { LocalPart = "person", Domain = "gmial.com" };
		Result<Email> typoResult = success (typo).CommonTypos ();
		Assert.True (typoResult.IsFailure);
		Assert.Contains ("person@gmail.com", typoResult.ErrorMessage);

		Email customTypo = new ("person@bad.test") { LocalPart = "person", Domain = "bad.test" };
		Dictionary<string, TypoMatch> customMatches = new () {
			["bad.test"] = new TypoMatch ("bad.test", "good.test", "{typodomain}:{local}:{domain}")
		};
		Result<Email> customResult = success (customTypo).CommonTypos (customMatches);
		Assert.True (customResult.IsFailure);
		Assert.Contains ("bad.test:person:good.test", customResult.ErrorMessage);
		Assert.True (success (new Email ("person@example.com") {
			LocalPart = "person",
			Domain = "example.com"
		}).CommonTypos (customMatches).IsSuccess);

		Assert.True (failure ().DisallowTld ("test").IsFailure);
		Assert.True (success (null).DisallowTld ("test").IsFailure);
		Assert.True (success (new Email ("user@example.com")).DisallowTld ("test").IsFailure);
		Assert.True (success (new Email ("user@example.test") { Domain = "example.test" })
			.DisallowTld (" TEST ").IsFailure);
		Assert.True (success (new Email ("user@example.com") { Domain = "example.com" })
			.DisallowTld ("test").IsSuccess);

		Assert.True (failure ().DisallowDomains (["example.com"]).IsFailure);
		Assert.True (success (null).DisallowDomains (["example.com"]).IsFailure);
		Assert.True (success (new Email ("user@example.com")).DisallowDomains (["example.com"]).IsFailure);
		Email domain = new ("user@example.com") { Domain = "example.com" };
		Assert.True (success (domain).DisallowDomains (null!).IsFailure);
		Assert.True (success (domain).DisallowDomains ([]).IsFailure);
		Assert.True (success (domain).DisallowDomains (["example.com"]).IsFailure);
		Assert.True (success (domain).DisallowDomains (["other.com"]).IsSuccess);

		Assert.True (failure ().AllowDomains (["example.com"]).IsFailure);
		Assert.True (success (null).AllowDomains (["example.com"]).IsFailure);
		Assert.True (success (new Email ("user@example.com")).AllowDomains (["example.com"]).IsFailure);
		Assert.True (success (domain).AllowDomains (null!).IsFailure);
		Assert.True (success (domain).AllowDomains ([]).IsFailure);
		Assert.True (success (domain).AllowDomains (["other.com"]).IsFailure);
		Assert.True (success (domain).AllowDomains (["example.com"]).IsSuccess);
	}

	[Fact]
	public void TemporaryDomainListOverloadHandlesPreconditionsAndMatches () {
		Assert.True (failure ().DisallowTemporaryServiceDomains (["blocked.test"]).IsFailure);
		Assert.True (success (null).DisallowTemporaryServiceDomains (["blocked.test"]).IsFailure);
		Assert.True (success (new Email ("user@example.com")).DisallowTemporaryServiceDomains (["blocked.test"]).IsFailure);

		Email blocked = new ("user@blocked.test") { Domain = "blocked.test" };
		Assert.True (success (blocked).DisallowTemporaryServiceDomains (["blocked.test"]).IsFailure);
		Assert.True (success (blocked).DisallowTemporaryServiceDomains (["other.test"]).IsSuccess);
	}

	[Fact]
	public async Task TemporaryDomainHttpListHandlesErrorsRefreshDuplicatesAndCache () {
		TemporaryServiceConfig errorConfig = new ("https://list.test") {
			HttpMessageHandler = new StubHttpMessageHandler (HttpStatusCode.InternalServerError, "unavailable")
		};
		Result<Email> httpError = await success (new Email ("user@example.test") { Domain = "example.test" }).DisallowTemporaryServiceDomains (errorConfig);
		Assert.True (httpError.IsFailure);

		TemporaryServiceConfig initialConfig = new ("https://list.test") {
			HttpMessageHandler = new StubHttpMessageHandler (
				HttpStatusCode.OK,
				"Disposable.test\r\nDISPOSABLE.TEST\r\nblocked.test\n"
			)
		};
		Result<Email> initialLoad = await success (new Email ("user@safe.test") { Domain = "safe.test" }).DisallowTemporaryServiceDomains (initialConfig);
		Assert.True (initialLoad.IsSuccess);

		Result<Email> cachedBlock = await success (new Email ("user@disposable.test") { Domain = "disposable.test" }).DisallowTemporaryServiceDomains (new TemporaryServiceConfig ("https://list.test"));
		Assert.True (cachedBlock.IsFailure);
		Result<Email> cachedAllow = await success (new Email ("user@safe.test") { Domain = "safe.test" }).DisallowTemporaryServiceDomains (new TemporaryServiceConfig ("https://list.test"));
		Assert.True (cachedAllow.IsSuccess);

		// cache is still valid here, so only ForceRefresh triggers a reload
		TemporaryServiceConfig forceConfig = new ("https://list.test", -1) {
			ForceRefresh = true,
			HttpMessageHandler = new StubHttpMessageHandler (HttpStatusCode.OK, "refresh.test\n")
		};
		Result<Email> forcedLoad = await success (new Email ("user@refresh.test") { Domain = "refresh.test" }).DisallowTemporaryServiceDomains (forceConfig);
		Assert.True (forcedLoad.IsFailure);

		// the forced load used CacheHours = -1, so the cache is now expired
		TemporaryServiceConfig refreshConfig = new ("https://list.test") {
			HttpMessageHandler = new StubHttpMessageHandler (HttpStatusCode.OK, "after-refresh.test\n")
		};
		Result<Email> refreshed = await success (new Email ("user@refresh.test") { Domain = "refresh.test" }).DisallowTemporaryServiceDomains (refreshConfig);
		Assert.True (refreshed.IsSuccess);
		Email afterRefreshEmail = new ("user@after-refresh.test") { Domain = "after-refresh.test" };
		Result<Email> afterRefreshResult = await success (afterRefreshEmail).DisallowTemporaryServiceDomains (new TemporaryServiceConfig ("https://list.test"));
		Assert.True (
			afterRefreshResult.IsFailure,
			$"Expected refreshed domain to be blocked; IsSuccess={afterRefreshResult.IsSuccess}, " +
			$"Domain={afterRefreshResult.Value?.Domain}, Error={afterRefreshResult.ErrorMessage}"
		);
	}

	[Fact]
	public async Task MxVerificationHandlesPreconditionsAndBypassConfigurations () {
		Assert.True ((await failure ().VerifyMxRecords ()).IsFailure);
		Assert.True ((await success (null).VerifyMxRecords ()).IsFailure);
		Assert.True ((await success (new Email ("user@example.com")).VerifyMxRecords ()).IsFailure);

		Email defaultBypass = new ("user@gmail.com") { Domain = "gmail.com" };
		Assert.True ((await success (defaultBypass).VerifyMxRecords ()).IsSuccess);

		Email configuredBypass = new ("user@custom.test") { Domain = "custom.test" };
		Assert.True ((await success (configuredBypass).VerifyMxRecords (new MxConfig {
			DnsServers = ["127.0.0.1"],
			BypassDomains = ["custom.test"]
		})).IsSuccess);

		Email fallbackBypass = new ("user@gmail.com") { Domain = "gmail.com" };
		Assert.True ((await success (fallbackBypass).VerifyMxRecords (new MxConfig ())).IsSuccess);
	}

	[Fact]
	public void FullValidationRunsTheEntireSuccessfulAndFailurePipelines () {
		Result<Email> valid = Email.FullValidation ("User@gmail.com");
		Assert.True (valid.IsSuccess);
		Assert.Equal ("user@gmail.com", valid.Value!.Address);
		Assert.Equal ("user", valid.Value.LocalPart);
		Assert.Equal ("gmail.com", valid.Value.Domain);

		Assert.True (Email.FullValidation ("not-an-email").IsFailure);
	}

	// [Fact]
	// public void RankResponseAndTypoMatchExposeTheirState () {
	// 	RankResponse response = new ();
	// 	Assert.Equal (0, response.Rank);
	// 	Assert.Equal ("", response.Reason);
	// 	response.AddReason ("first");
	// 	response.AddReason ("second");
	// 	Assert.Equal ("first,second,", response.Reason);

	// 	TypoMatch match = new ("typo.test", "valid.test");
	// 	Assert.Equal ("typo.test", match.TypoDomain);
	// 	Assert.Equal ("valid.test", match.CorrectDomain);
	// 	Assert.Contains ("{local}@{domain}", match.ResponseTemplate);
	// 	match.TypoDomain = "other.test";
	// 	match.CorrectDomain = "fixed.test";
	// 	match.ResponseTemplate = "custom";
	// 	Assert.Equal ("other.test", match.TypoDomain);
	// 	Assert.Equal ("fixed.test", match.CorrectDomain);
	// 	Assert.Equal ("custom", match.ResponseTemplate);

	// 	MxConfig mxConfig = new () { DnsServers = ["dns.test"], BypassDomains = ["mail.test"] };
	// 	Assert.Contains ("dns.test", mxConfig.DnsServers);
	// 	Assert.Contains ("mail.test", mxConfig.BypassDomains);
	// 	TemporaryServiceConfig tempConfig = new ("https://example.test/list") { CacheHours = 5 };
	// 	Assert.Equal ("https://example.test/list", tempConfig.ListUrl);
	// 	Assert.Equal (5, tempConfig.CacheHours);
	// }

	// [Fact]
	// public void RankerCoversEarlyRejectionsAndScoringRules () {
	// 	Assert.Equal (10, new Ranker ().Test (null!).Rank);
	// 	Assert.Equal (10, new Ranker ().Test ("").Rank);
	// 	Assert.Equal ("null/empty email,", new Ranker ().Test ("").Reason);
	// 	Assert.Equal (10, new Ranker ().Test ("invalid").Rank);
	// 	Assert.Equal ("no @,", new Ranker ().Test ("invalid").Reason);
	// 	Assert.Equal (10, new Ranker ().Test ("user@example").Rank);
	// 	Assert.Equal ("no . in the domain,", new Ranker ().Test ("user@example").Reason);
	// 	Assert.Equal (10, new Ranker ().Test ("user@nodot").Rank);
	// 	Assert.Equal ("no .,", new Ranker ().Test ("user@nodot").Reason);
	// 	Assert.Equal (10, new Ranker ().Test ("bad local@example.com").Rank);
	// 	Assert.Equal (10, new Ranker ().Test ("bad..local@example.com").Rank);
	// 	Assert.Equal (10, new Ranker ().Test ("user@hardbounce.example.com").Rank);
	// 	Assert.Contains ("domain contains hardbounce", new Ranker ().Test ("user@hardbounce.example.com").Reason);

	// 	Assert.Equal ("", Ranker.JustNumbers (null!));
	// 	Assert.Equal ("", Ranker.JustNumbers (""));
	// 	Assert.Equal ("12345", Ranker.JustNumbers ("a1b2-345"));
	// 	Assert.False (Ranker.EmailLocalIsValid (null!));
	// 	Assert.False (Ranker.EmailLocalIsValid (""));
	// 	Assert.False (Ranker.EmailLocalIsValid ("bad local"));
	// 	Assert.False (Ranker.EmailLocalIsValid ("bad..local"));
	// 	Assert.True (Ranker.EmailLocalIsValid ("good.local+tag"));

	// 	Ranker.AddBadWords (null!);
	// 	Ranker.AddBadWords ([]);
	// 	Ranker.AddBadWords (["ignored"]);
	// 	Assert.Throws<NullReferenceException> (() => Ranker.AddBadWords (null!));

	// 	Assert.Contains ("local length > 20", new Ranker ().Test ($"{'a' * 21}@example.com").Reason);
	// 	Assert.Contains ("more than 7 numbers in local", new Ranker ().Test ("12345678@example.com").Reason);
	// 	Assert.Contains ("more than 14 numbers in local", new Ranker ().Test ("123456789012345@example.com").Reason);
	// 	Assert.Contains ("more than 80% numbers in local", new Ranker ().Test ("12345678a@example.com").Reason);
	// 	Assert.Contains ("local = test", new Ranker ().Test ("test@example.com").Reason);
	// 	Assert.Contains ("local = asdf", new Ranker ().Test ("asdf@example.com").Reason);
	// 	Assert.Contains ("domain = yahoo.com", new Ranker ().Test ("user@yahoo.com").Reason);
	// 	Assert.Contains ("domain = reply.facebook.com", new Ranker ().Test ("user@reply.facebook.com").Reason);
	// 	Assert.Contains ("domain = email.zillow.com", new Ranker ().Test ("user@email.zillow.com").Reason);
	// 	Assert.Contains ("domain = reply.craigslist.org", new Ranker ().Test ("user@reply.craigslist.org").Reason);
	// 	Assert.Contains ("domain = reply.linkedin.com", new Ranker ().Test ("user@reply.linkedin.com").Reason);
	// 	Assert.Contains ("domain = test.com", new Ranker ().Test ("user@test.com").Reason);
	// 	Assert.Contains ("domain starts with reply.", new Ranker ().Test ("user@reply.example.com").Reason);
	// 	Assert.Contains ("domain has more than usual segments", new Ranker ().Test ("user@mail.example.com").Reason);
	// 	Assert.Contains ("domain tld is 1 char", new Ranker ().Test ("user@example.c").Reason);
	// 	Assert.Contains ("domain tld is edu", new Ranker ().Test ("user@example.edu").Reason);
	// 	Assert.Contains ("domain tld is longer than 4 characters", new Ranker ().Test ("user@example.technology").Reason);
	// 	Assert.Contains ("local starts with reply", new Ranker ().Test ("replyperson@example.com").Reason);
	// 	Assert.Contains ("local starts with reply-", new Ranker ().Test ("reply-person@example.com").Reason);
	// 	Assert.Contains ("local contains trigger words", new Ranker ().Test ("shitspam@example.com").Reason);
	// 	Assert.DoesNotContain ("local contains trigger words", new Ranker ().Test ("ordinary@example.com").Reason);
	// 	Assert.True (new Ranker ().Test ("user@test.com").Rank >= 10);
	// 	Assert.True (new Ranker ().Test ("reply-" + new string ('x', 15) + "@reply.example.technology").Rank >= 10);
	// }

	private static Result<Email> success (Email? email) {
		return Result<Email>.Success<Email> (email!);
	}

	private static Result<Email> failure () {
		return Result<Email>.Failure<Email> (-1, "test failure");
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
	private sealed class StubRanker (int rank) : IRanker
	{
		public string? LastAddress { get; private set; }

		public RankResponse Test (string email) {
			LastAddress = email;
			return new RankResponse { Rank = rank };
		}
	}
}
