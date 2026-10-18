using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingSoftIllustrationsPlaywrightTests
{
    private const string EvidenceRoot = "/cursor/stores/self/steps";

    [Fact]
    public async Task Soft_illustrations_layout_overlap_and_capture_when_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_ILLUSTRATIONS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_ILLUSTRATIONS") ?? "").Trim() == "1";
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(EvidenceRoot);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureViewportFlowAsync(browser, baseUrl, capture, 1440, 900, 1, false);
        await CaptureViewportFlowAsync(browser, baseUrl, capture, 390, 844, 2, true);
        await CaptureViewportFlowAsync(browser, baseUrl, capture, 360, 800, 2, true);
    }

    private static async Task CaptureViewportFlowAsync(
        IBrowser browser,
        string baseUrl,
        bool capture,
        int width,
        int height,
        int scale,
        bool mobile)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            DeviceScaleFactor = scale,
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndBeginWizardAsync(page, baseUrl);

        await AssertAndMaybeCaptureAsync(page, capture, mobile, width, 1, "kennismaken", null);
        await AdvanceFromStepAsync(page, 1);
        await AssertAndMaybeCaptureAsync(page, capture, mobile, width, 2, "beschikbaarheid", null);
        await AdvanceFromStepAsync(page, 2);
        await AssertAndMaybeCaptureAsync(page, capture, mobile, width, 3, "vervoer", null);
        await AdvanceFromStepAsync(page, 3);
        await AssertAndMaybeCaptureAsync(page, capture, mobile, width, 4, "werkervaring-ingevuld", FillWorkHistorySampleAsync);
        await AdvanceFromStepAsync(page, 4);
        await AssertAndMaybeCaptureAsync(page, capture, mobile, width, 5, "opleiding-dropdown", OpenEducationDirectionAsync);
    }

    private static async Task AssertAndMaybeCaptureAsync(
        IPage page,
        bool capture,
        bool mobile,
        int width,
        int stepNumber,
        string slug,
        Func<IPage, Task>? prepare)
    {
        await page.Locator($"[data-testid=onboarding-step-{stepNumber}-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
        if (prepare is not null)
        {
            await prepare(page);
        }

        await page.Locator("[data-testid=onboarding-illustration-art]").WaitForAsync(new() { Timeout = 60_000 });
        await AssertNoFormFigureOverlapAsync(page);
        if (stepNumber == 4 && mobile && width <= 390)
        {
            await AssertWorkHistoryDutiesGapAsync(page);
        }

        if (capture)
        {
            var suffix = mobile ? $"mobiel-{width}" : $"desktop-{width}";
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(EvidenceRoot, $"{stepNumber:D2}-{slug}-{suffix}.png"),
                FullPage = false,
            });
        }
    }

    private static async Task AssertNoFormFigureOverlapAsync(IPage page)
    {
        var audit = await page.EvaluateAsync<OverlapAudit>(
            """
            () => {
              const art = document.querySelector('[data-testid=onboarding-illustration-art]');
              const form = document.querySelector('.ob-illustrated-step__form');
              if (!art || !form) {
                return { ok: false, reason: 'missing-art-or-form' };
              }
              const a = art.getBoundingClientRect();
              const f = form.getBoundingClientRect();
              const stageW = a.width || window.innerWidth;
              const sideBand = Math.max(72, stageW * 0.19);
              const leftBand = { left: a.left, right: a.left + sideBand, top: a.top, bottom: a.bottom };
              const rightBand = {
                left: a.right - sideBand,
                right: a.right,
                top: a.top,
                bottom: a.bottom
              };
              function intersects(x, y) {
                return !(f.right <= x.left || f.left >= x.right || f.bottom <= x.top || f.top >= y.bottom);
              }
              if (intersects(leftBand, leftBand) || intersects(rightBand, rightBand)) {
                return { ok: false, reason: 'form-figure-overlap' };
              }
              const mascot = document.querySelector('.ob-wizard__mascot-anchor img');
              if (mascot) {
                const m = mascot.getBoundingClientRect();
                const overlapMascot = !(f.right <= m.left || f.left >= m.right || f.bottom <= m.top || f.top >= m.bottom);
                if (overlapMascot) {
                  return { ok: false, reason: 'mascot-overlap' };
                }
              }
              return { ok: true, reason: '' };
            }
            """);

        Assert.True(audit.Ok, $"Illustration overlap: {audit.Reason}");
    }

    private sealed class OverlapAudit
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
    }

    private static async Task RegisterAndBeginWizardAsync(IPage page, string baseUrl)
    {
        var email = $"illus.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Illus");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code), "Expected OTP from E2E mail stub.");
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        if (!page.Url.Contains("/candidate/start", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        }

        await page.Locator(".ob-wizard__welcome, [data-testid^=onboarding-step-]").First.WaitForAsync(new() { Timeout = 90_000 });
        if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Beginnen" }).ClickAsync(new() { Force = true });
        }

        await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
    }

    private static async Task AdvanceFromStepAsync(IPage page, int fromStep)
    {
        switch (fromStep)
        {
            case 1:
                await page.Locator(".ob-fields input").First.FillAsync("Illus");
                await page.Locator(".ob-fields input").Nth(1).FillAsync("Test");
                await page.Locator("#onboarding-home-address").FillAsync("2671GB");
                await page.WaitForTimeoutAsync(500);
                var suggestion = page.Locator(".kb-address__suggest button, .address-suggestions button").First;
                if (await suggestion.CountAsync() > 0)
                {
                    await suggestion.ClickAsync();
                }

                var birthText = page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").First;
                if (await birthText.CountAsync() > 0)
                {
                    await birthText.FillAsync("15-06-2000");
                }
                else
                {
                    var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
                    if (await birth.CountAsync() > 0)
                    {
                        await birth.FillAsync("2000-06-15");
                    }
                }

                break;
            case 2:
                await page.Locator(".ob-wizard__presets .lobsy-choice-tile, .ob-wizard__presets button").First.ClickAsync();
                break;
            case 3:
                await page.Locator("#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button").First.ClickAsync();
                await page.Locator(".ob-wizard__travel-times .lobsy-choice-tile, .ob-wizard__transport button")
                    .Filter(new() { HasText = "30" }).First.ClickAsync();
                break;
            case 4:
                break;
        }

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForTimeoutAsync(800);

        if (fromStep == 3 && await page.Locator("[data-testid=early-reward-nearby]").CountAsync() > 0)
        {
            await page.GetByTestId("onboarding-next").ClickAsync();
            await page.WaitForTimeoutAsync(800);
        }
    }

    private static async Task FillWorkHistorySampleAsync(IPage page)
    {
        var job = page.Locator("[data-testid=onboarding-work-job]").First;
        await job.Locator("input").First.FillAsync("Verkoopmedewerker");
        await job.Locator("input").Nth(1).FillAsync("Bakkerij Jansen");
        await job.Locator(".ob-job-dates__current input[type=checkbox]").CheckAsync();
        await job.Locator(".ob-job-duties textarea").FillAsync("Brood en taart verkopen, klanten helpen.");
    }

    private static async Task AssertWorkHistoryDutiesGapAsync(IPage page)
    {
        var gap = await page.EvaluateAsync<double>(
            """
            () => {
              const duties = document.querySelector('.ob-job-duties');
              const current = document.querySelector('.ob-job-dates__current');
              if (!duties || !current) return -1;
              const d = duties.getBoundingClientRect();
              const c = current.getBoundingClientRect();
              return d.top - c.bottom;
            }
            """);
        Assert.True(gap >= 11.5, $"Expected ≥12px gap between current-job checkbox and duties label, got {gap:F1}px");
    }

    private static async Task OpenEducationDirectionAsync(IPage page)
    {
        var hbo = page.Locator(".ob-wizard__education-grid .lobsy-choice-tile, .ob-wizard__education-grid button")
            .Filter(new() { HasText = "HBO" });
        await hbo.First.ClickAsync();
        await page.WaitForTimeoutAsync(400);
    }
}
