using System.ComponentModel.DataAnnotations;
public class EmployerPlan {
    public EmployerPlan(Person person) {
        this.person = person;
    }

    private Person person;

    public bool Eligible { get; set; }
    public bool NotEligible {get {return !Eligible;}}

    public int? PercentMatched { get; set; }
    public int? MatchLimit { get; set; }
    public int? AmountToSaveForMatch { 
        get
        {
            double? contribution = null;
            if (PercentMatched != null && MatchLimit != null && PercentMatched!= 0) {
                var percent = (PercentMatched ?? 0) / 100.0;
                var matchLimit = MatchLimit ?? 0;
                contribution = matchLimit / percent;
            }

            return (int?)contribution;
        }
    }
    public bool CompleteMatched { get { return AmountToSaveForMatch != null; } }
    public bool CompleteUnmatched { get { return AmountToSaveForNonMatched != null; } }

    public int? AmountToSaveForNonMatched {
        get { return ContributionAllowed - (AmountToSaveForMatch ?? 0); }
    }

    public int? AmountToSaveForBackdoorRoth { get; set; }

    public int? ContributionAllowed { 
        get
        {
            if (Eligible)
            {
                return 20500 + (person.FiftyOrOver ? 6500 : 0);
            }
            else
            {
                return null;
            }
        }
    }
}