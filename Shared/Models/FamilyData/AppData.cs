public class AppData : IAppData
{
    public AppData(IFamilyData familyData) {
        FamilyData = familyData;
    }

    public IFamilyData FamilyData { get; set; }
}