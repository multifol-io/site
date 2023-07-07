public class RSUGrant {
    public DateTime Date { get; set; }
    public string? Ticker { get; set; }
    public int VestEventsCount { get; set; }
    public int VestPeriodMonths { get; set;}
    public double Amount { get; set; }
    public double? Price { get; set; }
    public int? Shares { get; set; }

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

    public List<RSUVestEvent> VestEvents { get; set; } = new();
    public void CalculateShares() {
        if (Price != null) {
            Shares = (int)(Amount/Price);
        }
    }
}