namespace Test;

using com.janoserdelyi.EmailValidation;

public class Program
{
	public static void Main (
		string[] args
	) {


		var newchain = Email.Validator (" FOO@messytheface.com ")
			.ValidateFormat ()
			.Parse ()
			.Lower ()
			.Trim ()
			.LocalIsValid ()
			.CommonTypos ()
			.Rank ()
			.VerifyMxRecords (config: new MxConfig () {
				DnsServers = new System.Collections.Generic.List<string> () { "1.1.1.1" },
				BypassDomains = new System.Collections.Generic.List<string> () { "gmail.com", "yahoo.com", "aol.com" }
			});

		var bundled = Email.FullValidation (" FOOB@messytheface.com ").DisallowTld ("edu").DisallowTld ("com");

		if (bundled.IsSuccess == true) {
			Console.WriteLine ($"{bundled.Value.Address} rank : {bundled.Value.StaticRank}");
		} else {
			Console.WriteLine ($"failure : {bundled.ErrorMessage}");
		}

		Console.WriteLine ("done");

	}
}