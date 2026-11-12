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

[Collection("PlaywrightSmoke")]
public sealed class G78ReisImaginePlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, "desktop")]
    [InlineData(390, 844, "mobile")]
    public async Task Groep78_imagine_panel_opens_from_info_button(int width, int height, string label)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var repoRoot = RepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-reis-imagine", label);
        Directory.CreateDirectory(artifactDir);

        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(PupilQuestionSet.Groep78);
        var imagine = culture["LeerlingQ.9001.Voorbeeld"];
        var state = new PupilProgressStateDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "7A",
            "TEST-1",
            PupilCodeStatus.InProgress,
            0,
            60,
            0,
            0,
            false,
            "9001",
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
            JourneyThemeKey: PupilJourneyTheme.ZeeExpeditie);

        var body = PupilReisSsrHtml.BuildBody(
            state,
            def,
            culture,
            registry,
            "",
            "/leerling/reis/answer",
            null,
            true,
            PupilJourneyTheme.ZeeExpeditie);

        var cssScholen = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        var cssKies = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-kies.css");
        var jsPath = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js");
        var html = $"""
            <!DOCTYPE html>
            <html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>
            <body class="ll-g78-shell t-zee">{body}</body></html>
            """;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await page.SetContentAsync(html);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssScholen });
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssKies });
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = jsPath });

        var info = page.Locator(".ll-info-btn");
        await info.WaitForAsync();
        Assert.Equal("false", await info.GetAttributeAsync("aria-expanded"));
        Assert.Equal(0, await page.Locator("#ll-imagine-9001:not([hidden])").CountAsync());

        await info.ClickAsync();
        Assert.Equal("true", await info.GetAttributeAsync("aria-expanded"));
        var panel = page.Locator("#ll-imagine-9001");
        await panel.WaitForAsync();
        Assert.DoesNotContain("hidden", await panel.EvaluateAsync<string>("el => el.getAttribute('hidden')") ?? "", StringComparison.Ordinal);
        Assert.Contains(imagine, await panel.InnerTextAsync(), StringComparison.Ordinal);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, "g78-reis-imagine-open.png"),
            FullPage = true
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
