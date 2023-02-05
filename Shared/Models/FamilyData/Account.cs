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
        string? lastAccountNumber = null;
        List<Account> importedAccounts = new();

        //Fidelity Line 1: Account Number,Account Name,Symbol,Description,Quantity,Last Price,Last Price Change,Current Value
        //Vanguard Line 1: Account Number,Investment Name,Symbol,Shares,Share Price,Total Value,
        //Schwab Line 1: "Positions for CUSTACCS as of 09:13 PM ET, 02/04/2023","","","","","","","","","","","","","","","",""
        //Schwab Line 2: "","","","","","","","","","","","","","","","",""
        //Schwab Line 3: "Individual ...###","","","","","","","","","","","","","","","",""
        //Schwab Line 4: "Symbol","Description","Quantity","Price","Price Change %","Price Change $","Market Value","Day Change %","Day Change $","Cost Basis","Gain/Loss %","Gain/Loss $","Ratings","Reinvest Dividends?","Capital Gains?","% Of Account","Security Type"
        //Schwab End of Account: "Cash & Cash Investments","--","--","--","--","--","$5.20","0%","$0.00","--","--","--","--","--","--","0.29%","Cash and Money Market"
        //Schwab Totals of Account: "Account Total","--","--","--","--","--","$51,767.73","-1.77%","-$31.93","$547.79","33.1%","$181.34","--","--","--","--","--"

        // Process the header
        var headerChunks = lines[0].Split(',');

        int accountNumberIndex = 0; // shouldn't hardcode, but for some strange reason it wasn't matching for Fidelity CSV files!?!
        int accountNameIndex = -1;
        int symbolIndex = -1;
        int investmentNameIndex = -1;
        int investmentValueIndex = -1;
        
        int chunkIndex = 0;
        foreach (var headerChunk in headerChunks) {
            switch (headerChunk) {
                case "Account Name":
                    accountNameIndex = chunkIndex;
                    break;
                case "Symbol":
                    symbolIndex = chunkIndex;
                    break;
                case "Investment Name":
                case "Description":
                    investmentNameIndex = chunkIndex;
                    break;
                case "Current Value":
                case "Total Value":
                case "Market Value":
                    investmentValueIndex = chunkIndex;
                    break;
                case "Account Number":
                    accountNumberIndex = chunkIndex;
                    break;
                default:
                    break;
            }

            chunkIndex++;
        }

        Account? currentAccount = null;
        for (int lineNum = 1;lineNum < lines.Length; lineNum++) {
            var line = lines[lineNum];
            string? accountNumber = null;
            string? accountName = null;
            string? symbol = null;
            string? investmentName = null;
            string? investmentValue = null;
            if (line.Length > 0)
            {
                if (char.IsDigit(line[0]) || char.IsDigit(line[1]))
                {
                    // Process this data
                    var chunks = line.Split(',');
                    
                    for (int i=0; i < chunks.Length; i++) {
                        if (accountNumberIndex == i) {
                            accountNumber = chunks[i];
                        } else if (accountNameIndex == i) {
                            accountName = chunks[i];
                        } else if (symbolIndex == i) {
                            symbol = chunks[i];
                        } else if (investmentNameIndex == i) {
                            investmentName = chunks[i];
                        } else if (investmentValueIndex == i) {
                            investmentValue = chunks[i];
                        }
                    }

                    if (lastAccountNumber != accountNumber) {
                        currentAccount = new () { 
                            Note = accountName != null ? "⚠️" + accountName: "⚠️*"+ accountNumber.Substring(accountNumber.Length-4,4),
                            Custodian = (accountNameIndex != -1 ? "Fidelity" : "Vanguard")
                        };
                        importedAccounts.Add(currentAccount);
                        lastAccountNumber = accountNumber;
                    }
                    
                    if (investmentValue[0] == '$')
                    {
                        investmentValue = investmentValue.Substring(1);
                    }
                    double doubleValue = 0.0;
                    double.TryParse(investmentValue, out doubleValue);


                    if (symbol != null || investmentName != null) {
                        Investment newInvestment = new () { funds = funds, Ticker = symbol, Name = investmentName, Value = doubleValue };
                        currentAccount.Investments.Add(newInvestment);
                    }
                }

                if (line.Length > 1 && char.IsLetter(line[1])) // some account numbers start with a letter
                {
                    // stop processing file at first line that starts with a letter.
                    break;
                }
            }
        }
    
        return importedAccounts;
    }
}