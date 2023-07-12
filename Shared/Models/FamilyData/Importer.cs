using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components.Forms;



public class ImportResult {

    public List<Account> ImportedAccounts { get; set; } = new();
    private bool _ImportUpdatedAccounts;
    public bool ImportUpdatedAccounts {
        get { return _ImportUpdatedAccounts; }
        set {
            _ImportUpdatedAccounts = value;
            foreach (var account in UpdatedAccounts)
            {
                account.Import = _ImportUpdatedAccounts;
            }
        }
    }
    public List<Account> UpdatedAccounts { get; set; } = new();
    private bool _ImportNewAccounts;
    public bool ImportNewAccounts {
        get { return _ImportNewAccounts; }
        set {
            _ImportNewAccounts = value;
            foreach (var account in NewAccounts)
            {
                account.Import = _ImportNewAccounts;
            }
        }
    }
    public List<Account> NewAccounts { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
    public int DataFilesImported { get; set; }
}

public class ImportError
{
    public Exception? Exception { get; set; }
}

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
                    string? content = null;
                    try {
                        var buffer = new byte[file.Size];
                        using (var stream = file.OpenReadStream())
                        {
                            await stream.ReadAsync(buffer);
                            content = System.Text.Encoding.UTF8.GetString(buffer);
                        }

                        var lines = System.Text.RegularExpressions.Regex.Split(content, "\r\n|\r|\n");
                        var importedAccountsCSV = await Importer.ImportCSV(lines, funds, PIN);
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
    
    public static async Task<List<Account>> ImportCSV(string[] lines, IList<Fund> funds, int? PIN)
    {
        try {
            List<Account> importedAccounts = new();

            // Process the header
            int lineIndex = 0;
            string? line = lines[lineIndex++];
            var headerChunks = line.Split(',');
            var headerChunksLen = headerChunks.Length;

            if (headerChunksLen > 1 && FormatUtilities.TrimQuotes(headerChunks[0]) == ("COB Date") && FormatUtilities.TrimQuotes(headerChunks[1]) == "Security #") {
                //MERRILL EDGE
                Dictionary<string,Account> accountLookup = new();

                var chunks = await SplitCsvLine(lines[lineIndex++]);
                while (chunks.Count > 0 && chunks[0] != "")
                {
                    var accountNum = FormatUtilities.TrimQuotes(chunks[7]);
                    var accountRegistration = FormatUtilities.TrimQuotes(chunks[6]);
                    var accountNickname = FormatUtilities.TrimQuotes(chunks[5]);
                    var symbol = FormatUtilities.TrimQuotes(chunks[2]);
                    string? investmentName = FormatUtilities.TrimQuotes(chunks[4]);
                    double shares = FormatUtilities.ParseDouble(chunks[8]);
                    double value = FormatUtilities.ParseDouble(chunks[10] ?? "0.0", allowCurrency:true);
                    double costBasis = value - FormatUtilities.ParseDouble(chunks[11] ?? "0.0", allowCurrency:true);

                    if (value < 0.0 || value > 1.0) {
                        Account? newAccount = null;
                        accountLookup.TryGetValue(accountNum, out newAccount);
                        if (newAccount == null)
                        {
                            newAccount = new(PIN) {
                                Custodian = "Merrill Edge",
                                Note = "*" + accountNum.Substring(accountNum.Length - 4) + " " + accountNickname + " " + accountRegistration
                            };
                            importedAccounts.Add(newAccount);
                            accountLookup.Add(accountNum, newAccount);
                        }
                        
                        Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null), Value = value, SharesPIN = shares, CostBasis = costBasis };
                        newAccount?.Investments.Add(newInvestment);
                    }

                    chunks = await SplitCsvLine(lines[lineIndex++]);
                }
            } else if (headerChunksLen > 1 && headerChunks[0].StartsWith("Account Number") && headerChunks[1] == "Account Name") {
                // FIDELITY - blank line ends investment listings or ",,,,,,,,,,,,,,,"
                // Line 0: Account Number,Account Name,Symbol,Description,Quantity,Last Price,Last Price Change,Current Value
                string lastAccountNumber = "";
                line = lines[lineIndex++];
                Account? newAccount = null;
                while (line != "" && line != ",,,,,,,,,,,,,,,")
                {
                    List<string> chunks = await SplitCsvLine(line);
                    try {
                        var accountNumber = chunks[0];
                        var accountName = chunks[1];
                        var symbol = chunks[2];

                        // many tickers have "**" at end, signifying money market fund.
                        if (symbol.Length >= 2 && symbol.Substring(symbol.Length-2) == "**") { 
                            symbol = symbol.Substring(0, symbol.Length - 2);
                        }

                        string? investmentName;
                        double value;
                        double? price = null;
                        double? shares = null;
                        double? costBasis = null;
                        double? expenseRatio = null;

                        // many tickers have "**" at end, signifying money market fund.
                        if (symbol.Length >= 2 && symbol.Substring(symbol.Length-2) == "**") { 
                            symbol = symbol.Substring(0, symbol.Length - 2);
                        }

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

                        if (value < 0.0 || value > 1.0) {
                            if (lastAccountNumber != accountNumber)
                            {
                                newAccount = new(PIN) {
                                    Custodian = "Fidelity",
                                    Note = "*"+ accountNumber.Substring(accountNumber.Length-4,4)
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

                            if (newInvestment.Ticker == "PENDING") {
                                newInvestment.AssetType = global::AssetType.Cash;
                            }

                            newAccount?.Investments.Add(newInvestment);
                        }
                    } 
                    catch (Exception e)
                    {
                        Console.WriteLine("skipped line due to error: " + line);
                        Console.WriteLine(e.GetType().Name + " " + e.Message + " " + e.StackTrace);
                    }

                    line = lines[lineIndex++];
                }
            } 
            else if (headerChunksLen > 1 && headerChunks[0] == "Account Number" && headerChunks[1] == "Investment Name")
            {
                // VANGUARD - blank lines seperates accounts. two blank lines end investment listing.
                // Line 0: Account Number,Investment Name,Symbol,Shares,Share Price,Total Value,
                int lastAccountStartLineIndex = -1;
                line = null;
                Account? newAccount = null;
                while (lastAccountStartLineIndex != lineIndex - 1) {
                    lastAccountStartLineIndex = lineIndex;
                    if (line == null || line == "") {
                        newAccount = null;
                        line = lines[lineIndex++];
                        while (line != "")
                        {
                            var chunks = line.Split(',');
                            var accountNumber = chunks[0];
                            var symbol = chunks[2];
                            var investmentName = chunks[1];
                            double shares = FormatUtilities.ParseDouble(chunks[3]);
                            double price = FormatUtilities.ParseDouble(chunks[4], allowCurrency:true);
                            double value = FormatUtilities.ParseDouble(chunks[5], allowCurrency:true);
                            // costBasis not available in CSV
                                            
                            if (value < 0.0 || value > 1.0) {
                                if (newAccount == null) {
                                    newAccount = new(PIN) {
                                        Custodian = "Vanguard",
                                        Note = "*"+ accountNumber.Substring(accountNumber.Length-4,4)
                                    };
                                    importedAccounts.Add(newAccount);
                                }

                                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = investmentName, Price = price, Value = value, SharesPIN = shares };
                                newAccount?.Investments.Add(newInvestment);
                            }

                            line = lines[lineIndex++];
                        }
                    }
                }
            }
            else if (headerChunksLen > 1 && headerChunks[0] == "Fund Account Number" && headerChunks[1] == "Fund Name")
            {
                // VANGUARD - orig mutual fund account style - two blank lines end investment listing.
                // Line 0: Fund Account Number,Fund Name,Price,Shares,Total Value,
                Dictionary<string,Account> accountLookup = new();

                var chunks = await SplitCsvLine(lines[lineIndex]);
                int consecutiveBlanks = 0;

                while (chunks.Count > 0 && consecutiveBlanks != 2)
                {
                    if (chunks.Count == 1) {
                        consecutiveBlanks++;
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

                                        
                        if (value < 0.0 || value > 1.0) {
                            Account? newAccount = null;
                            accountLookup.TryGetValue(accountNum, out newAccount);
                            if (newAccount == null)
                            {
                                newAccount = new(PIN) {
                                    Custodian = "Vanguard",
                                    Note = "*" + accountNum.Substring(accountNum.Length - 4)
                                };
                                importedAccounts.Add(newAccount);
                                accountLookup.Add(accountNum, newAccount);
                            }
                                                    
                            Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null) , Value = value, SharesPIN = shares };
                            newAccount?.Investments.Add(newInvestment);
                        }
                    }

                    lineIndex++;
                    chunks = await SplitCsvLine(lines[lineIndex]);
                }
            }
            else if (headerChunks[0].StartsWith("Account Summary")) // sometimes headerChunksLen == 1, sometimes more.
            {
                var chunks = await SplitCsvLine(lines[lineIndex++]); // account header line
                chunks = await SplitCsvLine(lines[lineIndex++]); // accountInfo
                Account newAccount = new(PIN) {
                        Custodian = "ETrade",
                        Note = FormatUtilities.TrimQuotes(chunks[0])
                    };
                importedAccounts.Add(newAccount);

                chunks = await SplitCsvLine(lines[lineIndex]);
                bool foundHeader = false;
                while (!foundHeader) {
                    if (chunks.Count == 7 && chunks[0] == "Symbol" && chunks[1] == "Last Price $")
                    {
                        foundHeader = true;
                        lineIndex = lineIndex + 1;
                    }
                    else
                    {
                        lineIndex++;
                        chunks = await SplitCsvLine(lines[lineIndex]);
                    }
                }

                chunks = await SplitCsvLine(lines[lineIndex++]);

                while (chunks[0] != "TOTAL") {
                    var symbol = chunks[0];
                    string? investmentName = null;

                    double shares = FormatUtilities.ParseDouble(chunks[4]);
                    double value = FormatUtilities.ParseDouble(chunks[9], allowCurrency:true);
                    double costBasis = value - FormatUtilities.ParseDouble(chunks[7], allowCurrency:true);

                    if (chunks[0] == "CASH") {
                        shares = value;
                    }

                    if (value < 0.0 || value > 1.0) {
                        Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null), Value = value, SharesPIN = shares, CostBasis = costBasis };
                        newAccount?.Investments.Add(newInvestment);
                    }

