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

[Collection("PlaywrightSmoke")]
public sealed class G78PuzzleIntroScreenshotTests
{
    [Theory]
    [InlineData("schelpenrij", 1440, 900, "desktop")]
    [InlineData("schelpenrij", 390, 844, "mobile")]
    [InlineData("schelpenrij", 360, 740, "mobile360")]
    [InlineData("schatkaart", 1440, 900, "desktop")]
    [InlineData("schatkaart", 390, 844, "mobile")]
    [InlineData("schatkaart", 360, 740, "mobile360")]
    [InlineData("vuurtorenlampen", 1440, 900, "desktop")]
    [InlineData("vuurtorenlampen", 390, 844, "mobile")]
    [InlineData("vuurtorenlampen", 360, 740, "mobile360")]
    public async Task Intro_and_lamp_glow_render_without_clip(string puzzleKey, int width, int height, string label)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var repoRoot = RepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-puzzle-intro-fix", puzzleKey, label);
        Directory.CreateDirectory(artifactDir);

        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var body = PupilPuzzelSsrHtml.BuildBody(
            puzzleKey,
            culture,
            antiforgery: "",
            savePath: $"/leerling/puzzel/{puzzleKey}/save",
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie,
            registry,
            error: null);

        var cssScholen = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        var cssKies = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-kies.css");
        var jsPath = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js");
        var html = $"""
            <!DOCTYPE html>
            <html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><base href="http://127.0.0.1/"></head>
            <body class="ll-g78-shell t-zee">{body}</body></html>
            """;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
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
        await page.SetContentAsync(html);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssScholen });
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssKies });
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = jsPath });

        await AssertPuzzleIntroAndPlayWithinContainersAsync(page);
        await AssertCoachLobsterLoadsAsync(page, width);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, "intro.png"),
            FullPage = true
        });

        if (string.Equals(puzzleKey, "schelpenrij", StringComparison.Ordinal))
        {
            await AssertSchelpenSlotsInsidePreviewBoxAsync(page);
            await page.GetByRole(AriaRole.Button, new() { Name = "Start de puzzel" }).ClickAsync();
            await page.Locator(".ll-puzzle-schelp-choices").WaitForAsync();
            await AssertPuzzleIntroAndPlayWithinContainersAsync(page);
        }
        else if (puzzleKey == "schatkaart")
        {
            await Assertions.Expect(page.Locator(".prev-row--cards img[src*='match-1']").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".prev-row--cards img[src*='match-2']").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".prev-row--cards img[src*='match-3']").First).ToBeVisibleAsync();
            await AssertSchatkaartTilesInsideIntroBoxAsync(page);
        }
        else
        {
            await Assertions.Expect(page.Locator(".slot--beacon")).ToHaveCountAsync(3);
            await page.GetByRole(AriaRole.Button, new() { Name = "Start de puzzel" }).ClickAsync();
            await page.WaitForSelectorAsync(".ll-puzzle__beacon.is-lit", new PageWaitForSelectorOptions { Timeout = 20_000 });
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(artifactDir, "lamp-lit.png"),
                FullPage = true
            });
            var overlap = await page.EvaluateAsync<int>(
                """
                () => {
                  const lamps = [...document.querySelectorAll('.ll-puzzle__beacon')];
                  let overlaps = 0;
                  for (let i = 0; i < lamps.length; i++) {
                    const a = lamps[i].getBoundingClientRect();
                    for (let j = i + 1; j < lamps.length; j++) {
                      const b = lamps[j].getBoundingClientRect();
                      if (!(a.right <= b.left || a.left >= b.right || a.bottom <= b.top || a.top >= b.bottom)) overlaps++;
                    }
                  }
                  return overlaps;
                }
                """);
            Assert.Equal(0, overlap);
            var clipped = await page.EvaluateAsync<bool>(
                """
                () => {
                  const vw = document.documentElement.clientWidth;
                  return [...document.querySelectorAll('.ll-puzzle__beacon')].some(el => {
                    const r = el.getBoundingClientRect();
                    return r.left < -1 || r.right > vw + 1;
                  });
                }
                """);
            Assert.False(clipped);
        }
    }

    internal static Task AssertIntroNoHorizontalOverflowAsync(IPage page)
        => AssertPuzzleIntroAndPlayWithinContainersAsync(page);

    internal static async Task AssertPuzzleIntroAndPlayWithinContainersAsync(IPage page)
    {
        var cardOverflow = await page.EvaluateAsync<double>(
            """
            () => {
              const card = document.querySelector('[data-testid=ll-puzzle-root], .ll-puzzle-g78');
              if (!card) return 999;
              const cr = card.getBoundingClientRect();
              let max = 0;
              const checkInCard = (el) => {
                const r = el.getBoundingClientRect();
                if (r.width <= 0 && r.height <= 0) return;
                if (r.left < cr.left - 0.5) max = Math.max(max, cr.left - r.left);
                if (r.right > cr.right + 0.5) max = Math.max(max, r.right - cr.right);
              };
              card.querySelectorAll(
                '.ll-puzzle-intro, .ll-puzzle-play:not([hidden]), .prev-row-box, .prev-row, .slot, .ll-puzzle__cards, .ll-puzzle__card, .ll-puzzle-schelp-choices, .ll-puzzle-schelp-choices .ans, .ll-puzzle__lamps, .ll-puzzle__beacon'
              ).forEach(checkInCard);
              document.querySelectorAll('.prev-row-box').forEach(box => {
                const br = box.getBoundingClientRect();
                box.querySelectorAll('.slot').forEach(slot => {
                  const sr = slot.getBoundingClientRect();
                  if (sr.width <= 0 && sr.height <= 0) return;
                  if (sr.left < br.left - 0.5) max = Math.max(max, br.left - sr.left);
                  if (sr.right > br.right + 0.5) max = Math.max(max, sr.right - br.right);
                });
              });
              return max;
            }
            """);
        Assert.True(cardOverflow <= 1, $"Puzzle content overflow vs card {cardOverflow}px.");

        var viewportOverflow = await page.EvaluateAsync<double>(
            """
            () => {
              const root = document.querySelector('[data-testid=ll-puzzle-root], .ll-puzzle-g78');
              if (!root) return 999;
              const vw = document.documentElement.clientWidth;
              let max = 0;
              root.querySelectorAll(
                '.ll-puzzle-intro, .ll-puzzle-play:not([hidden]), .prev-row-box, .slot, .ll-puzzle__card, .ll-puzzle-schelp-choices .ans'
              ).forEach(el => {
                const r = el.getBoundingClientRect();
                if (r.width <= 0 && r.height <= 0) return;
                max = Math.max(max, r.right - vw, -r.left);
              });
              return Math.max(0, max);
            }
            """);
        Assert.True(viewportOverflow <= 1, $"Puzzle viewport overflow {viewportOverflow}px.");
    }

    internal static async Task AssertSchelpenSlotsInsidePreviewBoxAsync(IPage page)
    {
        var ok = await page.EvaluateAsync<bool>(
            """
            () => {
              const box = document.querySelector('.prev-row-box:not(.prev-row-box--cards):not(.prev-row-box--lamps)');
              if (!box) return false;
              const br = box.getBoundingClientRect();
              return [...box.querySelectorAll('.slot')].every(slot => {
                const r = slot.getBoundingClientRect();
                return r.left >= br.left - 0.5 && r.right <= br.right + 0.5;
              });
            }
            """);
        Assert.True(ok, "Schelpenrij preview shells must stay inside the intro preview box.");
    }

    internal static async Task AssertSchatkaartTilesInsideIntroBoxAsync(IPage page)
    {
        var allInside = await page.EvaluateAsync<bool>(
            """
            () => {
              const box = document.querySelector('.prev-row-box--cards');
              if (!box) return false;
              const br = box.getBoundingClientRect();
              return [...box.querySelectorAll('.slot--card')].every(slot => {
                const r = slot.getBoundingClientRect();
                return r.left >= br.left - 0.5 && r.right <= br.right + 0.5
                  && r.top >= br.top - 0.5 && r.bottom <= br.bottom + 0.5;
              });
            }
            """);
        Assert.True(allInside, "Schatkaart symbol tiles must stay inside the intro preview box.");
    }

    internal static async Task AssertCoachLobsterLoadsAsync(IPage page, int viewportWidth)
    {
        if (viewportWidth <= 600)
        {
            await Assertions.Expect(page.Locator(".ll-g78-coach-mobile .ring")).ToBeVisibleAsync();
            var portraitOk = await page.EvaluateAsync<bool>(
                "async () => (await fetch('/leerling/werelden/img/kreeft-portret.png')).ok");
            Assert.True(portraitOk, "Mobile coach uses kreeft-portret.png (served on acceptatie under /leerling/werelden/img/).");
            var ringUsesPortrait = await page.EvaluateAsync<bool>(
                """
                () => {
                  const ring = document.querySelector('.ll-g78-coach-mobile .ring');
                  if (!ring) return false;
                  return getComputedStyle(ring).backgroundImage.includes('kreeft-portret.png');
                }
                """);
            Assert.True(ringUsesPortrait, "Mobile coach ring CSS must reference kreeft-portret.png.");
            return;
        }

        await Assertions.Expect(page.Locator(".ll-g78-coach.d-only img.lob")).ToBeVisibleAsync();
        var desktopOk = await page.EvaluateAsync<bool>(
            """
            async () => {
              const img = document.querySelector('.ll-g78-coach.d-only img.lob');
              if (!img) return false;
              if (img.complete && img.naturalWidth > 0) return true;
              await new Promise((resolve, reject) => {
                img.addEventListener('load', resolve, { once: true });
                img.addEventListener('error', reject, { once: true });
              });
              return img.naturalWidth > 0;
            }
            """);
        Assert.True(desktopOk, "Desktop coach must load kreeft-kaart.png (same path as acceptatie SSR).");
    }

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

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
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
