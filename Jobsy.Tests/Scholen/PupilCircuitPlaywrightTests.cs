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
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
        await page.WaitForTimeoutAsync(3_000);
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
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
        await page.WaitForTimeoutAsync(3_000);
        Assert.Contains("/leerling/reis", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        await Assertions.Expect(page.Locator(".ll-question__meta")).ToContainTextAsync("Vraag 2");

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
                await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).ClickAsync();
                await page.WaitForFunctionAsync(
                    "() => !location.pathname.includes('/leerling/eiland')");
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
            await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).ClickAsync();
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

    [Fact(Timeout = 300_000)]
    public async Task Groep78_zee_route_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(3, answered: 0, chipsSaved: false, completeActivePuzzleGate: true);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_ROUTE_CAPTURE_DIR");

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                IsMobile = width < 600,
                HasTouch = width < 600,
                DeviceScaleFactor = width < 600 ? 2 : 1,
            });
            page.SetDefaultTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, chooseDefaultWorld: true);
            await page.WaitForURLAsync("**/leerling/route**", DomReady);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "De route" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Naar het Koraalrif" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Andere wereld kiezen" })).ToBeVisibleAsync();
            var speak = width >= 600
                ? page.Locator(".rt-coach [data-read-aloud]")
                : page.Locator(".m-only .guide [data-read-aloud]");
            await Assertions.Expect(speak).ToBeVisibleAsync();
            if (!string.IsNullOrEmpty(captureDir))
            {
                Directory.CreateDirectory(captureDir);
                await page.Locator(".btn-main").WaitForAsync(new() { Timeout = 30_000 });
                await speak.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-01-zee-route-{tag}.png"),
                    FullPage = true,
                });
            }

            await page.CloseAsync();
        }

        var desktop = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        var url = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(desktop, url, pupil.PlainCode, pupil.ClassId, chooseDefaultWorld: true);
        await desktop.WaitForURLAsync("**/leerling/route**", DomReady);
        await desktop.GetByRole(AriaRole.Link, new() { Name = "Naar het Koraalrif" }).ClickAsync();
        await desktop.WaitForURLAsync("**/leerling/reis**", DomReady);
        await desktop.CloseAsync();

        var mobile = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
        });
        await LoginAsync(mobile, url, pupil.PlainCode, pupil.ClassId, chooseDefaultWorld: true);
        await mobile.WaitForURLAsync("**/leerling/route**", DomReady);
        await mobile.GetByRole(AriaRole.Link, new() { Name = "Andere wereld kiezen" }).ClickAsync();
        await mobile.WaitForURLAsync("**/leerling/wereld**", DomReady);
        await mobile.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_tuin_route_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(
            3,
            answered: 0,
            chipsSaved: false,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.Eilandtuin);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_TUIN_ROUTE_CAPTURE_DIR");

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                IsMobile = width < 600,
                HasTouch = width < 600,
                DeviceScaleFactor = width < 600 ? 2 : 1,
            });
            page.SetDefaultTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Eilandtuin);
            await page.WaitForURLAsync("**/leerling/route**", DomReady);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "De route" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Zo ga je van de steiger naar jouw eiland.")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Naar de Bloemenbaai" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Andere wereld kiezen" })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".ll-g78-shell.t-tuin")).ToBeVisibleAsync();
            var speak = width >= 600
                ? page.Locator(".rt-coach [data-read-aloud]")
                : page.Locator(".m-only .guide [data-read-aloud]");
            await Assertions.Expect(speak).ToBeVisibleAsync();
            if (!string.IsNullOrEmpty(captureDir))
            {
                Directory.CreateDirectory(captureDir);
                await page.Locator(".btn-main").WaitForAsync(new() { Timeout = 30_000 });
                await speak.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-04-eilandtuin-route-{tag}.png"),
                    FullPage = true,
                });
            }

            await page.CloseAsync();
        }

        var desktop = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        var url = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(desktop, url, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Eilandtuin);
        await desktop.WaitForURLAsync("**/leerling/route**", DomReady);
        await desktop.GetByRole(AriaRole.Link, new() { Name = "Naar de Bloemenbaai" }).ClickAsync();
        await desktop.WaitForURLAsync("**/leerling/reis**", DomReady);
        await desktop.CloseAsync();

        var mobile = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
        });
        await LoginAsync(mobile, url, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Eilandtuin);
        await mobile.WaitForURLAsync("**/leerling/route**", DomReady);
        await mobile.GetByRole(AriaRole.Link, new() { Name = "Andere wereld kiezen" }).ClickAsync();
        await mobile.WaitForURLAsync("**/leerling/wereld**", DomReady);
        await mobile.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_ster_route_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(
            3,
            answered: 0,
            chipsSaved: false,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_STER_ROUTE_CAPTURE_DIR")
            ?? "/opt/cursor/artifacts/groep78-ster-route-capture";
        Directory.CreateDirectory(captureDir);

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                IsMobile = width < 600,
                HasTouch = width < 600,
                DeviceScaleFactor = width < 600 ? 2 : 1,
            });
            page.SetDefaultTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Sterrenkust);
            await page.WaitForURLAsync("**/leerling/route**", DomReady);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "De route" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Je route is een sterrenbeeld. Elke ster is een stop.")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Naar het Duinpad" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Andere wereld kiezen" })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".ll-g78-shell.t-ster")).ToBeVisibleAsync();
            var speak = width >= 600
                ? page.Locator(".rt-coach [data-read-aloud]")
                : page.Locator(".m-only .guide [data-read-aloud]");
            await Assertions.Expect(speak).ToBeVisibleAsync();
            await page.Locator(".btn-main").WaitForAsync(new() { Timeout = 30_000 });
            await speak.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(captureDir, $"built-07-sterrenkust-route-{tag}.png"),
                FullPage = true,
            });
            await page.CloseAsync();
        }

        WriteSterRouteSideBySideComparisons(captureDir);

        var desktop = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        var url = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(desktop, url, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        await desktop.WaitForURLAsync("**/leerling/route**", DomReady);
        await desktop.GetByRole(AriaRole.Link, new() { Name = "Naar het Duinpad" }).ClickAsync();
        await desktop.WaitForURLAsync("**/leerling/reis**", DomReady);
        await desktop.Locator(".ll-card").WaitForAsync();
        await desktop.CloseAsync();

        var mobile = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
        });
        await LoginAsync(mobile, url, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        await mobile.WaitForURLAsync("**/leerling/route**", DomReady);
        await mobile.GetByRole(AriaRole.Link, new() { Name = "Andere wereld kiezen" }).ClickAsync();
        await mobile.WaitForURLAsync("**/leerling/wereld**", DomReady);
        await mobile.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_tuin_vraag_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(
            3,
            answered: 0,
            chipsSaved: false,
            completeActivePuzzleGate: true,
            journeyThemeKey: PupilJourneyTheme.Eilandtuin);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_TUIN_VRAAG_CAPTURE_DIR");

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                IsMobile = width < 600,
                HasTouch = width < 600,
                DeviceScaleFactor = width < 600 ? 2 : 1,
            });
            page.SetDefaultTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Eilandtuin);
            await page.WaitForURLAsync("**/leerling/route**", DomReady);
            await page.GetByRole(AriaRole.Link, new() { Name = "Naar de Bloemenbaai" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/reis**", DomReady);
            await page.Locator("[data-testid=ll-tuin-vraag-band]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Attached,
                Timeout = 60_000,
            });
            await Assertions.Expect(page.Locator(".ll-g78-shell.t-tuin")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".vr-ans").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".vr-qrow [data-read-aloud]")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".vr-qmeta .kicker")).ToHaveTextAsync("De Bloemenbaai");
            if (!string.IsNullOrEmpty(captureDir))
            {
                Directory.CreateDirectory(captureDir);
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-05-eilandtuin-vraag-{tag}.png"),
                    FullPage = true,
                });
            }

            await page.CloseAsync();
        }

        var back = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        back.SetDefaultTimeout(90_000);
        var url = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(back, url, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Eilandtuin);
        await back.WaitForURLAsync("**/leerling/route**", DomReady);
        await back.GetByRole(AriaRole.Link, new() { Name = "Naar de Bloemenbaai" }).ClickAsync();
        await back.WaitForURLAsync("**/leerling/reis**", DomReady);
        await back.Locator("[data-testid=ll-vraag-prev]").ClickAsync();
        await back.WaitForURLAsync("**/leerling/route**", DomReady);
        await back.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_zee_vraag_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(3, answered: 0, chipsSaved: false, completeActivePuzzleGate: true);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_VRAAG_CAPTURE_DIR");

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                IsMobile = width < 600,
                HasTouch = width < 600,
                DeviceScaleFactor = width < 600 ? 2 : 1,
            });
            page.SetDefaultTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, chooseDefaultWorld: true);
            await page.WaitForURLAsync("**/leerling/route**", DomReady);
            await page.GetByRole(AriaRole.Link, new() { Name = "Naar het Koraalrif" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/reis**", DomReady);
            await page.Locator("[data-testid=ll-zee-vraag-band]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Attached,
                Timeout = 60_000,
            });
            await Assertions.Expect(page.Locator(".vr-ans").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".vr-qrow [data-read-aloud]")).ToBeVisibleAsync();
            if (!string.IsNullOrEmpty(captureDir))
            {
                Directory.CreateDirectory(captureDir);
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-02-zee-vraag-{tag}.png"),
                    FullPage = true,
                });
            }

            await page.CloseAsync();
        }

        var back = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        back.SetDefaultTimeout(90_000);
        var url = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(back, url, pupil.PlainCode, pupil.ClassId, chooseDefaultWorld: true);
        await back.WaitForURLAsync("**/leerling/route**", DomReady);
        await back.GetByRole(AriaRole.Link, new() { Name = "Naar het Koraalrif" }).ClickAsync();
        await back.WaitForURLAsync("**/leerling/reis**", DomReady);
        await back.Locator("[data-testid=ll-vraag-prev]").ClickAsync();
        await back.WaitForURLAsync("**/leerling/route**", DomReady);
        await back.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_tuin_puzzel_intro_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(
            3,
            answered: 15,
            chipsSaved: false,
            journeyThemeKey: PupilJourneyTheme.Eilandtuin);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_TUIN_PUZZEL_INTRO_CAPTURE_DIR");

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                IsMobile = width < 600,
                HasTouch = width < 600,
                DeviceScaleFactor = width < 600 ? 2 : 1,
            });
            page.SetDefaultTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Eilandtuin);
            await page.WaitForURLAsync("**/leerling/puzzel-intro/schelpenrij**", DomReady);
            await page.Locator("[data-testid=ll-tuin-puzzel-intro-band]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Attached,
                Timeout = 60_000,
            });
            await Assertions.Expect(page.Locator(".ll-g78-shell.t-tuin")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Vind het patroon in de rij schelpen in de koraaltuin.")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".pi-foot [data-read-aloud]")).ToBeVisibleAsync();
            if (!string.IsNullOrEmpty(captureDir))
            {
                Directory.CreateDirectory(captureDir);
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-06-eilandtuin-puzzel-intro-{tag}.png"),
                    FullPage = true,
                });
            }

            var coach = width >= 600
                ? page.Locator(".pi-coach-aside [data-read-aloud]")
                : page.Locator(".m-only .guide [data-read-aloud]");
            await coach.ClickAsync();

            await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", DomReady);
            await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
            await page.CloseAsync();
        }
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_zee_puzzel_intro_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(3, answered: 15, chipsSaved: false);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_PUZZEL_INTRO_CAPTURE_DIR");

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                IsMobile = width < 600,
                HasTouch = width < 600,
                DeviceScaleFactor = width < 600 ? 2 : 1,
            });
            page.SetDefaultTimeout(90_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, chooseDefaultWorld: true);
            await page.WaitForURLAsync("**/leerling/puzzel-intro/schelpenrij**", DomReady);
            await page.Locator("[data-testid=ll-zee-puzzel-intro-band]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Attached,
                Timeout = 60_000,
            });
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".pi-foot [data-read-aloud]")).ToBeVisibleAsync();
            if (!string.IsNullOrEmpty(captureDir))
            {
                Directory.CreateDirectory(captureDir);
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-03-zee-puzzel-intro-{tag}.png"),
                    FullPage = true,
                });
                if (width == 390)
                {
                    await page.ScreenshotAsync(new()
                    {
                        Path = Path.Combine(captureDir, "built-03-zee-puzzel-intro-390-viewport.png"),
                        FullPage = false,
                    });
                }
            }

            await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", DomReady);
            await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
            await page.CloseAsync();
        }
    }

    [Fact(Timeout = 1_200_000)]
    public async Task Groep78_full_pupil_journey_both_worlds_1440_and_390()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        var report = new List<string>();
        var allReport = new List<string>();
        Directory.CreateDirectory("/opt/cursor/artifacts");
        var reportPath = "/opt/cursor/artifacts/groep78-e2e-button-results.txt";
        if (File.Exists(reportPath))
        {
            File.Delete(reportPath);
        }

        foreach (var (theme, routeBtn, bandTestId, width, height, mobile, reisOnly) in new[]
                 {
                     (PupilJourneyTheme.ZeeExpeditie, "Naar het Koraalrif", "ll-zee-vraag-band", 1440, 900, false, false),
                     (PupilJourneyTheme.ZeeExpeditie, "Naar het Koraalrif", "ll-zee-vraag-band", 390, 844, true, false),
                     (PupilJourneyTheme.Eilandtuin, "Naar de Bloemenbaai", "ll-tuin-vraag-band", 1440, 900, false, false),
                     (PupilJourneyTheme.Eilandtuin, "Naar de Bloemenbaai", "ll-tuin-vraag-band", 390, 844, true, false),
                     (PupilJourneyTheme.Sterrenkust, "Naar het Duinpad", (string?)null, 1440, 900, false, true),
                     (PupilJourneyTheme.Sterrenkust, "Naar het Duinpad", (string?)null, 390, 844, true, true),
                 })
        {
            var seed = await SeedGroep78AtAsync(1, answered: 0, chipsSaved: false, completeActivePuzzleGate: true, journeyThemeKey: theme);
            var pupil = seed[0];
            var tag = $"{theme}-{width}";
            var consoleErrors = new List<string>();
            var failedRequests = new List<string>();

            await using var context = mobile
                ? await NewMobileContextAsync()
                : await _browser!.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
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

            async Task Step(string name, Func<Task> act)
            {
                try
                {
                    consoleErrors.Clear();
                    failedRequests.Clear();
                    await act();
                    await AssertGroep78UiCleanAsync(page, consoleErrors, failedRequests);
                    report.Add($"PASS | {tag} | {name}");
                }
                catch (Exception ex)
                {
                    report.Add($"FAIL | {tag} | {name} | {ex.Message}");
                }
            }

            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";

            try
            {
                await Step("Login + choose world", async () =>
                {
                    await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                    await page.WaitForURLAsync("**/leerling/route**", DomReady);
                });

                await Step("Footer Privacy link", async () =>
                {
                    var link = page.GetByRole(AriaRole.Link, new() { Name = "Privacyverklaring" });
                    await Assertions.Expect(link).ToBeVisibleAsync();
                    var href = await link.GetAttributeAsync("href");
                    Assert.False(string.IsNullOrWhiteSpace(href));
                });

                await Step("Footer Accessibility link", async () =>
                {
                    var link = page.GetByRole(AriaRole.Link, new() { Name = "Toegankelijkheid" });
                    await Assertions.Expect(link).ToBeVisibleAsync();
                    var href = await link.GetAttributeAsync("href");
                    Assert.False(string.IsNullOrWhiteSpace(href));
                });

                await Step("Route coach speaker", async () =>
                {
                    var speak = width >= 600
                        ? page.Locator(".rt-coach [data-read-aloud]")
                        : page.Locator(".m-only .guide [data-read-aloud]");
                    await speak.ClickAsync();
                });

                await Step("Andere wereld kiezen + back", async () =>
                {
                    await page.GetByRole(AriaRole.Link, new() { Name = "Andere wereld kiezen" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/wereld**", DomReady);
                    await page.Locator($"[data-testid=ll-wereld-{theme}]").ClickAsync();
                    await page.WaitForURLAsync("**/leerling/route**", DomReady);
                });

                await Step("Pauze from route", async () =>
                {
                    await page.GetByRole(AriaRole.Button, new() { Name = "Pauze" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/stop**", DomReady);
                    await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                    await page.WaitForURLAsync("**/leerling/route**", DomReady);
                });

                await Step("Route main CTA → reis", async () =>
                {
                    await page.GetByRole(AriaRole.Link, new() { Name = routeBtn }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/reis**", DomReady);
                    if (!string.IsNullOrEmpty(bandTestId))
                    {
                        await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                    }
                    else
                    {
                        await page.Locator(".ll-card").WaitForAsync();
                    }
                });

                if (reisOnly)
                {
                    continue;
                }

                await Step("Vorige Q1 → route", async () =>
                {
                    await page.Locator("[data-testid=ll-vraag-prev]").ClickAsync();
                    await page.WaitForURLAsync("**/leerling/route**", DomReady);
                    await page.GetByRole(AriaRole.Link, new() { Name = routeBtn }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/reis**", DomReady);
                    await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                });

                await Step("Question + coach speakers", async () =>
                {
                    await page.Locator(".vr-qrow [data-read-aloud]").ClickAsync();
                    var coach = width >= 600
                        ? page.Locator(".vr-coach-aside [data-read-aloud]")
                        : page.Locator(".m-only .guide [data-read-aloud]");
                    await coach.ClickAsync();
                });

                await Step("Answer Q1 (Soms)", async () => await AnswerGroep78ThemedAsync(page));

                await Step("Vorige → Q1 with prior answer", async () =>
                {
                    await page.Locator("[data-testid=ll-vraag-prev]").ClickAsync();
                    await Assertions.Expect(page.Locator(".vr-qmeta")).ToContainTextAsync("Vraag 1 van 60");
                    await Assertions.Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Soms" })).ToHaveAttributeAsync("aria-checked", "true");
                });

                await Step("Change Q1 answer (Ja)", async () =>
                {
                    await page.GetByRole(AriaRole.Radio, new() { Name = "Ja" }).ClickAsync();
                    await Assertions.Expect(page.Locator(".vr-qmeta")).ToContainTextAsync("Vraag 2 van 60");
                });

                await Step("Answer through to Schelpenrij intro", async () =>
                {
                    for (var i = 0; i < 20
                         && !page.Url.Contains("/puzzel-intro/", StringComparison.OrdinalIgnoreCase); i++)
                    {
                        await AnswerGroep78ThemedAsync(page);
                    }

                    await page.WaitForURLAsync("**/leerling/puzzel-intro/schelpenrij**", UrlCommitted);
                    var introBand = theme == PupilJourneyTheme.Eilandtuin
                        ? "ll-tuin-puzzel-intro-band"
                        : "ll-zee-puzzel-intro-band";
                    await page.Locator($"[data-testid={introBand}]").WaitForAsync();
                });

                await Step("Puzzle intro speaker", async () =>
                {
                    await page.Locator(".pi-foot [data-read-aloud]").ClickAsync();
                    var coach = width >= 600
                        ? page.Locator(".pi-coach-aside [data-read-aloud]")
                        : page.Locator(".m-only .guide [data-read-aloud]");
                    await coach.ClickAsync();
                });

                await Step("Start de puzzel", async () =>
                {
                    await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", UrlCommitted);
                    await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
                });
            }
            finally
            {
                await page.CloseAsync();
                allReport.AddRange(report);
                await File.AppendAllLinesAsync(reportPath, report);
                report.Clear();
            }
        }

        var failures = allReport.Where(l => l.StartsWith("FAIL", StringComparison.Ordinal)).ToList();
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static async Task AnswerGroep78ThemedAsync(IPage page)
    {
        if (page.Url.Contains("/puzzel-intro/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var before = await page.Locator(".vr-qmeta").InnerTextAsync();
        await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).ClickAsync();
        await page.WaitForFunctionAsync(
            """
            prev => {
                const meta = document.querySelector('.vr-qmeta');
                const path = location.pathname;
                return (meta && meta.innerText !== prev)
                    || path.includes('/puzzel-intro/')
                    || path.includes('/puzzel/');
            }
            """,
            before,
            new PageWaitForFunctionOptions { Timeout = 20_000 });
    }

    private static async Task AssertGroep78UiCleanAsync(
        IPage page,
        IReadOnlyList<string> consoleErrors,
        IReadOnlyList<string> failedRequests)
    {
        var body = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("_saveError", body, StringComparison.Ordinal);
        Assert.DoesNotContain("_saved", body, StringComparison.Ordinal);
        var untranslated = UntranslatedKeyPattern().Match(body);
        if (untranslated.Success)
        {
            throw new InvalidOperationException("Untranslated key visible: " + untranslated.Value);
        }

        Assert.Equal(0, await page.Locator(".vr-error").CountAsync());
        Assert.Equal(0, await page.Locator(".ll-error").CountAsync());
        var noisyConsole = consoleErrors
            .Where(e => !e.Contains("speechSynthesis", StringComparison.OrdinalIgnoreCase)
                        && !e.Contains("cors", StringComparison.OrdinalIgnoreCase)
                        && !e.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase)
                        && !e.Contains("fonts.googleapis.com", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (noisyConsole.Count > 0)
        {
            throw new InvalidOperationException("Console errors: " + string.Join(" | ", noisyConsole));
        }

        var noisyFailed = failedRequests
            .Where(r => !r.Contains("favicon", StringComparison.OrdinalIgnoreCase)
                        && !r.Contains(".gz", StringComparison.OrdinalIgnoreCase)
                        && !r.Contains("fonts.googleapis.com", StringComparison.OrdinalIgnoreCase)
                        && !r.Contains(" fonts.gstatic.com", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (noisyFailed.Count > 0)
        {
            throw new InvalidOperationException("Failed requests: " + string.Join(" | ", noisyFailed));
        }
    }

    private static Regex UntranslatedKeyPattern() => new(@"Leerling\.[A-Z][\w.]+", RegexOptions.Compiled);

    private static void WriteSterRouteSideBySideComparisons(string captureDir)
    {
        var compareDir = "/opt/cursor/artifacts/groep78-ster-route-compare";
        Directory.CreateDirectory(compareDir);
        var repoRoot = FindRepoRoot();
        foreach (var (mockRel, builtName, outName) in new[]
                 {
                     (
                         "docs/mockups/leerling/werelden/renders/desktop/07-sterrenkust-route.png",
                         "built-07-sterrenkust-route-1440.png",
                         "side-by-side-07-sterrenkust-route-1440.png"),
                     (
                         "docs/mockups/leerling/werelden/renders/mobile/07-sterrenkust-route.png",
                         "built-07-sterrenkust-route-390.png",
                         "side-by-side-07-sterrenkust-route-390.png"),
                 })
        {
            var mockPath = Path.Combine(compareDir, Path.GetFileName(mockRel));
            if (!File.Exists(mockPath))
            {
                TryGitShowMockup(repoRoot, mockRel, mockPath);
            }

            var builtPath = Path.Combine(captureDir, builtName);
            var outPath = Path.Combine(compareDir, outName);
            if (!File.Exists(mockPath) || !File.Exists(builtPath))
            {
                continue;
            }

            var script = Path.Combine(repoRoot, "tools", "banenkaart-lagen-capture", "side_by_side.py");
            if (!File.Exists(script))
            {
                continue;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                ArgumentList = { script, mockPath, builtPath, outPath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })?.WaitForExit(30_000);
        }
    }

    private static void TryGitShowMockup(string repoRoot, string gitPath, string dest)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("show");
            psi.ArgumentList.Add($"pr-878-mockups:{gitPath}");
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
            {
                return;
            }

            var bytes = proc.StandardOutput.BaseStream;
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            using var fs = File.Create(dest);
            bytes.CopyTo(fs);
            proc.WaitForExit(5_000);
        }
        catch
        {
            // Mockups are optional when pr-878-mockups ref is missing.
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

        return "/workspace";
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
        => LoginAsync(page, loginUrl, code, classId, journeyThemeKey: PupilJourneyTheme.ZeeExpeditie);

    private static Task LoginAsync(
        IPage page,
        string loginUrl,
        string code,
        Guid classId,
        bool chooseDefaultWorld)
        => LoginAsync(
            page,
            loginUrl,
            code,
            classId,
            chooseDefaultWorld ? PupilJourneyTheme.ZeeExpeditie : null);

    private static async Task LoginAsync(
        IPage page,
        string loginUrl,
        string code,
        Guid classId,
        string? journeyThemeKey)
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
                        || url.Contains("/leerling/route", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/start", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/puzzel/", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/puzzel-intro/", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase),
                    UrlCommitted);
                if (!string.IsNullOrEmpty(journeyThemeKey)
                    && page.Url.Contains("/leerling/wereld", StringComparison.OrdinalIgnoreCase))
                {
                    await page.Locator($"[data-testid=ll-wereld-{journeyThemeKey}]").ClickAsync();
                    await page.WaitForURLAsync(
                        url => url.Contains("/leerling/route", StringComparison.OrdinalIgnoreCase)
                            || url.Contains("/leerling/start", StringComparison.OrdinalIgnoreCase)
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
        var meta = page.Locator(".ll-question__meta");
        await meta.WaitForAsync();
        var before = await meta.InnerTextAsync();
        await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).ClickAsync();
        await page.WaitForFunctionAsync(
            """
            prev => {
                const meta = document.querySelector('.ll-question__meta');
                const path = location.pathname;
                return (meta && meta.innerText !== prev) || !path.includes('/leerling/reis');
            }
            """,
            before);
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
        await page.WaitForURLAsync("**/leerling/puzzel-intro/schelpenrij**", UrlCommitted);
        await page.Locator("[data-testid=ll-zee-puzzel-intro-band]").WaitForAsync();
        var elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds;
        Assert.True(elapsedMs < 3_000, $"puzzle intro load took {elapsedMs:F0} ms at {width}x{height}");
        await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", UrlCommitted);
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Overslaan" })).ToBeVisibleAsync();
        await page.CloseAsync();
    }

    private async Task<IReadOnlyList<Seed>> SeedGroep78AtAsync(
        int pupils,
        int answered,
        bool chipsSaved,
        bool completeActivePuzzleGate = false,
        string journeyThemeKey = PupilJourneyTheme.ZeeExpeditie)
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
                JourneyThemeKey = journeyThemeKey,
                LikesJson = "[]",
                DislikesJson = "[]",
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

    private async Task<Seed> SeedCompletedWithDreamJobAsync(string dreamKey)
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
        var bank = registry.Get(PupilQuestionSet.Groep78).Bank;
        var answers = bank.AllItems.ToDictionary(i => i.Id, _ => 4);
        var now = DateTime.UtcNow;
        db.PupilProgresses.Add(new PupilProgress
        {
            PupilCodeId = code.Id,
            AnswersJson = System.Text.Json.JsonSerializer.Serialize(answers),
            CurrentIndex = bank.AllItems.Count,
            LikesJson = """["dieren"]""",
            DislikesJson = """["lang-stilzitten"]""",
            DreamJobKey = dreamKey,
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
            => new(_handler, disposeHandler: false);

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
