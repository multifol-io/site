using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FamilyData : INotifyPropertyChanged
{
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public FamilyData(IAppData appData) {
        AppData = appData;
        Year = DateTime.Now.Year;

        Debts = new();
        Accounts = new();
        People = new();
        Questions = new();
        RetirementData = new();
        
        People.Add(new Person() { Identifier = "person 1", FamilyData = this, PersonIndex = 0 });
        People.Add(new Person() { Identifier = "person 2", FamilyData = this, PersonIndex = 1 });

        SetBackPointers();
    }

    // set back pointers in familydata
    public void SetBackPointers()
    {
        for (int personIndex = 0; personIndex < this.People.Count; personIndex++) {
            var person = this.People[personIndex];
            person.PersonIndex = personIndex;
            person.SetBackPointers(this);
        }
    }

    [JsonIgnore]
    public double PercentTotal { get; set; }
    
    [JsonIgnore]
    public IAppData AppData { get; set;}    

    public string? Title { get; set; }
    public RetirementData RetirementData { get; set; }
    public EmergencyFund EmergencyFund { get; set; } = new();

    public bool DebtsComplete {
        get {
            return ((Debts.Count == 0 && DebtFree == TriState.True) || Debts.Count > 0);
        }
    }
    
    public TriState DebtFree { get; set; }
    public List<Debt> Debts { get; set; }
    
    private TaxFilingStatus _taxFilingStatus = TaxFilingStatus.ChoiceNeeded;
    [Required]
    public TaxFilingStatus TaxFilingStatus { 
        get {
            return _taxFilingStatus;
        }
        set {
            _taxFilingStatus = value;
        }
    }

    public bool TaxFilingStatusLivingSeperately { get; set; }

    public string TaxFilingString {
        get {
            return TaxFilingStatus switch
            {
                TaxFilingStatus.Single => "Single",
                TaxFilingStatus.MarriedFilingJointly => "Married filing jointly",
                TaxFilingStatus.MarriedFilingSeperately => "Married filing separately",
                _ => "Choice needed",
            };
        }
    }

    public int? AdjustedGrossIncome { get; set; }
    public int? IncomeTaxPaid { get; set; }
    public int? TaxableToInvest { get; set; }


    public int? TotalIncome { 
        get {
            int totalSalaries = 0;
            for (int i = 0; i < PersonCount; i++)
            {
                var person = People[i];
                if (person.EmployerPlan.AnnualSalary != null)
                {
                    totalSalaries += person.EmployerPlan.AnnualSalary.Value;
                    var vestAmount = person.VestAmount();
                    totalSalaries += vestAmount.HasValue ? (int)(vestAmount.Value) : 0;
                }
            }
            
            return totalSalaries;
        }
    }

    public int? PlannedSavings {
        get {
            int? annualExpenses = EmergencyFund.MonthlyExpenses * 12;
            int investFromTaxable = TaxableToInvest ?? 0;

            return TotalIncome - (IncomeTaxPaid ?? 0) - annualExpenses + investFromTaxable;
        }
    }

    public string? FederalMarginalTaxBracket { get; set; }
    private int _Year;
    public int Year { 
        get {
            return _Year;
        }
        set {
            _Year = value;
        }
    }

    [JsonIgnore] // Transition to StateTaxRate on 7/28/2023 - https://github.com/bogle-tools/site/issues/208
    public string? StateMarginalTaxBracket {
        get {
            return StateTaxRate;
        }
        set {
            StateTaxRate = value;
        }
    }
    
    public string? StateTaxRate { get; set; }
    
    public string? StateOfResidence { get; set; }

    public bool NotSureNeedHelpWithAssetAllocation { get; set; }

    private double? _Stocks;
    public double? Stocks { 
        get {
            return _Stocks;
        }
        set {
            _Stocks = value;
            if (_Stocks != null && Bonds == null) { Bonds = 100.0 - _Stocks; }
            if (_Stocks != null || _Bonds != null) { AssetAllocationError = ((_Bonds + _Stocks) > 100.0); }
        }
    }

    private double? _Bonds;
    public double? Bonds { 
        get {
            return _Bonds;
        }
        set {
            _Bonds = value;
            if (_Bonds != null && Stocks == null) { Stocks = 100.0 - _Bonds; }
            if (_Stocks != null || _Bonds != null) { AssetAllocationError = ((_Bonds + _Stocks) > 100.0); }
        }
    }

    [JsonIgnore]
    public bool AssetAllocationError { get; set; }

    public int? International { get; set; }

    public double? StockBalance { get; set; }
    public double? InternationalStockBalance { get; set; }
    public double? BondBalance { get; set; }
    public double? CashBalance { get; set; }
    public double? OtherBalance { get; set; }

    public double ActualTotalStockAllocation {
        get {
            return (ActualStockAllocation / 100.0)* (1.0-ActualInternationalStockAllocation / 100.0) * 100.0;
        }
    }

    private void UpdateAllocations()
    {
        double stockAllocation = double.NaN;
        double internationalStockAllocation = double.NaN;
        double bondAllocation = double.NaN;
        double cashAllocation = double.NaN;
        double otherAllocation = double.NaN;

        var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);
        if (overallTotal > 0.0)
        {
            stockAllocation = ((StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0)) / overallTotal * 100.0;
            bondAllocation = (BondBalance ?? 0.0) / overallTotal * 100.0;
            cashAllocation = (CashBalance ?? 0.0) / overallTotal * 100.0;
            otherAllocation = (OtherBalance ?? 0.0) / overallTotal * 100.0;
        }

        var stockTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0);
        if (stockTotal > 0.0)
        {
            internationalStockAllocation =  (InternationalStockBalance ?? 0.0) / stockTotal * 100.0;
        }

        ActualStockAllocation = stockAllocation;
        ActualInternationalStockAllocation = internationalStockAllocation;
        ActualBondAllocation = bondAllocation;
        ActualCashAllocation = cashAllocation;
        ActualOtherAllocation = otherAllocation;
    }

    private double _ActualStockAllocation;
    public double ActualStockAllocation {
        get {
            return _ActualStockAllocation;
        }
        set {
            _ActualStockAllocation = value;
            OnPropertyChanged();
        }
    }

    private double _ActualBondAllocation;
    public double ActualBondAllocation
    {
        get {
            return _ActualBondAllocation;
        }
        set {
            _ActualBondAllocation = value;
            OnPropertyChanged();
        }
    }

    private double _ActualInternationalStockAllocation;
    public double ActualInternationalStockAllocation
    {
        get {
            return _ActualInternationalStockAllocation;
        }
        set {
            _ActualInternationalStockAllocation = value;
            OnPropertyChanged();
        }
    }

    private double _ActualCashAllocation;
    public double ActualCashAllocation
    {
        get {
            return _ActualCashAllocation;
        }
        set {
            _ActualCashAllocation = value;
            OnPropertyChanged();
        }
    }

    private double _ActualOtherAllocation;
    public double ActualOtherAllocation
    {
        get {
            return _ActualOtherAllocation;
        }
        set {
            _ActualOtherAllocation = value;
            OnPropertyChanged();
        }
    }

    public double OverallER { get; set; }
    public int InvestmentsMissingER { get; set; }
    public double ExpensesTotal { get; set; }

    public List<Person> People { get; set; }

    private double _Value;
    public double Value { 
        get {
            return _Value;
        }
        set {
            _Value = value;
            OnPropertyChanged();
        }
    }

    public int ValueStyle { get; set; } = 0;

    public List<Account> Accounts { get; set; }

    private int? _PIN;
    [JsonIgnore]
    public int? PIN { 
        get { return _PIN; }
        set {
            _PIN = value; 
            foreach (var account in Accounts) {
                account.PIN = _PIN;
            }
        }    
    }

    public bool PINProtected { get; set; }

    [JsonIgnore]
    public List<Investment> GroupedInvestments { 
        get {
            Dictionary<string,Investment> _GroupedInvestments = new();
            foreach (var account in Accounts) 
            {
                foreach (var investment in account.Investments)
                {
                    var key = string.IsNullOrEmpty(investment.Ticker) ? investment.Name ?? "missing ticker and name" : investment.Ticker;
                    if (investment.AssetType == AssetType.StocksAndBonds_ETF || investment.AssetType == AssetType.StocksAndBonds_Fund) {
                        if (investment.GetPercentage(AssetType.USStock) > 0.0) {
                            GetGroup(_GroupedInvestments, investment, key+"-S", assetType:AssetType.USStock);
                        }
                        if (investment.GetPercentage(AssetType.InternationalStock) > 0.0) {
                            GetGroup(_GroupedInvestments, investment, key+"-IS", assetType:AssetType.InternationalStock);
                        }
                        if (investment.GetPercentage(AssetType.Bond) > 0.0) {
                            GetGroup(_GroupedInvestments, investment, key+"-B", assetType:AssetType.Bond);
                        }
                        if (investment.GetPercentage(AssetType.InternationalBond) > 0.0) {
                            GetGroup(_GroupedInvestments, investment, key+"-IB", assetType:AssetType.InternationalBond);
                        }
                        if (investment.GetPercentage(AssetType.Cash) > 0.0) {
                            GetGroup(_GroupedInvestments, investment, key+"-C", assetType:AssetType.Cash);
                        }
                    } else {
                        var matchingInvestment = GetGroup(_GroupedInvestments, investment, key, assetType:null);
                    }
                }
            }

            var listInvestments = new List<Investment>();
            foreach (var key in _GroupedInvestments.Keys)
            {
                var investment = _GroupedInvestments[key];
                if (investment.Price != null && investment.SharesPIN != null)
                {
                    investment.UpdateValue();
                }

                if (investment.SharesPIN == 0.0)
                {
                    investment.SharesPIN = null;
                }

                listInvestments.Add(investment);
            }
            
            return listInvestments.OrderByDescending(i=>i.Value).ToList();
        }
    }

