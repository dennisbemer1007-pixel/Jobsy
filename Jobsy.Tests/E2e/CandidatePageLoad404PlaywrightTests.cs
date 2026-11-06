using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidatePageLoad404PlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
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

    [Fact]
    public async Task Candidate_ontdekkingsreis_has_no_same_origin_404_on_load()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var notFound = new List<string>();
        await using var context = await _browser!.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        page.Response += (_, resp) =>
        {
            if (resp.Status != 404)
            {
                return;
            }

            if (resp.Url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                notFound.Add(resp.Url);
            }
        };

        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis")}");
        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.WaitForSelectorAsync(".journey-overview", new() { Timeout = 60_000 });

        Assert.True(notFound.Count == 0, "Unexpected 404 responses: " + string.Join(", ", notFound));
    }
}
