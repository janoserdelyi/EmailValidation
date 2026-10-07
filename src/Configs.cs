namespace com.janoserdelyi.EmailValidation;

public class MxConfig
{
	public System.Collections.Generic.IList<string> DnsServers { get; set; } = new System.Collections.Generic.List<string> ();
	public System.Collections.Generic.IList<string> BypassDomains { get; set; } = new System.Collections.Generic.List<string> ();
}

public class TemporaryServiceConfig
{
	public TemporaryServiceConfig (
		string listUrl,
		int cacheHours = 24
	) {
		ListUrl = listUrl;
		CacheHours = cacheHours;
	}

	public string ListUrl { get; set; }
	public int CacheHours { get; set; }
	public bool ForceRefresh { get; set; }
	public System.Net.Http.HttpMessageHandler? HttpMessageHandler { get; set; }
}

// everything ValidationManager needs is supplied here, once, at construction time -
// so the per-call Validate method never has to take any services of its own
public class ValidationManagerOptions
{
	public IList<string> DnsServers { get; set; } = new List<string> ();
	public IList<string> BypassDomains { get; set; } = new List<string> ();

	// block list mode - everything is allowed except these. mutually exclusive with AllowedDomains
	public IList<string> DisallowedDomains { get; set; } = new List<string> ();

	// allow list mode - nothing is allowed except these. mutually exclusive with DisallowedDomains
	public IList<string> AllowedDomains { get; set; } = new List<string> ();
	public Dictionary<string, TypoMatch>? TypoMatches { get; set; }
	public TemporaryServiceConfig? TemporaryServiceConfig { get; set; }

	// optional - plug in your own persistence for known good/bad domains
	public IEmailDomainRepo? EmailDomainRepo { get; set; }

	// optional - plug in your own logging. called with a message for best-effort failures (eg. repo writes)
	public Action<string>? Logger { get; set; }
}
