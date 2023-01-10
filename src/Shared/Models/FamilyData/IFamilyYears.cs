public interface IFamilyYears
{
    public int CurrentYear { get; set; }
    public SearchModel Search { get; set; }


    public FamilyYear Active { get; }
}