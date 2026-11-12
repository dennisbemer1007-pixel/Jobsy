using Bunit;
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
public sealed class G78ResultPagePlaywrightTests : BunitContext
{
    public G78ResultPagePlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IPupilQuestionSetRegistry>(new PupilQuestionSetRegistry());
    }

    [Theory]
    [InlineData(1440, 900, "g78-result-page-desktop.png")]
    [InlineData(390, 844, "g78-result-page-mobile.png")]
    public async Task G78_result_page_renders_story_and_tiles(int width, int height, string fileName)
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var (result, progress) = PupilG78RealisticFixtures.LeadsGroup();
        var renderer = new PupilStoryRenderer();
        var story = renderer.Render(result, progress, "nl");
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var dto = new PupilResultPageDto(
            "8B",
            "DEMO-CODE",
            "Demo OBS",
            story,
            ["theater", "lezen"],
            [],
            null,
            null,
            QuestionSet: PupilQuestionSet.Groep78);
        var registry = Services.GetRequiredService<IPupilQuestionSetRegistry>();
        var html = PupilDitBenJijSsrHtml.BuildBody(
            dto, culture, groep78: true, "koraalrif", registry, passportV2: true, "/leerling/pdf");
        var wrapped = WrapHtml(html);

        Directory.CreateDirectory("/opt/cursor/artifacts");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            DeviceScaleFactor = width < 768 ? 2 : 1,
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(wrapped, new() { WaitUntil = WaitUntilState.Load });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine("/opt/cursor/artifacts", fileName),
            FullPage = true
        });
        Assert.True(await page.Locator("[data-testid=ll-g78-result-tiles]").IsVisibleAsync());
        Assert.True(await page.Locator("[data-testid=ll-g78-growth-competencies]").IsVisibleAsync());
    }

    private static string WrapHtml(string body)
    {
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web", "wwwroot"));
        var cssPath = Path.Combine(webRoot, "css", "features", "leerling-werelden-kies.css");
        var css = File.Exists(cssPath) ? File.ReadAllText(cssPath) : "";
        var jsPath = Path.Combine(webRoot, "js", "leerling-journey-ssr.js");
        var js = File.Exists(jsPath) ? File.ReadAllText(jsPath) : "";
        return $"""
            <!DOCTYPE html>
            <html lang="nl"><head><meta charset="utf-8"/><style>{css}</style></head>
            <body class="ll-g78-shell"><main>{body}</main>
            <script>{js}</script></body></html>
            """;
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
