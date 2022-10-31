using System.Text.Json.Serialization;

namespace IRS
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Employer401k
    {
        public int ContributionLimit { get; set; }
        public int CatchUpContributionLimit { get; set; }
    }

    public class ActiveParticipant
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class ContributionLimit
    {
        public int Family { get; set; }
        public int SelfOnly { get; set; }
        public int CatchUp { get; set; }
    }

    public class ContributionPhaseOutRange
    {
        public Single? Single { get; set; }
        public MarriedFiledJointly? MarriedFiledJointly { get; set; }
        public MarriedFiledSeparately? MarriedFiledSeparately { get; set; }
    }

    public class DeductabilityPhaseOutRange
    {
        public Single? Single { get; set; }
        public MarriedFiledJointly? MarriedFiledJointly { get; set; }
        public MarriedFiledSeparately? MarriedFiledSeparately { get; set; }
    }

    public class HSA
    {
        public ContributionLimit? ContributionLimit { get; set; }
        public int CatchUpAge { get; set; }
    }

    public class InactiveParticipant
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class IRA
    {
        public int ContributionLimit { get; set; }
        public int CatchUpContributionLimit { get; set; }
        public int CatchUpAge { get; set; }
        public DeductabilityPhaseOutRange? DeductabilityPhaseOutRange { get; set; }
    }

    public class MarriedFiledJointly
    {
        public ActiveParticipant? ActiveParticipant { get; set; }
        public InactiveParticipant? InactiveParticipant { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class MarriedFiledSeparately
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class Retirement
    {
        public string? name { get; set; }
        public string? repository { get; set; }
        public string? path { get; set; }
        public string? version { get; set; }
        public string? note { get; set; }
        public List<Source>? source { get; set; }
        public HSA? HSA { get; set; }
        public IRA? IRA { get; set; }
        public RothIRA? RothIRA { get; set; }
        public Employer401k? Employer401k { get; set; }
    }

    public class RothIRA
    {
        public int ContributionLimit { get; set; }
        public int CatchUpContributionLimit { get; set; }
        public int CatchUpAge { get; set; }
        public ContributionPhaseOutRange? ContributionPhaseOutRange { get; set; }
    }

    public class Single
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class Source
    {
        public string? date { get; set; }
        public string? title { get; set; }
        public string? url { get; set; }
    }


}
