namespace com.janoserdelyi.EmailValidation;

public class RankResponse
{
	public RankResponse () {

	}

	public int Rank { get; set; }

	public string Reason {
		get {
			// for now, this shouldn't be too horrible.
			string r = "";
			foreach (string reason in _reasons) {
				r += reason + ",";
			}

			return r;
		}
	}

	public void AddReason (
		string reason
	) {
		_reasons.Add (reason);
	}

	private readonly List<string> _reasons = [];
}

