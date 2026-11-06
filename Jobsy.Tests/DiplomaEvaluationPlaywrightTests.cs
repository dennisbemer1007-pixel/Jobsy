using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Components.Candidate.ProfileSections;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Add, edit and passport display for a foreign-diploma evaluation.
/// Playwright checks the rendered markup at a phone width; bunit drives the Blazor actions.
/// </summary>
[Collection("PlaywrightSmoke")]
public class DiplomaEvaluationPlaywrightTests : BunitContext
{
    private readonly EvalApi _api = new();

    public DiplomaEvaluationPlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IGeocodingClient>(new NullGeo());
        Services.AddSingleton(new HttpClient(_api) { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton(sp => new JobsyApiClient(sp.GetRequiredService<HttpClient>()));
        Services.AddSingleton<CandidateProfileEditor>();
        Services.AddSingleton<IFeatureFlags>(new FixedFlags());
    }

    [Fact]
    public async Task Candidate_adds_edits_and_sees_the_fact_on_the_passport()
    {
        var editor = Services.GetRequiredService<CandidateProfileEditor>();
        var cut = Render<EducationSection>(p => p.AddCascadingValue(editor));

        Assert.Contains("https://www.idw.nl/", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("https://www.s-bb.nl/", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Lobsy schat je niveau niet in", cut.Markup, StringComparison.Ordinal);

        await AssertMobileAsync(cut.Markup, async page =>
        {
            await Assertions.Expect(page.Locator("a[href='https://www.idw.nl/']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("a[href='https://www.s-bb.nl/']")).ToBeVisibleAsync();
        });

        cut.Find("[data-testid=diploma-eval-add]").Click();
        cut.Find("[data-testid=diploma-eval-title-input]").Input("Lisans");
        cut.Find("[data-testid=diploma-eval-level]").Input("vergelijkbaar met hbo-bachelor");
        cut.Find("[data-testid=diploma-eval-level-code]").Change("hbo-bachelor");
        cut.Find("[data-testid=diploma-eval-date]").Change("2024-06-01");
        cut.Find("[data-testid=diploma-eval-reference]").Input("IDW-99");

        await AssertMobileAsync(cut.Markup, async page =>
        {
            await page.Locator("[data-testid=diploma-eval-level]").FillAsync("vergelijkbaar met hbo-bachelor");
            await page.Locator("[data-testid=diploma-eval-reference]").FillAsync("IDW-99");
            var level = await page.Locator("[data-testid=diploma-eval-level]").InputValueAsync();
            Assert.Equal("vergelijkbaar met hbo-bachelor", level);
        });

        await cut.Find("[data-testid=diploma-eval-save]").ClickAsync();
        cut.WaitForAssertion(() => Assert.Contains("volgens waardering van Nuffic/SBB", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("vergelijkbaar met hbo-bachelor", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("IDW-99", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("vergelijkbaar met hbo-bachelor", editor.DiplomaEvaluations[0].EquivalentLevelText);
        Assert.Equal("hbo-bachelor", editor.DiplomaEvaluations[0].EquivalentLevelCode);
        cut.FindAll("button").First(b => b.TextContent.Contains("Bewerken", StringComparison.Ordinal)).Click();
        cut.Find("[data-testid=diploma-eval-reference]").Input("IDW-100");
        await cut.Find("[data-testid=diploma-eval-save]").ClickAsync();
        cut.WaitForAssertion(() => Assert.Contains("IDW-100", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("geheim-waardering.pdf", cut.Markup, StringComparison.Ordinal);

        var proof = Render<PassportProofTab>(p => p.Add(x => x.Active, true));
        proof.WaitForAssertion(() => Assert.Contains("data-testid=\"diploma-eval-display\"", proof.Markup, StringComparison.Ordinal));
        Assert.Contains("volgens waardering van Nuffic/SBB", proof.Markup, StringComparison.Ordinal);
        Assert.Contains("vergelijkbaar met hbo-bachelor", proof.Markup, StringComparison.Ordinal);
        Assert.Contains("Jouw keuze: hbo-bachelor", proof.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("geheim-waardering.pdf", proof.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("diploma-eval-download", proof.Markup, StringComparison.Ordinal);

        await AssertMobileAsync(proof.Markup, async page =>
        {
            await Assertions.Expect(page.GetByText("volgens waardering van Nuffic/SBB")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("vergelijkbaar met hbo-bachelor")).ToBeVisibleAsync();
            Assert.Equal(0, await page.GetByText("geheim-waardering.pdf").CountAsync());
        });
    }

    private static async Task AssertMobileAsync(string markup, Func<IPage, Task> assert)
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            Locale = "nl-NL"
        });
        var page = await context.NewPageAsync();
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/mijn-paspoort.css"));
        var html = "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><style>"
                   + ":root{--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;--brand:#0f2d5c;--danger:#9b1c1c;--danger-soft:#fef2f2;--success:#15803d;--success-soft:#ecfdf3;--radius-sm:8px;--font:\"Segoe UI\",\"Helvetica Neue\",Arial,sans-serif;}"
                   + "body{margin:0;padding:16px;background:#f5f2ee;color:var(--text);font-family:var(--font);font-size:16px;}"
                   + ".btn-compact{display:inline-flex;align-items:center;justify-content:center;min-height:2.75rem;padding:0.4rem 0.85rem;border:1px solid var(--border);border-radius:8px;background:#fff;color:var(--brand);font:inherit;font-weight:600;}"
                   + css
                   + "</style></head><body>"
                   + markup
                   + "</body></html>";
        await page.SetContentAsync(html);
        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);
        await assert(page);
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

        throw new InvalidOperationException("Could not locate Jobsy.sln.");
    }

    private sealed class FixedFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature is PlatformFeature.Employers or PlatformFeature.CandidatePassport);

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

    private sealed class EvalApi : HttpMessageHandler
    {
        private string _reference = "IDW-99";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (request.Method == HttpMethod.Post && path.Contains("diploma-evaluations", StringComparison.Ordinal))
            {
                var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);
                _reference = doc.RootElement.GetProperty("referenceNumber").GetString() ?? _reference;
                return Json(Fact(_reference));
            }

            if (request.Method == HttpMethod.Put && path.Contains("diploma-evaluations", StringComparison.Ordinal))
            {
                var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);
                _reference = doc.RootElement.GetProperty("referenceNumber").GetString() ?? _reference;
                return Json(Fact(_reference));
            }

            if (path.Contains("profile", StringComparison.Ordinal))
            {
                return Json(Profile());
            }

            if (path.Contains("masterdata", StringComparison.Ordinal))
            {
                return Json("[]");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private string Profile() =>
            $$"""
            {"id":"11111111-1111-1111-1111-111111111111","email":"sam@example.com","fullName":"Sam Tester","firstName":"Sam","lastName":"Tester","role":"Candidate","preferences":{"roles":[],"educations":["MBO"]},"emailVerified":true,"diplomaEvaluations":[{{Fact(_reference)}}]}
            """;

        private static string Fact(string reference) =>
            $$"""
            {"id":"22222222-2222-2222-2222-222222222222","diplomaTitle":"Lisans","issuingBody":"nuffic","equivalentLevelText":"vergelijkbaar met hbo-bachelor","equivalentLevelCode":"hbo-bachelor","evaluationDate":"2024-06-01","referenceNumber":"{{reference}}","hasDocument":true,"documentFileName":"geheim-waardering.pdf"}
            """;

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
    }
}
