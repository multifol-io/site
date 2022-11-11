public class SearchModel {
    public string Terms { get; set; } = "";
    public string Engine { get; set; } = "";
    public string Domain { get; set; } = "ask.com";
    public int Location { get; set; } = 2;
    public string BogleheadsUrl {
        get {
            switch (Location) {
                case 1: 
                    return "bogleheads.org";
                case 2: 
                    return "bogleheads.org/forum";
                case 3: 
                    return "bogleheads.org/wiki";
                case 4:
                    return "bogleheads.org/blog";
                default:
                    return "bogleheads.org";
            }
        }
    }

    public string UrlPattern { get; set; } = "/web?q=[terms]   [site]";
    public string CalculatedUrl { 
        get {
            string url = "";
            switch (Engine.ToLower()) {
                case "bing":
                    url = "https://bing.com/search?q=[terms]   [site]";
                    break;
                case "google":
                    url = "https://google.com/search?q=[terms]   [site]";
                    break;
                case "duckduckgo":
                    url = "https://duckduckgo.com?q=[terms]   [site]";
                    break;
                case "brave":
                    url = "https://search.brave.com/search?q=[terms]   [site]";
                    break;
                default:
                    url = "https://" + Domain + UrlPattern;
                    break;
            }

            var searchUrl = url.Replace("[terms]", Terms).Replace("[site]", "site:" + BogleheadsUrl);
            return searchUrl;
        }
    }
}