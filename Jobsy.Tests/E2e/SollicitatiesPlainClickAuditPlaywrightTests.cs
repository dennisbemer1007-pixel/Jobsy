using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class SollicitatiesPlainClickAuditPlaywrightTests(ITestOutputHelper output) : IAsyncLifetime
{
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

    [Theory]
    [InlineData("/candidate/applications?_plainSkin=1", "Sollicitaties")]
    [InlineData("/candidate/liked?_plainSkin=1", "Bewaard")]
    public async Task Plain_page_content_clickable_at_1440_and_390(string path, string label)
    {
        if (_baseUrl is null)
        {
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(_baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString(path));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("[data-plain-app]", new() { Timeout = 60_000 });

        var (at1440, at390) = await AccCandidatePageClickAudit.AuditPageAsync(page, _baseUrl, path);
        var rows = AccCandidatePageClickAudit.MergeRows(at1440, at390);
        var skipped = at1440.SkippedDangerous.Concat(at390.SkippedDangerous).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _output.WriteLine($"## {label}");
        _output.WriteLine(AccCandidatePageClickAudit.FormatMarkdownTable(rows, skipped));
        AccCandidatePageClickAudit.AssertAllOk(rows, _output);
    }
}
