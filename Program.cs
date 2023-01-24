using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using src;
using IRS;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ISearchModel>(sp => SearchModel.Create());

IRSData? irsData = await IRSData.Create(httpClient);
if (irsData != null) {
    builder.Services.AddSingleton<IRSData>(irsData);
    builder.Services.AddSingleton<IFamilyData>(new FamilyData(irsData));
} else {
    throw new Exception("irsData is null");
}


var fundsJson = await httpClient.GetAsync("https://raw.githubusercontent.com/bogle-tools/site/main/wwwroot/data/funds.json");
var Funds = await JsonSerializer.DeserializeAsync<List<Fund>>(fundsJson.Content.ReadAsStream());
if (Funds != null) {
    builder.Services.AddSingleton<IList<Fund>>(Funds);
}
await builder.Build().RunAsync();
