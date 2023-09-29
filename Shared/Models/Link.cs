public class Link {
    public Link(string url, string text) {
        Url = url;
        Text = text;
    }
    public string Url { get; private set; }

    public string Text { get; private set; }
}