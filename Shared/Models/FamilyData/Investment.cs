public class Investment 
{
    private IList<Fund>? funds;
    public Investment(IList<Fund> funds) 
    {
        this.funds = funds;
    }

    private string? _Name;
    public string? Name { 
        get {
            return _Name;
        }
        set {
            _Name = value;
        }
    }

    public bool AutoCompleted { get; set; }
    private string? _Ticker;
    public string? Ticker {
        get {
            return _Ticker;
        }
        set {
            if (value == null) { _Ticker = value; return; }
            _Ticker = value?.ToUpperInvariant();
            bool found = false;
            foreach (var fund in funds)
            {
                if (_Ticker == fund.Ticker)
                {
                    AutoComplete(fund.LongName, fund.ExpenseRatio);
                    found = true;
                }
            }
            if (!found) {
                if (AutoCompleted) {
                    ExpenseRatio = null;
                    Name = null;
                    AutoCompleted = false;
                }
            }
        }
    }

    private void AutoComplete(string? name, double? expenseRatio) {
        Name = name;
        ExpenseRatio = expenseRatio;
        AutoCompleted = true;
    }

    private double? _ExpenseRatio;
    public double? ExpenseRatio {
        get {
            return _ExpenseRatio;
        }
        set {
            _ExpenseRatio = value;
        }
     }
    public double? Shares { get; set; }
    public double? Price { get; set; }
    public double? Value { get; set; }
    public double Percentage { get; set; }
}