public class AppData : IAppData
{
    public AppData(FamilyData familyData) {
        FamilyData = familyData;
    }

    public FamilyData FamilyData { get; set; }
    public List<string> ProfileNames {get; set;}
    public string? CurrentProfileName {get; set;}
    public string LastPageUri {get; set;}
}