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
    private const string SampleTitle = "Allround Orderpicker";

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
                 })
        {
            await CaptureViewportAsync(browser, baseUrl, root, tag, width, height, mobile, scale, fullPage, assertLayout, baseUrl);
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

            await AssertActionButtonsInsideCardAsync(page, $"{width}x{height}");

            if (width == 1440)
            {
                await AssertTitleSingleLineAsync(page, SampleTitle);
                var menuToggle = page.Locator(".wg-vac .row-actions-menu__toggle").First;
                await menuToggle.ClickAsync();
                await page.Locator(".wg-vac .row-actions-menu.is-open .row-actions-menu__panel")
                    .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
                await AssertRowMenuPanelInsideCardAsync(page);
            }
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
        bool assertLayout,
        string vacaturesUrl)
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

        var metricsJson = await CollectActionMetricsJsonAsync(page);
        var metricsFile = Path.Combine(root, $"werkgever-vacatures-{tag}-metrics.json");
        await File.WriteAllTextAsync(metricsFile, metricsJson);

        var file = Path.Combine(root, $"werkgever-vacatures-{tag}.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = fullPage });
        AssertPngWidth(file, width * scale);

        if (assertLayout)
        {
            await AssertMetricsAsync(tag, metricsJson);
            if (!mobile && width is 1440 or 1280)
            {
                await CaptureConceptTabAsync(page, root, tag, vacaturesUrl);
                await CaptureRowMenuOpenAsync(page, root, tag, vacaturesUrl);
            }
        }
    }

    private static async Task CaptureConceptTabAsync(IPage page, string root, string tag, string vacaturesUrl)
    {
        await page.GotoAsync($"{vacaturesUrl.TrimEnd('/')}/werkgever/vacatures?tab=concept",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".wg-vac .ent-data-table, .wg-vac .ent-data-table__state").First
            .WaitForAsync(new() { Timeout = 60_000 });
        var afmaken = page.Locator(".wg-vac__actions-inner a.btn-compact, .wg-vac__actions-inner button.btn-compact")
            .Filter(new() { HasText = "Afmaken" })
            .First;
        await afmaken.WaitForAsync(new() { Timeout = 30_000 });
        var metrics = await CollectActionMetricsJsonAsync(page);
        await File.WriteAllTextAsync(Path.Combine(root, $"werkgever-vacatures-{tag}-concept-metrics.json"), metrics);
        await AssertMetricsAsync($"{tag}-concept", metrics);
        var afmakenTextOk = await afmaken.EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1");
        Assert.True(afmakenTextOk, $"{tag}-concept: Afmaken label clipped.");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(root, $"werkgever-vacatures-{tag}-concept.png"),
            FullPage = false
        });
    }

    private static async Task CaptureRowMenuOpenAsync(IPage page, string root, string tag, string vacaturesUrl)
    {
        await page.GotoAsync($"{vacaturesUrl.TrimEnd('/')}/werkgever/vacatures",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".wg-vac .ent-data-table tbody tr").First.WaitForAsync(new() { Timeout = 60_000 });
        var toggle = page.Locator(".wg-vac .row-actions-menu__toggle").First;
        await toggle.ClickAsync();
        var panel = page.Locator(".wg-vac .row-actions-menu.is-open .row-actions-menu__panel").First;
        await panel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        await AssertRowMenuPanelInsideCardAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(root, $"werkgever-vacatures-{tag}-row-menu-open.png"),
            FullPage = false
        });
    }

    private static async Task AssertActionButtonsInsideCardAsync(IPage page, string label)
    {
        var json = await CollectActionMetricsJsonAsync(page);
        await AssertMetricsAsync(label, json);
    }

    private static async Task AssertMetricsAsync(string tag, string metricsJson)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(metricsJson);
        if (doc.RootElement.TryGetProperty("reason", out var reason))
        {
            Assert.Fail($"{tag}: {reason.GetString()}");
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

    private static async Task AssertTitleSingleLineAsync(IPage page, string title)
    {
        var titleLink = page.Locator(".wg-vac__title").Filter(new() { HasText = title }).First;
        await titleLink.WaitForAsync(new() { Timeout = 30_000 });
        var singleLine = await titleLink.EvaluateAsync<bool>(
            """
            el => {
              const style = getComputedStyle(el);
              const lineHeight = parseFloat(style.lineHeight) || 20;
              return el.scrollHeight <= lineHeight * 1.35;
            }
            """);
        Assert.True(singleLine, $"Expected '{title}' title on one line at 1440px.");
    }

    private static async Task AssertRowMenuPanelInsideCardAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(
            """
            () => {
              const card = document.querySelector('.wg-vac .wg-card');
              const panel = document.querySelector('.wg-vac .row-actions-menu.is-open .row-actions-menu__panel')
                || document.querySelector('.wg-vac .row-actions-menu__panel');
              if (!card || !panel) return JSON.stringify({ ok: false, reason: 'missing card or menu panel' });
              const cardR = card.getBoundingClientRect();
              const panelR = panel.getBoundingClientRect();
              const limit = cardR.right - 8;
              return JSON.stringify({
                ok: panelR.right <= limit + 0.5 && panelR.left >= cardR.left + 4,
                panelRight: panelR.right,
                cardRightLimit: limit
              });
            }
            """);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("reason", out var reason))
        {
            Assert.Fail(reason.GetString());
        }

        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean(), $"Row menu panel clipped by card. Raw: {json}");
    }

    private static async Task<string> CollectActionMetricsJsonAsync(IPage page)
        => await page.EvaluateAsync<string>(
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
                  const style = getComputedStyle(btn);
                  if (style.display === 'none' || style.visibility === 'hidden') continue;
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

    private static void AssertPngWidth(string path, int expectedWidth)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24, $"PNG too small: {path}");
        var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        Assert.Equal(expectedWidth, width);
    }
}
