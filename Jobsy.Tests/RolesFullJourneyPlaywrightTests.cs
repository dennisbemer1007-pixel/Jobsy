using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Human-like navigation per role from ROLES_AND_VIEWS.md at 1280 and 375px with screenshots.
/// Run locally with <c>JOBSY_E2E_ROLES_JOURNEY=1</c>, <c>JOBSY_E2E_BASE_URL=http://127.0.0.1:5201</c>,
/// and <c>ConnectionStrings__JobsyDb</c> (same as candidate full journey).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class RolesFullJourneyPlaywrightTests
{
    private static readonly string DemoClassPath =
        $"/leraar/klas/{DemoSchoolIds.Class1AId:D}";

    private sealed record Step(string Path, string Slug);

    private sealed record RolePlan(
        string Slug,
        string DisplayName,
        string? LoginEmail,
        Step[] Steps,
        bool TapHomeNav = false,
        Step[]? PreLoginSteps = null);

    private static readonly RolePlan[] Plans =
    [
        new(
            "guest",
            "Gast (anoniem)",
            null,
            [
                new Step(E2eRoutes.Banenkaart, "01-banenkaart"),
                new Step("/register", "02-registreren"),
                new Step("/login", "03-inloggen"),
                new Step("/partner", "04-partner"),
                new Step("/hoe-werkt-lobsy", "05-hoe-werkt-lobsy"),
                new Step("/privacy", "06-privacy")
            ]),
        new(
            "branch-manager",
            "Filiaalmanager (BranchManager)",
            "ondernemer@jobsy.local",
            [
                new Step("/home", "01-home"),
                new Step("/employer/vacancies", "02-vacatures"),
                new Step("/branch/applicants", "03-sollicitaties"),
                new Step("/branch/vacancies/new", "04-nieuwe-vacature"),
                new Step("/employer/talent", "05-talentpool"),
                new Step("/branch/tokens", "06-tokens"),
                new Step("/employer/takeovers", "07-overnames")
            ],
            TapHomeNav: true),
        new(
            "regional-manager",
            "Regiomanager",
            "regio@jobsy.local",
            [
                new Step("/home", "01-home"),
                new Step("/employer/vacancies", "02-vacatures"),
                new Step("/regional/branches", "03-vestigingen"),
                new Step("/employer/tokens", "04-tokens")
            ]),
        new(
            "enterprise-manager",
            "Bedrijfsmanager (EnterpriseManager)",
            "enterprise@jobsy.local",
            [
                new Step("/home", "01-home"),
                new Step("/employer/vacancies", "02-vacatures"),
                new Step("/branch/applicants", "03-sollicitaties"),
                new Step("/employer/tokens", "04-tokens"),
                new Step("/employer/users", "05-gebruikers"),
                new Step("/employer/organization", "06-organisatie")
            ]),
        new(
            "intermediary",
            "Intermediair",
            "intermediair@jobsy.local",
            [
                new Step("/home", "01-home"),
                new Step("/intermediary", "02-bedrijvenoverzicht"),
                new Step("/intermediary/team", "03-team"),
                new Step("/employer/vacancies", "04-vacatures"),
                new Step("/employer/tokens", "05-tokens")
            ]),
        new(
            "sales-manager",
            "Salesmanager",
            "sales@jobsy.local",
            [
                new Step("/home", "01-home"),
                new Step("/salesmanager", "02-dashboard"),
                new Step("/salesmanager/toolkit", "03-toolkit"),
                new Step("/salesmanager/referrals", "04-referrals"),
                new Step("/salesmanager/onboarding", "05-onboarding"),
                new Step("/salesmanager/invoices", "06-facturen")
            ]),
        new(
            "ambassadeur",
            "Ambassadeur",
            "ambassadeur@jobsy.local",
            [
                new Step("/home", "01-home"),
                new Step("/ambassadeur", "02-dashboard"),
                new Step("/ambassadeur/toolkit", "03-toolkit"),
                new Step("/ambassadeur/finance", "04-financieel"),
                new Step("/ambassadeur/onboarding", "05-onboarding")
            ]),
        new(
            "admin",
            "Admin",
            "admin@jobsy.local",
            [
                new Step("/home", "01-home"),
                new Step("/admin/organisaties", "02-organisaties"),
                new Step("/admin/vacatures", "03-vacatures"),
                new Step("/admin/gebruikers", "04-gebruikers"),
                new Step("/admin/financien", "05-financieel"),
                new Step("/admin/instellingen", "06-instellingen"),
                new Step("/admin/beveiliging/systeemlogs", "07-logging")
            ]),
        new(
            "school-beheer",
            "Schoolbeheerder (SchoolAdmin)",
            "schoolbeheer@jobsy.local",
            [
                new Step("/school", "01-dashboard"),
                new Step("/school/klassen", "02-klassen"),
                new Step("/school/te-doen", "03-te-doen"),
                new Step("/school/resultaten", "04-resultaten"),
                new Step("/school/leraren", "05-leraren")
            ]),
        new(
            "leraar",
            "Leraar (Teacher)",
            "leraar@jobsy.local",
            [
                new Step("/leraar", "01-dashboard"),
                new Step(DemoClassPath, "02-klas-overzicht"),
                new Step($"{DemoClassPath}/codes", "03-codes"),
                new Step($"{DemoClassPath}/groep", "04-groep")
            ]),
        new(
            "werkcoach",
            "Werkcoach (Teacher)",
            "werkcoach@jobsy.local",
            [
                new Step("/leraar", "01-dashboard"),
                new Step(DemoClassPath, "02-klas-overzicht"),
                new Step($"{DemoClassPath}/droombanen", "03-droombanen"),
                new Step($"{DemoClassPath}/materiaal", "04-materiaal")
            ]),
        new(
            "gemeente",
            "Gemeente (publiek + partner)",
            "gemeente@jobsy.local",
            [
                new Step("/home", "02-home"),
                new Step("/employer/vacancies", "03-vacatures"),
                new Step("/regional/branches", "04-vestigingen")
            ],
            PreLoginSteps: [new Step("/westland", "01-westland-teaser")])
    ];

    [Fact]
    public async Task All_roles_desktop_and_mobile_with_screenshots()
    {
        if (!RoleJourneyE2e.ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable");
        }

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl), "Feature flags did not turn on in time");
        Assert.True(await CandidateJourneyE2e.WaitForAmbassadorsFlagAsync(baseUrl), "AmbassadorsEnabled did not turn on in time");
        Assert.True(await CandidateJourneyE2e.WaitForSchoolsFlagAsync(baseUrl), "SchoolsEnabled did not turn on in time");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in new[] { (1280, 900, "desktop"), (375, 812, "mobile") })
        {
            foreach (var plan in Plans)
            {
                await RunPlanAsync(browser, baseUrl, plan, width, height, tag);
            }
        }
    }

    private static async Task RunPlanAsync(
        IBrowser browser,
        string baseUrl,
        RolePlan plan,
        int width,
        int height,
        string viewportTag)
    {
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var capture = KbE2e.AttachCapture(page);

        if (plan.PreLoginSteps is { Length: > 0 })
        {
            foreach (var step in plan.PreLoginSteps)
            {
                await RoleJourneyE2e.VisitAsync(page, baseUrl, plan.Slug, viewportTag, step.Path, step.Slug);
            }
        }

        if (plan.LoginEmail is not null)
        {
            await RoleJourneyE2e.AssertLoginAsync(page, baseUrl, plan.LoginEmail);
        }

        foreach (var step in plan.Steps)
        {
            await RoleJourneyE2e.VisitAsync(page, baseUrl, plan.Slug, viewportTag, step.Path, step.Slug);
        }

        if (plan.TapHomeNav && viewportTag == "mobile")
        {
            await RoleJourneyE2e.TryTapBottomNavAsync(page, "Vacatures");
            await RoleJourneyE2e.WaitForRoleChromeAsync(page);
            await RoleJourneyE2e.ShotAsync(page, plan.Slug, viewportTag, "08-bottomnav-vacatures");
        }

        if (plan.LoginEmail is not null)
        {
            KbE2e.AssertHealthy(capture, requireNoIsochrone404: false);
        }
        else
        {
            await KbE2e.AssertNoCircuitErrorAsync(page);
        }
    }
}