public string estimatePortfolio() 
    {
        if (Value >= 10000000) {
            return "8-figures";
        } else if (Value >= 6666666) {
            return "high 7-figures";
        } else if (Value >= 3333333) {
            return "mid 7-figures";
        } else if (Value >= 1000000) {
            return "low 7-figures";
        } else if (Value >= 666666) {
            return "high 6-figures";
        } else if (Value >= 333333) {
            return "mid 6-figures";
        } else if (Value >= 100000) {
            return "low 6-figures";
        } else if (Value >= 66666) {
            return "high 5-figures";
        } else if (Value >= 33333) {
            return "mid 5-figures";
        } else if (Value >= 10000) {
            return "low 5-figures";
        } else if (Value >= 1000) {
            return "4-figures";
        } else if (Value == 0) {
            return "-";
        } else {
            return "less than $1,000";
        }
    }

    private Investment GetGroup(Dictionary<string, Investment> _GroupedInvestments, Investment investment, string? key, AssetType? assetType) {
        Investment? matchingInvestment;
        if (_GroupedInvestments.ContainsKey(key: key))
        {
            matchingInvestment = _GroupedInvestments[key];
        }
        else
        {
            matchingInvestment = new Investment(PIN) { Name = investment.Name, AssetType = assetType == null ? investment.AssetType : assetType, Ticker = key, PercentChange = investment.PercentChange, LastUpdated = investment.LastUpdated, SharesPIN = null, Price = investment.GetPrice(assetType, investment.Price), PreviousClose = investment.GetPrice(assetType, investment.PreviousClose), ValuePIN = 0.0 };
            _GroupedInvestments.Add(key, matchingInvestment);
        }

        if (investment.SharesPIN != null)
        {
            if (matchingInvestment.SharesPIN == null)
            {
                matchingInvestment.SharesPIN = investment.SharesPIN;
            }
            else
            {
                matchingInvestment.SharesPIN += investment.SharesPIN;
            }
        }

        if (investment.ValuePIN != null)
        {
            matchingInvestment.ValuePIN += investment.ValuePIN;
        }

        return matchingInvestment;
    }

    public string? AdditionalBackground { get; set; }
    public List<string> Questions { get; set; }

    private int? GetDebts(string? category) {
            int? total = null;
            foreach (var debt in Debts) {
                if (debt.Category == category) {
                    if (total == null) {
                        total = debt.Total;
                    } else {
                        total += debt.Total;
                    }
                }
            }

            return total;
    }

    public int? HighDebts {
        get {
            return GetDebts("High");
        }
    }

    public int? MediumDebts {
        get {
            return GetDebts("Medium");
        }
    }

    public int? LowDebts {
        get {
            return GetDebts("Low");
        }
    }

    public int? UnknownDebts {
        get {
            return GetDebts(null);
        }
    }

    public async Task UpdatePercentagesAsync()
    {
        await Task.Run(() =>
        {
            StockBalance = 0.0;
            InternationalStockBalance = 0.0;
            BondBalance = 0.0;
            CashBalance = 0.0;
            OtherBalance = 0.0;
            OverallER = 0.0;
            InvestmentsMissingER = 0;
            ExpensesTotal = 0;

            double totalValue = 0.0;
            foreach (var account in Accounts)
            {
                double accountValue = 0.0;
                foreach (var investment in account.Investments)
                {
                    accountValue += investment.ValuePIN ?? 0.0;
                }

                account.Value = accountValue;
                totalValue += accountValue;
            }
            Value = totalValue;

            foreach (var account in Accounts)
            {
                account.Percentage = account.Value / totalValue * 100;
                account.UpdatePercentages(totalValue, this);
            }

            UpdateAllocations();
        });
    }

    public int YearIndex 
    {
        get {
            return Year - 2023;
        }
    }

    public int PersonCount {
        get {
            switch (TaxFilingStatus) {
                case TaxFilingStatus.Single:
                case TaxFilingStatus.HeadOfHousehold:
                case TaxFilingStatus.MarriedFilingSeperately:
                    return 1;
                case TaxFilingStatus.MarriedFilingJointly:
                    return 2;
                case TaxFilingStatus.ChoiceNeeded:
                default:
                    return 0;
            }
        } 
    }

    public string SaveToJson()
    {
        var options = new JsonSerializerOptions() 
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        var jsonOut = JsonSerializer.Serialize(this, options);

        return jsonOut;
    }
    public static FamilyData? LoadFromJson(IAppData appData, string json, JsonSerializerOptions options) {
        var loadedData = JsonSerializer.Deserialize<FamilyData>(json, options);
        if (loadedData != null) {
            loadedData.AppData = appData;
            loadedData.Year = 2024;
            loadedData.SetBackPointers();
            if (loadedData.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart) {
                loadedData.TaxFilingStatus = TaxFilingStatus.MarriedFilingSeperately;
                loadedData.TaxFilingStatusLivingSeperately = true;
            }
            return loadedData;
        }
        else 
        {
            return null;
        }
    }
    
    public static async Task<FamilyData?> LoadFromJsonStream(IAppData appData, Stream jsonStream, JsonSerializerOptions options) {
        var loadedData = await JsonSerializer.DeserializeAsync<FamilyData>(jsonStream, options);
        if (loadedData != null && appData != null) {
            loadedData.AppData = appData;
            loadedData.Year = 2023;
            loadedData.SetBackPointers();
            if (loadedData.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart) {
                loadedData.TaxFilingStatus = TaxFilingStatus.MarriedFilingSeperately;
                loadedData.TaxFilingStatusLivingSeperately = true;
            }
            return loadedData;
        }
        else 
        {
            return null;
        }
    }

    [JsonIgnore]
    public SortedDictionary<string,List<Investment>> TickersToUpdate;

    public async Task RefreshPrices(HttpClient http) {
        TickersToUpdate = new();

        var now = DateTime.Now.Date;
        var previousMarketClose = PreviousMarketClose(now).ToLocalTime();
        var nextMarketClose = NextMarketClose(now).ToLocalTime();
        var nextMarketOpen = new DateTime(nextMarketClose.Year,nextMarketClose.Month,nextMarketClose.Day,13,30,00).ToLocalTime();
        var marketIsBeforeOpen = DateTime.Now < nextMarketOpen;
        bool marketIsOpen = DateTime.Now >= nextMarketOpen && DateTime.Now <= nextMarketClose;
        var marketIsAfterClose = DateTime.Now > nextMarketClose;
        foreach (var account in this.Accounts) {
            foreach (var investment in account.Investments) {
                bool fetchQuote = false;
                if (investment.IsStock || investment.IsETF) {
                    fetchQuote = investment.LastUpdated == null || (marketIsBeforeOpen && investment.LastUpdated < previousMarketClose) || marketIsOpen || (marketIsAfterClose && investment.LastUpdated < nextMarketClose);
                } else if (investment.IsFund) {
                    fetchQuote = investment.LastUpdated == null || investment.LastUpdated?.Date != previousMarketClose.Date;
                } else if (investment.IsIBond) {
                    await investment.CalculateIBondValue();
                }

                if (fetchQuote) {
                    var ticker = string.IsNullOrEmpty(investment.Ticker) ? investment.Name : investment.Ticker;
                    if (ticker != null)
                    {
                        if (!TickersToUpdate.ContainsKey(ticker)) {
                            Console.WriteLine(ticker + " " + investment.Shares);
                            TickersToUpdate.Add(ticker, new List<Investment> () { investment });
                        } else {
                            var investments = TickersToUpdate[ticker];
                            investments.Add(investment);
                        }
                    }
                }
            }
        }

        List<string> rsuTickers = new();
        for(int p=0;p<this.PersonCount;p++) {
            var person = this.People[p];
            foreach (var rsuGrant in person.RSUGrants) {
                if (!string.IsNullOrEmpty(rsuGrant.Ticker)) {
                    var ticker = rsuGrant.Ticker.ToUpper();
                    var grantInvestment = new Investment() { Ticker = ticker, GrantToUpdateQuote = rsuGrant };
                    if (!TickersToUpdate.ContainsKey(ticker)) {
                        TickersToUpdate.Add(ticker, new List<Investment> () { grantInvestment });
                    } else {
                        var investments = TickersToUpdate[ticker];
                        investments.Add(grantInvestment);
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(this.AppData.EODHistoricalDataApiKey))
        {
            foreach (var quote in TickersToUpdate)
            {
                try {
                    await FetchPriceAndUpdateInvestments(quote.Key, quote.Value, http);
                } catch (Exception ex) {
                    Console.WriteLine(ex.GetType().Name + ": " + ex.Message + " " + ex.StackTrace);
                }
            }

            TickersToUpdate.Clear();
        }
    }

    private async Task FetchPriceAndUpdateInvestments(string ticker, List<Investment> investments, HttpClient http)
    {
        if (!string.IsNullOrEmpty(this.AppData.EODHistoricalDataApiKey)) {
            var quoteDataJson = await http.GetStreamAsync($"https://api.bogle.tools/api/getquotes?ticker={ticker}&apikey={this.AppData.EODHistoricalDataApiKey}");
            var quoteData = await JsonSerializer.DeserializeAsync<QuoteData>(quoteDataJson);
            if (quoteData != null && quoteData?.Close != null) {
                bool usePercent = quoteData.Volume >= 0;
                UpdateInvestmentsPrice(investments, quoteData.Close, quoteData.PreviousClose, usePercent? quoteData.ChangeP : null, usePercent ? UnixTimeStampToDateTime(quoteData.Timestamp) : null);
            }
        }
    }

    public void UpdateInvestmentsPrice(List<Investment> investments, double? price, double? previousClose, double? percentChange, DateTime? lastUpdated) 
    {
        foreach (var investment in investments)
        {
            investment.Price = price;
            investment.PreviousClose = previousClose; 
            investment.PercentChange = percentChange;
            investment.LastUpdated = lastUpdated;

            investment.UpdateValue();
        }
    }

    public static DateTime? UnixTimeStampToDateTime( int? unixTimeStamp )
    {
        if (!unixTimeStamp.HasValue) return null;
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds( unixTimeStamp ?? 0 ).ToLocalTime();
        return dateTime;
    }

    public static DateTime PreviousMarketClose(DateTime dateTime) {
        return MarketClose(dateTime, -1);
    }

    private DateTime NextMarketClose(DateTime dateTime) {
        return MarketClose(dateTime, 1);
    }
    
    private static DateTime MarketClose(DateTime dateTime, int direction) {
        DateTime marketCloseDay;
        if (direction == -1) {
            marketCloseDay = dateTime.AddDays(direction);
        } else {
            marketCloseDay = dateTime;
        }

        switch (GetMarketDay(marketCloseDay)) {
            case MarketDay.Holiday:
            case MarketDay.WeekEnd:
                return MarketClose(marketCloseDay.AddDays(direction), direction);
            case MarketDay.HalfDay:
            {
                var datetime = new DateTime(marketCloseDay.Year, marketCloseDay.Month, marketCloseDay.Day, 13, 0, 0);
                var datetimeutc = TimeZoneInfo.ConvertTimeToUtc(datetime, GetEasternTimeZoneInfo());
                var datetimelocal = TimeZoneInfo.ConvertTimeFromUtc(datetimeutc, TimeZoneInfo.Local);
                return datetimelocal;
            }
            default:
            {
                var datetime = new DateTime(marketCloseDay.Year, marketCloseDay.Month, marketCloseDay.Day, 16, 0, 0);
                var datetimeutc = TimeZoneInfo.ConvertTimeToUtc(datetime, GetEasternTimeZoneInfo());
                var datetimelocal = TimeZoneInfo.ConvertTimeFromUtc(datetimeutc, TimeZoneInfo.Local);
                return datetimelocal;
            }
        }
    }

    private static TimeZoneInfo GetEasternTimeZoneInfo()
    {
        TimeZoneInfo tz = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
           ? TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
           : TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        return tz;
    }

    enum MarketDay {
        MarketDay,
        Holiday,
        HalfDay,
        WeekEnd
    }

    static DateTime[] holidays = {
        new DateTime(2023, 1, 2),
        new DateTime(2023, 1 , 16 ),
        new DateTime(2023, 2, 20),
        new DateTime(2023, 4, 7),
        new DateTime(2023, 5, 29),
        new DateTime(2023, 6, 19),
        new DateTime(2023, 7, 4),
        new DateTime(2023, 9, 4),
        new DateTime(2023, 11, 23),
        new DateTime(2023, 12, 25),
    };

    static DateTime[] halfDays = {
        new DateTime(2023, 7, 3),
        new DateTime(2023, 11, 24),
        new DateTime(2023, 12, 24),
    };

    private static MarketDay GetMarketDay(DateTime dateTime) {
        switch (dateTime.DayOfWeek) {
            case DayOfWeek.Saturday: 
            case DayOfWeek.Sunday:
                return MarketDay.WeekEnd;
            default:
                var date = dateTime.Date;
                if (holidays.Contains(date)) {
                    return MarketDay.Holiday;
                }

                if (halfDays.Contains(date)) {
                    return MarketDay.HalfDay;
                }

                return MarketDay.MarketDay;
        }
    }
}