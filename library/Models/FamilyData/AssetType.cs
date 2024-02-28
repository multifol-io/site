namespace Models;

public enum AssetTypes
{
    Unknown = 0,
    Bond,
    Bond_ETF,
    Bond_Fund,
    Cash,
    Cash_BankAccount,
    Cash_MoneyMarket,
    IBond,
    InternationalStock,
    InternationalStock_ETF,
    InternationalStock_Fund,
    InternationalBond,
    InternationalBond_ETF,
    InternationalBond_Fund,
    StocksAndBonds_ETF,
    StocksAndBonds_Fund,
    USStock,
    USStock_ETF,
    USStock_Fund,
    Stock, // move to *USStock
}