using System.Text.Json;
using System.Text.Json.Serialization;
using IRS;

public static class ProfileUtilities
{
    public static string Key { get; set; } = "localSave";
    public static string Value { get; set; } = "";
    public static string storedJson { get; set; } = "";

    public static LocalStorageAccessor? LocalStorageAccessor { get; set; }

    public static async Task Save(string key, FamilyData familyData)
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
        var jsonOut = JsonSerializer.Serialize(familyData, options);

        await LocalStorageAccessor.SetValueAsync(key, jsonOut);
    }

    public static async Task Load(IAppData appData)
    {
        try {
            storedJson = await LocalStorageAccessor.GetValueAsync<string>(appData.CurrentProfileKey);
            var options = new JsonSerializerOptions() 
            {
                Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
            };

            appData.FamilyData = FamilyData.LoadFromJson(appData.FamilyData, storedJson, options);
        } 
        catch (Exception e)
        {
            // Key + " in local storage not found...loading default."
            Console.WriteLine(e.GetType().Name + " " + e.Message);
        }
    }

    public static async Task Clear(IAppData appData, IRSData irsData)
    {
        appData.FamilyData = new FamilyData(irsData);
        await LocalStorageAccessor.RemoveAsync(Key);
    }

    public static async Task ClearAllAsync()
    {
        await LocalStorageAccessor.Clear();
    }
}