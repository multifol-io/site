using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Models;

public class EmergencyFund : INotifyPropertyChanged
{
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonIgnore] internal FamilyData? FamilyData { get; private set; }

    public void SetBackPointer(FamilyData familyData) { FamilyData = familyData; }

    [Required]
    public int? CurrentBalance { get; set; }

    private int? monthlyExpenses;
    public int? MonthlyExpenses
    {
        get { return monthlyExpenses; }
        set
        {
            monthlyExpenses = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AnnualExpenses));
            if (FamilyData != null)
            {
                FamilyData.UpdateValueInXUnits();
                FamilyData.UpdateChangeInXUnits();
            }
        }
    }

    public int? AnnualExpenses { get => MonthlyExpenses * 12; }

    public int? AmountToSave
    {
        get { return TargetMonths * MonthlyExpenses - CurrentBalance; }
    }

    public string CurrentMonthsString
    {
        get
        {
            return String.Format("{0:#,0.#}", CurrentMonths);
        }
    }

    public double? CurrentMonths
    {
        get { return (double?)CurrentBalance / (double?)MonthlyExpenses; }
    }

    public int TargetMonths { get; set; } = 3;

    [DefaultValue(false)]
    public bool ShowDollars { get; set; }
    public bool Complete { get { return AmountToSave != null; } }

    public string? FreeformAnswer { get; set; } = null;
    public string? AnswerType { get; set; }

    public string? Answer
    {
        get
        {
            switch (AnswerType)
            {
                case "months":
                    if (CurrentMonths != null)
                    {
                        return $"{CurrentMonths} months";
                    }
                    else
                    {
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