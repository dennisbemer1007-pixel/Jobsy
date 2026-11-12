using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using System.Security.Claims;
using System.Text.Json;

namespace Jobsy.Tests.Scholen;

/// <summary>Layout evidence: G78 and VO reis imagine panel at 390 / 768 / 1440.</summary>
[Collection("PlaywrightSmoke")]
public sealed class PupilReisImagineLayoutPlaywrightTests
{
    [Fact]
    public void Leerling_reis_ssr_stylesheet_bundle_includes_scholen_css_for_vo_and_g78()
    {
        var sheets = PageAssetCatalog.StylesheetsForPupilSsr("/leerling/reis");
        Assert.Contains(sheets, s => s.Contains("scholen.css", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(sheets, s => s.Contains("tokens.css", StringComparison.OrdinalIgnoreCase));

        var g78Extra = PageAssetCatalog.LeerlingGroep78ThemeStylesheets("/leerling/reis");
        Assert.Single(g78Extra);
        Assert.Contains("leerling-werelden-kies.css", g78Extra[0], StringComparison.OrdinalIgnoreCase);
    }

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

        var bodyClass = groep78 ? "ll-g78-shell t-zee" : "ll-reis-page";
        var html = "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
            + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + "<base href=\"http://127.0.0.1/\">"
            + "</head><body class=\"" + bodyClass + "\">" + body + "</body></html>";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await InstallWwwrootRoutingAsync(page, repoRoot);
        await page.SetContentAsync(html);
        await AddPupilReisStylesAsync(page, repoRoot, groep78);
        await page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Path = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js")
        });

        var stylesLoaded = await page.EvaluateAsync<bool>(
            """
            () => {
              const btn = document.querySelector('.ll-info-btn');
              const answers = document.querySelector('.answers .ans, .ll-chip');
              if (!btn) return false;
              const radius = parseFloat(getComputedStyle(btn).borderRadius);
              const w = btn.getBoundingClientRect().width;
              const round = radius >= w * 0.45;
              const answersOk = !answers || getComputedStyle(answers).borderRadius !== '0px'
                || getComputedStyle(answers).borderTopWidth !== '0px';
              return round && answersOk;
            }
            """);
        Assert.True(stylesLoaded, label + " pupil reis styles did not load");

