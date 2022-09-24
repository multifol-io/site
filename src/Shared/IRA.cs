using System.ComponentModel.DataAnnotations;
public class IRA {
    public IRA(Person person)
    {
        this.person = person;
    }

    private Person person;

    public int? ContributionAllowed { 
        get
        {
            if (person.TaxFiling.PersonCount > person.PersonIndex && person.TaxFiling.AdjustedGrossIncome != null)
            {
                return 6000 + (person.FiftyOrOver ? 1000 : 0);
            }
            else
            {
                return null;
            }
        }
    }

    public int? CalculateDeduction(int? income, TaxFilingStatus taxFilingStatus, Person person)
    {
        switch (taxFilingStatus) {
            case TaxFilingStatus.Single:
                if (person.EmployerPlan.Eligible) {
                    return ApplyRange(68000, 78000, person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                } else {
                    return ContributionAllowed;
                }
            case TaxFilingStatus.MarriedFilingJointly:
                if (person.Spouse != null) {
                    if (person.Spouse.EmployerPlan.Eligible) {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(109000, 129000, person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ApplyRange(204000, 214000, person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(109000, 129000, person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ContributionAllowed;
                        }
                    }
                } else {
                    return null;
                }
            case TaxFilingStatus.MarriedFilingSeperately:
                if (person.Spouse != null) {
                    if (person.Spouse.EmployerPlan.Eligible) {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(0, 10000, person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ApplyRange(0, 10000, person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        }
                    } else {
                        if (person.EmployerPlan.Eligible) {
                            return ApplyRange(0, 10000, person.TaxFiling.AdjustedGrossIncome, ContributionAllowed);
                        } else {
                            return ContributionAllowed;
                        }
                    }
                } else {
                    return null;
                }
            default:
            case TaxFilingStatus.None:
                return null;
        }
    }

    private int? ApplyRange(int low, int high, int? income, int? contributionAllowed) 
    {
        if (income <= low) return contributionAllowed;
        if (income >= high) return 0;
        return contributionAllowed * (high - income) / (high - low);
    }

    public int? DeductionAllowed { 
        get
        {
            return CalculateDeduction(person.TaxFiling.AdjustedGrossIncome, person.TaxFiling.TaxFilingStatus, person);
        }
    }
    public int? SavingsOpportunity {
        get {
            return ContributionAllowed;
        }
    }
}