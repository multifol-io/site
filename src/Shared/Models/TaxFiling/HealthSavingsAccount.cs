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
            if (!Eligible) return null;
            
            switch (Family)
            {
                
                case EmployeeFamily.Family:
                    return HSAVariables.ContributionLimit.Family;
                case EmployeeFamily.EmployeeOnly:
                    return HSAVariables.ContributionLimit.SelfOnly;
                default:
                    return null;
            }
        }
    }

    public int? EmployerContribution { get; set; }

    public int? AmountToSave { get { return Limit - (EmployerContribution ?? 0); }
    }
}