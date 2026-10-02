using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Fotokaarten applications UI smoke (mobile 390×844 + desktop 1280×800).
/// Soft-skips when <c>JOBSY_E2E_BASE_URL</c> is unset/unreachable.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateApplicationsPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_applications_fotokaarten_smoke()
        => await RunAsync(390, 844);

    [Fact]
    public async Task Desktop_applications_fotokaarten_smoke()
        => await RunAsync(1280, 800);

    private static async Task RunAsync(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var artifactDir = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "playwright-applications",
            $"{width}x{height}-{DateTime.UtcNow:yyyyMMddHHmmss}");
        Directory.CreateDirectory(artifactDir);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            AcceptDownloads = true
        });

        var page = await context.NewPageAsync();
        var pageErrors = new ConcurrentBag<string>();
        var consoleErrors = new ConcurrentBag<string>();
        page.PageError += (_, err) => pageErrors.Add(err);
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !IsIgnoredConsole(msg.Text))
            {
                consoleErrors.Add(msg.Text);
            }
        };

        if (!await TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed for applications smoke.");
        }

        await page.GotoAsync(
            baseUrl + "/candidate/applications",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".application-counters").WaitForAsync(new() { Timeout = 30_000 });
        await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "01-applications.png"), FullPage = true });

        await AssertNoOverflowAsync(page, width);

        // Timeline visible on active card (kb-timeline).
        await Assertions.Expect(page.Locator(".kb-timeline").First).ToBeVisibleAsync(new() { Timeout = 10_000 });
        // Dep C ABSENT: no CandidateJobListTabs.
        Assert.Equal(0, await page.Locator(".candidate-job-list-tabs, .kb-job-tabs").CountAsync());

        var allBtn = page.Locator(".application-counters__btn").Nth(0);
        var runningBtn = page.Locator(".application-counters__btn").Nth(1);
        var doneBtn = page.Locator(".application-counters__btn").Nth(2);
        var allN = int.Parse((await allBtn.Locator(".application-counters__n").InnerTextAsync()).Trim());
        var runningN = int.Parse((await runningBtn.Locator(".application-counters__n").InnerTextAsync()).Trim());
        var doneN = int.Parse((await doneBtn.Locator(".application-counters__n").InnerTextAsync()).Trim());
        Assert.True(allN >= 1, "expected at least one application");
        Assert.True(runningN >= 1, "expected at least one Loopt nog application");
        Assert.True(doneN >= 1, "expected at least one Afgerond application");

        var allCount = await page.Locator(".application-card, .kb-apps__compact").CountAsync();
        Assert.True(allCount >= runningN);

        await runningBtn.ClickAsync();
        await Assertions.Expect(page.Locator(".application-card, .kb-apps__compact")).ToHaveCountAsync(runningN, new() { Timeout = 5_000 });
        await allBtn.ClickAsync();
        await Assertions.Expect(page.Locator(".application-card, .kb-apps__compact")).ToHaveCountAsync(allCount, new() { Timeout = 5_000 });

        await doneBtn.ClickAsync();
        await Assertions.Expect(page.Locator(".application-card, .kb-apps__compact")).ToHaveCountAsync(doneN, new() { Timeout = 5_000 });

        // Hired is in Afgerond (done).
        var hiredCard = page.Locator(".application-card--hired, .application-card:has(.application-card__pill--hired)").First;
        try
        {
            await hiredCard.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
            var statuses = await page.Locator(".application-card__pill").AllInnerTextsAsync();
            Assert.Fail("expected a hired card in Afgerond; pills=" + string.Join(" | ", statuses));
        }

        // Back to full list for Pending menu / withdraw flows.
        await allBtn.ClickAsync();
        await Assertions.Expect(page.Locator(".application-card, .kb-apps__compact")).ToHaveCountAsync(allCount, new() { Timeout = 5_000 });

        // Menu: open / Esc
        var pendingCard = page.Locator(".application-card").Filter(new()
        {
            Has = page.Locator(".application-card__pill--pending")
        }).First;
        var menuToggle = pendingCard.Locator(".application-card__menu-toggle");
        await menuToggle.ClickAsync();
        await Assertions.Expect(menuToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menuToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(menuToggle).ToBeFocusedAsync();

        // Vacature bekijken
        await menuToggle.ClickAsync();
        await pendingCard.Locator(".application-card__menu-item", new() { HasText = "Vacature" }).ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("/vacancies/", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        await page.GoBackAsync();
        await page.Locator(".application-counters").WaitForAsync(new() { Timeout = 30_000 });

        // CV download (or finishes without error message)
        pendingCard = page.Locator(".application-card").Filter(new()
        {
            Has = page.Locator(".application-card__pill--pending")
        }).First;
        menuToggle = pendingCard.Locator(".application-card__menu-toggle");
        await menuToggle.ClickAsync();
        try
        {
            var downloadTask = page.WaitForDownloadAsync(new() { Timeout = 15_000 });
            await pendingCard.Locator(".application-card__menu-item", new() { HasText = "CV" }).ClickAsync();
            _ = await downloadTask;
        }
        catch (TimeoutException)
        {
            // Some environments stream inline; just ensure no error banner.
            Assert.Equal(0, await page.Locator(".apps-page__message").CountAsync());
        }

        // Withdraw confirm cancel then confirm
        // Counters: [0]=Alles, [1]=Loopt nog, [2]=Afgerond
        var runningBefore = int.Parse((await page.Locator(".application-counters__btn").Nth(1)
            .Locator(".application-counters__n").InnerTextAsync()).Trim());
        var doneBefore = int.Parse((await page.Locator(".application-counters__btn").Nth(2)
            .Locator(".application-counters__n").InnerTextAsync()).Trim());

        pendingCard = page.Locator(".application-card").Filter(new()
        {
            Has = page.Locator(".application-card__pill--pending")
        }).First;
        await pendingCard.Locator(".application-card__menu-toggle").ClickAsync();
        await pendingCard.Locator(".application-card__menu-item--danger").ClickAsync();
        await Assertions.Expect(page.Locator(".lobsy-dialog")).ToBeVisibleAsync();
        await page.Locator(".application-withdraw-actions .btn-compact").First.ClickAsync(); // cancel
        await Assertions.Expect(page.Locator(".lobsy-dialog")).ToBeHiddenAsync(new() { Timeout = 5_000 });

        await pendingCard.Locator(".application-card__menu-toggle").ClickAsync();
        await pendingCard.Locator(".application-card__menu-item--danger").ClickAsync();
        await page.Locator(".application-withdraw-actions__danger").ClickAsync();
        await Assertions.Expect(page.Locator(".apps-page__message")).ToContainTextAsync(
            new Regex("ingetrokken|withdrawn|wycofa", RegexOptions.IgnoreCase),
            new() { Timeout = 15_000 });

        var runningAfter = int.Parse((await page.Locator(".application-counters__btn").Nth(1)
            .Locator(".application-counters__n").InnerTextAsync()).Trim());
        var doneAfter = int.Parse((await page.Locator(".application-counters__btn").Nth(2)
            .Locator(".application-counters__n").InnerTextAsync()).Trim());
        Assert.Equal(runningBefore - 1, runningAfter);
        Assert.Equal(doneBefore + 1, doneAfter);

        // Withdraw absent on Hired.
        await allBtn.ClickAsync();

        hiredCard = page.Locator(".application-card--hired, .application-card:has(.application-card__pill--hired)").First;
        await hiredCard.Locator(".application-card__menu-toggle").ClickAsync();
        Assert.Equal(0, await hiredCard.Locator(".application-card__menu-item--danger").CountAsync());
        await page.Keyboard.PressAsync("Escape");

        // Menu open overflow
        await page.Locator(".application-card").First.Locator(".application-card__menu-toggle").ClickAsync();
        await AssertNoOverflowAsync(page, width);
        await page.Keyboard.PressAsync("Escape");

        await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "02-after-withdraw.png"), FullPage = true });

        var fatalPageErrors = pageErrors.Where(e =>
            !e.Contains("Maximum call stack size exceeded", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(fatalPageErrors.Count == 0, "pageerror: " + string.Join(" | ", fatalPageErrors));
        Assert.True(consoleErrors.IsEmpty, "console error: " + string.Join(" | ", consoleErrors));
    }

    private static async Task AssertNoOverflowAsync(IPage page, int viewportWidth)
    {
        var scrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
        Assert.True(scrollWidth <= viewportWidth + 1, $"horizontal overflow: scrollWidth={scrollWidth} viewport={viewportWidth}");
        var cardOverflow = await page.EvaluateAsync<bool>("""
            () => {
              const vw = window.innerWidth;
              return [...document.querySelectorAll('.application-card')].some(el => {
                const r = el.getBoundingClientRect();
                return r.right > vw + 1 || r.left < -1;
              });
            }
            """);
        Assert.False(cardOverflow, "an application card exceeds the viewport");
    }

    private static bool IsIgnoredConsole(string text)
        => text.Contains("favicon", StringComparison.OrdinalIgnoreCase)
           || text.Contains("WebSocket closed", StringComparison.OrdinalIgnoreCase)
           || text.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase)
           || text.Contains("net::ERR_", StringComparison.OrdinalIgnoreCase)
           || text.Contains("openfreemap.org", StringComparison.OrdinalIgnoreCase)
           || text.Contains("AJAXError", StringComparison.OrdinalIgnoreCase)
           || text.Contains("maplibre", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 30_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            var submit = page.Locator("button.login-submit");
            await submit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await page.WaitForFunctionAsync(
                "() => { const b = document.querySelector('button.login-submit'); return b && !b.disabled; }",
                null,
                new() { Timeout = 30_000 });
            await Task.WhenAll(
                page.WaitForURLAsync(
                    url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 }),
                submit.ClickAsync());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
