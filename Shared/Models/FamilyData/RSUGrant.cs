using System.Text.Json.Serialization;

public class RSUGrant {
    public DateOnly? Date { get; set; }
    public string? Ticker { get; set; }
    public double? VestPercent { get; set; }

    private int? _VestEventsCount;
    public int? VestEventsCount { 
        get { return _VestEventsCount; }
        set {
            _VestEventsCount = value;
            VestPercent = _VestEventsCount != null ? 100.0 / (double)_VestEventsCount.Value : 0;
        }
     }
    public int? FirstVestMonth { get; set;}
    public int? VestPeriodMonths { get; set;}
    public double Amount { get; set; }
    public double? Price { get; set; }
    public int? Shares { get; set; }
    public bool Edit { get; set; }

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

    [JsonIgnore]
    public List<RSUVestEvent> VestEvents { get; set; } = new();
    public void CalculateShares() {
        if (Price != null) {
            Shares = (int)(Amount/Price);
        }
    }

    public double? VestAmount(int year) {
        if (this.VestEvents.Count == 0) {
            CalculateVestEvents();
        }

        var vestYears = (from vestEvent in VestEvents 
                        where vestEvent.Date >= new DateOnly(year, 1, 1) && vestEvent.Date <= new DateOnly(year,12,31)
                        group vestEvent by vestEvent.Date.Year
                        into grp
                        select new { year = grp.Key, value = grp.Sum(ve=>ve.Value) });
        foreach (var vestYear in vestYears) {
            return vestYear.value;
        }

        return null;
    }

    public bool CalculateVestEvents() {
        this.VestEvents.Clear();
        if (this.Shares != null && this.VestPercent.HasValue && this.VestPercent > 0.0 && this.VestPeriodMonths.HasValue) {
            double sharesRemaining = (double)this.Shares;
            double remainderShares = 0.0;
            if (this.Date != null) {
                var vestIndex = 0;
                int vestMonth = 0;
                while (sharesRemaining > 0.0) {
                    var vestShares = this.VestPercent.Value / 100.0 * (double)this.Shares; 
                    remainderShares += vestShares - (int)vestShares;
                    var currentVestShares = (int)vestShares + (int)(remainderShares + .0000001);
                    remainderShares -= (int)remainderShares;
                    if (vestIndex == 0) {
                        vestMonth = this.FirstVestMonth ?? this.VestPeriodMonths.Value;
                    } else {
                        vestMonth += this.VestPeriodMonths.Value;
                    }
                    var vestEvent = new RSUVestEvent() {
                        Date = this.Date.Value.AddMonths(vestMonth),
                        Shares = currentVestShares,
                        Price = this.LastPrice
                    };

                    this.VestEvents.Add(vestEvent);
                    vestIndex++;
                    sharesRemaining -= currentVestShares;
                }

                return true;
            }
        }
        
        return false;
    }
}