public class FamilyYears {
    public FamilyYears(IRS.Retirement year1, IRS.Retirement year2)
    {
        //TODO: don't hardcode years. use current date, etc...
        Years[0] = new FamilyYear(year1, 2022);
        Years[1] = new FamilyYear(year2, 2023);
    }

    public FamilyYear[] Years = new FamilyYear[2];

    public int CurrentYear { get; set; }

    public FamilyYear Active {
        get { return Years[CurrentYear]; }
    }
}