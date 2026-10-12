using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Playwright helpers for the full candidate journey E2E capture run.</summary>
public static class CandidateJourneyE2e
{
    public const string ArtifactsRoot = "/opt/cursor/artifacts/screenshots/candidate-journey";

    public static bool ShouldRun =>
        string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_FULL_JOURNEY"),
            "1",
            StringComparison.Ordinal);

    public static async Task EnableLiveFeatureFlagsAsync(string baseUrl)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = baseUrl.TrimEnd('/'),
                ["ConnectionStrings:JobsyDb"] = conn
            })
            .Build();

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var platform = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            config);
        await platform.UpdateAsync(new PlatformFeatureUpdate(
            EmployersEnabled: true,
            PassportPdfV2Enabled: true,
            CandidateExternalVacanciesEnabled: true));
        await Task.Delay(35_000);
    }

    public static async Task<bool> WaitForFlagsAsync(string baseUrl)
    {
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        for (var i = 0; i < 12; i++)
        {
            try
            {
                using var doc = JsonDocument.Parse(await http.GetStringAsync(api + "/api/settings/feature-flags"));
                var root = doc.RootElement;
                if (root.GetProperty("employersEnabled").GetBoolean()
                    && root.GetProperty("passportPdfV2Enabled").GetBoolean()
                    && root.GetProperty("candidateExternalVacanciesEnabled").GetBoolean())
                {
                    return true;
                }
            }
            catch
            {
                // retry
            }

            await Task.Delay(5_000);
        }

        return false;
    }

    public static string ShotDir(string viewportTag)
    {
        var dir = Path.Combine(ArtifactsRoot, viewportTag);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static async Task WaitForCandidateChromeAsync(IPage page)
    {
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("h1, .ob-wizard, main, .passport-overview", new() { Timeout = 90_000 });
        await page.WaitForTimeoutAsync(1_200);
    }

    public static async Task ShotAsync(IPage page, string viewportTag, string stepSlug)
    {
        var path = Path.Combine(ShotDir(viewportTag), $"{stepSlug}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
    }

    public static async Task<string?> FetchEmailCodeAsync(string baseUrl, string email)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
                var json = await http.GetFromJsonAsync<JsonElement>(
                    $"{api}/api/e2e/last-email-code?email={Uri.EscapeDataString(email)}");
                if (json.TryGetProperty("code", out var code))
                {
                    return code.GetString();
                }
            }
            catch
            {
                // not ready
            }

            await Task.Delay(500);
        }

        return null;
    }

    public static async Task AnswerLikertUntilDoneAsync(IPage page, int maxClicks, int value = 4)
    {
        for (var i = 0; i < maxClicks; i++)
        {
            if (await page.Locator(".passport-overview, [data-testid=gd-result], .gd-result").CountAsync() > 0)
            {
                break;
            }

            var opt = page.Locator("fieldset.q-likert.is-current .q-likert__opt, .q-likert__opt:not(.q-likert--collapsed *)")
                .Nth(Math.Clamp(value - 1, 0, 4));
            if (await opt.CountAsync() == 0)
            {
                opt = page.Locator($"[data-testid=gd-likert-{value}]").First;
            }

            if (await opt.CountAsync() == 0)
            {
                break;
            }

            await opt.ClickAsync(new() { Timeout = 5_000 });
            await page.WaitForTimeoutAsync(180);
        }
    }

    public static async Task ClickPrimaryAsync(IPage page)
    {
        var btn = page.Locator("button.btn-primary:not([disabled])").First;
        if (await btn.CountAsync() > 0)
        {
            await btn.ClickAsync(new() { Timeout = 15_000 });
        }
    }

    public static async Task SelectMaxShedDepthAsync(IPage page)
    {
        var options = page.Locator(".journey-dive__opt:not(.is-done):not([disabled])");
        var count = await options.CountAsync();
        if (count == 0)
        {
            return;
        }

        await options.Last.ClickAsync(new() { Timeout = 10_000 });
    }

    public static async Task RenderPdfFirstPageAsync(byte[] pdf, string outputPng)
    {
        var dir = Path.GetDirectoryName(outputPng)!;
        Directory.CreateDirectory(dir);
        var pdfPath = Path.Combine(dir, "_tmp.pdf");
        await File.WriteAllBytesAsync(pdfPath, pdf);
        var psi = new ProcessStartInfo("pdftoppm", $"-png -f 1 -l 1 -singlefile \"{pdfPath}\" \"{outputPng[..^4]}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return;
        }

        await proc.WaitForExitAsync();
        File.Delete(pdfPath);
    }
}
