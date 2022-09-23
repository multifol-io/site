using System.ComponentModel.DataAnnotations;
public class RetirementFunds {
    public bool Eligible { get; set; }
    public bool Disabled {get {return !Eligible;}}

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

    public int SavingsOpportunity {
        get {
            return MatchLimit + ContributionRequired;
        }
    }
}