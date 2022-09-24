public class Person {
    public bool FiftyOrOver { get; set; }

    public RetirementFunds RetirementFunds { get; set; } = new();
    public HealthSavingsAccount HealthSavingsAccount { get; set; } = new();
}