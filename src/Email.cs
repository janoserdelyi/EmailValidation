namespace com.janoserdelyi.EmailValidation;

using System;
using com.janoserdelyi.Validation;

// i want to test something like

// var email = new Email("foo@bar.com").Lower().Trim().CheckFormat().Score().CheckMx();

public class Email
{
	public Email () {

	}

	public Email (
		string address
	) {
		Address = address;
	}

	public string? Address { get; set; }
	public string? LocalPart { get; set; }
	public string? Domain { get; set; }
	public bool Parsed { get; set; } = false;
	public bool ValidFormat { get; set; } = false;
	public int StaticRank { get; set; } = 0; // the rank based just off the value of the email itself-  no network checks

	public override string? ToString () {
		return Address;
	}

	// trying out something different
	public static Result<Email> Validator (
		string address
	) {
		var email = new Email (address);

		return Result<Email>.Evaluate<Email> (email);
	}

	public static Result<Email> FullValidation (
		string address
	) {
		return Validator (address)
			.ValidateFormat ()
			.Parse ()
			.Lower ()
			.Trim ()
			.LocalIsValid ()
			.CommonTypos ()
			.Rank ()
			.VerifyMxRecords ();
	}

	public Result<Email> Validate (

	) {
		return Result<Email>.Evaluate<Email> (this)
			.Ensure<Email> (
				e => AddressIsNotEmpty (e.Address),
				(int)Error.Empty,
				"No email address provided"
			).Ensure (
				e => IsLongEnough (e.Address, 5),
				(int)Error.TooShort,
				"Invalid format - email is too short to be real"
			)
			.Ensure (
				e => IsValidFormat (e),
				(int)Error.InvalidFormat,
				$"Invalid format. This email '{this.Address}' can't be real"
			).Map<Email, Email> (ParseParts);
	}

	// core validations without abstraction
	public static bool AddressIsNotEmpty (
		string? val
	) {
		return !string.IsNullOrEmpty (val);
	}

	public static bool IsLongEnough (
		string? val,
		int lengthmin
	) {
		if (string.IsNullOrEmpty (val)) {
			return false;
		}

		return val.Length >= Math.Abs (lengthmin); // Abs to avoid anyone being goofy with negative numbers
	}

	public static bool IsLength (
		string? val,
		int length
	) {
		if (string.IsNullOrEmpty (val)) {
			return false;
		}

		return val.Length == Math.Abs (length); // Abs to avoid anyone being goofy with negative numbers
	}

	public static bool IsValidFormat (
		Email? email
	) {
		if (email == null) {
			return false;
		}

		if (string.IsNullOrEmpty (email.Address)) {
			return false;
		}

		// this is crappy checking, but just for sake of this test
		string[] parts = email.Address.Split ('@');

		if (parts.Length != 2) {
			return false;
		}

		if (parts[1].IndexOf ('.') == -1) {
			return false;
		}

		return true;
	}

	public static Email? ParseParts (
		Email? email
	) {
		if (email == null) {
			return null;
		}

		if (string.IsNullOrEmpty (email.Address)) {
			return null;
		}

		// i'm assuming IsValidFormat has been run on this already
		return new Email () {
			Address = email.Address.ToLower ().Trim (),
			LocalPart = email.Address.Split ('@')[0],
			Domain = email.Address.Split ('@')[1]
		};
	}

	public static bool LocalPartIsValid (string local) {
		if (string.IsNullOrEmpty (local)) {
			return false;
		}
		// testing if the local part contains only valid chars
		const string pattern = @"[^a-zA-Z0-9\!\#\$\%\&'\*\+\-\/\=\?\^_\`\{\|\}\~\.]+";
		//const string local = "123abc.!#$%&'*+-/=?^_`{|}~,";
		//Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch(local, pattern));

		// technically i should also be checking that there are no consecutive dots 
		return !System.Text.RegularExpressions.Regex.IsMatch (local, pattern); // returning the inverse. this is checking that the local has something other that what the pattern lays out
	}


}

public static class EmailValidationExtensions
{

	public static Result<Email> ValidateFormat (
		this Result<Email> result
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot validate format");
		}

		if (result.Value.ValidFormat == true) {
			return result;
		}

		var isvalid = Email.IsValidFormat (result.Value);

