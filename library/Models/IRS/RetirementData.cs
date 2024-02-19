using System.Text.Json.Serialization;

namespace IRS
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Employer401k
    {
        public int ContributionLimit { get; set; }
        public int CatchUpContributionLimit { get; set; }
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
        public MarriedFilingJointly? MarriedFilingJointly { get; set; }
        public MarriedFilingSeparately? MarriedFilingSeparately { get; set; }
    }

    public class DeductabilityPhaseOutRange
    {
        public Single? Single { get; set; }
        public ActiveParticipant_MarriedFilingJointly? ActiveParticipant_MarriedFilingJointly { get; set; }
        public InactiveParticipant_MarriedFilingJointly? InactiveParticipant_MarriedFilingJointly { get; set; }
        public MarriedFilingSeparately? MarriedFilingSeparately { get; set; }
    }

    public class HSA
    {
        public ContributionLimit? ContributionLimit { get; set; }
        public int CatchUpAge { get; set; }
    }

    public class IRA
    {
        public int ContributionLimit { get; set; }
        public int CatchUpContributionLimit { get; set; }
        public int CatchUpAge { get; set; }
        public DeductabilityPhaseOutRange? DeductabilityPhaseOutRange { get; set; }
    }

    public class MarriedFilingJointly
    {
        public int Start { get; set; }
        public int End { get; set; }
    }
    public class ActiveParticipant_MarriedFilingJointly
    {
        public int Start { get; set; }
        public int End { get; set; }
    }
    public class InactiveParticipant_MarriedFilingJointly
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class MarriedFilingSeparately
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class RetirementData : DataDocument
    {

        public HSA? HSA { get; set; }
        public IRA? IRA { get; set; }
        public RothIRA? RothIRA { get; set; }
        public Employer401k Employer401k { get; set; }
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
}
