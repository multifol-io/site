public static class ImportPortfolioReview {
     public static FamilyData ParsePortfolioReview(string[] lines, IAppData appData, IList<Fund> funds) {
        double? portfolioSize = null;
        bool? assetParsing = null;
        Account? account = null;
        Investment? investment = null;
        FamilyData importedFamilyData = new(appData);
        string lastLine = null;
        bool afterPortfolioSize = false;
        foreach (var line in lines) {
            var tLine = line.ToLowerInvariant().Trim();
            try {
                if (!afterPortfolioSize && 
                    (
                        tLine.Contains("portfolio size") 
                        || tLine.Contains("total portfolio")
                        || tLine.Contains("retirement portfolio")
                        || tLine.Contains("retirement assets")
                        || tLine.Contains("current portfolio")
                    ) ) {
                    var colonLoc = tLine.IndexOf(":");
                    var dashLoc = tLine.IndexOf("-");
                    var equalsLoc = tLine.IndexOf("=");
                    var portfolioLoc = tLine.IndexOf("portfolio");
                    int useLoc;
                    if (colonLoc > portfolioLoc) {
                        useLoc = colonLoc;
                    } else if (equalsLoc > portfolioLoc) {
                        useLoc = equalsLoc;
                    } else if (dashLoc > portfolioLoc) {
                        useLoc = dashLoc;
                    } else {
                        useLoc = -1;
                    }

                    if (useLoc > -1) {
                        var valueStr = tLine[(useLoc+1)..tLine.Length].Trim();
                        var spaceLoc = valueStr.IndexOf(" ");
                        string? tSize = null;

                        if (spaceLoc > -1) {
                            tSize = valueStr[0..spaceLoc].Trim();
                        } else {
                            tSize = valueStr.Substring(0).Trim();
                        }

                        double multiplier = 1.0;
                        if (tSize != null) {
                            if (tSize.ToLowerInvariant().EndsWith("mm")) {
                                multiplier = 1000000.0;
                                tSize = tSize.Substring(0,tSize.Length-2);
                            } else if (tSize.ToLowerInvariant().EndsWith("m")) {
                                multiplier = 1000000.0;
                                tSize = tSize.Substring(0,tSize.Length-1);
                            } else if (tSize.ToLowerInvariant().EndsWith("k")) {
                                multiplier = 1000.0;
                                tSize = tSize.Substring(0,tSize.Length-1);
                            }

                            if (tSize.StartsWith("~")) {
                                tSize = tSize.Substring(1);
                            }

                            portfolioSize = FormatUtilities.ParseDouble(tSize, allowCurrency:true) * multiplier;
                            afterPortfolioSize = true;
                        }
                    }
                } else if (tLine.ToLowerInvariant().StartsWith("current retirement assets")) {
                    assetParsing = true;
                } else if (tLine.ToLowerInvariant().Contains("contributions")) {
                    assetParsing = false;
                } else if (tLine.ToLowerInvariant().Contains("questions")) {
                    assetParsing = false;
                } else if (string.IsNullOrEmpty(tLine)) {
                    account = null;
                } else if (assetParsing.HasValue && assetParsing.Value && StartsWithNumber(tLine) && account != null) {
                    try {
                        investment = ParseInvestmentLine(line, portfolioSize ?? 100.0, funds);
                        account?.Investments.Add(investment);
                        assetParsing = true;
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                }  else if (assetParsing.HasValue && assetParsing.Value && account == null) {
                    account = ParseAccountLine(line);
                    importedFamilyData.Accounts.Add(account);
                    assetParsing = true;                    
                }  else if (assetParsing.HasValue && assetParsing.Value && !StartsWithNumber(tLine)) {
                    account = ParseAccountLine(line);
                    importedFamilyData.Accounts.Add(account);
                    assetParsing = true;
                } else if (!assetParsing.HasValue && afterPortfolioSize && StartsWithNumber(tLine)) {
                    account = ParseAccountLine(lastLine);
                    try {
                        investment = ParseInvestmentLine(line, portfolioSize ?? 100.0, funds);
                        account?.Investments.Add(investment);
                        assetParsing = true;
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                }
            } catch (Exception e) {
                Console.WriteLine("Error processing line: " + tLine + "\n" + e.ToString() + "\n");
                importedFamilyData.Title = "Error processing line: " + tLine + "\n" + e.ToString() + "\n";
            }
            
            lastLine = line;
        }

        return importedFamilyData;
    }

    private static Account ParseAccountLine(string line) {
        Account account = null;
        string? custodian = null;
        int atIndex = line.IndexOf(" at ");
        int atEndIndex = atIndex + " at ".Length;
        
        int leftParenIndex = line.IndexOf("(");
        if (leftParenIndex == -1 || leftParenIndex < atIndex) { 
            leftParenIndex = line.Length;
        }

        if (atIndex == -1) {
            if (leftParenIndex > -1) {
                atIndex = leftParenIndex;
            } else {
                atIndex = line.Length;
            }
        }
        var accountType = line.Substring(0, atIndex).Trim();
        if (atIndex != line.Length) {
            custodian = line.Substring(atEndIndex, leftParenIndex - atEndIndex).Trim();
            //TODO: parse prefix
        }
        account = new Account() { Custodian = custodian, AccountType = accountType, Import = true };
        return account;
    }

    private static bool StartsWithNumber(string line) {
        return line.Length > 0 ? (Char.IsDigit(line[0]) || line[0] == '.') : false;
    }

    // right: 35% ProShares UltraPro S&P500 (UPRO) (.91%)
    // bad1: 5% VTCLX Vanguard Tax-Managed Capital Appreciation .09er
    // bad2: 10% (VTIVX-Vanguard TR 2045 Fund) (0.08)
    private static Investment ParseInvestmentLine(string line, double portfolioSize, IList<Fund> funds) {
        line = line.Trim();
        double percentOfPortfolio;
        int percentIndex = line.IndexOf("%");
        int firstSpaceLoc = line.IndexOf(" ");

        if (percentIndex > -1 && percentIndex < firstSpaceLoc) {
            percentOfPortfolio = double.Parse(line.Substring(0,percentIndex));
        } else if (firstSpaceLoc > -1) {
            percentIndex = firstSpaceLoc;
            percentOfPortfolio = double.Parse(line.Substring(0,firstSpaceLoc));
        } else {
            percentOfPortfolio = double.Parse(line.Substring(0));
        }

        int afterLeftParenIndex = line.IndexOf("(") + 1;
        int rightParenIndex = line.IndexOf(")");
        int afterLeftParenIndex2 = line.IndexOf("(",rightParenIndex+1 ) + 1;
        int rightParenIndex2 = line.IndexOf(")",rightParenIndex+1);
        string? investmentName = null;
        string? ticker = null;
        double? expRatio = null;

        if (afterLeftParenIndex > 0) {
            investmentName = line.Substring(percentIndex + 1, afterLeftParenIndex - 1 - (percentIndex + 1)).Trim();

            try {
                ticker = line.Substring(afterLeftParenIndex, rightParenIndex - afterLeftParenIndex).ToUpperInvariant();
                if (ticker.Length > 9) {
                    if (ticker.IndexOf(" ") < 0) {
                        ticker = ticker.Substring(0,8);
                    } else {
                        ticker = null;
                    }
                }
            } catch (Exception) {
            }

            try {
                var erRatioString = line.Substring(afterLeftParenIndex2, rightParenIndex2 - afterLeftParenIndex2);
                erRatioString = erRatioString.Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.PercentSymbol, "");
                expRatio = double.Parse(erRatioString);
            } catch (Exception) {
            }
        } else {
            investmentName = line.Substring(percentIndex + 1);
        }

        AssetType assetType = AssetType.Unknown;
        if (!string.IsNullOrEmpty(ticker)) {
            foreach (var fund in funds) {
                if (fund.Ticker == ticker) {
                    assetType = fund.AssetType ?? AssetType.Unknown;
                }
            }
        }

        return new Investment() { Name = investmentName, Ticker = ticker, ExpenseRatio = expRatio, ValuePIN = portfolioSize * percentOfPortfolio / 100, AssetType = assetType };
    }
}