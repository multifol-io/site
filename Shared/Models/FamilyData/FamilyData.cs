using IRS;
using System.ComponentModel.DataAnnotations;


public class FamilyData : IFamilyData
{
    private FamilyData() {
        Accounts = new();
        People = new();
        Questions = new();
        People.Add(new Person() { Identifier = "Person 1", FamilyData = this, personIndex = 0 });
        People.Add(new Person() { Identifier = "Person 2", FamilyData = this, personIndex = 1 });
    }
    public FamilyData(IRSData irsData) : this()
    {
        IRSData = irsData;
        Year = 2023;
    }
    public EmergencyFund EmergencyFund { get; set; } = new();

    public bool DebtsComplete {
        get {
            return ((Debts.Count == 0 && DebtFree == TriState.True) || Debts.Count > 0);
        }
    }
    
    public TriState DebtFree { get; set; }
    private List<Debt>? _debts;
    public List<Debt> Debts
    {
        get {
            if (_debts == null) {
                _debts = new List<Debt>();
            }

            return _debts;
        }
    }
    
    private TaxFilingStatus _taxFilingStatus = TaxFilingStatus.Single;
    [Required]
    public TaxFilingStatus TaxFilingStatus { 
        get {
            return _taxFilingStatus;
        }
        set {
            _taxFilingStatus = value;
        }
    }

    public int? AdjustedGrossIncome { get; set; }
    public int? IncomeTaxPaid { get; set; }
    public int? TaxableToInvest { get; set; }
    public int? PlannedSavings { 
        get {
            int? annualExpenses = EmergencyFund.MonthlyExpenses * 12;
            int investFromTaxable = TaxableToInvest ?? 0;
            return AdjustedGrossIncome - (IncomeTaxPaid ?? 0) - annualExpenses + investFromTaxable;
        }
    }
    public string? FederalMarginalTaxBracket { get; set; }
    private int _Year;
    public int Year { 
        get {
            return _Year;
        }
        set {
            _Year = value;
            if (IRSData != null) {
                IRSData.Year = value;
            }   
        }
    }
    public string? StateMarginalTaxBracket { get; set; }
    public string? StateOfResidence { get; set; }

    public double? Stocks { get; set; }
    public double? Bonds { get; set; }
    public int? International { get; set; }

    public List<Person> People { get; set; }

    public double Value { 
        get {
            double newValue = 0;
            foreach (var account in Accounts) 
            {
                newValue += account.Value;
            }
            
            return newValue;
        }
    }

    public int ValueStyle { get; set; } = 0;

    public List<Account> Accounts { get; set; }

    public string? AdditionalBackground { get; set; }
    public List<string> Questions { get; set; }

    private int? GetDebts(string? category) {
            int? total = null;
            foreach (var debt in Debts) {
                if (debt.Category == category) {
                    if (total == null) {
                        total = debt.Total;
                    } else {
                        total += debt.Total;
                    }
                }
            }

            return total;
    }

    public int? HighDebts {
        get {
            return GetDebts("High");
        }
    }

    public int? MediumDebts {
        get {
            return GetDebts("Medium");
        }
    }

    public int? LowDebts {
        get {
            return GetDebts("Low");
        }
    }

    public int? UnknownDebts {
        get {
            return GetDebts(null);
        }
    }

    public void UpdatePercentages()
    {
        double totalValue = this.Value;
        foreach (var account in Accounts)
        {
            account.Percentage = account.Value / totalValue * 100;
            account.UpdatePercentages(totalValue);
        }
    }

    public int YearIndex 
    {
        get {
            return Year - 2022;
        }
    }

    public IRSData? IRSData { get; set; }

    public int PersonCount {
        get {
            switch (TaxFilingStatus) {
                case TaxFilingStatus.Single:
                case TaxFilingStatus.HeadOfHousehold:
                case TaxFilingStatus.MarriedFilingSeperately:
                case TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart:
                    return 1;
                case TaxFilingStatus.MarriedFilingJointly:
                    return 2;
                case TaxFilingStatus.ChoiceNeeded:
                default:
                    return 0;
            }
        } 
    }
}