public class Retirement {
    public Retirement() {
        Pensions = new();
    }

    public int? RetirementAge { get; set; }
    public int? ForecastEndAge { get; set; } = 95;
    public int? SSAge { get; set; }
    public double? SSAnnual { get; set; }
    public List<Pension> Pensions { get; set; }
}