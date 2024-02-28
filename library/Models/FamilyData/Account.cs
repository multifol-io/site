using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Models;

public class Account : INotifyPropertyChanged
{
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public Account() {
        Investments = [];
        AvailableFunds = [];
    }

    public string Title {
        get {
            return (Identifier != null ? Identifier + " " : "") + AccountType + (!string.IsNullOrEmpty(Custodian) ? " at " + Custodian : "") + (Note != null ? $" ({Note})":"");
        }
    }

    [JsonIgnore]
    public Investment? SelectedInvestment { get; set; }
    [JsonIgnore]
    public int Owner { get; set; }
    public string? Identifier { get; set; }
    private string? _AccountType;

    public Investment? SettlementInvestment {
        get {
            foreach (var investment in this.Investments)
            {
                if (investment.AssetType == AssetTypes.Cash_MoneyMarket 
                    || investment.AssetType == AssetTypes.Cash_BankAccount 
                    || investment.AssetType == AssetTypes.Cash)
                {
                    return investment;
                }
            }

            return null;
        }
    }

    public string? AccountType {
        get { return _AccountType; }
        set {
            // migrate old values on 7/22/2023
            _AccountType = value switch
            {
                "401k" => "401(k)",
                "403b" => "403(b)",
                "457b" => "457(b)",
                "Roth 401k" => "Roth 401(k)",
                "Solo 401k" => "Solo 401(k)",
                _ => value,
            };
            if (Identifier == "our" && TaxType != "Taxable" && TaxType != "Other") {
                Identifier = null;
            }
        }
    }
    public string? Custodian { get; set; }
    public string? Note { get; set; }

