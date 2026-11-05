using Bunit;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests.Scholen;

public class PupilDroombaanGroep78SsrTests : JobsyBunitContext

{
    public PupilDroombaanGroep78SsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }
    private static readonly string[] DemoClassJobKeys =
    [
        "personal-trainer",
        "profsporter",
        "fysiotherapeut",
        "bioloog",
        "dierenverzorger",
    ];

    [Fact]
    public void Demo_class_five_jobs_use_sport_zorg_and_onderzoek_paintings_not_brandweer()
    {
        Assert.All(DemoClassJobKeys, k =>
            Assert.DoesNotContain("helpen", PupilGroep78JobPaint.PaintImagePath(k), StringComparison.Ordinal));
        Assert.Equal("/leerling/werelden/img/jobs/sport.jpg", PupilGroep78JobPaint.PaintImagePath("personal-trainer"));
        Assert.Equal("/leerling/werelden/img/jobs/sport-hero.jpg", PupilGroep78JobPaint.PaintImagePath("profsporter"));
        Assert.EndsWith("zorg-dieren-hero.jpg", PupilGroep78JobPaint.PaintImagePath("fysiotherapeut"), StringComparison.Ordinal);
        Assert.EndsWith("zorg-dieren.jpg", PupilGroep78JobPaint.PaintImagePath("dierenverzorger"), StringComparison.Ordinal);
        Assert.EndsWith("onderzoek.jpg", PupilGroep78JobPaint.PaintImagePath("bioloog"), StringComparison.Ordinal);
        Assert.NotEqual(
            PupilGroep78JobPaint.PaintImagePath("personal-trainer"),
            PupilGroep78JobPaint.PaintImagePath("profsporter"));
    }

    [Fact]
    public void PickCardCopy_fills_domain_and_blurb_for_all_demo_jobs()
    {
        foreach (var key in DemoClassJobKeys)
        {
            var (domain, blurb) = PupilGroep78JobPaint.PickCardCopy(key, "nl");
            Assert.False(string.IsNullOrWhiteSpace(domain), key);
            Assert.False(string.IsNullOrWhiteSpace(blurb), key);
            Assert.DoesNotContain("beroep om over na te denken", blurb, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Groep78_job_pick_html_renders_five_cards_with_domain_blurb_and_speak()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var story = new PupilStoryViewDto(
            "Titel",
            "Body",
            [],
            DemoClassJobKeys.Select(k => DreamJobCatalog.All.First(j => j.Key == k).TitleNl).ToList(),
            []);
        var model = new PupilDroombaanPageModel(
            new PupilResultPageDto(
                "8B",
                "ABC-DEF",
                "Demo College Lobsy",
                story,
                [],
                [],
                null,
                null,
                QuestionSet: PupilQuestionSet.Groep78,
                JobSuggestions: null),
            null,
            "",
            null);

        var html = PupilDroombaanSsrHtml.BuildBody(
            model,
            culture,
            """<input type="hidden" name="__RequestVerificationToken" value="t" />""",
            useGroep78Shell: true,
            journeyThemeKey: "zee",
            registry: new PupilQuestionSetRegistry(),
            forceJobPick: true);

        Assert.Contains("data-testid=\"ll-g78-job-picks\"", html, StringComparison.Ordinal);
        Assert.Equal(5, CountOccurrences(html, "<figure class=\"jpick-paint"));
        foreach (var key in DemoClassJobKeys)
        {
            var copy = PupilGroep78JobPaint.PickCardCopy(key, "nl");
            Assert.Contains(PupilGroep78JobPaint.PaintImagePath(key), html, StringComparison.Ordinal);
            Assert.Contains(copy.Blurb, html, StringComparison.Ordinal);
            Assert.Contains($">{copy.Domain}</p>", html, StringComparison.Ordinal);
            Assert.Contains($"ll-job-{key}", html, StringComparison.Ordinal);
        }

        Assert.Equal(6, CountOccurrences(html, "class=\"jspeak\""));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }

        return count;
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
}
