using System.ComponentModel.DataAnnotations;

public class EmergencyFund {
    public int TargetMonths {get;set;} = 3;
    [Required]
    public int? MonthlyExpenses { get; set;}
    public int EFOther { get; set;} = 0;
    public int? TargetEmergencyFund {
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
    
    public int? AmountToSave {
        get { return TargetEmergencyFund - CurrentBalance; }
    }

    public string CurrentMonthsString {
        get {
            return String.Format("{0:#,0.#}", CurrentMonths);
        }
    }

    public double? CurrentMonths {
        get { return (double?)CurrentBalance / (double?)MonthlyExpenses; }
    }

    public bool UseAmount { get; set; }
    public bool Complete { get { return AmountToSave != null; } }

    [Required]
    public int? CurrentBalance { get; set; }

    public int? ThreeMonths
    {
        get { return MonthlyExpenses * 3; }
    }
}