namespace Employer {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Employer>(myJsonResponse);
    public class Employer401k
    {
        public bool Offered { get; set; }
        public MatchRules MatchRules { get; set; }
        public int MatchLimit { get; set; }
    }

    public class HSA
    {
        public int EmployerContribution { get; set; }
    }

    public class MatchRules
    {
        public int MatchPercentage { get; set; }
    }

    public class MegaBackdoorRoth
    {
        public int ContributionLimit { get; set; }
    }

    public class RetirementSavings
    {
        public int Year { get; set; }
        public Employer401k Employer401k { get; set; }
        public HSA HSA { get; set; }
        public MegaBackdoorRoth MegaBackdoorRoth { get; set; }
    }

    public class Employer
    {
        public string name { get; set; }
        public string repository { get; set; }
        public string path { get; set; }
        public string version { get; set; }
        public List<string> note { get; set; }
        public List<object> source { get; set; }
        public string Company { get; set; }
        public RetirementSavings RetirementSavings { get; set; }
    }
}
