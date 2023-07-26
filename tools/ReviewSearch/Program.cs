using System.Text.Json;
using System.Text.Json.Serialization;
using api;

string cacheLocation = @"c:\reviews";
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
int? minAge = null;
int? maxAge = null;
bool debug = false;

if (args.Length == 0) {
    await ProcessTopics(minPortfolio, maxPortfolio, minAge, maxAge, debug, appData, funds!);
} else {
    int argIndex = 0;
    bool skip = false;
    int? start = null;
    int pages = 1;
    bool full = false;
    bool all = false;
    List<string> topics = new();
    foreach (var arg in args) {
        if (skip) {
            // skip already processed arg
            skip = false;
        } else if (arg.StartsWith('-')) {
            switch (arg.ToLowerInvariant()) {
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
                case "--minage":
                    minAge = int.Parse(args[argIndex+1]);
                    skip = true;
                    break;
                case "--maxage":
                    maxAge = int.Parse(args[argIndex+1]);
                    skip = true;
                    break;
                case "-d":
                case "--debug":
                    debug = true;
                    break;
                case "-f":
                case "--full":
                    full = true;
                    break;
                case "-a":
                case "--all":
                    all = true;
                    break;
                case "--cachefiles":
                case "-cf":
                    foreach (var file in Directory.GetFiles(@"c:\reviews")) {
                        var fileInfo = new FileInfo(file);
                        topics.Add(Path.GetFileNameWithoutExtension(fileInfo.Name));
                    }
                    break;
                default:
                    Console.WriteLine("unknown switch: " + arg);
                    return -1;
            }
        } else {
            topics.Add(arg);
        }

        argIndex++;
    }

    if (start != null) {
        await ProcessForumPages((int)start, pages, minPortfolio, maxPortfolio, minAge, maxAge, filter:(all?null:"portfolio"), debug:debug, showDetails:full, appData, funds!);
    } else if (topics.Count > 0) {
        foreach (var topic in topics) {
            await ProcessTopic(topic, title:"", minPortfolio, maxPortfolio, minAge, maxAge, debug:debug, showDetails:full, appData, funds!);
        }
    }
}

return 0;

static async Task ProcessTopic(string topic, string title, double? minPortfolio, double? maxPortfolio, int? minAge, int? maxAge, bool debug, bool showDetails, IAppData appData, IList<Fund> funds) {
    HttpClient httpClient = new();

    string postContent = await ForumUtilities.GetTopicPost(topic, "0", @"c:\reviews");

    postContent = postContent.Replace("<br>","").Replace("<em class=\"text-italics\">","").Replace("</em>","").Replace("<strong class=\"text-strong\">","").Replace("</strong>","").Replace("<span style=\"text-decoration:underline\">","").Replace("</span>","").Replace("<ul>","").Replace("</ul>","").Replace("<li>","").Replace("</li>","");
    var importedFamilyData = ImportPortfolioReview.ParsePortfolioReview(postContent.Split("\n"), debug:debug, appData, funds);

    bool isMatchPortfolio = true;
    if (minPortfolio.HasValue && maxPortfolio.HasValue) {
        isMatchPortfolio = importedFamilyData.Value >= minPortfolio && importedFamilyData.Value <= maxPortfolio;
    } else if (minPortfolio.HasValue) {
        isMatchPortfolio = importedFamilyData.Value >= minPortfolio;
    } else if (maxPortfolio.HasValue) {
        isMatchPortfolio = importedFamilyData.Value <= maxPortfolio;
    }

    bool isMatchAge = true;
    //TODO: consider Person[1] too
    if (minAge.HasValue && maxAge.HasValue) {
        isMatchAge = importedFamilyData.People[0].Age >= minAge && importedFamilyData.People[0].Age <= maxAge;
    } else if (minAge.HasValue) {
        isMatchAge = importedFamilyData.People[0].Age >= minAge;
    } else if (maxAge.HasValue) {
        isMatchAge = importedFamilyData.People[0].Age <= maxAge;
    }

    if (isMatchAge && isMatchPortfolio) {
        Console.WriteLine($"{topic},${importedFamilyData.Value},{importedFamilyData.Accounts.Count},{importedFamilyData.People[0].Age}"+(importedFamilyData.People[1].Age != null ? $",{importedFamilyData.People[1].Age}" : ",") + $",'{title}'");

        if (showDetails) {
            foreach (var account in importedFamilyData.Accounts) {
                Console.WriteLine(account.AccountType + " at " + account.Custodian);
                foreach (var investment in account.Investments) {
                    Console.WriteLine($"  {FormatUtilities.formatPercent3(investment.Value/importedFamilyData.Value*100.0)} {investment.Name} ({investment.Ticker}) ({investment.ExpenseRatio}%)");
                }
            }

            Console.WriteLine();
        }

        if (importedFamilyData.Title != null && debug) {
            Console.WriteLine(importedFamilyData.Title + "\n");
        }
    }
}