		if (isvalid == false) {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, "Invalid email format");
		}

		result.Value.ValidFormat = isvalid;

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> Parse (
		this Result<Email> result
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot parse");
		}

		if (result.Value.Parsed == true) {
			return result;
		}

		var parsedEmail = Email.ParseParts (result.Value);

		if (parsedEmail == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "Missing address");
		}

		result.Value.Parsed = true;
		result.Value.LocalPart = parsedEmail.LocalPart;
		result.Value.Domain = parsedEmail.Domain;

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> LocalIsValid (
		this Result<Email> result
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot validate local part");
		}

		if (result.Value.ValidFormat == false) {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, "Format validation must be done prior to checking the local part");
		}

		if (result.Value.Parsed == false) {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, "Parsing must be done prior to checking the local part");
		}

		if (result.Value.LocalPart == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No local part provided, cannot validate");
		}

		var isvalid = Email.LocalPartIsValid (result.Value.LocalPart);

		if (isvalid == false) {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, "Invalid email format - local part");
		}

		result.Value.ValidFormat = isvalid;

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> Lower (
		this Result<Email> result
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot lowercase");
		}

		if (result.Value.Address == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot lowercase");
		}

		result.Value.Address = result.Value.Address.ToLower ();

		if (result.Value.LocalPart != null) {
			result.Value.LocalPart = result.Value.LocalPart.ToLower ();
		}

		if (result.Value.Domain != null) {
			result.Value.Domain = result.Value.Domain.ToLower ();
		}

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> Trim (
		this Result<Email> result
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot trim");
		}

		if (result.Value.Address == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot trim");
		}

		result.Value.Address = result.Value.Address.Trim ();

		if (result.Value.LocalPart != null) {
			result.Value.LocalPart = result.Value.LocalPart.Trim ();
		}

		if (result.Value.Domain != null) {
			result.Value.Domain = result.Value.Domain.Trim ();
		}

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> Rank (
		this Result<Email> result
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot rank");
		}

		result.Value.StaticRank = EmailRank.Ranker.Test (result.Value.Address).Rank; // generally 10 and up is bad

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> CommonTypos (
		this Result<Email> result
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot check common typos");
		}

		if (result.Value.Domain == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No domain parsed from email, cannot check common typos");
		}

		if (result.Value.LocalPart == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No local part parsed from email, cannot check common typos");
		}

		string local = result.Value.LocalPart;
		string domain = result.Value.Domain;

		if (domain == "gmial.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is a squatter domain. Did you mean '{local}@gmail.com'?");
		}
		if (domain == "gamil.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@gmail.com'?");
		}
		if (domain == "gmaul.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@gmail.com'?");
		}
		if (domain == "gnail.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@gmail.com'?");
		}
		if (domain == "gmai.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@gmail.com'?");
		}
		if (domain == "gmsil.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@gmail.com'?");
		}
		if (domain == "ail.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is a squatter domain. Did you mean '{local}@aol.com'?");
		}
		if (domain == "hitnail.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@hotmail.com'?");
		}
		if (domain == "hitmail.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@hotmail.com'?");
		}
		if (domain == "hotnail.com") {
			return Result<Email>.Failure<Email> ((int)Error.InvalidFormat, $"'{domain}' is usually a typo. Did you mean '{local}@hotmail.com'?");
		}

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> DisallowTld (
		this Result<Email> result,
		string tldToBlock
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot check common typos");
		}

		if (result.Value.Domain == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No domain parsed from email, cannot check common typos");
		}

		// i know TLD's are a freaking mess. i'm just using the old original version - the type where the content is after the last dot of the domain not this double-dipping crap like .co.uk!
		string tld = result.Value.Domain.Substring (result.Value.Domain.LastIndexOf ('.') + 1);

		if (tldToBlock.StartsWith ('.')) {
			tldToBlock = tldToBlock.Substring (1);
		}

		tldToBlock = tldToBlock.ToLower ().Trim ();

		if (tld == tldToBlock) {
			return Result<Email>.Failure<Email> ((int)Error.NotAllowed, $"'{tld}' is not an allowed TLD");
		}

		return Result<Email>.Success<Email> (result.Value);
	}

	public static Result<Email> VerifyMxRecords (
		this Result<Email> result,
		MxConfig? config = null
	) {
		if (result.IsFailure == true) {
			return result;
		}

		if (result.Value == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No email provided, cannot verify MX records");
		}

		if (result.Value.Domain == null) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, "No domain parsed from email, cannot verify MX records");
		}

		if (config == null) {
			foreach (string dnsserver in defaultDnsServers) {
				MailVerifier.Verify.AddDns (dnsserver);
			}
			foreach (string bypassdomain in defaultBypassDomains) {
				MailVerifier.Verify.AddBypassDomain (bypassdomain);
			}
		} else {
			if (config.DnsServers.Count > 0) {
				foreach (string dnsserver in config.DnsServers) {
					MailVerifier.Verify.AddDns (dnsserver);
				}
			} else {
				foreach (string dnsserver in defaultDnsServers) {
					MailVerifier.Verify.AddDns (dnsserver);
				}
			}

			if (config.BypassDomains.Count > 0) {
				foreach (string bypassdomain in config.BypassDomains) {
					MailVerifier.Verify.AddBypassDomain (bypassdomain);
				}
			} else {
				foreach (string bypassdomain in defaultBypassDomains) {
					MailVerifier.Verify.AddBypassDomain (bypassdomain);
				}
			}
		}

		MailVerifier.Response r = null;
		try {
			r = MailVerifier.Verify.Check (result.Value.Address);
		} catch (System.ArgumentNullException oops) {
			r = new MailVerifier.Response ();
			r.Success = false;
			r.Message = oops.Message;
		} catch (System.ArgumentException oops) {
			r = new MailVerifier.Response ();
			r.Success = false;
			r.Message = oops.Message;
		} catch (Exception oops) {
			r = new MailVerifier.Response ();
			r.Success = false;
			r.Message = "Unknown exception. Record not added. " + oops.Message;
		}

		if (r.Success == false) {
			return Result<Email>.Failure<Email> ((int)Error.Empty, r.Message);
		}

		// adding this domain as a bypass domain for subsequent checks
		MailVerifier.Verify.AddBypassDomain (result.Value.Domain);

		return Result<Email>.Success<Email> (result.Value);
	}

	private static System.Collections.Generic.IList<string> defaultDnsServers = new System.Collections.Generic.List<string> () { "208.67.222.222", "208.67.220.220", "1.1.1.1" };
	private static System.Collections.Generic.IList<string> defaultBypassDomains = new System.Collections.Generic.List<string> () { "messytheface.com", "gmail.com", "yahoo.com", "live.com", "outlook.com", "aol.com" };

}