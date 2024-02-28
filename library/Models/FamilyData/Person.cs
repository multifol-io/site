using Employer;
using System.Text.Json.Serialization;

namespace Models;

public class Person {
    public Person() {
        this.EmployerPlan = new EmployerPlan();
        this.EmployerBenefits = new EmployerBenefits();
        this.IRA = new IRA();
        this.RothIRA = new RothIRA();
        this.HealthSavingsAccount = new HealthSavingsAccount();
        this.Retirement = new();
        this.RSUGrants = [];
    }

    // set back pointers in person
    public void SetBackPointers(FamilyData familyData)
    {
        this.FamilyData = familyData;
        this.EmployerBenefits.person = this;
        this.EmployerPlan.Person = this;
        this.HealthSavingsAccount.Person = this;
        this.IRA.Person = this;
        this.RothIRA.Person = this;
    }

    // back pointer
    [JsonIgnore]
    public FamilyData FamilyData { get; set; } = null!;


    public int? Age { get; set; }
    public Retirement Retirement { get; set; }

    public string? Identifier { get; set; }
    public string? Employer { get; set; }
    public EmployerPlan EmployerPlan { get; set; }
    
    public HealthSavingsAccount HealthSavingsAccount { get; set; }
    [JsonIgnore]
    public IRA IRA { get; set; }
    [JsonIgnore]
    public RothIRA RothIRA { get; set; }

    public EmployerBenefits EmployerBenefits { get; set; }

    public string? PossessiveID { 
        get {
            if (FamilyData.PersonCount > 1) {
                return Identifier switch
                {
                    "me" => "my",
                    "them" => "their",
                    "him" => "his",
                    "her" => "her",
                    null => null,
                    "person 1" => "person 1's",
                    "person 2" => "person 2's",
                    _ => "undefined",
                };
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
            if (FamilyData.TaxFilingStatus == TaxFilingStatus.MarriedFilingJointly 
            || FamilyData.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperately)
            {
                return this.PersonIndex switch
                {
                    0 => FamilyData.People[1],
                    1 => FamilyData.People[0],
                    _ => throw new InvalidDataException("PersonIndex not expected to be " + this.PersonIndex),
                };
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
            if (this.RothIRA.AmountToSave > 0) {
                return IRAType.Roth;
            } else if (this.IRA.DeductionAllowed > 0) {
                if (!this.IRA.HasExistingBalance) {
                    return IRAType.DeductibleIRAThenBackdoorRoth;
                } else {
                    return IRAType.DeductibleIRA;
                }
            } else {
                if (!this.IRA.HasExistingBalance) {
                    return IRAType.NondeductibleIRAThenBackdoorRoth;
                } else {
                    return IRAType.NondeductibleIRA;
                }
            }
        }
    }

    [JsonIgnore]
    public int PersonIndex { set; get; }
    
    public Person? OtherPerson { 
        get {
            int personIndex = PersonIndex;
            switch (personIndex) {
                case 0:
                    if (FamilyData.PersonCount > 1)
                    {
                        return FamilyData.People[1];
                    }
                    else
                    {
                        return null;
                    }
                case 1:
                    return FamilyData.People[0];
                default:
                    return null;
            }
        }
    }

    public List<RSUGrant> RSUGrants { get; set; }

    public double? VestAmount() {
        double vestingRSUs = 0.0;
        foreach (var rsuGrant in this.RSUGrants) {
            var vest = rsuGrant.VestAmount(FamilyData.Year);
            if (vest.HasValue) {
                vestingRSUs += vest.Value;
            }
        }
        return vestingRSUs;
    }
}