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
                newValue += investment.Value;
            }

            return newValue;
        }
    }
    public bool Edit { get; set; }
    public double Percentage { get; set; }

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
    public void UpdatePercentages(double totalValue)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = investment.Value / totalValue * 100;
        }
    }
}