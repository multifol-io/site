using System.ComponentModel.DataAnnotations;
public class IRA {
    public IRA(Person person)
    {
        this.person = person;
        if (this.person.TaxFiling?.IRSRetirement?.IRA != null) {
            this.iraVariables = this.person.TaxFiling.IRSRetirement.IRA;
        }
    }

    private Person person;
    private IRS.IRA? iraVariables;

    public bool HasExistingBalance { get; set; }

    public int? ContributionAllowed { 
        get
        {
            if (person.TaxFiling.PersonCount > person.PersonIndex && person.TaxFiling.AdjustedGrossIncome != null)
            {
                return iraVariables.ContributionLimit
                    + (person.FiftyOrOver ? iraVariables.CatchUpContributionLimit : 0);
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
                    return ApplyRange(iraVariables.DeductabilityPhaseOutRange.Single.Start,
                                      iraVariables.DeductabilityPhaseOutRange.Single.End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                } else {
                    return ContributionAllowed;
                }
            case TaxFilingStatus.MarriedFilingJointly:
                if (person.Spouse != null) {
                    if (person.Spouse.EmployerPlan.Eligible) {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFilingJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFilingJointly.End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.End,
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
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);                   
                        } else {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                      person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);           
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
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