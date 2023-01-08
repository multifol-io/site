using System.ComponentModel.DataAnnotations;
public class RothIRA {
    public RothIRA(Person person)
    {
        this.person = person;
        this.rothIraVariables = this.person.FamilyYear.RetirementData.RothIRA;
    }

    private Person person;
    private IRS.RothIRA rothIraVariables;

    public int MaximumContributionByAge { 
        get
        {
            return rothIraVariables.ContributionLimit
                + (person.FiftyOrOver ? rothIraVariables.CatchUpContributionLimit : 0);
        }
    }

    public int? ContributionAllowed {
        get { 
            if (person.FamilyYear.PersonCount > person.PersonIndex && person.FamilyYear.AdjustedGrossIncome != null) {
                return CalculateAllowedContribution(person.FamilyYear.AdjustedGrossIncome, person.FamilyYear.TaxFilingStatus, person);
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
                                  person.FamilyYear.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingJointly:
                return ApplyRange(rothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly.Start,
                                  rothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly.End,
                                  person.FamilyYear.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingSeperately: 
                return ApplyRange(rothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately.Start,
                                  rothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately.End,
                                  person.FamilyYear.AdjustedGrossIncome, MaximumContributionByAge);
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

    public int? AmountToSave {
        get {
            return ContributionAllowed;
        }
    }
}