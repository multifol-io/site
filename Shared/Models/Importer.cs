using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections;
using System;

public class Importer {
    public static async Task<ImportResult> ImportDataFiles(IReadOnlyList<IBrowserFile> files, IList<Fund> funds, List<Account> existingAccounts, int? PIN)
    {
        ImportResult result = new();

        if (files != null)
        {
            foreach (var file in files)
            {
                if (file.Name.ToLower().EndsWith(".csv"))
                {
                    try {
                        var importedAccountsCSV = await Importer.ImportCSV(file, funds, PIN);
                        result.ImportedAccounts.AddRange(importedAccountsCSV);
                        result.DataFilesImported++;
                    } catch (Exception e) {
                        result.Errors.Add(new ImportError() { Exception = e });
                    }
                } else if (file.Name.ToLower().EndsWith(".xlsx")) {
                    var buffer = new byte[file.Size];
                    using (var stream = file.OpenReadStream())
                    {
                        try {
                            var importedAccountsXLSX = await Importer.ImportXLSX(stream, funds, PIN);
                            result.ImportedAccounts.AddRange(importedAccountsXLSX);
                            result.DataFilesImported++;
                        } catch (Exception e) {
                            result.Errors.Add(new ImportError() { Exception = e });
                        }
                    }
                }
            }
        }

        foreach (var importedAccount in result.ImportedAccounts) 
        {
            bool foundMatch = false;
            foreach (var existingAccount in existingAccounts)
            {
                if (existingAccount.Note == importedAccount.Note && existingAccount.Custodian == importedAccount.Custodian)
                {
                    result.UpdatedAccounts.Add(importedAccount);
                    importedAccount.ReplaceAccount = existingAccount;
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                result.NewAccounts.Add(importedAccount);
            }
        }

        result.ImportedAccounts.Clear();

        return result;
    }
    
    public static async Task<List<Account>> ImportCSV(IBrowserFile file, IList<Fund> funds, int? PIN)
    {
        try {
            var stream = file.OpenReadStream();
            using var reader = new CsvReader(stream);
            var RowEnumerator = reader.GetRowEnumerator().GetAsyncEnumerator();
            await RowEnumerator.MoveNextAsync();
            string[] headerChunks = RowEnumerator.Current;
            int headerChunksLen = headerChunks.Length;
            if (headerChunksLen > 1 && headerChunks[0] == "COB Date" && headerChunks[1] == "Security #")
            {
                return await ImportMerrillEdge(RowEnumerator, funds, PIN);
            }
            else if (headerChunksLen > 1 && headerChunks[0].StartsWith("Account Number") && headerChunks[1] == "Account Name")
            {
                return await ImportFidelity(RowEnumerator, funds, PIN);
            }
            else if (headerChunksLen > 1 && headerChunks[0] == "Account Number" && headerChunks[1] == "Investment Name")
            {
                return await ImportVanguard(RowEnumerator, funds, PIN);
            }
            else if (headerChunksLen > 1 && headerChunks[0] == "Fund Account Number" && headerChunks[1] == "Fund Name")
            {
                return await ImportVanguardOrig(RowEnumerator, funds, PIN);
            }
            else if (headerChunks[0].StartsWith("Account Summary"))
            { // sometimes headerChunksLen == 1, sometimes more.
                return await ImportETrade(RowEnumerator, funds, PIN);
            }
            else if (headerChunks[0].StartsWith("Positions for "))
            {
                return await ImportSchwab(RowEnumerator, funds, PIN);
            }
            else if (headerChunksLen > 1 && headerChunks[0] == "Account Type" && headerChunks[1] == "Account Name" && headerChunks[2] == "Ticker")
            {
                return await ImportTRowePrice(RowEnumerator, funds, PIN);
            }
            else if (headerChunks[0] == "AMERIPRISE BROKERAGE")
            {
                return await ImportAmeriprise(RowEnumerator, funds, PIN);
            }
            else
            {
                throw new InvalidDataException("Importing this CSV file failed. We currently support importing CSV files from Ameriprise, eTrade, Fidelity, Merrill Edge, Schwab, or Vanguard");
            }
        } 
        catch (Exception e) {
            if (e is InvalidDataException) 
            {
                throw;
            }
            else 
            {
                throw new InvalidDataException("Importing this CSV file failed, even though we think it should have worked (we think it was from Ameriprise, eTrade, Fidelity, Merrill Edge, Schwab, or Vanguard).", e);
            }
        }
    }

    private static async Task<List<Account>> ImportMerrillEdge(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        Dictionary<string,Account> accountLookup = new();

        List<Account> importedAccounts = new();
        string[] chunks;        
        await rowEnumerator.MoveNextAsync();
        chunks = rowEnumerator.Current;

        while (chunks != null && chunks.Length > 0 && chunks[0] != "")
        {
            var accountNum = FormatUtilities.TrimQuotes(chunks[7]);
            var accountRegistration = FormatUtilities.TrimQuotes(chunks[6]);
            var accountNickname = FormatUtilities.TrimQuotes(chunks[5]);
            var symbol = FormatUtilities.TrimQuotes(chunks[2]);
            string? investmentName = FormatUtilities.TrimQuotes(chunks[4]);
            double shares = FormatUtilities.ParseDouble(chunks[8]);
            double value = FormatUtilities.ParseDouble(chunks[10] ?? "0.0", allowCurrency:true);
            double costBasis = value - FormatUtilities.ParseDouble(chunks[11] ?? "0.0", allowCurrency:true);

            accountLookup.TryGetValue(accountNum, out Account? newAccount);
            if (newAccount == null) {
                newAccount = new(PIN) {
                    Custodian = "Merrill Edge",
                    Note = "*" + accountNum.Substring(accountNum.Length - 4) + " " + accountNickname + " " + accountRegistration
                };
                importedAccounts.Add(newAccount);
                accountLookup.Add(accountNum, newAccount);
            }
            
            Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null), Value = value, SharesPIN = shares, CostBasis = costBasis };
            newAccount?.Investments.Add(newInvestment);

            await rowEnumerator.MoveNextAsync();
            chunks = rowEnumerator.Current;
        }

