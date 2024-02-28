namespace Models;

public class Pension 
{
    public string? Title { get; set; }
    public double Income { get; set; }
    public bool OneTime { get; set; }
    public int? BeginningAge { get; set; }
    public int? BeginningYear { get; set; }
    public bool HasCola { get; set; }
    public double SurvivorsBenefit { get; set; }
}