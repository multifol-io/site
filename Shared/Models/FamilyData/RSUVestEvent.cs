public class RSUVestEvent {
    public DateTime Date { get; set; }
    public int Shares { get; set; }
    public double? Price { get; set; }
    public double? Value {
        get {
            return Price != null ? Shares * Price : null;
        }
    }
}