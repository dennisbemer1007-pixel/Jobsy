using System.Reflection;
using Jobsy.Api.Models;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public class EmployerRolesFixlistTests
{
    [Fact]
    public void Employer_application_dtos_have_no_match_or_score_fields()
    {
        AssertNoMatchScore(typeof(EmployerApplicationDto));
        AssertNoMatchScore(typeof(EmployerApplicationItem));
    }

    [Fact]
    public void Vacancy_delete_rules_match_the_product_decision()
    {
        Assert.True(VacancyDeletionRules.EmployerMayDelete(VacancyStatus.Draft, 0));
        Assert.True(VacancyDeletionRules.EmployerMayDelete(VacancyStatus.Archived, 0));
        Assert.False(VacancyDeletionRules.EmployerMayDelete(VacancyStatus.Active, 0));
        Assert.False(VacancyDeletionRules.EmployerMayDelete(VacancyStatus.Draft, 2));
        Assert.True(VacancyDeletionRules.AdminMayPurgeWithApplications(3));
        Assert.False(VacancyDeletionRules.AdminMayPurgeWithApplications(0));
    }

    [Fact]
    public void Employer_can_reject_after_accept_until_hired()
    {
        Assert.True(ApplicationRules.CanEmployerReject(ApplicationStatus.Pending));
        Assert.True(ApplicationRules.CanEmployerReject(ApplicationStatus.Accepted));
        Assert.True(ApplicationRules.CanEmployerReject(ApplicationStatus.EmployerContacting));
        Assert.False(ApplicationRules.CanEmployerReject(ApplicationStatus.Hired));
        Assert.False(ApplicationRules.CanEmployerReact(ApplicationStatus.Accepted));
    }

    private static void AssertNoMatchScore(Type type)
    {
        var names = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);

        foreach (var name in names)
        {
            Assert.DoesNotContain("Match", name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Score", name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
