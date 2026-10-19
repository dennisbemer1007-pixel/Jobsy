using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Shared candidate auth for functional E2E (one login → storage state, warmup).
/// </summary>
public static class FunctionalE2eAuth
{
    private static readonly SemaphoreSlim StorageLock = new(1, 1);
    private static string? _storageStatePath;
    private static string? _storageBaseUrl;

    public static async Task WarmupStackAsync(string baseUrl)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        for (var i = 0; i < 40; i++)
        {
            try
            {
                var web = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
                var api = await http.GetAsync(baseUrl.TrimEnd('/').Replace(":5201", ":5200") + "/health");
                if (web.IsSuccessStatusCode && api.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // retry
            }

            await Task.Delay(500);
        }
    }

    public static async Task<string> GetCandidateStorageStatePathAsync(IBrowser browser, string baseUrl)
    {
        await StorageLock.WaitAsync();
        try
        {
            if (_storageStatePath is not null
                && string.Equals(_storageBaseUrl, baseUrl, StringComparison.OrdinalIgnoreCase)
                && File.Exists(_storageStatePath))
            {
                return _storageStatePath;
            }

            await WarmupStackAsync(baseUrl);
            var path = Path.Combine(Path.GetTempPath(), "jobsy-functional-e2e-candidate.json");
            var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? KbE2e.DefaultCompleteEmail;
            var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? KbE2e.DefaultPassword;

            await using var ctx = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            if (!await TryDemoLoginAsync(page, baseUrl, email)
                && !await KbE2e.TryLoginAsync(page, baseUrl, email, password))
            {
                throw new InvalidOperationException($"Functional E2E: could not authenticate {email} at {baseUrl}.");
            }

            await page.GotoAsync(baseUrl.TrimEnd('/') + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await KbE2e.WaitForStyledAppAsync(page);
            if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Functional E2E: session not established for {email}.");
            }
            await ctx.StorageStateAsync(new() { Path = path });
            _storageStatePath = path;
            _storageBaseUrl = baseUrl;
            return path;
        }
        finally
        {
            StorageLock.Release();
        }
    }

    public static async Task<IBrowserContext> NewCandidateContextAsync(
        IBrowser browser,
        string baseUrl,
        BrowserNewContextOptions options)
    {
        var storage = await GetCandidateStorageStatePathAsync(browser, baseUrl);
        options.StorageStatePath = storage;
        options.IgnoreHTTPSErrors = true;
        var ctx = await browser.NewContextAsync(options);
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        return ctx;
    }

    private static async Task<bool> TryDemoLoginAsync(IPage page, string baseUrl, string email)
    {
        if (!FunctionalE2eGate.IsRequired
            && !baseUrl.Contains("127.0.0.1", StringComparison.Ordinal)
            && !baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            var tokenInput = page.Locator("form.au-form input[type=hidden]").First;
            await tokenInput.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 30_000 });
            var tokenName = await tokenInput.GetAttributeAsync("name") ?? "";
            var tokenValue = await tokenInput.GetAttributeAsync("value") ?? "";
            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                return false;
            }

            var form = page.Context.APIRequest.CreateFormData();
            form.Set(tokenName, tokenValue);
            form.Set("email", email);
            form.Set("returnUrl", "/banenkaart");
            var response = await page.Context.APIRequest.PostAsync(
                baseUrl + "/account/demo-login",
                new() { Form = form });

            if (!response.Ok && response.Status is not 302 and not 301)
            {
                return false;
            }

            await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await KbE2e.WaitForStyledAppAsync(page);
            return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
