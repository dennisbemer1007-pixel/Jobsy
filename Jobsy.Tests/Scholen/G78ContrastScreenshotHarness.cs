using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

internal static class G78ContrastScreenshotHarness
{
    internal static CultureState NlCulture()
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

    internal static string RepoRoot()
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

    internal static async Task PrepareGroep78ReisPageAsync(
        IPage page,
        string repoRoot,
        PupilProgressStateDto state,
        PupilQuestionSetDef def,
        CultureState culture,
        int? selectedAnswer = null)
    {
        var registry = new PupilQuestionSetRegistry();
        if (selectedAnswer is int sel && state.CurrentItemId is not null)
        {
            var answers = new Dictionary<string, int>(state.Answers, StringComparer.Ordinal)
            {
                [state.CurrentItemId] = sel,
            };
            state = state with { Answers = answers };
        }

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

        var html = "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
            + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + "<base href=\"http://127.0.0.1/\">"
            + "<link href=\"https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,700&family=Lexend:wght@400;600&display=swap\" rel=\"stylesheet\">"
            + "</head><body class=\"ll-g78-shell t-zee\">" + body + "</body></html>";

        await InstallWwwrootRoutingAsync(page, repoRoot);
        await page.SetContentAsync(html);
        await AddPupilReisStylesAsync(page, repoRoot);
        await page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Path = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js"),
        });

        if (selectedAnswer is not null)
        {
            await page.EvaluateAsync(
                """
                (val) => {
                  const btn = document.querySelector('[data-ll-answer-value="' + val + '"]');
                  if (btn) { btn.classList.add('sel'); btn.setAttribute('aria-checked','true'); }
                }
                """,
                selectedAnswer.Value.ToString());
        }
    }

    internal static async Task PrepareDitBenJijPageAsync(
        IPage page,
        string repoRoot,
        PupilResultPageDto dto,
        CultureState culture)
    {
        var registry = new PupilQuestionSetRegistry();
        var body = PupilDitBenJijSsrHtml.BuildBody(
            dto,
            culture,
            groep78: true,
            PupilJourneyTheme.ZeeExpeditie,
            registry,
            passportV2: true,
            pdfHref: "/leerling/pdf");
        var html = PassportDayLinkScreenshotHarness.PupilShellDocument(body);
        var cssPaths = PassportDayLinkScreenshotHarness.StylesheetPaths(
            repoRoot,
            PageAssetCatalog.StylesheetsForPupilSsr("/leerling/dit-ben-jij"),
            "css/features/leerling-paspoort-g78.css");
        await InstallWwwrootRoutingAsync(page, repoRoot);
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        foreach (var css in cssPaths)
        {
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = css });
        }

        await page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Path = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "js", "leerling-journey-ssr.js"),
        });
    }

    internal static async Task AssertReisStylesAsync(IPage page)
    {
        var diag = await page.EvaluateAsync<string>(
            """
            () => {
              const card = document.querySelector('.card.ll-question');
              const scale = document.querySelector('.answers .ans');
              const contrast = document.querySelector('.ll-g78-contrast');
              const labels = document.querySelectorAll('.answers .ans');
              const info = document.querySelector('.ll-info-btn');
              const payload = {
                hasCard: !!card,
                ansCount: labels.length,
                hasContrast: !!contrast,
                hasInfo: !!info,
                cardRadius: card ? parseFloat(getComputedStyle(card).borderRadius) : 0,
                ansRadius: scale ? parseFloat(getComputedStyle(scale).borderRadius) : 0,
                overflow: document.documentElement.scrollWidth > window.innerWidth + 2,
                scrollW: document.documentElement.scrollWidth,
                innerW: window.innerWidth,
              };
              payload.ok = payload.hasCard && payload.hasContrast && payload.ansCount >= 5
                && payload.hasInfo && payload.cardRadius >= 8 && payload.ansRadius > 0 && !payload.overflow;
              return JSON.stringify(payload);
            }
            """);
        using var doc = System.Text.Json.JsonDocument.Parse(diag);
        var ok = doc.RootElement.GetProperty("ok").GetBoolean();
        Assert.True(ok, "G78 reis styles: " + diag);
    }

    internal static PupilProgressStateDto ReisStateForItem(int itemId)
    {
        var registry = new PupilQuestionSetRegistry();
        var def = registry.GetGroep78(PupilG78ScoringVersions.V2);
        var item = def.Bank.GetById(itemId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ?? throw new InvalidOperationException("Item " + itemId);
        var itemIndex = item.GlobalIndex;
        return new PupilProgressStateDto(
            Guid.NewGuid(),
            "7a Demo",
            "G78-TST",
            PupilCodeStatus.InProgress,
            itemIndex,
            def.Bank.AllItems.Count,
            itemIndex,
            def.PlatesShed(itemIndex),
            false,
            itemId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "koraalrif",
            new Dictionary<string, int>(),
            true,
            false,
            false,
            false,
            false,
            ["sport", "dieren"],
            [],
            null,
            null,
            PupilQuestionSet.Groep78,
            "question",
            null,
            PupilJourneyTheme.ZeeExpeditie,
            DateTime.UtcNow,
            PupilG78ScoringVersions.V2);
    }

    internal static PupilResultPageDto BuildResultDto((PupilResult Result, PupilProgress Progress) fixture)
    {
        var renderer = new PupilStoryRenderer();
        var story = renderer.Render(fixture.Result, fixture.Progress, "nl");
        var likeKeys = ParseLikes(fixture.Progress.LikesJson);
        return new PupilResultPageDto(
            "7a",
            "G78-TST",
            "Demoschool",
            story,
            likeKeys,
            [],
            null,
            null,
            QuestionSet: PupilQuestionSet.Groep78);
    }

    private static IReadOnlyList<string> ParseLikes(string? likesJson)
    {
        if (string.IsNullOrWhiteSpace(likesJson))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(likesJson) ?? [];
        }
        catch
        {
            return [];
        }
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

    private static async Task AddPupilReisStylesAsync(IPage page, string repoRoot)
    {
        var wwwroot = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot");
        foreach (var url in PageAssetCatalog.StylesheetsForPupilSsr("/leerling/reis")
                     .Concat(PageAssetCatalog.LeerlingGroep78ThemeStylesheets("/leerling/reis")))
        {
            var relative = url.Split('?')[0].TrimStart('/');
            var path = Path.Combine(wwwroot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = path });
            }
        }
    }

    private sealed class NoJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default!;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default!;
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
}
