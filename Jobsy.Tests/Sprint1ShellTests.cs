using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Web.Auth;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class AuthRedirectsTests
{
    [Theory]
    [InlineData(null, "/home")]
    [InlineData("", "/home")]
    [InlineData("/", "/home")]
    [InlineData("/banen", "/home")]
    [InlineData("/vacancies/abc", "/vacancies/abc")]
    public void PostLoginUrl_maps_anonymous_landings(string? input, string expected)
        => Assert.Equal(expected, AuthRedirects.PostLoginUrl(input));

    [Theory]
    [InlineData("/home", "/home")]
    [InlineData("/vacancies/11111111-1111-1111-1111-111111111111", "/vacancies/11111111-1111-1111-1111-111111111111")]
    [InlineData("//evil.com", "/home")]
    [InlineData("/\\evil", "/home")]
    [InlineData("https://evil.com", "/home")]
    [InlineData("/login?returnUrl=https://evil.com", "/home")]
    public void SafeLocalUrl_rejects_open_redirects(string input, string expected)
        => Assert.Equal(expected, AuthRedirects.SafeLocalUrl(input));
}

public class RoleNavCatalogTests
{
    [Fact]
    public void ForUser_anonymous_gets_search_saved_profile()
    {
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(new ClaimsIdentity()));
        Assert.Equal(3, items.Count);
        Assert.Contains(items, i => i.Href == "/");
        Assert.Contains(items, i => i.Href == "/candidate/liked");
        Assert.Contains(items, i => i.Href == "/login");
    }

    [Fact]
    public void ForUser_admin_via_roles_claim()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("roles", JobsyRoles.Admin));
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.Contains(items, i => i.Href == "/home");
        Assert.Contains(items, i => i.Href == "/admin/settings");
        Assert.True(RoleNavCatalog.IsActive(
            items.First(i => i.Href == "/admin/settings"),
            "admin/users"));
    }

    [Fact]
    public void ForUser_candidate_gets_search_saved_profile()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Candidate)], "test");
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.Equal(3, items.Count);
        Assert.Contains(items, i => i.Href == "/");
        Assert.Contains(items, i => i.Href == "/candidate/liked");
        Assert.Contains(items, i => i.Href == "/candidate/profile");
        Assert.DoesNotContain(items, i => i.Href == "/home");
    }

    [Fact]
    public void IsActive_matches_extra_path_prefix()
    {
        var item = new NavItem("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch"]);
        Assert.True(RoleNavCatalog.IsActive(item, "branch/applicants"));
        Assert.True(RoleNavCatalog.IsActive(item, "employer/vacancies"));
        Assert.False(RoleNavCatalog.IsActive(item, "admin/users"));
    }

    [Fact]
    public void TokensHrefFor_branch_manager()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.BranchManager)], "test");
        Assert.Equal("/branch/tokens", RoleNavCatalog.TokensHrefFor(new ClaimsPrincipal(identity)));
    }
}

public class RoleClaimMatchingTests
{
    [Fact]
    public void HasRole_matches_namespaced_roles_claim()
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/roles", JobsyRoles.Candidate));
        Assert.True(RoleClaimMatching.HasRole(new ClaimsPrincipal(identity), JobsyRoles.Candidate));
    }
}

public class VacancyVisibilityRulesUnitTests
{
    [Fact]
    public void IsPubliclyVisible_requires_active_and_date_window()
    {
        var today = new DateOnly(2026, 7, 24);
        var active = Vacancy(VacancyStatus.Active, today.AddDays(-1), today.AddDays(10));
        var draft = Vacancy(VacancyStatus.Draft, today.AddDays(-1), today.AddDays(10));
        var expired = Vacancy(VacancyStatus.Active, today.AddDays(-30), today.AddDays(-1));

        Assert.True(VacancyVisibilityRules.IsPubliclyVisible(active, today));
        Assert.False(VacancyVisibilityRules.IsPubliclyVisible(draft, today));
        Assert.False(VacancyVisibilityRules.IsPubliclyVisible(expired, today));
    }

    [Fact]
    public void CanAcceptApplications_respects_max()
    {
        var today = new DateOnly(2026, 7, 24);
        var vacancy = Vacancy(VacancyStatus.Active, today, today.AddMonths(1));
        vacancy.MaxApplications = 2;

        Assert.True(VacancyVisibilityRules.CanAcceptApplications(vacancy, today, 1));
        Assert.False(VacancyVisibilityRules.CanAcceptApplications(vacancy, today, 2));
    }

    private static Vacancy Vacancy(VacancyStatus status, DateOnly start, DateOnly end) => new()
    {
        Id = Guid.NewGuid(),
        Title = "t",
        Description = "d",
        HourlyWage = 14,
        StartDate = start,
        EndDate = end,
        Status = status,
        CompanyId = Guid.NewGuid(),
        Location = new GeoPoint(52, 4),
        RequiredTransport = TransportMode.Bike,
        MaxApplications = 5
    };
}
