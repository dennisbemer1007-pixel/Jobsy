using System.Collections.Concurrent;
using Jobsy.Web.KandidaatBanen;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Shared Playwright helpers for kandidaat-banen E2E (file 09). Soft-skip when
/// <c>JOBSY_E2E_BASE_URL</c> is unset/unreachable.
/// </summary>
public static class KbE2e
{
    public const string DefaultCompleteEmail = "kandidaat@jobsy.local";
    public const string DefaultIncompleteEmail = "valentine@jobsy.local";
    public const string DefaultPassword = "Jobsy123!";

    public static readonly (int Width, int Height) Desktop = (1440, 900);
    public static readonly (int Width, int Height) Mobile = (390, 844);

    /// <summary>
    /// Map route: <see cref="E2eRoutes.Banenkaart"/> when landing 04 present, else <see cref="KbRoutes.Map"/>.
    /// </summary>
    public static string MapPath => E2eRoutes.Banenkaart;

    public static string? TryBaseUrl()
    {
        var raw = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    public static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns null when E2E should soft-skip (no URL / unreachable).</summary>
    public static async Task<string?> TryReadyBaseUrlAsync()
    {
        var baseUrl = TryBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return null;
        }

        return baseUrl;
    }

    public static async Task<IBrowser> LaunchChromiumAsync()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var playwright = await Playwright.CreateAsync();
        return await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public static BrowserNewContextOptions DesktopContext()
        => new()
        {
            ViewportSize = new() { Width = Desktop.Width, Height = Desktop.Height },
            IgnoreHTTPSErrors = true
        };

    public static BrowserNewContextOptions MobileContext()
        => new()
        {
            ViewportSize = new() { Width = Mobile.Width, Height = Mobile.Height },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        };

    public static BrowserNewContextOptions ContextFor(int width, int height)
        => new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = width < 900,
            IsMobile = width < 900,
            IgnoreHTTPSErrors = true
        };

    public static KbPageCapture AttachCapture(IPage page)
    {
        var capture = new KbPageCapture();
        page.PageError += (_, err) => capture.PageErrors.Add(err);
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !IsIgnoredConsole(msg.Text))
            {
                capture.ConsoleErrors.Add(msg.Text);
            }
        };
        page.Response += (_, resp) =>
        {
            if (resp.Status != 404)
            {
                return;
            }

            var url = resp.Url;
            if (url.Contains("/fonts/", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref capture.Font404);
            }

            if (url.Contains("/api/travel/isochrones", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref capture.Isochrone404);
            }
        };
        return capture;
    }

    public static void AssertHealthy(KbPageCapture capture, bool requireNoIsochrone404 = true)
    {
        var stack = capture.PageErrors
            .Where(e => e.Contains("Maximum call stack size exceeded", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(stack.Count == 0, "Maximum call stack: " + string.Join(" | ", stack));

        var fatal = capture.PageErrors
            .Where(e => !e.Contains("Maximum call stack size exceeded", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(fatal.Count == 0, "pageerror: " + string.Join(" | ", fatal));

        var consoleStack = capture.ConsoleErrors
            .Where(e => e.Contains("Maximum call stack", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(consoleStack.Count == 0, "console stack: " + string.Join(" | ", consoleStack));

        Assert.Equal(0, capture.Font404);
        if (requireNoIsochrone404)
        {
            Assert.Equal(0, capture.Isochrone404);
        }
    }

    public static async Task AssertNoCircuitErrorAsync(IPage page)
    {
        var circuit = await page.EvaluateAsync<bool>("""
            () => {
              const t = document.body?.innerText || '';
              return t.includes('Even iets misgegaan')
                || t.includes('Something went wrong')
                || !!document.querySelector('.circuit-error, [data-testid=circuit-error]');
            }
            """);
        Assert.False(circuit, "Circuit.ErrorTitle visible (Even iets misgegaan)");
    }

    public static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string? email = null, string? password = null)
    {
        email ??= Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultCompleteEmail;
        password ??= Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.WaitForSelectorAsync("input[name='email'], input[type=email], #email", new() { Timeout = 30_000 });
            var emailInput = page.Locator("input[name='email'], input[type=email], #email").First;
            var passwordInput = page.Locator("input[name='password'], input[type=password], #password").First;
            await emailInput.FillAsync(email);
            await passwordInput.FillAsync(password);
            var submit = page.Locator("button.login-submit, button[type=submit]").First;
            await submit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            try
            {
                await page.WaitForFunctionAsync(
                    "() => { const b = document.querySelector('button.login-submit'); return !b || !b.disabled; }",
                    null,
                    new() { Timeout = 15_000 });
            }
            catch
            {
                // submit may not use login-submit class
            }

            await submit.ClickAsync();
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> TryLoginIncompleteAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_INCOMPLETE_EMAIL") ?? DefaultIncompleteEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_INCOMPLETE_PASSWORD")
            ?? Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD")
            ?? DefaultPassword;
        return await TryLoginAsync(page, baseUrl, email, password);
    }

    public static string ScreenshotDir()
    {
        var dir = Path.Combine(FindRepoRoot(), "artifacts", "e2e", "kandidaat-banen");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static async Task ScreenshotAsync(IPage page, string scenario, string viewport)
    {
        var path = Path.Combine(ScreenshotDir(), $"{scenario}-{viewport}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
    }

    /// <summary>
    /// Werkgevers OFF scenarios: only when <c>JOBSY_E2E_ALLOW_FEATURE_TOGGLE=1</c>
    /// and host is localhost/127.0.0.1. Never toggle on shared Acc.
    /// </summary>
    public static bool AllowsFeatureToggle(string baseUrl)
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JOBSY_E2E_ALLOW_FEATURE_TOGGLE"),
                "1",
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host is "localhost" or "127.0.0.1" or "::1";
    }

    /// <summary>Dep C absent on this stack — feature gating E2E always skips.</summary>
    public static bool HasEmployersFeatureGate()
        => false; // KB-FALLBACK(C): RequiresFeature / PlatformFeature.Employers absent

    public static bool HasCandidateJobListTabs()
        => false; // Dep C ABSENT

    public static bool HasDislikeSource()
        => false; // Dep D ABSENT (KbNoDislikeSource)

    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private static bool IsIgnoredConsole(string text)
        => text.Contains("favicon", StringComparison.OrdinalIgnoreCase)
           || text.Contains("WebSocket closed", StringComparison.OrdinalIgnoreCase)
           || text.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase)
           || text.Contains("net::ERR_", StringComparison.OrdinalIgnoreCase)
           || text.Contains("openfreemap.org", StringComparison.OrdinalIgnoreCase)
           || text.Contains("AJAXError", StringComparison.OrdinalIgnoreCase)
           || text.Contains("maplibre", StringComparison.OrdinalIgnoreCase)
           || text.Contains("cdn.", StringComparison.OrdinalIgnoreCase);

    public sealed class KbPageCapture
    {
        public ConcurrentBag<string> PageErrors { get; } = new();
        public ConcurrentBag<string> ConsoleErrors { get; } = new();
        public int Font404;
        public int Isochrone404;
    }
}
