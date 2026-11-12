using System.Text;
using Jobsy.Core.Careers;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Passport;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

internal static class PassportDayLinkScreenshotHarness
{
    public const string CookId = "90f75f67-495d-49fa-ab57-2f320e251d7e";

    public static CultureState CreateCulture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthenticationStateProvider>(new EmptyAuth());
        services.AddSingleton<IJSRuntime, NoJsRuntime>();
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var culture = services.BuildServiceProvider().GetRequiredService<CultureState>();
        culture.SetLanguageAsync("nl").GetAwaiter().GetResult();
        return culture;
    }

    public static IReadOnlyList<string> StylesheetPaths(string repoRoot, IReadOnlyList<string> catalogUrls, params string[] extraRelative)
    {
        var wwwroot = Path.Combine(repoRoot, "Jobsy.Web/wwwroot");
        var paths = new List<string>();
        foreach (var url in catalogUrls)
        {
            AddStylesheetPath(paths, wwwroot, url);
        }

        foreach (var rel in extraRelative)
        {
            AddStylesheetPath(paths, wwwroot, rel);
        }

        return paths;
    }

    private static void AddStylesheetPath(List<string> paths, string wwwroot, string urlOrRel)
    {
        var rel = urlOrRel.TrimStart('/').Split('?')[0];
        var path = Path.Combine(wwwroot, rel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path) && !paths.Contains(path, StringComparer.Ordinal))
        {
            paths.Add(path);
        }
    }

    public static string PupilShellDocument(string bodyInner, string themeClass = "t-zee")
        => $"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <base href="http://127.0.0.1/" />
            <link href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,700&family=Lexend:wght@400;600&display=swap" rel="stylesheet" />
            </head><body class="ll-g78-shell {themeClass}">{bodyInner}</body></html>
            """;

    public static string CandidatePassportDocument(string bodyInner)
        => $"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <base href="http://127.0.0.1/" />
            <link href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,700&family=Lexend:wght@400;600&display=swap" rel="stylesheet" />
            </head><body class="app-shell has-bottom-nav candidate-app">{bodyInner}</body></html>
            """;

    public static string DitBenJijResultBody(CultureState culture)
    {
        var stems = PupilJourneyTheme.ResultTileStems(PupilJourneyTheme.ZeeExpeditie);
        var tiles = stems.Take(3)
            .Select((stem, i) => new PupilStoryTileDto(stem, $"Label {i + 1}", "Korte uitleg."))
            .ToList();
        var result = new PupilResultPageDto(
            "Groep 8",
            "ABC-12",
            "Demoschool",
            new PupilStoryViewDto(
                "Dit ben jij",
                "Je bent nieuwsgierig en houdt van samenwerken. Onderweg ontdekte je wat bij je past.",
                tiles,
                ["Kok"],
                ["sport", "buiten"]),
            ["sport", "buiten"],
            [],
            null,
            null,
            JobExploreChips:
            [
                new PupilJobExploreChipDto("Kok", CookId),
                new PupilJobExploreChipDto("Hovenier", null),
            ]);
        var registry = new PupilQuestionSetRegistry();
        return PupilDitBenJijSsrHtml.BuildBody(
            result,
            culture,
            groep78: true,
            PupilJourneyTheme.ZeeExpeditie,
            registry,
            passportV2: true,
            pdfHref: "/leerling/pdf");
    }

    public static string CandidatePassportFitTabBody(CultureState culture)
    {
        var profile = new MeProfile { FullName = "Alex Demo", FirstName = "Alex", Preferences = new CandidatePreferences() };
        var flags = FeatureFlagSnapshot.Defaults with { EmployersEnabled = true, FutureJobsForYouEnabled = false };
        var card = PassportSsrDisplay.BuildCard(culture, profile, null, null, employersEnabled: true);
        var overview = PassportSsrDisplay.BuildOverview(culture, null, null);
        var dnaPanel = PassportSsrDisplay.BuildDnaPanel(culture, profile, null, null, 0, true);
        var fitState = new RoleFitCheckState
        {
            IsUnlocked = true,
            LastResult = new RoleFitCheckResult
            {
                JobTitle = "kok",
                MatchPercent = 72,
                Strengths = ["Samenwerken"],
                Gaps = ["Ervaring in de keuken"],
                ActionSteps = ["Zoek een proefstage"],
            },
        };
        var dayIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { CookId };
        var fitPanel = new PassportSsrFitHtml.Panel(fitState, [], "", 0, null, "", "", dayIds);
        var careerView = CareerPlanViewBuilder.BuildPassport(null);
        var careerPanel = new PassportSsrCareerHtml.Panel(careerView, [], "");
        var proofStrength = ProofStrengthRules.Build(new ProofStrengthRules.ProofSnapshot(0, 0, 0, 0, false));
        var proofPanel = new PassportSsrProofHtml.Panel(
            proofStrength,
            null,
            [],
            [],
            [],
            RefVerificationEnabled: false,
            RefVerificationEligible: false,
            HasConfirmedReference: false,
            FirstEmployerEntryId: null,
            UserIdForTip: "screenshot",
            DnaPassportPdf: false);
        var data = new PassportSsrData.PageData(
            PassportTabs.Fit,
            null,
            false,
            flags,
            profile,
            null,
            null,
            card,
            overview,
            dnaPanel,
            "Alex",
            4,
            null,
            FromDiscovery: false,
            fitPanel,
            ProactiveMatchingEnabled: false,
            careerPanel,
            proofPanel);
        return PassportSsrShells.BuildBody(culture, data, antiforgeryToken: "", openDataSection: null);
    }

    public static string G78PaspoortPageBody(CultureState culture, bool withDay)
    {
        var model = new PupilG78PassportWebDto(
            Enabled: true,
            OccupationCards:
            [
                new PupilG78PassportCardDto(
                    "Natuur en buiten",
                    "Kok",
                    "Je werkt met eten en mensen.",
                    ["Samen"],
                    82,
                    WhyFit: null,
                    DayEscoId: withDay ? CookId : null),
            ]);
        var topbar = """
            <header class="top ll-topbar"><a class="brand" href="/"><span class="brand__text">Lobsy</span></a></header>
            """;
        return topbar + PupilG78PassportSsrHtml.Render(model, culture);
    }

    public static string PupilDayPage(CultureState culture)
    {
        var day = new OccupationDayResponse
        {
            Enabled = true,
            Found = true,
            EscoId = CookId,
            TitleNl = "kok",
            Morning = "Je start je dag met de mise en place.",
            Midday = "Rond het middaguur is het druk in de keuken.",
            Afternoon = "In de middag serveer je de gerechten.",
            Closing = "Je ruimt op en sluit af.",
            Highlights = ["Samen koken"],
            VariesNote = "Elke keuken werkt anders.",
            ThinSource = false,
            Language = "nl",
            Blocks =
            [
                new OccupationDayBlock("07:00", "07:00 Start", "Je kijkt wat er op het menu staat."),
                new OccupationDayBlock("12:00", "12:00 Lunch", "Het is druk. Je helpt mee met serveren."),
            ],
            Tasks = ["Porties klaarmaken"],
            Skills = ["Nauwkeurig werken"],
        };
        return PupilOccupationDaySsrHtml.Build(culture, "kok", day, unknown: false, error: false);
    }

    private sealed class EmptyAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class NoJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => default;
    }
}
