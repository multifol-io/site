using System.Text.Json.Serialization;

public class Investment 
{
    [JsonIgnore]
    public IList<Fund>? funds { get; set; }

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
            _Ticker = value?.ToUpperInvariant().Trim();
            if (funds != null) {
                bool found = false;
                foreach (var fund in funds)
                {
                    if (_Ticker == fund.Ticker)
                    {
                        if (fund.AssetType == null)
                        {
                            fund.AssetType = global::AssetType.USStock;
                        }
                    
                        AutoComplete(fund);
                        found = true;
                        return;
                    }
                }
                if (!found) {
                    if (AutoCompleted) {
                        AutoComplete(null);
                    }
                }
            }
        }
    }

    private void AutoComplete(Fund fund) {
        if (fund == null) {
            Name = null;
            ExpenseRatio = null;
            AssetType = null;
            VanguardFundId = null;
            AutoCompleted = false;
        }
        else 
        {
            Name = fund.LongName;
            ExpenseRatio = fund.ExpenseRatio;
            AssetType = fund.AssetType;
            VanguardFundId = fund.VanguardFundId;
            AutoCompleted = true;
        }
    }
    public string? VanguardFundId { get; set;  }
    public AssetType? _AssetType;
    public AssetType? AssetType {
        get { return _AssetType; }
        set { 
            switch (value) {
                case global::AssetType.Stock:
                    _AssetType = global::AssetType.USStock;
                    break;
                default:
                    _AssetType = value;
                    break;
            }
        }
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

    private double? _Shares;
    public double? Shares {
        get {
            return _Shares;
        }
        set {
            _Shares = value;
            if (value != null) {
                Value = _Shares * Price;
            }
        }
    }
    public double? CostBasis { get; set; }
    private double? _Price;
    public double? Price {
        get {
            return _Price;
        }
        set {
            _Price = value;
            if (value != null) {
                Value = _Price * Shares;
            }
        }
    }
    public double? PreviousClose { get; set; }
    public double? PercentChange { get; set; }
    public DateTime? LastUpdated { get; set; }
    public double? Value { get; set; }
    [JsonIgnore]
    public double Percentage { get; set; }
}