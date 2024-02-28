public static class ImportPortfolioReview {
     public static (FamilyData,List<Link>) ParsePortfolioReview(string[] lines, bool debug, IAppData appData, IList<Fund> funds) {
        double? portfolioSize = null;
        bool? assetParsing = null;
        bool afterAge = false;
        Account? account = null;
        Investment? investment;
        FamilyData importedFamilyData = new(appData);
        string? lastLine = null;
        bool afterPortfolioSize = false;
        List<Link> Links = new();
        foreach (var line in lines) {
            var tLine = line.ToLowerInvariant().Trim();
            try {
                importedFamilyData.Title += "\n" + tLine;
                if (tLine.Contains("current retirement assets")
                    || tLine.Contains("current retirement investment assets")) {
                    importedFamilyData.Title += "|1";
                    assetParsing = true;
                } else if (afterAge && !afterPortfolioSize && 
                    (
                        tLine.Contains("portfolio size") 
                        || tLine.Contains("total portfolio")
                        || tLine.Contains("your portfolio")
                        || tLine.Contains("retirement portfolio")
                        || tLine.Contains("retirement assets")
                        || tLine.Contains("total invested assets")
                        || tLine.Contains("total investments")
                        || tLine.Contains("current portfolio")
                        || tLine.Contains("portfolio:")
                    ) ) {
                    importedFamilyData.Title += "|5";

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
                    } else if (portfolioLoc > -1) {
                        useLoc = portfolioLoc + "portfolio".Length;
                        if (useLoc > tLine.Length - 1) {
                            useLoc = -1;
                        }
                    } else {
                        useLoc = -1;
                    }

                    importedFamilyData.Title += $"|5.1 {useLoc}";

                    if (useLoc > -1) {
                        var valueStr = tLine[(useLoc+1)..tLine.Length].Trim();
                        var spaceLoc = valueStr.IndexOf(" ");
                        string? tSize = null;

                        importedFamilyData.Title += $"|5.2 {spaceLoc}";

                        if (spaceLoc > -1) {
                            tSize = valueStr[0..spaceLoc].Trim();
                        } else {
                            tSize = valueStr.Substring(0).Trim();
                        }

                        if (tSize != null && (tSize == "upper" || tSize == "high" ||tSize == "lower" || tSize == "mid" || tSize == "low"))
                        {
                            var words = valueStr.Split(' ','-');
                            double multiplier = 0.0;
                            int wordIndex = 0;
                            switch (words[wordIndex].ToLowerInvariant()) {
                                case "low":
                                case "lower":
                                    if (words[wordIndex+1] == "to" && words[wordIndex+2] == "mid") {
                                        multiplier = 3.75;
                                        wordIndex += 2;
                                    } else {
                                        multiplier = 2.5;
                                    }
                                    break;
                                case "mid":
                                    multiplier = 5;
                                    break;
                                case "high":
                                case "upper":
                                    multiplier = 7.5;
                                    break;
                            }

                            wordIndex++;
                            switch (words[wordIndex].ToLowerInvariant()) {
                                case "5":
                                case "five":
                                    multiplier *= 10000;
                                    break;
                                case "6":
                                case "six":
                                    multiplier *= 100000;
                                    break;
                                case "7":
                                case "seven":
                                    multiplier *= 1000000;
                                    break;
                                case "8":
                                case "eight":
                                    multiplier *= 10000000;
                                    break;
                            }

                            portfolioSize = multiplier;
                        } else if (tSize != null) {
                            var nextSpaceLoc = valueStr.IndexOf(" ", spaceLoc+1);
                            if (nextSpaceLoc == -1) {
                                nextSpaceLoc = valueStr.Length;
                            }

                            var nextWord = valueStr[(spaceLoc+1)..nextSpaceLoc]?.ToLowerInvariant();
                            if (nextWord != null) {
                                var nextWordLen = nextWord.Length;
                                if (nextWordLen >= 1 && !char.IsAsciiLetter(nextWord[nextWordLen - 1])) {
                                    nextWord = nextWord[..^1];
                                }
                            }

                            switch (nextWord) {
                                case "m":
                                case "mm":
                                case "million":
                                case "millions":
                                    tSize += "m";
                                    break;
                                case "k":
                                case "thousand":
                                case "thousands":
                                    tSize += "k";
                                    break;
                            }
                        }

                        if (portfolioSize == null) {
                            importedFamilyData.Title += "  tsize: " + tSize;

                            double multiplier = 1.0;
                            if (tSize != null) {
                                if (tSize.ToLowerInvariant().EndsWith("mm")) {
                                    multiplier = 1000000.0;
                                    tSize = tSize[..^2];
                                } else if (tSize.ToLowerInvariant().EndsWith("m")) {
                                    multiplier = 1000000.0;
                                    tSize = tSize[..^1];
                                } else if (tSize.ToLowerInvariant().EndsWith("k")) {
                                    multiplier = 1000.0;
                                    tSize = tSize[..^1];
                                }

                                if (tSize.StartsWith("~")) {
                                    tSize = tSize.Substring(1);
                                }

                                if (tSize.Length > 1 && tSize.Substring(tSize.Length - 1) == ",") {
                                    tSize = tSize[..^2];
                                }

                                portfolioSize = FormatUtilities.ParseDouble(tSize, allowCurrency:true) * multiplier;
                                importedFamilyData.Title += "  size: " + portfolioSize;
                                afterPortfolioSize = portfolioSize != 0.0;
                                importedFamilyData.Title += $"|13.4";
                            }
                        }
                    }
                } else if (importedFamilyData.People[0].Age == null && (tLine.StartsWith("age") || tLine.Contains(" age:") || tLine.StartsWith("my age") || tLine.StartsWith("me:"))) {
                    afterAge = true;
                    importedFamilyData.Title += "|6";
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
                        var okAge = int.TryParse(valueStr, out int age1);
                        if (okAge) {
                            importedFamilyData.People[0].Age = age1;
                            importedFamilyData.Title += "|6.1";
                            //TODO: 2nd age...
                        } else {
                            string number = "";
                            int personIndex = 0;
                            int charIndex = 0;

                            foreach (var c in valueStr.ToCharArray()) {
                                charIndex++;
                                bool isDigit = char.IsDigit(c);
                                if (isDigit) {
                                    number += c;
                                }

                                if (!isDigit || charIndex == valueStr.Length) {
                                    if (number != "") {
                                        var age = int.Parse(number);
                                        if (personIndex < 2) {
                                            importedFamilyData.People[personIndex].Age = age;
                                            importedFamilyData.Title += $"|6.2:{age}";
                                        }

                                        personIndex++;
                                        number = "";
                                    }
                                }
                            }
                        }
                    }
                } else if (tLine.StartsWith("contributions") 
                    || tLine.StartsWith("new annual contributions")
                    || tLine.StartsWith("annual contributions")
                    || tLine.EndsWith("annual contributions")
                    || tLine.StartsWith("annual retirement contributions")) {
                    importedFamilyData.Title += "|2";
                    assetParsing = false;
                } else if (tLine.StartsWith("available funds")) {
                    importedFamilyData.Title += "|2.1";
                    assetParsing = false;                    
                } else if (tLine.StartsWith("questions")) {
                    importedFamilyData.Title += "|3";
                    assetParsing = false;
                } else if (string.IsNullOrEmpty(tLine)) {
                    if (account != null && account.Investments.Count > 0) {
                        importedFamilyData.Title += "|11";
                        account = null;
                    }
                } else if (assetParsing.HasValue && assetParsing.Value && StartsWithNumber(tLine) && account != null) {
                    importedFamilyData.Title += "|4";
                    try {
                        investment = ParseInvestmentLine(line, portfolioSize ?? 100.0, debug:debug, funds, importedFamilyData);
                        account?.Investments.Add(investment);
                        assetParsing = true;
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                }  else if (assetParsing.HasValue && assetParsing.Value && account == null) {
                    importedFamilyData.Title += "|7";
                    account = ParseAccountLine(line, importedFamilyData);
                    importedFamilyData.Accounts.Add(account);
                    assetParsing = true;                    
                }  else if (assetParsing.HasValue && assetParsing.Value && !StartsWithNumber(tLine)) {
                    // importedFamilyData.Title += "|8";
                    // account = ParseAccountLine(line, importedFamilyData);
                    // importedFamilyData.Accounts.Add(account);
                    // assetParsing = true;
                } else if (!assetParsing.HasValue && afterPortfolioSize && StartsWithNumber(tLine)) {
                    importedFamilyData.Title += "|9";
                    account = ParseAccountLine(lastLine!, importedFamilyData);
                    if (account != null)
                    {
                        try
                        {
                            investment = ParseInvestmentLine(line, portfolioSize ?? 100.0, debug: debug, funds, importedFamilyData);
                            account.Investments.Add(investment);
                            importedFamilyData.Accounts.Add(account);
                            assetParsing = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                } else {
                    importedFamilyData.Title += "|10";
                }
            } catch (Exception e) {
                Console.WriteLine("Error processing line: " + tLine + "\n" + e.ToString() + "\n");
                importedFamilyData.Title += "Error processing line: " + tLine + "\n" + e.ToString() + "\n";
            }
            
            if (!string.IsNullOrEmpty(tLine)) {
                lastLine = line;
            }
        }

        if (importedFamilyData.PercentTotal > 150) {
            foreach (var account2 in importedFamilyData.Accounts) {
                foreach (var investment2 in account2.Investments) {
                    investment2.Value = investment2.Value / importedFamilyData.PercentTotal * 100.0;
                }
            }
        }

        return (importedFamilyData, Links);
    }

    private static Account ParseAccountLine(string line, FamilyData importedFamilyData) {
        importedFamilyData.Title += "  acc: " + line ;

        Account? account;
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
        var accountType = line[..atIndex].Trim();
        if (atIndex != line.Length && (leftParenIndex > atEndIndex)) {
            custodian = line[atEndIndex..leftParenIndex].Trim();
            //TODO: parse prefix
        }
        account = new Account() { Custodian = custodian, AccountType = accountType, Import = true };
        return account;
    }

    private static bool StartsWithNumber(string line) {
        return line.Length > 1 && 
            (Char.IsDigit(line[0]) 
                || line[0] == '.' 
                || line[0] == '$' 
                || (line[0] == '(')
            );
    }

    // right: 35% ProShares UltraPro S&P500 (UPRO) (.91%)
    // bad1: 5% VTCLX Vanguard Tax-Managed Capital Appreciation .09er
    // bad2: 10% (VTIVX-Vanguard TR 2045 Fund) (0.08)
    private static Investment ParseInvestmentLine(string line, double portfolioSize, bool debug, IList<Fund> funds, FamilyData importedFamilyData) {
        line = line.Trim();
        if (line.Length > 1 && line[0] == '(') {
            line = line[1..];
        }

        double? percentOfPortfolio = null;
        int percentIndex = line.IndexOf("%");
        int firstSpaceLoc = line.IndexOf(" ");
        int firstTabLoc = line.IndexOf('\t');

        try {
            importedFamilyData.Title += $"|20:{percentIndex},{firstSpaceLoc}";
            
            if (percentIndex > -1 && percentIndex < firstSpaceLoc) {
                importedFamilyData.Title += "|13";
                percentOfPortfolio = FormatUtilities.ParseDouble(line[..percentIndex], allowCurrency:true);
            } else if (percentIndex > -1 && percentIndex < firstTabLoc) {
                importedFamilyData.Title += "|13.5";
                percentOfPortfolio = FormatUtilities.ParseDouble(line[..percentIndex], allowCurrency:true);
            } else if (firstSpaceLoc > -1) {
                importedFamilyData.Title += "|14";
                percentIndex = firstSpaceLoc;
                percentOfPortfolio = FormatUtilities.ParseDouble(line[..firstSpaceLoc], allowCurrency:true);
                if (percentOfPortfolio > 100) {
                    percentOfPortfolio = null;
                }
            }
        } catch (Exception) {
            importedFamilyData.Title += "|16";
            percentOfPortfolio = null;
        }

        if (portfolioSize != 0.0 && line.StartsWith("$")) {
            percentOfPortfolio = percentOfPortfolio! / portfolioSize * 100.0;
        }

        importedFamilyData.Title += $"|12 ({percentOfPortfolio}%)";
        int afterLeftParenIndex = line.IndexOf("(") + 1;
        int rightParenIndex = line.IndexOf(")");
        int afterLeftParenIndex2 = line.IndexOf("(",rightParenIndex+1 ) + 1;
        int rightParenIndex2 = line.IndexOf(")",rightParenIndex+1);
        string? ticker = null;
        double? expRatio = null;

        string? investmentName = null;
        if (afterLeftParenIndex > 0)
        {
            if (percentIndex < afterLeftParenIndex) {
                investmentName = line[(percentIndex + 1)..(afterLeftParenIndex - 1)].Trim();
            }

            try
            {
                ticker = line[afterLeftParenIndex..rightParenIndex].ToUpperInvariant();
                if (ticker.Length > 9)
                {
                    if (ticker.IndexOf(" ") < 0)
                    {
                        ticker = ticker[..8];
                    }
                    else
                    {
                        ticker = null;
                    }
                }
            }
            catch (Exception)
            {
            }

            try
            {
                var erRatioString = line[afterLeftParenIndex2..rightParenIndex2];
                erRatioString = erRatioString.Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.PercentSymbol, "");
                expRatio = double.Parse(erRatioString);
            }
            catch (Exception)
            {
            }
        }
        else
        {
            investmentName = line[(percentIndex + 1)..];
        }

        AssetType assetType = AssetType.Unknown;
        if (!string.IsNullOrEmpty(ticker)) {
            foreach (var fund in funds) {
                if (fund.Ticker == ticker) {
                    assetType = fund.AssetType ?? AssetType.Unknown;
                }
            }
        }

        importedFamilyData.PercentTotal += percentOfPortfolio ?? 0.0;  //keep running total to see when people use 100% per account
        return new Investment() { Name = investmentName, Ticker = ticker, ExpenseRatio = expRatio, Value = portfolioSize * percentOfPortfolio / 100, AssetType = assetType };
    }
}