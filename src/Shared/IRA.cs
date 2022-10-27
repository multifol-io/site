using System.ComponentModel.DataAnnotations;
public class IRA {
    public IRA(Person person)
    {
        this.person = person;
    }

    private Person person;

    public int? ContributionAllowed { 
        get
        {
            if (person.TaxFiling.PersonCount > person.PersonIndex && person.TaxFiling.AdjustedGrossIncome != null)
            {
                return FinancialFigures.USA_IRS_IRA_ContributionLimit 
                    + (person.FiftyOrOver ? FinancialFigures.USA_IRS_IRA_CatchUpContributionLimit : 0);
            }
            else
            {
                return null;
            }
        }
    }

    public int? CalculateDeduction(int? income, TaxFilingStatus taxFilingStatus, Person person)
    {
        switch (taxFilingStatus) {
            case TaxFilingStatus.Single:
                if (person.EmployerPlan.Eligible) {
                    return ApplyRange(FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_Single_Start,
                                      FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_Single_End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                } else {
                    return ContributionAllowed;
                }
            case TaxFilingStatus.MarriedFilingJointly:
                if (person.Spouse != null) {
                    if (person.Spouse.EmployerPlan.Eligible) {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledJointlyActive_Start,
                                      FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledJointlyActive_End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ApplyRange(FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledJointlyInactive_Start,
                                      FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledJointlyInactive_End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledJointlyActive_Start,
                                      FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledJointlyActive_End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ContributionAllowed;
                        }
                    }
                } else {
                    return null;
                }
            case TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart:
            case TaxFilingStatus.MarriedFilingSeperately:
                if (person.Spouse != null) {
                    if (person.Spouse.EmployerPlan.Eligible) {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledSeperately_Start,
                                      FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledSeperately_End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);                    
                        } else {
                            return ApplyRange(FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledSeperately_Start,
                                      FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledSeperately_End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);           
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledSeperately_Start,
                                      FinancialFigures.USA_IRS_IRA_DeductabilityPhaseOutRange_MarriedFiledSeperately_End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ContributionAllowed;
                        }
                    }
                } else {
                    return null;
                }
            default:
            case TaxFilingStatus.None:
                return null;
        }
    }

    private int? ApplyRange(int low, int high, int? income, int? contributionAllowed) 
    {
        if (income <= low) return contributionAllowed;
        if (income >= high) return 0;
        return contributionAllowed * (high - income) / (high - low);
    }

    public int? DeductionAllowed { 
        get
        {
            return CalculateDeduction(person.TaxFiling.AdjustedGrossIncome, person.TaxFiling.TaxFilingStatus, person);
        }
    }
    public int? AmountToSave {
        get {
            return ContributionAllowed;
        }
    }
}