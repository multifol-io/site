namespace Models;

public static class Advisor {
    public static List<string> Advise(Investment investment, Account account, IAppData appData) {
        List<string> adviceItems = [];
        switch (account.TaxType) {
            case "Pre-Tax(work)":                
            case "Pre-Tax(other)":                
                switch (investment.AssetType) {
                    case AssetTypes.InternationalStock:
                    case AssetTypes.InternationalStock_ETF:
                    case AssetTypes.InternationalStock_Fund:
                        if (appData.ApplyTaxEfficientPlacementRules) {
                            adviceItems.Add("2nd: international->taxable");
                        }
                        break;
                }
                break;
            case "Taxable":
                switch (investment.AssetType) {
                    case AssetTypes.Bond:
                    case AssetTypes.Bond_ETF:
                    case AssetTypes.Bond_Fund:
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
                    case AssetTypes.InternationalStock:
                    case AssetTypes.InternationalStock_ETF:
                    case AssetTypes.InternationalStock_Fund:
                        if (appData.ApplyTaxEfficientPlacementRules) {
                            adviceItems.Add("2nd: international->taxable");
                        }
                        break;                    
                    case AssetTypes.IBond:
                    case AssetTypes.Bond:
                    case AssetTypes.Bond_ETF:
                    case AssetTypes.Bond_Fund:
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