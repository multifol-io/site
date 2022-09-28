using System.ComponentModel.DataAnnotations;
public class EmployerPlan {
    public EmployerPlan(Person person) {
        this.person = person;
    }

    private Person person;

    public bool Eligible { get; set; }
    public bool NotEligible {get {return !Eligible;}}

    public int? AnnualSalary { get; set; }
    public int? MatchA { get; set; }
    public int? MatchALimit { get; set; }
    public int? MatchB { get; set; }
    public int? MatchBLimit { get; set; }
    public int? MaxMatch { get; set; }
    public int? AmountToSaveForMatch { 
        get
        {
            double? contribution = null;
            if (MatchALimit != null) {
                double percentA = (MatchALimit ?? 100) / 100.0;
                if (percentA > 0) contribution = percentA * AnnualSalary;
                if (MatchB != null) {
                    double percentB = (MatchBLimit ?? 100) / 100.0;
                    if (percentB > 0) contribution = percentB * AnnualSalary;
                }
            }

            return (int?)contribution;
        }
    }
    public int? MatchAmount { 
        get
        {
            double? match = null;
            if (MatchA != null) {
                var percentA = (MatchA ?? 0) / 100.0;
                var matchALimit = (MatchALimit ?? 100.0) / 100.0;
                match = percentA * AnnualSalary * matchALimit;
            }
            if (MatchB != null) {
                var percentB = (MatchB ?? 0) / 100.0;
                var matchALimit = (MatchALimit ?? 100.0) / 100.0;
                var matchBLimit = (MatchBLimit ?? 100.0) / 100.0;
                match += percentB * AnnualSalary * (matchBLimit - matchALimit);
            }

            return (int?)match;
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