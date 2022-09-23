using System.ComponentModel.DataAnnotations;
public class EmergencyFund {
    public int TargetMonths {get;set;} = 3;
    [Required]
    public int MonthlyExpenses { get; set;} = 6000;
    public int EFOther { get; set;} = 0;
    public int TargetEmergencyFund {
        get 
        {
            switch (TargetMonths) {
                case 1:
                    return MonthlyExpenses;
                case 3:
                    return ThreeMonths;
                case 4:
                    return EFOther;
                default:
                    return 0;
            }

        }
    }
    
    public int SavingsOpportunity {
        get { return TargetEmergencyFund - CurrentBalance; }
    }

    [Required]
    public int CurrentBalance { get; set; }

    public int ThreeMonths
    {
        get { return MonthlyExpenses * 3; }
    }
}