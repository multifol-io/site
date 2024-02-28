using System.Text.Json.Serialization;

namespace Models;

public class IRA
{
    // back pointer
    [JsonIgnore]
    public Person? Person { get; set; }

    private IRS.IRA? IraVariables
    {
        get
        {
            return Person!.FamilyData.AppData.IRSData!.RetirementData.IRA;
        }
        set
        {
            Person!.FamilyData.AppData.IRSData!.RetirementData.IRA = value;
        }
    }

    public bool HasExistingBalance { get; set; }

    public int? ContributionAllowed
    {
        get
        {
            if (Person!.FamilyData.PersonCount > Person.PersonIndex && Person.FamilyData.AdjustedGrossIncome != null)
            {
                return IraVariables?.ContributionLimit
                    + (Person.FiftyOrOver ? IraVariables?.CatchUpContributionLimit : 0);
            }
            else
            {
                return null;
            }
        }
    }

    public int? CalculateDeduction(TaxFilingStatus taxFilingStatus, Person person)
    {
        switch (taxFilingStatus)
        {
            case TaxFilingStatus.Single:
                if (IraVariables?.DeductabilityPhaseOutRange?.Single == null) return null;
                if (person.EmployerPlan.Eligible == TriState.True)
                {
                    return ApplyRange(IraVariables.DeductabilityPhaseOutRange.Single.Start,
                                      IraVariables.DeductabilityPhaseOutRange.Single.End,
                                      person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                }
                else
                {
                    return ContributionAllowed;
                }
            case TaxFilingStatus.MarriedFilingJointly:
                if (person.Spouse == null
                    || IraVariables?.DeductabilityPhaseOutRange?.ActiveParticipant_MarriedFilingJointly == null
                    || IraVariables?.DeductabilityPhaseOutRange?.InactiveParticipant_MarriedFilingJointly == null)
                    return null;
                if (person.Spouse.EmployerPlan.Eligible == TriState.True)
                {
                    if (person.EmployerPlan.Eligible == TriState.True)
                    {
                        return ApplyRange(IraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.Start,
                                  IraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.End,
                                  person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                    }
                    else
                    {
                        return ApplyRange(IraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFilingJointly.Start,
                                  IraVariables.DeductabilityPhaseOutRange.InactiveParticipant_MarriedFilingJointly.End,
                                  person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                    }
                }
                else
                {
                    if (person.EmployerPlan.Eligible == TriState.True)
                    {
                        return ApplyRange(IraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.Start,
                                  IraVariables.DeductabilityPhaseOutRange.ActiveParticipant_MarriedFilingJointly.End,
                                  person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                    }
                    else
                    {
                        return ContributionAllowed;
                    }
                }
            case TaxFilingStatus.MarriedFilingSeperately:
                if (person.Spouse == null
                    || IraVariables?.DeductabilityPhaseOutRange?.MarriedFilingSeparately == null)
                    return null;
                if (person.Spouse.EmployerPlan.Eligible == TriState.True)
                {
                    if (person.EmployerPlan.Eligible == TriState.True)
                    {
                        return ApplyRange(IraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                  IraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                  person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                    }
                    else
                    {
                        return ApplyRange(IraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                  IraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                  person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                    }
                }
                else
                {
                    if (person.EmployerPlan.Eligible == TriState.True)
                    {
                        return ApplyRange(IraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.Start,
                                  IraVariables.DeductabilityPhaseOutRange.MarriedFilingSeparately.End,
                                  person.FamilyData.AdjustedGrossIncome, ContributionAllowed);
                    }
                    else
                    {
                        return ContributionAllowed;
                    }
                }
            default:
            case TaxFilingStatus.ChoiceNeeded:
                return null;
        }
    }

    private static int? ApplyRange(int low, int high, int? income, int? contributionAllowed)
    {
        if (income <= low) return contributionAllowed;
        if (income >= high) return 0;
        return contributionAllowed * (high - income) / (high - low);
    }

    public int? DeductionAllowed
    {
        get
        {
            return CalculateDeduction(Person!.FamilyData.TaxFilingStatus, Person);
        }
    }
    public int? AmountToSave
    {
        get
        {
            return ContributionAllowed;
        }
    }
}