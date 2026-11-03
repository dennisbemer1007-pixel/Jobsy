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
    [InlineData("schatkaart", 1440, 900, "desktop")]
    [InlineData("schatkaart", 390, 844, "mobile")]
    [InlineData("vuurtorenlampen", 1440, 900, "desktop")]
    [InlineData("vuurtorenlampen", 390, 844, "mobile")]
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
        await page.RouteAsync("**/images/**", async route =>
        {
            var uri = new Uri(route.Request.Url);
            var relative = uri.AbsolutePath.TrimStart('/');
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

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, "intro.png"),
            FullPage = true
        });

        if (puzzleKey == "schatkaart")
        {
            await Assertions.Expect(page.Locator(".prev-row--cards img[src*='match-1']").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".prev-row--cards img[src*='match-2']").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".prev-row--cards img[src*='match-3']").First).ToBeVisibleAsync();
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
