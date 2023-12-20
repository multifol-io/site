using IRS;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Employer {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Employer>(myJsonResponse);
    public class Employer401k
    {
        public TriState Offered { get; set; } = TriState.ChoiceNeeded;
        private List<MatchRule>? _MatchRules;
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
        public TriState HighDeductibleHealthPlanAvailable { get; set; }
        private List<EmployerContributionLevel>? _EmployerContributionLevels; 
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
        public string? Description { get; set; }
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
        // back pointer
        [JsonIgnore]
        public object? person { get; set; }

        private string? _Company;
        public string? Company { 
            get { return _Company; }
            set { 
                _Company = value;
            }
        }

        [JsonIgnore]
        public bool Downloaded { get; set; }
        [JsonIgnore]
        public bool Complete {
            get {
                return Employer401k.Offered != TriState.ChoiceNeeded && HSA.HighDeductibleHealthPlanAvailable != TriState.ChoiceNeeded;
            }
        }
        public Employer401k Employer401k { get; set; } = new();
        public HSA HSA { get; set; } = new();
        public MegaBackdoorRoth MegaBackdoorRoth { get; set; } = new();
    
        public async Task GetEmployerDataAsync(int year) {
            var employer = Company;
            HttpClient httpClient = new();
            var lEmployer = employer?.ToLowerInvariant().Trim();
        
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
                
                var options = new JsonSerializerOptions()
                {
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                };
                                   
                var employerData = JsonSerializer.Deserialize<Employer.EmployerBenefits>(employerStream, options);

                if (employerData != null)
                {
                    Employer401k = employerData.Employer401k;
                    HSA = employerData.HSA;
                    MegaBackdoorRoth = employerData.MegaBackdoorRoth;
                    Downloaded = true;
                    Console.WriteLine("employer data was found " + lEmployer);
                }
                else
                {
                    Employer401k = new();
                    HSA = new();
                    MegaBackdoorRoth = new();
                    Downloaded = false;
                    Console.WriteLine("employer data was null for " + lEmployer);
                }
            }
            catch (Exception e)
            {
                Employer401k = new();
                HSA = new();
                MegaBackdoorRoth = new();
                Downloaded = false;
                Console.WriteLine("employer data load failed for " + lEmployer + "\n" + e.Message);
            }
        }
    }
}
