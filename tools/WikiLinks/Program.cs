using System.Collections.Concurrent;
using System.Text.Json;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Sites;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

string linkJsonLocation = "c:\\linkJson";
string wikiUrl = "https://bogleheads.org/w/api.php";
int amountOfItems = 2000; // todo: don't hardcode this
HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
WikitextParser parser = new WikitextParser();
bool showSuccesses = false; //if set to true, will list all pages (even if they have no external links), and all external links, even if they are OK.
bool showLinkJson = false;
bool includeTalk = false;

var client = new WikiClient();
var wikiSite = new WikiSite(client, wikiUrl);
await wikiSite.Initialization;

await ProcessAllPages(wikiSite);

async Task<JsonDocument> GetLinksJsonDocument(string pageTitle, string filePath) {
    if (!File.Exists(filePath)) {
        var linkInfoUrl = $"https://www.bogleheads.org/w/api.php?action=parse&page="+pageTitle+"&prop=wikitext%7Cexternallinks&format=json";
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

async Task ProcessPage(WikiPage page, bool talkNamespace = false) {
    string? pageTitle = null;
    int pageId = -1;

    if (page != null) {
        pageTitle = (talkNamespace ? "Talk:" : "") + page?.Title;
        pageId = page?.Id ?? -1;
    } else {
        return;
    }

    bool? handlePage = true; //page?.Title?.StartsWith("Talk:");
    if (handlePage != null && !handlePage.Value) return;
    var filePathBase = Path.Combine(linkJsonLocation,pageTitle.Replace("/","--").Replace("*","--").Replace("?",""));
    var linksFilePath = filePathBase+".json";
    string resultsFileName = filePathBase+".results.json";
    ConcurrentDictionary<string,string> linkResults = new();
    bool headerShown = false;

    if (!File.Exists(resultsFileName)) {
        JsonDocument doc = await GetLinksJsonDocument(pageTitle, linksFilePath);
        JsonElement parseElement = new JsonElement(); //weird
        try {
            bool foundPage = doc.RootElement.TryGetProperty("parse", out parseElement);
            if (!foundPage) {
                return;
            }
        } catch (Exception e) {
            ShowTitle(pageTitle, linkJson:showLinkJson?File.ReadAllText(linksFilePath):null);
            Console.WriteLine("    PROGRAM ERROR: " + e.Message + "\n" + e.StackTrace);
            Console.WriteLine();
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
                if (!headerShown) {
                    Console.WriteLine(pageTitle + "   " +  "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
                    headerShown = true;
                }
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
                            if (!headerShown) {
                                ShowTitle(pageTitle, linkJson:showLinkJson?File.ReadAllText(linksFilePath):null);
                                headerShown = true;
                            }
                            linkResults.TryAdd(linkUrl,"OK:Known As Dead");
                        }
                    } else {
                        headerShown = await ProcessLink(pageTitle, linkUrl, headerShown, linksFilePath, linkResults);
                    }
                }).ToList();
                await Task.WhenAll(tasks);
            }
        } else if (showSuccesses) {
            ShowTitle(pageTitle, linkJson:showLinkJson?File.ReadAllText(linksFilePath):null);
            Console.WriteLine("    No External Links");
            Console.WriteLine();
        }
    } else {
        var resultsJson = File.ReadAllText(resultsFileName);
        linkResults = JsonSerializer.Deserialize<ConcurrentDictionary<string,string>>(resultsJson);
        if (!headerShown) {
            Console.WriteLine(pageTitle + "   " +  "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
            headerShown = true;
        }
    }

    foreach (var entry in linkResults) {
        Console.WriteLine("    "+entry.Value + "    " + entry.Key);
    }

    if (linkResults != null) {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(linkResults, options);
        File.WriteAllText(resultsFileName, jsonString);
    }

    if (headerShown) {
        Console.WriteLine();
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
    var allPages = new AllPagesGenerator(wikiSite) 
        {
            StartTitle = (debugStart != "" ? debugStart : "!"),
            EndTitle = (debugEnd != "" ? debugEnd : null)
        };

    var provider = WikiPageQueryProvider.FromOptions(PageQueryOptions.None);
    var pages = await allPages.EnumPagesAsync(provider).Take(amountOfItems).ToArrayAsync();

    foreach (var page in pages) {
        await ProcessPage(page);
        if (includeTalk) {
            await ProcessPage(page, talkNamespace:true);
        }
    }
}

async Task<bool> ProcessLink(string? pageTitle, string? linkUrl, bool headerShown, string filePath, ConcurrentDictionary<string, string> linkResults) {
    if (linkUrl != null && linkUrl.StartsWith("//")) {
        linkUrl = "https:" + linkUrl;
    }

    (HttpResponseMessage? response, Exception? e) = await FetchUrl(linkUrl);

    if (response == null || !response.IsSuccessStatusCode) {
        if (!headerShown) {
            ShowTitle(pageTitle, linkJson:showLinkJson?File.ReadAllText(filePath):null);
            headerShown = true;
        }

        if (response == null) {
            var message = e?.Message;
            if (message != null && message.Contains("inner exception")) {
                message = e?.InnerException?.Message;
            }

            if (message!.StartsWith("No such host is known.")) {
                linkResults.TryAdd(linkUrl!,"ERROR:" + message!);
            } else if (message!.StartsWith("The remote certificate is invalid because of errors in the certificate chain: NotTimeValid")) {
                if (showSuccesses) {
                    linkResults.TryAdd(linkUrl!,"OK?:" + message!);
                }
            } else if (message!.StartsWith("The remote certificate is invalid")) {
                linkResults.TryAdd(linkUrl!,"ERROR:" + message!);                
            } else if (message!.StartsWith("The request was canceled due to the configured HttpClient.Timeout")) {
                linkResults.TryAdd(linkUrl!,"ERROR:" + message!);
            } else {
                linkResults.TryAdd(linkUrl!,message!);
            }
        } else {
            if (response.Headers.Location != null) {
                linkResults.TryAdd(linkUrl!,"UPDATE LINK:redirected to " + response.Headers.Location);
            } else {
                switch (response.ReasonPhrase) {
                    case "Bad Request":
                        linkResults.TryAdd(linkUrl!,response.ReasonPhrase);
                        break;
                    case "Too Many Requests":
                        if (linkUrl != null) {
                            if ((linkUrl.StartsWith("http://ssrn.com/abstract") || linkUrl.StartsWith("http://papers.ssrn.com"))) {
                                if (showSuccesses) {
                                    linkResults.TryAdd(linkUrl!,"OK ssrn.com:" + response.ReasonPhrase);
                                }
                            } else {
                                linkResults.TryAdd(linkUrl!,"WARNING:" + response.ReasonPhrase);
                            }
                        }
                        break;
                    // These reasons should be ignored, not a problem that requires a URL change.
                    case "Unauthorized": 
                    case "Internal Server Error":
                        if (showSuccesses) {
                            linkResults.TryAdd(linkUrl!,"OK:" + response.ReasonPhrase!);
                        }
                        break;
                    case "Service Temporarily Unavailable":
                        linkResults.TryAdd(linkUrl!,"WARNING:" + response.ReasonPhrase!);
                        break;
                    default:
                        if (response.ReasonPhrase != null) {
                                switch (response.ReasonPhrase) {
                                    case "Not Found":
                                        linkResults.TryAdd(linkUrl!,"ERROR:"+response.ReasonPhrase!);
                                        break;
                                    default:
                                        linkResults.TryAdd(linkUrl!,response.ReasonPhrase!);
                                        break;
                                }
                        }
                        break;
                }
            }
        }
    } else if (showSuccesses) {
        if (!headerShown) {
            ShowTitle(pageTitle, linkJson:showLinkJson?File.ReadAllText(filePath):null);
            headerShown = true;
        }

        linkResults.TryAdd(linkUrl!,"OK");
    }

    return headerShown;
}

void ShowTitle(string? pageTitle, string? linkJson = null) {
    //Console.WriteLine(pageTitle + "   " + "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
    if (linkJson != null) {
        Console.WriteLine(linkJson);
    }
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
