using Employer;
using System.Text.Json.Serialization;

public class Person {
    public Person(FamilyYears familyYears, int personIndex) {
        this.FamilyYears = familyYears;
        this.EmployerPlan = new EmployerPlan(this);
        this.EmployerBenefits = new EmployerBenefits() { Year = familyYears.Year };
        this.IRA = new IRA(this);
        this.RothIRA = new RothIRA(this);
        this.HealthSavingsAccount = new HealthSavingsAccount(this);
        this.PersonIndex = personIndex;
        if (this.PersonIndex == 0) { Identifier = "person 1"; }
        else { Identifier = "person 2"; }
    }


    public int? Age { get; set; }
    public string Identifier { get; set; }
    public string? Employer { get; set; }
    public EmployerPlan EmployerPlan { get; set; }
    public HealthSavingsAccount HealthSavingsAccount { get; set; }
    [JsonIgnore]
    public IRA IRA { get; set; }
    [JsonIgnore]
    public RothIRA RothIRA { get; set; }

    private EmployerBenefits? _employerBenefits;
    [JsonIgnore]
    public EmployerBenefits? EmployerBenefits { 
        get { return _employerBenefits; }
        set { 
            _employerBenefits = value;
        }
    }


    public FamilyYears FamilyYears { private set; get; }

    public string? PossessiveID { 
        get {
            if (FamilyYears.PersonCount > 1) {
                switch (Identifier)
                {
                    case "me":
                        return "my";
                    case "them":
                        return "their";
                    case "him":
                        return "his";
                    case "her":
                        return "her";
                    case null:
                        return null;
                    case "person 1":
                        return "person 1's";
                    case "person 2":
                        return "person 2's";
                    default:
                        return "undefined";
                }
            } else {
                return null;
            }
        }
    }

    public bool FiftyOrOver { 
        get {
            return Age >= 50;
        }
    }
    public bool FiftyFiveOrOver { 
        get {
            return Age >= 55;
        }
    }

    public Person? Spouse { 
        get {
            if (FamilyYears.TaxFilingStatus == TaxFilingStatus.MarriedFilingJointly 
            || FamilyYears.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart 
            || FamilyYears.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperately)
            {
                switch (this.PersonIndex) {
                    case 0:
                        return FamilyYears.People[1];
                    case 1:
                        return FamilyYears.People[0];
                    default:
                        throw new InvalidDataException("PersonIndex not expected to be " + this.PersonIndex);
                }
            }
            else
            {
                return null;
            }
        }
    }

    public IRAType IRATypeRecommendation { 
        get
        {
            if (this.IRA.DeductionAllowed > 0) {
                if (!this.IRA.HasExistingBalance) {
                    return IRAType.DeductibleIRAThenBackdoorRoth;
                } else {
                    return IRAType.DeductibleIRA;
                }
            } else if (this.RothIRA.AmountToSave > 0) {
                    return IRAType.Roth;
            } else {
                if (!this.IRA.HasExistingBalance) {
                    return IRAType.NondeductibleIRAThenBackdoorRoth;
                } else {
                    return IRAType.NondeductibleIRA;
                }
            }
        }
    }

    public int PersonIndex { private set; get; }
    
    public Person? OtherPerson { 
        get {
            int personIndex = PersonIndex;
            switch (personIndex) {
                case 0:
                    if (FamilyYears.PersonCount > 1)
                    {
                        return FamilyYears.People[1];
                    }
                    else
                    {
                        return null;
                    }
                case 1:
                    return FamilyYears.People[0];
                default:
                    return null;
            }
        }
    }
}