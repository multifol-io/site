public class TaxFilings {
    public TaxFilings(IRS.Retirement year1, IRS.Retirement year2)
    {
        Years[0] = new TaxFiling(year1);
        Years[1] = new TaxFiling(year2);
    }
    public TaxFiling[] Years = new TaxFiling[2];

    public int CurrentYear { get; set; }

    public TaxFiling Active {
        get { return Years[CurrentYear]; }
    }
}