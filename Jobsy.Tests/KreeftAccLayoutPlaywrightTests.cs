using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using Xunit;

namespace Jobsy.Tests;

/// <summary>
/// Layout gate for kreeft acc fixes (follow-up #1093 @ 8f181cf). Targets acceptatie by default.
/// Run: JOBSY_KREEFT_ACC_LAYOUT=1 dotnet test --filter Kreeft_acc_layout
/// Optional: JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class KreeftAccLayoutPlaywrightTests
{
    private const string Password = KbE2e.DefaultPassword;

    [Fact]
    public async Task Kreeft_acc_layout_strip_reiskaart_and_coach()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_KREEFT_ACC_LAYOUT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = await ResolveBaseUrlAsync()
                      ?? throw new InvalidOperationException("JOBSY_E2E_BASE_URL unreachable (default https://acceptatie.lobsy.nl).");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await AssertPublicRouteAsync(browser, baseUrl, "/", "landing");
        await AssertPublicRouteAsync(browser, baseUrl, "/login", "login");
        await AssertAuthRouteAsync(browser, baseUrl, "/candidate/start?step=1", "start");
        await AssertAuthRouteAsync(browser, baseUrl, "/candidate/kompas", "kompas");
        await AssertAuthRouteAsync(browser, baseUrl, "/candidate/paspoort", "paspoort");
        await AssertAuthRouteAsync(browser, baseUrl, "/banenkaart", "banenkaart");
        await AssertAuthRouteAsync(browser, baseUrl, "/carriere", "carriere");
        await AssertPublicRouteAsync(browser, baseUrl, "/status/404", "404");
    }

    private static async Task<string?> ResolveBaseUrlAsync()
    {
        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "https://acceptatie.lobsy.nl";
        if (FunctionalE2eGate.IsRequired)
        {
            return await FunctionalE2eGate.RequireReachableBaseUrlAsync();
        }

        return await KbE2e.IsReachableAsync(baseUrl) ? baseUrl : null;
    }

    private static async Task AssertPublicRouteAsync(IBrowser browser, string baseUrl, string path, string label)
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await AssertViewportsAsync(page, label);
    }

    private static async Task AssertAuthRouteAsync(IBrowser browser, string baseUrl, string path, string label)
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        var page = await context.NewPageAsync();
        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(KbE2e.DefaultCompleteEmail);
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, Password));
        await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await AssertViewportsAsync(page, label);
        if (label is "paspoort" or "banenkaart" or "carriere")
        {
            await AssertCoachCloseAsync(page, label);
        }
    }

    private static async Task AssertViewportsAsync(IPage page, string label)
    {
        foreach (var (width, height, tag) in new[] { (1440, 900, "desk"), (390, 844, "mob") })
        {
            await page.SetViewportSizeAsync(width, height);
            await page.WaitForTimeoutAsync(450);
            await AssertStripAsync(page, width, tag, label);
            if (label is "landing" or "login" or "start" or "kompas" or "paspoort")
            {
                await AssertReiskaartAsync(page, width, tag, label);
            }

            if (label == "kompas")
            {
                await AssertKompasStackAsync(page, tag);
            }

            if (label == "login" && tag == "desk")
            {
                await AssertLoginStripUnderHeaderAsync(page);
            }

            foreach (var img in await page.Locator("[data-testid='kreeft-header-strip'] img, .kreeft-header-strip img").AllAsync())
            {
                var src = await img.GetAttributeAsync("src") ?? "";
                Assert.DoesNotContain("/strips//images/", src, StringComparison.Ordinal);
            }
        }
    }

    private static async Task AssertStripAsync(IPage page, int width, string tag, string label)
    {
        var strip = page.Locator("[data-testid='kreeft-header-strip'], .pub-landing__kreeft-strip, .err-kreeft-strip, .ob-rustig__hero-band, .kb-banenkaart-header-strip").First;
        if (await strip.CountAsync() == 0)
        {
            return;
        }

        var box = await strip.BoundingBoxAsync();
        Assert.NotNull(box);
        if (tag == "desk")
        {
            Assert.InRange(box!.Height, 210, 240);
        }
        else
        {
            Assert.InRange(box!.Height, 95, 115);
        }

        Assert.True(box!.Width >= width * 0.92, $"{label} strip width {box.Width} @ {width}px");
    }

    private static async Task AssertReiskaartAsync(IPage page, int width, string tag, string label)
    {
        var reis = page.Locator("[data-testid='kreeft-reiskaart']");
        if (await reis.CountAsync() == 0)
        {
            return;
        }

        var rb = await reis.First.BoundingBoxAsync();
        Assert.NotNull(rb);
        Assert.True(rb!.Width <= width + 4, $"{label} reiskaart overflow @ {tag}: {rb.Width}px > {width}px");

        if (tag == "mob")
        {
            var copy = page.Locator(".pub-landing__hero-copy h1, .au-card h1, .ob-rustig__body-main h1, .passport-layout__main-flow h1, #kompas-title, .kompas-hub-layout__main h1").First;
            if (await copy.CountAsync() > 0)
            {
                var cb = await copy.BoundingBoxAsync();
                if (cb is not null && rb is not null)
                {
                    Assert.True(rb.Y <= cb.Y + 2 || rb.Y + rb.Height <= cb.Y + 4,
                        $"{label}: reiskaart should sit above main copy on mobile");
                }
            }
        }

        if (tag == "desk" && label is "paspoort" or "start")
        {
            var main = page.Locator(".passport-layout__main-flow, .ob-rustig__body-main").First;
            if (await main.CountAsync() > 0)
            {
                var mb = await main.BoundingBoxAsync();
                if (mb is not null)
                {
                    Assert.True(rb!.X > mb.X + mb.Width * 0.35, $"{label}: reiskaart should be in the right column on desktop");
                }
            }
        }
    }

    private static async Task AssertKompasStackAsync(IPage page, string tag)
    {
        var strip = page.Locator(".kompas-page-wrap [data-testid='kreeft-header-strip'], .kompas-page-wrap .kreeft-header-strip").First;
        var reis = page.Locator("[data-testid='kreeft-reiskaart']").First;
        var title = page.Locator("#kompas-title").First;
        if (await strip.CountAsync() == 0 || await reis.CountAsync() == 0 || await title.CountAsync() == 0)
        {
            return;
        }

        var sb = await strip.BoundingBoxAsync();
        var rb = await reis.BoundingBoxAsync();
        var tb = await title.BoundingBoxAsync();
        Assert.NotNull(sb);
        Assert.NotNull(rb);
        Assert.NotNull(tb);

        if (tag == "mob")
        {
            Assert.InRange(sb!.Y, 40, 120);
            Assert.True(rb!.Y >= sb.Y + sb.Height - 4, "kompas mobile: reiskaart should follow header strip");
            Assert.True(tb!.Y >= rb.Y + rb.Height - 4, "kompas mobile: title should follow reiskaart");
            Assert.True(sb.Y < 400, "kompas mobile: strip must not be pushed to page bottom (display:contents regression)");
        }
        else
        {
            var gap = tb!.Y - (sb!.Y + sb.Height);
            Assert.InRange(gap, 18, 32);
        }
    }

    private static async Task AssertLoginStripUnderHeaderAsync(IPage page)
    {
        var header = page.Locator("header.app-header, .app-shell-header, header[data-testid='app-header']").First;
        var strip = page.Locator("[data-testid='kreeft-header-strip']").First;
        if (await header.CountAsync() == 0 || await strip.CountAsync() == 0)
        {
            return;
        }

        var hb = await header.BoundingBoxAsync();
        var sb = await strip.BoundingBoxAsync();
        Assert.NotNull(hb);
        Assert.NotNull(sb);
        var gap = sb!.Y - (hb!.Y + hb.Height);
        Assert.True(gap < 24, $"Login strip gap under header: {gap}px");
    }

    private static async Task AssertCoachCloseAsync(IPage page, string label)
    {
        var openBtn = page.Locator("[data-ssr-coach-open], #lobsy-coach-btn").First;
        if (await openBtn.CountAsync() == 0)
        {
            Assert.Fail($"{label}: Lobsy-hulp dock missing");
        }

        await openBtn.ScrollIntoViewIfNeededAsync();
        await openBtn.ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForFunctionAsync(
            "() => { var b = document.querySelector('[data-ssr-coach-open]'); return b && b.getAttribute('aria-expanded') === 'true'; }",
            null,
            new() { Timeout = 15_000 });
        await page.WaitForFunctionAsync(
            "() => !!document.querySelector('[data-ssr-assistant-close]')",
            null,
            new() { Timeout = 10_000 });
        await page.EvaluateAsync("() => { var c = document.querySelector('[data-ssr-assistant-close]'); if (c) c.click(); }");
        await page.WaitForFunctionAsync(
            "() => { var b = document.querySelector('[data-ssr-coach-open]'); return b && b.getAttribute('aria-expanded') === 'false'; }",
            null,
            new() { Timeout = 10_000 });
    }
}
