using System.ComponentModel.DataAnnotations;
public class HealthSavingsAccount {
    public bool Eligible { get; set; }
    public bool NotEligible {get {return !Eligible;}}

    public EmployeeFamily? Family { get; set; }
    
    public int? Limit { 
        get {
            if (!Eligible) return null;
            
            switch (Family)
            {
                
                case EmployeeFamily.Family:
                    return 7300;
                case EmployeeFamily.EmployeeOnly:
                    return 3500;
                default:
                    return null;
            }
        }
    }

    public int? EmployerContribution { get; set; }

    public int? AmountToSave { get { return Limit - (EmployerContribution ?? 0); }
    }
}