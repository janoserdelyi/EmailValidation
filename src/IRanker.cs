namespace com.janoserdelyi.EmailValidation;

public interface IRanker
{
	RankResponse Test (string email);
}