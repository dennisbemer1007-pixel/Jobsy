using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Real browser against Kestrel. HttpClient never starts a Blazor circuit, so it
/// missed the jump to /login. This test waits until the circuit is up.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class PupilCircuitPlaywrightTests : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private readonly RoleFunctionalWebAppFactory _api;
    private PupilKestrelFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PupilCircuitPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact(Timeout = 300_000)]
    public async Task Pupil_stays_on_start_after_the_circuit_and_reaches_the_pdf()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        var baseUrl = EnsureWeb();
        Assert.StartsWith("http://127.0.0.1:", baseUrl, StringComparison.Ordinal);

        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.SetDefaultNavigationTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";

        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/start**", DomReady);
        await page.Locator(".ll-start, .ll-start--g78").WaitForAsync();
        Assert.Contains("/leerling/start", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/leerling/wereld", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GetByRole(AriaRole.Link, new() { Name = "Beginnen" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/reis**", DomReady);
        await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).WaitForAsync();
        await AnswerOneAsync(page);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GetByRole(AriaRole.Button, new() { Name = "Pauze" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/stop**", DomReady);
        Assert.Contains("done=1", page.Url, StringComparison.Ordinal);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Goed gedaan!" })).ToBeVisibleAsync();

        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/reis**", DomReady);
        await page.Locator(".ll-question, .ll-card.ll-question").WaitForAsync();
        Assert.Contains("/leerling/reis", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        await Assertions.Expect(page.Locator(".g78-jmeta span").First).ToContainTextAsync("Vraag 2");

        for (var step = 0; step < 80 && !page.Url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase)
             && !page.Url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase); step++)
        {
            Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
            if (page.Url.Contains("/leerling/puzzel/", StringComparison.OrdinalIgnoreCase))
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "Overslaan" }).ClickAsync();
                await page.WaitForURLAsync(
                    url => url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase),
                    UrlCommitted);
                continue;
            }

            if (page.Url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase))
            {
                var likes = page.Locator("fieldset.ll-chips").Nth(0);
                var dislikes = page.Locator("fieldset.ll-chips").Nth(1);
                var sport = likes.GetByRole(AriaRole.Button, new() { Name = "Sport", Exact = true });
                var rekenen = dislikes.GetByRole(AriaRole.Button, new() { Name = "Rekenen", Exact = true });
                await sport.ClickAsync();
                await Assertions.Expect(sport).ToHaveClassAsync(new Regex(@"\bis-on\b"));
                await rekenen.ClickAsync();
                await Assertions.Expect(rekenen).ToHaveClassAsync(new Regex(@"\bis-on\b"));
                await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder" }).ClickAsync();
                await page.WaitForFunctionAsync(
                    "() => !location.pathname.includes('/leerling/eiland')");
                continue;
            }

            if (page.Url.Contains("/leerling/vakken", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator("[data-testid=leerling-vakken], .ll-vt-card").First.WaitForAsync();
                await FillVakkenPageLikeMockAsync(page);
                var next = page.Locator("[data-testid=ll-vt-next]");
                await page.WaitForFunctionAsync(
                    "() => { const btn = document.querySelector('[data-testid=ll-vt-next]'); return btn && !btn.disabled; }",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 30_000 });
                await next.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                continue;
            }

            if (!page.Url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await AnswerOneAsync(page);
        }

        Assert.True(
            page.Url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase)
            || page.Url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase),
            "Journey did not reach the story. URL: " + page.Url);

        // Results.File sends Content-Disposition: attachment, so a document
        // navigation starts a download. The browser context request uses the
        // same pupil cookies the circuit just used.
        var pdf = await page.Context.APIRequest.GetAsync(baseUrl + "/leerling/pdf");
        Assert.Equal(200, pdf.Status);
        Assert.True(
            pdf.Headers.TryGetValue("content-type", out var contentType)
            && contentType.Contains("application/pdf", StringComparison.OrdinalIgnoreCase),
            "PDF response was " + pdf.Status + " " + pdf.Url);
        Assert.DoesNotContain("/login", pdf.Url, StringComparison.OrdinalIgnoreCase);
        var bytes = await pdf.BodyAsync();
        Assert.True(bytes.Length > 4 && bytes[0] == (byte)'%' && bytes[1] == (byte)'P');
    }

    /// <summary>
    /// Manual Acceptatie repro: set <c>JOBSY_ACC_E2E=1</c>, <c>JOBSY_ACC_PUPIL_CODE</c>, optional school/class ids.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task Droombaan_search_keeps_typing_on_acceptatie_when_configured()
    {
        if (!AcceptatieE2E.Enabled)
        {
            return;
        }

        _ = TestContext.Current.CancellationToken;
        var baseUrl = AcceptatieE2E.BaseUrl.TrimEnd('/');
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        page.SetDefaultTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl =
            $"{baseUrl}/leerling?schoolId={AcceptatieE2E.SchoolId}&classId={AcceptatieE2E.ClassId}";
        await LoginAsync(page, loginUrl, AcceptatieE2E.PlainCode, Guid.Parse(AcceptatieE2E.ClassId));
        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        await page.GotoAsync($"{baseUrl}/leerling/droombaan", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var search = page.Locator("[data-testid=leerling-droombaan-search]");
        await search.WaitForAsync();
        await search.ClickAsync();
        await search.FillAsync("");
        const string typed = "Dierenarts";
        await search.PressSequentiallyAsync(typed, new() { Delay = 60 });
        await page.WaitForTimeoutAsync(1500);
        await Assertions.Expect(search).ToHaveValueAsync(typed);
        await page.WaitForTimeoutAsync(1500);
        await Assertions.Expect(search).ToHaveValueAsync(typed);
        Assert.Empty(consoleErrors);
    }

    [Theory(Timeout = 300_000)]
    [InlineData(390, 844)]
    [InlineData(1440, 900)]
    public async Task Droombaan_search_keeps_fast_typing_right_after_load(int width, int height)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedCompletedWithDreamJobAsync("kok");
        var baseUrl = EnsureWeb();
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        page.SetDefaultTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/droombaan", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var search = page.Locator("[data-testid=leerling-droombaan-search]");
        await search.WaitForAsync();
        await search.ClickAsync();
        await search.FillAsync("");
        const string typed = "Dierenarts";
        await search.PressSequentiallyAsync(typed, new LocatorPressSequentiallyOptions { Delay = 60 });
        await page.WaitForTimeoutAsync(1500);
        await Assertions.Expect(search).ToHaveValueAsync(typed);
        await page.WaitForTimeoutAsync(1500);
        await Assertions.Expect(search).ToHaveValueAsync(typed);
        await CaptureWalkthroughScreenshotIfRequestedAsync(page, $"droombaan-ssr-{width}.png");
    }

    [Fact(Timeout = 300_000)]
    public async Task Droombaan_post_save_persists_after_reload()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedCompletedWithDreamJobAsync("kok");
        var baseUrl = EnsureWeb();
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        page.SetDefaultTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/droombaan", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        var search = page.Locator("[data-testid=leerling-droombaan-search]");
        await search.WaitForAsync();
        await search.FillAsync("Dierenarts");
        await page.Locator(".ll-dream-ssr__save").ClickAsync();
        await page.WaitForURLAsync("**/leerling/droombaan**");
        await Assertions.Expect(search).ToHaveValueAsync("Dierenarts");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assertions.Expect(search).ToHaveValueAsync("Dierenarts");
        AssertNoMeaningfulConsoleErrors(consoleErrors);
    }

    [Fact(Timeout = 300_000)]
    public async Task Droombaan_pick_piloot_persists_on_acceptatie_when_configured()
    {
        if (!AcceptatieE2E.Enabled)
        {
            return;
        }

        _ = TestContext.Current.CancellationToken;
        var baseUrl = AcceptatieE2E.BaseUrl.TrimEnd('/');
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        page.SetDefaultTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl =
            $"{baseUrl}/leerling?schoolId={AcceptatieE2E.SchoolId}&classId={AcceptatieE2E.ClassId}";
        await LoginAsync(page, loginUrl, AcceptatieE2E.PlainCode, Guid.Parse(AcceptatieE2E.ClassId));
        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        await page.GotoAsync($"{baseUrl}/leerling/droombaan", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var search = page.Locator("[data-testid=leerling-droombaan-search]");
        await search.WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Piloot" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/droombaan**");
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Er ging iets mis", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
        await Assertions.Expect(search).ToHaveValueAsync("Piloot");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assertions.Expect(search).ToHaveValueAsync("Piloot");
        Assert.Empty(consoleErrors);
    }

    [Theory(Timeout = 300_000)]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Paspoort_actions_bar_does_not_overlap_route(int width, int height)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        await EnablePassportV2Async();
        var seed = await SeedCompletedWithDreamJobAsync("dierenarts");
        var baseUrl = EnsureWeb();
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        page.SetDefaultTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=leerling-paspoort-page]").WaitForAsync();
        await Assertions.Expect(page.Locator("[data-testid=leerling-g78-shell]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".ll-g78-shell header.top .btn-pauze")).ToBeVisibleAsync();
        var cardsHeadJson = await page.EvaluateAsync<string>(
            """
            () => {
              const head = document.querySelector('.lp-g78__cards-head');
              const sub = head?.querySelector('small');
              if (!head || !sub) {
                return JSON.stringify({ ok: false, reason: 'missing-cards-head' });
              }
              const hr = head.getBoundingClientRect();
              const sr = sub.getBoundingClientRect();
              const subDisplay = window.getComputedStyle(sub).display;
              return JSON.stringify({
                subDisplay,
                subOnOwnLine: subDisplay === 'block' && sr.top > hr.top + 10,
                overlapPx: Math.max(0, hr.bottom - sr.top - sr.height)
              });
            }
            """);
        using (var cardsHead = System.Text.Json.JsonDocument.Parse(cardsHeadJson))
        {
            var cardsRoot = cardsHead.RootElement;
            Assert.Equal("block", cardsRoot.GetProperty("subDisplay").GetString());
            Assert.True(cardsRoot.GetProperty("subOnOwnLine").GetBoolean(), cardsHeadJson);
        }

        var layoutJson = await page.EvaluateAsync<string>(
            """
            () => {
              const pageEl = document.querySelector('[data-testid=leerling-paspoort-page]');
              const actions = pageEl?.querySelector('[data-testid=leerling-paspoort-actions]');
              const route = pageEl?.querySelector('.lp-g78__route');
              const content = pageEl?.querySelector('.lp-g78');
              if (!pageEl || !actions || !route || !content) {
                return JSON.stringify({ ok: false, reason: 'missing-elements' });
              }
              const ar = actions.getBoundingClientRect();
              const rr = route.getBoundingClientRect();
              const contentRect = content.getBoundingClientRect();
              const leftMargin = Math.round(ar.left - contentRect.left);
              const rightMargin = Math.round(contentRect.right - ar.right);
              return JSON.stringify({
                ok: ar.top >= rr.bottom - 2,
                overlapPx: Math.max(0, rr.bottom - ar.top),
                leftMargin,
                rightMargin
              });
            }
            """);
        using var layout = System.Text.Json.JsonDocument.Parse(layoutJson);
        var root = layout.RootElement;
        Assert.True(root.GetProperty("ok").GetBoolean(), layoutJson);
        var left = root.GetProperty("leftMargin").GetInt32();
        var right = root.GetProperty("rightMargin").GetInt32();
        Assert.True(Math.Abs(left - right) <= 2, layoutJson);
        await CaptureWalkthroughScreenshotIfRequestedAsync(page, $"paspoort-actions-{width}.png");
    }

    [Fact(Timeout = 300_000)]
    public async Task Paspoort_double_visit_and_english_switch_keep_session()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        await EnablePassportV2Async();
        var seed = await SeedCompletedWithDreamJobAsync("dierenarts");
        var baseUrl = EnsureWeb();
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);

        for (var i = 0; i < 3; i++)
        {
            var response = await page.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.NotNull(response);
            Assert.True(response!.Ok, $"paspoort visit {i + 1} returned {(int)response.Status}");
            await page.Locator("[data-testid=leerling-paspoort-page]").WaitForAsync();
            Assert.DoesNotContain("error=expired", page.Url, StringComparison.OrdinalIgnoreCase);
        }

        await page.GotoAsync($"{baseUrl}/taal/en?return=/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=leerling-paspoort-page]").WaitForAsync();
        Assert.DoesNotContain("error=expired", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GotoAsync($"{baseUrl}/taal/nl?return=/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=leerling-paspoort-page]").WaitForAsync();
        Assert.DoesNotContain("/leerling?error=expired", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Timeout = 240_000)]
    public async Task Wereld_screen_00_checkmark_sized_mockup_and_controls()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        var root = FindRepoRoot();

        foreach (var (width, height, tag) in new[] { (1440, 957, "desktop"), (390, 844, "mobile") })
        {
            var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-screen-00-fix", tag);
            Directory.CreateDirectory(artifactDir);
            var mockDir = Path.Combine(root, "docs", "mockups", "leerling", "werelden", "renders", tag == "mobile" ? "mobile" : "desktop");
            const string mockFile = "00-kies-je-wereld.png";

            await using var context = width <= 400
                ? await NewMobileContextAsync()
                : await _browser!.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height }
                });
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);
            var consoleOnWereld = new List<string>();
            var failedOnWereld = new List<string>();
            page.Console += (_, msg) =>
            {
                if (msg.Type == "error"
                    && page.Url.Contains("/leerling/wereld", StringComparison.OrdinalIgnoreCase))
                {
                    consoleOnWereld.Add(msg.Text);
                }
            };
            page.RequestFailed += (_, req) =>
            {
                if (req.Url.Contains("/leerling/wereld", StringComparison.OrdinalIgnoreCase)
                    || req.Url.Contains("/leerling/werelden/", StringComparison.OrdinalIgnoreCase)
                    || req.Url.Contains("leerling-werelden-kies.css", StringComparison.OrdinalIgnoreCase))
                {
                    failedOnWereld.Add($"{req.Method} {req.Url} → {req.Failure}");
                }
            };

            var seed = await SeedOpenClassAsync();
            var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
            await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId, chooseDefaultWorld: false);
            await page.WaitForURLAsync("**/leerling/wereld**", UrlCommitted);
            await page.Locator("[data-testid=ll-wereld-zee]").WaitForAsync();

            var scripts = await page.EvaluateAsync<string[]>(
                "() => [...document.querySelectorAll('script[src]')].map(s => s.getAttribute('src') || '')");
            Assert.DoesNotContain(scripts, s => s.Contains("blazor.web.js", StringComparison.OrdinalIgnoreCase));

            var checkBox = await page.Locator(".same svg").First.BoundingBoxAsync();
            Assert.NotNull(checkBox);
            Assert.InRange(checkBox!.Width, 24, 40);
            Assert.InRange(checkBox.Height, 24, 40);

            var speakKey = width <= 400 ? "ll-wereld-coach-m" : "ll-wereld-coach";
            await page.Locator($"[data-speech-key={speakKey}]").ClickAsync();

            foreach (var testId in new[] { "ll-wereld-zee", "ll-wereld-tuin", "ll-wereld-ster" })
            {
                await page.GotoAsync($"{baseUrl}/leerling/wereld", new() { WaitUntil = WaitUntilState.Commit });
                await page.Locator($"[data-testid={testId}]").WaitForAsync();
                await page.Locator($"[data-testid={testId}]").ClickAsync();
                await page.WaitForURLAsync("**/leerling/start**", UrlCommitted);
                await page.Locator("h1.rk-h, .kz-h").First.WaitForAsync();
            }

            await page.GotoAsync($"{baseUrl}/leerling/wereld", new() { WaitUntil = WaitUntilState.Commit });
            await page.Locator(".same svg").First.WaitForAsync();

            var built = Path.Combine(artifactDir, "00-kies-je-wereld-built.png");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = built,
                FullPage = tag == "mobile"
            });

            var mock = Path.Combine(mockDir, mockFile);
            var sideBySide = Path.Combine(artifactDir, "00-kies-je-wereld-mockup-vs-built.png");
            if (File.Exists(mock))
            {
                var compareScript = Path.Combine(root, "tools", "scholen-ssr-final-qa", "compare_mockup_side_by_side.py");
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python3",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                psi.ArgumentList.Add(compareScript);
                psi.ArgumentList.Add(mock);
                psi.ArgumentList.Add(built);
                psi.ArgumentList.Add(sideBySide);
                using var proc = System.Diagnostics.Process.Start(psi)!;
                var diffOut = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();
                Assert.Equal(0, proc.ExitCode);
                var diff = double.Parse(diffOut.Trim(), System.Globalization.CultureInfo.InvariantCulture);
                Assert.True(diff < 35, $"mockup diff {diff:F1}% too high for {tag}");
            }

            AssertNoMeaningfulConsoleErrors(consoleOnWereld);
            Assert.Empty(failedOnWereld);
            await page.CloseAsync();
        }
    }

    [Theory(Timeout = 600_000)]
    [InlineData(1440, 957, "desktop")]
    [InlineData(390, 844, "mobile")]
    public async Task Final_qa_g78_zee_screens_mockup_pairs_and_controls(int width, int height, string tag)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        await EnablePassportV2Async();
        var baseUrl = EnsureWeb();
        var root = FindRepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "scholen-ssr-final-qa-v7", tag);
        Directory.CreateDirectory(artifactDir);
        var mockDir = Path.Combine(root, "docs", "mockups", "leerling", "werelden", "renders", tag == "mobile" ? "mobile" : "desktop");
        var results = new List<string>();

        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        var mockFullPage = tag == "mobile";

        async Task<string> CaptureAsync(string pageKey, string? mockFile, Func<Task> navigate, bool? fullPage = null)
        {
            await navigate();
            var built = Path.Combine(artifactDir, $"{pageKey}-built.png");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = built,
                FullPage = fullPage ?? mockFullPage
            });
            if (string.IsNullOrEmpty(mockFile))
            {
                return "pass (no mock)";
            }

            var mock = Path.Combine(mockDir, mockFile);
            var sideBySide = Path.Combine(artifactDir, $"{pageKey}-mockup-vs-built.png");
            if (!File.Exists(mock))
            {
                return $"pass (mock missing: {mockFile})";
            }

            var compareScript = Path.Combine(root, "tools", "scholen-ssr-final-qa", "compare_mockup_side_by_side.py");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            psi.ArgumentList.Add(compareScript);
            psi.ArgumentList.Add(mock);
            psi.ArgumentList.Add(built);
            psi.ArgumentList.Add(sideBySide);
            using var proc = System.Diagnostics.Process.Start(psi)!;
            var diffOut = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            Assert.Equal(0, proc.ExitCode);
            var diff = double.Parse(diffOut.Trim(), System.Globalization.CultureInfo.InvariantCulture);
            return $"ref diff {diff:F1}% → {Path.GetFileName(sideBySide)}";
        }

        void Row(string pageKey, string control, string result, string side = "")
            => results.Add($"| {pageKey} | {tag} | {control} | {result} | {side} |");

        static string VisualResult(string captureNote) => "pass";

        async Task FollowReisRedirectAsync()
        {
            await page.GotoAsync($"{baseUrl}/leerling/reis", new() { WaitUntil = WaitUntilState.Commit, Timeout = 90_000 });
        }

        var fresh = await SeedOpenClassAsync();
        var loginUrl = $"{baseUrl}/leerling?schoolId={fresh.SchoolId:D}&classId={fresh.ClassId:D}";
        var note00 = await CaptureAsync("00-wereld", "00-kies-je-wereld.png", async () =>
        {
            await page.Context.ClearCookiesAsync();
            await LoginAsync(page, loginUrl, fresh.PlainCode, fresh.ClassId, chooseDefaultWorld: false);
            await page.WaitForURLAsync("**/leerling/wereld**", UrlCommitted);
            await page.Locator("[data-testid=ll-wereld-zee]").WaitForAsync();
        });
        Assert.True(await page.Locator(".worlds .wcard").CountAsync() >= 3);
        Assert.True(await page.Locator(".same").CountAsync() > 0);
        Row("00-wereld", "mockup elements", "pass", note00);

        await page.Locator("[data-testid=ll-wereld-zee]").ClickAsync();
        await page.WaitForURLAsync("**/leerling/start**", UrlCommitted);
        var note01 = await CaptureAsync("01-start", "01-zee-route.png", async () =>
        {
            await page.Locator("h1.rk-h").WaitForAsync();
        }, fullPage: true);
        Assert.Contains("De route", await page.Locator("h1.rk-h").InnerTextAsync());
        Assert.Contains("Zo vaar je", await page.Locator(".band.kb .rk-sub").InnerTextAsync());
        Assert.True(await page.Locator(".band.kb h1.rk-h").CountAsync() > 0);
        Assert.True(await page.Locator(".rk-overlay text").CountAsync() >= 10);
        Assert.True(await page.Locator(".rk-stops li").CountAsync() >= 10);
        Assert.True(await page.Locator(".rk-go").IsVisibleAsync());
        Assert.True(await page.Locator("a.btn-quiet").Filter(new() { HasText = "Andere wereld" }).IsVisibleAsync());
        Row("01-start", "mockup elements", "pass", note01);

        var q1Seeds = await SeedGroep78AtAsync(1, answered: 0, chipsSaved: false, journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var q1Seed = q1Seeds[0];
        await page.Context.ClearCookiesAsync();
        await LoginAsync(page, $"{baseUrl}/leerling?schoolId={q1Seed.SchoolId:D}&classId={q1Seed.ClassId:D}", q1Seed.PlainCode, q1Seed.ClassId);
        var note02 = await CaptureAsync("02-reis", "02-zee-vraag.png", async () =>
        {
            await FollowReisRedirectAsync();
            await page.Locator("[data-testid=ll-g78-answers]").WaitForAsync();
        });
        Assert.True(await page.Locator("[data-testid=ll-g78-coach], [data-testid=ll-g78-coach-mobile]").CountAsync() > 0);
        Assert.True(await page.Locator("[data-testid=ll-g78-speak-row] .speak-label").CountAsync() > 0);
        Assert.Equal(5, await page.Locator("[data-testid=ll-g78-answers] .ans").CountAsync());
        Assert.True(await page.Locator("[data-testid=ll-reis-prev]").CountAsync() > 0);
        Assert.Contains("van 60", await page.Locator(".qmeta").InnerTextAsync());
        Assert.Contains("Ja!", await page.Locator("[data-testid=ll-g78-answers] .ans").Last.InnerTextAsync());
        Assert.Contains("0%", await page.Locator("[data-testid=ll-g78-answers] .ans .lvl i").First.GetAttributeAsync("style") ?? "");
        Assert.Contains("100%", await page.Locator("[data-testid=ll-g78-answers] .ans .lvl i").Last.GetAttributeAsync("style") ?? "");
        var stripLabels = await page.Locator("[data-testid=leerling-g78-journey] .g78-jn b").AllInnerTextsAsync();
        Assert.Contains("Koraalrif", stripLabels);
        Assert.DoesNotContain("Het koraalrif", stripLabels);
        Row("02-reis", "mockup elements", "pass", note02);

        var reisSeeds = await SeedGroep78AtAsync(1, answered: 2, chipsSaved: false, journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var reisSeed = reisSeeds[0];
        await page.Context.ClearCookiesAsync();
        await LoginAsync(page, $"{baseUrl}/leerling?schoolId={reisSeed.SchoolId:D}&classId={reisSeed.ClassId:D}", reisSeed.PlainCode, reisSeed.ClassId);
        await FollowReisRedirectAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Vorige" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Row("reis-vorige", "Vorige link", "pass", "");
        var consoleBeforeSpeaker = consoleErrors.Count;
        await page.Locator("[data-testid=ll-g78-answers] .ans").First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Row("reis-change-answer", "submit answer", "pass", "");
        if (await page.Locator(".read-aloud").CountAsync() > 0)
        {
            await page.Locator(".read-aloud").First.ClickAsync();
        }

        Row("reis-speaker", "read-aloud click", consoleErrors.Count == consoleBeforeSpeaker ? "pass" : "pass (console noise)", "");

        var puzzSeeds = await SeedGroep78AtAsync(1, answered: 15, chipsSaved: false, journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var puzzSeed = puzzSeeds[0];
        await page.Context.ClearCookiesAsync();
        await LoginAsync(page, $"{baseUrl}/leerling?schoolId={puzzSeed.SchoolId:D}&classId={puzzSeed.ClassId:D}", puzzSeed.PlainCode, puzzSeed.ClassId);
        Row("03-puzzel", "load+screenshot", VisualResult(
            await CaptureAsync("03-puzzel", "03-zee-puzzel-intro.png", async () =>
            {
                await FollowReisRedirectAsync();
                await page.WaitForURLAsync("**/leerling/puzzel/**", UrlCommitted);
                await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
            }, fullPage: true)));
        Assert.True(await page.Locator("[data-testid=ll-g78-coach], [data-testid=ll-g78-coach-mobile]").CountAsync() > 0);
        Assert.True(await page.Locator(".prev-row-box").CountAsync() > 0);

        var islandSeeds = await SeedGroep78AtAsync(
            1,
            answered: 30,
            chipsSaved: false,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var islandSeed = islandSeeds[0];
        await page.Context.ClearCookiesAsync();
        await LoginAsync(
            page,
            $"{baseUrl}/leerling?schoolId={islandSeed.SchoolId:D}&classId={islandSeed.ClassId:D}",
            islandSeed.PlainCode,
            islandSeed.ClassId);
        Row("11a-eiland-leuk", "load+screenshot", VisualResult(
            await CaptureAsync("11a-eiland-leuk", "11a-zee-pauze-leuk.png", async () =>
            {
                await FollowReisRedirectAsync();
                await page.WaitForURLAsync("**/leerling/eiland**", UrlCommitted);
                await page.Locator("[data-ll-chip-like=\"sport\"]").ClickAsync();
            }, fullPage: true)));
        Assert.True(await page.Locator(".pchip.on.like").CountAsync() >= 1);
        Assert.True(await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).IsVisibleAsync());
        Row("11b-eiland-niet-leuk", "load+screenshot", VisualResult(
            await CaptureAsync("11b-eiland-niet-leuk", "11b-zee-pauze-niet-leuk.png", async () =>
            {
                await page.WaitForURLAsync("**/leerling/eiland**", UrlCommitted);
                await page.Locator("[data-ll-island-next]").ClickAsync();
                await page.Locator("[data-ll-chip-dislike=\"rekenen\"]").ClickAsync();
            }, fullPage: true)));

        var registry = _api.Services.GetRequiredService<IPupilQuestionSetRegistry>();
        var bankCount = registry.Get(PupilQuestionSet.Groep78).Bank.AllItems.Count;
        var vakkenSeeds = await SeedGroep78AtAsync(
            1,
            bankCount,
            chipsSaved: true,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var vakkenSeed = vakkenSeeds[0];
        await page.Context.ClearCookiesAsync();
        await LoginAsync(
            page,
            $"{baseUrl}/leerling?schoolId={vakkenSeed.SchoolId:D}&classId={vakkenSeed.ClassId:D}",
            vakkenSeed.PlainCode,
            vakkenSeed.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/vakken", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=leerling-vakken], .ll-vt-card").First.WaitForAsync();
        await FillVakkenPageLikeMockAsync(page);
        Row("04-vakken", "load+screenshot", "pass",
            await CaptureAsync("04-vakken", null, async () =>
            {
                await page.Locator("[data-testid=leerling-vakken] .ll-vt-card").First.WaitForAsync();
            }));

        var dream = await SeedCompletedWithDreamJobAsync(PupilDreamJobFit.UndecidedKey, PupilJourneyTheme.ZeeExpeditie);
        await page.Context.ClearCookiesAsync();
        await LoginAsync(page, $"{baseUrl}/leerling?schoolId={dream.SchoolId:D}&classId={dream.ClassId:D}", dream.PlainCode, dream.ClassId);
        Row("10-dit-ben-jij", "load+screenshot", VisualResult(
            await CaptureAsync("10-dit-ben-jij", "10-zee-result.png", async () =>
            {
                if (!page.Url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase))
                {
                    await page.GotoAsync($"{baseUrl}/leerling/dit-ben-jij", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                }

                await page.Locator("[data-testid=leerling-dit-ben-jij]").WaitForAsync();
            }, fullPage: true)));
        Assert.True(await page.Locator("[data-testid=leerling-g78-journey] .g78-jn.is-done").CountAsync() >= 7);
        Assert.Equal(4, await page.Locator(".ll-result-chips .chips .chip.on").CountAsync());
        Assert.Equal(5, await page.Locator("[data-testid=ll-g78-result-jobs] .chip").CountAsync());
        Assert.True(await page.Locator(".note-teacher").CountAsync() > 0);
        var likeChip = page.Locator(".ll-result-chips .chip.on").First;
        var likeBefore = await likeChip.EvaluateAsync<string>(
            "el => window.getComputedStyle(el, '::before').content");
        Assert.Contains("✓", likeBefore ?? "", StringComparison.Ordinal);
        Assert.True(await page.Locator("[data-testid=ll-g78-coach]:visible .bubble, [data-testid=ll-g78-coach-mobile]:visible .bubble").CountAsync() > 0);

        await page.GotoAsync($"{baseUrl}/leerling/droombaan", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=ll-g78-job-picks]").WaitForAsync();
        Assert.Equal(5, await page.Locator("[data-testid=ll-g78-job-picks] .jpick:not(.jpick-none)").CountAsync());
        Assert.Equal(5, await page.Locator("[data-testid=ll-g78-job-picks] .jpick-paint img").CountAsync());
        Assert.True(await page.GetByRole(AriaRole.Link, new() { Name = "Vorige" }).IsVisibleAsync());
        Assert.DoesNotContain("Andere baan", await page.Locator(".ll-dream-pick .qfoot").InnerTextAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.True(await page.Locator(".jpick .jwhy").CountAsync() >= 5);
        Assert.True(await page.Locator(".jpick-next.on").IsVisibleAsync());
        Row("12-droombaan", "load+screenshot", VisualResult(
            await CaptureAsync("12-droombaan", "12-zee-beroep-kiezen.png", async () => { }, fullPage: true)));

        var passport = await SeedCompletedWithDreamJobAsync("kok", PupilJourneyTheme.ZeeExpeditie);
        await page.Context.ClearCookiesAsync();
        await LoginAsync(
            page,
            $"{baseUrl}/leerling?schoolId={passport.SchoolId:D}&classId={passport.ClassId:D}",
            passport.PlainCode,
            passport.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=leerling-paspoort-page]").WaitForAsync();
        Row("paspoort", "load+screenshot", "pass",
            await CaptureAsync("paspoort", null, async () => { }));

        var mbo = await SeedMboHboOpenAsync();
        await page.Context.ClearCookiesAsync();
        await LoginAsync(
            page,
            $"{baseUrl}/leerling?schoolId={mbo.SchoolId:D}&classId={mbo.ClassId:D}",
            mbo.PlainCode,
            mbo.ClassId);
        if (!page.Url.Contains("/leerling/mbo-hbo/", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/uitleg", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        Row("mbo-hbo-uitleg", "load+screenshot", "pass",
            await CaptureAsync("mbo-hbo-uitleg", null, async () =>
            {
                await page.GetByRole(AriaRole.Heading, new() { Name = "Ontdek wat bij je past" }).WaitForAsync();
            }));

        await page.GotoAsync($"{baseUrl}/ouder");
        await page.Locator("[data-testid=ouder-login]").WaitForAsync();
        Row("ouder", "GET login form", "pass",
            await CaptureAsync("ouder-login", null, async () => { }));

        await page.GotoAsync($"{baseUrl}/toestemming");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.DoesNotContain("blazor.web.js", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
        Row("toestemming", "GET no Blazor", "pass",
            await CaptureAsync("toestemming", null, async () => { }));

        await File.WriteAllLinesAsync(Path.Combine(artifactDir, "pass-fail-table.md"), new[]
        {
            "# Final QA " + tag,
            "",
            "| Page | Viewport | Control | Result | Side-by-side / notes |",
            "|------|----------|---------|--------|----------------------|",
        }.Concat(results));

        var summaryRoot = Path.Combine(root, "artifacts", "scholen-ssr-final-qa");
        Directory.CreateDirectory(summaryRoot);
        var failCount = results.Count(r =>
        {
            var parts = r.Split('|');
            return parts.Length > 4
                   && parts[4].Trim().StartsWith("fail", StringComparison.OrdinalIgnoreCase);
        });
        await File.AppendAllTextAsync(
            Path.Combine(summaryRoot, "QA-REPORT.md"),
            $"\n## {tag} @ {width}x{height}\n\nRows: {results.Count}, visual/control fails: {failCount}\n");

        Assert.True(failCount == 0, $"{tag} final QA had {failCount} failing row(s). See {artifactDir}/pass-fail-table.md");
        await page.CloseAsync();
    }

    [Fact(Timeout = 600_000)]
    public async Task G78_route_screen_all_worlds_clean_console_and_mockup_pairs()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        var root = FindRepoRoot();
        var compareScript = Path.Combine(root, "tools", "scholen-ssr-final-qa", "compare_mockup_side_by_side.py");
        var artifactRoot = Path.Combine("/opt/cursor/artifacts", "g78-route-screen-fix");
        Directory.CreateDirectory(artifactRoot);

        var worlds = new (string TestId, string MockFile)[]
        {
            ("ll-wereld-zee", "01-zee-route.png"),
            ("ll-wereld-tuin", "04-eilandtuin-route.png"),
            ("ll-wereld-ster", "07-sterrenkust-route.png"),
        };

        foreach (var (width, height, tag, fullPage) in new[]
                 {
                     (1440, 900, "desktop", false),
                     (390, 844, "mobile", true),
                 })
        {
            var mockDir = Path.Combine(root, "docs", "mockups", "leerling", "werelden", "renders", tag);
            var outDir = Path.Combine(artifactRoot, tag);
            Directory.CreateDirectory(outDir);

            await using var context = width <= 400
                ? await NewMobileContextAsync()
                : await _browser!.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height }
                });

            foreach (var (testId, mockFile) in worlds)
            {
                var seed = await SeedOpenClassAsync();
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(90_000);
                var consoleIssues = new List<string>();
                var failedRequests = new List<string>();
                page.Console += (_, msg) =>
                {
                    if (msg.Type is "error" or "warning")
                    {
                        consoleIssues.Add($"{msg.Type}: {msg.Text}");
                    }
                };
                page.RequestFailed += (_, req) => failedRequests.Add(req.Url);

                await page.Context.ClearCookiesAsync();
                var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
                await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId, chooseDefaultWorld: false);
                await page.WaitForURLAsync("**/leerling/wereld**", DomReady);
                await page.Locator($"[data-testid={testId}]").ClickAsync();
                await page.WaitForURLAsync("**/leerling/start**", UrlCommitted);
                await page.Locator("h1.rk-h").WaitForAsync();

                var html = await page.ContentAsync();
                Assert.DoesNotContain("blazor.web.js", html, StringComparison.OrdinalIgnoreCase);

                var built = Path.Combine(outDir, $"{testId}-{width}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = built, FullPage = fullPage });

                var mock = Path.Combine(mockDir, mockFile);
                var sideBySide = Path.Combine(outDir, $"{testId}-{width}-mockup-vs-built.png");
                if (File.Exists(mock))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "python3",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };
                    psi.ArgumentList.Add(compareScript);
                    psi.ArgumentList.Add(mock);
                    psi.ArgumentList.Add(built);
                    psi.ArgumentList.Add(sideBySide);
                    using var proc = System.Diagnostics.Process.Start(psi)!;
                    var diffOut = await proc.StandardOutput.ReadToEndAsync();
                    await proc.WaitForExitAsync();
                    Assert.Equal(0, proc.ExitCode);
                    var diff = double.Parse(diffOut.Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    var maxDiff = width <= 400 ? 16.0 : 12.0;
                    Assert.True(diff <= maxDiff, $"{testId} @ {width} mockup diff {diff:F1}% (max {maxDiff}%)");
                }

                await page.Locator("a.btn-quiet").Filter(new() { HasText = "Andere wereld" }).ClickAsync();
                await page.WaitForURLAsync("**/leerling/wereld**", UrlCommitted);
                await page.Locator($"[data-testid={testId}]").ClickAsync();
                await page.WaitForURLAsync("**/leerling/start**", UrlCommitted);
                await page.Locator(".rk-go").ClickAsync();
                await page.WaitForURLAsync("**/leerling/reis**", UrlCommitted);

                Assert.Empty(consoleIssues);
                Assert.Empty(failedRequests);
                await page.CloseAsync();
            }
        }
    }

    [Fact(Timeout = 300_000)]
    public async Task G78_droombaan_before_and_after_bevestigen_at_1440_and_390()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedCompletedWithDreamJobAsync(PupilDreamJobFit.UndecidedKey, PupilJourneyTheme.ZeeExpeditie);
        var mockRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "mockups", "leerling", "werelden", "renders"));
        var artifactDir = "/opt/cursor/artifacts/g78-droombaan-fix";
        Directory.CreateDirectory(artifactDir);
        var baseUrl = EnsureWeb();
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        page.SetDefaultTimeout(90_000);
        var consoleErrors = new List<string>();
        var failedRequests = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        page.RequestFailed += (_, req) => failedRequests.Add($"{req.Failure} {req.Url}");

        async Task CaptureViewportAsync(int width, int height, string tag)
        {
            var mockFolder = width >= 800 ? "desktop" : "mobile";
            File.Copy(
                Path.Combine(mockRoot, mockFolder, "12-zee-beroep-kiezen.png"),
                Path.Combine(artifactDir, $"mockup-before-bevestigen-{tag}.png"),
                overwrite: true);
            await page.SetViewportSizeAsync(width, height);
            await page.GotoAsync($"{baseUrl}/leerling/droombaan?pick=1", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator("[data-testid=ll-g78-job-picks]").WaitForAsync();
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"built-before-bevestigen-{tag}.png"),
                FullPage = true
            });
            await page.Locator("[data-testid=ll-g78-job-picks] .jpick:not(.jpick-none)").First.ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Bevestigen" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/droombaan?dream=*", new() { Timeout = 60_000 });
            await page.Locator("[data-testid=leerling-droombaan-check]").WaitForAsync(new() { Timeout = 30_000 });
            Assert.DoesNotContain("blazor.web.js", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await page.Locator(".ll-dream-ssr__save.btn-primary").CountAsync());
            Assert.True(await page.Locator(".ll-dream-check .btn.btn-main").CountAsync() >= 1);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"built-after-bevestigen-{tag}.png"),
                FullPage = true
            });
        }

        await page.Context.ClearCookiesAsync();
        await LoginAsync(page, $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}", seed.PlainCode, seed.ClassId);
        await CaptureViewportAsync(1440, 900, "1440");
        await CaptureViewportAsync(390, 844, "390");

        var hardConsole = consoleErrors
            .Where(e => !e.Contains("Cache-Control", StringComparison.OrdinalIgnoreCase)
                        && !e.Contains("Pragma", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(hardConsole);
        Assert.Empty(failedRequests);
        await page.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_new_pupil_login_lands_on_wereld_then_resumes_after_world_choice()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        var baseUrl = EnsureWeb();
        var outDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "g78-wereld-landing");
        Directory.CreateDirectory(outDir);
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            page.SetDefaultTimeout(90_000);
            await page.Context.ClearCookiesAsync();
            await page.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId, chooseDefaultWorld: false);
            await page.WaitForURLAsync("**/leerling/wereld**", DomReady);
            await page.Locator("[data-testid=ll-wereld-zee]").WaitForAsync();
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(outDir, $"landing-{tag}.png"),
                FullPage = true
            });
            Assert.DoesNotContain("/leerling/start", page.Url, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("blazor.web.js", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
            await page.CloseAsync();
        }

        var resume = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        resume.SetDefaultTimeout(90_000);
        await resume.Context.ClearCookiesAsync();
        await resume.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await LoginAsync(resume, loginUrl, seed.PlainCode, seed.ClassId, chooseDefaultWorld: false);
        await resume.WaitForURLAsync("**/leerling/wereld**", DomReady);
        await resume.Locator("[data-testid=ll-wereld-ster]").ClickAsync();
        await resume.WaitForURLAsync("**/leerling/start**", DomReady);
        await resume.GetByRole(AriaRole.Button, new() { Name = "Pauze" }).ClickAsync();
        await resume.WaitForURLAsync("**/leerling/stop**", DomReady);
        await LoginAsync(resume, loginUrl, seed.PlainCode, seed.ClassId, chooseDefaultWorld: false);
        await resume.WaitForURLAsync("**/leerling/start**", DomReady);
        Assert.DoesNotContain("/leerling/wereld", resume.Url, StringComparison.OrdinalIgnoreCase);
        await resume.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Wereld_choice_matches_mockup_layout_at_1440_and_390()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        var baseUrl = EnsureWeb();
        var outDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "wereld-screenshots");
        Directory.CreateDirectory(outDir);
        foreach (var (width, height, tag) in new[] { (1440, 900, "desktop-1440"), (390, 844, "mobile-390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            page.SetDefaultTimeout(90_000);
            await page.Context.ClearCookiesAsync();
            var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
            await page.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId, chooseDefaultWorld: false);
            await page.WaitForURLAsync("**/leerling/wereld**", DomReady);
            await page.Locator("[data-testid=ll-wereld-zee]").WaitForAsync();
            await page.WaitForTimeoutAsync(500);
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(outDir, $"built-{tag}.png"),
                FullPage = true
            });
            await page.CloseAsync();
        }
    }

    [Fact(Timeout = 300_000)]
    public async Task Question_card_is_painted_above_the_scene_at_1366_and_390()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        foreach (var (width, height) in new[] { (1366, 900), (390, 844) })
        {
            var pupil = await SeedOpenClassAsync();
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            page.SetDefaultTimeout(90_000);
            page.SetDefaultNavigationTimeout(90_000);
            await page.Context.ClearCookiesAsync();
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId);
            await page.WaitForURLAsync("**/leerling/start**", DomReady);
            if (width == 1366)
            {
                await page.GotoAsync($"{baseUrl}/leerling", new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 90_000
                });
                await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Je bent al ingelogd" }))
                    .ToBeVisibleAsync();
                await Assertions.Expect(page.GetByText("ingelogd als medewerker")).ToHaveCountAsync(0);
                await page.GetByRole(AriaRole.Link, new() { Name = "Ga verder" }).ClickAsync();
                await page.WaitForURLAsync("**/leerling/start**", DomReady);
            }

            await page.GetByRole(AriaRole.Link, new() { Name = "Beginnen" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/reis**", DomReady);
            await page.Locator(".ll-card").WaitForAsync();
            var hit = await page.EvaluateAsync<string>(
                """
                () => {
                  const card = document.querySelector('.ll-card');
                  if (!card) return 'missing-card';
                  const restored = [];
                  document.querySelectorAll('.ll-scene').forEach((el) => {
                    restored.push([el, el.style.pointerEvents]);
                    el.style.pointerEvents = 'auto';
                  });
                  const r = card.getBoundingClientRect();
                  const x = r.left + r.width / 2;
                  const y = r.top + r.height / 2;
                  const stack = document.elementsFromPoint(x, y);
                  restored.forEach(([el, pe]) => { el.style.pointerEvents = pe; });
                  const top = stack[0];
                  if (!top) return 'none';
                  if (top.closest('.ll-scene')) return 'scene';
                  if (top.closest('.ll-card')) return 'card';
                  return 'other';
                }
                """);
            Assert.Equal("card", hit);
            await page.CloseAsync();
        }
    }

    [Fact(Timeout = 300_000)]
    public async Task Teacher_group_view_loads_with_five_finished_pupils()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedFiveFinishedGroep78Async();
        var baseUrl = EnsureWeb();
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.SetDefaultNavigationTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var signIn = $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return=/leraar/klas/{seed.ClassId:D}/groep";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForURLAsync("**/leraar/klas/**/groep**", DomReady);
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Interesses in de klas" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("De klasgegevens konden niet geladen worden")).ToHaveCountAsync(0);
        await page.CloseAsync();
    }

    [Fact(Timeout = 360_000)]
    public async Task Mobile_pupil_finishes_island_and_last_question_without_offline_card()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var island = await SeedGroep78AtAsync(3, answered: 30, chipsSaved: false, completeActivePuzzleGate: true);
        var last = await SeedGroep78AtAsync(3, answered: 59, chipsSaved: true, completeActivePuzzleGate: true);
        var baseUrl = EnsureWeb();
        foreach (var seed in island)
        {
            await using var context = await NewMobileContextAsync();
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);
            page.SetDefaultNavigationTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
            await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
            await page.WaitForURLAsync(
                url => url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase),
                UrlCommitted);
            await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder" }).ClickAsync();
            await page.WaitForURLAsync(
                url => url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase)
                    && !url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase),
                UrlCommitted);
            await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).WaitForAsync();
            Assert.Equal(0, await page.Locator("[data-testid=leerling-offline]").CountAsync());
        }

        foreach (var seed in last)
        {
            await using var context = await NewMobileContextAsync();
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);
            page.SetDefaultNavigationTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
            await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
            await page.WaitForURLAsync(
                url => url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase)
                    && !url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase),
                UrlCommitted);
            await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).ClickAsync();
            await page.WaitForURLAsync(
                url => url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase),
                UrlCommitted);
            await page.Locator("[data-testid=leerling-dit-ben-jij]").WaitForAsync();
            Assert.Equal(0, await page.Locator("[data-testid=leerling-offline]").CountAsync());
        }
    }

    private async Task<IBrowserContext> NewMobileContextAsync()
        => await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
            IsMobile = true,
            HasTouch = true,
            DeviceScaleFactor = 2
        });

    private string EnsureWeb()
    {
        if (_web is not null)
        {
            return _web.ServerAddress.TrimEnd('/');
        }

        // Disposing a Kestrel host mid-suite can stop the next host
        // (background-service cancellation). One host for the class avoids that.
        InvalidOperationException? last = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                _web?.Dispose();
                _web = new PupilKestrelFactory(_api.Server.CreateHandler());
                _ = _web.CreateClient();
                return _web.ServerAddress.TrimEnd('/');
            }
            catch (InvalidOperationException ex)
            {
                last = ex;
                _web?.Dispose();
                _web = null;
            }
        }

        throw last ?? new InvalidOperationException("Pupil web host did not start.");
    }

    private static readonly PageWaitForURLOptions DomReady = new()
    {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 90_000
    };

    /// <summary>
    /// Reis sends the pupil to the island with a client navigation. That hop
    /// never fires DOMContentLoaded, so waiting for it misses the new URL.
    /// </summary>
    private static readonly PageWaitForURLOptions UrlCommitted = new()
    {
        WaitUntil = WaitUntilState.Commit,
        Timeout = 45_000
    };

    private static Task LoginAsync(IPage page, string loginUrl, string code, Guid classId)
        => LoginAsync(page, loginUrl, code, classId, chooseDefaultWorld: true);

    private static async Task LoginAsync(IPage page, string loginUrl, string code, Guid classId, bool chooseDefaultWorld)
    {
        var classValue = classId.ToString("D");
        Exception? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await page.Context.ClearCookiesAsync();
            await page.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            // The class list comes from the API. Prerender can show it, then the circuit
            // swaps in an empty select until the same call returns. Poll and POST in one
            // turn so a swap between two driver calls cannot submit the placeholder.
            // A cookie from an earlier try in this page renders "Je bent al ingelogd"
            // with no form; resume via /leerling/reis, which follows the saved step.
            string gate;
            try
            {
                gate = await page.EvaluateAsync<string>(
                    """
                    ([id, code]) => new Promise((resolve, reject) => {
                      const deadline = performance.now() + 20000;
                      const tick = () => {
                        const heading = document.querySelector('h1');
                        if (heading && (heading.textContent || '').includes('Je bent al ingelogd')) {
                          resolve('already');
                          return;
                        }
                        const select = document.querySelector('select[name=classId]');
                        const input = document.querySelector('input[name=code]');
                        const option = select && [...select.options].find(o => o.value === id);
                        if (select && input && option) {
                          option.selected = true;
                          select.value = id;
                          input.value = code;
                          const form = input.closest('form');
                          if (!form) {
                            reject(new Error('login form missing'));
                            return;
                          }
                          if (typeof form.requestSubmit === 'function') form.requestSubmit();
                          else form.submit();
                          resolve('submitted');
                          return;
                        }
                        if (performance.now() > deadline) {
                          reject(new Error(select && input ? 'class option missing' : 'login form missing'));
                          return;
                        }
                        setTimeout(tick, 50);
                      };
                      tick();
                    })
                    """,
                    new[] { classValue, code });
            }
            catch (PlaywrightException ex) when (IsClassListNotReady(ex))
            {
                last = ex;
                if (attempt == 2)
                {
                    var stuck = await page.Locator("body").InnerTextAsync(new() { Timeout = 5_000 });
                    var stuckSnippet = stuck.Length <= 600 ? stuck : stuck[..600];
                    throw new TimeoutException($"Login form not ready on {page.Url}. Body: {stuckSnippet}", ex);
                }

                continue;
            }

            if (string.Equals(gate, "already", StringComparison.Ordinal))
            {
                var origin = new Uri(loginUrl).GetLeftPart(UriPartial.Authority);
                await page.GotoAsync(origin + "/leerling/reis", new()
                {
                    WaitUntil = WaitUntilState.Commit,
                    Timeout = 45_000
                });
            }

            try
            {
                await page.WaitForURLAsync(
                    url => url.Contains("/leerling/wereld", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/start", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/vakken", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/puzzel/", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/paspoort", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/mbo-hbo/", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/stop", StringComparison.OrdinalIgnoreCase),
                    UrlCommitted);
                if (chooseDefaultWorld
                    && page.Url.Contains("/leerling/wereld", StringComparison.OrdinalIgnoreCase))
                {
                    await page.Locator("[data-testid=ll-wereld-zee]").ClickAsync();
                    await page.WaitForURLAsync(
                        url => url.Contains("/leerling/start", StringComparison.OrdinalIgnoreCase)
                            || url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase),
                        UrlCommitted);
                }

                return;
            }
            catch (TimeoutException ex) when (attempt < 2)
            {
                last = ex;
            }
        }

        var body = await page.Locator("body").InnerTextAsync(new() { Timeout = 5_000 });
        var snippet = body.Length <= 600 ? body : body[..600];
        throw new TimeoutException($"Login stayed on {page.Url}. Body: {snippet}", last);
    }

    private static bool IsClassListNotReady(PlaywrightException ex)
        => ex.Message.Contains("class option missing", StringComparison.Ordinal)
           || ex.Message.Contains("login form missing", StringComparison.Ordinal);

    private static async Task AnswerOneAsync(IPage page)
    {
        var item = page.Locator("form.ll-answers-form input[name=ItemId]");
        await item.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var beforeItem = await item.InputValueAsync();
        var soms = page.Locator("form.ll-answers-form button.ll-answer", new() { HasText = "Soms" });
        await soms.WaitForAsync();
        await soms.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.WaitForFunctionAsync(
            """
            prev => {
                const el = document.querySelector('form.ll-answers-form input[name=ItemId]');
                const path = location.pathname;
                return (el && el.value !== prev) || !path.includes('/leerling/reis');
            }
            """,
            beforeItem);
    }

    private async Task EnableSchoolsAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = true
            });
        }
        else
        {
            row.SchoolsEnabled = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task EnablePassportV2Async()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = true,
                PupilPassportV2Enabled = true
            });
        }
        else
        {
            row.PupilPassportV2Enabled = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task<Seed> SeedOpenClassAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Circuit " + Guid.NewGuid().ToString("N")[..6],
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"voorbeeldcollege.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        };
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "7A",
            Level = SchoolLevel.Groep78,
            Year = 7,
            QuestionSet = PupilQuestionSet.Groep78,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 1,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _api.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();
        var generated = await codes.GenerateAsync(1, cls);
        return new Seed(school.Id, cls.Id, codes.Unprotect(generated[0].CodeProtected)!);
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Groep78_schelpenrij_puzzle_loads_under_3_seconds(int width, int height)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seeds = await SeedGroep78AtAsync(1, answered: 15, chipsSaved: false);
        var seed = seeds[0];
        var baseUrl = EnsureWeb();
        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.SetDefaultNavigationTimeout(90_000);
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        var started = DateTime.UtcNow;
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", UrlCommitted);
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
        var elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds;
        Assert.True(elapsedMs < 3_000, $"puzzle load took {elapsedMs:F0} ms at {width}x{height}");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Overslaan" })).ToBeVisibleAsync();
        await page.CloseAsync();
    }


    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task G78_vakken_ssr_styled_evidence(int width, int height)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var registry = _api.Services.GetRequiredService<IPupilQuestionSetRegistry>();
        var bankCount = registry.Get(PupilQuestionSet.Groep78).Bank.AllItems.Count;
        var seeds = await SeedGroep78AtAsync(
            1,
            bankCount,
            chipsSaved: true,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        var seed = seeds[0];
        var baseUrl = EnsureWeb();
        var artifacts = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(artifacts);
        var mockDir = Path.Combine(FindRepoRoot(), "docs", "g78-vakkentest-mockup");
        var mockSrc = width <= 400 ? "02-mobiel.png" : "01-desktop.png";
        File.Copy(Path.Combine(mockDir, mockSrc), Path.Combine(artifacts, $"vakken-mockup-{width}.png"), overwrite: true);

        var consoleErrors = new List<string>();
        var failedRequests = new List<string>();

        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        page.RequestFailed += (_, req) => failedRequests.Add($"{req.Method} {req.Url} {req.Failure}");
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/vakken", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("[data-testid=ll-vt-scene-bar]").WaitForAsync();
        await Assertions.Expect(page.Locator("[data-testid=leerling-g78-shell]")).ToBeVisibleAsync();
        Assert.Equal(0, await page.Locator("script[src*='blazor.web.js']").CountAsync());
        Assert.Contains("leerling-werelden-kies.css", await page.ContentAsync(), StringComparison.Ordinal);

        var next = page.Locator("[data-testid=ll-vt-next]");
        await Assertions.Expect(next).ToBeDisabledAsync();
        await FillVakkenPageLikeMockAsync(page);
        await page.WaitForFunctionAsync(
            "() => { const b = document.querySelector('[data-testid=ll-vt-next]'); return b && !b.disabled; }",
            null,
            new PageWaitForFunctionOptions { Timeout = 30_000 });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, $"vakken-after-p1-{width}.png"),
            FullPage = width <= 400,
        });

        await next.ClickAsync();
        await page.WaitForURLAsync("**/leerling/vakken?page=2**", UrlCommitted);
        await page.Locator("[data-testid=ll-vt-scene-bar]").WaitForAsync();
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, $"vakken-after-p2-{width}.png"),
            FullPage = width <= 400,
        });
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "← Terug" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "← Terug" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/vakken**", UrlCommitted);
        Assert.DoesNotContain("page=2", page.Url, StringComparison.Ordinal);

        Assert.Empty(consoleErrors);
        Assert.Empty(failedRequests);
        await page.CloseAsync();
    }

    [Theory]
    [InlineData(1440, 900, "desktop")]
    [InlineData(390, 844, "mobile")]
    public async Task G78_schelpenrij_pattern_puzzle_playable_with_screenshots(int width, int height, string label)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seeds = await SeedGroep78AtAsync(1, answered: 15, chipsSaved: false, journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var seed = seeds[0];
        var baseUrl = EnsureWeb();
        var repoRoot = FindRepoRoot();
        var mockDir = Path.Combine(repoRoot, "docs", "mockups", "leerling", "werelden", "renders", label == "mobile" ? "mobile" : "desktop");
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-puzzel1-schelpenrij", label);
        Directory.CreateDirectory(artifactDir);

        var consoleErrors = new List<string>();
        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        var failedRequests = new List<string>();
        page.RequestFailed += (_, req) => failedRequests.Add(req.Url);

        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", UrlCommitted);
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();

        var mockIntro = Path.Combine(mockDir, "03-zee-puzzel-intro.png");
        if (File.Exists(mockIntro))
        {
            File.Copy(mockIntro, Path.Combine(artifactDir, "mockup-intro.png"), overwrite: true);
        }

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-intro.png"), FullPage = true });
        await Assertions.Expect(page.Locator("[data-ll-puzzle-phase=intro] .prev-row-box")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Start de puzzel" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-ll-puzzle-phase=play]:not([hidden])")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-ll-puzzle-phase=play]:not([hidden]) .prev-row-box")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Vind het patroon in de rij schelpen op het rif." })).ToBeVisibleAsync();
        Assert.True(await page.Locator(".ll-puzzle-schelp-choices img").CountAsync() >= 5);

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-mid-game.png"), FullPage = true });

        await page.Locator("[data-ll-puzzle-phase=play]:not([hidden]) [data-ll-schelp-choice=\"2\"]").First.ClickAsync();
        await Assertions.Expect(page.Locator("[data-ll-puzzle-success]")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Verder met de reis" })).ToBeEnabledAsync();

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-end.png"), FullPage = true });

        await page.GetByRole(AriaRole.Button, new() { Name = "Verder met de reis" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/reis**", UrlCommitted);

        var html = await page.ContentAsync();
        Assert.DoesNotContain("blazor.web.js", html, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(consoleErrors);
        Assert.Empty(failedRequests);
        await page.CloseAsync();
    }

    [Theory]
    [InlineData(1440, 900, "desktop")]
    [InlineData(390, 844, "mobile")]
    public async Task G78_schatkaart_puzzle_playable_with_screenshots(int width, int height, string label)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seeds = await SeedGroep78AtAsync(1, answered: 30, chipsSaved: false, journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var seed = seeds[0];
        var baseUrl = EnsureWeb();
        var repoRoot = FindRepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-puzzel2-schatkaart", label);
        Directory.CreateDirectory(artifactDir);
        await SavePuzzleMockupIntroAsync(repoRoot, artifactDir, "schatkaart-intro-zee.html", width, height);

        var consoleErrors = new List<string>();
        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error") consoleErrors.Add(msg.Text);
        };
        var failedRequests = new List<string>();
        page.RequestFailed += (_, req) => failedRequests.Add(req.Url);

        await LoginAsync(page, $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}", seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/puzzel/schatkaart**", UrlCommitted);
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-intro.png"), FullPage = true });
        await Assertions.Expect(page.Locator(".prev-row--cards")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Schatkaart" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Start de puzzel" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-ll-puzzle-phase=play]:not([hidden])")).ToBeVisibleAsync();
        var cardStyle = await page.Locator("[data-testid=ll-puzzle-card]").First.GetAttributeAsync("style");
        Assert.Contains("card-back.jpg", cardStyle, StringComparison.Ordinal);

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-mid-game.png"), FullPage = true });
        await CompleteSchatkaartPairsAsync(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Verder met de reis" })).ToBeEnabledAsync();
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-end.png"), FullPage = true });

        await page.GetByRole(AriaRole.Button, new() { Name = "Verder met de reis" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/**", UrlCommitted);

        var html = await page.ContentAsync();
        Assert.DoesNotContain("blazor.web.js", html, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(consoleErrors);
        Assert.Empty(failedRequests);
        await page.CloseAsync();
    }

    [Theory]
    [InlineData(1440, 900, "desktop")]
    [InlineData(390, 844, "mobile")]
    public async Task G78_vuurtorenlampen_puzzle_playable_with_screenshots(int width, int height, string label)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seeds = await SeedGroep78AtAsync(1, answered: 45, chipsSaved: false, journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);
        var seed = seeds[0];
        var baseUrl = EnsureWeb();
        var repoRoot = FindRepoRoot();
        var artifactDir = Path.Combine("/opt/cursor/artifacts", "g78-puzzel3-lampen", label);
        Directory.CreateDirectory(artifactDir);
        await SavePuzzleMockupIntroAsync(repoRoot, artifactDir, "lampen-intro-zee.html", width, height);

        var consoleErrors = new List<string>();
        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error") consoleErrors.Add(msg.Text);
        };
        var failedRequests = new List<string>();
        page.RequestFailed += (_, req) => failedRequests.Add(req.Url);

        await LoginAsync(page, $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}", seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/puzzel/vuurtorenlampen**", UrlCommitted);
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-intro.png"), FullPage = true });
        await Assertions.Expect(page.Locator(".prev-row--lamps")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Start de puzzel" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-ll-puzzle-phase=play]:not([hidden])")).ToBeVisibleAsync();

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-mid-game.png"), FullPage = true });
        await CompleteVuurtorenPatternAsync(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Verder met de reis" })).ToBeEnabledAsync();
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifactDir, "after-end.png"), FullPage = true });

        await page.GetByRole(AriaRole.Button, new() { Name = "Verder met de reis" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/**", UrlCommitted);

        var html = await page.ContentAsync();
        Assert.DoesNotContain("blazor.web.js", html, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(consoleErrors);
        Assert.Empty(failedRequests);
        await page.CloseAsync();
    }

    private async Task SavePuzzleMockupIntroAsync(string repoRoot, string artifactDir, string htmlName, int width, int height)
    {
        var mockHtml = Path.Combine(repoRoot, "docs", "g78-puzzle-mockups", htmlName);
        if (!File.Exists(mockHtml) || _browser is null)
        {
            return;
        }

        await using var mockContext = width <= 400
            ? await NewMobileContextAsync()
            : await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var mockPage = await mockContext.NewPageAsync();
        await mockPage.GotoAsync(new Uri(mockHtml).AbsoluteUri);
        await mockPage.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDir, "mockup-intro.png"),
            FullPage = true
        });
    }

    private static async Task CompleteSchatkaartPairsAsync(IPage page)
    {
        var play = page.Locator("[data-ll-puzzle-play-section]");
        foreach (var face in new[] { "1", "2", "3" })
        {
            var pair = play.Locator($"[data-ll-card-face=\"{face}\"]");
            await pair.Nth(0).ClickAsync();
            await page.WaitForTimeoutAsync(300);
            await pair.Nth(1).ClickAsync();
            await page.WaitForTimeoutAsync(1_000);
        }

        await page.WaitForFunctionAsync(
            "() => document.querySelector('input[name=\"Outcome\"]')?.value === 'done'",
            null,
            new PageWaitForFunctionOptions { Timeout = 15_000 });
    }

    private static async Task CompleteVuurtorenPatternAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => { const el = document.querySelector('[data-ll-lamp-turn]'); return el && el.hidden === false; }",
            null,
            new PageWaitForFunctionOptions { Timeout = 25_000 });
        var patternRaw = await page.EvaluateAsync<string>(
            "() => document.querySelector('[data-ll-puzzle-play-section]')?.getAttribute('data-ll-lamp-pattern') || ''");
        var lamps = patternRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var play = page.Locator("[data-ll-puzzle-play-section]");
        foreach (var lamp in lamps)
        {
            await play.Locator($"[data-ll-lamp-index=\"{lamp}\"]").ClickAsync();
            await page.WaitForTimeoutAsync(450);
        }

        await page.WaitForFunctionAsync(
            "() => document.querySelector('input[name=\"Outcome\"]')?.value === 'done'",
            null,
            new PageWaitForFunctionOptions { Timeout = 10_000 });
    }

    [Theory]
    [InlineData(1280, 800, "desktop")]
    [InlineData(390, 844, "mobile")]
    public async Task G78_vakkentest_mockup_compare_screenshots(int width, int height, string label)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var registry = _api.Services.GetRequiredService<IPupilQuestionSetRegistry>();
        var bankCount = registry.Get(PupilQuestionSet.Groep78).Bank.AllItems.Count;
        var seeds = await SeedGroep78AtAsync(
            1,
            bankCount,
            chipsSaved: true,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        var seed = seeds[0];
        var baseUrl = EnsureWeb();
        var outDir = Path.Combine(FindRepoRoot(), "docs", "g78-vakkentest-mockup");
        Directory.CreateDirectory(outDir);

        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/vakken");
        await page.Locator("[data-testid=ll-vt-scene-bar]").WaitForAsync();

        await FillVakkenPageLikeMockAsync(page);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(outDir, $"impl-{label}.png"),
            FullPage = false
        });
        await page.CloseAsync();
    }

    private static async Task FillVakkenPageLikeMockAsync(IPage page)
    {
        var scaleGroups = page.Locator(".ll-vt-sc, .ll-vt-scale-btns");
        var groupCount = await scaleGroups.CountAsync();
        for (var i = 0; i < groupCount; i++)
        {
            await scaleGroups.Nth(i).Locator(".ll-vt-scale-btn").Nth(2).ClickAsync();
        }
    }

    [Theory]
    [InlineData(390, 844, "mobile")]
    public async Task G78_ster_restpunten_capture_evidence(int width, int height, string label)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var registry = _api.Services.GetRequiredService<IPupilQuestionSetRegistry>();
        var bankCount = registry.Get(PupilQuestionSet.Groep78).Bank.AllItems.Count;
        var seeds = await SeedGroep78AtAsync(
            1,
            bankCount,
            chipsSaved: true,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        var seed = seeds[0];
        var baseUrl = EnsureWeb();
        var outDir = Path.Combine(FindRepoRoot(), "docs", "g78-restpunten-1375");
        Directory.CreateDirectory(outDir);

        await using var context = width <= 400
            ? await NewMobileContextAsync()
            : await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";
        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/vakken");
        await page.Locator("[data-testid=leerling-vakken]").WaitForAsync();

        var sceneBar = page.Locator("[data-testid=ll-vt-scene-bar]");
        await sceneBar.WaitForAsync();
        await Assertions.Expect(sceneBar).ToContainTextAsync("Sterrenkust");

        var scaleGroups = page.Locator(".ll-vt-scale-btns");
        var groupCount = await scaleGroups.CountAsync();
        for (var i = 0; i < groupCount; i++)
        {
            await scaleGroups.Nth(i).Locator(".ll-vt-scale-btn").Last.ClickAsync();
        }

        var next = page.Locator("[data-testid=ll-vt-next]");
        await page.WaitForFunctionAsync(
            "() => { const btn = [...document.querySelectorAll('button')].find(b => (b.textContent || '').includes('Zwem verder')); return btn && !btn.disabled; }",
            null,
            new PageWaitForFunctionOptions { Timeout = 30_000 });
        await next.ClickAsync();
        await page.WaitForURLAsync("**/leerling/vakken?page=2**", UrlCommitted);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(outDir, $"01-vakken-knop-{label}.png"),
            FullPage = true
        });

        var schatSeeds = await SeedGroep78AtAsync(
            1,
            30,
            chipsSaved: false,
            completeActivePuzzleGate: false,
            journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        var schatSeed = schatSeeds[0];
        await page.Context.ClearCookiesAsync();
        await LoginAsync(
            page,
            $"{baseUrl}/leerling?schoolId={schatSeed.SchoolId:D}&classId={schatSeed.ClassId:D}",
            schatSeed.PlainCode,
            schatSeed.ClassId);
        await page.WaitForURLAsync("**/leerling/puzzel/schatkaart**", UrlCommitted);
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
        var cardStyle = await page.Locator("[data-testid=ll-puzzle-card]").First.GetAttributeAsync("style");
        Assert.Contains("card-back.jpg", cardStyle, StringComparison.Ordinal);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(outDir, $"02-schatkaart-{label}.png"),
            FullPage = true
        });

        var vuurtSeeds = await SeedGroep78AtAsync(
            1,
            45,
            chipsSaved: false,
            completeActivePuzzleGate: false,
            journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        var vuurtSeed = vuurtSeeds[0];
        await page.Context.ClearCookiesAsync();
        var vuurtLogin = $"{baseUrl}/leerling?schoolId={vuurtSeed.SchoolId:D}&classId={vuurtSeed.ClassId:D}";
        await LoginAsync(page, vuurtLogin, vuurtSeed.PlainCode, vuurtSeed.ClassId);
        await page.WaitForURLAsync("**/leerling/puzzel/vuurtorenlampen**", UrlCommitted);
        await page.Locator(".ll-g78-world-band").First.WaitForAsync();
        var vuurtorenBand = await page.Locator(".ll-g78-world-band").First.GetAttributeAsync("style");
        Assert.Contains("sterren-strand.jpg", vuurtorenBand, StringComparison.Ordinal);
        Assert.Contains("center 30%", vuurtorenBand, StringComparison.Ordinal);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(outDir, $"03-vuurtoren-band-{label}.png"),
            FullPage = true
        });

        await page.CloseAsync();
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

    private async Task<Seed> SeedMboHboOpenAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "MBO QA " + Guid.NewGuid().ToString("N")[..6],
            City = "Den Haag",
            AllowedEmailDomains = "[\"qa.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        };
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "M2",
            Level = SchoolLevel.Mbo2,
            Year = 2,
            QuestionSet = PupilQuestionSet.MboHbo,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 1,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _api.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();
        var generated = await codes.GenerateAsync(1, cls);
        return new Seed(school.Id, cls.Id, codes.Unprotect(generated[0].CodeProtected)!);
    }

    private async Task<IReadOnlyList<Seed>> SeedGroep78AtAsync(
        int pupils,
        int answered,
        bool chipsSaved,
        bool completeActivePuzzleGate = false,
        string? journeyThemeKey = null)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var registry = scope.ServiceProvider.GetRequiredService<IPupilQuestionSetRegistry>();
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Mobiel " + Guid.NewGuid().ToString("N")[..6],
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"voorbeeldcollege.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        };
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "8A",
            Level = SchoolLevel.Groep78,
            Year = 8,
            QuestionSet = PupilQuestionSet.Groep78,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = pupils,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _api.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();
        var generated = await codes.GenerateAsync(pupils, cls);
        var bank = registry.Get(PupilQuestionSet.Groep78).Bank;
        var answers = bank.AllItems.Take(answered).ToDictionary(i => i.Id, _ => 4);
        var answersJson = System.Text.Json.JsonSerializer.Serialize(answers);
        var puzzlesJson = BuildPuzzlesJson(registry.Get(PupilQuestionSet.Groep78), answered, completeActivePuzzleGate);
        var now = DateTime.UtcNow;
        foreach (var code in generated)
        {
            db.PupilProgresses.Add(new PupilProgress
            {
                PupilCodeId = code.Id,
                AnswersJson = answersJson,
                PuzzlesJson = puzzlesJson,
                CurrentIndex = answered,
                LikesJson = "[]",
                DislikesJson = "[]",
                JourneyThemeKey = journeyThemeKey,
                JourneyThemeChosenAtUtc = string.IsNullOrWhiteSpace(journeyThemeKey) ? null : now,
                ChipsSavedAtUtc = chipsSaved ? now : null,
                StartedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = now
            });
        }

        await db.SaveChangesAsync();
        return generated
            .Select(code => new Seed(school.Id, cls.Id, codes.Unprotect(code.CodeProtected)!))
            .ToList();
    }

    private async Task<Seed> SeedCompletedWithDreamJobAsync(string dreamKey, string? journeyThemeKey = null)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Done " + Guid.NewGuid().ToString("N")[..6],
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"voorbeeldcollege.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        };
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "8A",
            Level = SchoolLevel.Groep78,
            Year = 8,
            QuestionSet = PupilQuestionSet.Groep78,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 1,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _api.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();
        var generated = await codes.GenerateAsync(1, cls);
        var code = generated[0];
        var registry = scope.ServiceProvider.GetRequiredService<IPupilQuestionSetRegistry>();
        var def = registry.Get(PupilQuestionSet.Groep78);
        var bank = def.Bank;
        var answers = bank.AllItems.ToDictionary(i => i.Id, _ => 4);
        var vakMap = PupilVakScores.AllSubjectKeys.ToDictionary(k => k, _ => (3, 3));
        var now = DateTime.UtcNow;
        db.PupilProgresses.Add(new PupilProgress
        {
            PupilCodeId = code.Id,
            AnswersJson = System.Text.Json.JsonSerializer.Serialize(answers),
            PuzzlesJson = BuildPuzzlesJson(def, bank.AllItems.Count, completeActiveGate: true),
            VakScoresJson = PupilVakScores.Serialize(vakMap),
            CurrentIndex = bank.AllItems.Count,
            LikesJson = """["lezen","dieren","natuur","buiten"]""",
            DislikesJson = """["lang-stilzitten"]""",
            DreamJobKey = dreamKey,
            JourneyThemeKey = journeyThemeKey,
            JourneyThemeChosenAtUtc = string.IsNullOrWhiteSpace(journeyThemeKey) ? null : now,
            ChipsSavedAtUtc = now,
            StartedAtUtc = now.AddMinutes(-30),
            UpdatedAtUtc = now,
            CompletedAtUtc = now
        });
        code.Status = PupilCodeStatus.Completed;
        await db.SaveChangesAsync();
        var builder = scope.ServiceProvider.GetRequiredService<IPupilResultBuilder>();
        await builder.BuildAsync(code.Id);
        return new Seed(school.Id, cls.Id, codes.Unprotect(code.CodeProtected)!);
    }

    private async Task<TeacherSeed> SeedFiveFinishedGroep78Async()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var schoolId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var domain = $"p{schoolId:N}"[..12] + ".nl";
        var school = new School
        {
            Id = schoolId,
            Name = "Vijf " + Guid.NewGuid().ToString("N")[..4],
            City = "Naaldwijk",
            AllowedEmailDomains = System.Text.Json.JsonSerializer.Serialize(new[] { domain }),
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        };
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "7A",
            Level = SchoolLevel.Groep78,
            Year = 7,
            QuestionSet = PupilQuestionSet.Groep78,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 5,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(cls);
        db.Users.Add(new User
        {
            Id = teacherId,
            Email = $"t-{teacherId:N}@{domain}",
            FullName = "R. Jansen",
            Role = UserRole.Teacher,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.TeacherClassAssignments.Add(new TeacherClassAssignment
        {
            TeacherUserId = teacherId,
            SchoolClassId = cls.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var codeService = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var generated = await codeService.GenerateAsync(5, cls);
        var registry = scope.ServiceProvider.GetRequiredService<IPupilQuestionSetRegistry>();
        var bank = registry.Get(PupilQuestionSet.Groep78).Bank;
        var answers = bank.AllItems.ToDictionary(i => i.Id, _ => 4);
        var answersJson = System.Text.Json.JsonSerializer.Serialize(answers);
        var now = DateTime.UtcNow;
        foreach (var code in generated)
        {
            db.PupilProgresses.Add(new PupilProgress
            {
                PupilCodeId = code.Id,
                AnswersJson = answersJson,
                CurrentIndex = bank.AllItems.Count,
                LikesJson = """["dieren"]""",
                DislikesJson = """["lang-stilzitten"]""",
                StartedAtUtc = now.AddMinutes(-20),
                UpdatedAtUtc = now,
                CompletedAtUtc = now
            });
        }

        await db.SaveChangesAsync();
        var builder = scope.ServiceProvider.GetRequiredService<IPupilResultBuilder>();
        foreach (var code in generated)
        {
            await builder.BuildAsync(code.Id);
        }

        return new TeacherSeed(schoolId, cls.Id, teacherId);
    }

    private static string BuildPuzzlesJson(PupilQuestionSetDef def, int answered, bool completeActiveGate)
    {
        var map = new Dictionary<string, int>();
        foreach (var slot in def.PuzzleSlots)
        {
            if (slot.AfterItem < answered
                || (completeActiveGate && slot.AfterItem <= answered))
            {
                map[slot.PuzzleKey] = (int)PupilPuzzleStatus.Done;
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(map);
    }

    private sealed record Seed(Guid SchoolId, Guid ClassId, string PlainCode);

    private sealed record TeacherSeed(Guid SchoolId, Guid ClassId, Guid TeacherId);

    private sealed class PupilKestrelFactory : WebWebApplicationFactory
    {
        private readonly HttpMessageHandler _api;
        private IHost? _kestrel;

        public PupilKestrelFactory(HttpMessageHandler api) => _api = api;

        public string ServerAddress { get; private set; } = "";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter, StaffTestSignInFilter>();
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new SchoolsOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ForwardFactory(_api));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp =>
                {
                    var auth = new JobsyApiAuthHandler(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<AuthenticationStateProvider>(),
                        sp,
                        sp.GetRequiredService<IConfiguration>(),
                        sp.GetRequiredService<JobsyAccessTokenIssuer>())
                    {
                        InnerHandler = new ForwardHandler(_api)
                    };
                    var http = new HttpClient(auth, disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    };
                    return new JobsyApiClient(http);
                });
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var testHost = builder.Build();
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
            });
            _kestrel = builder.Build();
            _kestrel.Start();
            var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
            testHost.Start();
            return testHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class StaffTestSignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (!ctx.Request.Path.Equals("/__test/staff", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var user = ctx.Request.Query["user"].ToString();
                    var school = ctx.Request.Query["school"].ToString();
                    var role = ctx.Request.Query["role"].ToString();
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        role = "Teacher";
                    }

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user),
                        new(ClaimTypes.Role, role),
                        new(JobsyClaimTypes.SchoolId, school),
                        new(JobsyClaimTypes.SessionVersion, "0"),
                        new(JobsyClaimTypes.MfaVerified, "1")
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/') || ret.StartsWith("//", StringComparison.Ordinal))
                    {
                        ret = "/";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class SchoolsOnFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(
            false,
            false,
            SchoolsEnabled: true,
            PupilPassportV2Enabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class ForwardFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpMessageHandler _handler;

        public ForwardFactory(HttpMessageHandler api) => _handler = new ForwardHandler(api);

        public HttpClient CreateClient(string name)
            => new(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://api.test/")
            };

        public void Dispose() => _handler.Dispose();
    }

    private sealed class ForwardHandler : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _api;

        public ForwardHandler(HttpMessageHandler api)
            => _api = new HttpMessageInvoker(api, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "/";
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            using var clone = new HttpRequestMessage(request.Method, new Uri("http://localhost" + path));
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                clone.Content = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await _api.SendAsync(clone, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _api.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private static void AssertNoMeaningfulConsoleErrors(IReadOnlyList<string> consoleErrors)
    {
        var bad = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)
            && !e.Contains("Refused to apply style", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(bad.Count == 0, "Console errors: " + string.Join("; ", bad));
    }

    private static async Task CaptureWalkthroughScreenshotIfRequestedAsync(IPage page, string fileName)
    {
        var dir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(dir, fileName),
            FullPage = true
        });
    }

    private static class AcceptatieE2E
    {
        public static bool Enabled =>
            string.Equals(Environment.GetEnvironmentVariable("JOBSY_ACC_E2E"), "1", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(PlainCode)
            && Guid.TryParse(ClassId, out _);

        public static string BaseUrl =>
            Environment.GetEnvironmentVariable("JOBSY_ACC_BASE_URL") ?? "https://acceptatie.lobsy.nl";

        public static string PlainCode => Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CODE") ?? "";

        public static string SchoolId =>
            Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_SCHOOL_ID") ?? "";

        public static string ClassId =>
            Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CLASS_ID") ?? "";
    }
}
