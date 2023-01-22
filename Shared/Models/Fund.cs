using System.Text.Json.Serialization;

public class Fund
{
    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }
    [JsonPropertyName("longName")] 
    public string? LongName { get; set; }
    [JsonPropertyName("expenseRatio")]
    public double? ExpenseRatio { get; set; }
}