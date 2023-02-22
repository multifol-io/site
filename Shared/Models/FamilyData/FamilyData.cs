using IRS;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class FamilyData : IFamilyData
{
    private FamilyData() {
        Debts = new();
        Accounts = new();
        People = new();
        Questions = new();
    }

    public FamilyData(IRSData irsData) : this()
    {
        IRSData = irsData;
        Year = 2023;
        People.Add(new Person() { Identifier = "person 1", FamilyData = this, PersonIndex = 0 });
        People.Add(new Person() { Identifier = "person 2", FamilyData = this, PersonIndex = 1 });
        SetBackPointers();
    }

    // set back pointers in familydata
    public void SetBackPointers()
    {
        for (int personIndex = 0; personIndex < this.People.Count; personIndex++) {
            var person = this.People[personIndex];
            person.PersonIndex = personIndex;
            person.SetBackPointers(this);
        }
    }

    public EmergencyFund EmergencyFund { get; set; } = new();

    public bool DebtsComplete {
        get {
            return ((Debts.Count == 0 && DebtFree == TriState.True) || Debts.Count > 0);
        }
    }
    
    public TriState DebtFree { get; set; }
    public List<Debt> Debts { get; set; }
    
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

    public double? StockBalance { get; set; }
    public double? InternationalStockBalance { get; set; }
    public double? BondBalance { get; set; }
    public double? CashBalance { get; set; }
    public double? OtherBalance { get; set; }

    public double ActualStockAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);
            if (overallTotal > 0.0)
            {
                return ( (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) ) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double ActualInternationalStockAllocation {
        get {
            var stockTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0);
            if (stockTotal > 0.0)
            {
                return (InternationalStockBalance ?? 0.0) / stockTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }
    
    public double ActualBondAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);

            if (overallTotal > 0.0)
            {
                return (BondBalance ?? 0.0) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double ActualCashAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);

            if (overallTotal > 0.0)
            {
                return (CashBalance ?? 0.0) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double ActualOtherAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);

            if (overallTotal > 0.0)
            {
                return (OtherBalance ?? 0.0) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }
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
        StockBalance = 0.0;
        InternationalStockBalance = 0.0;
        BondBalance = 0.0;
        CashBalance = 0.0;
        OtherBalance = 0.0;

        double totalValue = this.Value;
        foreach (var account in Accounts)
        {
            account.Percentage = account.Value / totalValue * 100;
            account.UpdatePercentages(totalValue, this);
        }
    }

    public int YearIndex 
    {
        get {
            return Year - 2022;
        }
    }

    [JsonIgnore]
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