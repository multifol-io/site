namespace Models
{
    public interface IProfileUtility
    {
        Task<List<String>> GetProfileNames();
        Task Load(IAppData appData);
        Task Save(string? key, FamilyData? familyData);
        Task Save(string? key, string familyDataJson);
        Task<string> GetProfileData(string profileName);
        Task ClearAllAsync();
    }
}