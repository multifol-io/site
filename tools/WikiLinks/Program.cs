using System.Collections.Concurrent;
using System.Text.Json;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Sites;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

string linkJsonLocation = @"c:\linkJson";
string wikiApiUrl = "https://bogleheads.org/w/api.php";
string wikiBaseUrl = "https://bogleheads.org/wiki/";

int amountOfItems = 2000; // todo: don't hardcode this
HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
WikitextParser parser = new WikitextParser();

bool showSuccesses = false; //if set to true, will list all pages (even if they have no external links), and all external links, even if they are OK.
bool forceFetch = false;
bool outputCSV = false;

var client = new WikiClient();
var wikiSite = new WikiSite(client, wikiApiUrl);
await wikiSite.Initialization;

await ProcessAllPages(wikiSite);

async Task<JsonDocument> GetLinksJsonDocument(string pageTitle, string filePath) {
    if (!File.Exists(filePath)) {
        var linkInfoUrl = $"{wikiApiUrl}?action=parse&page={pageTitle}&prop=wikitext%7Cexternallinks&format=json";
        var jsonResponse = await httpClient.GetAsync(linkInfoUrl);
        var fileStream = File.Create(filePath);
        (await jsonResponse.Content.ReadAsStreamAsync()).CopyTo(fileStream);
        fileStream.Close();
    }
    
    using (FileStream fs = File.Open(filePath, FileMode.Open)) {
        var doc = await JsonDocument.ParseAsync(fs);
        return doc;
    }
}

async Task ProcessPage(WikiPage page, Dictionary<string, string> linkInfos, bool talkNamespace = false) {
    string? pageTitle = null;
    int pageId = -1;

    if (page != null) {
        pageTitle = (talkNamespace ? "Talk:" : "") + page?.Title;
        pageId = page?.Id ?? -1;
    } else {
        return;
    }

    bool? handlePage = true;
    if (handlePage != null && !handlePage.Value) return;
    var escapedPageTitle = pageTitle.Replace("/","--").Replace("*","--").Replace("?","");
    var filePathBase = Path.Combine(linkJsonLocation,escapedPageTitle);

    var linksFilePath = filePathBase+".json";
    string resultsFileName = filePathBase+".results.json";
    ConcurrentDictionary<string,string> linkResults = new();

    if (!File.Exists(resultsFileName) || forceFetch) {
        JsonDocument doc = await GetLinksJsonDocument(pageTitle, linksFilePath);
        JsonElement parseElement = new JsonElement(); //weird
        try {
            bool foundPage = doc.RootElement.TryGetProperty("parse", out parseElement);
            if (!foundPage) {
                return;
            }
        } catch (Exception e) {
            Console.WriteLine(pageTitle + "," +  wikiBaseUrl + pageTitle?.Replace(" ","%20")+" ,"+"PROGRAM ERROR: " + e.Message + "\n" + e.StackTrace);
            return;
        }

        var hasExtLinks = parseElement.TryGetProperty("externallinks", out JsonElement extLinks);
        var hasWikiText = parseElement.TryGetProperty("wikitext", out JsonElement wikitext);

        if (hasExtLinks) {
            var text = wikitext.GetProperty("*").GetString();
            var ast = parser.Parse(text);
            var parserTags = HarvestAst(ast, 0, parser);

            var extlinksArray = extLinks.EnumerateArray().ToList();
            if (extlinksArray.Count > 0) {
                var tasks = extlinksArray.Select(async link =>
                {
                    var linkUrl = link.GetString()!;
                    bool deadLinkKnown = false;
                    if (parserTags != null) {
                        foreach (var parserTag in parserTags) {
                            var chunks = parserTag.Split('|');
                            bool urlMatch = false;
                            bool deadLink = false;
                            foreach (var chunk in chunks) {
                                if (chunk.Trim() == "url="+linkUrl) {
                                    urlMatch = true;
                                }
                                if (chunk.Trim() == "url-status=dead") {
                                    deadLink = true;
                                }
                            }

                            if (urlMatch && deadLink) {
                                deadLinkKnown = true;
                            }
                        }
                    }
                    
                    if (deadLinkKnown) {
                        if (showSuccesses) {
                            linkResults.TryAdd(linkUrl,"OK,Known As Dead");
                        }
                    } else {
                        await ProcessLink(pageTitle, linkUrl, linksFilePath, linkResults);
                    }
                }).ToList();
                await Task.WhenAll(tasks);
            }
        }
    } else {
        var resultsJson = File.ReadAllText(resultsFileName);
        if (resultsJson != null) {
            linkResults = JsonSerializer.Deserialize<ConcurrentDictionary<string,string>>(resultsJson)!;
        }
    }

    SortedDictionary<string, string> sortedLinkResults = new SortedDictionary<string, string>();
    foreach (var entry in linkResults) {
        sortedLinkResults.Add(entry.Key,entry.Value);
    }

    foreach (var entry in sortedLinkResults!) {
        string value = entry.Value;
        string[] values = entry.Value.Split(',');

        if (values.Length > 1) {
            if (values[1].StartsWith("The request was canceled due to the configured HttpClient.Timeout")) {
                values[1] = "HttpClient Timeout - " + httpClient.Timeout;
            } else if (values[1].StartsWith("No such host is known.")) {
                values [1] = "Hostname not resolved in DNS.";
            }

            value = $"{values[0]},{values[1]}";
        }

        linkInfos.Add(entry.Key, value);

        if (outputCSV) { 
            string uriHost;
            try {
                var uri = new Uri(entry.Key);
                uriHost = uri.Host;
            } catch (Exception) {
                uriHost = "";
            }

            Console.WriteLine(pageTitle + "," +  wikiBaseUrl + pageTitle?.Replace(" ","%20")+" ,"+entry.Value + ",\"" + entry.Key+"\"," + uriHost);
        }
    }

    if (sortedLinkResults != null) {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(sortedLinkResults, options);
        File.WriteAllText(resultsFileName, jsonString);
    }
}

