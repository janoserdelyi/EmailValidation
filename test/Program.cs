namespace Test;

using System.Runtime.CompilerServices;
using com.janoserdelyi.EmailValidation;
using com.janoserdelyi.Validation;
using MailVerifier;
using Xunit;

// public class EmailValidationTest
// {
// 	[Theory]
// 	[InlineData (" FirstName@foo.com ")]
// 	public void EmailValidation_Supplied (string value) {

// 		var val = Email.Validator (value);

// 		Assert.True (val.IsSuccess, "Email has been lower-cased");
// 	}

// 	[Theory]
// 	[InlineData (" FirstName@foo.com ")]
// 	public void EmailValidation_Trimmed (string value) {

// 		var val = Email.Validator (value).Trim ();

// 		Assert.True (val.Value?.Address == "FirstName@foo.com", "Email has been lower-cased");
// 	}

// 	[Theory]
// 	[InlineData (" FirstName@foo.com ")]
// 	public void EmailValidation_Lower (string value) {

// 		var val = Email.Validator (value).Trim ().Lower ();

// 		Assert.True (val.Value?.Address == "firstname@foo.com", "Email has been lower-cased");
// 	}

// 	[Theory]
// 	[InlineData ("a@b.c")]
// 	public void EmailValidation_Length (string value) {

// 		var val = Email.Validator (value).Ensure (
// 			e => Email.IsLongEnough (e.Address, 5),
// 			(int)Error.TooShort,
// 			"Invalid format - email is too short to be real"
// 		);

// 		Assert.True (val.IsSuccess, "Email is at least 5 characters long");
// 	}

// 	[Theory]
// 	[InlineData ("a@b.c")]
// 	public void EmailValidation_ValidFormat (string value) {

// 		var val = Email.Validator (value).ValidateFormat ();

// 		Assert.True (val.IsSuccess, $"'{value}' is a valid format");
// 	}

// 	[Theory]
// 	[InlineData ("a@b.c")]
// 	public void EmailValidation_Parse (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ();

// 		Assert.True (val.IsSuccess, $"'{value}' is a valid format to parse");
// 		Assert.True (val.Value?.Parsed, $"'{value}' parsed successfully");
// 		Assert.True (val.Value?.LocalPart == "a", $"'{value}' parsed localpart successfully");
// 		Assert.True (val.Value?.Domain == "b.c", $"'{value}' parsed domain successfully");
// 	}


// 	[Theory]
// 	[InlineData ("a@b.edu")]
// 	public void EmailValidation_Disallow_TLD (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ().DisallowTld ("edu");

// 		Assert.True (val.IsFailure, $"'{value}' .edu domains are not allowed");
// 	}

// 	[Theory]
// 	[InlineData ("example@foo.bar")]
// 	[InlineData ("example@bar.baz")]
// 	public void EmailValidation_Disallow_domains (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ().DisallowDomains (new List<string> { "foo.bar", "bar.baz" });

// 		Assert.True (val.IsFailure, $"'{value}'s domain is not allowed");
// 	}

// 	[Theory]
// 	[InlineData ("example@gmail.com")]
// 	[InlineData ("example@yahoo.com")]
// 	public void EmailValidation_Allow_Only_Domains (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ().AllowDomains (new List<string> { "foo.bar", "bar.baz" });

// 		Assert.True (val.IsFailure, $"'{value}'s domain is not allowed");
// 	}

// 	[Theory]
// 	[InlineData ("8==D~~~@fake.com")]
// 	[InlineData ("{$$$$}@fake.com")]
// 	public void EmailValidation_Local_Is_Valid (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ().LocalIsValid ();

// 		Assert.True (val.IsSuccess, $"'{value}' amazingly has a valid local part");
// 	}

// 	[Theory]
// 	[InlineData ("foo@gmial.com")]
// 	[InlineData ("foo@gamil.com")]
// 	[InlineData ("foo@gmaul.com")]
// 	[InlineData ("foo@gnail.com")]
// 	[InlineData ("foo@gmai.com")]
// 	[InlineData ("foo@gmsil.com")]
// 	[InlineData ("foo@ail.com")]
// 	[InlineData ("foo@hitnail.com")]
// 	[InlineData ("foo@hitmail.com")]
// 	[InlineData ("foo@hotnail.com")]
// 	public void EmailValidation_Common_Typos (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ().CommonTypos ();

// 		Assert.True (val.IsFailure, val.ErrorMessage);
// 	}

// 	// i need to do ranking

