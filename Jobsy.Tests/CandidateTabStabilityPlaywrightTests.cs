using System.Net;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mobile guardrail: fast Profiel↔Carrière switching must not show the circuit
/// error screen or trip HTTP 429. Soft-skips when <c>JOBSY_E2E_BASE_URL</c> is
/// unset or the host is unreachable (local CI without Acc secrets).
/// </summary>
public class CandidateTabStabilityPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    /// <summary>Bottom-nav destinations for a candidate (search → saved → applications → career → profile).</summary>
    private static readonly string[] CandidateTabs =
    [
        "/banenkaart",
        "/candidate/liked",
        "/candidate/applications",
        "/carriere",
        "/candidate/profile"
    ];

    [Fact]
    public async Task Mobile_candidate_tab_switching_has_no_error_screen_or_429()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            // Soft-skip: set JOBSY_E2E_BASE_URL (e.g. https://acceptatie.lobsy.nl) in CI secrets to enforce.
            return;
        }

        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });

        var status429 = 0;
        context.Response += (_, response) =>
        {
            if (response.Status == 429)
            {
                Interlocked.Increment(ref status429);
            }
        };

        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });

        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 60_000 });

        // Warm both destinations, then thrash Profiel ↔ Carrière.
        await page.GotoAsync(baseUrl + "/candidate/profile", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.WaitForTimeoutAsync(500);

        for (var i = 0; i < 10; i++)
        {
            await page.GotoAsync(baseUrl + "/carriere", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            await page.WaitForTimeoutAsync(300);

            await AssertNoCircuitErrorAsync(page);

            await page.GotoAsync(baseUrl + "/candidate/profile", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            await page.WaitForTimeoutAsync(300);

            await AssertNoCircuitErrorAsync(page);
        }

        foreach (var tab in CandidateTabs)
        {
            await page.GotoAsync(baseUrl + tab, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            await page.WaitForTimeoutAsync(400);
            await AssertNoCircuitErrorAsync(page);
        }

        Assert.Equal(0, status429);
    }

    [Fact]
    public void Playwright_guardrail_source_is_wired_for_ci()
    {
        var root = FindRepoRoot();
        var testFile = Path.Combine(root, "Jobsy.Tests", "CandidateTabStabilityPlaywrightTests.cs");
        var workflow = Path.Combine(root, ".github", "workflows", "pr-tests.yml");
        Assert.True(File.Exists(testFile));
        Assert.True(File.Exists(workflow));
        var src = File.ReadAllText(testFile);
        Assert.Contains("390", src);
        Assert.Contains("844", src);
        Assert.Contains("/carriere", src);
        Assert.Contains("/candidate/profile", src);
        Assert.Contains("Even iets misgegaan", src);
        Assert.Contains("JOBSY_E2E_BASE_URL", src);
    }

    private static async Task AssertNoCircuitErrorAsync(IPage page)
    {
        var body = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("Even iets misgegaan", body, StringComparison.OrdinalIgnoreCase);
        var html = await page.ContentAsync();
        Assert.DoesNotContain("circuit-error", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.GetAsync(baseUrl.TrimEnd('/') + "/");
            return response.StatusCode is HttpStatusCode.OK
                or HttpStatusCode.Redirect
                or HttpStatusCode.Found
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.MovedPermanently;
        }
        catch
        {
            return false;
        }
    }

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
