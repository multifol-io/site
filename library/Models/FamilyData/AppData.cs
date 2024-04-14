using IRS;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Models;

public class AppData : IAppData, INotifyPropertyChanged
{
    public AppData(string baseAddress = "https://bogle.tools")
    {
        BaseAddress = baseAddress;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public FamilyData? FamilyData { get; set; }
    public string? CurrentProfileName { get { return currentProfileName; } set { currentProfileName = value; OnPropertyChanged(); } }
    public string? LastPageUri { get; set; }
    public IRSData? IRSData { get; set; }
    public string? EODHistoricalDataApiKey { get; set; }
    public bool ShowValues { get { return showValues; } set { showValues = value; OnPropertyChanged(); } }
    public int Year { get; set; }
    [JsonIgnore] // no longer using as of 7/8/2023, stop saving.
    public ImportResult? ImportResult { get; set; }
    public bool ApplyStockSizeRules { get; set; }
    public bool ApplyTaxEfficientPlacementRules { get; set; }
    public bool AllowAfterTaxPercentage { get; set; }

    [JsonIgnore]
    public List<Fund> Funds
    {
        get
        {
            if (funds is null)
            {
                Task.Run(async () => funds = await LoadFundsAsync());
            }

            if (funds != null)
            {
                return funds;
            }
            else
            {
                return new List<Fund>();
            }
        }
    }

    public string BaseAddress { get; private set; }

    public async Task<List<Fund>> LoadFundsAsync()
    {
        var httpClient = new HttpClient();
        var fundsUri = new Uri(BaseAddress + "/data/funds.json");
        var fundsJson = await httpClient.GetAsync(fundsUri.AbsoluteUri);

        var stocksUri = new Uri(BaseAddress + "/data/USStocks.json");
        var stocksJson = await httpClient.GetAsync(stocksUri.AbsoluteUri);

        JsonSerializerOptions options = new()
        {
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        var funds = await JsonSerializer.DeserializeAsync<List<Fund>>(fundsJson.Content.ReadAsStream(), options);
        var Stocks = await JsonSerializer.DeserializeAsync<List<Fund>>(stocksJson.Content.ReadAsStream(), options);
        if (funds is not null && Stocks is not null)
        {
            funds.AddRange(Stocks);
            return funds;
        }
        else
        {
            return new List<Fund>();
        }
    }

    private List<Fund>? funds;
    private string? currentProfileName;
    private bool showValues;


}