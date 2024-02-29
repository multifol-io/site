using System.Text.Json;
using System.Text.Json.Serialization;

namespace Models;

public class BrowserProfileUtility(LocalStorageAccessor localStorageAccessor) : IProfileUtility
{
    private string StoredJson { get; set; } = "";
    private LocalStorageAccessor LocalStorageAccessor { get; set; } = localStorageAccessor;

    public async Task Save(string? key, FamilyData? familyData)
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

        if (familyData is not null) {
            var familyDataJson = JsonSerializer.Serialize(familyData, options);

            await Save(key, familyDataJson);
        }
    }

    public async Task Save(string? key, string familyDataJson)
    {
        if (key is not null) {
            await LocalStorageAccessor!.SetValueAsync(key, familyDataJson);
        }
    }


    public async Task<string> GetProfileData(string profileName)
    {
        var profileData = await LocalStorageAccessor!.GetValueAsync<string>(profileName);
        return profileData;
    }

    public async Task Load(IAppData appData)
    {
        ArgumentNullException.ThrowIfNull(appData);

        if (appData.CurrentProfileName == null)
        {
            throw new ArgumentNullException("appData.CurrentProfileName");
        }
        
        StoredJson = await LocalStorageAccessor!.GetValueAsync<string>(appData.CurrentProfileName);
        var options = new JsonSerializerOptions() 
        {
            Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
        };
        
        var loadedFamilyData = FamilyData.LoadFromJson(appData, StoredJson, options);
        if (loadedFamilyData == null) {
            // error loading profile. how should we handle this?
        } else {
            foreach (var account in loadedFamilyData.Accounts) {
                if (string.IsNullOrEmpty(account.Identifier) || account.Identifier == "our") {
                    account.Owner = 0;
                } else if (account.Identifier == loadedFamilyData.People[0].PossessiveID) {
                    account.Owner = 1;
                } else if (loadedFamilyData.PersonCount == 2 && account.Identifier == loadedFamilyData.People[1].PossessiveID) {
                    account.Owner = 2;
                }
            }

            await loadedFamilyData.UpdatePercentagesAsync();
            appData.FamilyData = loadedFamilyData;
        }
    }

    public async Task ClearAllAsync()
    {
        await LocalStorageAccessor!.Clear();
    }

    public async Task<List<String>> GetProfileNames()
    {
        var keys = new List<string>();
        var keysJsonElement = await LocalStorageAccessor!.GetKeys();
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