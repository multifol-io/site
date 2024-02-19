public interface ISearchModel {
    public string Terms { get; set; }
    public string Engine { get; set; }
    public string Domain { get; set; }
    public int Location { get; set; }
    public string BogleheadsUrl { get; }

    public string UrlPattern { get; set; }
    public string CalculatedUrl { get; }
}