using IRS;
using System.Text.Json;

public class FamilyYears : IFamilyYears
{
    public async static Task<FamilyYears?> Create(HttpClient httpClient) 
    {
        FamilyYears? familyYears = null;

        var retirementYear1 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.retirement.2022.json");
        var retirementYear2 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.retirement.2023.json");
        var taxRatesYear1 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.tax-rates.2022.json");
        var taxRatesYear2 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.tax-rates.2023.json");
        RetirementData? retirementDataY1 = await JsonSerializer.DeserializeAsync<IRS.RetirementData>(retirementYear1);
        RetirementData? retirementDataY2 = await JsonSerializer.DeserializeAsync<IRS.RetirementData>(retirementYear2);
        TaxRateData? taxRatesY1 = await JsonSerializer.DeserializeAsync<IRS.TaxRateData>(taxRatesYear1);
        TaxRateData? taxRatesY2 = await JsonSerializer.DeserializeAsync<IRS.TaxRateData>(taxRatesYear2);
        if (retirementDataY1 != null && retirementDataY2 != null && taxRatesY1 != null && taxRatesY2 != null) {
            familyYears = new FamilyYears(retirementDataY1, retirementDataY2, taxRatesY1, taxRatesY2);
            familyYears.CurrentYear = 1;
        }

        familyYears.Search = new SearchModel();

        return familyYears;
    }

    public FamilyYears(IRS.RetirementData retirementDataY1, IRS.RetirementData retirementDataY2, IRS.TaxRateData taxRatesY1, TaxRateData taxRatesY2)
    {
        //TODO: don't hardcode years. use current date, etc...
        Years[0] = new FamilyYear(retirementDataY1, taxRatesY1, 2022);
        Years[1] = new FamilyYear(retirementDataY2, taxRatesY2, 2023);
    }

    public FamilyYear[] Years = new FamilyYear[2];

    public int CurrentYear { get; set; }

    public FamilyYear Active {
        get { return Years[CurrentYear]; }
    }
    public SearchModel Search { get; set; }
}