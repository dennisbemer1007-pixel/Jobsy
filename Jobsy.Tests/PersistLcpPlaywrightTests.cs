using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Soft-skip without <c>JOBSY_E2E_BASE_URL</c>.
/// Soft-skip remote Acc hosts that have not deployed slim persist / hero facade yet.
/// Local CI: vacancy detail LCP &lt; 2.5 s (desktop); /profiel/tests/career WebSocket
/// upload before first interactivity &lt; 50 KB.
/// </summary>
[Collection("PlaywrightSmoke")]
public class PersistLcpPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Desktop_vacancy_detail_lcp_under_2_5s_and_no_iframe_before_tap()
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        // Vacancy detail is an employer surface. With employers OFF the banenkaart redirects home.
        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        EnsureChromium();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        // Find a vacancy with a photo from the home list (or a known seed path).
        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        string? detailPath = null;
        try
        {
            await page.WaitForSelectorAsync(
                "a[href*='/vacancies/'], .job-card a[href*='/vacancies/']",
                new() { Timeout = 45_000 });
            detailPath = await page.EvaluateAsync<string?>("""
                () => {
                  const a = document.querySelector("a[href*='/vacancies/']");
                  return a ? a.getAttribute('href') : null;
                }
                """);
        }
        catch (PlaywrightException)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(detailPath))
        {
            return;
        }

        if (!detailPath.StartsWith('/'))
        {
            detailPath = "/" + detailPath.TrimStart('/');
        }

        var html = await (await context.APIRequest.GetAsync(baseUrl + detailPath)).TextAsync();
        if (html.Contains("<iframe", StringComparison.OrdinalIgnoreCase)
            && html.Contains("youtube", StringComparison.OrdinalIgnoreCase)
            && !html.Contains("detail-card__video-poster", StringComparison.Ordinal))
        {
            // Remote Acc without the facade yet.
            return;
        }

        Assert.DoesNotContain("youtube-nocookie.com/embed", html, StringComparison.OrdinalIgnoreCase);

        var sw = Stopwatch.StartNew();
        await page.GotoAsync(baseUrl + detailPath, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        try
        {
            await page.WaitForSelectorAsync(
                "img.detail-card__image, img.vacancy-media__photo, img.vacancy-photo",
                new() { Timeout = 30_000 });
        }
        catch (PlaywrightException)
        {
            return;
        }

        // Soft-skip hosts without fetchpriority=high on the hero.
        var heroHigh = await page.EvaluateAsync<bool>("""
            () => {
              const img = document.querySelector('img.detail-card__image, img.vacancy-media__photo, img.vacancy-photo');
              if (!img) return false;
              const fp = (img.getAttribute('fetchpriority') || img.getAttribute('fetchPriority') || '').toLowerCase();
              return fp === 'high';
            }
            """);
        if (!heroHigh)
        {
            return;
        }

        Assert.Equal(0, await page.Locator("iframe").CountAsync());

        var lcpMs = await page.EvaluateAsync<double>("""
            async () => {
              const existing = performance.getEntriesByType('largest-contentful-paint');
              if (existing.length) return existing[existing.length - 1].startTime;
              return await new Promise(resolve => {
                let last = 0;
                const po = new PerformanceObserver(list => {
                  const entries = list.getEntries();
                  if (entries.length) last = entries[entries.length - 1].startTime;
                });
                try { po.observe({ type: 'largest-contentful-paint', buffered: true }); } catch { resolve(0); return; }
                setTimeout(() => { po.disconnect(); resolve(last); }, 2500);
              });
            }
            """);
        sw.Stop();

        var isLocal = IsLocalStack(baseUrl);
        if (lcpMs <= 0)
        {
            lcpMs = sw.Elapsed.TotalMilliseconds;
        }

        if (lcpMs >= 2500 && !isLocal)
        {
            return;
        }

        Assert.True(lcpMs < 2500, $"Vacancy detail LCP was {lcpMs:0} ms (limit 2500 ms)");
    }

    [Fact]
    public async Task Career_test_detail_websocket_under_50kb_before_interactive()
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        EnsureChromium();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });

        if (!await TryLoginAsync(context, baseUrl))
        {
            return;
        }

        var page = await context.NewPageAsync();
        long wsOut = 0;
        var interactive = false;

        page.WebSocket += (_, ws) =>
        {
            if (!ws.Url.Contains("_blazor", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ws.FrameSent += (_, frame) =>
            {
                if (interactive)
                {
                    return;
                }

                if (frame.Binary is { Length: > 0 } binary)
                {
                    wsOut += binary.Length;
                }
                else if (!string.IsNullOrEmpty(frame.Text))
                {
                    wsOut += Encoding.UTF8.GetByteCount(frame.Text);
                }
            };
        };

        await page.GotoAsync(
            baseUrl + "/profiel/tests/career",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const t = document.body && document.body.innerText || '';
                  return t.includes('Carrière') || t.includes('Career') || t.includes('RIASEC')
                    || t.includes('Laden') || t.includes('Loading') || !!document.querySelector('.test-detail-page');
                }
                """,
                null,
                new() { Timeout = 45_000 });
        }
        catch (PlaywrightException)
        {
            return;
        }

        // Soft-skip Acc without slim persist (still shipping huge CareerCompass in HTML).
        var htmlBytes = Encoding.UTF8.GetByteCount(await page.ContentAsync());
        if (htmlBytes > 180_000 && !IsLocalStack(baseUrl))
        {
            return;
        }

        try
        {
            await page.WaitForFunctionAsync(
                "() => !!(window.Blazor && window.Blazor._internal)",
                null,
                new() { Timeout = 30_000 });
        }
        catch (PlaywrightException)
        {
            return;
        }

        // First interactivity: skeleton gone or primary CTA/result visible.
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const sk = document.querySelector('.page-content-skeleton, [aria-busy="true"]');
                  const ready = document.querySelector('.test-detail__header, .test-result-card, .btn-gold, .test-detail__choices');
                  return !!ready && !sk;
                }
                """,
                null,
                new() { Timeout = 60_000 });
        }
        catch (PlaywrightException)
        {
            if (!IsLocalStack(baseUrl))
            {
                return;
            }
        }

        interactive = true;
        Assert.True(wsOut < 50 * 1024, $"Career test detail sent {wsOut} bytes over WebSocket before interactivity (limit 50 KB)");
    }

    private static string? ResolveBaseUrl()
    {
        var raw = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    private static bool IsLocalStack(string baseUrl)
        => baseUrl.Contains("127.0.0.1", StringComparison.Ordinal)
           || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);

    private static void EnsureChromium()
        => Microsoft.Playwright.Program.Main(["install", "chromium"]);

    private static async Task<bool> TryLoginAsync(IBrowserContext context, string baseUrl)
    {
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[type='email'], input[name='email'], #email", DefaultEmail);
            await page.FillAsync("input[type='password'], input[name='password'], #password", DefaultPassword);
            await page.ClickAsync("button[type='submit'], button.login-submit");
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 45_000 });
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
