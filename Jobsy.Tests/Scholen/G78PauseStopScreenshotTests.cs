using Microsoft.AspNetCore.Http;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78PauseStopScreenshotTests
{
    [Theory]
    [InlineData(1440, 900, "desktop")]
    [InlineData(390, 844, "mobile")]
    public async Task Paused_stop_screen_renders_with_theme_css(int width, int height, string label)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var repoRoot = RepoRoot();
        var artifactDir = "/opt/cursor/artifacts/pause-stop-fix";
        Directory.CreateDirectory(artifactDir);

        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var journey = PupilGroep78JourneySnapshot.FromPauseQuery(
            new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["g78"] = "1",
                ["theme"] = PupilJourneyTheme.ZeeExpeditie,
                ["jn"] = "koraalrif",
                ["ja"] = "5",
                ["jt"] = "60",
            }),
            culture,
            registry,
            PupilJourneyTheme.ZeeExpeditie);

        var body = PupilStopSsrHtml.BuildBody(
            culture,
            done: "1",
            antiforgeryField: "",
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie,
            registry: registry,
            journey: journey);

        var frame = PupilLeerlingSsrFrame.RenderDocument(new PupilLeerlingSsrDocumentOptions
        {
            PageTitle = culture["Leerling.Stop.Title"],
            BodyHtml = body,
            Pupil = new System.Security.Claims.ClaimsPrincipal(),
            RoutePath = "/leerling/stop",
            UseGroep78Shell = true,
            JourneyThemeKey = PupilJourneyTheme.ZeeExpeditie,
            ShowPause = false,
        });

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
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

        await page.SetContentAsync(frame);

        foreach (var href in PageAssetCatalog.StylesheetsForPupilSsr("/leerling/stop"))
        {
            var path = href.TrimStart('/');
            var file = Path.Combine(wwwroot, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(file))
            {
                await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = file });
            }
        }

        var imgDir = Path.Combine(wwwroot, "leerling", "werelden", "img");
        await page.RouteAsync("**/leerling/werelden/img/**", async route =>
        {
            var name = Path.GetFileName(route.Request.Url);
            var local = Path.Combine(imgDir, name);
            if (File.Exists(local))
            {
                await route.FulfillAsync(new RouteFulfillOptions { Path = local });
            }
            else
            {
                await route.FallbackAsync();
            }
        });

        await page.Locator("[data-testid=leerling-stop-pause]").WaitForAsync();
        await Assertions.Expect(page.Locator(".g78-jmeta span")).ToContainTextAsync("5 van 60");
        Assert.Equal(0, await page.Locator("[data-testid=ll-g78-coach]").CountAsync());
        Assert.Equal(0, await page.Locator(".ll-g78-coach-mobile").CountAsync());

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, $"pause-stop-{label}.png"),
            FullPage = true
        });
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