// 	[Theory]
// 	[InlineData ("foo@gmail.com")]
// 	[InlineData ("foo@yahoo.com")]
// 	[InlineData ("foo@aol.com")]
// 	[InlineData ("foo@protonmail.com")]
// 	public void EmailValidation_Verify_Mx (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ().VerifyMxRecords (config: new MxConfig () {
// 			DnsServers = new System.Collections.Generic.List<string> () { "1.1.1.1" },
// 			BypassDomains = new System.Collections.Generic.List<string> () { "gmail.com", "yahoo.com", "aol.com" }
// 		}).Result;

// 		Assert.True (val.IsSuccess, $"'{value}'s domain has MX records");
// 	}

// 	[Theory]
// 	[InlineData ("janos@janoserdelyi.com")]
// 	public void EmailValidation_Verify_Mailserver (string value) {

// 		var val = Email.Validator (value).Ensure (
// 			e => emailHasRealServer (e.Address!),
// 			128,
// 			"real email server not found for this address"
// 		);
// 	}

// 	[Theory]
// 	[InlineData ("a@losemymail.com")]
// 	public void EmailValidation_TemporaryDomain (string value) {

// 		var val = Email.Validator (value).ValidateFormat ().Parse ().DisallowTemporaryServiceDomains (
// 			new TemporaryServiceConfig (
// 				"https://raw.githubusercontent.com/disposable/disposable-email-domains/master/domains.txt",
// 				1
// 			)
// 		).Result;

// 		Assert.True (val.IsFailure, $"'{value}' is a blocked domain");
// 	}

// 	private static bool emailHasRealServer (
// 		string emailAddress
// 	) {
// 		MailVerifier.Verify.AddDns ("1.1.1.1");
// 		MailVerifier.Verify.AddDns ("208.67.220.220");

// 		MailVerifier.Response? response;

// 		try {
// 			response = MailVerifier.Verify.Check (emailAddress).Result;
// 		} catch (Exception oops) {
// 			Console.WriteLine (oops);
// 			return false;
// 		}

// 		return response.Success;
// 	}
//}


public class Program {
	public static void Main (
	//string[] args
	) {

		var localpartUnicode = Email.Validator ("jános@janoserdelyi.com").Lower ().Trim ().ValidateFormat ().Parse ().LocalIsValid ();
		var localpartASCII = Email.Validator ("janos@janoserdelyi.com").Lower ().Trim ().ValidateFormat ().Parse ().LocalIsValid ();

		var tempServiceExample = Email.Validator ("a@losemymail.com").ValidateFormat ().Parse ().DisallowTemporaryServiceDomains (
			new TemporaryServiceConfig (
				"https://raw.githubusercontent.com/disposable/disposable-email-domains/master/domains.txt",
				1
			)
		).Result;

		var tempServiceExample2 = Email.Validator ("a@losemymail.com").ValidateFormat ().Parse ().DisallowTemporaryServiceDomains (
			new TemporaryServiceConfig (
				"https://raw.githubusercontent.com/disposable/disposable-email-domains/master/domains.txt",
				1
			)
		).Result;

		// my example piece
		var chainedValidationExample = Email.Validator (" boo@FOO.bar.boo ")
			.Lower ()
			.Trim ()
			// this is how you can inject any custom checks anywhere in the pipeline. the checks just need to return a boolean for success/fail
			.Ensure (
				e => Email.IsLongEnough (e.Address, 5),
				(int)Error.TooShort,
				"Invalid format - email is too short to be real"
			)
			.ValidateFormat ()
			.Parse ()
			.DisallowTld ("edu")
			.DisallowDomains (new List<string> { "foo.bar", "bar.baz" })
			.AllowDomains (new List<string> { "bread.com" })
			.LocalIsValid ()
			.CommonTypos ()
			.Rank ()
			.VerifyMxRecords (config: new MxConfig () {
				DnsServers = new System.Collections.Generic.List<string> () { "1.1.1.1" },
				BypassDomains = new System.Collections.Generic.List<string> () { "gmail.com", "yahoo.com", "aol.com" }
			});

		// testing to make sure i can take an existing result, operate on it, then pick it back up for further validation
		var interrupted = Email.Validator ("foobar@yahoo.com")
			.ValidateFormat ()
			.Parse ()
			.Lower ()
			.Trim ()
			.LocalIsValid ()
			.CommonTypos ()
			.Rank ();

		if (interrupted.IsSuccess == true && interrupted.Value.StaticRank > 0) {
			interrupted = interrupted.VerifyMxRecords ().Result;
		}

		Console.WriteLine ();

		var bundled = Email.FullValidation (" FOOB@messytheface.com ").DisallowTld ("edu").DisallowTld ("com");

		if (bundled.IsSuccess == true) {
			Console.WriteLine ($"{bundled.Value.Address} rank : {bundled.Value.StaticRank}");
		} else {
			Console.WriteLine ($"failure : {bundled.ErrorMessage}");
		}

		Console.WriteLine ("done");

	}
}
