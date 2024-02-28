using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Models;

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

    public int TargetMonths {get;set;} = 3;

    [DefaultValue(false)]
    public bool ShowDollars { get; set; }
    public bool Complete { get { return AmountToSave != null; } }

    public string? FreeformAnswer { get; set; } = null;
    public string? AnswerType { get; set; }

    public string? Answer {
        get {
            switch (AnswerType) {
                case "months":
                    if (CurrentMonths != null) {
                        return $"{CurrentMonths} months"; 
                    } else {
                        return $"missing data";
                    }
                case "dollars":
                    return FormatUtilities.FormatMoney(CurrentBalance);
                case "freeform":
                    return FreeformAnswer;
                default:
                    return null;
            }
        }
    }
}