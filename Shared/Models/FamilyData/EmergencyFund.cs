using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class EmergencyFund {
    [Required]
    public int? CurrentBalance { get; set; }

    [Required]
    public int? MonthlyExpenses { get; set;}
    
    public int? AmountToSave {
        get { return TargetMonths * MonthlyExpenses - CurrentBalance; }
    }

    public string CurrentMonthsString {
        get {
            return String.Format("{0:#,0.#}", CurrentMonths);
        }
    }

    public double? CurrentMonths {
        get { return (double?)CurrentBalance / (double?)MonthlyExpenses; }
    }

    [DefaultValue(3)]
    public int TargetMonths {get;set;} = 3;

    [DefaultValue(false)]
    public bool ShowDollars { get; set; }
    public bool Complete { get { return AmountToSave != null; } }
}