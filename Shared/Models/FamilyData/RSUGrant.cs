public class RSUGrant {
    public DateTime Date { get; set; }
    public string? Ticker { get; set; }
    public int VestEventsCount { get; set; }
    public int VestPeriodMonths { get; set;}
    public double Amount { get; set; }
    public double? Price { get; set; }

    private double? _LastPrice;
    public double? LastPrice { 
        get { return _LastPrice; }
        set {
            _LastPrice = value;
            foreach (var vestEvent in VestEvents) {
                vestEvent.Price = _LastPrice;
            }
        }
    }
    
    public int? Shares {
        get {
            if (Price != null) {
                return (int)(Amount/Price);
            } else return null;
        }
    }
    public List<RSUVestEvent> VestEvents { get; set; } = new();
}