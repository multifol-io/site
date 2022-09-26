using System.ComponentModel.DataAnnotations;
public class Debt {

    public string? Name { get; set; }
    public double? Rate { get; set; }
    public double? Total { get; set; }
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
            } else {
                return null;
            }
        }
    }
}