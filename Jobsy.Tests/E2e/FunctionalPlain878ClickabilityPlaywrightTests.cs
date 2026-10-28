using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Plain paspoort + banenkaart: klik elke zichtbare interactieve control (1440 + 390) met elementFromPoint.
/// CI: <c>Suite=FunctionalE2e</c>. Acceptatie: <c>JOBSY_ACC_PLAIN_CLICK=1</c>.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class FunctionalPlain878ClickabilityPlaywrightTests
{
    private static readonly (string Path, string Title)[] Pages =
    [
        ("/candidate/paspoort", "Paspoort plain"),
        ("/banenkaart?maxMinutes=30&transport=Auto&originLat=52.0705&originLng=4.3007&originLabel=Naaldwijk", "Banenkaart plain shell"),
    ];

    [Fact]
    public async Task Plain878_every_interactive_clickable_on_1440_and_390()
    {
        if (!ShouldRun(out var accOnly))
        {
            return;
        }

        var (run, baseUrl) = await TryBeginAsync();
        if (!run)
        {
            return;
        }

        var plainLocal = (Environment.GetEnvironmentVariable("JOBSY_PLAIN_CLICK_CAPTURE") ?? "").Trim() == "1";
        if (plainLocal)
        {
            using var ping = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _ = await ping.GetAsync(baseUrl.TrimEnd('/') + "/");
        }
        else
        {
            await FunctionalE2eAuth.WarmupStackAsync(baseUrl);
        }
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var allAudits = new List<Plain878PageClickAuditHelpers.PageAuditResult>();
        foreach (var (path, _) in Pages)
        {
            foreach (var (width, height, mobile) in new[] { (1440, 900, false), (390, 844, true) })
            {
                await using var ctx = await FunctionalE2eAuth.NewCandidateContextAsync(browser, baseUrl, new()
                {
                    ViewportSize = new() { Width = width, Height = height },
                    IsMobile = mobile,
                    HasTouch = mobile,
                    Locale = "nl-NL",
                });
                var page = await ctx.NewPageAsync();
                page.SetDefaultTimeout(120_000);
                await Plain878PageClickAuditHelpers.PreparePageAsync(page, baseUrl, path);
                allAudits.Add(await Plain878PageClickAuditHelpers.AuditPageAsync(page, path, width));
            }
        }

        const string guestBanenkaart =
            "/banenkaart?maxMinutes=30&transport=Auto&originLat=52.0705&originLng=4.3007&originLabel=Naaldwijk";
        foreach (var (width, height, mobile) in new[] { (1440, 900, false), (390, 844, true) })
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
            });
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await Plain878PageClickAuditHelpers.PreparePageAsync(page, baseUrl, guestBanenkaart);
            allAudits.Add(await Plain878PageClickAuditHelpers.AuditPageAsync(page, guestBanenkaart + " (gast)", width));
        }

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")
            ?? "/opt/cursor/artifacts";
        Directory.CreateDirectory(artifactDir);
        var report = BuildReport(allAudits, accOnly);
        var reportPath = Path.Combine(artifactDir, "plain878-click-audit.md");
        await File.WriteAllTextAsync(reportPath, report, Encoding.UTF8);

        var failures = allAudits
            .SelectMany(a => a.Elements.Where(e => !e.Skipped && !e.Ok).Select(e => $"{a.PagePath}@{a.ViewportWidth}: {e.Label} ({e.Reason})"))
            .ToList();
        Assert.True(failures.Count == 0, "Niet-klikbare elementen:\n" + string.Join("\n", failures) + "\n\n" + report);
    }

    private static string BuildReport(IReadOnlyList<Plain878PageClickAuditHelpers.PageAuditResult> audits, bool accRun)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Plain878 klik-audit");
        sb.AppendLine();
        sb.AppendLine(accRun ? "Omgeving: Acceptatie" : "Omgeving: functionele E2E gate");
        sb.AppendLine();

        foreach (var (path, title) in Pages)
        {
            var subset = audits.Where(a => a.PagePath.StartsWith(path.Split('?')[0], StringComparison.OrdinalIgnoreCase)).ToList();
            var rows = Plain878PageClickAuditHelpers.MergeRows(subset);
            sb.AppendLine(Plain878PageClickAuditHelpers.FormatMarkdownTable(title, rows));
            sb.AppendLine();
        }

        var skipped = audits.SelectMany(a => a.Elements.Where(e => e.Skipped))
            .Select(e => e.Label + ": " + e.SkipReason)
            .Distinct()
            .ToList();
        if (skipped.Count > 0)
        {
            sb.AppendLine("## Overgeslagen (veiligheid)");
            foreach (var s in skipped)
            {
                sb.AppendLine("- " + s);
            }
        }

        return sb.ToString();
    }

    private static bool ShouldRun(out bool accOnly)
    {
        accOnly = (Environment.GetEnvironmentVariable("JOBSY_ACC_PLAIN_CLICK") ?? "").Trim() == "1";
        if (accOnly)
        {
            return true;
        }

        if ((Environment.GetEnvironmentVariable("JOBSY_PLAIN_CLICK_CAPTURE") ?? "").Trim() == "1")
        {
            return true;
        }

        if (FunctionalE2eGate.IsRequired)
        {
            return true;
        }

        return false;
    }

    private static IllustratedPagesWebFactory? _plainClickFactory;

    private static async Task<(bool Run, string BaseUrl)> TryBeginAsync()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_PLAIN_CLICK_CAPTURE") ?? "").Trim() == "1")
        {
            _plainClickFactory ??= new IllustratedPagesWebFactory(plainSkin: true);
            return (true, _plainClickFactory.ServerAddress);
        }

        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "";
        if (FunctionalE2eGate.IsRequired)
        {
            baseUrl = await FunctionalE2eGate.RequireReachableBaseUrlAsync();
            return (true, baseUrl);
        }

        var acc = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").Trim();
        if ((Environment.GetEnvironmentVariable("JOBSY_ACC_PLAIN_CLICK") ?? "").Trim() == "1")
        {
            if (!await SmokeReachability.IsReachableAsync(acc))
            {
                Assert.Fail("Acceptatie unreachable for plain click audit.");
            }

            return (true, acc);
        }

        if (string.IsNullOrWhiteSpace(baseUrl) || !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return (false, baseUrl);
        }

        return (true, baseUrl);
    }
}
