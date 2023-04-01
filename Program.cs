using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using src;
using IRS;
using Microsoft.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ISearchModel>(sp => SearchModel.Create());

IRSData? irsData = await IRSData.Create(httpClient);
if (irsData != null) {
    builder.Services.AddSingleton<IRSData>(irsData);
    var appData = new AppData(new FamilyData(irsData));
    builder.Services.AddSingleton<IAppData>(appData);
} else {
    throw new Exception("irsData is null");
}

var fundsUri = new Uri(builder.HostEnvironment.BaseAddress + "/data/funds.json");
var fundsJson = await httpClient.GetAsync(fundsUri.AbsoluteUri);
JsonSerializerOptions options = new() {
    Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
};

var Funds = await JsonSerializer.DeserializeAsync<List<Fund>>(fundsJson.Content.ReadAsStream(), options);
if (Funds != null) {
    builder.Services.AddSingleton<IList<Fund>>(Funds);
}

builder.Services.AddScoped<LocalStorageAccessor>();
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
});
await builder.Build().RunAsync();