static List<string>? HarvestAst(Node node, int level, WikitextParser parser, bool showAll = false)
{
    List<string>? ParserTags = null;
    var indension = new string('.', level);
    var ns = node.ToString();
    bool isParserTag = node.GetType().Name == "ParserTag";
    if (showAll || isParserTag) {
        //Console.WriteLine("{0,-20} [{1}]", indension + node.GetType()?.Name, Escapse(ns));
        if (isParserTag) {
            ParserTags = new();
            var result = Escape(ns);
            if (result != null) {
                ParserTags.Add(result);
            }
        }
    }
    foreach (var child in node.EnumChildren()) {
        var parserTags = HarvestAst(child, level + 1, parser);
        if (parserTags != null) {
            if (ParserTags == null) {
                ParserTags = new();
            }

            ParserTags.AddRange(parserTags);
        }
    }
    return ParserTags;
}

static string? Escape(string? expr)
{
    return expr?.Replace("\r", "\\r").Replace("\n", "\\n");
}

async Task ProcessAllPages(WikiSite wikiSite) {    
    var debugStart = "";
    var debugEnd = debugStart;
    Scan scanInfo = new("github.com/multifol-io/site/tree/main/tools/WikiLinks",DateTime.Now);

    var allPages = new AllPagesGenerator(wikiSite) 
        {
            StartTitle = (debugStart != "" ? debugStart : "!"),
            EndTitle = (debugEnd != "" ? debugEnd : null)
        };

    var provider = WikiPageQueryProvider.FromOptions(PageQueryOptions.None);
    var pages = await allPages.EnumPagesAsync(provider).Take(amountOfItems).ToArrayAsync();

    if (outputCSV) {
        Console.WriteLine("Page,PageUrl,LinkStatus,LinkStatusDetail,LinkUrl,LinkDomain");
    }

    foreach (var page in pages) {
        Dictionary<string, string> results = new();
        await ProcessPage(page, results, talkNamespace:false);
        if (results.Count > 0) {
            scanInfo.Results.Add(page.Title!,results);
        }
    }

    var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    string scanInfoJson = JsonSerializer.Serialize<Scan>(scanInfo, options);
    File.WriteAllText(@"..\..\..\bogleheads-wiki-links\brokenLinks.json", scanInfoJson);
}

