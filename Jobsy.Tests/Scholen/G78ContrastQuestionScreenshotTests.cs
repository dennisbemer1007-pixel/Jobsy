using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78ContrastQuestionScreenshotTests
{
    [Theory]
    [InlineData(9001, 1440, 900, "desktop")]
    [InlineData(9001, 390, 844, "mobile")]
    [InlineData(9016, 1440, 900, "desktop")]
    [InlineData(9016, 390, 844, "mobile")]
    public async Task Contrast_question_and_scale_render(int itemId, int width, int height, string label)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var repoRoot = RepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-contrast-v2", itemId.ToString(), label);
        Directory.CreateDirectory(artifactDir);

        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.GetGroep78(PupilG78ScoringVersions.V2);
        var itemIndex = def.Bank.GetById(itemId.ToString(System.Globalization.CultureInfo.InvariantCulture))!.GlobalIndex;
        var state = new PupilProgressStateDto(
            Guid.NewGuid(),
            "7a Groep",
            "G78-TST",
            PupilCodeStatus.InProgress,
            itemIndex,
            def.Bank.AllItems.Count,
            itemIndex,
            def.PlatesShed(itemIndex),
            itemIndex >= def.Bank.AllItems.Count,
            itemId.ToString(System.Globalization.CultureInfo.InvariantCulture),
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
            "question",
            null,
            PupilJourneyTheme.ZeeExpeditie,
            DateTime.UtcNow,
            PupilG78ScoringVersions.V2);

        var body = PupilReisSsrHtml.BuildBody(
            state,
            def,
            culture,
            registry,
            antiforgeryHtml: "",
            answerSavePath: "/leerling/reis/antwoord",
            error: null,
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie);

        var cssScholen = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        var cssKies = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-kies.css");
        var html = $"""
            <!DOCTYPE html>
            <html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <base href="http://127.0.0.1/">
            <link rel="stylesheet" href="file://{cssScholen}">
            <link rel="stylesheet" href="file://{cssKies}">
            </head>
            <body class="ll-g78-shell t-zee">{body}</body></html>
            """;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await page.SetContentAsync(html);
        var path = Path.Combine(artifactDir, $"question-{itemId}-{label}.png");
        await page.Locator(".ll-g78-contrast, .answers").First.ScreenshotAsync(new LocatorScreenshotOptions { Path = path });
        Assert.True(File.Exists(path));
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
