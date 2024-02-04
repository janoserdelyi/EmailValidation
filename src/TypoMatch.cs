namespace com.janoserdelyi.EmailValidation;

public class TypoMatch
{
	public TypoMatch (
		string typoDomain,
		string correctDomain,
		string responseTemplate = "'{typodomain}' is usually a typo. Did you mean '{local}@{domain}'?"
	) {
		TypoDomain = typoDomain;
		CorrectDomain = correctDomain;
		ResponseTemplate = responseTemplate;
	}

	public string TypoDomain { get; set; }
	public string CorrectDomain { get; set; }
	public string ResponseTemplate { get; set; }
}
