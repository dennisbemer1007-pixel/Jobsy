using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

/// <summary>Layout evidence: G78 and VO reis imagine panel at 390 / 768 / 1440.</summary>
[Collection("PlaywrightSmoke")]
public sealed class PupilReisImagineLayoutPlaywrightTests
{
    [Theory]
    [InlineData(true, 390, 844, "g78-390")]
    [InlineData(true, 768, 1024, "g78-768")]
    [InlineData(true, 1440, 900, "g78-1440")]
    [InlineData(false, 390, 844, "vo-390")]
    [InlineData(false, 768, 1024, "vo-768")]
    [InlineData(false, 1440, 900, "vo-1440")]
    public async Task Imagine_panel_layout_no_overlap_with_speak_or_answers(bool groep78, int width, int height, string label)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var repoRoot = RepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-vo-imagine-polish", label);
        Directory.CreateDirectory(artifactDir);

        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(groep78 ? PupilQuestionSet.Groep78 : PupilQuestionSet.Vo);
        var itemId = groep78 ? "9001" : "9101";
        var state = groep78
            ? new PupilProgressStateDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "7A",
                "TEST-1",
                PupilCodeStatus.InProgress,
                0,
                60,
                0,
                0,
                false,
                itemId,
                "koraalrif",
                new Dictionary<string, int>(),
                true,
                false,
                false,
                false,
                false,
                [],
                [],
                null,
                null,
                PupilQuestionSet.Groep78,
                JourneyThemeKey: PupilJourneyTheme.ZeeExpeditie)
            : new PupilProgressStateDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "4HAVO",
                "TEST-2",
                PupilCodeStatus.InProgress,
                0,
                100,
                0,
                0,
                false,
                itemId,
                "koraalrif",
                new Dictionary<string, int>(),
                true,
                false,
                false,
                false,
                false,
                [],
                [],
                null,
                null,
                PupilQuestionSet.Vo);

        var body = PupilReisSsrHtml.BuildBody(
            state,
            def,
            culture,
            registry,
            "",
            "/leerling/reis/answer",
            null,
            groep78,
            groep78 ? PupilJourneyTheme.ZeeExpeditie : null);

        var cssScholen = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        var cssKies = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-kies.css");
        var jsPath = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js");
        var bodyClass = groep78 ? "ll-g78-shell t-zee" : "ll-reis-page";
        var html = "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
            + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + "<style>body.ll-reis-page{margin:0;padding:16px;background:#f6efe4;font-family:system-ui,sans-serif;}</style>"
            + "</head><body class=\"" + bodyClass + "\">" + body + "</body></html>";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await page.SetContentAsync(html);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssScholen });
        if (groep78)
        {
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssKies });
        }

        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = jsPath });

        await page.Locator(".ll-question__head .ll-info-btn").ClickAsync();
        await page.Locator("#ll-imagine-" + itemId + ".is-open, #ll-imagine-" + itemId + ":not([hidden])").WaitForAsync();

        var overlap = await page.EvaluateAsync<bool>(
            """
            () => {
              const panel = document.querySelector('.ll-info.is-open, .ll-info:not([hidden])');
              const speak = document.querySelector('[data-testid="ll-g78-speak-row"]')
                || document.querySelector('[data-speech-key^="ll-q-"]')?.closest('.qrow');
              const answers = document.querySelector('.answers, .ll-chips');
              if (!panel) return false;
              const pr = panel.getBoundingClientRect();
              const gap = 4;
              const speakOk = !speak || speak.getBoundingClientRect().bottom <= pr.top + gap;
              const answersOk = !answers || answers.getBoundingClientRect().top >= pr.bottom - gap;
              return !(speakOk && answersOk);
            }
            """);

        Assert.False(overlap, label + " imagine panel overlaps speak row or answers");

        var headAlign = await page.EvaluateAsync<bool>(
            """
            () => {
              const head = document.querySelector('.ll-question__head');
              const btn = head?.querySelector('.ll-info-btn');
              const title = head?.querySelector('h1');
              if (!head || !btn || !title) return false;
              const br = btn.getBoundingClientRect();
              const tr = title.getBoundingClientRect();
              return br.top >= tr.top - 4 && br.top <= tr.top + 18;
            }
            """);
        Assert.True(headAlign, label + " info button should align with question heading");

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, "imagine-open.png"),
            FullPage = false
        });

        await page.Locator(".ll-info__close").ClickAsync();
        await page.WaitForTimeoutAsync(280);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, "imagine-closed.png"),
            FullPage = false
        });
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static CultureState NlCulture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime, NoJs>();
        services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var culture = services.BuildServiceProvider().GetRequiredService<CultureState>();
        culture.SetLanguageAsync("nl").GetAwaiter().GetResult();
        return culture;
    }

    private sealed class NoJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => default;
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
}
