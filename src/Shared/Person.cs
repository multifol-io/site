using System.ComponentModel.DataAnnotations;
public class Person{
    public string? TargetMonths {get;set;}
    [Required]
    public int MonthlyExpenses { get; set;} = 6000;
    public int EFOther { get; set;}
    public int TargetEmergencyFund {
        get 
        {
            switch (TargetMonths) {
                case "0":
                    return 0;
                case "1":
                    return MonthlyExpenses;
                case "3":
                    return ThreeMonths;
                case "Other":
                    return EFOther;
                default:
                    return 0;
            }

        }
    }
    public int SavingsOpportunity {
        get {
            return TargetEmergencyFund - EmergencyFund;
        }
        set {
            
        }
    }
    [Required]
    public int EmergencyFund { get; set; }

    public int ThreeMonths
    {
        get { return MonthlyExpenses * 3; }
    }
    
}