        return importedAccounts;
    }

    private static async Task<List<Account>> ImportFidelity(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();
        await rowEnumerator.MoveNextAsync();
        string[] chunks = rowEnumerator.Current;
        // FIDELITY - blank line ends investment listings or ",,,,,,,,,,,,,,,"
        // Line 0: Account Number,Account Name,Symbol,Description,Quantity,Last Price,Last Price Change,Current Value
        string lastAccountNumber = "";
        Account? newAccount = null;
        while (chunks != null && chunks.Length != 0) {
            try {
                var accountNumber = chunks[0];
                var accountName = chunks[1];
                var symbol = chunks[2];

                string? investmentName;
                double value;
                double? price = null;
                double? shares = null;
                double? costBasis = null;
                double? expenseRatio = null;

                if (symbol == "Pending Activity")
                {
                    investmentName = symbol;
                    value = FormatUtilities.ParseDouble(chunks[6], allowCurrency:true);
                    symbol = "PENDING";
                    expenseRatio = 0.0;
                } else {
                    investmentName = chunks[3];
                    value = FormatUtilities.ParseDouble(chunks[7], allowCurrency:true);
                    price = FormatUtilities.ParseDouble(chunks[5], allowCurrency:true);
                    shares = FormatUtilities.ParseDouble(chunks[4]);
                    costBasis = FormatUtilities.ParseDouble(chunks[13], allowCurrency:true);
                }

                // many tickers have "**" at end, signifying money market fund.
                if (symbol.Length >= 2 && symbol.Substring(symbol.Length-2) == "**") { 
                    symbol = symbol.Substring(0, symbol.Length - 2);
                    shares = value;
                    price = 1.0;
                }

                if (lastAccountNumber != accountNumber)
                {
                    newAccount = new(PIN) {
                        Custodian = "Fidelity",
                        Note = string.Concat("*", accountNumber.AsSpan(accountNumber.Length-4,4))
                    };
                    newAccount.GuessAccountType();
                    importedAccounts.Add(newAccount);
                    lastAccountNumber = accountNumber;
                }

                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = investmentName, Price = price, Value = value, SharesPIN = shares, CostBasis = costBasis };
                if (expenseRatio != null)
                {
                    newInvestment.ExpenseRatio = expenseRatio;
                }

                if (newInvestment.Ticker == "PENDING" || newInvestment.Ticker == "FCASH") {
                    newInvestment.AssetType = global::AssetType.Cash;
                }

                newAccount?.Investments.Add(newInvestment);
            } 
            catch (Exception e)
            {
                string? line = null;
                foreach (var chunk in chunks) {
                    line += (line == null ? "," : "" )+ chunk.ToString();
                } 
                Console.WriteLine("skipped line due to error: " + line);
                Console.WriteLine(e.GetType().Name + " " + e.Message + " " + e.StackTrace);
            }

            await rowEnumerator.MoveNextAsync();
            chunks = rowEnumerator.Current;
        }

        return importedAccounts;
    } 

    private static async Task<List<Account>> ImportVanguard(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();
        string[] chunks;        
        // VANGUARD - blank lines seperates accounts. two blank lines end investment listing.
        // Line 0: Account Number,Investment Name,Symbol,Shares,Share Price,Total Value,
        
        Account? newAccount = null;
        while (await rowEnumerator.MoveNextAsync()) {
            chunks = rowEnumerator.Current;

            if (chunks.Length == 1) {
                if (newAccount == null) {
                    break;
                } else {
                    newAccount = null;
                }
            } else if (chunks.Length > 1) {
                var accountNumber = chunks[0];
                var symbol = chunks[2];
                var investmentName = chunks[1];
                double shares = FormatUtilities.ParseDouble(chunks[3]);
                double price = FormatUtilities.ParseDouble(chunks[4], allowCurrency:true);
                double value = FormatUtilities.ParseDouble(chunks[5], allowCurrency:true);
                // costBasis not available in CSV
                                
                if (newAccount == null) {
                    newAccount = new(PIN) {
                        Custodian = "Vanguard",
                        Note = string.Concat("*", accountNumber.AsSpan(accountNumber.Length-4,4))
                    };
                    importedAccounts.Add(newAccount);
                }

                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = investmentName, Price = price, Value = value, SharesPIN = shares };
                newAccount?.Investments.Add(newInvestment);
            }
        }

        newAccount = null;
        while (await rowEnumerator.MoveNextAsync()) {
            chunks = rowEnumerator.Current;
            if (chunks.Length == 7)
            {
                    await rowEnumerator.MoveNextAsync();
                    break;
            }
        }

        // handle the separate PLAN section if this Vanguard account has a 401(k) plan in the CSV file.
        chunks = rowEnumerator.Current;
        while (chunks.Length == 7)
        {
            var planName = chunks[1];
            var investmentName = chunks[2];
            double shares = FormatUtilities.ParseDouble(chunks[3]);
            double price = FormatUtilities.ParseDouble(chunks[4], allowCurrency:true);
            double value = FormatUtilities.ParseDouble(chunks[5], allowCurrency:true);
                            
            if (newAccount == null) {
                newAccount = new(PIN) {
                    Custodian = "Vanguard",
                    Note = planName
                };
                importedAccounts.Add(newAccount);
            }

            Investment newInvestment = new (PIN) { funds = funds, Name = investmentName, Price = price, Value = value, SharesPIN = shares };
            newAccount?.Investments.Add(newInvestment);

            await rowEnumerator.MoveNextAsync();
            chunks = rowEnumerator.Current;
        }

        return importedAccounts;
    }
                    
    private static async Task<List<Account>> ImportVanguardOrig(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();
        string[] chunks;        
                        
        // VANGUARD - orig mutual fund account style - two blank lines end investment listing.
        // Line 0: Fund Account Number,Fund Name,Price,Shares,Total Value,
        Dictionary<string,Account> accountLookup = new();

        int consecutiveBlanks = 0;
        while (await rowEnumerator.MoveNextAsync()) {
            chunks = rowEnumerator.Current;

            if (chunks.Length == 1) {
                consecutiveBlanks++;
                if (consecutiveBlanks == 2) {
                    break;
                }
            } else {
                consecutiveBlanks = 0;
                var fundAndAccountNum = FormatUtilities.TrimQuotes(chunks[0]).Split('-');
                var fundNumber = fundAndAccountNum[0];
                var accountNum = fundAndAccountNum[1];
                string? symbol = null;
                string? investmentName = null;
                foreach (var fund in funds)
                {
                    if (fundNumber == fund.VanguardFundId) 
                    {
                        symbol = fund.Ticker;
                        investmentName = fund.LongName;
                        break;
                    }
                }

                double shares = FormatUtilities.ParseDouble(chunks[3]);
                double value = FormatUtilities.ParseDouble(chunks[4], allowCurrency:true);
                // costBasis not available in CSV

                accountLookup.TryGetValue(accountNum, out Account? newAccount);
                if (newAccount == null)
                {
                    newAccount = new(PIN) {
                        Custodian = "Vanguard",
                        Note = string.Concat("*", accountNum.AsSpan(accountNum.Length - 4))
                    };
                    importedAccounts.Add(newAccount);
                    accountLookup.Add(accountNum, newAccount);
                }
                                        
                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = (investmentName ?? null) , Value = value, SharesPIN = shares };
                newAccount?.Investments.Add(newInvestment);
            }
        }

        return importedAccounts;
    }

    private static async Task<List<Account>> ImportETrade(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();
        string[] chunks;        
        await rowEnumerator.MoveNextAsync();// account header line
        await rowEnumerator.MoveNextAsync();// accountInfo
        chunks = rowEnumerator.Current;
                    
        Account newAccount = new(PIN) {
                Custodian = "ETrade",
                Note = FormatUtilities.TrimQuotes(chunks[0])
            };
        importedAccounts.Add(newAccount);

        bool foundHeader = false;
        while (!foundHeader && await rowEnumerator.MoveNextAsync()) {
            chunks = rowEnumerator.Current;
            if (chunks.Length == 10 && chunks[0] == "Symbol" && chunks[1] == "Last Price $")
            {
                foundHeader = true;
            }
        }
        
        while (await rowEnumerator.MoveNextAsync()) {
            chunks = rowEnumerator.Current;
            
            AssetType? assetType = null;
            var symbol = chunks[0];
            string? investmentName = null;

            if (chunks.Length > 1)
            {
                double shares = FormatUtilities.ParseDouble(chunks[4]);
                double value = FormatUtilities.ParseDouble(chunks[9], allowCurrency:true);
                double price = FormatUtilities.ParseDouble(chunks[1], allowCurrency:true);
                double costBasis = value - FormatUtilities.ParseDouble(chunks[7], allowCurrency:true);

                if (symbol == "CASH") {
                    shares = value;
                    assetType = AssetType.Cash_MoneyMarket;
                    price = 1.0;
                } else if (symbol == "TOTAL") {
                    break;
                }

                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Price = price, Name = (investmentName != null ? investmentName : null), Value = value, SharesPIN = shares, CostBasis = costBasis };
                if (assetType != null) {
                    newInvestment.AssetType = assetType;
                }
                newAccount?.Investments.Add(newInvestment);
            }
        }

        return importedAccounts;
    }

    private static async Task<List<Account>> ImportSchwab(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();
        await rowEnumerator.MoveNextAsync(); // goto empty chunks

        while (await rowEnumerator.MoveNextAsync()) { // go to account name row
            string[] chunks = rowEnumerator.Current;

            Account? newAccount = new(PIN) {
                Custodian = "Schwab",
                Note = chunks[0]
            };
            importedAccounts.Add(newAccount);
            
            await rowEnumerator.MoveNextAsync(); // go to investment headers line
            while (await rowEnumerator.MoveNextAsync()) { // go to investment line
                chunks = rowEnumerator.Current;

                // SCHWAB - line with blank cells divides accounts
                // Line 0: "Positions for CUSTACCS as of 09:13 PM ET, 02/04/2023","","","","","","","","","","","","","","","",""
                // Line 1: "","","","","","","","","","","","","","","","",""
                // Line 2: "Individual ...###","","","","","","","","","","","","","","","",""
                // Line 3: "Symbol","Description","Quantity","Price","Price Change %","Price Change $","Market Value","Day Change %","Day Change $","Cost Basis","Gain/Loss %","Gain/Loss $","Ratings","Reinvest Dividends?","Capital Gains?","% Of Account","Security Type"
                // 0-n lines: investment info
                // End of Account: "Cash & Cash Investments","--","--","--","--","--","$5.20","0%","$0.00","--","--","--","--","--","--","0.29%","Cash and Money Market"
                // Totals of Account: "Account Total","--","--","--","--","--","$51,767.73","-1.77%","-$31.93","$547.79","33.1%","$181.34","--","--","--","--","--"
                
                var symbol = chunks[0];
                if (symbol == "Account Total") {
                    await rowEnumerator.MoveNextAsync();
                    break;
                }

                string? investmentName = null;
                double shares = FormatUtilities.ParseDouble(chunks[2]);
                double value = FormatUtilities.ParseDouble(chunks[6], allowCurrency:true);
                double costBasis = FormatUtilities.ParseDouble(chunks[9], allowCurrency:true);
                AssetType? assetType = null;
                switch (symbol) {
                    case "Cash & Cash Investments":
                        symbol = null;
                        investmentName = "Cash & Cash Investments";
                        assetType = AssetType.Cash_MoneyMarket;
                        shares = value;
                        break;
                    default:
                        investmentName = chunks[1];
                        break;
                }
                
                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null), Value = value, SharesPIN = shares, CostBasis = costBasis };
                if (assetType != null) {
                    newInvestment.AssetType = assetType;
                }

                newAccount?.Investments.Add(newInvestment);
            }
        }

        return importedAccounts;
    }


    private static async Task<List<Account>> ImportTRowePrice(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();
        string[] chunks;        
                    
        // T Rowe Price
        // "Account Type","Account Name","Ticker","Account Number","Owners","Quantity","Price","Change","Market Value","Daily $ Change","Daily % Change","PRR"
        // "Rollover IRA","Total Equity Market Index Fund","POMIX","7777-7","Robert","3","$49.33","$0.52","$1","$1","1.07%","9.97%"
        // "Roth IRA","Total Equity Market Index Fund","POMIX","1111-1","Robert","4","$49.33","$0.52","$2","$2","1.07%","13.98%"
        // "Transfer On Death","Total Equity Market Index Fund","POMIX","2222-2","Robert","6","$49.33","$0.52","$3","$3","1.07%","7.19%"
        Account? newAccount = null;
        string? lastAccountNumber = null;
        newAccount = null;
        while (await rowEnumerator.MoveNextAsync()) {
            chunks = rowEnumerator.Current;
            var accountNumber = chunks[3];
            var symbol = chunks[2];
            var investmentName = chunks[1];
            double shares = FormatUtilities.ParseDouble(chunks[5]);
            double price = FormatUtilities.ParseDouble(chunks[6], allowCurrency:true);
            double value = FormatUtilities.ParseDouble(chunks[8], allowCurrency:true);
            // costBasis not available in CSV
                            
            if (lastAccountNumber != accountNumber) {
                newAccount = new(PIN) {
                    Custodian = "T Rowe Price",
                    Note = string.Concat("*", accountNumber.AsSpan(accountNumber.Length-4,4))
                };
                importedAccounts.Add(newAccount);
            }

            Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = investmentName, Price = price, Value = value, SharesPIN = shares };
            newAccount?.Investments.Add(newInvestment);
        }

        return importedAccounts;
    }

    private static async Task<List<Account>> ImportAmeriprise(IAsyncEnumerator<string[]> rowEnumerator, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();
        string[] chunks;        
        await rowEnumerator.MoveNextAsync();
                    
        // Ameriprise
        Dictionary<string,Account> accountLookup = new();
        bool processing = false;

        int? accountNameCol = null;
        int? accountDescriptionCol = null;
        int? valueCol = null;
        int? typeCol = null;
        int? quantityCol = null;
        int? symbolCol = null;
        int? descriptionCol = null;
        while (await rowEnumerator.MoveNextAsync()) {
            chunks = rowEnumerator.Current;

            if (!processing)
            {
                int i = 0;
                int count = 0;
                foreach (var chunk in chunks)
                {
                    switch (FormatUtilities.TrimQuotes(chunk))
                    {
                        case "Mkt. Value":
                            valueCol = i;
                            count++;
                            break;
                        case "Account Name":
                            accountNameCol = i;
                            count++;
                            break;
                        case "Account Description":
                            accountDescriptionCol = i;
                            count++;
                            break;
                        case "Symbol":
                            symbolCol = i;
                            count++;
                            break;
                        case "Type":
                            typeCol = i;
                            count++;
                            break;
                        case "Description":
                            descriptionCol = i;
                            count++;
                            break;
                        case "Quantity":
                            quantityCol = i;
                            count++;
                            break;
                    }

                    i++;
                }

                if (count > 0) {
                    processing = true;
                }
            } else {
                if (chunks[0].StartsWith("Total "))
                {
                    processing = false;
                    continue;
                }

                var type = GetValue(chunks, typeCol);
                string? symbol;
                if (type != null)
                {
                    symbol = type;
                }
                else 
                {
                    symbol = GetValue(chunks, symbolCol);
                }



                var investmentName = GetValue(chunks, descriptionCol);
                var accountName = GetValue(chunks, accountNameCol)!;
                var accountDescription = GetValue(chunks, accountDescriptionCol)!;
                var quantity = FormatUtilities.ParseDoubleOrNull(GetValue(chunks, quantityCol));
                var value = FormatUtilities.ParseDoubleOrNull(GetValue(chunks, valueCol), allowCurrency:true);
                if (symbol is not null && symbol.ToUpper() == "CASH") {
                    symbol = null;
                    investmentName = "Cash";
                }

                StoreInvestment(accountLookup, importedAccounts, funds, "Ameriprise", value, accountDescription!.Substring(accountDescription.Length-4), symbol, investmentName, quantity, costBasis:null, PIN:PIN);
            }
        }

        return importedAccounts;
    }

    private static Account? StoreInvestment(Dictionary<string,Account> accountLookup, List<Account> importedAccounts, IList<Fund> funds, string custodian, double? value, string account, string? symbol, string? investmentName, double? shares, double? costBasis, int? PIN)
    {
        Account? newAccount = null;

        accountLookup.TryGetValue(account, out newAccount);
        if (newAccount == null)
        {
            newAccount = new(PIN) {
                Custodian = custodian,
                Note = "*" + account
            };
            newAccount.GuessAccountType();
            importedAccounts.Add(newAccount);
            accountLookup.Add(account, newAccount);
        }

        Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = investmentName, Value = value, SharesPIN = shares, CostBasis = costBasis };
        newAccount?.Investments.Add(newInvestment);

        return newAccount;
    }

    private static string? GetValue(string[] chunks, int? columnIndex)
    {
        return (columnIndex.HasValue ? FormatUtilities.TrimQuotes(chunks[columnIndex.Value]) : null);
    }

    public static async Task<List<Account>> ImportXLSX(Stream stream, IList<Fund> funds, int? PIN) {
        List<Account> importedAccounts = new();

        MemoryStream ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        using (var document = SpreadsheetDocument.Open(ms, false))
        {
            // Retrieve a reference to the workbook part.
            WorkbookPart wbPart = document.WorkbookPart!;

            // Find the sheet with the supplied name, and then use that to retrieve a reference to the first worksheet.
            Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().Where(s => s?.Name == "Holdings Ungrouped").FirstOrDefault() ?? throw new ArgumentException("sheetName");
            WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(theSheet.Id);
            // For shared strings, look up the value in the shared strings table.
            var stringTable = 
                wbPart.GetPartsOfType<SharedStringTablePart>()
                .FirstOrDefault() ?? throw new ArgumentException("stringTable");

            int row = 11;
            bool contentOver = false;
            Dictionary<string,Account> accountLookup = new();
            while (!contentOver) {
                    string? accountName = GetValue(GetCell(wsPart, "A" + row.ToString()), stringTable);
                    if (string.IsNullOrEmpty(accountName)) {
                        contentOver = true;
                        break;
                    }

                    string? investmentName = GetValue(GetCell(wsPart, "B" + row.ToString()), stringTable);
                    string? symbol = GetValue(GetCell(wsPart, "D" + row.ToString()), stringTable);
                    double shares = FormatUtilities.ParseDouble(GetCell(wsPart, "I" + row.ToString()).InnerText, allowCurrency:false);
                    double value = FormatUtilities.ParseDouble(GetCell(wsPart, "J" + row.ToString()).InnerText, allowCurrency:true);
                    double costBasis = FormatUtilities.ParseDouble(GetCell(wsPart, "N" + row.ToString()).InnerText, allowCurrency:true);

                accountLookup.TryGetValue(accountName, out Account? newAccount);
                if (newAccount == null) {
                    newAccount = new(PIN) {
                        Custodian = "Morgan Stanley",
                        Note = "*" + accountName
                    };
                    newAccount.GuessAccountType();
                    accountLookup.Add(accountName, newAccount);
                    importedAccounts.Add(newAccount);
                }

                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = investmentName, Value = value, SharesPIN = shares, CostBasis = costBasis };
                newAccount?.Investments.Add(newInvestment);
                row++;
            }
        }

        return importedAccounts;
    }

    private static string? GetValue(Cell cell, SharedStringTablePart stringTable) {
        try {
            return stringTable.SharedStringTable.ElementAt(int.Parse(cell.InnerText)).InnerText;
        } catch (Exception) {
            return null;
        }
    }
    
    private static Cell GetCell(WorksheetPart wsPart, string cellReference) {
        return wsPart.Worksheet.Descendants<Cell>().Where(c => c.CellReference.Value == cellReference)?.FirstOrDefault();
    }

    // TODO: testing ETrade scenarios, i don't think this routine works well...a string split with commas, and no spaces, returns a list with 1 item.
    // don't fix without doing a full walkthrough to make sure all file formats work.
    private static async Task<List<string>> SplitCsvLine(string s) {
        List<string> str = new List<string>();

        await Task.Run(() => {
            int i;
            int a = 0;
            int count = 0;
            for (i = 0; i < s.Length; i++) {
                switch (s[i]) {
                    case ',':
                        if ((count & 1) == 0) {
                            str.Add(s.Substring(a, i - a));
                            a = i + 1;
                        }
                        break;
                    case '"':
                    case '\'': count++; break;
                }
            }
            str.Add(s.Substring(a));
        });

        return str;
    }

    static void DebugWriteChunks(string[] chunks) {
        foreach (string chunk in chunks) {
            Console.Write("'" + chunk + "',");
        }
        Console.WriteLine();
    }
}