using System.ComponentModel.DataAnnotations;
public class TaxFiling {
    public TaxFiling(IRS.Retirement irs_retirement) {
        this.IRSRetirement = irs_retirement;
    }

    public IRS.Retirement IRSRetirement { get; private set; }
    
    [Required]
    public int PersonCount { get; set; }
    
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
}