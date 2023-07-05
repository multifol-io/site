using System.ComponentModel;
using System.Text.Json.Serialization;

public class Investment 
{
    public Investment() {

    }

    public Investment(int? pin) : this()
    {
        PIN = pin;
    }

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

    [JsonIgnore]
    public bool IsETF {
        get {
            var assetType = AssetType ?? global::AssetType.Unknown;
            switch (assetType) {
                case  global::AssetType.USStock_ETF:
                case  global::AssetType.InternationalStock_ETF:
                case  global::AssetType.Bond_ETF:
                    return true;
                default:
                    return false;
            }
        }
    }

    [JsonIgnore]
    public bool IsStockOrBond {
        get {
            switch (AssetType) {
                case global::AssetType.USStock:
                case global::AssetType.InternationalStock:
                case global::AssetType.Bond:
                    return true;
                default:
                    return false;
            }
        }
    }

    [JsonIgnore]
    public bool IsFund {
        get {
            switch (AssetType) {
                case global::AssetType.USStock_Fund:
                case global::AssetType.InternationalStock_Fund:
                case global::AssetType.Bond_Fund:
                    return true;
                default:
                    return false;
            }
        }
    }

    [JsonIgnore]
    public bool IsCash {
        get {
            switch (AssetType) {
                case global::AssetType.Cash:
                case global::AssetType.Cash_BankAccount:
                case global::AssetType.Cash_MoneyMarket:
                    return true;
                default:
                    return false;
            }
        }
    }

    [JsonIgnore]
    [JsonPropertyName("AssetType")]
    private AssetType _TransitionAssetType; 
    public string TransitionAssetType {
        get { return _TransitionAssetType.ToString(); }
        set { 
            switch (value) {
                case "Unknown": _TransitionAssetType = global::AssetType.Unknown; break;
                case "Stock": // move to *USStock
                case "USStock": _TransitionAssetType = global::AssetType.USStock; break;
                case "InternationalStock": _TransitionAssetType = global::AssetType.InternationalStock; break;
                case "Bond": _TransitionAssetType = global::AssetType.Bond; break;
                case "Cash": _TransitionAssetType = global::AssetType.Cash; break;
                case "MoneyMarket": _TransitionAssetType = global::AssetType.Cash_MoneyMarket; break;
                case "BankAccount": _TransitionAssetType = global::AssetType.Cash_BankAccount; break;
                case "Fund_USStock": _TransitionAssetType = global::AssetType.USStock_Fund; break;
                case "Fund_Bond": _TransitionAssetType = global::AssetType.Bond_Fund; break;
                case "Fund_InternationalStock": _TransitionAssetType = global::AssetType.InternationalStock_Fund; break;
                case "Fund_Mixed": _TransitionAssetType = global::AssetType.StocksAndBonds_Fund; break;
                case "ETF_USStock": _TransitionAssetType = global::AssetType.USStock_ETF; break;
                case "ETF_InternationalStock": _TransitionAssetType = global::AssetType.InternationalStock_ETF; break;
                case "ETF_Bond": _TransitionAssetType = global::AssetType.Bond_ETF; break;
                case "ETF_Mixed": _TransitionAssetType = global::AssetType.StocksAndBonds_ETF; break;
            }

            AssetType = _TransitionAssetType;
        }
    }

    [JsonPropertyName("AssetType2")]
    public AssetType? AssetType { get; set; }

    private double? _ExpenseRatio;
    public double? ExpenseRatio {
        get {
            return _ExpenseRatio;
        }
        set {
            _ExpenseRatio = value;
        }
     }

    private int? _PIN;
    [JsonIgnore]
    public int? PIN {
        get { return _PIN; }
        set { 
            _PIN = value;
        }
    }

    [JsonIgnore]
    public double? SharesPIN {
        get {
            return Shares * (PIN ?? 1);
        }
        set {
            Shares = value / (PIN ?? 1);
        }
    }

    private double? _Shares;
    public double? Shares {
        get {
            return _Shares;
        }
        set {
            _Shares = value;
            UpdateValue();
        }
    }

    [JsonIgnore]
    public double? ValuePIN {
        get {
            return Value * (PIN ?? 1);
        }
        set {
            Value = value / (PIN ?? 1);
        }
    }

    private double? _Value;
    public double? Value {
        get {
            return _Value;
        }
        set {
            _Value = value;
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
            UpdateValue();
        }
    }

    public void UpdateValue() {
        if (Price != null && SharesPIN != null) {
            ValuePIN = Price * SharesPIN;
        }
    }

    public double? PreviousClose { get; set; }
    public double? PercentChange { get; set; }
    public DateTime? LastUpdated { get; set; }
    [JsonIgnore]
    public double Percentage { get; set; }
}