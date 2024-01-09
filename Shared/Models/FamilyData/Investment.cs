using System.ComponentModel;
using System.Text.Json.Serialization;

public class Investment 
{
    public Investment() {
        Transactions = new();
    }

    public Investment(int? pin) : this()
    {
        PIN = pin;
    }

    [JsonIgnore]
    public Transaction SelectedTransaction { get; set; }

    [JsonIgnore]
    public bool Selected { get; set; }
    
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

    public List<Transaction> Transactions { get; set; }

    public DateOnly? PurchaseDate { get; set; }
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

    public double? GetPrice(AssetType? assetType, double? price) {
        if (assetType == null) { return price; }
        else {
            return assetType switch
            {
                global::AssetType.USStock or 
                global::AssetType.InternationalStock or
                global::AssetType.Bond or
                global::AssetType.InternationalBond or
                global::AssetType.Cash => price * GetPercentage(assetType),
                _ => throw new ArgumentException("unexpected " + assetType.ToString()),
            };
        }
    }

    public double? GetPercentage(AssetType? assetType) {
        if (assetType == null) { return 100.0 / 100.0; }
        else {
            return assetType switch
            {
                global::AssetType.USStock => USStockPercent / 100.0,
                global::AssetType.InternationalStock => InternationalStockPercent / 100.0,
                global::AssetType.Bond => USBondsPercent / 100.0,
                global::AssetType.InternationalBond => InternationalBondsPercent / 100.0,
                global::AssetType.Cash => CashPercent / 100.0,
                _ => throw new ArgumentException("unexpected " + assetType.ToString()),
            };
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
            USStockPercent = fund.StockAlloc;
            InternationalStockPercent = fund.IntlStockAlloc;
            USBondsPercent = fund.BondAlloc;
            InternationalBondsPercent = fund.IntlBondAlloc;
            CashPercent = fund.CashAlloc;
        }
    }
    public string? VanguardFundId { get; set;  }

