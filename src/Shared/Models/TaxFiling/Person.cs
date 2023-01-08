public class Person {
    public Person(FamilyYear familyYear, int personIndex) {
        this.FamilyYear = familyYear;
        this.EmployerPlan = new EmployerPlan(this);
        this.IRA = new IRA(this);
        this.RothIRA = new RothIRA(this);
        this.HealthSavingsAccount = new HealthSavingsAccount(this);
        this.PersonIndex = personIndex;
        if (this.PersonIndex == 0) { Identifier = "me"; }
        else { Identifier = "them"; }
    }

    public Employer.Employer _employer;
    public Employer.Employer Employer { 
        get { return _employer; }
        set { 
            _employer = value;
            
            if (_employer != null) {
                if (_employer.RetirementSavings.HSA != null) {
                    HealthSavingsAccount.EmployerContribution = _employer.RetirementSavings.HSA.EmployerContribution;
                }

                if (_employer.RetirementSavings.Employer401k != null) {
                    EmployerPlan.Eligible = _employer.RetirementSavings.Employer401k.Offered;
                    EmployerPlan.MatchA = _employer.RetirementSavings.Employer401k.MatchRules[0].MatchPercentage;
                    if (_employer.RetirementSavings.Employer401k.MatchRules[0].ForNextPercent != null) {
                        EmployerPlan.MatchALimit = _employer.RetirementSavings.Employer401k.MatchRules[0].ForNextPercent;
                    }
                    if (_employer.RetirementSavings.Employer401k.MatchRules.Count == 2) {
                        EmployerPlan.MatchA = _employer.RetirementSavings.Employer401k.MatchRules[1].MatchPercentage;
                        if (_employer.RetirementSavings.Employer401k.MatchRules[1].ForNextPercent != null) {
                            EmployerPlan.MatchALimit = _employer.RetirementSavings.Employer401k.MatchRules[1].ForNextPercent;
                        }
                    }

                    EmployerPlan.MaxMatch = _employer.RetirementSavings.Employer401k.MatchLimit;
                    EmployerPlan.AmountToSaveForBackdoorRoth = _employer.RetirementSavings.MegaBackdoorRoth.ContributionLimit;
                }
            } else {
                HealthSavingsAccount.EmployerContribution = null;
                EmployerPlan.Eligible = false;
                EmployerPlan.MatchA = null;
                EmployerPlan.MaxMatch = null;
                EmployerPlan.AmountToSaveForBackdoorRoth = null;
            }
        }
    }

    public Person? Spouse { 
        get {
            if (FamilyYear.TaxFilingStatus == TaxFilingStatus.MarriedFilingJointly 
            || FamilyYear.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart 
            || FamilyYear.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperately)
            {
                switch (this.PersonIndex) {
                    case 0:
                        return FamilyYear.People[1];
                    case 1:
                        return FamilyYear.People[0];
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
    
    public Person OtherPerson { 
        get {
            int personIndex = PersonIndex;
            switch (personIndex) {
                case 0:
                    if (FamilyYear.PersonCount > 1)
                    {
                        return FamilyYear.People[1];
                    }
                    else
                    {
                        return null;
                    }
                case 1:
                    return FamilyYear.People[0];
                default:
                    return null;
            }
        }
    }

    public FamilyYear FamilyYear { private set; get; }

    public string Identifier { get; set; }
    public int? Age { get; set; }
    public bool FiftyOrOver { get; set; }
    public bool FiftyFiveOrOver { get; set; }

    public EmployerPlan EmployerPlan { get; set; }
    public HealthSavingsAccount HealthSavingsAccount { get; set; }
    public IRA IRA { get; set; }
    public RothIRA RothIRA { get; set; }
}