static async Task ProcessForumPages(int start, int pages, double? minPortfolio, double? maxPortfolio, int? minAge, int? maxAge, string? filter, bool debug, bool showDetails, IAppData appData, IList<Fund> funds) {
    HttpClient httpClient = new();
    
    for (int i=start; i<start+pages; i++) {
        string? topics = await ForumUtilities.GetTopicsFromForum("1", i*50, filter);
        var topicLines = topics.Split('\n');
        Console.WriteLine($"start page:{i}  post:{i*50}");

        await ProcessTopicLines(topicLines, minPortfolio, maxPortfolio, minAge, maxAge, debug:debug, showDetails:showDetails, appData, funds);
    }
}

static async Task ProcessTopicLines(string[] topicLines, double? minPortfolio, double? maxPortfolio, int? minAge, int? maxAge, bool debug, bool showDetails, IAppData appData, IList<Fund> funds) {
    foreach (var topicLine in topicLines) {
        var spaceLoc = topicLine.IndexOf(" ");
        if (spaceLoc > -1) {
            var topic = topicLine[0..spaceLoc];
            var title = topicLine[(spaceLoc + 1)..];

            if (topic != "6212") { // don't parse "asking portfolio questions"
                // Console.WriteLine(topic + " " + title);
                await ProcessTopic(topic, title, minPortfolio, maxPortfolio, minAge, maxAge, debug:debug, showDetails, appData, funds);
            }
        }
    }
}

static async Task ProcessTopics(double? minPortfolio, double? maxPortfolio, int? minAge, int? maxAge, bool debug, IAppData appData, IList<Fund> funds) {
        string topicList = "409135 portfolio review - NYC 54/53\n"
        + "408853 Portfolio Review - 38 y/o Consultant, Soon to be Married\n"
        + "408867 Portfolio review for a 36 year old new poster\n"
        + "408844 Portfolio Review - used space before units '1.35 M'\n"
        + "407986 Portfolio advice: Simplifying, Planning, and (hopefully) retiring one day\n"
        + "408865 Long-time Boglehead - 2023 Portfolio Refresh\n"
        + "348471 Portfolio review (2 year update) - > 100% by design \n"
        + "385354 Portfolio Question -- Poor harvesting by design\n"
        + "409223 Portfolio Review Request - 1 Year Post Edward Jones To DIY Portfolio -- Poor harvesting by design\n"
        + "404458 TODO - data looks mostly good...nothing harvested\n"
        + "406482 TODO - portfolio size parsing problem\n"
        + "409111 TODO - misses Taxable section, so less than 100%\n"
        + "396366 TODO portfolio size way off\n"
        + "391553 TODO this person assigned 100% to each account"
        + "401670 TODO perhaps stop parsing accounts after reached 100%"
        + "407956 Portfolio Sanity Check -- TODO - misses assets $$ \n"
        + "408103 Portfolio review – before life gets more expensive! -- TODO - cannot parse badly formatted investments\n"
        + "408017 Portfolio Check-up / New to this -- TODO - sholld be able to harvest retirement assets\n"
        + "408057 Portfolio review for the first time - thanks -- TODO - should 2.8% iBond harvest better?\n"
        + "78881 A young boglehead portfolio review - TODO - doesn't yet harvest 'just north of $50k'\n"
        + "407996 portfolio review - TODO - misses 2nd - 7th accounts \n";

        var topicLines = topicList.Split('\n');
        await ProcessTopicLines(topicLines, minPortfolio, maxPortfolio, minAge, maxAge, debug:debug, showDetails:false, appData, funds);
}
