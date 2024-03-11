using IRS;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Models;

public class AppData : IAppData, INotifyPropertyChanged
{
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public FamilyData? FamilyData { get; set; }
    public string? CurrentProfileName {get; set;}
    public string? LastPageUri {get; set;}
    public IRSData? IRSData {get; set;}
    public string? EODHistoricalDataApiKey { get; set;}
    public bool ShowValues { get{ return showValues; } set{ showValues = value; OnPropertyChanged(); } }
    public int Year {get; set;} 
    [JsonIgnore] // no longer using as of 7/8/2023, stop saving.
    public ImportResult? ImportResult { get; set; }
    public bool ApplyStockSizeRules { get; set; }
    public bool ApplyTaxEfficientPlacementRules { get; set; }
    public bool AllowAfterTaxPercentage { get; set;}

    private bool showValues;
}