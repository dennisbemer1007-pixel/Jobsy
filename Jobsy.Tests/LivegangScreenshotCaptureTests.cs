using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Run with LIVEGANG_SHOTS=1, LIVEGANG_PHASE=before|after, JOBSY_E2E_BASE_URL, ConnectionStrings__JobsyDb.</summary>
[Collection("PlaywrightSmoke")]
public class LivegangScreenshotCaptureTests
{
    private const string CandidateEmail = "kandidaat@jobsy.local";
    private const string EmployerEmail = "ondernemer@jobsy.local";
    private const string Password = "Jobsy123!";
    private static readonly List<string> Network404s = [];

    [Fact]
    public async Task Capture_livegang_evidence_when_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("LIVEGANG_SHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("Stack not reachable at JOBSY_E2E_BASE_URL.");
        var phase = Environment.GetEnvironmentVariable("LIVEGANG_PHASE") ?? "after";
        var root = Environment.GetEnvironmentVariable("LIVEGANG_SHOTS_DIR")
                   ?? $"/cursor/stores/self/livegang-fixes/{phase}";
        Directory.CreateDirectory(root);
        Network404s.Clear();

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl), "Feature flags did not turn on in time.");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await KbE2e.LaunchChromiumAsync();

        var skipNotificationsPage = string.Equals(phase, "before", StringComparison.OrdinalIgnoreCase);

        await CaptureVacancyWizardAsync(browser, baseUrl, root);
        await ClearDemoNotificationsAsync();
        await CaptureNotificationsAsync(browser, baseUrl, root, withItems: false, skipPage: skipNotificationsPage);
        await SeedDemoNotificationsAsync();
        await CaptureNotificationsAsync(browser, baseUrl, root, withItems: true, skipPage: skipNotificationsPage);
        await CaptureVacaturesListAsync(browser, baseUrl, root);

        var networkLog = Path.Combine(root, "network-404s.txt");
        await File.WriteAllLinesAsync(
            networkLog,
            Network404s.Count == 0
                ? ["(none)"]
                : Network404s.Distinct(StringComparer.Ordinal).OrderBy(u => u, StringComparer.Ordinal));
        Assert.Empty(Network404s);
    }

    private static async Task CaptureVacancyWizardAsync(IBrowser browser, string baseUrl, string root)
    {
        foreach (var (tag, width, height, mobile, scale) in new[]
                 {
                     ("desktop-1440", 1440, 900, false, 1),
                     ("mobile-390", 390, 844, true, 1),
                     ("mobile-390-2x", 390, 844, true, 2),
                 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                DeviceScaleFactor = scale,
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            TrackNetwork404s(page);
            page.SetDefaultTimeout(90_000);
            await AssertLoginAsync(page, baseUrl, EmployerEmail);
            if (mobile)
            {
                await page.GotoAsync($"{baseUrl}/werkgever",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
                await KbE2e.WaitForStyledAppAsync(page);
            }

            await page.GotoAsync($"{baseUrl}/werkgever/vacatures/nieuw",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "choice-tiles.css", "vacancy-wizard.css");
            await page.Locator("[data-testid='vacancy-wizard']").WaitForAsync(new() { Timeout = 90_000 });
            await page.Locator("[data-testid='vac-wizard-step-1']").WaitForAsync();
            var picker = page.Locator("[data-testid='vacancy-category-picker'] .lobsy-choice-tile");
            if (await picker.CountAsync() > 0)
            {
                await picker.First.ClickAsync();
                await page.Locator("[data-testid='vacancy-category-picker'] .lobsy-choice-tile.is-selected")
                    .WaitForAsync(new() { Timeout = 15_000 });
            }

            var file = Path.Combine(root, $"vac-wizard-step1-selected-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = false });
            AssertPngWidth(file, width * scale);
        }
    }

    private static async Task CaptureNotificationsAsync(
        IBrowser browser, string baseUrl, string root, bool withItems, bool skipPage = false)
    {
        var suffix = withItems ? "with-items" : "empty";
        foreach (var (role, email) in new[] { ("candidate", CandidateEmail), ("employer", EmployerEmail) })
        {
            foreach (var (tag, width, height, mobile) in new[]
                     {
                         ("mobile-390", 390, 844, true),
                         ("desktop-1440", 1440, 900, false),
                     })
            {
                await using var context = await browser.NewContextAsync(new()
                {
                    ViewportSize = new() { Width = width, Height = height },
                    IsMobile = mobile,
                    HasTouch = mobile,
                    Locale = "nl-NL",
                    IgnoreHTTPSErrors = true
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                var page = await context.NewPageAsync();
                TrackNetwork404s(page);
                await AssertLoginAsync(page, baseUrl, email);
                var home = role == "employer" ? $"{baseUrl}/werkgever/vacatures" : $"{baseUrl}/candidate/applications";
                await page.GotoAsync(home,
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
                await page.WaitForTimeoutAsync(600);

                var bell = page.Locator(".notification-bell__toggle").First;
                await bell.WaitForAsync(new() { Timeout = 30_000 });
                await bell.ClickAsync();
                await page.Locator(".notification-bell__panel").WaitForAsync(new() { State = WaitForSelectorState.Visible });
                if (withItems)
                {
                    await page.Locator(".notification-bell__list .notification-bell__item")
                        .Nth(2)
                        .WaitForAsync(new() { Timeout = 30_000 });
                }

                var bellFile = Path.Combine(root, $"notifications-bell-{suffix}-{role}-{tag}.png");
                await page.ScreenshotAsync(new() { Path = bellFile, FullPage = false });
                AssertPngWidth(bellFile, width);

                if (skipPage)
                {
                    continue;
                }

                await page.GotoAsync($"{baseUrl}/notifications",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
                await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "notifications.css");
                if (withItems)
                {
                    await page.Locator(".notifications-page__list .notifications-page__item")
                        .Nth(2)
                        .WaitForAsync(new() { Timeout = 30_000 });
                    await AssertAndLogNotificationDotAsync(page, root, $"notifications-page-{suffix}-{role}-{tag}");
                }

                var pageFile = Path.Combine(root, $"notifications-page-{suffix}-{role}-{tag}.png");
                await page.ScreenshotAsync(new() { Path = pageFile, FullPage = false });
                AssertPngWidth(pageFile, width);
            }
        }
    }

    private static async Task CaptureVacaturesListAsync(IBrowser browser, string baseUrl, string root)
    {
        foreach (var (tag, width, height, mobile, fullPage) in new[]
                 {
                     ("desktop-1440-full", 1440, 900, false, true),
                     ("mobile-390", 390, 844, true, false),
                 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            TrackNetwork404s(page);
            await AssertLoginAsync(page, baseUrl, EmployerEmail);
            await page.GotoAsync($"{baseUrl}/werkgever/vacatures",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.Locator(".wg-vac, .ent-filter-bar, .wg-page-header").First.WaitForAsync(new() { Timeout = 60_000 });
            await page.WaitForTimeoutAsync(800);

            var scrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
            var clientWidth = await page.EvaluateAsync<int>("() => document.documentElement.clientWidth");
            var metricsFile = Path.Combine(root, $"werkgever-vacatures-{tag}-metrics.txt");
            await File.WriteAllTextAsync(metricsFile,
                $"scrollWidth={scrollWidth}{Environment.NewLine}clientWidth={clientWidth}{Environment.NewLine}overflowPx={scrollWidth - clientWidth}{Environment.NewLine}");

            var file = Path.Combine(root, $"werkgever-vacatures-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = fullPage });
            AssertPngWidth(file, width);
        }
    }

    private static async Task ClearDemoNotificationsAsync()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        foreach (var email in new[] { CandidateEmail, EmployerEmail })
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                continue;
            }

            var rows = await db.UserNotifications.Where(n => n.UserId == user.Id).ToListAsync();
            db.UserNotifications.RemoveRange(rows);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoNotificationsAsync()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var demo = await db.UserNotifications
            .Where(n => n.Title.StartsWith("Livegang demo "))
            .ToListAsync();
        db.UserNotifications.RemoveRange(demo);
        await db.SaveChangesAsync();

        foreach (var email in new[] { CandidateEmail, EmployerEmail })
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                continue;
            }

            for (var i = 1; i <= 3; i++)
            {
                db.UserNotifications.Add(new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Title = $"Livegang demo {i}",
                    Body = "Voorbeeld melding voor screenshot.",
                    Category = "Livegang",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
                });
            }
        }

        await db.SaveChangesAsync();
        await WaitForSeededDemoNotificationsAsync();
    }

    private static async Task WaitForSeededDemoNotificationsAsync()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        for (var i = 0; i < 20; i++)
        {
            await using var db = new JobsyDbContext(options);
            var ok = true;
            foreach (var email in new[] { CandidateEmail, EmployerEmail })
            {
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
                if (user is null)
                {
                    ok = false;
                    break;
                }

                var count = await db.UserNotifications.AsNoTracking()
                    .CountAsync(n => n.UserId == user.Id && n.Title.StartsWith("Livegang demo "));
                if (count < 3)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                return;
            }

            await Task.Delay(250);
        }
    }

    private static async Task AssertAndLogNotificationDotAsync(IPage page, string root, string shotTag)
    {
        var json = await page.EvaluateAsync<string>(
            """
            () => {
              const dot = document.querySelector('.notifications-page__unread-dot');
              const btn = dot?.closest('.notifications-page__item-btn');
              const item = dot?.closest('.notifications-page__item');
              const top = dot?.closest('.notifications-page__item-top');
              const sheets = Array.from(document.styleSheets)
                .map(s => s.href || '')
                .filter(h => h.includes('notifications.css'));
              if (!dot || !btn) {
                return JSON.stringify({ ok: false, reason: 'no dot', sheets });
              }
              const dotStyle = getComputedStyle(dot);
              const topStyle = top ? getComputedStyle(top) : null;
              const dotR = dot.getBoundingClientRect();
              const btnR = btn.getBoundingClientRect();
              const itemR = item?.getBoundingClientRect();
              const gapBtnRight = btnR.right - dotR.right;
              const gapItemRight = itemR ? itemR.right - dotR.right : null;
              return JSON.stringify({
                ok: gapBtnRight <= 20,
                gapBtnRight,
                gapItemRight,
                dotX: dotR.left,
                dotRight: dotR.right,
                btnRight: btnR.right,
                itemRight: itemR?.right ?? null,
                dotPosition: dotStyle.position,
                dotTop: dotStyle.top,
                dotRightCss: dotStyle.right,
                topDisplay: topStyle?.display ?? null,
                topWidth: topStyle?.width ?? null,
                notificationsCssHrefs: sheets
              });
            }
            """);
        var logPath = Path.Combine(root, "notifications-dot-computed-styles.txt");
        await File.AppendAllTextAsync(logPath, $"[{shotTag}]{Environment.NewLine}{json}{Environment.NewLine}{Environment.NewLine}");

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var gap = doc.RootElement.GetProperty("gapBtnRight").GetDouble();
        Assert.True(gap <= 20, $"{shotTag}: unread dot gap to card inner right is {gap}px (max 20). Raw: {json}");
        Assert.Equal("absolute", doc.RootElement.GetProperty("dotPosition").GetString());
        var sheets = doc.RootElement.GetProperty("notificationsCssHrefs");
        Assert.True(sheets.GetArrayLength() > 0, $"{shotTag}: notifications.css not loaded.");
    }

    private static void TrackNetwork404s(IPage page)
    {
        page.Response += (_, response) =>
        {
            if (response.Status != 404)
            {
                return;
            }

            var url = response.Url;
            if (url.Contains("/css/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("/js/", StringComparison.OrdinalIgnoreCase))
            {
                Network404s.Add(url);
            }
        };
    }

    private static void AssertPngWidth(string path, int expectedWidth)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24, $"PNG too small: {path}");
        var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        Assert.Equal(expectedWidth, width);
    }

    private static async Task AssertLoginAsync(IPage page, string baseUrl, string email)
    {
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            if (await KbE2e.TryLoginAsync(page, baseUrl, email, Password))
            {
                return;
            }

            await page.WaitForTimeoutAsync(1500 * attempt);
        }

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, Password));
    }
}
