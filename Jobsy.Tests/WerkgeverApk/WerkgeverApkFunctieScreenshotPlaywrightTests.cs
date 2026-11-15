using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.WerkgeverApk;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.ValueObjects;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.WerkgeverApk;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace Jobsy.Tests.WerkgeverApk;

[Collection("PlaywrightSmoke")]
public sealed class WerkgeverApkFunctieScreenshotPlaywrightTests
    : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private const string EnvRun = "JOBSY_WERKGEVER_APK_FUNCTIE_SCREENSHOTS";
    private const string OrderpickerEsco = "cd94def5-3442-4c2e-ae3d-0761a3008bcb";
    private const string MagazijnEsco = "040fcfa5-24be-43eb-afd5-828297175b16";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static string OutDir => "/opt/cursor/artifacts/functie";

    private readonly RoleFunctionalWebAppFactory _api;
    private WerkgeverApkPlaywrightWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private FunctieSeed? _seed;

    public WerkgeverApkFunctieScreenshotPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRun)
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        await EnableLoopbaanApkFeaturesAsync();
        _seed = await SeedFunctieDemoAsync();
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web"));
        if (Directory.Exists(webRoot))
        {
            WerkgeverApkTemplateLocator.SetWebContentRoot(webRoot);
        }

        _web = new WerkgeverApkPlaywrightWebFactory(_api.Server.CreateHandler());
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact(Timeout = 600_000)]
    public async Task Capture_functie_screens_when_env_set()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRun || _browser is null || _web is null || _seed is null)
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        var baseUrl = _web.ServerAddress.TrimEnd('/');

        await CaptureManagerAsync(baseUrl, $"/werkgever/loopbaan-apk/teams/{_seed.TeamId:D}/codes/{_seed.PickCodeId:D}/functie?q=orderp", "08-functie-kiezen", "Kies de officiële functienaam");
        await CaptureEmployeeAsync(baseUrl, _seed.FunctiePlainCode, "/loopbaan-apk/functie", "09-mijn-functie", "Jouw functie:");
        await CaptureManagerAsync(baseUrl, $"/werkgever/loopbaan-apk/teams/{_seed.TeamId:D}/functie-totaal?esco={OrderpickerEsco}", "10-totaalbeeld-functie", "Competenties uit het officiële profiel");
        await CaptureManagerAsync(baseUrl, $"/werkgever/loopbaan-apk/teams/{_seed.TeamId:D}/functie-totaal?esco={MagazijnEsco}", "10b-te-weinig", "Nog te weinig deelnemers");
    }

    private async Task CaptureManagerAsync(string baseUrl, string path, string slug, string mustContain)
    {
        await using var context = await _browser!.NewContextAsync(new() { ViewportSize = new ViewportSize { Width = 1440, Height = 900 }, Locale = "nl-NL" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={_api.EnterpriseId:D}&role=EnterpriseManager&company={_api.CompanyId:D}&return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var body = await page.ContentAsync();
        Assert.Contains(mustContain, body, StringComparison.Ordinal);
        await CapturePairAsync(page, slug);
    }

    private async Task CaptureEmployeeAsync(string baseUrl, string code, string path, string slug, string mustContain)
    {
        await using var context = await _browser!.NewContextAsync(new() { ViewportSize = new ViewportSize { Width = 1440, Height = 900 }, Locale = "nl-NL" });
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/loopbaan-apk", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("input[name=code]").FillAsync(code);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var body = await page.ContentAsync();
        Assert.Contains(mustContain, body, StringComparison.Ordinal);
        await CapturePairAsync(page, slug);
    }

    private async Task CapturePairAsync(IPage page, string slug)
    {
        await CaptureAsync(page, slug, 1440);
        await CaptureAsync(page, slug, 390);
    }

    private async Task CaptureAsync(IPage page, string slug, int width)
    {
        var height = width <= 400 ? 844 : 900;
        await page.SetViewportSizeAsync(width, height);
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(150);
        var path = Path.Combine(OutDir, $"{slug}-{width}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
        Assert.True(new FileInfo(path).Length > 10_000, $"Screenshot too small: {path}");
    }

    private async Task EnableLoopbaanApkFeaturesAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") };
            db.PlatformFeatureSettings.Add(row);
        }

        row.EmployersEnabled = false;
        row.EmployerLoopbaanApkEnabled = true;
        await db.SaveChangesAsync();
        scope.ServiceProvider.GetRequiredService<IFeatureFlags>().Invalidate();
    }

    private async Task<FunctieSeed> SeedFunctieDemoAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IWerkgeverApkEmployeeCodeService>();
        var company = await db.Companies.FirstAsync(c => c.Id == _api.CompanyId);
        company.LoopbaanApkEnabled = true;
        company.Name = "Demo Logistiek B.V.";
        company.ParentCompanyId = null;
        var team = await db.WerkgeverApkTeams.FirstOrDefaultAsync(t => t.CompanyId == company.Id);
        if (team is null)
        {
            team = new WerkgeverApkTeam
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Team Logistiek",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = _api.EnterpriseId
            };
            db.WerkgeverApkTeams.Add(team);
        }

        if (await db.WerkgeverApkEmployeeCodes.CountAsync(c => c.TeamId == team.Id) < 12)
        {
            await codes.GenerateAsync(12, team);
        }

        var all = await db.WerkgeverApkEmployeeCodes
            .Include(c => c.Progress)
            .Where(c => c.TeamId == team.Id)
            .OrderBy(c => c.Number)
            .ToListAsync();

        var progressJson = CompleteProgressJson();
        var lowFlexJson = LowFlexProgressJson();

        for (var i = 0; i < all.Count; i++)
        {
            var code = all[i];
            code.EscoOccupationId = i switch
            {
                < 6 => OrderpickerEsco,
                < 9 => MagazijnEsco,
                _ => OrderpickerEsco
            };
            code.Label ??= $"Demo {code.Number}";
            if (i == 0)
            {
                code.Label = "Sara de Wit";
            }

            code.Status = WerkgeverApkEmployeeCodeStatus.Completed;
            code.SharedWithManager = i < 9;
            code.SharedAtUtc = DateTime.UtcNow;
            code.Progress ??= new WerkgeverApkEmployeeProgress { EmployeeCodeId = code.Id };
            code.Progress.ProgressJson = i == 0 ? lowFlexJson : progressJson;
            code.Progress.WelcomeSeen = true;
            code.Progress.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        var pick = all[0];
        var functie = all.First(c => codes.Unprotect(c.CodeProtected) is not null);
        return new FunctieSeed(team.Id, pick.Id, codes.Unprotect(functie.CodeProtected)!);
    }

    private static string CompleteProgressJson()
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers[q.Id] = 4;
            }
        }

        return JsonSerializer.Serialize(new { answers });
    }

    private static string LowFlexProgressJson()
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var answers = CompleteProgressJsonDictionary(catalog);
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Competentie, SchoolLevel.Mbo3))
        {
            if (q.Dimension.StartsWith("Creativiteit", StringComparison.Ordinal))
            {
                answers[q.Id] = 1;
            }
            if (string.Equals(q.Dimension, "Resultaatgerichtheid", StringComparison.Ordinal))
            {
                answers[q.Id] = 5;
            }
            if (string.Equals(q.Dimension, "Samenwerken", StringComparison.Ordinal))
            {
                answers[q.Id] = 5;
            }
        }

        return JsonSerializer.Serialize(new { answers });
    }

    private static Dictionary<string, int> CompleteProgressJsonDictionary(WerkgeverApkEmployeeCatalog catalog)
    {
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers[q.Id] = 4;
            }
        }

        return answers;
    }

    private sealed record FunctieSeed(Guid TeamId, Guid PickCodeId, string FunctiePlainCode);
}