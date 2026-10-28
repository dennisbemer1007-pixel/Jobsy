using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Acceptatie klikaudit voor Sollicitaties (Dennis): alle zichtbare interactieve elementen op 1440 en 390.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class AccSollicitatiesClickAuditPlaywrightTests(ITestOutputHelper output) : IAsyncLifetime
{
    private const string AcceptatieGrokbotEmail = "lobsy-test@mail.grokbot.com";
    private readonly ITestOutputHelper _output = output;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact]
    public async Task Acceptatie_sollicitaties_every_interactive_is_clickable_at_1440_and_390()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null || !baseUrl.Contains("acceptatie.lobsy.nl", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine("Skip: set JOBSY_E2E_BASE_URL to https://acceptatie.lobsy.nl for acc klikaudit.");
            return;
        }

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        if (!await TryAcceptatieLoginAsync(page, baseUrl))
        {
            throw new InvalidOperationException("Could not log in on acceptatie for klikaudit.");
        }

        var ensure = await page.APIRequest.PostAsync(baseUrl + "/api/me/acceptatie-sollicitaties-demo-ensure");
        _output.WriteLine($"demo-ensure: {(int)ensure.Status} {ensure.StatusText}");

        var (at1440, at390) = await AccCandidatePageClickAudit.AuditPageAsync(
            page,
            baseUrl,
            "/candidate/applications");

        var rows = AccCandidatePageClickAudit.MergeRows(at1440, at390);
        var skipped = at1440.SkippedDangerous.Concat(at390.SkippedDangerous).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var table = AccCandidatePageClickAudit.FormatMarkdownTable(rows, skipped);
        _output.WriteLine(table);

        AccCandidatePageClickAudit.AssertAllOk(rows, _output);
    }

    private static async Task<bool> TryAcceptatieLoginAsync(IPage page, string baseUrl)
    {
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? KbE2e.DefaultPassword;
        var preferred = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL");
        if (!string.IsNullOrWhiteSpace(preferred)
            && await KbE2e.TryLoginAsync(page, baseUrl, preferred.Trim(), password))
        {
            return true;
        }

        foreach (var email in new[] { AcceptatieGrokbotEmail, "onboarding.e2e@jobsy.local", "test-kandidaat@lobsy.nl" })
        {
            if (await KbE2e.TryLoginAsync(page, baseUrl, email, password))
            {
                return true;
            }
        }

        return false;
    }
}
