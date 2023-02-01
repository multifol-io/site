using System.Text.Json.Serialization;

public class HealthSavingsAccount {
    public IRS.HSA? HSAVariables {
        get {
            if (person?.FamilyData?.IRSData != null) {
                return person.FamilyData.IRSData.RetirementData.HSA;
            }

            return null;
        }
    }

    [JsonIgnore]
    public Person person { get; set; }
    public TriState HasHSA { get; set; }
    public bool? DoesNotHaveHSA {
        get {
            switch (HasHSA) {
                case TriState.ChoiceNeeded:
                case TriState.False:
                    return true;
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
                 || person.HealthSavingsAccount.HasExternalHDHP
                );
        }
    }

    public bool EligibleForHSACatchUpOnly {
        get {
            return (!EligibleForHSA && person.FiftyFiveOrOver && person?.OtherPerson != null && person.OtherPerson.HealthSavingsAccount.EligibleForHSA);
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
            if (HasHSA == TriState.False && person.OtherPerson != null && person.OtherPerson.HealthSavingsAccount.DoesNotHaveHSA.HasValue && person.OtherPerson.HealthSavingsAccount.DoesNotHaveHSA.Value) return null;
            
            int? contributionLimit = null;

            switch (Family)
            {
                case EmployeeFamily.Family:
                    contributionLimit = HSAVariables?.ContributionLimit?.Family;
                    break;
                case EmployeeFamily.Individual:
                    contributionLimit = HSAVariables?.ContributionLimit?.SelfOnly;
                    break;
                case EmployeeFamily.CatchUp:
                    contributionLimit = 0;
                    break;
                default:
                    break;
            }

            if (person != null && person.FiftyFiveOrOver) {
                if (contributionLimit == null) {
                    contributionLimit = HSAVariables?.ContributionLimit?.CatchUp;
                } else {
                    contributionLimit += HSAVariables?.ContributionLimit?.CatchUp;
                }
            }

            return contributionLimit;
        }
    }
}