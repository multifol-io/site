public class Investment 
{
    private IList<Fund> funds;
    public Investment(IList<Fund> funds) 
    {
        this.funds = funds;
    }
    public string Name { get; set; }
    private string _Ticker;
    public string Ticker {
        get {
            return _Ticker;
        }
        set {
            _Ticker = value.ToUpperInvariant();
            foreach (var fund in funds)
            {
                if (_Ticker == fund.Ticker)
                {
                    Name = fund.LongName;
                    ExpenseRatio = fund.ExpenseRatio;
                }
            }
        }
    }
    public double? ExpenseRatio { get; set; }
    public double? Shares { get; set; }
    public double? Price { get; set; }
    public double Value { get; set; }
    public double Percentage { get; set; }
}