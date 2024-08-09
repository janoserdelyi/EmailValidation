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
}