        await page.Locator(".ll-question__head .ll-info-btn").ClickAsync();
        var panel = page.Locator("#ll-imagine-" + itemId);
        await panel.WaitForAsync();
        await page.WaitForTimeoutAsync(280);

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
              return br.top >= tr.top - 4 && br.top <= tr.top + 32;
            }
            """);
        Assert.True(headAlign, label + " info button should align with question heading");

        var panelMetrics = await page.EvaluateAsync<JsonElement>(
            """
            () => {
              const panel = document.querySelector('.ll-info:not([hidden])');
              const text = panel?.querySelector('.ll-info__text');
              const lead = panel?.querySelector('.ll-info__lead strong');
              const btn = document.querySelector('.ll-question__head .ll-info-btn');
              if (!panel || !text || !btn) {
                return { ok: false };
              }
              const rgb = (css) => {
                const m = css.match(/rgba?\(\s*(\d+),\s*(\d+),\s*(\d+)/);
                return m ? [+m[1], +m[2], +m[3]] : [0, 0, 0];
              };
              const lum = ([r, g, b]) => {
                const lin = [r, g, b].map((v) => {
                  const c = v / 255;
                  return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
                });
                return 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2];
              };
              const ratio = (fg, bg) => {
                const L1 = lum(rgb(fg)) + 0.05;
                const L2 = lum(bg) + 0.05;
                return L1 > L2 ? L1 / L2 : L2 / L1;
              };
              const panelBg = [244, 235, 221];
              const bodyColor = getComputedStyle(text).color;
              const leadColor = lead ? getComputedStyle(lead).color : bodyColor;
              const btnRadius = parseFloat(getComputedStyle(btn).borderRadius);
              const btnW = btn.getBoundingClientRect().width;
              return {
                ok: true,
                opacity: parseFloat(getComputedStyle(panel).opacity),
                bodyContrast: ratio(bodyColor, panelBg),
                leadContrast: ratio(leadColor, panelBg),
                round: btnRadius >= btnW * 0.45,
                bodyText: (text.textContent || '').trim()
              };
            }
            """);

        Assert.True(panelMetrics.GetProperty("ok").GetBoolean(), label + " panel metrics missing");
        Assert.True(panelMetrics.GetProperty("opacity").GetDouble() >= 0.99, label + " panel opacity");
        Assert.True(panelMetrics.GetProperty("bodyContrast").GetDouble() >= 4.5, label + " body contrast");
        Assert.True(panelMetrics.GetProperty("leadContrast").GetDouble() >= 4.5, label + " lead contrast");
        Assert.True(panelMetrics.GetProperty("round").GetBoolean(), label + " info button should be round");
        var bodyText = panelMetrics.GetProperty("bodyText").GetString() ?? "";
        Assert.DoesNotContain("Stel je voor:", bodyText, StringComparison.OrdinalIgnoreCase);
        Assert.True(bodyText.Length > 0 && char.IsUpper(bodyText[0]), label + " imagine body should start with a capital letter");

        if (groep78)
        {
            var lobsterOk = await page.EvaluateAsync<bool>(
                """
                () => {
                  const img = document.querySelector('.ll-g78-coach .lob');
                  return !!img && img.naturalWidth > 0;
                }
                """);
            Assert.True(lobsterOk, label + " coach lobster image should load");
        }
        else
        {
            var coachCueOk = await page.EvaluateAsync<bool>(
                """
                () => {
                  const cue = document.querySelector('[data-testid="ll-coach-cue"]');
                  const card = document.querySelector('.ll-card.ll-question');
                  if (!cue || !card || !card.contains(cue)) return false;
                  const display = getComputedStyle(cue).display;
                  if (window.innerWidth >= 900) return display === 'none';
                  return display !== 'none';
                }
                """);
            Assert.True(coachCueOk, label + " coach cue should live in the card and hide on desktop");

            if (width >= 900)
            {
                var boxesAligned = await page.EvaluateAsync<bool>(
                    """
                    () => {
                      const boxes = [...document.querySelectorAll('.ll-answer--vo .ll-answer__box')];
                      if (boxes.length < 5) return false;
                      const tops = boxes.map((b) => b.getBoundingClientRect().top);
                      return Math.max(...tops) - Math.min(...tops) <= 2;
                    }
                    """);
                Assert.True(boxesAligned, label + " VO answer checkboxes should share one row");
            }
        }

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

    private static async Task InstallWwwrootRoutingAsync(IPage page, string repoRoot)
    {
        var wwwroot = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot");
        await page.RouteAsync("**/*", async route =>
        {
            var uri = new Uri(route.Request.Url);
            if (!string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal))
            {
                await route.FallbackAsync();
                return;
            }

            var relative = uri.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(relative))
            {
                await route.FallbackAsync();
                return;
            }

            var file = Path.Combine(wwwroot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(file))
            {
                await route.FulfillAsync(new RouteFulfillOptions { Path = file });
                return;
            }

            await route.FallbackAsync();
        });
    }

    private static async Task AddPupilReisStylesAsync(IPage page, string repoRoot, bool groep78)
    {
        var wwwroot = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot");
        foreach (var url in PageAssetCatalog.StylesheetsForPupilSsr("/leerling/reis"))
        {
            var relative = url.Split('?')[0].TrimStart('/');
            var path = Path.Combine(wwwroot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = path });
            }
        }

        if (!groep78)
        {
            return;
        }

        foreach (var url in PageAssetCatalog.LeerlingGroep78ThemeStylesheets("/leerling/reis"))
        {
            var relative = url.Split('?')[0].TrimStart('/');
            var path = Path.Combine(wwwroot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = path });
            }
        }
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
