using IRS;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Models;
using src;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ISearchModel>(sp => SearchModel.Create());

builder.Services.AddScoped<LocalStorageAccessor>();
var lsa = builder.Services.BuildServiceProvider().GetService<LocalStorageAccessor>();
string? CurrentProfileName = null;
string? EODHistoricalDataApiKey = null;
if (lsa != null)
{
    builder.Services.AddScoped<IProfileUtility>(sp => new BrowserProfileUtility(lsa));
    CurrentProfileName = await lsa.GetValueAsync<string>("CurrentProfileName");
    EODHistoricalDataApiKey = await lsa.GetValueAsync<string>("EODHistoricalDataApiKey");
}

IRSData? irsData = await IRSData.Create(httpClient);
if (irsData != null) {
    var appData = new AppData(builder.HostEnvironment.BaseAddress) { IRSData = irsData };
    irsData.AppData = appData;
    appData.CurrentProfileName = CurrentProfileName;
    appData.EODHistoricalDataApiKey = EODHistoricalDataApiKey;
    builder.Services.AddSingleton<IRSData>(irsData);
    builder.Services.AddSingleton<IAppData>(appData);
    await appData.LoadFundsAsync();
    builder.Services.AddSingleton<IList<Fund>>(appData.Funds);
}
else {
    throw new Exception("irsData is null");
}

await builder.Build().RunAsync();
