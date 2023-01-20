public class Account 
{
    public string Identifier { get; set; }
    public string AccountType { get; set; }
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
    private List<Investment>? _Investments;
    public List<Investment> Investments {
        get {
            if (_Investments == null)
            {
                _Investments = new List<Investment>();
            }

            return _Investments;
        }
    }

    private List<Investment>? _AvailableFunds;
    public List<Investment> AvailableFunds {
        get {
            if (_AvailableFunds == null)
            {
                _AvailableFunds = new List<Investment>();
            }

            return _AvailableFunds;
        }
    }

    public void UpdatePercentages(double totalValue)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = investment.Value ?? 0 / totalValue * 100;
        }
    }
}