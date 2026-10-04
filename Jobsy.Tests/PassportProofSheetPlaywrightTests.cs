using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Components.Candidate.ProfileSections;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Bewijzen editor must stay visible on desktop (the mobile filter-sheet rule hides .filter-sheet)
/// and Opslaan must be tappable at 390×844, above the bottom nav.
/// </summary>
[Collection("PlaywrightSmoke")]
public class PassportProofSheetPlaywrightTests : BunitContext
{
    public PassportProofSheetPlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IGeocodingClient>(new NullGeo());
        Services.AddSingleton(new HttpClient(new ProfileApi()) { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton(sp => new JobsyApiClient(sp.GetRequiredService<HttpClient>()));
        Services.AddSingleton<CandidateProfileEditor>();
        Services.AddSingleton<IFeatureFlags>(new FixedFlags());
    }

    [Fact]
    public void Profiel_opgeslagen_is_shown_once()
    {
        var cut = Render<PassportDataTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() => Assert.Contains("Mijn motivatie", cut.Markup, StringComparison.Ordinal));
        cut.FindAll("button").First(b => b.TextContent.Contains("Mijn motivatie", StringComparison.Ordinal)).Click();
        var editor = Services.GetRequiredService<CandidateProfileEditor>();
        editor.Message = "Profiel opgeslagen.";
        editor.Notify();
        cut.WaitForAssertion(() => Assert.Contains("Profiel opgeslagen.", cut.Markup, StringComparison.Ordinal));
        Assert.Equal(1, Count(cut.Markup, "Profiel opgeslagen."));
        Assert.Contains("role=\"status\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Escape_closes_the_bewijzen_editor()
    {
        var cut = Render<PassportProofTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() => Assert.Contains("Werkgever toevoegen", cut.Markup, StringComparison.Ordinal));
        OpenEditor(cut);
        cut.Find("[data-testid=passport-proof-sheet]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.DoesNotContain("passport-proof-sheet", cut.Markup, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(390, 844)]
    public async Task Bewijzen_editor_is_visible_and_save_is_tappable(int width, int height)
    {
        var cut = Render<PassportProofTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() => Assert.Contains("Werkgever toevoegen", cut.Markup, StringComparison.Ordinal));
        OpenEditor(cut);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            HasTouch = width < 768,
            IsMobile = width < 768
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(Html(cut.Markup, includeProbe: width >= 769));

        var sheetDisplay = await page.EvalOnSelectorAsync<string>("[data-testid=passport-proof-sheet]", "el => getComputedStyle(el).display");
        Assert.Equal("flex", sheetDisplay);
        if (width >= 769)
        {
            var probeHidden = await page.EvalOnSelectorAsync<string>("#filter-sheet-probe", "el => getComputedStyle(el).display");
            Assert.Equal("none", probeHidden);
            var centered = await page.EvaluateAsync<bool>("""
                () => {
                  const sheet = document.querySelector('[data-testid=passport-proof-sheet]');
                  if (!sheet) return false;
                  const r = sheet.getBoundingClientRect();
                  const cx = r.left + r.width / 2;
                  return r.width > 200 && r.height > 80
                    && Math.abs(cx - window.innerWidth / 2) < 48
                    && r.top > 8 && r.bottom < window.innerHeight - 8;
                }
                """);
            Assert.True(centered, "Desktop Bewijzen editor should be a centered dialog, not display:none.");
        }

        var saveHits = await page.EvaluateAsync<bool>("""
            () => {
              const btn = document.querySelector('[data-testid=passport-proof-save]');
              if (!btn) return false;
              const r = btn.getBoundingClientRect();
              if (r.width < 8 || r.height < 8) return false;
              if (r.top < 0 || r.bottom > window.innerHeight + 1) return false;
              const x = r.left + r.width / 2;
              const y = r.top + r.height / 2;
              const el = document.elementFromPoint(x, y);
              if (!el) return false;
              if (el.closest && el.closest('.bottom-nav')) return false;
              return !!(el === btn || (el.closest && el.closest('[data-testid=passport-proof-save]')));
            }
            """);
        Assert.True(saveHits, $"Opslaan must be tappable at {width}x{height}, not under .bottom-nav.");

        if (width <= 390)
        {
            var close = await page.Locator("[data-jobsy-dialog-close]").BoundingBoxAsync();
            Assert.NotNull(close);
            Assert.True(close!.Width >= 44 && close.Height >= 44, $"Sluiten is {close.Width}x{close.Height}, need at least 44x44.");
            Assert.Equal("Sluiten", await page.Locator("[data-jobsy-dialog-close]").GetAttributeAsync("aria-label"));
        }
    }

    [Theory]
    [InlineData("button[data-jobsy-dialog-close]")]
    [InlineData("button.filter-sheet__cancel")]
    public void Closing_the_bewijzen_editor_returns_focus_to_the_opener(string closer)
    {
        var cut = Render<PassportProofTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() => Assert.Contains("Werkgever toevoegen", cut.Markup, StringComparison.Ordinal));
        OpenEditor(cut);
        cut.Find(closer).Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain("passport-proof-sheet", cut.Markup, StringComparison.Ordinal));
        Assert.Contains(
            JSInterop.Invocations,
            call => call.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Escape_returns_focus_to_the_opener()
    {
        var cut = Render<PassportProofTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() => Assert.Contains("Werkgever toevoegen", cut.Markup, StringComparison.Ordinal));
        OpenEditor(cut);
        cut.Find("[data-testid=passport-proof-sheet]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.DoesNotContain("passport-proof-sheet", cut.Markup, StringComparison.Ordinal));
        Assert.Contains(
            JSInterop.Invocations,
            call => call.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    private static void OpenEditor(IRenderedComponent<PassportProofTab> cut)
    {
        cut.FindAll("button").First(b => b.TextContent.Contains("Werkgever toevoegen", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"passport-proof-sheet\"", cut.Markup, StringComparison.Ordinal));
    }

    private static string Html(string markup, bool includeProbe)
    {
        var root = FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var passport = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/mijn-paspoort.css"));
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
               + "<style>" + app + passport + "</style></head><body>"
               + "<div class=\"app-shell has-bottom-nav\">"
               + "<header class=\"app-header\"><span>Lobsy</span></header>"
               + "<main class=\"app-main\">" + markup + "</main>"
               + "<nav class=\"bottom-nav\"><a class=\"bottom-nav__item\" href=\"/carriere\">Carrière</a></nav>"
               + "</div>"
               + (includeProbe ? "<div id=\"filter-sheet-probe\" class=\"filter-sheet\">probe</div>" : "")
               + "</body></html>";
    }

    private static string FindRoot()
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

    private static int Count(string text, string needle)
    {
        var n = 0;
        var i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            n++;
            i += needle.Length;
        }

        return n;
    }

    private sealed class FixedFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate()
        {
        }
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "t"), new Claim(ClaimTypes.Role, "Candidate")],
                "t"))));
    }

    private sealed class NullGeo : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class ProfileApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("profile", StringComparison.Ordinal))
            {
                const string body = """
                    {"id":"11111111-1111-1111-1111-111111111111","email":"sam@example.com","fullName":"Sam Tester","firstName":"Sam","lastName":"Tester","role":"Candidate","preferences":{"roles":[],"educations":[]},"emailVerified":true}
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        }
    }
}
