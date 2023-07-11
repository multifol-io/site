
public static class Advisor {
    public static List<string> Advise(Investment investment, Account account, IAppData appData) {
        List<string> adviceItems = new();
        switch (account.TaxType) {
            case "Pre-Tax":                
                switch (investment.AssetType) {
                    case AssetType.InternationalStock:
                    case AssetType.InternationalStock_ETF:
                    case AssetType.InternationalStock_Fund:
                        if (appData.ApplyTaxEfficientPlacementRules) {
                            adviceItems.Add("2nd: international->taxable");
                        }
                        break;
                }
                break;
            case "Taxable":
                switch (investment.AssetType) {
                    case AssetType.Bond:
                    case AssetType.Bond_ETF:
                    case AssetType.Bond_Fund:
                        if (appData.ApplyTaxEfficientPlacementRules) {
                            adviceItems.Add("1st: bonds->pre-tax");
                        }
                        break;
                    default:
                        break;
                }
                break;
            case "Post-Tax":
                switch (investment.AssetType) {
                    case AssetType.InternationalStock:
                    case AssetType.InternationalStock_ETF:
                    case AssetType.InternationalStock_Fund:
                        if (appData.ApplyTaxEfficientPlacementRules) {
                            adviceItems.Add("2nd: international->taxable");
                        }
                        break;                    
                    case AssetType.Bond:
                    case AssetType.Bond_ETF:
                    case AssetType.Bond_Fund:
                        if (appData.ApplyTaxEfficientPlacementRules) {
                            adviceItems.Add("1st: bonds->pre-tax");
                        }
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }

        return adviceItems;
    }
}