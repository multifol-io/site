using IRS;

namespace Models;

public interface IAppData {
    public FamilyData? FamilyData { get; set; }
    public string? CurrentProfileName {get; set;}
    public string? LastPageUri {get; set;}
    public IRSData? IRSData {get; set;}
    public string? EODHistoricalDataApiKey { get; set;}
    public bool ShowValues { get; set; }
    public int Year {get; set;} 
    public ImportResult? ImportResult { get; set; }
    public bool ApplyStockSizeRules { get; set; }
    public bool ApplyTaxEfficientPlacementRules { get; set; }
    public bool AllowAfterTaxPercentage { get; set;}
}