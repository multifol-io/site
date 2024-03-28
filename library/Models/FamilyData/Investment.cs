using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Models;

public class Investment : INotifyPropertyChanged
{
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public Investment() {
        Transactions = [];
    }

    [JsonIgnore]
    public Transaction? SelectedTransaction { get; set; }

    [JsonIgnore]
    public bool Selected { get; set; }
    
    [JsonIgnore]
    public IList<Fund>? Funds { get; set; }

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
    public DateOnly? NextRateStart { get; set; }
    public double? InterestRate { get; set; }
    public double? CurrentRate { get; set; }
    public double? NextRate { get; set; }
    public bool AutoCompleted { get; set; }
    private string? _Ticker;
    public string? Ticker {
        get {
            return _Ticker;
        }
        set {
            if (value == null) { _Ticker = value; return; }
            _Ticker = value?.ToUpperInvariant().Trim();
            if (Funds != null) {
                bool found = false;
                foreach (var fund in Funds)
                {
                    if (_Ticker == fund.Ticker)
                    {
                        fund.AssetType ??= AssetTypes.USStock;
                    
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

    public double? GetPrice(AssetTypes? assetType, double? price) {
        if (assetType == null) { return price; }
        else {
            return assetType switch
            {
                AssetTypes.USStock or 
                AssetTypes.InternationalStock or
                AssetTypes.Bond or
                AssetTypes.InternationalBond or
                AssetTypes.Cash => price * GetPercentage(assetType),
                _ => throw new ArgumentException("unexpected " + AssetType.ToString()),
            };
        }
    }

    [JsonIgnore]
    public string? AccountName { get; set; }

    public bool IsAccountHeader { get { return AccountName != null; } }

    [JsonIgnore]
    public bool IsAssetTypeUnknown
    {
        get {
            return AssetType == AssetTypes.Unknown;
        }
    }

    [JsonIgnore]
    public bool IsBalancedFund
    {
        get {
            return (AssetType == AssetTypes.StocksAndBonds_ETF || AssetType == AssetTypes.StocksAndBonds_Fund);
        }
    }

    [JsonIgnore]
    public double TotalPercent
    {
        get {
            return (USStockPercent ?? 0.0) + (InternationalStockPercent ?? 0.0) + (USBondsPercent ?? 0.0) + (InternationalBondsPercent ?? 0.0) + (CashPercent ?? 0.0);
        }
    }

    [JsonIgnore]
    public bool DoesBalancedFundEqual100
    {
        get {
            return Math.Round(TotalPercent, 1) > 99.5;
        }
    }

    [JsonIgnore]
    public bool MissingCostBasis
    {
        get {
            return CostBasis == null;
        }
    }

    public double? GetPercentage(AssetTypes? assetType) {
        if (assetType == null) { return 100.0 / 100.0; }
        else {
            return assetType switch
            {
                AssetTypes.USStock => (USStockPercent ?? 0.0) / 100.0,
                AssetTypes.InternationalStock => (InternationalStockPercent ?? 0.0) / 100.0,
                AssetTypes.Bond => (USBondsPercent ?? 0.0) / 100.0,
                AssetTypes.InternationalBond => (InternationalBondsPercent ?? 0.0) / 100.0,
                AssetTypes.Cash => (CashPercent ?? 0.0) / 100.0,
                _ => throw new ArgumentException("unexpected " + AssetType.ToString()),
            };
        }
    }

    private void AutoComplete(Fund? fund) {
        if (fund == null) {
            Name = null;
            ExpenseRatio = null;
            AssetType = AssetTypes.Unknown;
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
            var assetType = AssetType ?? AssetTypes.Unknown;
            return assetType switch
            {
                AssetTypes.USStock_ETF or AssetTypes.InternationalStock_ETF or AssetTypes.Bond_ETF or AssetTypes.InternationalBond_ETF or AssetTypes.StocksAndBonds_ETF => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    public bool IsIBond {
        get {
            return AssetType switch
            {
                AssetTypes.IBond => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    public bool IsStock {
        get {
            return AssetType switch
            {
                AssetTypes.USStock or AssetTypes.InternationalStock => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    public bool IsFund {
        get {
            return AssetType switch
            {
                AssetTypes.USStock_Fund or AssetTypes.InternationalStock_Fund or AssetTypes.Bond_Fund or AssetTypes.InternationalBond_Fund or AssetTypes.StocksAndBonds_Fund => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    public bool IsCash {
        get {
            return AssetType switch
            {
                AssetTypes.Cash or AssetTypes.Cash_BankAccount or AssetTypes.Cash_MoneyMarket => true,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    [JsonPropertyName("AssetType")]
    private AssetTypes _TransitionAssetType; 
    public string TransitionAssetType {
        get { return _TransitionAssetType.ToString(); }
        set { 
            switch (value) {
                case "Unknown": _TransitionAssetType = AssetTypes.Unknown; break;
                case "Stock": // move to *USStock
                case "USStock": _TransitionAssetType = AssetTypes.USStock; break;
                case "InternationalStock": _TransitionAssetType = AssetTypes.InternationalStock; break;
                case "Bond": _TransitionAssetType = AssetTypes.Bond; break;
                case "Cash": _TransitionAssetType = AssetTypes.Cash; break;
                case "MoneyMarket": _TransitionAssetType = AssetTypes.Cash_MoneyMarket; break;
                case "BankAccount": _TransitionAssetType = AssetTypes.Cash_BankAccount; break;
                case "Fund_USStock": _TransitionAssetType = AssetTypes.USStock_Fund; break;
                case "Fund_Bond": _TransitionAssetType = AssetTypes.Bond_Fund; break;
                case "Fund_InternationalStock": _TransitionAssetType = AssetTypes.InternationalStock_Fund; break;
                case "Fund_Mixed": _TransitionAssetType = AssetTypes.StocksAndBonds_Fund; break;
                case "ETF_USStock": _TransitionAssetType = AssetTypes.USStock_ETF; break;
                case "ETF_InternationalStock": _TransitionAssetType = AssetTypes.InternationalStock_ETF; break;
                case "ETF_Bond": _TransitionAssetType = AssetTypes.Bond_ETF; break;
                case "ETF_Mixed": _TransitionAssetType = AssetTypes.StocksAndBonds_ETF; break;
            }

            AssetType = _TransitionAssetType;
        }
    }

    public double? USStockPercent { get; set; }
    public double? InternationalStockPercent { get; set; }
    public double? USBondsPercent { get; set; }
    public double? InternationalBondsPercent { get; set; }
    public double? CashPercent { get; set; }

    private AssetTypes? _AssetType;
    [JsonPropertyName("AssetType2")]
    public AssetTypes? AssetType {
        get { return _AssetType; }
        set {
            _AssetType = value;
        }
    }

    public int InvestmentOrder {
        get {
            return (AssetType ?? AssetTypes.Unknown) switch
            {
                AssetTypes.USStock or AssetTypes.USStock_ETF or AssetTypes.USStock_Fund or AssetTypes.Stock=> 1,
                AssetTypes.InternationalStock or AssetTypes.InternationalStock_ETF or AssetTypes.InternationalStock_Fund => 2,
                AssetTypes.Bond or AssetTypes.IBond or AssetTypes.Bond_ETF or AssetTypes.Bond_Fund or AssetTypes.InternationalBond or AssetTypes.InternationalBond_ETF or AssetTypes.InternationalBond_Fund => 3,
                AssetTypes.StocksAndBonds_ETF or AssetTypes.StocksAndBonds_Fund => 4,
                AssetTypes.Cash or AssetTypes.Cash_BankAccount or AssetTypes.Cash_MoneyMarket => 5,
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

    private double? _Shares;
    public double? Shares {
        get {
            return _Shares;
        }
        set {
            _Shares = value;
            OnPropertyChanged();
            UpdateValue();
        }
    }

    [JsonIgnore]
    public RSUGrant? GrantToUpdateQuote { get; set; }

    private double? _Value;
    public double? Value {
        get {
            return _Value;
        }
        set {
            _Value = value;
            OnPropertyChanged();
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
            OnPropertyChanged();
        }
    }
 
    public void UpdateValue() {
        if (Price != null && Shares != null) {
            switch (AssetType ?? AssetTypes.Unknown) {
                case AssetTypes.Unknown:
                case AssetTypes.Bond:
                    break;
                default:
                    Value = Price * Shares;
                    break;
            }
        }
    }

    public double? PreviousClose { get; set; }

    private double? _PercentChange;
    public double? PercentChange
    {
        get
        {
            return _PercentChange;
        }
        set
        {
            _PercentChange = value;
            OnPropertyChanged();
        }
    }

    private DateTime? _LastUpdated;
    public DateTime? LastUpdated
    {
        get
        {
            return _LastUpdated;
        }
        set
        {
            _LastUpdated = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double Percentage { get; set; }

    public static Dictionary<string,List<double>>? IBondRates { get; set; }
    
    public static async Task LoadIBondRates() {
        if (IBondRates == null) {
            IBondRates = [];
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
                List<double> rates = [];
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
                            var rate = DoubleFromPercentageString(chunk);
                            rates.Add(rate);
                            break;
                        default:
                            if (string.IsNullOrEmpty(chunk))
                            {
                                continue;
                            }
                            else 
                            {
                                var fixedRate = DoubleFromPercentageString(chunk);
                                rates.Add(fixedRate);
                            }
                            break;
                    }
                    
                    chunkNum++;
                }

                IBondRates[date!] = rates;
            }
        }
    }

    static string GetRateDate(int month, int year) 
    {
        if (month < 5) {
            return "11/" + (year-1).ToString()[2..];
        }
        else if (month < 11) {
            return "05/" + (year).ToString()[2..];
        } else {
            return "11/" + (year).ToString()[2..];
        }
    }

    public async Task<double?> CalculateIBondValue(bool current = false)
    {
        await LoadIBondRates();

        double? currentValue = null;
        if (IBondRates != null)
        {
            if (PurchaseDate.HasValue)
            {
                var now = DateTime.Now;
                var month = PurchaseDate.Value.Month;
                var year = PurchaseDate.Value.Year;
                var date = GetRateDate(month, year);

                if (current)
                {
                    var yearBeforeNow = new DateOnly(now.Year - 1, now.Month, 1);
                    if (PurchaseDate.Value.CompareTo(yearBeforeNow) > 0)
                    {
                        currentValue = 0;
                    }
                }

                bool foundDate = IBondRates.TryGetValue(date, out List<double>? rates);

                if (foundDate && rates != null)
                {
                    var nowMonth = now.Month;
                    var nowYear = now.Year;
                    decimal value = CostBasis == null ? 0.0m : (decimal)CostBasis;
                    decimal bondQuantity = (value / 25.0m);

                    if (bondQuantity != 0m) 
                    {
                        double currentRate = 0.0;
                        var monthsLeft = GetTotalMonths(PurchaseDate.Value, DateTime.Now);
                        var i = rates.Count - 1;
                        int monthsToCompoundThisRound = 6;
                        while (monthsLeft > 0)
                        {
                            //TODO: if (current), calculate currentValue and value
                            monthsToCompoundThisRound = monthsLeft >= 6 ? 6 : monthsLeft;
                            currentRate = rates[i];
                            var price = Math.Round(value/bondQuantity*(decimal)Math.Pow((1.0+currentRate/2.0),((double)monthsToCompoundThisRound/6.0)),2, MidpointRounding.AwayFromZero);
                            value = bondQuantity * price;
                            monthsLeft -= monthsToCompoundThisRound;
                            i--;
                        }

                        InterestRate = rates[0];
                        CurrentRate = monthsToCompoundThisRound != 6 ? currentRate : rates[i];
                        if (monthsToCompoundThisRound != 6 && i > 0) 
                        {
                            NextRate = rates[i];
                            var nextMonthStart = nowMonth + 6 - monthsToCompoundThisRound + 1;
                            var nextYearStart = nowYear;
                            if (nextMonthStart > 12)
                            {
                                nextMonthStart -= 12;
                                nextYearStart++;
                            }
                            NextRateStart = new DateOnly(nextYearStart, nextMonthStart, 1);
                        } else {
                            NextRate = null;
                            NextRateStart = null;
                        }
                        
                        if (PurchaseDate <= DateOnly.FromDateTime(DateTime.Now))
                        {
                            Value = (int)value;
                        }
                        else
                        {
                            Value = null;
                        }
                    } else {
                        Value = null;
                    }
                }
                else
                {
                    InterestRate = null;
                    CurrentRate = null;
                    Value = null;
                    NextRate = null;
                    NextRateStart = null;
                }
            }
        }

        if (current)
        {
            return currentValue;
        } else {
            return Value;
        }
    }

    private static int GetTotalMonths(DateOnly purchaseDate, DateTime now)
    {
        var months = ((now.Year - purchaseDate.Year) * 12 + now.Month - purchaseDate.Month);
        return months;
    }

    private static double DoubleFromPercentageString(string value)
    {
        return double.Parse(value.Replace("%","")) / 100;
    }
}