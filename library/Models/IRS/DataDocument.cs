namespace IRS
{
    public class DataDocument 
    {
        public string? name { get; set; }
        public string? repository { get; set; }
        public string? path { get; set; }
        public string? version { get; set; }
        public string? note { get; set; }
        public List<Source>? source { get; set; }
    }
}