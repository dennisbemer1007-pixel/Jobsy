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
    [InlineData("/login", "/home")]
    [InlineData("/login?returnUrl=/vacancies/1", "/home")]
    public void PostLoginUrl_maps_anonymous_landings(string? input, string expected)
        => Assert.Equal(expected, AuthRedirects.PostLoginUrl(input));

    [Theory]
    [InlineData("/home", "/home")]
    [InlineData("/vacancies/11111111-1111-1111-1111-111111111111", "/vacancies/11111111-1111-1111-1111-111111111111")]
    [InlineData("//evil.com", "/home")]
    [InlineData("/\\evil", "/home")]
    [InlineData("https://evil.com", "/home")]
    [InlineData("/login?returnUrl=https://evil.com", "/home")]
    [InlineData("\"onclick=alert(1)", "/home")]
    [InlineData("/\"onclick=alert(1)", "/home")]
    [InlineData("/javascript:alert(1)", "/home")]
    [InlineData("/privacy/data", "/privacy/data")]
    public void SafeLocalUrl_rejects_open_redirects(string input, string expected)
        => Assert.Equal(expected, AuthRedirects.SafeLocalUrl(input));

    [Fact]
    public void ResolveRequestedReturnUrl_accepts_returnTo_and_redirect_aliases()
    {
        Assert.Equal("/home", AuthRedirects.ResolveRequestedReturnUrl());
        Assert.Equal("/home", AuthRedirects.ResolveRequestedReturnUrl(null, "", "https://evil.example"));
        Assert.Equal(
            "/employer/vacancies/123",
            AuthRedirects.ResolveRequestedReturnUrl(null, "/employer/vacancies/123", "/ignored"));
        Assert.Equal(
            "/vacancies/abc",
            AuthRedirects.ResolveRequestedReturnUrl(null, null, "/vacancies/abc"));
        Assert.Equal(
            "/employer/tokens",
            AuthRedirects.ResolveRequestedReturnUrl("/employer/tokens", "/other"));
        Assert.Equal(
            "/home",
            AuthRedirects.ResolveRequestedReturnUrl("/login?returnUrl=https://evil.com"));
        Assert.Equal(
            "/vacancies/abc",
            AuthRedirects.ResolveRequestedReturnUrl("https://evil.example", "/vacancies/abc"));
        Assert.Equal("/home", AuthRedirects.ResolveRequestedReturnUrl("/login", "/login?x=1"));
    }

    [Fact]
    public void Session_return_url_strips_query_and_fragment()
    {
        Assert.Equal(
            "/employer/vacancies",
            AuthRedirects.ResolveSessionReturnUrl("/employer/vacancies?email=secret@jobsy.local#frag"));
        Assert.Equal("", AuthRedirects.PathOnly(null));
        Assert.Equal("/home", AuthRedirects.PathOnly("/home?token=abc"));
    }

    [Fact]
    public void AppendReturnUrl_sanitizes_and_keeps_existing_query()
    {
        Assert.Equal(
            "/login?error=session-expired&returnUrl=%2Femployer%2Fvacancies",
            AuthRedirects.AppendReturnUrl("/login?error=session-expired", "/employer/vacancies"));
        Assert.Equal(
            "/login?error=invalid&returnUrl=%2Fhome",
            AuthRedirects.AppendReturnUrl("/login?error=invalid", "https://evil.example"));
    }
}

