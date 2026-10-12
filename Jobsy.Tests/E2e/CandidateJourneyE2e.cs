using System.Diagnostics;
using System.Net.Http.Headers;
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
    public static string ArtifactsRoot =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT"))
            ? "/cursor/stores/self/fulljourney"
            : Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")!;

    private static string ProvisionSecret =>
        Environment.GetEnvironmentVariable("JOBSY_E2E_PROVISION_SECRET")
        ?? "local-dev-jobsy-provision-secret";

    private static void AddProvisionHeader(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("X-Jobsy-Provision-Secret", ProvisionSecret);
    }

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
        if (await WaitForFlagsAsync(baseUrl)
            && await WaitForAmbassadorsFlagAsync(baseUrl)
            && await WaitForSchoolsFlagAsync(baseUrl))
        {
            return;
        }

        await platform.UpdateAsync(new PlatformFeatureUpdate(
            EmployersEnabled: true,
            PassportPdfV2Enabled: true,
            CandidateExternalVacanciesEnabled: true,
            AmbassadorsEnabled: true,
            SchoolsEnabled: true));
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

    public static async Task<bool> WaitForSchoolsFlagAsync(string baseUrl)
    {
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        for (var i = 0; i < 12; i++)
        {
            try
            {
                using var doc = JsonDocument.Parse(await http.GetStringAsync(api + "/api/settings/feature-flags"));
                if (doc.RootElement.GetProperty("schoolsEnabled").GetBoolean())
                {
                    return true;
                }
            }
            catch
            {
                // retry
            }

            await Task.Delay(2_000);
        }

        return false;
    }

    public static async Task<bool> WaitForAmbassadorsFlagAsync(string baseUrl)
    {
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        for (var i = 0; i < 12; i++)
        {
            try
            {
                using var doc = JsonDocument.Parse(await http.GetStringAsync(api + "/api/settings/feature-flags"));
                if (doc.RootElement.GetProperty("ambassadorsEnabled").GetBoolean())
                {
                    return true;
                }
            }
            catch
            {
                // retry
            }

            await Task.Delay(2_000);
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
        var temp = path[..^4] + ".tmp.png";
        await page.ScreenshotAsync(new() { Path = temp, FullPage = false });
        try
        {
            File.Move(temp, path, overwrite: true);
        }
        catch (IOException)
        {
            await page.ScreenshotAsync(new() { Path = path, FullPage = false });
            try
            {
                File.Delete(temp);
            }
            catch (IOException)
            {
                // best-effort
            }
        }
    }

    public static async Task ShotViewportAsync(IPage page, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await page.ScreenshotAsync(new() { Path = outputPath, FullPage = false });
    }

    public static async Task<string?> LoginApiBearerAsync(string baseUrl, string email, string password)
    {
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var response = await http.PostAsJsonAsync($"{api}/api/auth/local-login", new { email, password });
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("sessionToken", out var token) ? token.GetString() : null;
    }

    public static async Task<Guid?> SeedExternalVacancyFixtureAsync(string baseUrl, string candidateEmail)
    {
        if (string.IsNullOrWhiteSpace(candidateEmail))
        {
            return null;
        }

        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var url = $"{api}/api/e2e/fixture/external-vacancy?email={Uri.EscapeDataString(candidateEmail)}";
        using var response = await http.PostAsync(url, null);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("id", out var id) && id.TryGetGuid(out var guid) ? guid : null;
    }

    public static async Task ClearCapturedEmailCodesAsync(string baseUrl)
    {
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _ = await http.PostAsync($"{api}/api/e2e/clear-email-codes", null);
    }

    public static async Task<string?> FetchEmailCodeAsync(string baseUrl, string email)
        => await FetchEmailCodeAsync(baseUrl, email, staleCode: null, maxAttempts: 40);

    public static async Task<string?> FetchEmailCodeAsync(string baseUrl, string email, string? staleCode, int maxAttempts = 40)
        => await FetchEmailCodeAsync(baseUrl, email, staleCode, afterSequence: null, maxAttempts);

    public static async Task<string?> FetchEmailCodeAsync(
        string baseUrl,
        string email,
        string? staleCode,
        long? afterSequence,
        int maxAttempts = 40)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
                var json = await http.GetFromJsonAsync<JsonElement>(
                    $"{api}/api/e2e/last-email-code?email={Uri.EscapeDataString(email)}");
                if (json.TryGetProperty("code", out var codeProp))
                {
                    var value = codeProp.GetString();
                    var sequence = json.TryGetProperty("sequence", out var seqProp) && seqProp.TryGetInt64(out var s)
                        ? s
                        : (long?)null;
                    if (!string.IsNullOrWhiteSpace(value)
                        && (afterSequence is null || (sequence is not null && sequence > afterSequence))
                        && (staleCode is null || !string.Equals(value, staleCode, StringComparison.Ordinal)))
                    {
                        return value;
                    }
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

    public static async Task<string?> WaitForOpenChallengeCodeAsync(
        string baseUrl,
        string email,
        int maxAttempts = 80)
        => await WaitForOpenChallengeCodeAsync(baseUrl, email, challengeId: null, maxAttempts);

    public static async Task<string?> WaitForOpenChallengeCodeAsync(
        string baseUrl,
        string email,
        Guid? challengeId,
        int maxAttempts = 80)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var json = await http.GetFromJsonAsync<JsonElement>(
                    $"{api}/api/e2e/open-email-challenge?email={Uri.EscapeDataString(email)}");
                if (challengeId is Guid expected
                    && json.TryGetProperty("challengeId", out var cidProp)
                    && cidProp.TryGetGuid(out var openId)
                    && openId != expected)
                {
                    await Task.Delay(500);
                    continue;
                }

                if (json.TryGetProperty("code", out var codeProp))
                {
                    var value = codeProp.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
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

    public static async Task<long?> GetEmailCodeSequenceAsync(string baseUrl, string email)
    {
        try
        {
            var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var json = await http.GetFromJsonAsync<JsonElement>(
                $"{api}/api/e2e/last-email-code?email={Uri.EscapeDataString(email)}");
            if (json.TryGetProperty("sequence", out var seqProp) && seqProp.TryGetInt64(out var seq))
            {
                return seq;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    public static async Task AnswerLikertUntilDoneAsync(IPage page, int maxClicks, int value = 4)
    {
        await DismissAssistantOverlayAsync(page);
        for (var i = 0; i < maxClicks; i++)
        {
            if (await page.Locator(".passport-overview, [data-testid=gd-result], .gd-result").CountAsync() > 0)
            {
                break;
            }

            var likert = page.Locator(".test-flow .test-likert, .test-likert").First;
            var optIndex = Math.Clamp(value - 1, 0, 4);
            var likertOpt = likert.Locator(".test-likert__opt").Nth(optIndex);
            if (await likertOpt.CountAsync() > 0)
            {
                await likertOpt.ClickAsync(new() { Timeout = 5_000, Force = IsMobileViewport(page) });
                await page.WaitForTimeoutAsync(450);
            }
            else if (await likert.Locator("input[type=radio]").Nth(optIndex).CountAsync() > 0)
            {
                try
                {
                    await likert.Locator("input[type=radio]").Nth(optIndex)
                        .CheckAsync(new() { Timeout = 5_000, Force = true });
                }
                catch (PlaywrightException)
                {
                    await page.EvaluateAsync(
                        """
                        value => {
                          const root = document.querySelector('.test-flow .test-likert, .test-likert');
                          if (!root) return;
                          const input = root.querySelector(`input[type=radio][value="${value}"]`);
                          if (!input) return;
                          input.checked = true;
                          input.dispatchEvent(new Event('change', { bubbles: true }));
                        }
                        """,
                        Math.Clamp(value, 1, 5));
                }

                await page.WaitForTimeoutAsync(450);
            }

            if (await likert.CountAsync() > 0)
            {
                var testNext = page.Locator("button.test-flow__next:not([disabled])").First;
                if (await testNext.CountAsync() == 0)
                {
                    await page.EvaluateAsync(
                        """
                        value => {
                          const root = document.querySelector('.test-flow .test-likert, .test-likert');
                          if (!root) return;
                          const input = root.querySelector(`input[type=radio][value="${value}"]`);
                          if (!input) return;
                          input.checked = true;
                          input.dispatchEvent(new Event('change', { bubbles: true }));
                        }
                        """,
                        Math.Clamp(value, 1, 5));
                    await page.WaitForTimeoutAsync(450);
                    testNext = page.Locator("button.test-flow__next:not([disabled])").First;
                }

                if (await testNext.CountAsync() > 0)
                {
                    await testNext.ClickAsync(new() { Timeout = 15_000, Force = IsMobileViewport(page) });
                    await page.WaitForTimeoutAsync(350);
                }

                continue;
            }

            var opt = page.Locator("fieldset.q-likert.is-current .q-likert__opt")
                .Nth(Math.Clamp(value - 1, 0, 4));
            if (await opt.CountAsync() == 0)
            {
                opt = page.Locator(".q-likert__scale .q-likert__opt").Nth(Math.Clamp(value - 1, 0, 4));
            }

            if (await opt.CountAsync() == 0)
            {
                opt = page.Locator($"[data-testid=gd-likert-{value}]").First;
            }

            if (await opt.CountAsync() == 0)
            {
                break;
            }

            await opt.ClickAsync(new() { Timeout = 5_000 });
            await page.WaitForTimeoutAsync(450);
        }

        for (var wait = 0; wait < 30; wait++)
        {
            var testNext = page.Locator("button.test-flow__next:not([disabled])").First;
            if (await testNext.CountAsync() > 0)
            {
                await testNext.ClickAsync(new() { Timeout = 15_000, Force = true });
                await page.WaitForTimeoutAsync(300);
                break;
            }

            var volgende = page.GetByRole(AriaRole.Button, new() { Name = "Volgende", Exact = true });
            if (await volgende.CountAsync() > 0 && await volgende.IsEnabledAsync())
            {
                await volgende.ClickAsync(new() { Timeout = 5_000 });
                await page.WaitForTimeoutAsync(300);
                break;
            }

            await page.WaitForTimeoutAsync(120);
        }

        await DismissAssistantOverlayAsync(page);
        var nextTest = page.GetByRole(AriaRole.Button, new() { Name = "Volgende test", Exact = true });
        if (await nextTest.CountAsync() > 0 && await nextTest.IsEnabledAsync())
        {
            await nextTest.ClickAsync(new() { Timeout = 15_000, Force = IsMobileViewport(page) });
            await page.WaitForTimeoutAsync(350);
        }
    }

    public static async Task DismissAssistantOverlayAsync(IPage page)
    {
        var width = page.ViewportSize?.Width ?? 1280;
        if (width >= 900)
        {
            return;
        }

        await page.EvaluateAsync(
            """
            () => {
              document.querySelector('.lobsy-assistant.is-open .share-modal__close')?.click();
              document.querySelector('.lobsy-assistant-tab.is-open')?.classList.remove('is-open');
              for (const el of document.querySelectorAll('.lobsy-assistant-tab, .lobsy-assistant-tab__btn')) {
                el.style.pointerEvents = 'none';
              }
            }
            """);
        await page.WaitForTimeoutAsync(200);
    }

    public static async Task<bool> TryAdvanceDiscoveryStepAsync(IPage page)
    {
        await DismissAssistantOverlayAsync(page);
        var force = IsMobileViewport(page);

        var testNext = page.Locator("button.test-flow__next:not([disabled])").First;
        if (await testNext.CountAsync() > 0)
        {
            await testNext.ClickAsync(new() { Timeout = 15_000, Force = force });
            await page.WaitForTimeoutAsync(350);
            return true;
        }

        var journeyNext = page.Locator(".journey-step__foot .btn-primary:not([disabled])").First;
        if (await journeyNext.CountAsync() > 0)
        {
            await journeyNext.ClickAsync(new() { Timeout = 15_000, Force = force });
            await page.WaitForTimeoutAsync(350);
            return true;
        }

        var primary = page.Locator("button.btn-primary:not([disabled])").First;
        if (await primary.CountAsync() > 0)
        {
            await primary.ClickAsync(new() { Timeout = 15_000, Force = force });
            await page.WaitForTimeoutAsync(350);
            return true;
        }

        return false;
    }

    private static bool IsMobileViewport(IPage page)
        => (page.ViewportSize?.Width ?? 1280) < 900;

    public static async Task ClickPrimaryAsync(IPage page)
    {
        await DismissAssistantOverlayAsync(page);
        var btn = page.Locator("button.btn-primary:not([disabled])").First;
        if (await btn.CountAsync() > 0)
        {
            await btn.ClickAsync(new() { Timeout = 15_000, Force = IsMobileViewport(page) });
        }
    }

    public static async Task SelectMaxShedDepthAsync(IPage page)
    {
        await DismissAssistantOverlayAsync(page);
        var picked = await page.EvaluateAsync<bool>(
            """
            () => {
              const opts = [...document.querySelectorAll('.journey-dive__opt:not(.is-done):not([disabled])')];
              if (opts.length === 0) {
                return false;
              }
              opts[opts.length - 1].click();
              return true;
            }
            """);
        if (!picked)
        {
            return;
        }

        await page.WaitForTimeoutAsync(400);
    }

    /// <summary>Clicks the mini-test primary (Volgende test on the last question).</summary>
    public static async Task TryAdvanceMiniTestWizardAsync(IPage page)
    {
        await DismissAssistantOverlayAsync(page);
        var testNext = page.Locator("button.test-flow__next:not([disabled])").First;
        if (await testNext.CountAsync() > 0)
        {
            await testNext.ClickAsync(new() { Timeout = 15_000, Force = IsMobileViewport(page) });
            await page.WaitForTimeoutAsync(500);
        }
    }

    public static async Task SignInExistingCandidateViaEmailCodeAsync(
        IPage page,
        string baseUrl,
        string email,
        string returnPath = "/candidate/paspoort?tab=dna")
    {
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        var sequenceBefore = await GetEmailCodeSequenceAsync(baseUrl, email) ?? 0;
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, $"{api}/api/auth/email-code/start")
        {
            Content = JsonContent.Create(new { email, culture = "nl" })
        };
        AddProvisionHeader(startRequest);
        using var startResponse = await http.SendAsync(startRequest);
        startResponse.EnsureSuccessStatusCode();
        using var startDoc = JsonDocument.Parse(await startResponse.Content.ReadAsStringAsync());
        var challengeId = startDoc.RootElement.GetProperty("challengeId").GetGuid();

        var code = await WaitForOpenChallengeCodeAsync(baseUrl, email, challengeId, maxAttempts: 80);
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException(
                $"No OTP captured for challenge {challengeId} — set JOBSY_E2E_CAPTURE_EMAIL_CODES=1 and restart API.");
        }

        using var verifyRequest = new HttpRequestMessage(HttpMethod.Post, $"{api}/api/auth/email-code/verify")
        {
            Content = JsonContent.Create(new
            {
                challengeId,
                code,
                rememberDevice = true,
                userAgent = "JobsyE2E/1.0"
            })
        };
        AddProvisionHeader(verifyRequest);
        using var verifyResponse = await http.SendAsync(verifyRequest);
        if (!verifyResponse.IsSuccessStatusCode)
        {
            var detail = await verifyResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"email-code verify failed ({(int)verifyResponse.StatusCode}): {detail}");
        }
        using var verifyDoc = JsonDocument.Parse(await verifyResponse.Content.ReadAsStringAsync());
        var sessionToken = verifyDoc.RootElement.GetProperty("sessionToken").GetString()
            ?? throw new InvalidOperationException("email-code verify returned no sessionToken");

        await page.Context.ClearCookiesAsync();
        await PlaywrightCookieConsent.AcceptAsync(page.Context);
        var safeReturn = Uri.EscapeDataString(returnPath);
        var token = Uri.EscapeDataString(sessionToken);
        await page.GotoAsync(
            $"{baseUrl.TrimEnd('/')}/account/register-signin?token={token}&returnUrl={safeReturn}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
    }

    public static async Task<byte[]?> FetchPaspoortPdfAsync(IPage page, string baseUrl)
    {
        var path = baseUrl.TrimEnd('/') + Jobsy.Web.Hosting.CandidatePassportPdfEndpoints.LobsyCvPath;
        try
        {
            var cookies = await page.Context.CookiesAsync(baseUrl);
            if (cookies.Count > 0)
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}")));
                using var response = await http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                    if (IsPdfBody(body, contentType))
                    {
                        return body;
                    }
                }
            }
        }
        catch
        {
            // fall through
        }

        try
        {
            var bytes = await page.EvaluateAsync<byte[]>(
                """
                async path => {
                  const res = await fetch(path, { credentials: 'include', cache: 'no-store' });
                  if (!res.ok) return [];
                  const buf = new Uint8Array(await res.arrayBuffer());
                  return Array.from(buf);
                }
                """,
                Jobsy.Web.Hosting.CandidatePassportPdfEndpoints.LobsyCvPath);
            if (bytes is { Length: > 5_000 } && bytes[0] == (byte)'%' && bytes[1] == (byte)'P')
            {
                return bytes;
            }
        }
        catch
        {
            // fall through
        }

        return null;
    }

    public static async Task<byte[]?> FetchPaspoortPdfViaApiBearerAsync(string baseUrl, string email, string password)
    {
        var bearer = await LoginApiBearerAsync(baseUrl, email, password);
        if (string.IsNullOrWhiteSpace(bearer))
        {
            return null;
        }

        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{api}/api/me/lobsy-cv.pdf");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        var body = await response.Content.ReadAsByteArrayAsync();
        return IsPdfBody(body, contentType) ? body : null;
    }

    private static bool IsPdfBody(byte[] body, string contentType)
        => body.Length > 5_000
           && body[0] == (byte)'%'
           && body[1] == (byte)'P'
           && (string.IsNullOrWhiteSpace(contentType)
               || contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase));

    public static async Task RenderPdfFirstPageAsync(byte[] pdf, string outputPng)
    {
        var dir = Path.GetDirectoryName(outputPng)!;
        Directory.CreateDirectory(dir);
        var pdfPath = Path.Combine(Path.GetTempPath(), $"jobsy-journey-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(pdfPath, pdf);
        try
        {
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
        }
        finally
        {
            try
            {
                File.Delete(pdfPath);
            }
            catch (IOException)
            {
                // temp cleanup is best-effort
            }
        }
    }
}
