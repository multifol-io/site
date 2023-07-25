using System.Text.Json;
using System.Text.Json.Serialization;
using api;

string cacheLocation = "c:\reviews";
HttpClient httpClient = new();

var fundsUri = new Uri("https://bogle.tools/data/funds.json");
var fundsJson = await httpClient.GetAsync(fundsUri.AbsoluteUri);

var stocksUri = new Uri("https://bogle.tools/data/USStocks.json");
var stocksJson = await httpClient.GetAsync(stocksUri.AbsoluteUri);

JsonSerializerOptions options = new() {
    Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
};

var funds = await JsonSerializer.DeserializeAsync<List<Fund>>(fundsJson.Content.ReadAsStream(), options);
var Stocks = await JsonSerializer.DeserializeAsync<List<Fund>>(stocksJson.Content.ReadAsStream(), options);
funds?.AddRange(Stocks);

AppData appData = new();
double? minPortfolio = null;
double? maxPortfolio = null;

if (args.Length == 0) {
    await ProcessTopics(minPortfolio, maxPortfolio, appData, funds!);
} else {
    int argIndex = 0;
    bool skip = false;
    int? start = null;
    int pages = 1;
    bool full = false;
    bool all = false;
    foreach (var arg in args) {
        if (skip) {
            // skip already processed arg
            skip = false;
        } else if (arg.StartsWith('-')) {

            switch (arg) {
                case "-s":
                case "--start":
                    start = int.Parse(args[argIndex+1]);
                    skip = true;
                    break;
                case "-p":
                case "--pages":
                    pages = int.Parse(args[argIndex+1]);
                    skip = true;
                    break;
                case "--min":
                    minPortfolio = double.Parse(args[argIndex+1]);
                    skip = true;
                    break;
                case "--max":
                    maxPortfolio = double.Parse(args[argIndex+1]);
                    skip = true;
                    break;
                case "-f":
                case "--full":
                    full = true;
                    break;
                case "-a":
                case "--all":
                    all = true;
                    break;
                default:
                    Console.WriteLine("unknown switch: " + arg);
                    return -1;
            }
        } else {
            await ProcessTopic(arg, title:"", minPortfolio, maxPortfolio, showDetails:full, appData, funds!);
        }

        argIndex++;
    }

    if (start != null) {
        await ProcessForumPages((int)start, pages, minPortfolio, maxPortfolio, filter:(all?null:"portfolio"), showDetails:full, appData, funds!);
    }
}

return 0;

static async Task ProcessTopic(string topic, string title, double? minPortfolio, double? maxPortfolio, bool showDetails, IAppData appData, IList<Fund> funds) {
    HttpClient httpClient = new();
    var topicStream = await httpClient.GetStreamAsync($"https://api.bogle.tools/api/gettopic?topic={topic}");
    string postContent = new StreamReader(topicStream).ReadToEnd().Replace("<br>","").Replace("<em class=\"text-italics\">","").Replace("</em>","").Replace("<strong class=\"text-strong\">","").Replace("</strong>","").Replace("<span style=\"text-decoration:underline\">","").Replace("</span>","");
    var importedFamilyData = ImportPortfolioReview.ParsePortfolioReview(postContent.Split("\n"), appData, funds);

    bool isMatch = true;
    if (minPortfolio.HasValue && maxPortfolio.HasValue) {
        isMatch = importedFamilyData.Value >= minPortfolio && importedFamilyData.Value <= maxPortfolio;
    } else if (minPortfolio.HasValue) {
        isMatch = importedFamilyData.Value >= minPortfolio;
    } else if (maxPortfolio.HasValue) {
        isMatch = importedFamilyData.Value <= maxPortfolio;
    }

    if (isMatch) {
        Console.WriteLine($"{topic},${importedFamilyData.Value},{importedFamilyData.Accounts.Count},'{title}'");

        if (showDetails) {
            foreach (var account in importedFamilyData.Accounts) {
                Console.WriteLine(account.AccountType + " at " + account.Custodian);
                foreach (var investment in account.Investments) {
                    Console.WriteLine($"  {FormatUtilities.formatPercent3(investment.Value/importedFamilyData.Value*100.0)} {investment.Name} ({investment.Ticker}) ({investment.ExpenseRatio}%)");
                }
            }

            Console.WriteLine();
        }

        if (importedFamilyData.Title != null && importedFamilyData.Title.StartsWith("Error")) {
            Console.WriteLine(importedFamilyData.Title + "\n");
        }
    }
}

static async Task ProcessForumPages(int start, int pages, double? minPortfolio, double? maxPortfolio, string? filter, bool showDetails, IAppData appData, IList<Fund> funds) {
    HttpClient httpClient = new();
    
    for (int i=start; i<start+pages; i++) {
        string? topics = await ForumUtilities.GetTopicsFromForum("1", i*50, filter);
        var topicLines = topics.Split('\n');
        // Console.WriteLine($"start: {i*50}");
        // foreach (var topicLine in topicLines) {
        //     Console.WriteLine(topicLine);
        // }

        await ProcessTopicLines(topicLines, minPortfolio, maxPortfolio, showDetails:showDetails, appData, funds);
    }
}

static async Task ProcessTopicLines(string[] topicLines, double? minPortfolio, double? maxPortfolio, bool showDetails, IAppData appData, IList<Fund> funds) {
    foreach (var topicLine in topicLines) {
        var spaceLoc = topicLine.IndexOf(" ");
        if (spaceLoc > -1) {
            var topic = topicLine[0..spaceLoc];
            var title = topicLine[(spaceLoc + 1)..];

            if (topic != "6212") { // don't parse "asking portfolio questions"
                // Console.WriteLine(topic + " " + title);
                await ProcessTopic(topic, title, minPortfolio, maxPortfolio, showDetails, appData, funds);
            }
        }
    }
}

static async Task ProcessTopics(double? minPortfolio, double? maxPortfolio, IAppData appData, IList<Fund> funds) {
        string topicList = "409135 portfolio review - NYC 54/53\n"
        + "408853 Portfolio Review - 38 y/o Consultant, Soon to be Married\n"
        + "408867 Portfolio review for a 36 year old new poster\n"
        + "408844 Portfolio Review - used space before units '1.35 M'\n"
        + "407986 Portfolio advice: Simplifying, Planning, and (hopefully) retiring one day\n"
        + "408865 Long-time Boglehead - 2023 Portfolio Refresh\n"
        + "348471 Portfolio review (2 year update) - > 100% by design \n"
        + "385354 Portfolio Question -- Poor harvesting by design\n"
        + "409223 Portfolio Review Request - 1 Year Post Edward Jones To DIY Portfolio -- Poor harvesting by design\n"
        + "407956 Portfolio Sanity Check -- TODO - misses assets $$ \n"
        + "408103 Portfolio review – before life gets more expensive! -- TODO - cannot parse badly formatted investments\n"
        + "408017 Portfolio Check-up / New to this -- TODO - sholld be able to harvest retirement assets\n"
        + "408057 Portfolio review for the first time - thanks -- TODO - should 2.8% iBond harvest better?\n"
        + "78881 A young boglehead portfolio review - TODO - doesn't yet harvest 'just north of $50k'\n"
        + "407996 portfolio review - TODO - misses 2nd - 7th accounts \n";

        var topicLines = topicList.Split('\n');
        await ProcessTopicLines(topicLines, minPortfolio, maxPortfolio, showDetails:false, appData, funds);
}
