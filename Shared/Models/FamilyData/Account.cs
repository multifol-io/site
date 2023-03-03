using System.Text.Json.Serialization;

public class Account 
{
    public Account() {
        Investments = new();
        AvailableFunds = new();
    }

    public string? Identifier { get; set; }
    public string AccountType { get; set; } = "Taxable";
    public string? Custodian { get; set; }
    public string? Note { get; set; }
    public double Value { 
        get {
            double newValue = 0;
            foreach (var investment in Investments) 
            {
                newValue += investment.Value ?? 0;
            }

            return newValue;
        }
    }
    public bool Edit { get; set; }
    [JsonIgnore]
    public double Percentage { get; set; }

    public string FullName 
    {
        get {
            return (Identifier != null ? Identifier+ " " : "") + AccountType + (Custodian != null ? " at " + Custodian : "");
        }
    }

    public List<Investment> Investments { get; set; }

    public List<Investment> AvailableFunds { get; set; }

    public void UpdatePercentages(double totalValue, FamilyData familyData)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = (investment.Value ?? 0) / totalValue * 100;
            if (investment.AssetType == null)
            {
                familyData.OtherBalance += investment.Value;
            } 
            else
            {
                switch (investment.AssetType) {
                    case AssetType.Stock:
                        familyData.StockBalance += investment.Value;
                        break;
                    case AssetType.InternationalStock:
                        familyData.InternationalStockBalance += investment.Value;
                        break;
                    case AssetType.Bond:
                        familyData.BondBalance += investment.Value;
                        break;
                    case AssetType.Cash:
                        familyData.CashBalance += investment.Value;
                        break;
                    default:
                        throw new InvalidDataException("unexpected case");
                }
            }
        }
    }

    public void GuessAccountType() 
    {
        if (Note.Contains("401K") || Note.Contains("401k"))
        {
            AccountType = "401k";
        }
        else if (Note.Contains("HSA") || Note.Contains("Health Savings Account"))
        {
            AccountType = "HSA";
        }
    }
}