                    chunks = await SplitCsvLine(lines[lineIndex++]);
                }
            }
            else if (headerChunks[0].StartsWith("\"Positions for "))
            {
                // SCHWAB - line with blank cells divides accounts
                // Line 0: "Positions for CUSTACCS as of 09:13 PM ET, 02/04/2023","","","","","","","","","","","","","","","",""
                // Line 1: "","","","","","","","","","","","","","","","",""
                // Line 2: "Individual ...###","","","","","","","","","","","","","","","",""
                // Line 3: "Symbol","Description","Quantity","Price","Price Change %","Price Change $","Market Value","Day Change %","Day Change $","Cost Basis","Gain/Loss %","Gain/Loss $","Ratings","Reinvest Dividends?","Capital Gains?","% Of Account","Security Type"
                // 0-n lines: investment info
                // End of Account: "Cash & Cash Investments","--","--","--","--","--","$5.20","0%","$0.00","--","--","--","--","--","--","0.29%","Cash and Money Market"
                // Totals of Account: "Account Total","--","--","--","--","--","$51,767.73","-1.77%","-$31.93","$547.79","33.1%","$181.34","--","--","--","--","--"
                var chunks = lines[lineIndex++].Split("\",\"");
                Account? newAccount = null;
                while (chunks.Length > 0 && chunks[0] == "\"")
                {
                    var accountNameChunks = lines[lineIndex++].Split(',');
                    newAccount = new(PIN) {
                        Custodian = "Schwab",
                        Note = "*"+ accountNameChunks[0].Substring(1, accountNameChunks[0].Length - 2)
                    };

                    if (newAccount != null) {  // TODO: only doing to prove to compiler that ctor won't throw ... best way?
                        importedAccounts.Add(newAccount);
                        lineIndex++; // skip investmentHeaders line
                        chunks = lines[lineIndex++].Split("\",\"");
                        while (lineIndex < lines.Length - 1 && chunks[0] != "\"") {
                            var symbol = chunks[0].Substring(1); // front quote wasn't removed by split.
                            string? investmentName = null;
                            double shares = FormatUtilities.ParseDouble(chunks[2]);
                            double value = FormatUtilities.ParseDouble(chunks[6], allowCurrency:true);
                            double costBasis = FormatUtilities.ParseDouble(chunks[9], allowCurrency:true);
                            switch (symbol) {
                                case "Cash & Cash Investments":
                                    symbol = "CASH";
                                    shares = value;
                                    break;
                                case "Account Total":
                                    value = 0.0;
                                    shares = 0.0;
                                    break;
                                default:
                                    investmentName = chunks[1];
                                    break;
                            }

                            if (value < 0.0 || value > 1.0) {
                                Investment newInvestment = new (PIN) { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null), Value = value, SharesPIN = shares, CostBasis = costBasis };
                                newAccount?.Investments.Add(newInvestment);
                            }

                            chunks = lines[lineIndex++].Split("\",\"");
                        }
                    }
                }
            }
            else if (headerChunks[0] == "\"AMERIPRISE BROKERAGE\"")
            {
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
                foreach (var line2 in lines)
                {
                    var chunks = await SplitCsvLine(line2);

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
                        if (chunks[0].StartsWith("\"Total "))
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
                        
                        var newAccount = StoreInvestment(accountLookup, importedAccounts, funds, "Ameriprise", value, accountDescription!.Substring(accountDescription.Length-4), symbol, investmentName, quantity, costBasis:null, PIN:PIN);
                    }
                }
            }
            else
            {
                throw new InvalidDataException("Importing this CSV file failed. We currently support importing CSV files from Ameriprise, eTrade, Fidelity, Merrill Edge, Schwab, or Vanguard");
            }

            return importedAccounts;
        } catch (Exception e) {
            if (e is InvalidDataException) 
            {
                throw e;
            }
            else 
            {
                throw new InvalidDataException("Importing this CSV file failed, even though we think it should have worked (we think it was from Ameriprise, eTrade, Fidelity, Merrill Edge, Schwab, or Vanguard).", e);
            }
        }
    }


    private static Account? StoreInvestment(Dictionary<string,Account> accountLookup, List<Account> importedAccounts, IList<Fund> funds, string custodian, double? value, string account, string? symbol, string? investmentName, double? shares, double? costBasis, int? PIN)
    {
        Account? newAccount = null;

        if (value < 0.0 || value > 1.0) {
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
        }

        return newAccount;
    }

    private static string? GetValue(List<string> chunks, int? columnIndex)
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
            WorkbookPart wbPart = document.WorkbookPart;

            // Find the sheet with the supplied name, and then use that to retrieve a reference to the first worksheet.
            Sheet theSheet = wbPart?.Workbook.Descendants<Sheet>().Where(s => s?.Name == "Holdings Ungrouped").FirstOrDefault();

            // Throw an exception if there is no sheet.
            if (theSheet == null)
            {
                throw new ArgumentException("sheetName");
            }

            WorksheetPart wsPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            // For shared strings, look up the value in the shared strings table.
            var stringTable = 
                wbPart.GetPartsOfType<SharedStringTablePart>()
                .FirstOrDefault();

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

                    if (value < 0.0 || value > 1.0) {
                        Account? newAccount = null;
                        accountLookup.TryGetValue(accountName, out newAccount);
                        if (newAccount == null)
                        {
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
                    }
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
}