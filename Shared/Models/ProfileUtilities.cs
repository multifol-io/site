using System.Text.Json;
using System.Text.Json.Serialization;
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
        var jsonOut = JsonSerializer.Serialize(familyData, options);

        await LocalStorageAccessor.SetValueAsync(key, jsonOut);
    }

    public static async Task Load(IAppData appData)
    {
        try {
            storedJson = await LocalStorageAccessor.GetValueAsync<string>(appData.CurrentProfileName);
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

    public static async Task Clear(IAppData appData, string key, IRSData irsData)
    {
        appData.FamilyData = new FamilyData(irsData);
        await LocalStorageAccessor.RemoveAsync(key);
    }

    public static async Task ClearAllAsync()
    {
        await LocalStorageAccessor.Clear();
    }

    public static async Task SetProfileNames(IAppData appData)
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
                        break;
                    default:
                        keys.Add(value);
                        break;
                }
            }
        }

        appData.ProfileNames = keys;
    }
}