using IRS;

public interface IAppData {
    public FamilyData FamilyData { get; set; }
    public Task<List<string>> CalculateProfileNames();
    public string? CurrentProfileName {get; set;}
    public string LastPageUri {get; set;}
    public IRSData IRSData {get; set;}
    public string EODHistoricalDataApiKey { get; set;}
    public int Year {get; set;} 
    public string PortfolioView { get; set; }
}