using IRS;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;


public class FamilyYears : IFamilyYears
{
    public async static Task<FamilyYears?> Create(HttpClient httpClient) 
    {
        FamilyYears? familyYears = null;

        var retirementYear1 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.retirement.2022.json");
        var retirementYear2 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.retirement.2023.json");
        var taxRatesYear1 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.tax-rates.2022.json");
        var taxRatesYear2 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.tax-rates.2023.json");
        RetirementData? retirementDataY1 = await JsonSerializer.DeserializeAsync<IRS.RetirementData>(retirementYear1);
        RetirementData? retirementDataY2 = await JsonSerializer.DeserializeAsync<IRS.RetirementData>(retirementYear2);
        TaxRateData? taxRatesY1 = await JsonSerializer.DeserializeAsync<IRS.TaxRateData>(taxRatesYear1);
        TaxRateData? taxRatesY2 = await JsonSerializer.DeserializeAsync<IRS.TaxRateData>(taxRatesYear2);
        if (retirementDataY1 != null && retirementDataY2 != null && taxRatesY1 != null && taxRatesY2 != null) {
            familyYears = new FamilyYears(retirementDataY1, retirementDataY2, taxRatesY1, taxRatesY2);
            familyYears.Year = 2023;
        }

        return familyYears;
    }

    public FamilyYears(IRS.RetirementData retirementDataY1, IRS.RetirementData retirementDataY2, IRS.TaxRateData taxRatesY1, TaxRateData taxRatesY2)
    {
        RetirementDataY1 = retirementDataY1;
        RetirementDataY2 = retirementDataY2;
        TaxRateDataY1 = taxRatesY1;
        TaxRateDataY2 = taxRatesY2;
    }

    public int YearIndex 
    {
        get {
            return Year - 2022;
        }
    }

    public RetirementData RetirementData
    {
        get {
            if (YearIndex == 1) {
                return RetirementDataY2;
            } else if (YearIndex == 0) {
                return RetirementDataY1;
            } else {
                throw new InvalidDataException("year is not supported");
            }
        }
    }
    public TaxRateData TaxRateData
    {
        get {
            if (YearIndex == 1) {
                return TaxRateDataY2;
            } else if (YearIndex == 0) {
                return TaxRateDataY1;
            } else {
                throw new InvalidDataException("year is not supported");
            }
        }
    }

    [JsonIgnore]
    public RetirementData RetirementDataY1 { get; private set; }
    [JsonIgnore]
    public RetirementData RetirementDataY2 { get; private set; }
    [JsonIgnore]
    public TaxRateData TaxRateDataY1 { get; private set; }
    [JsonIgnore]
    public TaxRateData TaxRateDataY2 { get; private set; }

    
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
    public string? StateOfResidence { get; set; }
    public int Year { get; set; }
    public string? AdditionalBackground { get; set; }
    private List<string>? _Questions;
    public List<string> Questions
    {
        get {
            if (_Questions == null)
            {
                _Questions = new List<string>();
            }

            return _Questions;
        }
    }

    private List<Person>? _people;
    public List<Person> People {
        get {
            if (_people == null) {
                _people = new List<Person>();
                _people.Add(new Person(this, 0));
                _people.Add(new Person(this, 1));
            }

            return _people;
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
    public string? StateMarginalTaxBracket { get; set; }
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

    public double? Stocks { get; set; }
    public double? Bonds { get; set; }
    public int? International { get; set; }

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

    private List<Account>? _Accounts;
    public List<Account> Accounts {
        get {
            if (_Accounts == null)
            {
                _Accounts = new List<Account>();
            }

            return _Accounts;
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
}