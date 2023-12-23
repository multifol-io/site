using System.Text.Json.Serialization;

public class Transaction
{
    [JsonIgnore]
    public Investment FromInvestment { get; set; }
    [JsonIgnore]
    public Investment ToInvestment { get; set; }
    public double? Value { get; set; }
}