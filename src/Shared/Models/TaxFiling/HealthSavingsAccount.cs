using System.ComponentModel.DataAnnotations;
public class HealthSavingsAccount {
    public HealthSavingsAccount(Person person)
    {
        this.person = person;
    }

    public IRS.HSA HSAVariables {
        get {
            return this.person.TaxFiling.IRSRetirement.HSA;
        }
    }

    private Person person;
    public bool Eligible { get; set; }
    public bool NotEligible {get {return !Eligible;}}

    public EmployeeFamily? Family { get; set; }
    
    public int? Limit { 
        get {
            if (!Eligible && person.OtherPerson != null && !person.OtherPerson.HealthSavingsAccount.Eligible) return null;
            
            int contributionLimit = 0;

            switch (Family)
            {
                case EmployeeFamily.Family:
                    contributionLimit = HSAVariables.ContributionLimit.Family;
                    break;
                case EmployeeFamily.EmployeeOnly:
                    contributionLimit = HSAVariables.ContributionLimit.SelfOnly;
                    break;
                default:
                    break;
            }

            if (person.FiftyFiveOrOver) {
                contributionLimit += HSAVariables.ContributionLimit.CatchUp;
            }

            return contributionLimit;
        }
    }

    public int? EmployerContribution { get; set; }

    public int? AmountToSave { get { return Limit - (EmployerContribution ?? 0); }
    }
}