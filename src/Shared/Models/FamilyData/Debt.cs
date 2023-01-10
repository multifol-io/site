using System.ComponentModel.DataAnnotations;
public class Debt {

    public string? Name { get; set; }
    public double? Rate { get; set; }
    public int? Total { get; set; }
    public DateOnly? PayoffDate { get; set; }
    public string? Category 
    {
        get { 
            if (Rate > 8) {
                return "High";
            } else if (Rate <= 8 && Rate > 3) {
                return "Medium";
            } else if (Rate <= 3) {
                return "Low";
            } else if (Rate == null) {
                return "Unknown";
            } else {
                return null;
            }
        }
    }
}