using IRS;
using System.ComponentModel.DataAnnotations;
public class FamilyYear {
    public FamilyYear(RetirementData retirementData, TaxRateData taxRateData, int year) {
        this.RetirementData = retirementData;
        this.TaxRateData = taxRateData;
        this.Year = year;
    }

    public RetirementData RetirementData { get; private set; }
    public TaxRateData TaxRateData { get; private set; }
    
    [Required]
    public int PersonCount { get; set; }

    public string StateOfResidence { get; set; }
    
    public int Year { get; private set; }

    private List<Person>? _people;
    public List<Person> People {
        get {
            if (_people == null) {
                _people = new List<Person>();
                _people.Add(new Person(this, 0));
                _people.Add(new Person(this, 1));
            }

            return _people;
        }
    }
    
    private TaxFilingStatus _taxFilingStatus;
    [Required]
    public TaxFilingStatus TaxFilingStatus { 
        get {
            return _taxFilingStatus;
        }
        set {
            _taxFilingStatus = value;
            switch (_taxFilingStatus)
            {
                case TaxFilingStatus.Single:
                case TaxFilingStatus.MarriedFilingSeperately:
                case TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart:
                    PersonCount = 1;
                    break;
                case TaxFilingStatus.MarriedFilingJointly:
                    PersonCount = 2;
                    break;
                case TaxFilingStatus.None:
                default:
                    PersonCount = 0;
                    break;
            }
        }
    }

    public int? AdjustedGrossIncome { get; set; }
    public string? FederalMarginalTaxBracket { get; set; }
    public string? StateMarginalTaxBracket { get; set; }
    public EmergencyFund EmergencyFund { get; set; } = new();

    private List<Debt>? _debts;
    public List<Debt> Debts
    {
        get {
            if (_debts == null) {
                _debts = new List<Debt>();
            }

            return _debts;
        }
    }

    public double? Stocks { get; set; }
    public double? Bonds { get; set; }
    public int? International { get; set; }

    private int? GetDebts(string category) {
            int? total = null;
            foreach (var debt in Debts) {
                if (debt.Category == category) {
                    if (total == null) {
                        total = debt.Total;
                    } else {
                        total += debt.Total;
                    }
                }
            }

            return total;
    }

    public int? HighDebts {
        get {
            return GetDebts("High");
        }
    }

    public int? MediumDebts {
        get {
            return GetDebts("Medium");
        }
    }

    public int? LowDebts {
        get {
            return GetDebts("Low");
        }
    }

    public int? UnknownDebts {
        get {
            return GetDebts(null);
        }
    }

    public double Value { 
        get {
            double newValue = 0;
            foreach (var account in Accounts) 
            {
                newValue += account.Value;
            }
            
            return newValue;
        }
    }
    private List<Account> _Accounts;
    public List<Account> Accounts {
        get {
            if (_Accounts == null)
            {
                _Accounts = new List<Account>();
            }

            return _Accounts;
        }
    }

    public void UpdatePercentages()
    {
        double totalValue = this.Value;
        foreach (var account in Accounts)
        {
            account.Percentage = account.Value / totalValue * 100;
            account.UpdatePercentages(totalValue);
        }
    }
}