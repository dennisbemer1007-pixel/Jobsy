using System.Security.Claims;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;
using Jobsy.Web.Auth;
using Jobsy.Web.Components.Admin;
using Jobsy.Web.Components.Employer;
using Jobsy.Web.Help;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Uat;

/// <summary>
/// Executes one UAT grid row: route/authz contract, bottom-nav, how-to deep links,
/// chrome controls, and domain rules referenced by the scenario text.
/// </summary>
public static class UatScriptRunner
{
    private static readonly Lazy<RazorRouteIndex> Routes = new(RazorRouteIndex.Load);

    private static readonly Regex PathRx = new(
        @"`(/[a-zA-Z0-9{}_?=&./+*-]*)`",
        RegexOptions.Compiled);

    private static readonly string[] AuthEndpoints =
    [
        "/account/login",
        "/account/logout",
        "/account/demo-login",
        "/account/external/entra",
        "/account/external/google",
        "/account/session-activity",
        "/account/session-security"
    ];

    private static readonly HashSet<string> KnownMissingRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/admin/launch"
    };

    public static void Execute(UatScenario scenario)
    {
        var blob = scenario.Scenario + " " + scenario.Expected;
        var roles = ExpandRoles(scenario.Role);
        Assert.NotEmpty(roles);

        AssertChromeContracts(scenario, blob);

        foreach (var role in roles)
        {
            ExecuteForRole(scenario, blob, role);
        }
    }

    private static void ExecuteForRole(UatScenario scenario, string blob, string? jobsyRole)
    {
        AssertNavigation(scenario, blob, jobsyRole);
        AssertHowTo(scenario, blob, jobsyRole);
        AssertMentionedRoutes(scenario, blob, jobsyRole);
        AssertDomainRules(scenario, blob, jobsyRole);
    }

    private static void AssertChromeContracts(UatScenario scenario, string blob)
    {
        var root = RepoRoot.Find();
        if (Contains(blob, "cookie", "Alleen noodzakelijk", "Accepteer cookies"))
        {
            var cookie = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/CookieConsentBanner.razor"));
            Assert.Contains("Alleen noodzakelijk", cookie, StringComparison.Ordinal);
            Assert.Contains("Accepteer cookies", cookie, StringComparison.Ordinal);
            Assert.Contains("href=\"/privacy\"", cookie, StringComparison.Ordinal);
        }

        if (Contains(blob, "footer", "Privacy", "Algemene voorwaarden", "Gebruiksvoorwaarden", "Wie zijn wij", "Westland"))
        {
            var footer = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Layout/AppFooter.razor"));
            Assert.Contains("href=\"/privacy\"", footer, StringComparison.Ordinal);
            Assert.Contains("href=\"/algemene-voorwaarden\"", footer, StringComparison.Ordinal);
            Assert.Contains("href=\"/gebruiksvoorwaarden\"", footer, StringComparison.Ordinal);
            Assert.Contains("href=\"/wie-zijn-wij\"", footer, StringComparison.Ordinal);
            Assert.Contains("href=\"/westland\"", footer, StringComparison.Ordinal);
        }

        if (Contains(blob, "Feedback"))
        {
            var feedback = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Feedback/FeedbackWidget.razor"));
            Assert.Contains("Feedback.Type.Bug", feedback, StringComparison.Ordinal);
            Assert.Contains("Feedback.Type.Error", feedback, StringComparison.Ordinal);
            Assert.Contains("Feedback.Type.Feature", feedback, StringComparison.Ordinal);
        }

        if (Contains(blob, "Lobsy-assistent", "Assistent"))
        {
            Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/Components/LobsyAssistantChat.razor")));
        }

        if (Contains(blob, "ShareModal", "WhatsApp", "Kopieer link") && Contains(blob, "Delen", "share", "Share"))
        {
            var share = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/ShareModal.razor"));
            Assert.Contains("WhatsApp", share, StringComparison.OrdinalIgnoreCase);
        }

        if (Contains(blob, "PublishOptions", "Publiceren", "PushBom", "Highlight")
            && Contains(scenario.Role, "Filiaalmanager", "Bedrijfsmanager", "Intermediair", "Admin"))
        {
            Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/Components/PublishOptionsDialog.razor")));
        }

        if (Contains(blob, "Consent", "Akkoord en verder", "NeedsConsentReaccept"))
        {
            Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/Components/ConsentReacceptDialog.razor")));
        }

        if (Contains(blob, "Taalkeuze", "Nederlands", "English", "العربية", "Polski", "Română", "RTL"))
        {
            Assert.Contains(JobsyLanguages.All, l => l.Code == "nl");
            Assert.Contains(JobsyLanguages.All, l => l.Code == "en");
            Assert.Contains(JobsyLanguages.All, l => l.Code == "pl");
            Assert.Contains(JobsyLanguages.All, l => l.Code == "ro");
            Assert.Contains(JobsyLanguages.All, l => l.Code == "ar" && l.IsRightToLeft);
        }

        if (Contains(blob, "evil.example", "open redirect", "returnUrl=https"))
        {
            Assert.Equal("/home", AuthRedirects.SafeLocalUrl("https://evil.example"));
            Assert.Equal("/home", AuthRedirects.SafeLocalUrl("/login?returnUrl=https://evil.com"));
            Assert.Equal("/admin/users", AuthRedirects.SafeLocalUrl("/admin/users"));
        }

        if (Contains(blob, "IBAN", "MaskedIban", "full IBAN", "voluit"))
        {
            var masked = ISalesManagerPayoutService.MaskIban("NL91ABNA0417164300");
            Assert.DoesNotContain("0417164300", masked, StringComparison.Ordinal);
            Assert.StartsWith("NL", masked, StringComparison.Ordinal);
            Assert.Contains("**", masked, StringComparison.Ordinal);
        }

        if (Contains(blob, "session-expired"))
        {
            var login = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Login.razor"));
            Assert.Contains("error=session-expired", login + File.ReadAllText(Path.Combine(root, "Jobsy.Web/Security/SessionInactivityMiddleware.cs")), StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertNavigation(UatScenario scenario, string blob, string? jobsyRole)
    {
        var principal = PrincipalFor(jobsyRole);
        var items = RoleNavCatalog.ForUser(principal);

        if (jobsyRole is null)
        {
            if (Contains(blob, "bottom-nav", "Bottom-nav", "geen bottom"))
            {
                Assert.Empty(items);
            }

            return;
        }

        if (Contains(blob, "bottom-nav", "Bottom-nav", "Elke bottom-nav", "rondklikken"))
        {
            Assert.NotEmpty(items);
            foreach (var item in items)
            {
                AssertRouteExistsOrAuthEndpoint(item.Href, $"{scenario.Id}: nav {jobsyRole} → {item.Href}");
            }
        }

        if (Contains(blob, "TokenWalletChip", "Tokenchip", "Tokens chip"))
        {
            var href = RoleNavCatalog.TokensHrefFor(principal);
            var employer = JobsyRoles.IsEmployer(Enum.Parse<Jobsy.Core.Enums.UserRole>(jobsyRole));
            if (Contains(scenario.Expected, "Niet zichtbaar", "Verborgen", "hidden")
                && !Contains(scenario.Expected, "Mijn Saldo"))
            {
                if (jobsyRole is JobsyRoles.Candidate or JobsyRoles.SalesManager or JobsyRoles.Ambassadeur)
                {
                    Assert.False(employer);
                }
            }
            else if (employer)
            {
                Assert.True(href is "/branch/tokens" or "/employer/tokens");
            }
        }

        if (string.Equals(jobsyRole, JobsyRoles.EnterpriseManager, StringComparison.Ordinal)
            && Contains(blob, "Organisatie", "desktop-only", "Desktop"))
        {
            Assert.Contains(items, i => i.Href == "/employer/organization" && i.DesktopOnly);
            foreach (var module in EnterpriseNavItems.OrganizationModules)
            {
                AssertRouteExistsOrAuthEndpoint(module.Href, $"{scenario.Id}: org module {module.Href}");
            }
        }

        if (string.Equals(jobsyRole, JobsyRoles.Admin, StringComparison.Ordinal)
            && Contains(blob, "Settings-subnav", "settings-subnav", "16 modules"))
        {
            foreach (var module in AdminNavItems.SettingsModules)
            {
                AssertRouteExistsOrAuthEndpoint(module.Href, $"{scenario.Id}: admin settings {module.Href}");
            }
        }
    }

    private static void AssertHowTo(UatScenario scenario, string blob, string? jobsyRole)
    {
        if (jobsyRole is null || !Contains(blob, "How-to", "Hoe werkt Lobsy", "how-to", "deep link"))
        {
            return;
        }

        var guide = HowLobsyRoleGuides.ForRole(jobsyRole);
        if (string.Equals(jobsyRole, JobsyRoles.Admin, StringComparison.Ordinal))
        {
            Assert.Null(guide);
            return;
        }

        if (guide is null)
        {
            return;
        }

        AssertRouteExistsOrAuthEndpoint(guide.Primary.Href, $"{scenario.Id}: how-to primary");
        if (guide.Secondary is not null)
        {
            AssertRouteExistsOrAuthEndpoint(guide.Secondary.Href, $"{scenario.Id}: how-to secondary");
        }

        foreach (var step in guide.Steps)
        {
            foreach (var link in step.Links.Where(l => l.Href is not "#" and not ""))
            {
                AssertRouteExistsOrAuthEndpoint(link.Href, $"{scenario.Id}: how-to {link.Href}");
            }
        }
    }

    private static void AssertMentionedRoutes(UatScenario scenario, string blob, string? jobsyRole)
    {
        var scenarioPaths = ExtractPaths(scenario.Scenario)
            .Select(RazorRouteIndex.CanonicalPath)
            .Where(IsPagePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var accessCheck = scenarioPaths.Count == 1;

        foreach (var raw in ExtractPaths(blob))
        {
            if (!IsPagePath(raw))
            {
                continue;
            }

            var path = Alias(RazorRouteIndex.CanonicalPath(raw));
            if (path.Contains("{kvk", StringComparison.OrdinalIgnoreCase)
                || path.Contains("8-digit", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Contains(Routes.Value.Pages, p => p.Templates.Any(t => t.Contains("KvkNumber", StringComparison.Ordinal)));
                continue;
            }

            if (AuthEndpoints.Any(e => path.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (path.EndsWith("/*", StringComparison.Ordinal))
            {
                var prefix = path[..^2];
                var under = Routes.Value.Under(prefix);
                Assert.True(under.Count > 0, $"{scenario.Id}: no pages under {prefix}");
                if (jobsyRole is not null && ExpectsDenied(scenario, prefix) && !string.Equals(jobsyRole, JobsyRoles.Admin, StringComparison.Ordinal))
                {
                    Assert.All(under.Take(12), p =>
                        Assert.False(
                            RazorRouteIndex.RoleMayOpen(p, jobsyRole),
                            $"{scenario.Id}: {jobsyRole} should not open {p.Templates[0]}"));
                }

                continue;
            }

            if (KnownMissingRoutes.Contains(path))
            {
                Assert.Null(Routes.Value.Find(path));
                continue;
            }

            var page = Routes.Value.Find(path);
            if (page is null)
            {
                if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("checkout-stub", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("payout-checkout", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Assert.Fail($"{scenario.Id}: route {path} has no Blazor @page (role {scenario.Role}).");
            }

            var allowed = RazorRouteIndex.RoleMayOpen(page, jobsyRole);
            if (jobsyRole is null && page.Authorize && !page.AllowAnonymous)
            {
                Assert.False(allowed, $"{scenario.Id}: guest must not open {path}.");
                continue;
            }

            if (!accessCheck || !string.Equals(path, scenarioPaths[0], StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ExpectsDenied(scenario, path))
            {
                Assert.False(
                    allowed,
                    $"{scenario.Id}: expected deny for {scenario.Role} on {path} ({page.FilePath}).");
            }
            else if (ExpectsAllowed(scenario, jobsyRole, path))
            {
                Assert.True(
                    allowed,
                    $"{scenario.Id}: expected {scenario.Role} to open {path} ({page.FilePath}).");
            }
        }
    }

    private static string Alias(string path)
        => path.Equals("/employer/partner-sales", StringComparison.OrdinalIgnoreCase)
            ? "/employer/sales"
            : path;

    private static bool IsPagePath(string raw)
    {
        var path = RazorRouteIndex.CanonicalPath(raw);
        return !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("://", StringComparison.Ordinal);
    }

    private static void AssertDomainRules(UatScenario scenario, string blob, string? jobsyRole)
    {
        if (Contains(blob, "PII", "progressive", "vóór Accept", "pre-accept", "PiiRevealed"))
        {
            Assert.False(ApplicationRules.IsPiiRevealed(Jobsy.Core.Enums.ApplicationStatus.Pending));
            Assert.True(ApplicationRules.IsPiiRevealed(Jobsy.Core.Enums.ApplicationStatus.Accepted));
            Assert.True(ApplicationRules.IsPiiRevealed(Jobsy.Core.Enums.ApplicationStatus.EmployerContacting));
            Assert.True(ApplicationRules.IsPiiRevealed(Jobsy.Core.Enums.ApplicationStatus.Hired));
        }

        if (Contains(blob, "pas na Hired", "e-mail/telefoon pas"))
        {
            Assert.False(ApplicationRules.IsDirectContactRevealed(Jobsy.Core.Enums.ApplicationStatus.Accepted));
            Assert.False(ApplicationRules.IsDirectContactRevealed(Jobsy.Core.Enums.ApplicationStatus.EmployerContacting));
            Assert.True(ApplicationRules.IsDirectContactRevealed(Jobsy.Core.Enums.ApplicationStatus.Hired));
        }

        if (Contains(blob, "Gulden Middenweg", "match < 50", "vangnet", "ViaSafetyNet"))
        {
            Assert.Equal(50, MatchScoreWeights.GuldenMiddenwegThreshold);
        }

        if (Contains(blob, "VacancyLifecycle", "Publiceren") && jobsyRole == JobsyRoles.RegionalManager)
        {
            Assert.False(JobsyRoles.CanManageVacancyLifecycle(Jobsy.Core.Enums.UserRole.RegionalManager));
            Assert.False(JobsyRoles.CanReactToApplications(Jobsy.Core.Enums.UserRole.RegionalManager));
        }

        if (Contains(blob, "PostLogin", "na login vanaf `/`"))
        {
            Assert.Equal("/home", AuthRedirects.PostLoginUrl("/"));
            Assert.Equal("/home", AuthRedirects.PostLoginUrl("/banen"));
        }
    }

    private static void AssertRouteExistsOrAuthEndpoint(string href, string because)
    {
        var path = RazorRouteIndex.CanonicalPath(href);
        if (path is "#" or "")
        {
            return;
        }

        if (AuthEndpoints.Any(e => path.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (KnownMissingRoutes.Contains(path))
        {
            return;
        }

        var page = Routes.Value.Find(path);
        Assert.True(page is not null, because);
    }

    private static bool ExpectsDenied(UatScenario scenario, string path)
    {
        if (Contains(scenario.Scenario, "IDOR", "Uitloggen", "Back-button"))
        {
            return false;
        }

        if (path is "/login" or "/access-denied" or "/Error" or "/register")
        {
            return false;
        }

        return Contains(scenario.Expected,
            "403",
            "Login-challenge",
            "login-challenge",
            "Geen toegang",
            "Authorize-fail",
            "access-denied",
            "Access Denied",
            "Redirect naar login");
    }

    private static bool ExpectsAllowed(UatScenario scenario, string? jobsyRole, string path)
    {
        if (ExpectsDenied(scenario, path))
        {
            return false;
        }

        if (jobsyRole is null)
        {
            return true;
        }

        return scenario.Scenario.Contains($"`{path}`", StringComparison.OrdinalIgnoreCase)
            || scenario.Scenario.Contains("Open `" + path, StringComparison.OrdinalIgnoreCase)
            || scenario.Scenario.Contains("Open `/", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string?> ExpandRoles(string roleCell)
    {
        var text = roleCell.Trim();
        if (text.Equals("Gast", StringComparison.OrdinalIgnoreCase))
        {
            return [null];
        }

        if (text.Equals("Alle rollen", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Alle rollen", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                null,
                JobsyRoles.Candidate,
                JobsyRoles.BranchManager,
                JobsyRoles.RegionalManager,
                JobsyRoles.EnterpriseManager,
                JobsyRoles.Intermediary,
                JobsyRoles.SalesManager,
                JobsyRoles.Ambassadeur,
                JobsyRoles.Admin
            ];
        }
        if (text.Contains("Alle ingelogde rollen", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                JobsyRoles.Candidate,
                JobsyRoles.BranchManager,
                JobsyRoles.RegionalManager,
                JobsyRoles.EnterpriseManager,
                JobsyRoles.Intermediary,
                JobsyRoles.SalesManager,
                JobsyRoles.Ambassadeur,
                JobsyRoles.Admin
            ];
        }

        if (text.Contains("Alle werkgeverrollen", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Werkgever", StringComparison.OrdinalIgnoreCase) && !text.Contains("Filiaal", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                JobsyRoles.BranchManager,
                JobsyRoles.RegionalManager,
                JobsyRoles.EnterpriseManager,
                JobsyRoles.Intermediary
            ];
        }

        if (text.Contains("Non-kandidaat", StringComparison.OrdinalIgnoreCase)
            || text.Contains("niet-kandidaat", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                JobsyRoles.BranchManager,
                JobsyRoles.RegionalManager,
                JobsyRoles.EnterpriseManager,
                JobsyRoles.Intermediary,
                JobsyRoles.SalesManager,
                JobsyRoles.Ambassadeur,
                JobsyRoles.Admin
            ];
        }

        var parts = text.Split(['/', ',', '&'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var roles = new List<string?>();
        foreach (var part in parts)
        {
            var cleaned = part
                .Replace("BranchManager", "Filiaalmanager", StringComparison.OrdinalIgnoreCase)
                .Replace("(", "")
                .Replace(")", "")
                .Trim();
            if (cleaned.Equals("Gast", StringComparison.OrdinalIgnoreCase))
            {
                roles.Add(null);
                continue;
            }

            var mapped = RazorRouteIndex.ToJobsyRole(cleaned.Split(' ', 2)[0]);
            if (mapped is null && cleaned.Contains("Kandidaat", StringComparison.OrdinalIgnoreCase))
            {
                mapped = JobsyRoles.Candidate;
            }

            if (mapped is not null && !roles.Contains(mapped))
            {
                roles.Add(mapped);
            }
        }

        if (roles.Count == 0)
        {
            // Combined labels like "Filiaalmanager / Bedrijfsmanager / Intermediair"
            // already split; leftover adjectives ("ingelogd") yield empty — treat as no-op role skip.
            var fallback = RazorRouteIndex.ToJobsyRole(text);
            if (fallback is not null)
            {
                return [fallback];
            }
        }

        return roles.Count == 0 ? [JobsyRoles.Candidate] : roles;
    }

    private static ClaimsPrincipal PrincipalFor(string? jobsyRole)
    {
        if (jobsyRole is null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var identity = new ClaimsIdentity("uat");
        identity.AddClaim(new Claim(ClaimTypes.Role, jobsyRole));
        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> ExtractPaths(string blob)
        => PathRx.Matches(blob)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static bool Contains(string haystack, params string[] needles)
        => needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));
}
