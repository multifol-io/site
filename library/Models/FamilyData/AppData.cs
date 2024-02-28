using IRS;
using System.Text.Json.Serialization;

public class AppData : IAppData
{
    public FamilyData? FamilyData { get; set; }
    public string? CurrentProfileName {get; set;}
    public string? LastPageUri {get; set;}
    public IRSData? IRSData {get; set;}
    public string? EODHistoricalDataApiKey { get; set;}
    public int Year {get; set;} 
    [JsonIgnore] // no longer using as of 7/8/2023, stop saving.
    public ImportResult? ImportResult { get; set; }
    public bool ApplyStockSizeRules { get; set; }
    public bool ApplyTaxEfficientPlacementRules { get; set; }
    public bool AllowAfterTaxPercentage { get; set;}
}