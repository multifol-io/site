public class AppData : IAppData
{
    public AppData(FamilyData familyData) {
        FamilyData = familyData;
    }

    public FamilyData FamilyData { get; set; }
}