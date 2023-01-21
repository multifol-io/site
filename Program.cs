using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using src;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(async(sp) => await SearchModel.CreateAsync());

FamilyYears? familyYears = await FamilyYears.Create(httpClient);
if (familyYears != null) {
    builder.Services.AddSingleton<IFamilyYears>(familyYears);
}

var fundsJson = await httpClient.GetAsync("https://raw.githubusercontent.com/bogle-tools/site/main/wwwroot/data/funds.json");
var Funds = await JsonSerializer.DeserializeAsync<List<Fund>>(fundsJson.Content.ReadAsStream());
builder.Services.AddSingleton<IList<Fund>>(Funds);
await builder.Build().RunAsync();
