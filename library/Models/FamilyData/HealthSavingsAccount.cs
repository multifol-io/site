using System.Text.Json.Serialization;

public class HealthSavingsAccount {
    // back pointer
    [JsonIgnore]
    public Person person { get; set; }
    
    public IRS.HSA? HSAVariables {
        get {
            if (person?.FamilyData?.AppData.IRSData != null) {
                return person.FamilyData.AppData.IRSData.RetirementData.HSA;
            }

            return null;
        }
    }

    public TriState EmployerOffersHSA { get; set; }
    public bool? EmployerDoesNotOfferHSA {
        get {
            switch (EmployerOffersHSA) {
                case TriState.ChoiceNeeded:
                case TriState.False:
                    if (person.EmployerPlan.AnnualSalary != 0)
                        return true;
                    else
                        return false;
                case TriState.True:
                default:
                    return false;
            }
        }
    }

    public bool EligibleForHSA {
        get {
            return (
                (person?.EmployerBenefits != null && person.EmployerBenefits.HSA.HighDeductibleHealthPlanAvailable == TriState.True)
                 || (person?.HealthSavingsAccount != null && person.HealthSavingsAccount.HasExternalHDHP)
                );
        }
    }

    public bool EligibleForHSACatchUpOnly {
        get {
            return (!EligibleForHSA && person != null && person.FiftyFiveOrOver && person?.OtherPerson != null && person.OtherPerson.HealthSavingsAccount.EligibleForHSA);
        }
    }

    public EmployeeFamily? Family { get; set; }
    private string? _EmployerContributionString; 
    public string? EmployerContributionString { 
        get {
            return _EmployerContributionString;
        }
        set {
            _EmployerContributionString = value;
            if (_EmployerContributionString != null) {
                EmployerContribution = int.Parse(_EmployerContributionString);
            }
        }
     }
    public int? EmployerContribution { get; set; }

    public bool HasEmployerHDHP { get; set; }
    public bool HasExternalHDHP { get; set; }
    public int? AmountToSave { get { return Limit - (EmployerContribution ?? 0); } }
    
    public int? Limit { 
        get {
            int? contributionLimit = null;

            if (EligibleForHSA || EligibleForHSACatchUpOnly) { 
                switch (Family)
                {
                    case EmployeeFamily.Family:
                        contributionLimit = HSAVariables?.ContributionLimit?.Family + (person.FiftyFiveOrOver ? HSAVariables?.ContributionLimit?.CatchUp : 0);
                        break;
                    case EmployeeFamily.Individual:
                        contributionLimit = HSAVariables?.ContributionLimit?.SelfOnly + (person.FiftyFiveOrOver ? HSAVariables?.ContributionLimit?.CatchUp : 0);
                        break;
                    case EmployeeFamily.CatchUp:
                        contributionLimit = HSAVariables?.ContributionLimit?.CatchUp;
                        break;
                    default:
                    case EmployeeFamily.ChoiceNeeded:
                        break;
                }
            }

            return contributionLimit;
        }
    }
}