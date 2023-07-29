using System.Text.Json.Serialization;

public class Fund
{
    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }
    [JsonPropertyName("longName")] 
    public string? LongName { get; set; }
    [JsonPropertyName("expenseRatio")]
    public double? ExpenseRatio { get; set; }
    [JsonPropertyName("assetType")]
    public AssetType? AssetType { get; set; }
    [JsonPropertyName("vanguardFundId")]
    public string? VanguardFundId { get; set;  }

    [JsonPropertyName("stock")]
    public double? StockAlloc { get; set; }
    [JsonPropertyName("intlstock")]
    public double? IntlStockAlloc { get; set; }
    [JsonPropertyName("bondalloc")]
    public double? BondAlloc { get; set; }
    [JsonPropertyName("intlbondalloc")]
    public double? IntlBondAlloc { get; set; }
    [JsonPropertyName("cash")]
    public double? CashAlloc { get; set; }
}