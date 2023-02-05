using System.Globalization;
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

    public void UpdatePercentages(double totalValue)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = (investment.Value ?? 0) / totalValue * 100;
        }
    }

    public static List<Account> ImportCSV(string[] lines, IList<Fund> funds)
    {
        List<Account> importedAccounts = new();

        // Process the header
        int lineIndex = 0;
        string? line = lines[lineIndex++];
        var headerChunks = line.Split(',');

        if (headerChunks.Length > 2) {
            if (headerChunks[0].StartsWith("Account Number") && headerChunks[1] == "Account Name")
            {
                // FIDELITY - blank line ends investment listings.
                // Line 0: Account Number,Account Name,Symbol,Description,Quantity,Last Price,Last Price Change,Current Value
                string lastAccountNumber = "";
                line = lines[lineIndex++];
                Account? newAccount = null;
                while (line != "")
                {
                    var chunks = line.Split(',');
                    var accountNumber = chunks[0];
                    var accountName = chunks[1];
                    var symbol = chunks[2];
                    var investmentName = chunks[3];

                    var investmentValue = chunks[7];
                    if (investmentValue[0] == '$')
                    {
                        investmentValue = investmentValue.Substring(1);
                    }

                    double doubleValue = 0.0;
                    double.TryParse(investmentValue, out doubleValue);
                    if (lastAccountNumber != accountNumber)
                    {
                        newAccount = new() {
                            Custodian = "Fidelity",
                            Note = "⚠️*"+ accountNumber.Substring(accountNumber.Length-4,4) + " " + accountName
                        };
                        importedAccounts.Add(newAccount);
                        lastAccountNumber = accountNumber;
                    }

                    Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = investmentName, Value = doubleValue };
                    newAccount?.Investments.Add(newInvestment);
                    line = lines[lineIndex++];
                }
            } 
            else if (headerChunks[0] == "Account Number" && headerChunks[1] == "Investment Name")
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
        } else {
            throw new InvalidDataException("CSV file doesn't appear to be Fidelity, Schwab, or Vanguard");
        }

        return importedAccounts;
    }
}