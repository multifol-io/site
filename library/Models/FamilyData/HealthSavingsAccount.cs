using System.Text.Json.Serialization;

namespace Models;

public class HealthSavingsAccount {
    // back pointer
    [JsonIgnore]
    public Person? Person { get; set; }
    
    public IRS.HSA? HSAVariables {
        get {
            if (Person!.FamilyData?.AppData.IRSData != null) {
                return Person.FamilyData.AppData.IRSData.RetirementData.HSA;
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
                    if (Person!.EmployerPlan.AnnualSalary != 0)
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
                (Person!.EmployerBenefits != null && Person.EmployerBenefits.HSA.HighDeductibleHealthPlanAvailable == TriState.True)
                 || (Person.HealthSavingsAccount != null && Person.HealthSavingsAccount.HasExternalHDHP)
                );
        }
    }

    public bool EligibleForHSACatchUpOnly {
        get {
            return (!EligibleForHSA && Person!.FiftyFiveOrOver && Person.OtherPerson != null && Person.OtherPerson.HealthSavingsAccount.EligibleForHSA);
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
                        contributionLimit = HSAVariables?.ContributionLimit?.Family + (Person!.FiftyFiveOrOver ? HSAVariables?.ContributionLimit?.CatchUp : 0);
                        break;
                    case EmployeeFamily.Individual:
                        contributionLimit = HSAVariables?.ContributionLimit?.SelfOnly + (Person!.FiftyFiveOrOver ? HSAVariables?.ContributionLimit?.CatchUp : 0);
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