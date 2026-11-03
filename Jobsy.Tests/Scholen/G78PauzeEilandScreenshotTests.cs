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

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78PauzeEilandScreenshotTests
{
    [Theory]
    [InlineData(1, 1440, 900, "desktop")]
    [InlineData(1, 390, 844, "mobile")]
    [InlineData(2, 1440, 900, "desktop")]
    [InlineData(2, 390, 844, "mobile")]
    public async Task Island_step_shows_matching_copy_without_viewport_clip(int step, int width, int height, string label)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var repoRoot = RepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-pauze-eiland", $"step-{step}", label);
        Directory.CreateDirectory(artifactDir);

        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(PupilQuestionSet.Groep78);
        var progress = new PupilProgressStateDto(
            Guid.NewGuid(),
            "8B",
            "ABC-DEF",
            PupilCodeStatus.InProgress,
            30,
            def.Bank.AllItems.Count,
            30,
            0,
            false,
            def.Bank.AllItems[29].Id,
            "pauze-eiland",
            new Dictionary<string, int>(),
            true,
            false,
            true,
            false,
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null,
            PupilQuestionSet.Groep78,
            "island",
            null,
            PupilJourneyTheme.ZeeExpeditie,
            DateTime.UtcNow);

        var body = PupilEilandSsrHtml.BuildBody(
            progress,
            def,
            culture,
            registry,
            antiforgery: "",
            savePath: "/leerling/eiland/save",
            error: null,
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie);

        var topbar = """
            <header class="top ll-topbar"><a class="brand" href="/"><span class="brand__text">Lobsy</span></a></header>
            """;
        var cssApp = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "app.css");
        var cssScholen = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        var cssKies = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-kies.css");
        var jsPath = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js");
        var html = $"""
            <!DOCTYPE html>
            <html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><base href="http://127.0.0.1/"></head>
            <body class="ll-g78-shell t-zee">{topbar}{body}</body></html>
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
        if (File.Exists(cssApp))
        {
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssApp });
        }

        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssScholen });
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssKies });
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = jsPath });

        if (step == 2)
        {
            await page.Locator("[data-ll-island-next]").ClickAsync();
        }

        await Assertions.Expect(page.Locator(".brand__text")).ToHaveTextAsync("Lobsy");
        await Assertions.Expect(page.Locator($"[data-ll-island-active-step=\"{step}\"]")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".step-pill:visible")).ToContainTextAsync($"Stap {step} van 2");

        var journeyMeta = page.Locator($"[data-ll-island-jmeta=\"{step}\"]:visible");
        await Assertions.Expect(journeyMeta).ToContainTextAsync($"stap {step} van 2", new() { IgnoreCase = true });

        if (step == 1)
        {
            await Assertions.Expect(page.Locator(".island-lead:visible")).ToContainTextAsync("Even rust");
            await Assertions.Expect(page.Locator("[data-ll-island-step=\"2\"]")).ToBeHiddenAsync();
        }
        else
        {
            await Assertions.Expect(page.Locator(".island-lead:visible")).ToContainTextAsync("Even verder");
            await Assertions.Expect(page.Locator("[data-ll-island-step=\"1\"]")).ToBeHiddenAsync();
            await page.Locator("[data-ll-island-prev]").ClickAsync();
            await Assertions.Expect(page.Locator("[data-ll-island-active-step=\"1\"]")).ToHaveCountAsync(1);
            await page.Locator("[data-ll-island-next]").ClickAsync();
        }

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, $"step-{step}.png"),
            FullPage = true
        });

        await AssertNoHorizontalClipAsync(page);
    }

    [Fact]
    public async Task Same_chip_cannot_stay_like_and_dislike_in_ui_and_hidden_fields()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var repoRoot = RepoRoot();
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(PupilQuestionSet.Groep78);
        var progress = new PupilProgressStateDto(
            Guid.NewGuid(),
            "8B",
            "ABC-DEF",
            PupilCodeStatus.InProgress,
            30,
            def.Bank.AllItems.Count,
            30,
            0,
            false,
            def.Bank.AllItems[29].Id,
            "pauze-eiland",
            new Dictionary<string, int>(),
            true,
            false,
            true,
            false,
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null,
            PupilQuestionSet.Groep78,
            "island",
            null,
            PupilJourneyTheme.ZeeExpeditie,
            DateTime.UtcNow);

        var body = PupilEilandSsrHtml.BuildBody(
            progress,
            def,
            culture,
            registry,
            antiforgery: "",
            savePath: "/leerling/eiland/save",
            error: null,
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie);

        var jsPath = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js");
        var html = $"""<!DOCTYPE html><html lang="nl"><body class="ll-g78-shell t-zee">{body}</body></html>""";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = jsPath });

        const string key = "sport";
        await page.Locator($"[data-ll-chip-like=\"{key}\"]").ClickAsync();
        await page.Locator("[data-ll-island-next]").ClickAsync();
        await page.Locator($"[data-ll-chip-dislike=\"{key}\"]").ClickAsync();

        var likeOn = await page.Locator($"[data-ll-island-step=\"2\"] [data-ll-chip-like=\"{key}\"].on").CountAsync();
        var dislikeOn = await page.Locator($"[data-ll-island-step=\"2\"] [data-ll-chip-dislike=\"{key}\"].on").CountAsync();
        Assert.Equal(0, likeOn);
        Assert.Equal(1, dislikeOn);

        var likesVal = await page.Locator("input[name=\"Likes\"]").InputValueAsync();
        var dislikesVal = await page.Locator("input[name=\"Dislikes\"]").InputValueAsync();
        Assert.DoesNotContain(key, likesVal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(key, dislikesVal, StringComparison.OrdinalIgnoreCase);

        await page.Locator("[data-ll-island-prev]").ClickAsync();
        likeOn = await page.Locator($"[data-ll-island-step=\"1\"] [data-ll-chip-like=\"{key}\"].on").CountAsync();
        dislikeOn = await page.Locator($"[data-ll-island-step=\"1\"] [data-ll-chip-dislike=\"{key}\"].on").CountAsync();
        Assert.Equal(0, likeOn);
        Assert.Equal(0, dislikeOn);
    }

    private static async Task AssertNoHorizontalClipAsync(IPage page)
    {
        var overflow = await page.EvaluateAsync<double>(
            """
            () => {
              const vw = document.documentElement.clientWidth;
              let max = 0;
              document.querySelectorAll('.ll-g78-island-wrap, .ll-island-card').forEach(el => {
                const r = el.getBoundingClientRect();
                if (r.right > vw + 0.5) max = Math.max(max, r.right - vw);
                if (r.left < -0.5) max = Math.max(max, -r.left);
              });
              return max;
            }
            """);
        Assert.True(overflow <= 1, $"Horizontal clip {overflow}px.");
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static CultureState NlCulture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        services.AddSingleton<IJSRuntime, NoJsRuntime>();
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var culture = services.BuildServiceProvider().GetRequiredService<CultureState>();
        culture.SetLanguageAsync("nl").GetAwaiter().GetResult();
        return culture;
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class NoJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => default;
    }
}
