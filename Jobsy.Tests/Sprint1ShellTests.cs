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
        // Admin uses AdminLayout sidebar (AdminNav); bottom nav is empty (D1).
        Assert.Empty(items);
        Assert.Empty(RoleNavCatalog.Admin);
        Assert.Contains(AdminNav.AvailableItems(), i => i.Href == "/admin");
        Assert.Contains(AdminNav.AvailableItems(), i => i.Href == "/admin/instellingen");
    }

    [Fact]
    public void ForUser_candidate_gets_search_saved_applications_career_profile()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Candidate)], "test");
        var user = new ClaimsPrincipal(identity);
        var items = RoleNavCatalog.ForUser(user);
        Assert.Equal(5, items.Count);
        Assert.Equal(
            new[]
            {
                "/",
                "/candidate/liked",
                "/candidate/applications",
                "/carriere",
                "/candidate/profile"
            },
            items.Select(i => i.Href));
        Assert.Equal("Nav.Search", items[0].TitleKey);
        Assert.Equal("Nav.Applications", items[2].TitleKey);
        Assert.Equal("Nav.CareerPath", items[3].TitleKey);
        Assert.Equal("Nav.Profile", items[4].TitleKey);
        Assert.DoesNotContain(items, i => i.Href == "/candidate/hoe-werkt-lobsy");
        Assert.DoesNotContain(items, i => i.Href == "/home");
        Assert.DoesNotContain(items, i => i.Href == "/profiel");
        var saved = items.First(i => i.Href == "/candidate/liked");
        Assert.Contains("/candidate/shared", saved.ExtraActivePaths ?? []);
        Assert.DoesNotContain("/candidate/applications", saved.ExtraActivePaths ?? []);
        var profile = items.First(i => i.Href == "/candidate/profile");
        Assert.Contains("/profiel", profile.ExtraActivePaths ?? []);
        Assert.Contains("/home", profile.ExtraActivePaths ?? []);
        Assert.Equal("/candidate/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(user));
    }

    [Fact]
    public void ForUser_branch_and_enterprise_get_empty_catalog_werkgever_owns_nav()
    {
        foreach (var role in new[] { JobsyRoles.BranchManager, JobsyRoles.EnterpriseManager })
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test");
            var items = RoleNavCatalog.ForUser(new ClaimsPrincipal(identity));
            Assert.Empty(items);
        }

        var ctx = new WerkgeverNavContext(CandidateInsightsEnabled: true);
        var bm = WerkgeverNav.For(EmployerRole.Bedrijfsmanager, ctx).SelectMany(g => g.Items);
        Assert.Contains(bm, i => i.Href == "/werkgever/sollicitaties");
    }

    [Fact]
    public void ForUser_branch_manager_with_candidate_apps_uses_werkgever_nav()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, JobsyRoles.BranchManager));
        identity.AddClaim(new Claim(JobsyClaimTypes.HasCandidateApplications, "1"));
        Assert.Empty(RoleNavCatalog.ForUser(new ClaimsPrincipal(identity)));

        var items = WerkgeverNav.For(
            EmployerRole.Vestigingsmanager,
            new WerkgeverNavContext(HasCandidateApplications: true, CandidateInsightsEnabled: true))
            .SelectMany(g => g.Items);
        Assert.Contains(items, i => i.Href == "/werkgever/tokens");
        Assert.Contains(items, i => i.Href == "/candidate/applications");
    }

    [Theory]
    [InlineData(JobsyRoles.BranchManager)]
    [InlineData(JobsyRoles.RegionalManager)]
    [InlineData(JobsyRoles.EnterpriseManager)]
    [InlineData(JobsyRoles.Intermediary)]
    [InlineData(JobsyRoles.SalesManager)]
    [InlineData(JobsyRoles.Ambassadeur)]
    public void HowLobsyHrefFor_employer_sales_and_ambassadeur_use_shared_guide(string role)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test");
        var user = new ClaimsPrincipal(identity);
        var items = RoleNavCatalog.ForUser(user);
        Assert.DoesNotContain(items, i => i.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(items, i => i.Href == "/candidate/hoe-werkt-lobsy");
        Assert.DoesNotContain(items, i => i.Href == "/candidate/vacancies");
        Assert.DoesNotContain(items, i => i.TitleKey == "Nav.HowLobsyWorks");
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(user));
    }

    [Fact]
    public void ForUser_optional_applications_stay_in_werkgever_nav_not_how_lobsy()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, JobsyRoles.BranchManager));
        identity.AddClaim(new Claim(JobsyClaimTypes.HasCandidateApplications, "1"));
        var user = new ClaimsPrincipal(identity);
        Assert.Empty(RoleNavCatalog.ForUser(user));
        var items = WerkgeverNav.For(
            EmployerRole.Vestigingsmanager,
            new WerkgeverNavContext(HasCandidateApplications: true))
            .SelectMany(g => g.Items);
        Assert.Contains(items, i => i.Href == "/candidate/applications");
        Assert.DoesNotContain(items, i => i.Href == "/candidate/vacancies");
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(user));
    }

    [Fact]
    public void ForUser_admin_includes_dedicated_ats_vacancies_nav()
    {
        var admin = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Admin)], "test"));
        Assert.Empty(RoleNavCatalog.ForUser(admin));
        Assert.Contains(AdminNav.AvailableItems(), i => i.Href == "/admin/vacatures/ats" && i.LabelKey == "AdminNav.Ats");

        var candidate = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Candidate)], "test"));
        Assert.DoesNotContain(RoleNavCatalog.ForUser(candidate), i => i.Href.Contains("/admin/", StringComparison.Ordinal));
    }

    [Fact]
    public void HowLobsyHrefFor_admin_and_guests_have_no_guide_link()
    {
        var admin = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Admin)], "test"));
        var items = RoleNavCatalog.ForUser(admin);
        Assert.DoesNotContain(items, i => i.TitleKey == "Nav.HowLobsyWorks");
        Assert.DoesNotContain(items, i => i.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(items, i => i.Href == "/candidate/hoe-werkt-lobsy");
        Assert.Null(RoleNavCatalog.HowLobsyHrefFor(admin));
        Assert.Null(RoleNavCatalog.HowLobsyHrefFor(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.Null(RoleNavCatalog.HowLobsyHrefFor(null));
    }

    [Fact]
    public void IsActive_matches_extra_path_prefix()
    {
        var item = new NavItem("Nav.Vacancies", "/werkgever/vacatures", NavIcons.Vacancies, ["/werkgever/sollicitaties"]);
        Assert.True(RoleNavCatalog.IsActive(item, "werkgever/sollicitaties"));
        Assert.True(RoleNavCatalog.IsActive(item, "werkgever/vacatures"));
        Assert.False(RoleNavCatalog.IsActive(item, "admin/users"));
    }

    [Fact]
    public void IsActive_tokens_does_not_highlight_vacancies()
    {
        var vacancies = new NavItem("WgNav.Vacancies", "/werkgever/vacatures", NavIcons.Vacancies, ["/werkgever/vacatures/nieuw"]);
        var tokens = new NavItem("WgNav.BalanceBuy", "/werkgever/tokens", NavIcons.Tokens);
        var items = new[] { vacancies, tokens };

        Assert.True(RoleNavCatalog.IsActive(tokens, "werkgever/tokens", items));
        Assert.False(RoleNavCatalog.IsActive(vacancies, "werkgever/tokens", items));
        Assert.True(RoleNavCatalog.IsActive(vacancies, "werkgever/vacatures/nieuw", items));
    }

    [Fact]
    public void TokensHrefFor_employer_is_shared()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.BranchManager)], "test");
        Assert.Equal("/werkgever/tokens", RoleNavCatalog.TokensHrefFor(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void ForUser_enterprise_catalog_empty_werkgever_has_org_items()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.EnterpriseManager)], "test");
        Assert.Empty(RoleNavCatalog.ForUser(new ClaimsPrincipal(identity)));
        var items = WerkgeverNav.For(EmployerRole.Bedrijfsmanager, new WerkgeverNavContext(HasTakeovers: true, HasApiOrCsvImport: true))
            .SelectMany(g => g.Items)
            .ToList();
        Assert.Contains(items, i => i.Href == "/werkgever");
        Assert.Contains(items, i => i.Href == "/werkgever/vacatures");
        Assert.Contains(items, i => i.Href == "/werkgever/tokens");
        Assert.Contains(items, i => i.Href == "/werkgever/organisatie/team");
        Assert.Contains(items, i => i.Href == "/werkgever/organisatie/vestigingen");
    }

    [Fact]
    public void ForUser_intermediary_has_no_batch_tool()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, JobsyRoles.Intermediary)], "test");
        Assert.Empty(RoleNavCatalog.ForUser(new ClaimsPrincipal(identity)));
        var items = WerkgeverNav.For(EmployerRole.Intermediair, new WerkgeverNavContext())
            .SelectMany(g => g.Items);
        Assert.Contains(items, i => i.Href == "/intermediary");
        Assert.Contains(items, i => i.Href == "/werkgever/vacatures");
        Assert.Contains(items, i => i.Href == "/werkgever/tokens");
        Assert.DoesNotContain(items, i => i.Href == "/intermediary/batch");
    }

    [Fact]
    public void Talent_nav_extra_paths_live_on_werkgever_catalog()
    {
        var talent = WerkgeverNav.Catalog.SelectMany(g => g.Items).First(i => i.Key == "talent");
        Assert.Contains("/employer/talent-contacts", talent.Aliases);
        var insights = WerkgeverNav.Catalog.SelectMany(g => g.Items).First(i => i.Key == "insights");
        Assert.Contains("/employer/kandidaatinzichten", insights.Aliases);
        var im = WerkgeverNav.For(EmployerRole.Intermediair, new WerkgeverNavContext(CandidateInsightsEnabled: true))
            .SelectMany(g => g.Items);
        Assert.DoesNotContain(im, i => i.Key == "insights");
    }

    [Fact]
    public void Regional_nav_includes_candidate_insights_via_werkgever()
    {
        Assert.Empty(RoleNavCatalog.Regional);
        var items = WerkgeverNav.For(EmployerRole.Regiomanager, new WerkgeverNavContext(CandidateInsightsEnabled: true))
            .SelectMany(g => g.Items);
        Assert.Contains(items, i => i.Href == "/werkgever/kandidaatinzichten");
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
