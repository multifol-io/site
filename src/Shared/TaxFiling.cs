using System.ComponentModel.DataAnnotations;
public class TaxFiling {

    [Required]
    public int PersonCount { get; set; }
    
    private List<Person>? _people;
    public List<Person> People {
        get {
            if (_people == null) {
                _people = new List<Person>();
                _people.Add(new Person());
                _people.Add(new Person());
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

    public int? ModifiedAGI { get; set; }

    public EmergencyFund EmergencyFund { get; set; } = new();
}