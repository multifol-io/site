using System.Text.Json.Serialization;

public class IRA {
    // back pointer
    [JsonIgnore]
    public Person person { get; set; }

    private IRS.IRA? iraVariables {
        get {
            return person.FamilyData.AppData.IRSData.RetirementData.IRA;
        }
        set {
            person.FamilyData.AppData.IRSData.RetirementData.IRA = value;
        }
    }

    public bool HasExistingBalance { get; set; }

    public int? ContributionAllowed { 
        get
        {
            if (person.FamilyData.PersonCount > person.PersonIndex && person.FamilyData.AdjustedGrossIncome != null)
            {
                return iraVariables?.ContributionLimit
                    + (person.FiftyOrOver ? iraVariables?.CatchUpContributionLimit : 0);
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
                if (person.EmployerPlan.Eligible == TriState.True) {
                    return ApplyRange(iraVariables.DeductabilityPhaseOutRange.Single.Start,
                                      iraVariables.DeductabilityPhaseOutRange.Single.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                } else {
                    return ContributionAllowed;
                }
            case TaxFilingStatus.MarriedFilingJointly:
                if (person.Spouse != null) {
                    if (person.Spouse.EmployerPlan.Eligible == TriState.True) {
                        if (person.EmployerPlan.Eligible == TriState.True) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFilingJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFilingJointly.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                        }
                    } else {
                        if (person.EmployerPlan.Eligible == TriState.True) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.Start,
                                      iraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
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
                    if (person.Spouse.EmployerPlan.Eligible == TriState.True) {
                        if (person.EmployerPlan.Eligible == TriState.True) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);                   
                        } else {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);           
                        }
                    } else {
                        if (person.EmployerPlan.Eligible == TriState.True) {
                            return ApplyRange(iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                      iraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ContributionAllowed;
                        }
                    }
                } else {
                    return null;
                }
            default:
            case TaxFilingStatus.ChoiceNeeded:
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
            return CalculateDeduction(person.FamilyData.AdjustedGrossIncome, person.FamilyData.TaxFilingStatus, person);
        }
    }
    public int? AmountToSave {
        get {
            return ContributionAllowed;
        }
    }
}