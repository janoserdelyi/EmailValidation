namespace Test;

using com.janoserdelyi.EmailValidation;
using com.janoserdelyi.Validation;

public class Program
{
	public static void Main (
		string[] args
	) {

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
			interrupted = interrupted.VerifyMxRecords ();
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