    [JsonIgnore]
    public bool IsETF {
        get {
            var assetType = AssetType ?? global::AssetType.Unknown;
            return assetType switch
            {
                global::AssetType.USStock_ETF or global::AssetType.InternationalStock_ETF or global::AssetType.Bond_ETF or global::AssetType.InternationalBond_ETF or global::AssetType.StocksAndBonds_ETF => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    public bool IsStock {
        get {
            return AssetType switch
            {
                global::AssetType.USStock or global::AssetType.InternationalStock => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    public bool IsFund {
        get {
            return AssetType switch
            {
                global::AssetType.USStock_Fund or global::AssetType.InternationalStock_Fund or global::AssetType.Bond_Fund or global::AssetType.InternationalBond_Fund or global::AssetType.StocksAndBonds_Fund => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    public bool IsCash {
        get {
            return AssetType switch
            {
                global::AssetType.Cash or global::AssetType.Cash_BankAccount or global::AssetType.Cash_MoneyMarket => true,
                _ => false,
            };
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

    public double? USStockPercent { get; set; }
    public double? InternationalStockPercent { get; set; }
    public double? USBondsPercent { get; set; }
    public double? InternationalBondsPercent { get; set; }
    public double? CashPercent { get; set; }

    private AssetType? _AssetType;
    [JsonPropertyName("AssetType2")]
    public AssetType? AssetType {
        get { return _AssetType; }
        set {
            _AssetType = value;
        }
    }

    public int InvestmentOrder {
        get {
            return (AssetType ?? global::AssetType.Unknown) switch
            {
                global::AssetType.USStock or global::AssetType.USStock_ETF or global::AssetType.USStock_Fund or global::AssetType.Stock=> 1,
                global::AssetType.InternationalStock or global::AssetType.InternationalStock_ETF or global::AssetType.InternationalStock_Fund => 2,
                global::AssetType.Bond or global::AssetType.IBond or global::AssetType.Bond_ETF or global::AssetType.Bond_Fund or global::AssetType.InternationalBond or global::AssetType.InternationalBond_ETF or global::AssetType.InternationalBond_Fund => 3,
                global::AssetType.StocksAndBonds_ETF or global::AssetType.StocksAndBonds_Fund => 4,
                global::AssetType.Cash or global::AssetType.Cash_BankAccount or global::AssetType.Cash_MoneyMarket => 5,
                _ => 6,
            };
        }
    }

    public string InvestmentOrderCategory {
        get {
            return InvestmentOrder switch
            {
                1 => "US Stocks",
                2 => "International Stocks",
                3 => "Bonds",
                4 => "Balanced Funds",
                5 => "Cash",
                _ => "Other",
            };
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
    public RSUGrant GrantToUpdateQuote { get; set; }

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
            if (_Price != null && GrantToUpdateQuote != null) {
                GrantToUpdateQuote.LastPrice = Price;
            }
            UpdateValue();
        }
    }

    public void UpdateValue() {
        if (Price != null && SharesPIN != null) {
            switch (AssetType ?? global::AssetType.Unknown) {
                case global::AssetType.Unknown:
                case global::AssetType.Bond:
                    break;
                default:
                    ValuePIN = Price * SharesPIN;
                    break;
            }
        }
    }

    public double? PreviousClose { get; set; }
    public double? PercentChange { get; set; }
    public DateTime? LastUpdated { get; set; }

    [JsonIgnore]
    public double Percentage { get; set; }

    public static Dictionary<string,List<double>>? IBondRates { get; set; }
    
    static async Task LoadIBondRates() {
        IBondRates = new();
        var ibondsUri = new Uri("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/treasury-direct/i-bond-rate-chart.csv");
        var httpClient = new HttpClient();
        var ibondsCsv = await httpClient.GetAsync(ibondsUri.AbsoluteUri);
        var stream = await ibondsCsv.Content.ReadAsStreamAsync();
        using var reader = new CsvReader(stream);
        var RowEnumerator = reader.GetRowEnumerator().GetAsyncEnumerator();
        await RowEnumerator.MoveNextAsync();
        await RowEnumerator.MoveNextAsync();
        while (await RowEnumerator.MoveNextAsync())
        {
            string[] chunks = RowEnumerator.Current;
            int chunkNum = 0;
            string? date = null;
            List<double> rates = new();
            foreach (var chunk in chunks)
            {
                switch (chunkNum) 
                {
                    case 0:
                        date = chunk[..5];
                        if (!char.IsDigit(date[0])) 
                        {
                            // lines at bottom of the csv file that don't start with a dates should be skipped.
                            return;
                        }
                        break;
                    case 1:
                        break;
                    default:
                        if (string.IsNullOrEmpty(chunk))
                        {
                            continue;
                        }
                        else 
                        {
                            var rate = DoubleFromPercentageString(chunk);
                            rates.Add(rate);
                        }
                        break;
                }
                
                chunkNum++;
            }

            IBondRates[date!] = rates;
        }
    }

    static string GetRateDate(int month, int year) 
    {
        if (month < 5) {
            return "11/" + (year-1).ToString().Substring(2);
        }
        else if (month < 11) {
            return "05/" + (year).ToString().Substring(2);
        } else {
            return "11/" + (year).ToString().Substring(2);
        }
    }

    public async Task CalculateIBondValue()
    {
        if (IBondRates == null) {
            await LoadIBondRates();
        }

        if (IBondRates != null)
        {
            if (PurchaseDate.HasValue)
            {
                var month = PurchaseDate.Value.Month;
                var year = PurchaseDate.Value.Year;
                var date = GetRateDate(month, year);
                var rates = IBondRates[date];

                var nowMonth = DateTime.Now.Month;
                var nowYear = DateTime.Now.Year;
                double value = CostBasis ?? 0.0;
                double bondQuantity = (value / 25.0);
                for (int i = rates.Count - 1; i >= 0; i--)
                {
                    var monthCount = i > 0 ? 6 : GetMonthsLeft(PurchaseDate.Value, DateTime.Now);
                    value = (bondQuantity*Math.Round(value/bondQuantity*Math.Pow((1.0+rates[i]/2.0),((double)monthCount/6.0)),2));
                }

                ValuePIN = (int)value;
            }
        }
    }

    private static int GetMonthsLeft(DateOnly purchaseDate, DateTime now)
    {
        var months = ((now.Year - purchaseDate.Year) * 12 + now.Month - purchaseDate.Month) % 6;
        return months;
    }

    private static double DoubleFromPercentageString(string value)
    {
        return double.Parse(value.Replace("%","")) / 100;
    }
}