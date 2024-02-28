using System.Text.Json.Serialization;

namespace Models;

public class RothIRA {
    // back pointer
    [JsonIgnore]
    public Person? Person { get; set; }

    private IRS.RothIRA RothIraVariables {
        get { return Person!.FamilyData.AppData.IRSData!.RetirementData.RothIRA!; }
    }

    public int MaximumContributionByAge { 
        get
        {
            return RothIraVariables.ContributionLimit
                + (Person!.FiftyOrOver ? RothIraVariables.CatchUpContributionLimit : 0);
        }
    }

    public int? ContributionAllowed {
        get { 
            if (Person!.FamilyData.PersonCount > Person.PersonIndex && Person.FamilyData.AdjustedGrossIncome != null) {
                return CalculateAllowedContribution(Person.FamilyData.TaxFilingStatus, Person);
            } else {
                return null;
            }
        }
    }

    public int? CalculateAllowedContribution(TaxFilingStatus taxFilingStatus, Person person)
    {
        if (RothIraVariables is not null && RothIraVariables.ContributionPhaseOutRange is not null) {
            switch (taxFilingStatus) {
                case TaxFilingStatus.Single:
                    return ApplyRange(RothIraVariables.ContributionPhaseOutRange.Single!.Start,
                                    RothIraVariables.ContributionPhaseOutRange.Single.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                case TaxFilingStatus.MarriedFilingJointly:
                    return ApplyRange(RothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly!.Start,
                                    RothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                case TaxFilingStatus.MarriedFilingSeperately: 
                    if (person.FamilyData.TaxFilingStatusLivingSeperately) {
                        return ApplyRange(RothIraVariables.ContributionPhaseOutRange.Single!.Start,
                                    RothIraVariables.ContributionPhaseOutRange.Single.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                    } else {
                        return ApplyRange(RothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately!.Start,
                                    RothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                    }
                case TaxFilingStatus.ChoiceNeeded:
                default:
                    return null;
            }
        }
        return null;
    }

    private static int? ApplyRange(int low, int high, int? income, int? contributionAllowed) 
    {
        if (income <= low) return contributionAllowed;
        if (income >= high) return 0;
        return contributionAllowed * (high - income) / (high - low);
    }

    public int? AmountToSave {
        get {
            return ContributionAllowed;
        }
    }
}