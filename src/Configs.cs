namespace com.janoserdelyi.EmailValidation;

public class MxConfig
{
	public System.Collections.Generic.IList<string> DnsServers { get; set; } = new System.Collections.Generic.List<string> ();
	public System.Collections.Generic.IList<string> BypassDomains { get; set; } = new System.Collections.Generic.List<string> ();
}