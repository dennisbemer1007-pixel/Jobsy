using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
/// <summary>Captures browser console errors on candidate applications (mobile).</summary>
public sealed class SollicitatiesMobileConsolePlaywrightTests(ITestOutputHelper output) : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _localWeb;
    private string? _localBaseUrl;

    public async ValueTask InitializeAsync()
    {
        if (await KbE2e.TryReadyBaseUrlAsync() is not null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _localWeb = new IllustratedPagesWebFactory();
        _localBaseUrl = _localWeb.ServerAddress.TrimEnd('/');
        await ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _localWeb?.Dispose();
        await ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Mobile_applications_page_has_no_console_errors()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync() ?? _localBaseUrl;
        if (baseUrl is null)
        {
            return;
        }

        var errors = new List<string>();
        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(390, 844));
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        page.Console += (_, msg) =>
        {
            if (msg.Type is "error" && !IsIgnoredConsole(msg.Text))
            {
                errors.Add(msg.Text);
            }
        };
        page.PageError += (_, err) => errors.Add(err);

        if (UseCaptureTestSignIn(baseUrl, _localBaseUrl))
        {
            await page.GotoAsync(baseUrl + "/__test/sign-in?return=/candidate/applications");
        }
        else if (KbE2e.IsLocalCiStack(baseUrl))
        {
            await FunctionalE2eAuth.SignInSeededCandidateAsync(page, baseUrl, "/candidate/applications");
        }
        else if (!await TryAcceptatieOrDefaultLoginAsync(page, baseUrl))
        {
            throw new InvalidOperationException($"Could not log in at {baseUrl}.");
        }
        else
        {
            if (baseUrl.Contains("acceptatie.lobsy.nl", StringComparison.OrdinalIgnoreCase))
            {
                await page.APIRequest.PostAsync(baseUrl + "/api/me/acceptatie-sollicitaties-demo-ensure");
            }

            await page.GotoAsync(
                baseUrl + "/candidate/applications",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
        }

        await page.WaitForSelectorAsync("[data-testid=\"applications-page-ssr\"]", new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1500);

        var berichtenTab = page.Locator(".sollicitaties-segment__tab[data-sollic-tab=\"berichten\"]");
        if (await berichtenTab.CountAsync() > 0)
        {
            await berichtenTab.ClickAsync();
            await page.WaitForTimeoutAsync(1500);

            var thread = page.Locator(".sollic-berichten-ti--mob.sollic-berichten-ti__select").First;
            if (await thread.CountAsync() > 0)
            {
                await thread.ClickAsync();
                await page.WaitForTimeoutAsync(1000);
                var back = page.Locator("[data-sollic-berichten-back]").First;
                if (await back.CountAsync() > 0)
                {
                    await back.ClickAsync();
                    await page.WaitForTimeoutAsync(500);
                }
            }
        }

        var card = page.Locator(".sollic-card.sollic-card--app:not([hidden])").First;
        if (await card.CountAsync() > 0)
        {
            await card.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(1000);
        }

        foreach (var e in errors)
        {
            output.WriteLine("console.error: " + e);
        }

        Assert.Empty(errors);
    }

    private static bool IsIgnoredConsole(string text)
        => text.Contains("favicon", StringComparison.OrdinalIgnoreCase)
           || text.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase)
           || text.Contains("net::ERR_", StringComparison.OrdinalIgnoreCase);

    private static bool UseCaptureTestSignIn(string baseUrl, string? localBaseUrl) =>
        localBaseUrl is not null && string.Equals(baseUrl, localBaseUrl, StringComparison.Ordinal);

    private static async Task<bool> TryAcceptatieOrDefaultLoginAsync(IPage page, string baseUrl)
    {
        var password = KbE2e.ResolveCandidatePassword(baseUrl);
        if (await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.ResolveCandidateEmail(), password))
        {
            return true;
        }

        if (baseUrl.Contains("acceptatie.lobsy.nl", StringComparison.OrdinalIgnoreCase))
        {
            string[] acceptatieCandidates =
            [
                "lobsy-test@mail.grokbot.com",
                "onboarding.e2e@jobsy.local",
                "test-kandidaat@lobsy.nl",
            ];
            foreach (var email in acceptatieCandidates)
            {
                if (await KbE2e.TryLoginAsync(page, baseUrl, email, password))
                {
                    return true;
                }
            }

            return false;
        }

        return await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, password);
    }
}
