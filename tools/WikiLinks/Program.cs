using System.Text.Json;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Sites;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

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
    var linkInfoUrl = $"https://www.bogleheads.org/w/api.php?action=parse&page="+pageTitle+"&prop=wikitext%7Cexternallinks&format=json";
    var jsonResponse = await httpClient.GetAsync(linkInfoUrl);
    var doc = await JsonDocument.ParseAsync(await jsonResponse.Content.ReadAsStreamAsync());
    JsonElement parseElement = new JsonElement(); //weird
    try {
        bool foundPage = doc.RootElement.TryGetProperty("parse", out parseElement);
        if (!foundPage) {
            return;
        }
    } catch (Exception e) {
        ShowTitle(pageTitle, linkJson:showLinkJson?await jsonResponse.Content.ReadAsStringAsync():null);
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

        bool headerShown = false;
        foreach (var link in extLinks.EnumerateArray()) {
            var linkUrl = link.GetString();
            bool deadLinkKnown = false;
            if (parserTags != null) {
                foreach (var parserTag in parserTags) {
                    if (parserTag.Contains("|url="+linkUrl+" ") && parserTag.Contains("|url-status=dead ")) {
                        deadLinkKnown = true;
                    }
                }
            }
            
            if (deadLinkKnown) {
                if (showSuccesses) {
                    if (!headerShown) {
                        ShowTitle(pageTitle, linkJson:showLinkJson?await jsonResponse.Content.ReadAsStringAsync():null);
                        headerShown = true;
                    }
                    Console.WriteLine("    Known As Dead    " + linkUrl);
                }
            } else {
                headerShown = await ProcessLink(pageTitle, linkUrl, headerShown, jsonResponse);
            }
        }

        if (headerShown) {
            Console.WriteLine();
        }
    } else if (showSuccesses) {
        ShowTitle(pageTitle, linkJson:showLinkJson?await jsonResponse.Content.ReadAsStringAsync():null);
        Console.WriteLine("    No External Links");
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
    var debugEnd = "";
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

async Task<bool> ProcessLink(string? pageTitle, string? linkUrl, bool headerShown, HttpResponseMessage jsonResponse) {
    if (linkUrl != null && linkUrl.StartsWith("//")) {
        linkUrl = "https:" + linkUrl;
    }

    (HttpResponseMessage? response, Exception? e) = await FetchUrl(linkUrl);

    if (response == null || !response.IsSuccessStatusCode) {
        if (!headerShown) {
            ShowTitle(pageTitle, linkJson:showLinkJson?await jsonResponse.Content.ReadAsStringAsync():null);
            headerShown = true;
        }

        if (response == null) {
            var message = e?.Message;
            if (message != null && message.Contains("inner exception")) {
                message = e?.InnerException?.Message;
            }

            Console.WriteLine("    " + message + "    " + linkUrl);                            
        } else {
            if (response.Headers.Location != null) {
                Console.WriteLine("    redirected to " + response.Headers.Location + "    from " + linkUrl);
            } else {
                switch (response.ReasonPhrase) {
                    case "Bad Request":
                        var newUrl = linkUrl?.Replace("http://", "https://");
                        (HttpResponseMessage? response2, Exception? e2) = await FetchUrl(newUrl);
                        if (response2 != null && response2.IsSuccessStatusCode) {
                            Console.WriteLine("    Update to use https. " + "    " + linkUrl);
                        } else {
                            Console.WriteLine("    " + response.ReasonPhrase + "    " + linkUrl);
                        }
                        break;
                    case "Too Many Requests":
                        if (linkUrl != null && (!(linkUrl.StartsWith("http://ssrn.com/abstract") || linkUrl.StartsWith("http://papers.ssrn.com")))) {
                            Console.WriteLine("    " + response.ReasonPhrase + "    " + linkUrl);
                        }
                        break;
                    // These reasons should be ignored, not a problem that requires a URL change.
                    case "Unauthorized": 
                    case "Internal Server Error":
                        break;
                    default:
                        Console.WriteLine("    " + response.ReasonPhrase + "    " + linkUrl);
                        break;
                }
            }
        }
    } else if (showSuccesses) {
        if (!headerShown) {
            ShowTitle(pageTitle, linkJson:showLinkJson?await jsonResponse.Content.ReadAsStringAsync():null);
            headerShown = true;
        }

        Console.WriteLine("    OK    " + linkUrl);
    }

    return headerShown;
}

void ShowTitle(string? pageTitle, string? linkJson = null) {
    Console.WriteLine(pageTitle + "   " + "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
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
