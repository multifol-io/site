using IRS;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Employer {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Employer>(myJsonResponse);
    public class Employer401k
    {
        public TriState Offered { get; set; } = TriState.ChoiceNeeded;
        private List<MatchRule> _MatchRules;
        public List<MatchRule> MatchRules {
            get {
                if (_MatchRules == null) {
                    _MatchRules = new();
                }

                return _MatchRules;
            }
            set {
                _MatchRules = value;
            }
         }
        public int? MatchLimit { get; set; }
    }

    public class HSA
    {
        public bool HighDeductibleHealthPlanAvailable { get; set; }
        private List<EmployerContributionLevel> _EmployerContributionLevels; 
        public List<EmployerContributionLevel> EmployerContributionLevels { 
            get {
                if (_EmployerContributionLevels == null) {
                    _EmployerContributionLevels = new();
                }

                return _EmployerContributionLevels;
            }
            set {
                _EmployerContributionLevels = value;
            }
        }
    }

    public class EmployerContributionLevel {
        public string Description { get; set; }
        public int Amount { get; set; }
    }

    public class MatchRule
    {
        public int? MatchPercentage { get; set; }
        public int? ForNextPercent { get; set; }
    }

    public class MegaBackdoorRoth
    {
        public int? ContributionLimit { get; set; }
    }

    public class EmployerBenefits : DataDocument
    {
        private string _Company;
        public string Company { 
            get { return _Company; }
            set { 
                _Company = value;
            }
        }
        public int Year { get; set; }

        [JsonIgnore]
        public bool Downloaded { get; set; }
        public Employer401k Employer401k { get; set; } = new();
        public HSA HSA { get; set; } = new();
        public MegaBackdoorRoth MegaBackdoorRoth { get; set; } = new();
    
        public async Task GetEmployerDataAsync(int year) {
            var employer = Company;
            HttpClient httpClient = new();
            var lEmployer = employer.ToLowerInvariant().Trim();
        
            if (lEmployer == "test") {
                var requestUri = "https://api.saving.bogle.tools/api/UploadEmployerInfo?code=8VyPqHmGuPDZq6G2tmbJ7g0vN9BqIQhSGmRA-jBBEInkAzFuUvlxuA==";
                
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri))
                {
                    var body = new Employer.EmployerBenefits()
                    {
                        Company = lEmployer
                    };

                    string jsonString = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    HttpResponseMessage responseMsg = await httpClient.SendAsync(request).ConfigureAwait(false);

                    if (responseMsg == null)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "The response message was null when executing operation {0}.",
                                request.Method));
                    }
                }
            }

            try {
                var employerStream = await httpClient.GetStreamAsync(
                    "https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/employers/" 
                    + lEmployer + "/" + lEmployer + ".retirement." + year + ".json");
                
                var employerData = JsonSerializer.Deserialize<Employer.EmployerBenefits>(employerStream);

                if (employerData != null)
                {
                    Employer401k = employerData.Employer401k;
                    HSA = employerData.HSA;
                    MegaBackdoorRoth = employerData.MegaBackdoorRoth;
                    Downloaded = true;
                    Company = Company;
                }
                else
                {
                    Console.WriteLine("employer data was null for " + lEmployer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("message: " + e.Message);
            }
        }
    }
}
