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

    private List<Account> _Accounts;
    public List<Account> Accounts {
        get {
            if (_Accounts == null)
            {
                _Accounts = new List<Account>();
                var taxable = new Account() { AccountType = "Taxable" };
                var vti = new Investment() { Ticker = "VTI", Name = "Vanguard Total Index", ExpenseRatio = .04, Value = 200 };
                taxable.Investments.Add(vti);
                _Accounts.Add(taxable);
                var my401k = new Account() { AccountType = "401k", Identifier = "My", Custodian = "Fidelity" };
                var vti2 = new Investment() { Ticker = "VTI", Name = "Vanguard Total Index", ExpenseRatio = .04, Value = 2000 };
                my401k.Investments.Add(vti2);
                var bnd = new Investment() { Ticker = "BND", Name = "Vanguard Total Bond", ExpenseRatio = .08, Value = 2000 };
                my401k.Investments.Add(bnd);
                _Accounts.Add(my401k);
            }

            return _Accounts;
        }
    }
}