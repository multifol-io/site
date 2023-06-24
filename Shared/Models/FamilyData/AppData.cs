using IRS;

public class AppData : IAppData
{
    public FamilyData FamilyData { get; set; }
    public async Task<List<string>> CalculateProfileNames()
    {
        return await ProfileUtilities.GetProfileNames();
    }
    public string? CurrentProfileName {get; set;}
    public string LastPageUri {get; set;}
    public IRSData IRSData {get; set;}
    public string EODHistoricalDataApiKey { get; set;}
    public int Year {get; set;} 
}