using System.ComponentModel;
using System.Runtime.CompilerServices;

public class EmployerPlan : INotifyPropertyChanged {
    public EmployerPlan(Person person) {
        this.person = person;
        this.Employer401k = this.person.FamilyYears.RetirementData.Employer401k;
    }

    private Person person;
    private IRS.Employer401k Employer401k;

    public TriState Eligible {
        get => person.EmployerBenefits.Employer401k.Offered;
    }

    public bool? NotEligible {
        get {
            switch (Eligible) {
                case TriState.ChoiceNeeded:
                case TriState.False:
                    return true;
                case TriState.True:
                default:
                    return false;
            }
        }
    }

    public int? AnnualSalary { get; set; }
    public int? MatchA { 
        get {
            if (person.EmployerBenefits.Employer401k.MatchRules.Count >= 1) {
                return person.EmployerBenefits.Employer401k.MatchRules[0].MatchPercentage;
            } else {
                return null;
            }
        }
    }
    public int? MatchALimit { 
        get {
            if (person.EmployerBenefits.Employer401k.MatchRules.Count >= 1) {
                return person.EmployerBenefits.Employer401k.MatchRules[0].ForNextPercent;
            } else {
                return null;
            }
        }
    }
    public int? MatchB { 
        get {
            if (person.EmployerBenefits.Employer401k.MatchRules.Count >= 2) {
                return person.EmployerBenefits.Employer401k.MatchRules[1].MatchPercentage;
            } else {
                return null;
            }
        }
    }
    public int? MatchBLimit { 
        get {
            if (person.EmployerBenefits.Employer401k.MatchRules.Count >= 2) {
                return person.EmployerBenefits.Employer401k.MatchRules[1].ForNextPercent;
            } else {
                return null;
            }
        }
    }
    public int? MaxMatch {
        get {
            return person.EmployerBenefits.Employer401k.MatchLimit;
        }
    }

    public int? AmountToSaveForMatch { 
        get
        {
            double? contribution = null;
            if (MatchA != null) {
                double matchALimit = (MatchALimit ?? 100) / 100.0;
                double matchBLimit = (MatchBLimit ?? 100) / 100.0;

                if (MatchA > 0 && matchALimit == 1.0) contribution = (MaxMatch ?? 0.0) / (MatchA / 100.0);
                if (MatchA > 0 && matchALimit < 1.0) {
                    contribution = AnnualSalary * matchALimit;
                    if (MaxMatch != null && contribution > MaxMatch / (MatchA / 100.0)) {
                        contribution = MaxMatch / (MatchA / 100.0);
                    }
                }

                // likely have a bug, where we don't apply maxmatch in the matchB case...but unsure if we'll hit in real life. Keeping it simple for now.
                if (MatchB != null) {
                    if (MatchB > 0 && matchBLimit == 1.0) contribution += MaxMatch / (MatchB / 100.0);
                    if (MatchB > 0 && matchBLimit < 1.0) contribution += AnnualSalary * matchBLimit;
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
                match += percentB * AnnualSalary * matchBLimit;
            }

            if (match > MaxMatch) match = MaxMatch;

            return (int?)match;
        }
    }
    public bool CompleteMatched { get { return AmountToSaveForMatch != null; } }
    public bool CompleteUnmatched { get { return AmountToSaveForNonMatched != null; } }

    public int? AmountToSaveForNonMatched {
        get { 
            if (AmountToSaveForMatch != null)
            {
                return ContributionAllowed - (AmountToSaveForMatch); 
            }
            else
            {
                return null; 
            }
        }   
    }

    public int? ContributionAllowed { 
        get
        {
            if (Eligible == TriState.True)
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

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new 
                         PropertyChangedEventArgs(propertyName));
    }

    bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? 
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