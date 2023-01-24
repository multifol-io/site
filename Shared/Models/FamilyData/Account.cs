public class Account 
{
    public Account() {
        Investments = new();
        AvailableFunds = new();
    }
    
    public string? Identifier { get; set; }
    public string AccountType { get; set; } = "Taxable";
    public string? Custodian { get; set; }
    public double Value { 
        get {
            double newValue = 0;
            foreach (var investment in Investments) 
            {
                newValue += investment.Value ?? 0;
            }

            return newValue;
        }
    }
    public bool Edit { get; set; }
    public double Percentage { get; set; }

    public string FullName 
    {
        get {
            return (Identifier != null ? Identifier+ " " : "") + AccountType + (Custodian != null ? " at " + Custodian : "");
        }
    }

    public List<Investment> Investments { get; set; }

    public List<Investment> AvailableFunds { get; set; }

    public void UpdatePercentages(double totalValue)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = (investment.Value ?? 0) / totalValue * 100;
        }
    }
}