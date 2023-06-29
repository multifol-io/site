using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Wordprocessing;
using IRS;

public static class ProfileUtilities
{
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
        var familyDataJson = JsonSerializer.Serialize(familyData, options);

        await Save(key, familyDataJson);
    }

    public static async Task Save(string key, string familyDataJson)
    {
        await LocalStorageAccessor.SetValueAsync(key, familyDataJson);
    }


    public static async Task<string> GetProfileData(string profileName)
    {
        var profileData = await LocalStorageAccessor.GetValueAsync<string>(profileName);
        return profileData;
    }

    public static async Task Load(IAppData appData)
    {
        if (appData == null)
        {
            throw new ArgumentNullException(nameof(appData));
        }
        
        storedJson = await LocalStorageAccessor.GetValueAsync<string>(appData.CurrentProfileName);
        var options = new JsonSerializerOptions() 
        {
            Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
        };
        
        var loadedFamilyData = await FamilyData.LoadFromJson(appData, storedJson, options);
        appData.FamilyData = loadedFamilyData;
    }

    public static async Task Clear(IAppData appData, string key, IRSData irsData)
    {
        appData.FamilyData = new FamilyData(appData);
        await ProfileUtilities.Save(key, appData.FamilyData);
    }

    public static async Task ClearAllAsync()
    {
        await LocalStorageAccessor.Clear();
    }

    public static async Task<List<String>> GetProfileNames()
    {
        var keys = new List<string>();
        var keysJsonElement = await LocalStorageAccessor.GetKeys();
        foreach (var el in keysJsonElement.EnumerateArray())
        {
            string? value = el.GetString();
            if (value != null) {
                switch (value) {
                    case "CurrentProfileName":
                    case "i18nextLng":
                    case "EODHistoricalDataApiKey":
                        break;
                    default:
                        keys.Add(value);
                        break;
                }
            }
        }

        return keys;
    }
}