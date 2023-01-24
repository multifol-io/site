using System.ComponentModel.DataAnnotations;
public class RothIRA {
    public RothIRA(Person person)
    {
        this.person = person;
    }

    private Person person;
    private IRS.RothIRA rothIraVariables {
        get { return this.person.FamilyData.IRSData.RetirementData.RothIRA; }
    }

    public int MaximumContributionByAge { 
        get
        {
            return rothIraVariables.ContributionLimit
                + (person.FiftyOrOver ? rothIraVariables.CatchUpContributionLimit : 0);
        }
    }

    public int? ContributionAllowed {
        get { 
            if (person.FamilyData.PersonCount > person.PersonIndex && person.FamilyData.AdjustedGrossIncome != null) {
                return CalculateAllowedContribution(person.FamilyData.AdjustedGrossIncome, person.FamilyData.TaxFilingStatus, person);
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
                return ApplyRange(rothIraVariables.ContributionPhaseOutRange.Single.Start,
                                  rothIraVariables.ContributionPhaseOutRange.Single.End,
                                  person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingJointly:
                return ApplyRange(rothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly.Start,
                                  rothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly.End,
                                  person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingSeperately: 
                return ApplyRange(rothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately.Start,
                                  rothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately.End,
                                  person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.ChoiceNeeded:
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

    public int? AmountToSave {
        get {
            return ContributionAllowed;
        }
    }
}