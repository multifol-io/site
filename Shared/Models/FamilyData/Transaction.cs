using System.Text.Json.Serialization;

public class Transaction
{
    public string? HostTicker { get; set; }
    public string? OtherTicker { get; set; }

    [JsonIgnore]
    public bool Selected { get; set; }

    [JsonIgnore]
    public bool IsDraft { get; set; }

    private double? _Value;
    private double? _Shares;
    private double? _Price;

    public double? CustomValue(Investment investment) 
    {
        var ticker = investment.Ticker;
        switch (Type) 
        {
            case "Dividend":
                if (investment.AssetType == AssetType.Bond || investment.AssetType == AssetType.Bond_ETF || investment.AssetType == AssetType.Bond_Fund)
                {
                    return HostTicker == ticker ? 0 : -Value;
                }
                else
                {
                    return HostTicker == ticker ? -Value : Value;
                }
            case "Buy":
                return HostTicker == ticker ? Value : -Value;
            case "Sale":
                return HostTicker == ticker ? -Value : Value;

        }

        return null;
    }

    public string? Type { get; set; }

    public double? Value {
        set {
            _Value = value;
            if (_Value != null && _Shares != null && _Price == null)
            {
                _Price = _Value / _Shares;
            }

            if (_Value != null && _Shares == null && _Price != null)
            {
                _Shares = _Value / _Price;
            }
        }
        get { 
            return _Value;
        }
    }
    public double? Shares {
        set {
            _Shares = value;
            if (_Shares != null && _Value != null && _Price == null)
            {
                _Price = _Value / _Shares;
            }

            if (_Shares != null && _Value == null && _Price != null)
            {
                _Value = _Price * _Shares;
            }
        }
        get { 
            return _Shares;
        }
    }
    public double? Price {
        set {
            _Price = value;
            if (_Price != null &&_Shares != null && _Value == null)
            {
                _Value = _Price * _Shares;
            }

            if (_Price != null && _Shares == null && _Value != null)
            {
                _Shares = _Value / _Price;
            }
        }
        get { 
            return _Price;
        }
    }
}