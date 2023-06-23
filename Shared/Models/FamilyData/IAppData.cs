public interface IAppData {
    public FamilyData FamilyData { get; set; }
    public List<string> ProfileNames {get; set;}
    public string CurrentProfileName {get; set;}
    public string CurrentProfileKey { get; }
    public string LastPageUri {get; set;}


}