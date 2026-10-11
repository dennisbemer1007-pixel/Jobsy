using Bunit;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// Walkthrough PNGs for deep onboarding → paspoort (before: Draft+25 answers; after: Completed).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class PassportDeepOnboardingWalkthroughTests : BunitContext
{
    private static string ArtifactsDir =>
        Environment.GetEnvironmentVariable("CURSOR_ARTIFACTS_DIR")
        ?? "/opt/cursor/artifacts/screenshots";

    public PassportDeepOnboardingWalkthroughTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public async Task Save_paspoort_before_after_pngs_for_deep_onboarding()
    {
        Directory.CreateDirectory(ArtifactsDir);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();

        var before = RenderOverview(draftDeepOnboarding: true);
        var after = RenderOverview(draftDeepOnboarding: false);

        await SaveViewportAsync(browser, before, "paspoort-deep-onboarding-before-desktop", 1280, 800);
        await SaveViewportAsync(browser, before, "paspoort-deep-onboarding-before-mobile", 390, 844);
        await SaveViewportAsync(browser, after, "paspoort-deep-onboarding-after-desktop", 1280, 800);
        await SaveViewportAsync(browser, after, "paspoort-deep-onboarding-after-mobile", 390, 844);

        Assert.Contains("Eerste indruk", before, StringComparison.Ordinal);
        Assert.Contains("Volledig", after, StringComparison.Ordinal);
    }

    private string RenderOverview(bool draftDeepOnboarding)
    {
        var kompas = BuildKompas(draftDeepOnboarding);
        CandidateDnaViewBuilder.TextLookup t = k => UiStrings.Get(k, "nl");
        CandidateDnaViewBuilder.TextFormat format = (k, args) =>
            string.Format(UiStrings.Get(k, "nl"), args);
        var view = CandidateDnaViewBuilder.FromKompas(kompas, t, format, "nl");

        var cut = Render<PassportOverview>(p => p
            .Add(x => x.CompetenceProgress, Progress(view, AssessmentKind.Competence))
            .Add(x => x.CareerProgress, Progress(view, AssessmentKind.Career))
            .Add(x => x.CultureProgress, Progress(view, AssessmentKind.Culture))
            .Add(x => x.ValuesProgress, Progress(view, AssessmentKind.Values))
            .Add(x => x.CompletedCount, view.CompletedCount)
            .Add(x => x.Strongest, view.Cards[0].TopLabel)
            .Add(x => x.StrongestStatus, Status(view.Cards[0], t))
            .Add(x => x.WorkFit, view.Cards[1].TopLabel)
            .Add(x => x.WorkFitStatus, (string?)null)
            .Add(x => x.CultureHome, view.Cards[2].TopLabel)
            .Add(x => x.CultureHomeStatus, Status(view.Cards[2], t))
            .Add(x => x.Important, view.Cards[3].TopLabel)
            .Add(x => x.ImportantStatus, Status(view.Cards[3], t)));

        return WrapHtml(cut.Markup);
    }

    private static double Progress(CandidateDnaViewBuilder.DnaViewModel view, AssessmentKind kind)
    {
        var card = view.Cards.First(c => c.Kind == kind);
        return card.Completed ? 1.0 : card.IsProvisional ? 0.65 : 0.0;
    }

    private static string Status(CandidateDnaViewBuilder.DnaCard card, CandidateDnaViewBuilder.TextLookup t)
    {
        if (card.Completed)
        {
            return t("Dna.StatusComplete");
        }

        if (card.IsProvisional)
        {
            return t("Onboarding.ProvisionalBadge");
        }

        return t("Test.Status.NotDone");
    }

    private static CandidateKompasState BuildKompas(bool draftOnly)
    {
        var status = draftOnly ? CandidateCompetencyStatuses.Draft : CandidateCompetencyStatuses.Completed;
        var scores = new CompetencyScoreSet
        {
            Samenwerken = 78,
            Resultaatgerichtheid = 82,
            Stressbestendigheid = 71,
            Innovatie = 69,
            Extraversie = 74
        };
        var riasec = new RiasecScoreSet
        {
            Realistic = 55,
            Investigative = 62,
            Artistic = 48,
            Social = 80,
            Enterprising = 58,
            Conventional = 52
        };

        return new CandidateKompasState
        {
            Competencies = State(25, 25, status, scores),
            CareerInterests = State(25, 25, status, riasec),
            Culture = CultureState(18, status),
            Values = ValuesState(25, status),
            ProfileCompletenessPercent = draftOnly ? 55 : 92
        };
    }

    private static CandidateCompetencyState State(int answered, int total, string status, CompetencyScoreSet scores)
        => new()
        {
            Status = status,
            Answers = Enumerable.Range(1, answered).ToDictionary(i => i.ToString(), _ => 4),
            AnsweredCount = answered,
            QuestionCount = total,
            Scores = status == CandidateCompetencyStatuses.Completed ? scores : null,
            PreviewScores = status == CandidateCompetencyStatuses.Draft ? scores : null,
            CompletedAtUtc = status == CandidateCompetencyStatuses.Completed ? DateTime.UtcNow : null
        };

    private static CandidateCareerInterestState State(int answered, int total, string status, RiasecScoreSet scores)
        => new()
        {
            Status = status,
            Answers = Enumerable.Range(1, answered).ToDictionary(i => i.ToString(), _ => 4),
            AnsweredCount = answered,
            QuestionCount = total,
            Scores = status == CandidateCompetencyStatuses.Completed ? scores : null,
            PreviewScores = status == CandidateCompetencyStatuses.Draft ? scores : null,
            CompletedAtUtc = status == CandidateCompetencyStatuses.Completed ? DateTime.UtcNow : null
        };

    private static CandidateCultureState CultureState(int answered, string status)
    {
        var answers = Enumerable.Range(1, answered).ToDictionary(i => i.ToString(), _ => 4);
        return new CandidateCultureState
        {
            Status = status,
            Answers = answers,
            Scores = new CulturePersonalityScoreSet
            {
                Autonomy = 70,
                Informal = 65,
                Collaboration = 72,
                Flexibility = 68,
                Innovation = 60,
                PeopleFirst = 75
            },
            PreviewScores = status == CandidateCompetencyStatuses.Draft
                ? new CulturePersonalityScoreSet
                {
                    Autonomy = 70,
                    Informal = 65,
                    Collaboration = 72,
                    Flexibility = 68,
                    Innovation = 60,
                    PeopleFirst = 75
                }
                : null,
            CompletedAtUtc = status == CandidateCompetencyStatuses.Completed ? DateTime.UtcNow : null
        };
    }

    private static CandidateValuesState ValuesState(int answered, string status)
    {
        var answers = Enumerable.Range(1, answered).ToDictionary(i => i.ToString(), _ => 4);
        return new CandidateValuesState
        {
            Status = status,
            Answers = answers,
            Scores = new SchwartzValuesScoreSet
            {
                Autonomy = 70,
                Connection = 82,
                Achievement = 65,
                Stability = 58,
                Impact = 72
            },
            PreviewScores = status == CandidateCompetencyStatuses.Draft
                ? new SchwartzValuesScoreSet
                {
                    Autonomy = 70,
                    Connection = 82,
                    Achievement = 65,
                    Stability = 58,
                    Impact = 72
                }
                : null,
            CompletedAtUtc = status == CandidateCompetencyStatuses.Completed ? DateTime.UtcNow : null
        };
    }

    private static string WrapHtml(string body)
        => $"""
            <!DOCTYPE html>
            <html lang="nl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <link rel="stylesheet" href="file://{FindRepoRoot()}/Jobsy.Web/wwwroot/css/app.css" />
              <link rel="stylesheet" href="file://{FindRepoRoot()}/Jobsy.Web/wwwroot/css/features/paspoort.css" />
              <title>Paspoort walkthrough</title>
            </head>
            <body class="panel-page passport-page">
              {body}
            </body>
            </html>
            """;

    private static string FindRepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static async Task SaveViewportAsync(
        IBrowser browser,
        string html,
        string name,
        int width,
        int height)
    {
        var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = width, Height = height } });
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ArtifactsDir, $"{name}.png"),
            FullPage = true
        });
        await page.CloseAsync();
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }
}
