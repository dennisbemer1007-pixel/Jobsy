using System.Security.Claims;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
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
            var assistant = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/LobsyAssistantChat.razor"));
            Assert.Contains("lobsy-assistant-tab", assistant, StringComparison.Ordinal);
            Assert.DoesNotContain("lobsy-assistant__fab", assistant, StringComparison.Ordinal);
        }

        if (Contains(blob, "Hoe werkt Lobsy")
            && Contains(blob, "account-menu", "Account-menu", "userknop"))
        {
            var header = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Layout/AuthHeader.razor"));
            Assert.Contains("HowLobsyHrefFor", header, StringComparison.Ordinal);
            Assert.Contains("Nav.HowLobsyWorks", header, StringComparison.Ordinal);
            var nav = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Layout/BottomNav.razor"));
            Assert.DoesNotContain("Nav.HowLobsyWorks", nav, StringComparison.Ordinal);
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
            Assert.Equal(17, AdminNavItems.SettingsModules.Length);
            foreach (var module in AdminNavItems.SettingsModules)
            {
                AssertRouteExistsOrAuthEndpoint(module.Href, $"{scenario.Id}: admin settings {module.Href}");
            }

            var root = RepoRoot.Find();
            var settingsNav = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Admin/AdminSettingsSubnav.razor"));
            Assert.Contains("admin-sublinks--wrap", settingsNav, StringComparison.Ordinal);
            Assert.DoesNotContain("pill-scroller", settingsNav, StringComparison.Ordinal);
            var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
            Assert.Contains(".admin-sublinks.admin-sublinks--wrap", css, StringComparison.Ordinal);
            Assert.Contains("flex-wrap: wrap", css, StringComparison.Ordinal);
        }
    }

    private static void AssertHowTo(UatScenario scenario, string blob, string? jobsyRole)
    {
        if (jobsyRole is null || !Contains(blob, "How-to", "Hoe werkt Lobsy", "how-to", "deep link"))
        {
            return;
        }

        var guide = HowLobsyRoleGuides.ForRole(jobsyRole);
        var howHref = RoleNavCatalog.HowLobsyHrefFor(PrincipalFor(jobsyRole));
        if (string.Equals(jobsyRole, JobsyRoles.Admin, StringComparison.Ordinal))
        {
            Assert.Null(guide);
            Assert.Null(howHref);
            return;
        }

        Assert.False(string.IsNullOrWhiteSpace(howHref));
        AssertRouteExistsOrAuthEndpoint(howHref!, $"{scenario.Id}: how-to account-menu href");

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

        if (Contains(blob, "competentietest", "Competenties", "Top 10 vacatures", "60%"))
        {
            Assert.Equal(25, CompetencyTestCatalog.QuestionCount);
            Assert.Equal(60, ProfileVacancyMatchCalculator.DisplayThreshold);
            Assert.Equal(10, ProfileVacancyMatchCalculator.MaxResults);
            Assert.Equal(4, CompetencyTestCatalog.CategoryCodes.Length);
        }

        if (Contains(blob, "Beroepentest", "RIASEC", "Holland-code", "Beroepen-kompas"))
        {
            Assert.Equal(25, CareerTestCatalog.QuestionCount);
            Assert.Equal(6, CareerTestCatalog.RiasecCodes.Length);
        }

        if (Contains(blob, "Mijn Beroepen-kompas", "Super-match", "Wat betekent dit voor jou?"))
        {
            Assert.Equal(95, CareerCompassBuilder.SuperMatchMin);
            Assert.Equal(85, CareerCompassBuilder.StrongMatchMin);
            Assert.Equal(75, CareerCompassBuilder.BroadenMin);
            Assert.Equal(0.32, ProfileVacancyMatchCalculator.InterestWeightDeepAnalysis);
            Assert.True(ProfileVacancyMatchCalculator.InterestWeightDeepAnalysis
                        > ProfileVacancyMatchCalculator.InterestWeightQuickScan);
            Assert.True(ProfileVacancyMatchCalculator.InterestWeightDeepAnalysisOnly
                        > ProfileVacancyMatchCalculator.InterestWeightQuickScanOnly);

            var root = RepoRoot.Find();
            var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerCompassPanel.razor"));
            Assert.Contains("Kompas.BandSuper", panel, StringComparison.Ordinal);
            Assert.Contains("Kompas.BandStrong", panel, StringComparison.Ordinal);
            Assert.Contains("Kompas.BandBroaden", panel, StringComparison.Ordinal);
            Assert.Contains("Kompas.PracticalTitle", panel, StringComparison.Ordinal);
            Assert.DoesNotContain("RIASEC", panel, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("OCEAN", panel, StringComparison.OrdinalIgnoreCase);

            var pdf = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/AssessmentReportPdfService.cs"));
            Assert.Contains("GetBrandLogoPng", pdf, StringComparison.Ordinal);
            Assert.Contains("Wat betekent dit voor jou?", pdf, StringComparison.Ordinal);
            Assert.Contains("Jouw loopbaanrapport", pdf, StringComparison.Ordinal);

            Assert.Equal("Mijn Beroepen-kompas", Jobsy.Web.Localization.UiStrings.Get("Kompas.Career", "nl"));
        }

        if (Contains(blob, "OpenAI", "algemene beroepen", "Nederlandse arbeidsmarkt", "zoeksleutels"))
        {
            Assert.Equal(0.65, ProfileVacancyMatchCalculator.OccupationFitWeight);
            Assert.Equal(0.35, ProfileVacancyMatchCalculator.RiasecFitWeight);
            var gen = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Infrastructure/Services/CareerCompassGenerationService.cs"));
            Assert.Contains("CareerCompassPrompt.System", gen, StringComparison.Ordinal);
            Assert.Contains("response body not logged", gen, StringComparison.Ordinal);
            Assert.Contains("json_object", gen, StringComparison.Ordinal);
            var prompt = CareerCompassPrompt.System;
            Assert.Contains("Nederlandse arbeidsmarkt", prompt, StringComparison.Ordinal);
            Assert.Contains("extraversie", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("95-100", prompt, StringComparison.Ordinal);
            Assert.DoesNotContain("Lobsy-vacature", prompt, StringComparison.OrdinalIgnoreCase);
            var merge = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Infrastructure/Services/DeepAnalysisService.cs"));
            Assert.Contains("CompassJson", merge, StringComparison.Ordinal);
            Assert.Contains("GenerateFromCareerDeepAsync", merge, StringComparison.Ordinal);
            var match = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Infrastructure/Services/ProfileVacancyMatchService.cs"));
            Assert.Contains("CareerOccupations", match, StringComparison.Ordinal);
        }

        if (Contains(blob, "Diepte-analyse", "150 vragen", "200 vragen"))
        {
            Assert.Equal(150, DeepAnalysisCatalog.QuestionCount);
            Assert.Equal(200, DeepAnalysisCatalog.CareerQuestionCount);
            Assert.Equal(2.99m, FlexCommercialSettings.DefaultDeepAnalysisPriceEuro);
            Assert.Equal(150, DeepAnalysisCatalog.Questions.Select(q => q.PromptNl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(200, DeepAnalysisCatalog.CareerQuestions.Select(q => q.PromptNl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.DoesNotContain(DeepAnalysisCatalog.Questions, q => q.PromptNl.Contains("variant", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(DeepAnalysisCatalog.CareerQuestions, q => q.PromptNl.Contains("variant", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(30, DeepAnalysisCatalog.CompetenceItemsPerDomain);
        }

        if (Contains(blob, "Mijn Lobsy Kompas", "match-%", "Beste match", ">80% match"))
        {
            var root = RepoRoot.Find();
            var home = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateHomePanel.razor"));
            Assert.Contains("CandidateKompas", home, StringComparison.Ordinal);
            var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
            Assert.Contains("Kompas.TabDna", kompas, StringComparison.Ordinal);
            Assert.Contains("Kompas.TabProfile", kompas, StringComparison.Ordinal);
            Assert.Contains("Kompas.TabTests", kompas, StringComparison.Ordinal);
            Assert.Contains("Kompas.TabFit", kompas, StringComparison.Ordinal);
            Assert.DoesNotContain("Kompas.TabCompetencies", kompas, StringComparison.Ordinal);
            Assert.DoesNotContain("period-tabs", home, StringComparison.Ordinal);
            Assert.DoesNotContain("Kompas.OpenMap", kompas, StringComparison.Ordinal);
            Assert.Contains("role=\"tablist\"", kompas, StringComparison.Ordinal);
            Assert.Contains("RoleFitCheckPanel", kompas, StringComparison.Ordinal);
            Assert.Contains("<CandidateKompas", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor")), StringComparison.Ordinal);
            Assert.Equal(RoleFitCheckCopy.Locked, Jobsy.Web.Localization.UiStrings.Get("Fit.Locked", "nl"));
            var dto = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Models/VacancyListItemDto.cs"));
            Assert.Contains("MatchPercent", dto, StringComparison.Ordinal);
            Assert.Contains("minMatchPercent", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/VacanciesController.cs")), StringComparison.Ordinal);
            Assert.True(TransportLabels.Parse("E-bike") == Jobsy.Core.Enums.TransportMode.Bike);
        }

        if (Contains(blob, "Wie ben ik?", "persoonsprofiel", "Lobsy-CV-bijlage"))
        {
            var root = RepoRoot.Find();
            var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/WhoAmIPanel.razor"));
            Assert.Contains("WhoAmI.AttachCv", panel, StringComparison.Ordinal);
            Assert.Contains("CultureScorePanel", panel, StringComparison.Ordinal);
            Assert.False(WhoAmICompleteness.IsUnlocked(false, true, true, true));
            Assert.True(WhoAmICompleteness.IsUnlocked(true, true, true, true));
            Assert.DoesNotContain("@", WhoAmIPrompt.User(
                new CompetencyScores(80, 70, 60, 50),
                new RiasecScores(80, 40, 30, 50, 20, 10),
                new CulturePersonalityScores(
                Autonomy: 70, Informal: 60, Collaboration: 80, Flexibility: 55, Innovation: 50, PeopleFirst: 65,
                Openness: 55, Conscientiousness: 70, Extraversion: 60, Agreeableness: 75, EmotionalStability: 70)), StringComparison.Ordinal);
        }

        if (Contains(blob, "accordeon per vaardigheid", "workshops"))
        {
            var root = RepoRoot.Find();
            var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CompetencyScorePanel.razor"));
            Assert.Contains("competency-skill__details", panel, StringComparison.Ordinal);
            Assert.Contains("TrainingOffersBlock", panel, StringComparison.Ordinal);
            Assert.Contains("CampaignCompetence", panel, StringComparison.Ordinal);
            Assert.Contains("CompetencyTrainingCatalog.MeaningKey", panel, StringComparison.Ordinal);
            Assert.DoesNotContain("OCEAN", panel, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RIASEC", panel, StringComparison.OrdinalIgnoreCase);
            var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
            Assert.Contains("TestsOverviewPanel", kompas, StringComparison.Ordinal);
            Assert.DoesNotContain("Talent.CandidateTitle", kompas, StringComparison.Ordinal);
            Assert.Equal("competence", TrainingTracking.CampaignCompetence);
            Assert.Contains(TrainingFieldCatalog.Vaardigheden, TrainingFieldCatalog.Detect([CompetencyTrainingCatalog.SearchBlob(CompetencyTestCatalog.Samenwerken)]));
        }

        if (Contains(blob, "DISC-Analyse", "gedragsstijl", "workshops"))
        {
            var root = RepoRoot.Find();
            var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CultureScorePanel.razor"));
            Assert.Contains("competency-skill__details", panel, StringComparison.Ordinal);
            Assert.Contains("TrainingOffersBlock", panel, StringComparison.Ordinal);
            Assert.Equal(18, CulturePersonalityCatalog.QuestionCount);
            Assert.True(DeepAnalysisCatalog.SupportsDeepAnalysis(AssessmentKind.Culture));
            Assert.Equal(150, DeepAnalysisCatalog.CultureQuestionCount);
            Assert.Equal("culture", TrainingTracking.CampaignCulture);
            Assert.Equal("culture", TrainingTracking.CampaignDisc);
        }

        if (Contains(blob, "Cultuur Fit", "cultuurpijlers"))
        {
            var root = RepoRoot.Find();
            Assert.Equal(3, CulturePillarCatalog.MinSelected);
            Assert.Equal(5, CulturePillarCatalog.MaxSelected);
            Assert.Contains("CultureFitPercent", File.ReadAllText(Path.Combine(root, "Jobsy.Api/Models/VacancyListItemDto.cs")), StringComparison.Ordinal);
            Assert.Contains("CulturePillarsJson", File.ReadAllText(Path.Combine(root, "Jobsy.Core/Entities/Vacancy.cs")), StringComparison.Ordinal);
            var create = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Branch/CreateVacancy.razor"));
            Assert.Contains("CulturePillarCatalog", create, StringComparison.Ordinal);
            var map = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/jobMap.js"));
            Assert.Contains("cultureFitHtml", map, StringComparison.Ordinal);
            Assert.DoesNotContain("OCEAN", File.ReadAllText(Path.Combine(root, "Jobsy.Core/Rules/CultureFitBuilder.cs")), StringComparison.Ordinal);
        }

        if (Contains(blob, "leeftijdsfilter", "talentpool", "ContactUnlock", "48 uur"))
        {
            Assert.Equal(48, TalentContactRules.TalentContactRequestHours);
            Assert.Equal(1m, TalentContactRules.DefaultUnlockCostTokens);
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

        if (string.Equals(jobsyRole, JobsyRoles.EnterpriseManager, StringComparison.Ordinal)
            && Contains(blob, "e-mail+naam+rol+vestigingen"))
        {
            var root = RepoRoot.Find();
            var users = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Employer/Users.razor"));
            Assert.Contains("InviteExtraCompanies", users, StringComparison.Ordinal);
            Assert.Contains("EmployerInviteCompanyOptions", users, StringComparison.Ordinal);

            var orgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var branchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            InviteCompanyOption[] companies =
            [
                new(orgId, "Bemer IT Solutions", "Laan 1", ParentCompanyId: null),
                new(branchId, "Bemer IT Solutions", "Laan 1", orgId, "000012345678")
            ];
            Assert.Empty(EmployerInviteCompanyOptions.ExtraMembershipChoices(companies, branchId));
            Assert.Single(EmployerInviteCompanyOptions.ExtraMembershipChoices(companies, orgId));
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
