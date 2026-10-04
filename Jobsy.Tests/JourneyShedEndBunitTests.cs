using System.Net;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// A finished journey opens the shed with no answers in memory. "Naar het licht" must reach
/// the end screen instead of re-posting an empty test save.
/// </summary>
public sealed class JourneyShedEndBunitTests : BunitContext
{
    private readonly RecordingHandler _handler = new();

    public JourneyShedEndBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: false, passport: true));
        Services.AddSingleton<IGeocodingClient>(new NoGeocoder());
        Services.AddSingleton<UserFacingError>();
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
        Services.AddSingleton<GratisDnaStorage>();
        Services.AddSingleton<GratisDnaMergeService>();
    }

    [Fact]
    public void Completed_shed_10_opens_the_end_screen_with_facts()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/candidate/ontdekkingsreis?stap=shed-10");

        var cut = Render<DiscoveryJourney>();
        cut.WaitForAssertion(() => Assert.Contains("Naar het licht", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Dit ben jij, Sanne", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Afmaken", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Verbinding", cut.Markup, StringComparison.Ordinal);
        });
        Assert.DoesNotContain("Niet bewaard", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Er ging iets mis", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("stap=klaar", nav.Uri, StringComparison.Ordinal);
        Assert.DoesNotContain(_handler.Calls, c => c.Contains("PUT api/me/values", StringComparison.Ordinal));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Calls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var call = $"{request.Method} {path}";
            Calls.Add(call);
            var body = BodyFor(path);
            var status = path.Contains("career-path", StringComparison.OrdinalIgnoreCase)
                ? HttpStatusCode.NoContent
                : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = status == HttpStatusCode.NoContent
                    ? null
                    : new StringContent(body, Encoding.UTF8, "application/json")
            });
        }

        private static string BodyFor(string path)
        {
            if (path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                      "currentStep": 10,
                      "isComplete": true,
                      "completedAtUtc": "2026-10-03T12:00:00Z",
                      "finishReached": true,
                      "wizardVersion": 3,
                      "shouldShow": false,
                      "steps": [],
                      "impression": {
                        "label": "Dit ben jij",
                        "strengths": [{ "code": "Resultaatgerichtheid", "label": "Afmaken", "sentence": "Je maakt af.", "percent": 90 }],
                        "riasec": [{ "code": "C", "label": "Ordenen", "sentence": "Je ordent.", "percent": 90 }],
                        "topValue": { "code": "Connection", "label": "Verbinding", "sentence": "Je verbindt.", "percent": 100 },
                        "matchCards": []
                      }
                    }
                    """;
            }

            if (path.Contains("/profile", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                      "id": "11111111-1111-1111-1111-111111111111",
                      "firstName": "Sanne",
                      "lastName": "Test",
                      "fullName": "Sanne Test",
                      "testAiConsentAt": "2026-10-01T00:00:00Z",
                      "preferences": { "homeAddress": "2671 AA Naaldwijk" }
                    }
                    """;
            }

            if (path.Contains("/values", StringComparison.OrdinalIgnoreCase))
            {
                var answers = string.Join(",", Enumerable.Range(1, 25).Select(id => $"\"{id}\":4"));
                return "{\"status\":\"Completed\",\"answers\":{" + answers + "},\"completedAtUtc\":\"2026-10-03T12:00:00Z\"}";
            }

            if (path.Contains("competenc", StringComparison.OrdinalIgnoreCase)
                || path.Contains("career-interest", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/culture", StringComparison.OrdinalIgnoreCase))
            {
                return "{\"status\":\"Completed\",\"answers\":{}}";
            }

            return "{}";
        }
    }

    private sealed class NoGeocoder : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "sanne"), new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }

    private sealed class FixedFlags(bool employers, bool passport) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, passport));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature switch
            {
                PlatformFeature.Employers => employers,
                PlatformFeature.CandidatePassport => passport,
                _ => false
            });

        public void Invalidate()
        {
        }
    }
}
