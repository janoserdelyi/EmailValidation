namespace com.janoserdelyi.EmailValidation;

// optional - implement this against whatever storage you use to persist known good/bad domains
public interface IEmailDomainRepo
{
	Task<IEnumerable<EmailDomainDto>?> SelectGoodDomains (CancellationToken cancellationToken = default);
	Task<IEnumerable<EmailDomainDto>?> SelectBadDomains (CancellationToken cancellationToken = default);
	Task Upsert (EmailDomainDto domain, CancellationToken cancellationToken = default);
}

public class EmailDomainDto
{
	public string Domain { get; set; } = string.Empty;
	public bool IsGood { get; set; }
	public string? Reason { get; set; }
	public DateTime CreatedDate { get; set; }
}
