using System.ComponentModel.DataAnnotations;
public class IRA {
    public IRA(Person person)
    {
        this.person = person;
        if (this.person.FamilyYear?.IRSRetirement?.IRA != null) {
            this.iraVariables = this.person.FamilyYear.IRSRetirement.IRA;
        }
    }

    private Person person;
    private IRS.IRA? iraVariables;

    public bool HasExistingBalance { get; set; }

    public int? ContributionAllowed { 
        get
        {
            if (person.FamilyYear.PersonCount > person.PersonIndex && person.FamilyYear.AdjustedGrossIncome != null)
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
                                      person.FamilyYear.AdjustedGrossIncome, ContributionAllowed);
                } else {
                    return ContributionAllowed;
                }
            case TaxFilingStatus.MarriedFilingJointly:
                if (person.Spouse != null) {
                    if (person.Spouse.EmployerPlan.Eligible) {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFiledJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFiledJointly.End,
                                      person.FamilyYear.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFiledJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFiledJointly.End,
                                      person.FamilyYear.AdjustedGrossIncome, ContributionAllowed);
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFiledJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFiledJointly.End,
                                      person.FamilyYear.AdjustedGrossIncome, ContributionAllowed);
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
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFiledSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFiledSeparately.End,
                                      person.FamilyYear.AdjustedGrossIncome, ContributionAllowed);                   
                        } else {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFiledSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFiledSeparately.End,
                                      person.FamilyYear.AdjustedGrossIncome, ContributionAllowed);           
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFiledSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFiledSeparately.End,
                                      person.FamilyYear.AdjustedGrossIncome, ContributionAllowed);
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
            return CalculateDeduction(person.FamilyYear.AdjustedGrossIncome, person.FamilyYear.TaxFilingStatus, person);
        }
    }
    public int? AmountToSave {
        get {
            return ContributionAllowed;
        }
    }
}