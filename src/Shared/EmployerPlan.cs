using System.ComponentModel.DataAnnotations;
public class EmployerPlan {
    public EmployerPlan(Person person) {
        this.person = person;
    }

    private Person person;

    public bool Eligible { get; set; }
    public bool NotEligible {get {return !Eligible;}}

    public int PercentMatched { get; set; }
    public int MatchLimit { get; set; }
    public int ContributionRequired { 
        get
        {
            double contribution = 0;
            if (PercentMatched != 0) {
                contribution = MatchLimit / (PercentMatched / 100.0);
            }

            return (int)contribution;
        }
    }

    public int? NonMatchedContributionAmount {
        get { return ContributionAllowed - ContributionRequired; }
    }
    
    public int? ContributionAllowed { 
        get
        {
            if (Eligible)
            {
                return 20500 + (person.FiftyOrOver ? 6000 : 0);
            }
            else
            {
                return null;
            }
        }
    }

    public int? SavingsOpportunity {
        get {
            if (!Eligible) return null;

            return MatchLimit + ContributionRequired;
        }
    }
}