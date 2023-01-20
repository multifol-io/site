using IRS;

public interface IFamilyYears
{
    public EmergencyFund EmergencyFund { get; set; }
    public List<Debt> Debts { get; }
    public int? HighDebts { get; }
    public int? MediumDebts { get; }
    public int? LowDebts { get; }
    public int? UnknownDebts { get; }
    public TaxFilingStatus TaxFilingStatus { get; set; }
    public int Year { get; set; }
    public string? FederalMarginalTaxBracket { get; set; }
    public string StateOfResidence { get; set; }
    public string? StateMarginalTaxBracket { get; set; }
    public int PersonCount { get; }
    public List<Person> People { get; }
    public double? Stocks { get; set; }
    public double? Bonds { get; set; }
    public int? International { get; set; }
    public List<Account> Accounts { get; }
    public double Value { get; }
    public int ValueStyle { get; set; }
    public string? AdditionalBackground { get; set; }
    public List<string> Questions {get; }

    
    public int? AdjustedGrossIncome { get; set; }
    public int? IncomeTaxPaid { get; set; }
    public int? TaxableToInvest { get; set; }
    public int? PlannedSavings { get; }

    public void UpdatePercentages();
    public RetirementData RetirementData { get; }
    public TaxRateData TaxRateData { get; }
}