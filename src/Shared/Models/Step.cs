    public class Step
    {
        public string? step { get; set; }
        public string? title { get; set; }
        public string? diagramTitle { get; set; }
        public string? bestDiagramTitle {
            get {
                if (diagramTitle != null) { return diagramTitle; }
                else { return title; }
            }
        }
        public int number {get; set;}
        public string? level {get; set;}
        public string? summary { get; set; }
        public string? priority { get; set; }
        public string? returns { get; set; }
        public string? description { get; set; }
    }