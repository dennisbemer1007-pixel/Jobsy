using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class SollicitatiesPlainClickAuditPlaywrightTests(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly (string Path, string Title)[] Pages =
    [
        ("/candidate/applications?_plainSkin=1", "Sollicitaties plain"),
        ("/candidate/liked?_plainSkin=1", "Bewaard plain"),
    ];

    private readonly ITestOutputHelper _output = output;
    private IllustratedPagesWebFactory? _web;
    private string? _baseUrl;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _web = new IllustratedPagesWebFactory(plainSkin: true);
        _baseUrl = _web.ServerAddress.TrimEnd('/');
        await ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _web?.Dispose();
        await ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Plain_sollicitaties_and_bewaard_clickable_on_1440_and_390()
    {
        if (_baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        var allAudits = new List<Plain878PageClickAuditHelpers.PageAuditResult>();

        foreach (var (path, _) in Pages)
        {
            foreach (var (width, height, mobile) in new[] { (1440, 900, false), (390, 844, true) })
            {
                await using var context = await browser.NewContextAsync(new()
                {
                    IgnoreHTTPSErrors = true,
                    ViewportSize = new() { Width = width, Height = height },
                    IsMobile = mobile,
                    HasTouch = mobile,
                    Locale = "nl-NL",
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(120_000);
                await page.GotoAsync(_baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString(path));
                await PlaywrightCookieConsent.AcceptOnPageAsync(page);
                await Plain878PageClickAuditHelpers.PreparePageAsync(page, _baseUrl, path);
                allAudits.Add(await Plain878PageClickAuditHelpers.AuditPageAsync(page, path, width));
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Plain Sollicitaties + Bewaard klik-audit");
        sb.AppendLine();
        foreach (var (path, title) in Pages)
        {
            var subset = allAudits.Where(a => a.PagePath == path).ToList();
            var rows = Plain878PageClickAuditHelpers.MergeRows(subset);
            sb.AppendLine(Plain878PageClickAuditHelpers.FormatMarkdownTable(title, rows));
        }

        var report = sb.ToString();
        _output.WriteLine(report);
        Directory.CreateDirectory("/opt/cursor/artifacts");
        await File.WriteAllTextAsync("/opt/cursor/artifacts/plain-sollicitaties-click-audit.md", report, Encoding.UTF8);

        var failures = allAudits
            .SelectMany(a => a.Elements.Where(e => !e.Skipped && !e.Ok)
                .Select(e => $"{a.PagePath}@{a.ViewportWidth}: {e.Label} ({e.Reason})"))
            .ToList();
        Assert.True(failures.Count == 0, "Niet-klikbare elementen:\n" + string.Join("\n", failures) + "\n\n" + report);
    }
}