    private double _Value;
    public double Value { 
        get {
            return _Value;
        }
        set {
            _Value = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public bool Edit { get; set; }
    [JsonIgnore]
    public bool View { get; set; }
    [JsonIgnore]
    public double Percentage { get; set; }

    public string FullName 
    {
        get {
            return (Identifier != null ? Identifier+ " " : "") + AccountType + (Custodian != null ? " at " + Custodian : "");
        }
    }

    public Investment? FindInvestment(string ticker)
    {
        foreach (var investment in Investments)
        {
            if (investment.Ticker == ticker)
            {
                return investment;
            }
        }

        return null;
    }

    public List<Investment> Investments { get; set; }

    public List<Investment> AvailableFunds { get; set; }

    [JsonIgnore]
    public Account? ReplaceAccount { get; set; }

    [JsonIgnore]
    public bool Import { get; set; }

    public double? CalculateCostBasis() {
        double costBasis = 0.0;
        int investmentsMissingCostBasis = 0;
        switch (TaxType) {
            case "Taxable":
                foreach (var investment in Investments)
                {
                    if (investment.CostBasis != null)
                    {
                        costBasis += investment.CostBasis.Value;
                    }
                    else if (investment.AssetType == AssetTypes.Cash
                        || investment.AssetType == AssetTypes.Cash_BankAccount
                        || investment.AssetType == AssetTypes.Cash_MoneyMarket)
                    {
                        costBasis += investment.Value ?? 0.0;
                    }
                    else
                    {
                        investmentsMissingCostBasis++;
                    }
                }
                break;
            case "Post-Tax":
            case "Pre-Tax":
                return costBasis;
        }

        if (investmentsMissingCostBasis > 0) {
            return null;
        } else {
            return costBasis;
        }
    }

    public async Task UpdatePercentagesAsync(double totalValue, FamilyData familyData)
    {
        await Task.Run(() => {
            foreach (var investment in Investments)
            {
                investment.Percentage = (investment.Value ?? 0) / totalValue * 100;
                
                UpdateInvestmentCategoryTotals(investment, familyData);
                foreach (var transaction in investment.Transactions)
                {
                    if (transaction.HostTicker != null)
                    {
                        var foundInvestment = FindInvestment(transaction.HostTicker);
                        if (foundInvestment != null)
                        {
                            UpdateInvestmentCategoryTotals(investment, familyData, overrideValue: transaction.CustomValue(foundInvestment));
                        }
                    }
                }

                if (investment.ExpenseRatio.HasValue && investment.Value.HasValue) {
                    familyData.OverallER += investment.Percentage / 100.0 * investment.ExpenseRatio.Value;
                    familyData.ExpensesTotal += investment.Value.Value * investment.ExpenseRatio.Value / 100.0;
                } else {
                    if (investment.IsETF || investment.IsFund)
                    {
                        familyData.InvestmentsMissingER++;
                    }
                }
            }
        });
    }

    public static void UpdateInvestmentCategoryTotals(Investment investment, FamilyData familyData, double? overrideValue = null, bool useNegative = false)
    {
        double? value = (investment.Value ?? 0.0) * (useNegative?-1:1);
        if (overrideValue != null)
        {
            value = overrideValue * (useNegative?-1:1);
        }

        if (investment.AssetType == null)
        {
            familyData.OtherBalance += value;
        } 
        else
        {
            switch (investment.AssetType) {
                case AssetTypes.Stock: 
                case AssetTypes.USStock:
                case AssetTypes.USStock_ETF:
                case AssetTypes.USStock_Fund:
                    familyData.StockBalance += value;
                    break;
                case AssetTypes.InternationalStock:
                case AssetTypes.InternationalStock_ETF:
                case AssetTypes.InternationalStock_Fund:
                    familyData.InternationalStockBalance += value;
                    break;
                case AssetTypes.Bond:
                case AssetTypes.Bond_ETF:
                case AssetTypes.Bond_Fund:
                case AssetTypes.IBond:
                case AssetTypes.InternationalBond:
                case AssetTypes.InternationalBond_ETF:
                case AssetTypes.InternationalBond_Fund:
                    familyData.BondBalance += value;
                    break;
                case AssetTypes.Cash:
                case AssetTypes.Cash_BankAccount:
                case AssetTypes.Cash_MoneyMarket:
                    familyData.CashBalance += value;
                    break;
                case AssetTypes.StocksAndBonds_ETF:
                case AssetTypes.StocksAndBonds_Fund:
                    familyData.StockBalance += value * investment.GetPercentage(AssetTypes.USStock);
                    familyData.InternationalStockBalance += value * investment.GetPercentage(AssetTypes.InternationalStock);
                    familyData.BondBalance += value * investment.GetPercentage(AssetTypes.Bond);
                    familyData.BondBalance += value * investment.GetPercentage(AssetTypes.InternationalBond);
                    familyData.CashBalance += value * investment.GetPercentage(AssetTypes.Cash);
                    break;
                case AssetTypes.Unknown:
                    familyData.OtherBalance += value;
                    break;
                default:
                    throw new InvalidDataException("unexpected case");
            }
        }
    }

    public void GuessAccountType() 
    {
        if (Note is not null) {
            if (Note.Contains("401K") || Note.Contains("401k"))
            {
                AccountType = "401(k)";
            }
            else if (Note.Contains("HSA") || Note.Contains("Health Savings Account"))
            {
                AccountType = "HSA";
            }
        }
    }

    public string TaxType {
        get {
            return AccountType switch
            {
                "401(k)" or "403(b)" or "457(b)" or "457(b) Governmental" or "SEP IRA" or "Solo 401(k)" or "SIMPLE IRA" => "Pre-Tax(work)",
                "Annuity (Qualified)" or "Inherited IRA" or "Traditional IRA" or "Rollover IRA" => "Pre-Tax(other)",
                "Inherited Roth IRA" or "Roth 401(k)" or "Roth IRA" or "HSA" => "Post-Tax",
                "Annuity (Non-Qualified)" or "Brokerage" or "Individual" or "Taxable" => "Taxable",
                "Refundable Deposit" => "Refundable Deposits",
                "Life Insurance" => "For Beneficiaries (POD)",
                "529" => "Education Savings",
                _ => "Other",
            };
        }
    }

    public double? AfterTaxPercentage { get; set; }

    public string TaxType2 { 
        get {
            return TaxType.StartsWith("Pre-Tax") ? "Pre-Tax" : TaxType;
        }
    }

    public int PortfolioReviewOrder {
        get {
            var ownerCategory = (Owner + 1) * 7; // 7=Joint, 14=First Person, 21= Second Person
            int order = AccountType switch
            {
                "Annuity (Non-Qualified)" or "Brokerage" or "Individual" or "Taxable" => 1 + Owner,
                "401(k)" or "403(b)" or "457(b)" or "457(b) Governmental" or "Roth 401(k)" or "SIMPLE IRA" or "Solo 401(k)" or "SEP IRA" => 3 + ownerCategory,
                "Annuity (Qualified)" or "Inherited IRA" or "Traditional IRA" or "Rollover IRA" => 4 + ownerCategory,
                "Inherited Roth IRA" or "Roth IRA" => 5 + ownerCategory,
                "HSA" => 6 + ownerCategory,
                _ => 7 + ownerCategory,
            };

            if (TaxType == "Pre-Tax(work)" && CurrentOrPrevious == "current") {
                order--;
            }

            return order;
        }
    }

    [JsonIgnore] // on 2/9/2024 - moved from bool to string.
    public bool CurrentEmployerRetirementFund {
        get {
            return false;
        }
        set {
            //transition value to new property.
            if (value)
            {
                CurrentOrPrevious = "current";
            }
            else
            {
                CurrentOrPrevious = "previous";
            }
        }
    }

    public string CurrentEmployerString {
        get {
            if (CurrentOrPrevious == "n/a" || CurrentOrPrevious == "") return "";
            else return CurrentOrPrevious;
        }
    }

    public string CurrentOrPrevious { get; set; } = "";

    public static readonly List<string> CurrentOrPreviousOptions = ["current", "previous"];
}