async Task ProcessLink(string? pageTitle, string? linkUrl, string filePath, ConcurrentDictionary<string, string> linkResults) {
    if (linkUrl != null && linkUrl.StartsWith("//")) {
        linkUrl = "https:" + linkUrl;
    }

    Uri? uri = null;
    try {
        uri = new Uri(linkUrl!);
    } catch (Exception e2) {
        linkResults.TryAdd(linkUrl!,"ERROR," + e2.Message);
        return;
    }
    
    (HttpResponseMessage? response, Exception? e) = await FetchUrl(linkUrl);

    if (response == null || !response.IsSuccessStatusCode) {
        if (response == null) {
            var message = e?.Message!;
            if (message != null && message.Contains("inner exception")) {
                message = e?.InnerException?.Message;
            }

            message = message!.Replace(",","--");

            if (message!.StartsWith("No such host is known.")) {
                linkResults.TryAdd(linkUrl!,"ERROR," + message);
            } else if (message!.StartsWith("The remote certificate is invalid because of errors in the certificate chain: NotTimeValid")) {
                if (showSuccesses) {
                    linkResults.TryAdd(linkUrl!,"OK?," + message);
                }
            } else if (message!.StartsWith("The remote certificate is invalid")) {
                linkResults.TryAdd(linkUrl!,"ERROR," + message);                
            } else if (message!.StartsWith("The request was canceled due to the configured HttpClient.Timeout")) {
                linkResults.TryAdd(linkUrl!,"ERROR," + message);
            } else {
                linkResults.TryAdd(linkUrl!,"OTHER,"+message);
            }
        } else {
            if (response.Headers.Location != null) {
                linkResults.TryAdd(linkUrl!,"UPDATE LINK,redirected to " + response.Headers.Location);
            } else {
                switch (response.ReasonPhrase) {
                    case "Bad Request":
                        linkResults.TryAdd(linkUrl!,response.ReasonPhrase);
                        break;
                    case "Too Many Requests":
                        if (linkUrl != null) {
                            if (uri!.Host.EndsWith("ssrn.com")) {
                                if (showSuccesses) {
                                    linkResults.TryAdd(linkUrl!,"OK ssrn.com," + response.ReasonPhrase);
                                }
                            } else {
                                linkResults.TryAdd(linkUrl!,"WARNING," + response.ReasonPhrase);
                            }
                        }
                        break;
                    // These reasons should be ignored, not a problem that requires a URL change.
                    case "Unauthorized": 
                    case "Internal Server Error":
                        if (showSuccesses) {
                            linkResults.TryAdd(linkUrl!,"OK," + response.ReasonPhrase!);
                        }
                        break;
                    case "Service Temporarily Unavailable":
                        linkResults.TryAdd(linkUrl!,"WARNING," + response.ReasonPhrase!);
                        break;
                    case "Not Found":
                        linkResults.TryAdd(linkUrl!,"ERROR,"+response.ReasonPhrase!);
                        break;
                    default:
                        linkResults.TryAdd(linkUrl!,"OTHER,"+response.ReasonPhrase!);
                        break;
                }
            }
        }
    } else if (showSuccesses) {
        linkResults.TryAdd(linkUrl!,"OK");
    }

    return;
}

async Task<(HttpResponseMessage?, Exception?)> FetchUrl(string? linkUrl) {
    HttpResponseMessage? response = null;
    Exception? e = null;
    try {
        response = await httpClient.GetAsync(linkUrl);
    } catch (Exception ex) {
        e = ex;
    }

    return (response,e);
}


public class Scan {
    public Scan(string tool, DateTime timestamp) {
        Tool = tool;
        Timestamp = timestamp;
    }
    
    public string Tool { get; private set; }
    public DateTime Timestamp { get; private set; }
    public Dictionary<string, Dictionary<string,string>> Results { get; set; } = new();
}