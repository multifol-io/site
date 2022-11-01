public class TaxFilings {
    public TaxFilings(IRS.Retirement year1, IRS.Retirement year2)
    {
        //TODO: don't hardcode years. use current date, etc...
        Years[0] = new TaxFiling(year1, 2022);
        Years[1] = new TaxFiling(year2, 2023);
    }

    public TaxFiling[] Years = new TaxFiling[2];

    public int CurrentYear { get; set; }

    public TaxFiling Active {
        get { return Years[CurrentYear]; }
    }
}