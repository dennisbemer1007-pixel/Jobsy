using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Run capture with LIVEGANG_SHOTS=1, LIVEGANG_PHASE=before|after, LIVEGANG_SHOTS_SCOPE=vacatures-actions.
/// Layout assertion runs when JOBSY_E2E_BASE_URL is set.
/// </summary>
[Collection("PlaywrightSmoke")]
public class VacaturesActionsLayoutTests
{
    private const string EmployerEmail = "ondernemer@jobsy.local";
    private const string Password = "Jobsy123!";

    [Fact]
    public async Task Capture_vacatures_actions_evidence_when_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("LIVEGANG_SHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        if (!string.Equals(Environment.GetEnvironmentVariable("LIVEGANG_SHOTS_SCOPE"), "vacatures-actions", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("Stack not reachable at JOBSY_E2E_BASE_URL.");
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl), "Feature flags did not turn on in time.");
        var phase = Environment.GetEnvironmentVariable("LIVEGANG_PHASE") ?? "after";
        var assertLayout = !string.Equals(phase, "before", StringComparison.OrdinalIgnoreCase);
        var root = Environment.GetEnvironmentVariable("LIVEGANG_SHOTS_DIR")
                   ?? $"/cursor/stores/self/vacatures-actions/{phase}";
        Directory.CreateDirectory(root);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await KbE2e.LaunchChromiumAsync();

        foreach (var (tag, width, height, mobile, scale, fullPage) in new[]
                 {
                     ("desktop-1440", 1440, 900, false, 1, false),
                     ("desktop-1280", 1280, 800, false, 1, false),
                     ("mobile-390", 390, 844, true, 1, false),
                     ("mobile-390-2x", 390, 844, true, 2, false),
                 })
        {
            await CaptureViewportAsync(browser, baseUrl, root, tag, width, height, mobile, scale, fullPage, assertLayout);
        }
    }

    [Fact]
    public async Task Vacatures_row_action_buttons_fit_inside_table_card()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl), "Feature flags did not turn on in time.");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await KbE2e.LaunchChromiumAsync();

        foreach (var (width, height) in new[] { (1440, 900), (1280, 800), (390, 844) })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = width < 640,
                HasTouch = width < 640,
                Locale = "nl-NL",
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
            if (width < 640)
            {
                await page.GotoAsync($"{baseUrl}/werkgever",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
                await KbE2e.WaitForStyledAppAsync(page);
            }

            await page.GotoAsync($"{baseUrl}/werkgever/vacatures",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "werkgever.css");
            await page.Locator(".wg-vac .wg-vac__actions, .wg-vac .ent-data-table, .wg-vac .ent-data-table__state").First
                .WaitForAsync(new() { Timeout = 60_000 });

            var scrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
            var clientWidth = await page.EvaluateAsync<int>("() => document.documentElement.clientWidth");
            Assert.True(scrollWidth <= clientWidth + 1,
                $"Viewport {width}x{height}: horizontal page overflow {scrollWidth - clientWidth}px.");

            var json = await page.EvaluateAsync<string>(
                """
                () => {
                  const card = document.querySelector('.wg-vac .wg-card');
                  const cells = document.querySelectorAll('.wg-vac .wg-vac__actions');
                  if (!card || cells.length === 0) {
                    return JSON.stringify({ ok: false, reason: 'missing card or action cells' });
                  }
                  const cardR = card.getBoundingClientRect();
                  const cardRightLimit = cardR.right - 8;
                  const buttons = [];
                  let ok = true;
                  for (const cell of cells) {
                    const btns = cell.querySelectorAll('.wg-vac__actions-inner button, .wg-vac__actions-inner a.btn-compact');
                    for (const btn of btns) {
                      const r = btn.getBoundingClientRect();
                      const style = getComputedStyle(btn);
                      if (style.display === 'none' || style.visibility === 'hidden') continue;
                      const textOk = btn.scrollWidth <= btn.clientWidth + 1;
                      const edgeOk = r.right <= cardRightLimit + 0.5;
                      if (!textOk || !edgeOk) ok = false;
                      buttons.push({
                        label: (btn.textContent || '').trim().slice(0, 40),
                        right: r.right,
                        cardRightLimit,
                        textOk,
                        edgeOk,
                        scrollWidth: btn.scrollWidth,
                        clientWidth: btn.clientWidth
                      });
                    }
                  }
                  return JSON.stringify({ ok, cardRight: cardR.right, buttons });
                }
                """);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("reason", out var reason))
            {
                Assert.Fail($"{width}x{height}: {reason.GetString()}");
            }

            Assert.True(doc.RootElement.GetProperty("ok").GetBoolean(),
                $"{width}x{height}: action button layout failed. Raw: {json}");
        }
    }

    private static async Task CaptureViewportAsync(
        IBrowser browser,
        string baseUrl,
        string root,
        string tag,
        int width,
        int height,
        bool mobile,
        int scale,
        bool fullPage,
        bool assertLayout)
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
        page.SetDefaultTimeout(90_000);
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
        if (mobile)
        {
            await page.GotoAsync($"{baseUrl}/werkgever",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await KbE2e.WaitForStyledAppAsync(page);
        }

        await page.GotoAsync($"{baseUrl}/werkgever/vacatures",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "werkgever.css");
        await page.Locator(".wg-vac, .ent-filter-bar, .wg-page-header").First.WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(800);

        var metricsJson = await page.EvaluateAsync<string>(
            """
            () => {
              const card = document.querySelector('.wg-vac .wg-card');
              const cells = document.querySelectorAll('.wg-vac .wg-vac__actions');
              const scrollWidth = document.documentElement.scrollWidth;
              const clientWidth = document.documentElement.clientWidth;
              const cardR = card?.getBoundingClientRect();
              const cardRightLimit = cardR ? cardR.right - 8 : null;
              const rows = [];
              for (const cell of cells) {
                const btns = cell.querySelectorAll('.wg-vac__actions-inner button, .wg-vac__actions-inner a.btn-compact');
                for (const btn of btns) {
                  const r = btn.getBoundingClientRect();
                  rows.push({
                    label: (btn.textContent || '').trim().slice(0, 32),
                    right: Math.round(r.right * 10) / 10,
                    cardRightLimit,
                    textClipped: btn.scrollWidth > btn.clientWidth + 1,
                    pastCard: cardRightLimit != null && r.right > cardRightLimit + 0.5
                  });
                }
              }
              return JSON.stringify({
                scrollWidth,
                clientWidth,
                overflowPx: scrollWidth - clientWidth,
                cardRight: cardR?.right ?? null,
                buttons: rows
              });
            }
            """);

        var metricsFile = Path.Combine(root, $"werkgever-vacatures-{tag}-metrics.json");
        await File.WriteAllTextAsync(metricsFile, metricsJson);

        var file = Path.Combine(root, $"werkgever-vacatures-{tag}.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = fullPage });
        AssertPngWidth(file, width * scale);

        using var doc = System.Text.Json.JsonDocument.Parse(metricsJson);
        if (!assertLayout)
        {
            return;
        }

        var overflow = doc.RootElement.GetProperty("overflowPx").GetInt32();
        Assert.True(overflow <= 1, $"{tag}: page horizontal overflow {overflow}px.");
        foreach (var btn in doc.RootElement.GetProperty("buttons").EnumerateArray())
        {
            Assert.False(btn.GetProperty("textClipped").GetBoolean(),
                $"{tag}: clipped text on '{btn.GetProperty("label").GetString()}'.");
            Assert.False(btn.GetProperty("pastCard").GetBoolean(),
                $"{tag}: '{btn.GetProperty("label").GetString()}' past card edge.");
        }
    }

    private static void AssertPngWidth(string path, int expectedWidth)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24, $"PNG too small: {path}");
        var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        Assert.Equal(expectedWidth, width);
    }
}
