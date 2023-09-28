using System.Text.Json;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Sites;

string wikiUrl = "https://bogleheads.org/w/api.php";
int amountOfItems = 2000; // todo: don't hardcode this
HttpClient httpClient = new();
bool showSuccesses = false; //if set to true, will list all pages (even if they have no external links), and all external links, even if they are OK.

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

    var linkInfoUrl = $"https://www.bogleheads.org/w/api.php?action=query&titles="+pageTitle+"&prop=links%7Cextlinks&pllimit=max&format=json";
    var jsonResponse = await httpClient.GetAsync(linkInfoUrl);
    var doc = await JsonDocument.ParseAsync(await jsonResponse.Content.ReadAsStreamAsync());
    JsonElement pageInfo = new JsonElement(); //weird
    try {
        JsonElement pagesElement = doc.RootElement.GetProperty("query").GetProperty("pages");
        bool pageFound = false;
        foreach (var property in pagesElement.EnumerateObject()) {
            pageInfo = property.Value;

            if (property.Name != "-1") {
                pageFound = true;
            }
        }

        if (!pageFound) return;
    } catch (Exception e) {
        Console.WriteLine(pageTitle + "   " + "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
        Console.WriteLine(await jsonResponse.Content.ReadAsStringAsync());
        Console.WriteLine("    PROGRAM ERROR: " + e.Message);
        Console.WriteLine();
        return;
    }

    var hasExtLinks = pageInfo.TryGetProperty("extlinks", out JsonElement extLinks);
    if (hasExtLinks) {
        bool headerShown = false;
        foreach (var link in extLinks.EnumerateArray()) {
            var linkUrl = link.GetProperty("*").GetString();
            headerShown = await ProcessLink(pageTitle, linkUrl, headerShown);
        }

        if (headerShown) {
            Console.WriteLine();
        }
    } else if (showSuccesses) {
        Console.WriteLine(pageTitle + "   " + "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
        Console.WriteLine("    No External Links");
        Console.WriteLine();
    }   
}

async Task ProcessAllPages(WikiSite wikiSite) {    
    var allPages = new AllPagesGenerator(wikiSite) {};

    var provider = WikiPageQueryProvider.FromOptions(PageQueryOptions.None);
    var pages = await allPages.EnumPagesAsync(provider).Take(amountOfItems).ToArrayAsync();

    foreach (var page in pages) {
        await ProcessPage(page);
        await ProcessPage(page, talkNamespace:true); 
    }
}

async Task<bool> ProcessLink(string? pageTitle, string? linkUrl, bool headerShown) {
    if (linkUrl != null && linkUrl.StartsWith("//")) {
        linkUrl = "https:" + linkUrl;
    }

    (HttpResponseMessage? response, Exception? e) = await FetchUrl(linkUrl);

    if (response == null || !response.IsSuccessStatusCode) {
        if (!headerShown) {
            Console.WriteLine(pageTitle + "   " + "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
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
                if (response.ReasonPhrase == "Bad Request" || response.ReasonPhrase == "Internal Server Error") {
                    var newUrl = linkUrl?.Replace("http://", "https://");
                    (HttpResponseMessage? response2, Exception? e2) = await FetchUrl(newUrl);
                    if (response2 != null && response2.IsSuccessStatusCode) {
                        Console.WriteLine("    Update to use https. " + "    " + linkUrl);
                    } else {
                        Console.WriteLine("    " + response.ReasonPhrase + "    " + linkUrl);
                    }
                } else {
                    Console.WriteLine("    " + response.ReasonPhrase + "    " + linkUrl);
                }
            }
        }
    } else if (showSuccesses) {
        if (!headerShown) {
            Console.WriteLine(pageTitle + "   " + "https://bogleheads.org/wiki/" + pageTitle?.Replace(" ","%20"));
            headerShown = true;
        }

        Console.WriteLine("    OK    " + linkUrl);
    }


    return headerShown;
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
