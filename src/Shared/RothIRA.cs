using System.ComponentModel.DataAnnotations;
public class RothIRA {
    public RothIRA(Person person)
    {
        this.person = person;
    }

    private Person person;

    public int MaximumContributionByAge { 
        get
        {
            return 6000 + (person.FiftyOrOver ? 1000 : 0);
        }
    }

    public int? ContributionAllowed {
        get { 
            if (person.TaxFiling.PersonCount > person.PersonIndex && person.TaxFiling.AdjustedGrossIncome != null) {
                return CalculateAllowedContribution(person.TaxFiling.AdjustedGrossIncome, person.TaxFiling.TaxFilingStatus, person);
            } else {
                return null;
            }
        }
    }

    public int? CalculateAllowedContribution(int? income, TaxFilingStatus taxFilingStatus, Person person)
    {
        switch (taxFilingStatus) {
            case TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart: 
            case TaxFilingStatus.Single:
                return ApplyRange(129000, 144000, person.TaxFiling.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingJointly:
                return ApplyRange(204000, 214000, person.TaxFiling.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingSeperately: 
                return ApplyRange(0,10000,person.TaxFiling.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.None:
            default:
                return null;
        }
    }

    private int? ApplyRange(int low, int high, int? income, int? contributionAllowed) 
    {
        if (income <= low) return contributionAllowed;
        if (income >= high) return 0;
        return contributionAllowed * (high - income) / (high - low);
    }

    public int? SavingsOpportunity {
        get {
            return ContributionAllowed;
        }
    }
}