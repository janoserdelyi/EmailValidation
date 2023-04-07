# EmailValidation
consolidating a ton of email validation stuff. also aiming for a functional style

An example : 

```csharp
var chainedValidationExample = Email.Validator (" FOO ")
	.Lower ()
	.Trim ()
	// this is how you can inject any custom checks anywhere in the pipeline. the checks just need to return a boolean for success/fail
	// ideally you're hitting up the lowest complexity/computation actions first
	.Ensure (
		e => Email.IsLongEnough (e.Address, 5),
		(int)Error.TooShort,
		"Invalid format - email is too short to be real"
	)
	.ValidateFormat ()
	.Parse ()
	.DisallowTld ("edu")
	.LocalIsValid ()
	.CommonTypos ()
	.Rank ()
	.VerifyMxRecords (config: new MxConfig () {
		DnsServers = new System.Collections.Generic.List<string> () { "1.1.1.1" },
		BypassDomains = new System.Collections.Generic.List<string> () { "gmail.com", "yahoo.com", "aol.com" }
	});
```

this will return a `Result<Email>` type which can be inspected for success or failure. If a success the `Value` will contain the `Email` object. Failures will not contain a `Value` but will have an `ErrorCode` and `ErrorMessage`.
