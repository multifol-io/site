using System.Globalization;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

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

    public void UpdatePercentages(double totalValue)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = (investment.Value ?? 0) / totalValue * 100;
        }
    }

    private void GuessAccountType() 
    {
        if (Note.Contains("401K") || Note.Contains("401k")) { AccountType = "401k"; }
        else if (Note.Contains("HSA") || Note.Contains("Health Savings Account")) { AccountType = "HSA"; }
    }

    public static async Task<List<Account>> ImportXLSX(Stream stream, IList<Fund> funds) {
        List<Account> importedAccounts = new();

        MemoryStream ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        using (var document = SpreadsheetDocument.Open(ms, false))
        {

            // Retrieve a reference to the workbook part.
            WorkbookPart wbPart = document.WorkbookPart;

            // Find the sheet with the supplied name, and then use that 
            // Sheet object to retrieve a reference to the first worksheet.
            Sheet theSheet = wbPart?.Workbook.Descendants<Sheet>().Where(s => s?.Name == "Holdings Ungrouped").FirstOrDefault();

            // Throw an exception if there is no sheet.
            if (theSheet == null)
            {
                throw new ArgumentException("sheetName");
            }

            WorksheetPart wsPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            // For shared strings, look up the value in the
            // shared strings table.
            var stringTable = 
                wbPart.GetPartsOfType<SharedStringTablePart>()
                .FirstOrDefault();

            int row = 11;
            bool contentOver = false;
            Dictionary<string,Account> accountLookup = new();
            while (!contentOver) {
                    var accountNameCell = GetCell(wsPart, "A" + row.ToString());
                    var accountName = GetValue(accountNameCell, stringTable);
                    if (string.IsNullOrEmpty(accountName)) {
                        contentOver = true;
                        break;
                    }
                    var investmentNameCell = GetCell(wsPart, "B" + row.ToString());
                    var investmentName = GetValue(investmentNameCell, stringTable);
                    var symbolCell = GetCell(wsPart, "D" + row.ToString());
                    var symbol = GetValue(symbolCell, stringTable);
                    var marketValue = GetCell(wsPart, "J" + row.ToString()).InnerText;
                    double doubleValue = 0.0;
                    double.TryParse(marketValue,
                                            NumberStyles.AllowCurrencySymbol | NumberStyles.Float | NumberStyles.AllowThousands,
                                            CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"),
                                            out doubleValue);
                    Account? newAccount = null;
                    accountLookup.TryGetValue(accountName, out newAccount);
                    if (newAccount == null)
                    {
                        newAccount = new() {
                            Custodian = "Morgan Stanley",
                            Note = "⚠️*" + accountName
                        };
                        newAccount.GuessAccountType();
                        accountLookup.Add(accountName, newAccount);
                        importedAccounts.Add(newAccount);
                    }

                    Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = investmentName, Value = doubleValue };
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

    public static List<Account> ImportCSV(string[] lines, IList<Fund> funds)
    {
        try {
            List<Account> importedAccounts = new();

            // Process the header
            int lineIndex = 0;
            string? line = lines[lineIndex++];
            var headerChunks = line.Split(',');
            var headerChunksLen = headerChunks.Length;

            if (headerChunksLen > 1 && TrimQuotes(headerChunks[0]) == ("COB Date") && TrimQuotes(headerChunks[1]) == "Security #") {
                //MERRILL EDGE
                Dictionary<string,Account> accountLookup = new();

                var chunks = SplitCsvLine(lines[lineIndex++]);
                while (chunks.Count > 0 && chunks[0] != "")
                {
                    var accountNum = TrimQuotes(chunks[7]);
                    var accountRegistration = TrimQuotes(chunks[6]);
                    var accountNickname = TrimQuotes(chunks[5]);
                    var symbol = TrimQuotes(chunks[2]);
                    string? investmentName = TrimQuotes(chunks[4]);
                    string? investmentValue = TrimQuotes(chunks[10]);

                    Account? newAccount = null;
                    accountLookup.TryGetValue(accountNum, out newAccount);
                    if (newAccount == null)
                    {
                        newAccount = new() {
                            Custodian = "Merrill Edge",
                            Note = "⚠️ "+ accountNickname + " " + accountRegistration + " --" + accountNum.Substring(accountNum.Length - 4)
                        };
                        importedAccounts.Add(newAccount);
                        accountLookup.Add(accountNum, newAccount);
                    }

                    if (investmentValue != null) {
                        double doubleValue = 0.0;
                        double.TryParse(investmentValue,
                                                NumberStyles.AllowCurrencySymbol | NumberStyles.Float | NumberStyles.AllowThousands,
                                                CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"),
                                                out doubleValue);
                        Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null) , Value = doubleValue };
                        newAccount?.Investments.Add(newInvestment);
                    }

                    chunks = SplitCsvLine(lines[lineIndex++]);
                }
            } else if (headerChunksLen > 1 && headerChunks[0].StartsWith("Account Number") && headerChunks[1] == "Account Name") {
                // FIDELITY - blank line ends investment listings or ",,,,,,,,,,,,,,,"
                // Line 0: Account Number,Account Name,Symbol,Description,Quantity,Last Price,Last Price Change,Current Value
                string lastAccountNumber = "";
                line = lines[lineIndex++];
                Account? newAccount = null;
                while (line != "" && line != ",,,,,,,,,,,,,,,")
                {
                    List<string> chunks = Account.SplitCsvLine(line);
                    var accountNumber = chunks[0];
                    var accountName = chunks[1];
                    var symbol = chunks[2];
                    var investmentName = chunks[3];

                    var investmentValue = Account.TrimQuotes(chunks[7]);

                    if (investmentValue[0] == '$')
                    {
                        investmentValue = investmentValue.Substring(1);
                    }

                    double doubleValue = 0.0;
                    double.TryParse(investmentValue,
                                            NumberStyles.AllowCurrencySymbol | NumberStyles.Float | NumberStyles.AllowThousands,
                                            CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"),
                                            out doubleValue);

                    if (lastAccountNumber != accountNumber)
                    {
                        newAccount = new() {
                            Custodian = "Fidelity",
                            Note = "⚠️*"+ accountNumber.Substring(accountNumber.Length-4,4) + " " + accountName
                        };
                        newAccount.GuessAccountType();
                        importedAccounts.Add(newAccount);
                        lastAccountNumber = accountNumber;
                    }

                    Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = investmentName, Value = doubleValue };
                    newAccount?.Investments.Add(newInvestment);
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
                            var investmentValue = chunks[5];
                            if (investmentValue[0] == '$')
                            {
                                investmentValue = investmentValue.Substring(1);
                            }

                            double doubleValue = 0.0;
                            double.TryParse(investmentValue, out doubleValue);
                            if (newAccount == null) {
                                newAccount = new() {
                                    Custodian = "Vanguard",
                                    Note = "⚠️*"+ accountNumber.Substring(accountNumber.Length-4,4)
                                };
                                importedAccounts.Add(newAccount);
                            }

                            Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = investmentName, Value = doubleValue };
                            newAccount?.Investments.Add(newInvestment);
                            line = lines[lineIndex++];
                        }
                    }
                }
            }
            else if (headerChunks[0].StartsWith("Account Summary")) // sometimes headerChunksLen == 1, sometimes more.
            {
                var chunks = SplitCsvLine(lines[lineIndex++]); // account header line
                chunks = SplitCsvLine(lines[lineIndex++]); // accountInfo
                Account newAccount = new() {
                        Custodian = "ETrade",
                        Note = "⚠️"+ TrimQuotes(chunks[0])
                    };
                importedAccounts.Add(newAccount);

                chunks = SplitCsvLine(lines[lineIndex]);
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
                        chunks = SplitCsvLine(lines[lineIndex]);
                    }
                }

                chunks = Account.SplitCsvLine(lines[lineIndex++]);

                while (chunks[0] != "TOTAL") {
                    var symbol = chunks[0];
                    string? investmentName = null;
                    string investmentValue = chunks[9];
                    if (investmentValue != null) {
                        double doubleValue = 0.0;
                        double.TryParse(investmentValue,
                            NumberStyles.AllowCurrencySymbol | NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"),
                            out doubleValue);
                        Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null) , Value = doubleValue };
                        newAccount?.Investments.Add(newInvestment);
                    }

                    chunks = Account.SplitCsvLine(lines[lineIndex++]);
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
                    newAccount = new() {
                        Custodian = "Schwab",
                        Note = "⚠️"+ accountNameChunks[0].Substring(1, accountNameChunks[0].Length - 2)
                    };

                    if (newAccount != null) {  // TODO: only doing to prove to compiler that ctor won't throw ... best way?
                        importedAccounts.Add(newAccount);
                        lineIndex++; // skip investmentHeaders line
                        chunks = lines[lineIndex++].Split("\",\"");
                        while (lineIndex < lines.Length - 1 && chunks[0] != "\"") {
                            var symbol = chunks[0].Substring(1); // front quote wasn't removed by split.
                            string? investmentName = null;
                            string? investmentValue;
                            switch (symbol) {
                                case "Cash & Cash Investments":
                                    symbol = "CASH";
                                    investmentValue = chunks[6];
                                    break;
                                case "Account Total":
                                    investmentValue = null;
                                    break;
                                default:
                                    investmentName = chunks[1];
                                    investmentValue = chunks[6];
                                    break;
                            }

                            if (investmentValue != null) {
                                double doubleValue = 0.0;
                                double.TryParse(investmentValue,
                                                        NumberStyles.AllowCurrencySymbol | NumberStyles.Float | NumberStyles.AllowThousands,
                                                        CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"),
                                                        out doubleValue);
                                Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = (investmentName != null ? investmentName : null) , Value = doubleValue };
                                newAccount?.Investments.Add(newInvestment);
                            }

                            chunks = lines[lineIndex++].Split("\",\"");
                        }
                    }
                }
            }
            else
            {
                throw new InvalidDataException("CSV file doesn't appear to be Fidelity, Schwab, or Vanguard");
            }

            return importedAccounts;
        } catch (Exception e) {
            Console.WriteLine(e.Message + "\n" + e.InnerException + "\n" + e.Source + "\n" + e.StackTrace);
            throw e;
        }
    }

    public static string TrimQuotes(string input) {
        if (input == null) { return input; }
        int start = 0;
        int end = input.Length - 1;

        if (end > 1 && input[end] == '"') {
            end--;
        }

        if (input[0] == '"') {
            start++;
        }

        return input.Substring(start, end);
    }

    // TODO: testing ETrade scenarios, i don't think this routine works well...a string split with commas, and no spaces, returns a list with 1 item.
    // don't fix without doing a full walkthrough to make sure all file formats work.
    public static List<string> SplitCsvLine(string s) {
        int i;
        int a = 0;
        int count = 0;
        List<string> str = new List<string>();
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
        return str;
    }
}