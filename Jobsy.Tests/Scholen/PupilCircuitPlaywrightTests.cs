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
using Jobsy.Web.Scholen;
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
    private static readonly object SharedWebGate = new();
    private static PupilKestrelFactory? _sharedWeb;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    private const string Groep78ReadAloudInitScript =
        """
        () => {
          const voices = [{
            lang: "nl-NL",
            name: "Test NL",
            localService: true,
            voiceURI: "test-nl",
            default: true
          }];
          class MockUtterance {
            constructor(text) {
              this.text = String(text || "");
              this.lang = "nl-NL";
              this.rate = 1;
              this.onstart = null;
              this.onend = null;
              this.onerror = null;
            }
          }
          window.SpeechSynthesisUtterance = MockUtterance;
          const synth = {
            speaking: false,
            getVoices: function () { return voices; },
            speak: function (utter) {
              this.speaking = true;
              const self = this;
              setTimeout(function () {
                if (utter && typeof utter.onstart === "function") {
                  utter.onstart({});
                }
                if (utter && typeof utter.onend === "function") {
                  utter.onend({});
                }
                self.speaking = false;
              }, 0);
            },
            cancel: function () { this.speaking = false; },
            addEventListener: function () {},
            removeEventListener: function () {}
          };
          window.__lobsyReadAloudSynth = synth;
          try {
            Object.defineProperty(window, "speechSynthesis", {
              value: synth,
              configurable: true,
              writable: false
            });
          } catch (e) {
            window.speechSynthesis = synth;
          }
          const reapply = () => {
            window.__lobsyReadAloudSynth = synth;
            if (window.speechSynthesis !== synth) {
              try { window.speechSynthesis = synth; } catch (err) { /* ignore */ }
            }
          };
          reapply();
          window.addEventListener("pageshow", reapply);
        }
        """;

    public PupilCircuitPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        _ = EnsureWeb();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
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
                await CompleteGroep78IslandAsync(page);
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
            await CompleteGroep78IslandAsync(page);
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
        await desktop.Locator("[data-testid=ll-ster-vraag-band]").WaitForAsync();
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
    public async Task Groep78_ster_vraag_screen_links_and_capture()
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
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_STER_VRAAG_CAPTURE_DIR")
            ?? "/opt/cursor/artifacts/groep78-ster-vraag-capture";
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
            await page.GetByRole(AriaRole.Link, new() { Name = "Naar het Duinpad" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/reis**", DomReady);
            await page.Locator("[data-testid=ll-ster-vraag-band]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Attached,
                Timeout = 60_000,
            });
            await Assertions.Expect(page.Locator(".ll-g78-shell.t-ster")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".vr-ans").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".vr-qrow [data-read-aloud]")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".vr-qmeta .kicker")).ToHaveTextAsync("Het Duinpad");
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(captureDir, $"built-08-sterrenkust-vraag-{tag}.png"),
                FullPage = true,
            });
            await page.CloseAsync();
        }

        WriteSterVraagSideBySideComparisons(captureDir);

        var back = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        back.SetDefaultTimeout(90_000);
        var url = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(back, url, pupil.PlainCode, pupil.ClassId, journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        await back.WaitForURLAsync("**/leerling/route**", DomReady);
        await back.GetByRole(AriaRole.Link, new() { Name = "Naar het Duinpad" }).ClickAsync();
        await back.WaitForURLAsync("**/leerling/reis**", DomReady);
        await back.Locator("[data-testid=ll-vraag-prev]").ClickAsync();
        await back.WaitForURLAsync("**/leerling/route**", DomReady);
        await back.CloseAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_ster_puzzel_intro_screen_links_and_capture()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedGroep78AtAsync(
            3,
            answered: 15,
            chipsSaved: false,
            journeyThemeKey: PupilJourneyTheme.Sterrenkust);
        var pupil = seed[0];
        var baseUrl = EnsureWeb();
        var captureDir = Environment.GetEnvironmentVariable("LEERLING_STER_PUZZEL_INTRO_CAPTURE_DIR")
            ?? "/opt/cursor/artifacts/groep78-ster-puzzel-intro-capture";
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
            await page.WaitForURLAsync("**/leerling/puzzel-intro/schelpenrij**", DomReady);
            await page.Locator("[data-testid=ll-ster-puzzel-intro-band]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Attached,
                Timeout = 60_000,
            });
            await Assertions.Expect(page.Locator(".ll-g78-shell.t-ster")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Vind het patroon in de rij schelpen op het maanstrand.")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".pi-foot [data-read-aloud]")).ToBeVisibleAsync();
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(captureDir, $"built-09-sterrenkust-puzzel-intro-{tag}.png"),
                FullPage = true,
            });

            var coach = width >= 600
                ? page.Locator(".pi-coach-aside [data-read-aloud]")
                : page.Locator(".m-only .guide [data-read-aloud]");
            await coach.ClickAsync();

            await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", DomReady);
            await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
            await page.CloseAsync();
        }

        WriteSterPuzzelIntroSideBySideComparisons(captureDir);
    }

    [Fact(Timeout = 420_000)]
    public async Task Groep78_schelpenrij_puzzle_play_capture_three_worlds()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        var zeeCapture = "/opt/cursor/artifacts/groep78-zee-puzzel-play-capture";
        var tuinCapture = "/opt/cursor/artifacts/groep78-tuin-puzzel-play-capture";
        var sterCapture = "/opt/cursor/artifacts/groep78-ster-puzzel-play-capture";
        Directory.CreateDirectory(zeeCapture);
        Directory.CreateDirectory(tuinCapture);
        Directory.CreateDirectory(sterCapture);

        foreach (var (theme, bandTestId, shellClass, captureDir, builtPrefix) in new[]
                 {
                     (PupilJourneyTheme.ZeeExpeditie, "ll-zee-puzzel-play-band", "t-zee", zeeCapture, "zee"),
                     (PupilJourneyTheme.Eilandtuin, "ll-tuin-puzzel-play-band", "t-tuin", tuinCapture, "tuin"),
                     (PupilJourneyTheme.Sterrenkust, "ll-ster-puzzel-play-band", "t-ster", sterCapture, "ster"),
                 })
        {
            var seed = await SeedGroep78AtAsync(3, answered: 15, chipsSaved: false, journeyThemeKey: theme);
            var pupil = seed[0];
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
                await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                await page.WaitForURLAsync("**/leerling/puzzel-intro/schelpenrij**", DomReady);
                await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
                await page.WaitForURLAsync("**/leerling/puzzel/schelpenrij**", DomReady);
                await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                await Assertions.Expect(page.Locator($".ll-g78-shell.{shellClass}")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator("[data-testid=ll-puzzle-root]")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator("[data-testid=ll-puzzle-shell]")).ToHaveCountAsync(PupilSchelpenrijPuzzle.ShellCount);
                await Assertions.Expect(page.Locator(".pp-card .vr-qrow [data-read-aloud]")).ToBeVisibleAsync();
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-{builtPrefix}-schelpenrij-play-{tag}.png"),
                    FullPage = true,
                });
                await page.CloseAsync();
            }
        }

        WriteZeePuzzelPlaySideBySideComparisons(zeeCapture);
        WriteTuinPuzzelPlaySideBySideComparisons(tuinCapture);
        WriteSterPuzzelPlaySideBySideComparisons(sterCapture);
    }

    [Fact(Timeout = 300_000)]
    public async Task Groep78_result_themed_buttons_three_worlds()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        await EnablePassportV2Async();
        var baseUrl = EnsureWeb();
        var report = new List<string>();
        Directory.CreateDirectory("/opt/cursor/artifacts");
        var reportPath = "/opt/cursor/artifacts/groep78-result-button-results.txt";

        foreach (var (theme, bandTestId, width, height, mobile) in new[]
                 {
                     (PupilJourneyTheme.ZeeExpeditie, "ll-zee-result-band", 1440, 900, false),
                     (PupilJourneyTheme.ZeeExpeditie, "ll-zee-result-band", 390, 844, true),
                     (PupilJourneyTheme.Eilandtuin, "ll-tuin-result-band", 1440, 900, false),
                     (PupilJourneyTheme.Eilandtuin, "ll-tuin-result-band", 390, 844, true),
                     (PupilJourneyTheme.Sterrenkust, "ll-ster-result-band", 1440, 900, false),
                     (PupilJourneyTheme.Sterrenkust, "ll-ster-result-band", 390, 844, true),
                 })
        {
            var pupil = await SeedGroep78CompletedAsync(journeyThemeKey: theme);
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
                await Step("Login to result", async () =>
                {
                    await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                    await page.WaitForURLAsync("**/leerling/dit-ben-jij**", DomReady);
                    await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                });
                await Step("Story speaker", async () =>
                {
                    await page.Locator(".rs-story-card [data-read-aloud]").First.ClickAsync();
                });
                await Step("Tile speakers", async () =>
                {
                    var tiles = page.Locator(".rs-tile [data-read-aloud]");
                    for (var i = 0; i < await tiles.CountAsync(); i++)
                    {
                        await tiles.Nth(i).ClickAsync();
                    }
                });
                await Step("Jobs speaker", async () =>
                {
                    await page.Locator(".rs-likes-card .vr-qrow [data-read-aloud]").ClickAsync();
                });
                await Step("Coach speaker", async () =>
                {
                    var coach = width >= 600
                        ? page.Locator(".rs-coach-aside [data-read-aloud]")
                        : page.Locator(".rs-mobile-coach [data-read-aloud]");
                    await coach.ClickAsync();
                });
                await Step("Pauze", async () =>
                {
                    await page.GetByRole(AriaRole.Button, new() { Name = "Pauze" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/stop**", DomReady);
                    await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                    await page.WaitForURLAsync("**/leerling/dit-ben-jij**", DomReady);
                });
                await Step("Jouw DNA-paspoort link", async () =>
                {
                    var link = page.GetByRole(AriaRole.Link, new() { Name = "Jouw DNA-paspoort" });
                    await Assertions.Expect(link).ToBeVisibleAsync();
                    await Assertions.Expect(link).ToHaveClassAsync(new Regex(@"\bbtn-ghost\b"));
                    if (width < 600)
                    {
                        await Assertions.Expect(link).ToHaveCSSAsync("border-width", "2px");
                    }

                    await link.ClickAsync();
                    await page.WaitForURLAsync("**/leerling/paspoort**", DomReady);
                    await page.GoBackAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    await page.WaitForURLAsync("**/leerling/dit-ben-jij**", DomReady);
                });
                await Step("PDF link", async () =>
                {
                    var link = page.GetByRole(AriaRole.Link, new() { Name = "PDF" });
                    await Assertions.Expect(link).ToHaveAttributeAsync("href", "/leerling/pdf");
                    await Assertions.Expect(link).ToHaveClassAsync(new Regex(@"\bbtn-ghost\b"));
                    if (width < 600)
                    {
                        await Assertions.Expect(link).ToHaveCSSAsync("border-width", "2px");
                    }
                });
                await Step("Check je droombaan", async () =>
                {
                    await page.GetByRole(AriaRole.Link, new() { Name = "Check je droombaan" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/droombaan**", DomReady);
                });
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        await File.WriteAllLinesAsync(reportPath, report);
        var failures = report.Where(l => l.StartsWith("FAIL", StringComparison.Ordinal)).ToList();
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact(Timeout = 420_000)]
    public async Task Groep78_result_screen_capture_three_worlds()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        await EnablePassportV2Async();
        var baseUrl = EnsureWeb();
        var zeeCapture = "/opt/cursor/artifacts/groep78-zee-result-capture";
        var tuinCapture = "/opt/cursor/artifacts/groep78-tuin-result-capture";
        var sterCapture = "/opt/cursor/artifacts/groep78-ster-result-capture";
        Directory.CreateDirectory(zeeCapture);
        Directory.CreateDirectory(tuinCapture);
        Directory.CreateDirectory(sterCapture);
        foreach (var dir in new[] { zeeCapture, tuinCapture, sterCapture })
        {
            foreach (var file in Directory.EnumerateFiles(dir, "built-*-result-*.png"))
            {
                File.Delete(file);
            }
        }

        foreach (var compareDir in new[]
                 {
                     "/opt/cursor/artifacts/groep78-zee-result-compare",
                     "/opt/cursor/artifacts/groep78-tuin-result-compare",
                     "/opt/cursor/artifacts/groep78-ster-result-compare",
                 })
        {
            Directory.CreateDirectory(compareDir);
            foreach (var file in Directory.EnumerateFiles(compareDir, "side-by-side-*.png"))
            {
                File.Delete(file);
            }
        }

        foreach (var (theme, bandTestId, shellClass, captureDir, builtPrefix, mockPrefix) in new[]
                 {
                     (PupilJourneyTheme.ZeeExpeditie, "ll-zee-result-band", "t-zee", zeeCapture, "zee", "10-zee-result"),
                     (PupilJourneyTheme.Eilandtuin, "ll-tuin-result-band", "t-tuin", tuinCapture, "tuin", "10-eilandtuin-result"),
                     (PupilJourneyTheme.Sterrenkust, "ll-ster-result-band", "t-ster", sterCapture, "ster", "10-sterrenkust-result"),
                 })
        {
            var pupil = await SeedGroep78CompletedAsync(journeyThemeKey: theme);
            foreach (var (width, height, tag, mobile) in new[]
                     {
                         (1440, 900, "1440", false),
                         (390, 844, "390", true),
                     })
            {
                await using var context = mobile
                    ? await NewMobileContextAsync()
                    : await _browser!.NewContextAsync(new BrowserNewContextOptions
                    {
                        ViewportSize = new ViewportSize { Width = width, Height = height },
                    });
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(90_000);
                var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
                await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                await page.WaitForURLAsync("**/leerling/dit-ben-jij**", DomReady);
                await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                await Assertions.Expect(page.Locator($".ll-g78-shell.{shellClass}")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator("[data-testid=leerling-dit-ben-jij]")).ToBeVisibleAsync();
                await WaitForGroep78ResultFootReadyAsync(page, mobile);
                var outPath = Path.Combine(captureDir, $"built-{builtPrefix}-result-{tag}.png");
                if (File.Exists(outPath))
                {
                    File.Delete(outPath);
                }

                await page.Locator(".rs-foot").ScrollIntoViewIfNeededAsync();
                await page.ScreenshotAsync(new()
                {
                    Path = outPath,
                    FullPage = true,
                });
            }
        }

        WriteZeeResultSideBySideComparisons(zeeCapture);
        WriteTuinResultSideBySideComparisons(tuinCapture);
        WriteSterResultSideBySideComparisons(sterCapture);
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

    [Fact(Timeout = 420_000)]
    public async Task Groep78_eiland_11a_capture_three_worlds()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        var zeeCapture = "/opt/cursor/artifacts/groep78-zee-eiland-11a-capture";
        var tuinCapture = "/opt/cursor/artifacts/groep78-tuin-eiland-11a-capture";
        var sterCapture = "/opt/cursor/artifacts/groep78-ster-eiland-11a-capture";
        Directory.CreateDirectory(zeeCapture);
        Directory.CreateDirectory(tuinCapture);
        Directory.CreateDirectory(sterCapture);

        foreach (var (theme, bandTestId, shellClass, captureDir, builtPrefix) in new[]
                 {
                     (PupilJourneyTheme.ZeeExpeditie, "ll-zee-eiland-band", "t-zee", zeeCapture, "zee"),
                     (PupilJourneyTheme.Eilandtuin, "ll-tuin-eiland-band", "t-tuin", tuinCapture, "tuin"),
                     (PupilJourneyTheme.Sterrenkust, "ll-ster-eiland-band", "t-ster", sterCapture, "ster"),
                 })
        {
            var seed = await SeedGroep78AtAsync(
                1,
                answered: 30,
                chipsSaved: false,
                completeActivePuzzleGate: true,
                journeyThemeKey: theme);
            var pupil = seed[0];
            foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
            {
                await using var context = await NewGroep78BrowserContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                    IsMobile = width < 600,
                    HasTouch = width < 600,
                    DeviceScaleFactor = width < 600 ? 2 : 1,
                });
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(90_000);
                var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
                await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                await page.WaitForURLAsync("**/leerling/eiland**", UrlCommitted);
                await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                await Assertions.Expect(page.Locator("[data-testid=ll-eiland-leuk-card]")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator($".ll-g78-shell.{shellClass}")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator(".read-aloud__error >> visible=true")).ToHaveCountAsync(0);
                await page.GetByTestId("ll-island-chip-dieren").ClickAsync();
                await page.GetByTestId("ll-island-chip-tekenen").ClickAsync();
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(captureDir, $"built-11a-{builtPrefix}-pauze-leuk-{tag}.png"),
                    FullPage = true,
                });
            }
        }

        WriteZeeEiland11aSideBySideComparisons(zeeCapture);
        WriteTuinEiland11aSideBySideComparisons(tuinCapture);
        WriteSterEiland11aSideBySideComparisons(sterCapture);
    }

    private static int _groep78E2eReportSeq;
    private static readonly SemaphoreSlim Groep78E2eReportLock = new(1, 1);
    private const string Groep78E2eReportPath = "/opt/cursor/artifacts/groep78-e2e-button-results.txt";

    [Theory(Timeout = 2_400_000)]
    [InlineData(PupilJourneyTheme.ZeeExpeditie, "Naar het Koraalrif", "ll-zee-vraag-band", 1440, 900, false)]
    [InlineData(PupilJourneyTheme.ZeeExpeditie, "Naar het Koraalrif", "ll-zee-vraag-band", 390, 844, true)]
    [InlineData(PupilJourneyTheme.Eilandtuin, "Naar de Bloemenbaai", "ll-tuin-vraag-band", 1440, 900, false)]
    [InlineData(PupilJourneyTheme.Eilandtuin, "Naar de Bloemenbaai", "ll-tuin-vraag-band", 390, 844, true)]
    [InlineData(PupilJourneyTheme.Sterrenkust, "Naar het Duinpad", "ll-ster-vraag-band", 1440, 900, false)]
    [InlineData(PupilJourneyTheme.Sterrenkust, "Naar het Duinpad", "ll-ster-vraag-band", 390, 844, true)]
    public async Task Groep78_full_pupil_journey_three_worlds_1440_and_390(
        string theme,
        string routeBtn,
        string bandTestId,
        int width,
        int height,
        bool mobile)
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        await EnablePassportV2Async();
        var baseUrl = EnsureWeb();
        Directory.CreateDirectory("/opt/cursor/artifacts");
        if (Interlocked.Increment(ref _groep78E2eReportSeq) == 1 && File.Exists(Groep78E2eReportPath))
        {
            File.Delete(Groep78E2eReportPath);
        }

        var report = await RunGroep78FullPupilJourneyVariantAsync(
            baseUrl, theme, routeBtn, bandTestId, width, height, mobile);
        await AppendGroep78E2eReportLinesAsync(report);
        var failures = report.Where(l => l.StartsWith("FAIL", StringComparison.Ordinal)).ToList();
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static async Task AppendGroep78E2eReportLinesAsync(IReadOnlyList<string> lines)
    {
        await Groep78E2eReportLock.WaitAsync();
        try
        {
            await File.AppendAllLinesAsync(Groep78E2eReportPath, lines);
            await File.AppendAllLinesAsync(Groep78E2eReportPath, [""]);
        }
        finally
        {
            Groep78E2eReportLock.Release();
        }
    }

    private async Task<IReadOnlyList<string>> RunGroep78FullPupilJourneyVariantAsync(
        string baseUrl,
        string theme,
        string routeBtn,
        string bandTestId,
        int width,
        int height,
        bool mobile)
    {
        var report = new List<string>();
        var seed = await SeedGroep78AtAsync(1, answered: 0, chipsSaved: false, completeActivePuzzleGate: true, journeyThemeKey: theme);
        var pupil = seed[0];
        var tag = $"{theme}-{width}";
        var consoleErrors = new List<string>();
        var failedRequests = new List<string>();

        await using var context = mobile
            ? await NewMobileContextAsync()
            : await NewGroep78BrowserContextAsync(new BrowserNewContextOptions
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
                    await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                });

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
                    await page.Locator($"[data-testid={PuzzleIntroBandTestId(theme)}]").WaitForAsync();
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

                await Step("Schelpenrij puzzle speakers", async () =>
                {
                    await page.Locator(".pp-card .vr-qrow [data-read-aloud]").ClickAsync();
                    var coach = width >= 600
                        ? page.Locator(".pp-coach-aside [data-read-aloud]")
                        : page.Locator(".m-only .guide [data-read-aloud]");
                    await coach.ClickAsync();
                });

                await Step("Schelpenrij puzzle controls", async () =>
                {
                    await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Overslaan" })).ToBeVisibleAsync();
                    var shells = page.Locator("[data-testid=ll-puzzle-shell]");
                    await Assertions.Expect(shells).ToHaveCountAsync(PupilSchelpenrijPuzzle.ShellCount);
                    await shells.Nth(1).ClickAsync();
                    await shells.Nth(3).ClickAsync();
                    await Assertions.Expect(page.GetByTestId("ll-puzzle-continue")).ToBeVisibleAsync();
                });

                await Step("Complete Schelpenrij puzzle", async () =>
                {
                    await page.GetByTestId("ll-puzzle-continue").ClickAsync();
                    await page.WaitForURLAsync("**/leerling/reis**", UrlCommitted);
                    await page.Locator($"[data-testid={bandTestId}]").WaitForAsync();
                });

                await Step("Reach Pauze-eiland 11a", async () =>
                {
                    page.SetDefaultTimeout(600_000);
                    try
                    {
                        await AdvanceGroep78UntilIslandAsync(page);
                        await page.WaitForURLAsync("**/leerling/eiland**", UrlCommitted);
                        await Assertions.Expect(page.Locator("[data-testid=ll-eiland-leuk-card]")).ToBeVisibleAsync();
                        await page.Locator($"[data-testid={IslandBandTestId(theme)}]").WaitForAsync();
                    }
                    finally
                    {
                        page.SetDefaultTimeout(90_000);
                    }
                });

                await Step("Island 11a lead speaker", async () =>
                {
                    await page.Locator(".vr-qrow [data-read-aloud]").First.ClickAsync();
                });

                await Step("Island 11a coach speaker", async () =>
                {
                    var coach = width >= 600
                        ? page.Locator(".vr-coach-aside [data-read-aloud]")
                        : page.Locator(".m-only .guide [data-read-aloud]");
                    await coach.ClickAsync();
                });

                await Step("Island 11a like chips", async () =>
                {
                    var chips = page.Locator("[data-testid^=ll-island-chip-]");
                    var count = await chips.CountAsync();
                    Assert.True(count >= PupilInterestChipCatalog.LikeChips.Count);
                    foreach (var key in new[] { "sport", "dieren", "tekenen", "natuur", "__other__" })
                    {
                        var testId = key == "__other__" ? "ll-island-chip-other" : $"ll-island-chip-{key}";
                        await page.GetByTestId(testId).ClickAsync();
                    }
                });

                await Step("Island 11a Vorige to reis and back", async () =>
                {
                    await page.GetByTestId("ll-eiland-prev").ClickAsync();
                    await page.WaitForURLAsync("**/leerling/eiland**", UrlCommitted);
                    await Assertions.Expect(page.Locator("[data-testid=ll-eiland-leuk-card]")).ToBeVisibleAsync();
                });

                await Step("Island 11a Verder to stap 2", async () =>
                {
                    await page.GetByTestId("ll-eiland-continue").ClickAsync();
                    await page.WaitForURLAsync("**/leerling/eiland/stap-2**", UrlCommitted);
                });

                await Step("Island stap 2 finish", async () =>
                {
                    await page.Locator("fieldset.ll-chips").First
                        .GetByRole(AriaRole.Button, new() { Name = "Rekenen", Exact = true }).ClickAsync();
                    await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/reis**", UrlCommitted);
                });

                var reachedResult = false;
                await Step("Finish journey to Dit ben jij", async () =>
                {
                    page.SetDefaultTimeout(600_000);
                    try
                    {
                        await AdvanceGroep78UntilResultAsync(page);
                        await page.Locator("[data-testid=leerling-dit-ben-jij]").WaitForAsync();
                        await page.Locator($"[data-testid={ResultBandTestId(theme)}]").WaitForAsync();
                        reachedResult = true;
                    }
                    finally
                    {
                        page.SetDefaultTimeout(90_000);
                    }
                });

                if (!reachedResult)
                {
                    return report;
                }

                await Step("Result story speaker", async () =>
                {
                    await Assertions.Expect(page.Locator(".rs-story-card")).ToBeVisibleAsync();
                    await page.Locator(".rs-story-card [data-read-aloud]").First.ClickAsync();
                });

                await Step("Result tile speakers", async () =>
                {
                    var tiles = page.Locator(".rs-tile [data-read-aloud]");
                    var count = await tiles.CountAsync();
                    for (var t = 0; t < count; t++)
                    {
                        await tiles.Nth(t).ClickAsync();
                    }
                });

                await Step("Result jobs speaker", async () =>
                {
                    await page.Locator(".rs-likes-card .vr-qrow [data-read-aloud]").ClickAsync();
                });

                await Step("Result coach speaker", async () =>
                {
                    var coach = width >= 600
                        ? page.Locator(".rs-coach-aside [data-read-aloud]")
                        : page.Locator(".rs-mobile-coach [data-read-aloud]");
                    await coach.ClickAsync();
                });

                await Step("Pauze from result", async () =>
                {
                    await page.GetByRole(AriaRole.Button, new() { Name = "Pauze" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/stop**", DomReady);
                    await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId, journeyThemeKey: theme);
                    await page.WaitForURLAsync("**/leerling/dit-ben-jij**", DomReady);
                });

                await Step("Jouw DNA-paspoort link", async () =>
                {
                    var link = page.GetByRole(AriaRole.Link, new() { Name = "Jouw DNA-paspoort" });
                    await Assertions.Expect(link).ToBeVisibleAsync();
                    await Assertions.Expect(link).ToHaveClassAsync(new Regex(@"\bbtn-ghost\b"));
                    if (mobile)
                    {
                        await Assertions.Expect(link).ToHaveCSSAsync("border-width", "2px");
                    }

                    await link.ClickAsync();
                    await page.WaitForURLAsync("**/leerling/paspoort**", DomReady);
                    await page.GoBackAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    await page.WaitForURLAsync("**/leerling/dit-ben-jij**", DomReady);
                });

                await Step("PDF link", async () =>
                {
                    var link = page.GetByRole(AriaRole.Link, new() { Name = "PDF" });
                    await Assertions.Expect(link).ToHaveAttributeAsync("href", "/leerling/pdf");
                    await Assertions.Expect(link).ToHaveClassAsync(new Regex(@"\bbtn-ghost\b"));
                    if (mobile)
                    {
                        await Assertions.Expect(link).ToHaveCSSAsync("border-width", "2px");
                    }
                });

                await Step("Check je droombaan", async () =>
                {
                    await page.GetByRole(AriaRole.Link, new() { Name = "Check je droombaan" }).ClickAsync();
                    await page.WaitForURLAsync("**/leerling/droombaan**", DomReady);
                });
        }
        finally
        {
            await page.CloseAsync();
        }

        return report;
    }

    private static async Task WaitForGroep78ResultFootReadyAsync(IPage page, bool mobile)
    {
        var passport = page.GetByRole(AriaRole.Link, new() { Name = "Jouw DNA-paspoort" });
        await passport.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var pdf = page.GetByRole(AriaRole.Link, new() { Name = "PDF" });
        await Assertions.Expect(pdf).ToBeVisibleAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
                const links = document.querySelectorAll('.rs-foot .btn.btn-ghost');
                if (links.length < 2) return false;
                return [...links].every(el => getComputedStyle(el).borderWidth === '2px');
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 60_000 });
        if (mobile)
        {
            await Assertions.Expect(passport).ToHaveCSSAsync("border-width", "2px");
        }
    }

    private static string PuzzleIntroBandTestId(string theme)
        => theme switch
        {
            var t when string.Equals(t, PupilJourneyTheme.Eilandtuin, StringComparison.Ordinal) => "ll-tuin-puzzel-intro-band",
            var t when string.Equals(t, PupilJourneyTheme.Sterrenkust, StringComparison.Ordinal) => "ll-ster-puzzel-intro-band",
            _ => "ll-zee-puzzel-intro-band",
        };

    private static string ResultBandTestId(string theme)
        => theme switch
        {
            var t when string.Equals(t, PupilJourneyTheme.Eilandtuin, StringComparison.Ordinal) => "ll-tuin-result-band",
            var t when string.Equals(t, PupilJourneyTheme.Sterrenkust, StringComparison.Ordinal) => "ll-ster-result-band",
            _ => "ll-zee-result-band",
        };

    private static string IslandBandTestId(string theme)
        => theme switch
        {
            var t when string.Equals(t, PupilJourneyTheme.Eilandtuin, StringComparison.Ordinal) => "ll-tuin-eiland-band",
            var t when string.Equals(t, PupilJourneyTheme.Sterrenkust, StringComparison.Ordinal) => "ll-ster-eiland-band",
            _ => "ll-zee-eiland-band",
        };

    private static async Task CompleteGroep78IslandAsync(IPage page)
    {
        var themedLeuk = page.Locator("[data-testid=ll-eiland-leuk-card]");
        if (await themedLeuk.CountAsync() > 0)
        {
            await page.GetByTestId("ll-island-chip-sport").ClickAsync();
            await page.GetByTestId("ll-island-chip-dieren").ClickAsync();
            await page.Locator(".vr-qrow [data-read-aloud]").First.ClickAsync();
            var coach = page.Locator(".vr-coach-aside [data-read-aloud], .m-only .guide [data-read-aloud]").First;
            if (await coach.CountAsync() > 0)
            {
                await coach.ClickAsync();
            }

            await page.GetByTestId("ll-eiland-continue").ClickAsync();
            await page.WaitForURLAsync("**/leerling/eiland/stap-2**", UrlCommitted);
            var dislikes = page.Locator("fieldset.ll-chips").First;
            await dislikes.GetByRole(AriaRole.Button, new() { Name = "Rekenen", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).ClickAsync();
            await page.WaitForFunctionAsync("() => !location.pathname.includes('/leerling/eiland')");
            return;
        }

        var likes = page.Locator("fieldset.ll-chips").Nth(0);
        var dislikesClassic = page.Locator("fieldset.ll-chips").Nth(1);
        if (await dislikesClassic.CountAsync() == 0)
        {
            dislikesClassic = page.Locator("fieldset.ll-chips").First;
            await likes.GetByRole(AriaRole.Button, new() { Name = "Sport", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/eiland/stap-2**", UrlCommitted);
            await dislikesClassic.GetByRole(AriaRole.Button, new() { Name = "Rekenen", Exact = true }).ClickAsync();
        }
        else
        {
            await likes.GetByRole(AriaRole.Button, new() { Name = "Sport", Exact = true }).ClickAsync();
            await dislikesClassic.GetByRole(AriaRole.Button, new() { Name = "Rekenen", Exact = true }).ClickAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).ClickAsync();
        await page.WaitForFunctionAsync("() => !location.pathname.includes('/leerling/eiland')");
    }

    private static async Task AdvanceGroep78UntilIslandAsync(IPage page, int maxSteps = 120)
    {
        for (var step = 0; step < maxSteps; step++)
        {
            if (page.Url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (page.Url.Contains("/puzzel-intro/", StringComparison.OrdinalIgnoreCase))
            {
                await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
                continue;
            }

            if (page.Url.Contains("/leerling/puzzel/", StringComparison.OrdinalIgnoreCase))
            {
                await CompleteActiveGroep78PuzzleAsync(page);
                continue;
            }

            if (page.Url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase))
            {
                await AnswerGroep78ThemedAsync(page);
                continue;
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException("Did not reach /leerling/eiland within the step budget.");
    }

    private static async Task AdvanceGroep78UntilResultAsync(IPage page, int maxSteps = 280)
    {
        for (var step = 0; step < maxSteps; step++)
        {
            if (page.Url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (page.Url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase))
            {
                await CompleteGroep78IslandAsync(page);
                continue;
            }

            if (page.Url.Contains("/puzzel-intro/", StringComparison.OrdinalIgnoreCase))
            {
                await page.GetByRole(AriaRole.Link, new() { Name = "Start de puzzel" }).ClickAsync();
                continue;
            }

            if (page.Url.Contains("/leerling/puzzel/", StringComparison.OrdinalIgnoreCase))
            {
                await CompleteActiveGroep78PuzzleAsync(page);
                continue;
            }

            if (page.Url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase))
            {
                await AnswerGroep78ThemedAsync(page);
                continue;
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException("Did not reach /leerling/dit-ben-jij within the step budget.");
    }

    private static async Task CompleteActiveGroep78PuzzleAsync(IPage page)
    {
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync(new() { Timeout = 20_000 });
        var url = page.Url;

        if (url.Contains("schelpenrij", StringComparison.OrdinalIgnoreCase))
        {
            await SolveSchelpenrijPuzzleByPlayingAsync(page);
        }
        else if (url.Contains("schatkaart", StringComparison.OrdinalIgnoreCase))
        {
            await SolveSchatkaartPuzzleByPlayingAsync(page);
        }
        else if (url.Contains("vuurtorenlampen", StringComparison.OrdinalIgnoreCase))
        {
            await SolveVuurtorenlampenPuzzleByPlayingAsync(page);
        }
        else
        {
            throw new InvalidOperationException("Unknown groep 78 puzzle: " + url);
        }

        var continueBtn = page.GetByTestId("ll-puzzle-continue");
        await continueBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await continueBtn.ClickAsync(new() { Force = true });
        await page.WaitForURLAsync(
            url => !url.Contains("/puzzel/", StringComparison.OrdinalIgnoreCase),
            UrlCommitted);
    }

    /// <summary>One swap (indices 1 and 3) solves <see cref="PupilSchelpenrijPuzzle.InitialPlayOrder"/>.</summary>
    private static async Task SolveSchelpenrijPuzzleByPlayingAsync(IPage page)
    {
        var shells = page.Locator("[data-testid=ll-puzzle-shell]");
        await Assertions.Expect(shells).ToHaveCountAsync(PupilSchelpenrijPuzzle.ShellCount);
        await shells.Nth(1).ClickAsync();
        await shells.Nth(3).ClickAsync();
        await Assertions.Expect(page.GetByTestId("ll-puzzle-continue")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    private static async Task SolveSchatkaartPuzzleByPlayingAsync(IPage page)
    {
        var cards = page.Locator("[data-testid=ll-puzzle-card]");
        await Assertions.Expect(cards).ToHaveCountAsync(6);

        async Task<bool> IsMatchedAsync(int index)
        {
            var cls = await cards.Nth(index).GetAttributeAsync("class") ?? "";
            return cls.Contains("is-matched", StringComparison.Ordinal);
        }

        async Task<string> FaceAsync(int index) => (await cards.Nth(index).InnerTextAsync()).Trim();

        async Task<bool> IsFaceDownAsync(int index) => await FaceAsync(index) == "?";

        async Task<int> MatchedCountAsync()
        {
            var count = 0;
            for (var i = 0; i < 6; i++)
            {
                if (await IsMatchedAsync(i))
                {
                    count++;
                }
            }

            return count;
        }

        async Task FlipPairAsync(int a, int b)
        {
            await cards.Nth(a).ClickAsync(new() { Force = true });
            await Task.Delay(550);
            await cards.Nth(b).ClickAsync(new() { Force = true });
            await Task.Delay(900);
        }

        var known = new Dictionary<string, int>(StringComparer.Ordinal);
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            if (await page.GetByTestId("ll-puzzle-continue").IsVisibleAsync())
            {
                return;
            }

            var openIndex = -1;
            string? openFace = null;
            for (var i = 0; i < 6; i++)
            {
                if (await IsMatchedAsync(i))
                {
                    continue;
                }

                var face = await FaceAsync(i);
                if (face != "?")
                {
                    openIndex = i;
                    openFace = face;
                    break;
                }
            }

            if (openIndex >= 0 && openFace is not null)
            {
                if (known.TryGetValue(openFace, out var mate) && mate != openIndex && !await IsMatchedAsync(mate))
                {
                    await cards.Nth(mate).ClickAsync(new() { Force = true });
                    await Task.Delay(900);
                    known.Remove(openFace);
                    continue;
                }

                for (var j = 0; j < 6; j++)
                {
                    if (j == openIndex || await IsMatchedAsync(j) || !await IsFaceDownAsync(j))
                    {
                        continue;
                    }

                    await cards.Nth(j).ClickAsync(new() { Force = true });
                    await Task.Delay(900);
                    var faceJ = await FaceAsync(j);
                    if (string.Equals(openFace, faceJ, StringComparison.Ordinal))
                    {
                        known.Remove(openFace);
                    }
                    else
                    {
                        known[openFace] = openIndex;
                        known[faceJ] = j;
                    }

                    break;
                }

                continue;
            }

            var before = await MatchedCountAsync();
            var progressed = false;
            for (var a = 0; a < 6 && !progressed; a++)
            {
                if (await IsMatchedAsync(a) || !await IsFaceDownAsync(a))
                {
                    continue;
                }

                for (var b = a + 1; b < 6; b++)
                {
                    if (await IsMatchedAsync(b) || !await IsFaceDownAsync(b))
                    {
                        continue;
                    }

                    await FlipPairAsync(a, b);
                    var after = await MatchedCountAsync();
                    if (after > before || await page.GetByTestId("ll-puzzle-continue").IsVisibleAsync())
                    {
                        progressed = true;
                        break;
                    }
                }
            }

            if (!progressed)
            {
                await Task.Delay(400);
            }
        }

        throw new InvalidOperationException("Schatkaart puzzle was not solved by playing.");
    }

    private static async Task SolveVuurtorenlampenPuzzleByPlayingAsync(IPage page)
    {
        var root = page.Locator("[data-testid=ll-puzzle-root]");
        var lamps = root.Locator("[data-testid=ll-puzzle-lamp]");
        await lamps.First.WaitForAsync();
        await root.GetByText("Kijk goed…").WaitForAsync(new() { Timeout = 20_000 });

        var pattern = new List<int>();
        var lastLit = -1;
        var deadline = DateTime.UtcNow.AddSeconds(25);
        while (DateTime.UtcNow < deadline)
        {
            if (await root.GetByText("Jouw beurt").IsVisibleAsync())
            {
                break;
            }

            var litNow = -1;
            for (var i = 0; i < 3; i++)
            {
                if (await lamps.Nth(i).EvaluateAsync<bool>("el => el.classList.contains('is-lit')"))
                {
                    litNow = i;
                }
            }

            if (litNow >= 0 && litNow != lastLit)
            {
                pattern.Add(litNow);
                lastLit = litNow;
            }
            else if (litNow < 0)
            {
                lastLit = -1;
            }

            await Task.Delay(35);
        }

        if (pattern.Count < 3)
        {
            throw new InvalidOperationException("Could not capture vuurtoren lamp pattern.");
        }

        foreach (var lamp in pattern.Take(3))
        {
            await lamps.Nth(lamp).ClickAsync(new() { Force = true });
            await Task.Delay(300);
        }

        await Assertions.Expect(page.GetByTestId("ll-puzzle-continue")).ToBeVisibleAsync(new() { Timeout = 30_000 });
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
                    || path.includes('/puzzel/')
                    || path.includes('/leerling/eiland')
                    || path.includes('/leerling/dit-ben-jij')
                    || path.includes('/leerling/droombaan');
            }
            """,
            before,
            new PageWaitForFunctionOptions { Timeout = 45_000 });
    }

    private static async Task WaitGroep78ReadAloudCleanAsync(IPage page)
    {
        var readAloudErrors = page.Locator(".read-aloud__error >> visible=true");
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            if (await readAloudErrors.CountAsync() == 0)
            {
                var body = await page.Locator("body").InnerTextAsync();
                if (!body.Contains("lukt nu even niet", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            await Task.Delay(150);
        }
    }

    private static async Task WaitGroep78VisibleErrorsClearAsync(IPage page)
    {
        await WaitGroep78ReadAloudCleanAsync(page);
        var errors = page.Locator(".vr-error, .ll-error");
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (await errors.CountAsync() == 0)
            {
                return;
            }

            var retry = page.GetByRole(AriaRole.Button, new() { Name = "Probeer opnieuw" });
            if (await retry.CountAsync() > 0 && await retry.First.IsVisibleAsync())
            {
                await retry.First.ClickAsync();
                await Task.Delay(500);
                continue;
            }

            await Task.Delay(300);
        }
    }

    private static async Task AssertGroep78UiCleanAsync(
        IPage page,
        IReadOnlyList<string> consoleErrors,
        IReadOnlyList<string> failedRequests)
    {
        await WaitGroep78VisibleErrorsClearAsync(page);

        var body = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions { Timeout = 5_000 });
        Assert.DoesNotContain("_saveError", body, StringComparison.Ordinal);
        Assert.DoesNotContain("_saved", body, StringComparison.Ordinal);
        Assert.DoesNotContain("lukt nu even niet", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await page.Locator(".read-aloud__error >> visible=true").CountAsync());
        var untranslated = UntranslatedKeyPattern().Match(body);
        if (untranslated.Success)
        {
            throw new InvalidOperationException("Untranslated key visible: " + untranslated.Value);
        }

        var vrCount = await page.Locator(".vr-error").CountAsync();
        var llCount = await page.Locator(".ll-error").CountAsync();
        if (vrCount + llCount > 0)
        {
            var vrText = vrCount > 0 ? await page.Locator(".vr-error").AllInnerTextsAsync() : [];
            var llText = llCount > 0 ? await page.Locator(".ll-error").AllInnerTextsAsync() : [];
            throw new InvalidOperationException(
                "Visible errors: vr=" + string.Join(" | ", vrText) + " ll=" + string.Join(" | ", llText));
        }
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
                        && !r.Contains(" fonts.gstatic.com", StringComparison.OrdinalIgnoreCase)
                        && !(r.Contains("/leerling/pdf", StringComparison.OrdinalIgnoreCase)
                             && r.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (noisyFailed.Count > 0)
        {
            throw new InvalidOperationException("Failed requests: " + string.Join(" | ", noisyFailed));
        }
    }

    private static Regex UntranslatedKeyPattern() => new(@"Leerling\.[A-Z][\w.]+", RegexOptions.Compiled);

    private static void WriteSterRouteSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-ster-route-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/07-sterrenkust-route.png",
                    "built-07-sterrenkust-route-1440.png",
                    "side-by-side-07-sterrenkust-route-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/07-sterrenkust-route.png",
                    "built-07-sterrenkust-route-390.png",
                    "side-by-side-07-sterrenkust-route-390.png"),
            ]);

    private static void WriteSterVraagSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-ster-vraag-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/08-sterrenkust-vraag.png",
                    "built-08-sterrenkust-vraag-1440.png",
                    "side-by-side-08-sterrenkust-vraag-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/08-sterrenkust-vraag.png",
                    "built-08-sterrenkust-vraag-390.png",
                    "side-by-side-08-sterrenkust-vraag-390.png"),
            ]);

    private static void WriteSterPuzzelIntroSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-ster-puzzel-intro-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/09-sterrenkust-puzzel-intro.png",
                    "built-09-sterrenkust-puzzel-intro-1440.png",
                    "side-by-side-09-sterrenkust-puzzel-intro-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/09-sterrenkust-puzzel-intro.png",
                    "built-09-sterrenkust-puzzel-intro-390.png",
                    "side-by-side-09-sterrenkust-puzzel-intro-390.png"),
            ]);

    private static void WriteZeePuzzelPlaySideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-zee-puzzel-play-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/03-zee-puzzel-intro.png",
                    "built-zee-schelpenrij-play-1440.png",
                    "side-by-side-zee-schelpenrij-play-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/03-zee-puzzel-intro.png",
                    "built-zee-schelpenrij-play-390.png",
                    "side-by-side-zee-schelpenrij-play-390.png"),
            ]);

    private static void WriteTuinPuzzelPlaySideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-tuin-puzzel-play-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/06-eilandtuin-puzzel-intro.png",
                    "built-tuin-schelpenrij-play-1440.png",
                    "side-by-side-tuin-schelpenrij-play-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/06-eilandtuin-puzzel-intro.png",
                    "built-tuin-schelpenrij-play-390.png",
                    "side-by-side-tuin-schelpenrij-play-390.png"),
            ]);

    private static void WriteSterPuzzelPlaySideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-ster-puzzel-play-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/09-sterrenkust-puzzel-intro.png",
                    "built-ster-schelpenrij-play-1440.png",
                    "side-by-side-ster-schelpenrij-play-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/09-sterrenkust-puzzel-intro.png",
                    "built-ster-schelpenrij-play-390.png",
                    "side-by-side-ster-schelpenrij-play-390.png"),
            ]);

    private static void WriteZeeEiland11aSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-zee-eiland-11a-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/11a-zee-pauze-leuk.png",
                    "built-11a-zee-pauze-leuk-1440.png",
                    "side-by-side-11a-zee-pauze-leuk-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/11a-zee-pauze-leuk.png",
                    "built-11a-zee-pauze-leuk-390.png",
                    "side-by-side-11a-zee-pauze-leuk-390.png"),
            ]);

    private static void WriteTuinEiland11aSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-tuin-eiland-11a-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/11a-eilandtuin-pauze-leuk.png",
                    "built-11a-tuin-pauze-leuk-1440.png",
                    "side-by-side-11a-eilandtuin-pauze-leuk-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/11a-eilandtuin-pauze-leuk.png",
                    "built-11a-tuin-pauze-leuk-390.png",
                    "side-by-side-11a-eilandtuin-pauze-leuk-390.png"),
            ]);

    private static void WriteSterEiland11aSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-ster-eiland-11a-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/11a-sterrenkust-pauze-leuk.png",
                    "built-11a-ster-pauze-leuk-1440.png",
                    "side-by-side-11a-sterrenkust-pauze-leuk-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/11a-sterrenkust-pauze-leuk.png",
                    "built-11a-ster-pauze-leuk-390.png",
                    "side-by-side-11a-sterrenkust-pauze-leuk-390.png"),
            ]);

    private static void WriteZeeResultSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-zee-result-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/10-zee-result.png",
                    "built-zee-result-1440.png",
                    "side-by-side-10-zee-result-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/10-zee-result.png",
                    "built-zee-result-390.png",
                    "side-by-side-10-zee-result-390.png"),
            ]);

    private static void WriteTuinResultSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-tuin-result-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/10-eilandtuin-result.png",
                    "built-tuin-result-1440.png",
                    "side-by-side-10-eilandtuin-result-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/10-eilandtuin-result.png",
                    "built-tuin-result-390.png",
                    "side-by-side-10-eilandtuin-result-390.png"),
            ]);

    private static void WriteSterResultSideBySideComparisons(string captureDir)
        => WriteMockupSideBySideComparisons(
            captureDir,
            "/opt/cursor/artifacts/groep78-ster-result-compare",
            [
                (
                    "docs/mockups/leerling/werelden/renders/desktop/10-sterrenkust-result.png",
                    "built-ster-result-1440.png",
                    "side-by-side-10-sterrenkust-result-1440.png"),
                (
                    "docs/mockups/leerling/werelden/renders/mobile/10-sterrenkust-result.png",
                    "built-ster-result-390.png",
                    "side-by-side-10-sterrenkust-result-390.png"),
            ]);

    private static void WriteMockupSideBySideComparisons(
        string captureDir,
        string compareDir,
        IReadOnlyList<(string MockRel, string BuiltName, string OutName)> pairs)
    {
        Directory.CreateDirectory(compareDir);
        var repoRoot = FindRepoRoot();
        foreach (var (mockRel, builtName, outName) in pairs)
        {
            var mockPath = Path.Combine(compareDir, Path.GetFileName(mockRel));
            if (!File.Exists(mockPath) || new FileInfo(mockPath).Length == 0)
            {
                if (File.Exists(mockPath))
                {
                    File.Delete(mockPath);
                }

                TryGitShowMockup(repoRoot, mockRel, mockPath);
            }

            var builtPath = Path.Combine(captureDir, builtName);
            var outPath = Path.Combine(compareDir, outName);
            if (!File.Exists(mockPath) || !File.Exists(builtPath))
            {
                continue;
            }

            if (File.Exists(outPath))
            {
                File.Delete(outPath);
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
            foreach (var gitRef in new[]
                     {
                         "HEAD",
                         "origin/cursor/leerling-result-mockups-10-3fb0",
                         "pr-878-mockups",
                         "pr-878",
                     })
            {
                psi.ArgumentList.Clear();
                psi.ArgumentList.Add("show");
                psi.ArgumentList.Add($"{gitRef}:{gitPath}");
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc is null)
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                using (var fs = File.Create(dest))
                {
                    proc.StandardOutput.BaseStream.CopyTo(fs);
                }

                proc.WaitForExit(5_000);
                if (proc.ExitCode == 0 && new FileInfo(dest).Length > 0)
                {
                    return;
                }

                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }
            }
        }
        catch
        {
            // Mockups are optional when pr-878 refs are missing.
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

    private async Task<IBrowserContext> NewGroep78BrowserContextAsync(BrowserNewContextOptions options)
    {
        var ctx = await _browser!.NewContextAsync(options);
        await ctx.AddInitScriptAsync(Groep78ReadAloudInitScript);
        return ctx;
    }

    private async Task<IBrowserContext> NewMobileContextAsync()
        => await NewGroep78BrowserContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
            IsMobile = true,
            HasTouch = true,
            DeviceScaleFactor = 2
        });

    private string EnsureWeb()
    {
        lock (SharedWebGate)
        {
            if (_sharedWeb is not null)
            {
                return _sharedWeb.ServerAddress.TrimEnd('/');
            }

            // One Kestrel host for the suite avoids background-service cancellation
            // when a per-test host is torn down mid-run.
            InvalidOperationException? last = null;
            for (var attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    var factory = new PupilKestrelFactory(_api.Server.CreateHandler());
                    _ = factory.CreateClient();
                    _sharedWeb = factory;
                    return factory.ServerAddress.TrimEnd('/');
                }
                catch (InvalidOperationException ex)
                {
                    last = ex;
                    Thread.Sleep(200 * (attempt + 1));
                }
            }

            throw last ?? new InvalidOperationException("Pupil web host did not start.");
        }
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

    [Fact(Timeout = 180_000)]
    public async Task Groep78_vuurtoren_play_solver_smoke()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        var seed = await SeedGroep78AtAsync(1, answered: 45, chipsSaved: true, completeActivePuzzleGate: false);
        var pupil = seed[0];
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);
        var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/puzzel/vuurtorenlampen", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
        await SolveVuurtorenlampenPuzzleByPlayingAsync(page);
        await Assertions.Expect(page.GetByTestId("ll-puzzle-continue")).ToBeVisibleAsync();
    }

    [Fact(Timeout = 180_000)]
    public async Task Groep78_schatkaart_play_solver_smoke()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        var seed = await SeedGroep78AtAsync(1, answered: 30, chipsSaved: false, completeActivePuzzleGate: false);
        var pupil = seed[0];
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);
        var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
        await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId);
        await page.GotoAsync($"{baseUrl}/leerling/puzzel/schatkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=ll-puzzle-root]").WaitForAsync();
        await SolveSchatkaartPuzzleByPlayingAsync(page);
        await Assertions.Expect(page.GetByTestId("ll-puzzle-continue")).ToBeVisibleAsync();
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

    private async Task<Seed> SeedGroep78CompletedAsync(string journeyThemeKey = PupilJourneyTheme.ZeeExpeditie)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Result " + Guid.NewGuid().ToString("N")[..6],
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
            Name = "8B",
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
            JourneyThemeKey = journeyThemeKey,
            LikesJson = """["lezen","dieren","natuur","duiken"]""",
            DislikesJson = "[]",
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
