
namespace com.janoserdelyi.EmailValidation;

// 2017-11-16 first stab! not sure what i'm doing yet...
public static class Ranker
{
	// general ideas - 
	//	high numbers are bad
	//	exit after 10 is reached to exit early

	// this can be used before or after more extended checks like looking against a database or dns lookups or telnet verification of a mx domain
	// ideally before. this should be a quicker and less expensive operation (no networking involved being a biggie)

	public static RankResponse Test (
		string email
	) {
		const int MAX = 10;

		RankResponse resp = new () { Rank = 0 };

		// first some easy kills
		if (string.IsNullOrEmpty (email)) {
			resp.Rank = MAX;
			resp.AddReason ("null/empty email");
			return resp;
		}
		if (!email.Contains ('@')) {
			resp.Rank = MAX;
			resp.AddReason ("no @");
			return resp;
		}
		if (!email.Contains ('.')) {
			resp.Rank = MAX;
			resp.AddReason ("no .");
			return resp;
		}
		if (!email.Split ('@')[1].Contains ('.')) {
			resp.Rank = MAX;
			resp.AddReason ("no . in the domain");
			return resp;
		}

		// let's break out the parts
		string local = email.Split ('@')[0].ToLower ();
		string domain = email.Split ('@')[1].ToLower ();

		if (!EmailLocalIsValid (local)) {
			resp.Rank = MAX;
			resp.AddReason ("invalid local");
			return resp;
		}

		if (domain.IndexOf ("hardbounce") > -1) {
			resp.Rank = MAX;
			resp.AddReason ("domain contains hardbounce");
			return resp;
		}

		// a long local length is suspect
		if (local.Length > 20) {
			resp.AddReason ("local length > 20");
			resp.Rank++;
		}

		// a lot of numbers in the local is suspect
		string justnumbers = JustNumbers (local);
		// just a lot of numbers regardless of local size
		if (justnumbers.Length > 7) {
			resp.AddReason ("more than 7 numbers in local");
			resp.Rank++;
		}
		if (justnumbers.Length > 14) {
			resp.AddReason ("more than 14 numbers in local");
			resp.Rank = resp.Rank + 2;
		}

		// now for proportion
		if (justnumbers.Length > 0 && (justnumbers.Length * 1.0 / local.Length) > .8) {
			resp.AddReason ("more than 80% numbers in local");
			resp.Rank++;
		}

		// i should probably do the same as above for non-alphanumeric characters

		// any number sequence greater than 4 is suspicious. i see a lot of birthday years, graduation years, etc so 4 will be considered ok

		// some common bogus inputs
		// if this list grows i'll make a List<string> and test contains
		if (local == "test") {
			resp.AddReason ("local = test");
			resp.Rank++;
		}
		if (local == "asdf") {
			resp.AddReason ("local = asdf");
			resp.Rank++;
		}

		// some domains are just poop
		switch (domain) {
			case "yahoo.com":
				// all of yahoo got breached. this puts some stink on any yahoo account
				resp.AddReason ("domain = yahoo.com");
				resp.Rank++;
				break;
			case "reply.facebook.com":
				// yeah.... no
				resp.AddReason ("domain = reply.facebook.com");
				resp.Rank += 8;
				break;
			case "email.zillow.com":
				resp.AddReason ("domain = email.zillow.com");
				resp.Rank += 8;
				break;
			case "reply.craigslist.org":
				resp.AddReason ("domain = reply.craigslist.org");
				resp.Rank += 10;
				break;
			case "reply.linkedin.com":
				resp.AddReason ("domain = reply.linkedin.com");
				resp.Rank += 8;
				break;
			case "test.com": // yes this is a legit domain, but it's still not a good sign
				resp.AddReason ("domain = test.com");
				resp.Rank += 10;
				break;
		}

		// ===============================================
		// check if it's bad enough
		if (resp.Rank >= 10) {
			return resp;
		}
		// ===============================================

		if (domain.StartsWith ("reply.")) {
			resp.AddReason ("domain starts with reply.");
			resp.Rank += 7;
		}

		// if the last domain segment is 1 character
		string[] domainSegments = domain.Split ('.');
		string domaintail = domainSegments[domainSegments.Length - 1];

		if (domainSegments.Length > 2) {
			resp.AddReason ("domain has more than usual segments");
			resp.Rank += domainSegments.Length - 2; // bump it based on segment count
		}

		if (domaintail.Length == 1) {
			resp.AddReason ("domain tld is 1 char");
			resp.Rank++;
		} else {
			// these systems can be problematic, plus aging out accounts
			if (domaintail == "edu") {
				resp.AddReason ("domain tld is edu");
				resp.Rank++;
			}

			if (domaintail.Length > 4) {
				resp.AddReason ("domain tld is longer than 4 characters");
				resp.Rank += 2;
			}
		}

		// i see emails for zillow and others start with "reply-". baaaad
		if (local.StartsWith ("reply")) {
			resp.AddReason ("local starts with reply");
			resp.Rank++;
		}
		if (local.StartsWith ("reply-")) {
			resp.AddReason ("local starts with reply-");
			resp.Rank++;
		}

		// ===============================================
		// check if it's bad enough
		if (resp.Rank >= 10) {
			return resp;
		}
		// ===============================================

		// it might be bad word time
		HashSet<string> words = getBadWords ();
		foreach (string word in words) {
			if (local.IndexOf (word) > -1) {
				resp.AddReason ("local contains trigger words");
				resp.Rank += 2;
				//break; // hrmmm. i should let it stack
			}
		}

		return resp;
	}

	// copying some things from com.nestiny.common. i don't want deps on this
	public static string JustNumbers (string input) {
		if (string.IsNullOrEmpty (input)) {
			return input;
		}
		return System.Text.RegularExpressions.Regex.Replace (input, "[^\\d]+", "");
	}

	// email - local part checking only. (the part before the @)
	public static bool EmailLocalIsValid (string local) {
		if (string.IsNullOrEmpty (local)) {
			return false;
		}
		// testing if the local part contains only valid chars
		const string pattern = @"[^a-zA-Z0-9\!\#\$\%\&'\*\+\-\/\=\?\^_\`\{\|\}\~\.]+";
		//const string local = "123abc.!#$%&'*+-/=?^_`{|}~,";
		//Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch(local, pattern));

		bool isMatch = !System.Text.RegularExpressions.Regex.IsMatch (local, pattern);
		if (!isMatch) {
			return false;
		}

		// technically i should also be checking that there are no consecutive dots
		// so let's do that. just looking for 2 in a row. this will catch 3,4,etc in a row. none are acceptable
		return local.IndexOf ("..") == -1;
	}

	// this could obviously be expanded and likely some localization should be taken into account
	private static HashSet<string> getBadWords () {
		if (badwords.Count == 0) {
			badwords.Add ("shit");
			badwords.Add ("fuck");
			badwords.Add ("asshole");
			badwords.Add ("spam");
		}
		return badwords;
	}

	private static HashSet<string> badwords = new HashSet<string> ();
}
