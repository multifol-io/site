using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;
using Azure.Storage.Blobs;
using Microsoft.JSInterop;
using System.Text;

public class EmployerPlan : INotifyPropertyChanged {
    public EmployerPlan(Person person) {
        this.person = person;
        this.Employer401k = this.person.TaxFiling.IRSRetirement.Employer401k;
    }

    private Person person;
    private IRS.Employer401k? Employer401k;

    private async Task GetEmployerDataAsync(string employer) {
        HttpClient httpClient = new();
        var lEmployer = employer.ToLowerInvariant().Trim();
        var year = person.TaxFiling.Year;
    
        if (lEmployer == "test") {
            var requestUri = "https://api.saving.bogle.tools/api/UploadEmployerInfo?code=8VyPqHmGuPDZq6G2tmbJ7g0vN9BqIQhSGmRA-jBBEInkAzFuUvlxuA==";
            
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri))
            {
                var body = new Employer.Employer()
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

        var employerStream = await httpClient.GetStreamAsync(
            "https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/employers/" 
            + lEmployer + "/" + lEmployer + ".retirement." + year + ".json");
        var employerData = JsonSerializer.Deserialize<Employer.Employer>(employerStream);
        person.Employer = employerData;
    }

    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private string _employer;
    public string Employer { 
        get{ return _employer; }
        set{
            _employer = value;
            GetEmployerDataAsync(_employer);
        }
    }

    private bool _eligible;
    public bool Eligible {
        get => _eligible;
        set => SetProperty(ref _eligible, value);
    }

    public bool NotEligible {get {return !Eligible;}}

    public int? AnnualSalary { get; set; }
    public int? MatchA { get; set; }
    public int? MatchALimit { get; set; }
    public int? MatchB { get; set; }
    public int? MatchBLimit { get; set; }
    public int? MaxMatch { get; set; }
    public int? AmountToSaveForMatch { 
        get
        {
            double? contribution = null;
            if (MatchA != null) {
                double matchALimit = (MatchALimit ?? 100) / 100.0;
                double matchBLimit = (MatchBLimit ?? 100) / 100.0;

                if (MatchA > 0 && matchALimit == 1.0) contribution = (MaxMatch ?? 0.0) / (MatchA / 100.0);
                if (MatchA > 0 && matchALimit < 1.0) contribution = AnnualSalary * matchALimit * (MatchA / 100.0);
                if (MatchB != null) {
                    if (MatchB > 0 && matchBLimit == 1.0) contribution += MaxMatch / (MatchB / 100.0);
                    if (MatchB > 0 && matchBLimit < 1.0) contribution += AnnualSalary * (matchBLimit-matchALimit);
                }
            }

            return (int?)contribution;
        }
    }
    public int? MatchAmount { 
        get
        {
            double? match = null;
            if (MatchA != null) {
                var percentA = (MatchA ?? 0) / 100.0;
                var matchALimit = (MatchALimit ?? 100.0) / 100.0;
                match = percentA * AnnualSalary * matchALimit;
            }
            if (MatchB != null) {
                var percentB = (MatchB ?? 0) / 100.0;
                var matchALimit = (MatchALimit ?? 100.0) / 100.0;
                var matchBLimit = (MatchBLimit ?? 100.0) / 100.0;
                match += percentB * AnnualSalary * (matchBLimit - matchALimit);
            }

            if (match > MaxMatch) match = MaxMatch;

            return (int?)match;
        }
    }
    public bool CompleteMatched { get { return AmountToSaveForMatch != null; } }
    public bool CompleteUnmatched { get { return AmountToSaveForNonMatched != null; } }

    public int? AmountToSaveForNonMatched {
        get { return ContributionAllowed - (AmountToSaveForMatch ?? 0); }
    }

    public int? AmountToSaveForBackdoorRoth { get; set; }

    public int? ContributionAllowed { 
        get
        {
            if (Eligible)
            {
                return Employer401k.ContributionLimit
                    + (person.FiftyOrOver ? Employer401k.CatchUpContributionLimit : 0);
            }
            else
            {
                return null;
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new 
                         PropertyChangedEventArgs(propertyName));
    }

    bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string 
                                                     propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}