public class RoleNavCatalogTests
{
    [Fact]
    public void ForUser_anonymous_gets_empty_nav()
    {
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(new ClaimsIdentity()));
        Assert.Empty(items);
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
        Assert.True(RoleNavCatalog.IsActive(
            items.First(i => i.Href == "/admin/settings"),
            "admin/notifications"));
        Assert.True(RoleNavCatalog.IsActive(
            items.First(i => i.Href == "/admin/finance"),
            "admin/sales-managers"));
    }

    [Fact]
    public void ForUser_candidate_gets_search_how_saved_applications_profile()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Candidate)], "test");
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.Equal(5, items.Count);
        Assert.Contains(items, i => i.Href == "/");
        Assert.Contains(items, i => i.Href == "/candidate/hoe-werkt-lobsy");
        Assert.Contains(items, i => i.Href == "/candidate/liked");
        Assert.Contains(items, i => i.Href == "/candidate/applications");
        Assert.Contains(items, i => i.Href == "/candidate/profile");
        Assert.DoesNotContain(items, i => i.Href == "/home");
        var saved = items.First(i => i.Href == "/candidate/liked");
        Assert.Contains("/candidate/shared", saved.ExtraActivePaths ?? []);
        Assert.DoesNotContain("/candidate/applications", saved.ExtraActivePaths ?? []);
    }

    [Fact]
    public void ForUser_branch_and_enterprise_get_applications_nav()
    {
        foreach (var role in new[] { JobsyRoles.BranchManager, JobsyRoles.EnterpriseManager })
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test");
            var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
            Assert.Contains(items, i => i.Href == "/branch/applicants" && i.TitleKey == "Nav.Applications");
            var vacancies = items.First(i => i.TitleKey == "Nav.Vacancies");
            Assert.DoesNotContain("/branch/applicants", vacancies.ExtraActivePaths ?? []);
        }
    }

    [Fact]
    public void ForUser_branch_manager_with_candidate_apps_gets_applications_nav()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, JobsyRoles.BranchManager));
        identity.AddClaim(new Claim(JobsyClaimTypes.HasCandidateApplications, "1"));
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.Contains(items, i => i.Href == "/branch/tokens");
        Assert.Contains(items, i => i.Href == "/candidate/applications");
    }

    [Theory]
    [InlineData(JobsyRoles.BranchManager)]
    [InlineData(JobsyRoles.RegionalManager)]
    [InlineData(JobsyRoles.EnterpriseManager)]
    [InlineData(JobsyRoles.Intermediary)]
    [InlineData(JobsyRoles.SalesManager)]
    [InlineData(JobsyRoles.Ambassadeur)]
    public void ForUser_non_admin_roles_get_how_lobsy_nav(string role)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test");
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.Contains(items, i => i.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(items, i => i.Href == "/candidate/hoe-werkt-lobsy");
        Assert.Equal("Nav.HowLobsyWorks", items[^1].TitleKey);
    }

    [Fact]
    public void ForUser_candidate_keeps_how_lobsy_rightmost()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Candidate)], "test");
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.Equal("/candidate/hoe-werkt-lobsy", items[^1].Href);
    }

    [Fact]
    public void ForUser_optional_applications_do_not_push_how_lobsy_left()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, JobsyRoles.BranchManager));
        identity.AddClaim(new Claim(JobsyClaimTypes.HasCandidateApplications, "1"));
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.Contains(items, i => i.Href == "/candidate/applications");
        Assert.Equal("Nav.HowLobsyWorks", items[^1].TitleKey);
    }

    [Fact]
    public void ForUser_admin_does_not_get_how_lobsy_nav()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Admin)], "test");
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
        Assert.DoesNotContain(items, i => i.TitleKey == "Nav.HowLobsyWorks");
        Assert.DoesNotContain(items, i => i.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(items, i => i.Href == "/candidate/hoe-werkt-lobsy");
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
    public void IsActive_tokens_does_not_highlight_vacancies()
    {
        var items = RoleNavCatalog.Branch;
        var vacancies = items.First(i => i.Href == "/branch/vacancies");
        var tokens = items.First(i => i.Href == "/branch/tokens");

        Assert.True(RoleNavCatalog.IsActive(tokens, "branch/tokens", items));
        Assert.False(RoleNavCatalog.IsActive(vacancies, "branch/tokens", items));
        Assert.True(RoleNavCatalog.IsActive(vacancies, "branch/vacancies/new", items));
    }

    [Fact]
    public void TokensHrefFor_branch_manager()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.BranchManager)], "test");
        Assert.Equal("/branch/tokens", RoleNavCatalog.TokensHrefFor(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void ForUser_enterprise_keeps_ops_nav_and_desktop_organization_hub()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.EnterpriseManager)], "test");
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));

        Assert.Contains(items, i => i.Href == "/home");
        Assert.Contains(items, i => i.Href == "/employer/vacancies");
        Assert.Contains(items, i => i.Href == "/employer/tokens");
        Assert.Contains(items, i => i.Href == "/employer/users");
        Assert.Contains(items, i => i.Href == "/employer/organization" && i.DesktopOnly);

        Assert.DoesNotContain(items, i => i.Href == "/employer/salary-tables");
        Assert.DoesNotContain(items, i => i.Href == "/employer/branches");
        Assert.DoesNotContain(items, i => i.Href == "/employer/regions");
        Assert.DoesNotContain(items, i => i.Href == "/employer/csv-import");
        Assert.DoesNotContain(items, i => i.Href == "/employer/company");

        var org = items.First(i => i.Href == "/employer/organization");
        Assert.True(RoleNavCatalog.IsActive(org, "employer/salary-tables", items));
        Assert.True(RoleNavCatalog.IsActive(org, "employer/csv-import", items));
        Assert.True(RoleNavCatalog.IsActive(org, "employer/branches", items));
        Assert.False(RoleNavCatalog.IsActive(org, "employer/users", items));
    }

    [Fact]
    public void ForUser_intermediary_has_no_batch_tool()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Intermediary)], "test");
        var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));

        Assert.Contains(items, i => i.Href == "/intermediary");
        Assert.Contains(items, i => i.Href == "/employer/vacancies");
        Assert.Contains(items, i => i.Href == "/employer/tokens");
        Assert.DoesNotContain(items, i => i.Href == "/intermediary/batch");
        Assert.DoesNotContain(items, i => i.TitleKey == "Nav.BatchTool");
    }

    [Fact]
    public void ClusterAroundAssistant_candidate_puts_three_left_and_how_on_the_right()
    {
        var clusters = RoleNavCatalog.ClusterAroundAssistant(RoleNavCatalog.Candidate);

        Assert.Equal(new[] { "Nav.Search", "Nav.Saved", "Nav.MyApplications" }, clusters.Left.Select(i => i.TitleKey));
        Assert.Equal(new[] { "Nav.Profile", "Nav.HowLobsyWorks" }, clusters.Right.Select(i => i.TitleKey));
        Assert.Empty(clusters.Overflow);
        Assert.False(clusters.HasOverflow);
    }

    [Fact]
    public void ClusterAroundAssistant_admin_splits_six_items_evenly()
    {
        var clusters = RoleNavCatalog.ClusterAroundAssistant(RoleNavCatalog.Admin);

        Assert.Equal(3, clusters.Left.Count);
        Assert.Equal(3, clusters.Right.Count);
        Assert.Empty(clusters.Overflow);
        Assert.Equal("Nav.Home", clusters.Left[0].TitleKey);
        Assert.Equal("Nav.Settings", clusters.Right[^1].TitleKey);
    }

    [Fact]
    public void ClusterAroundAssistant_keeps_how_lobsy_rightmost_and_overflows_the_rest()
    {
        var clusters = RoleNavCatalog.ClusterAroundAssistant(RoleNavCatalog.Enterprise);

        Assert.Equal(3, clusters.Left.Count);
        Assert.Equal("Nav.HowLobsyWorks", clusters.Right[^1].TitleKey);
        Assert.True(clusters.HasOverflow);
        Assert.Contains(clusters.Overflow, i => i.Href == "/employer/organization" && i.DesktopOnly);
        Assert.True(clusters.Left.Count + clusters.Right.Count <= 6);
        Assert.DoesNotContain(clusters.Left.Concat(clusters.Right), i => i.TitleKey == "Nav.More");
    }

    [Fact]
    public void ClusterAroundAssistant_empty_catalog_is_empty()
        => Assert.Equal(BottomNavClusters.Empty, RoleNavCatalog.ClusterAroundAssistant([]));
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
