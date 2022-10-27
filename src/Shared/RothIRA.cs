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
            return FinancialFigures.USA_IRS_IRA_ContributionLimit 
                + (person.FiftyOrOver ? FinancialFigures.USA_IRS_IRA_CatchUpContributionLimit : 0);
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
                return ApplyRange(FinancialFigures.USA_IRS_RothIRA_ContributionPhaseOutRange_Single_Start,
                                  FinancialFigures.USA_IRS_RothIRA_ContributionPhaseOutRange_Single_End,
                                  person.TaxFiling.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingJointly:
                return ApplyRange(FinancialFigures.USA_IRS_RothIRA_ContributionPhaseOutRange_MarriedFiledJointly_Start,
                                  FinancialFigures.USA_IRS_RothIRA_ContributionPhaseOutRange_MarriedFiledJointly_End,
                                  person.TaxFiling.AdjustedGrossIncome, MaximumContributionByAge);
            case TaxFilingStatus.MarriedFilingSeperately: 
                return ApplyRange(FinancialFigures.USA_IRS_RothIRA_ContributionPhaseOutRange_MarriedFiledSeperately_Start,
                                  FinancialFigures.USA_IRS_RothIRA_ContributionPhaseOutRange_MarriedFiledSeperately_End,
                                  person.TaxFiling.AdjustedGrossIncome, MaximumContributionByAge);
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