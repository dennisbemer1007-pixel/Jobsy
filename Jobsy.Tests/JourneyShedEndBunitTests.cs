using System.Net;
using System.Text;
using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using System.Text.Json;
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
        Services.AddSingleton(sp =>
        {
            var culture = new CultureState(
                sp.GetRequiredService<IJSRuntime>(),
                sp,
                sp.GetRequiredService<AuthenticationStateProvider>());
            culture.InitializeFromLanguage("nl");
            return culture;
        });
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: false, passport: true));
        Services.AddSingleton<IGeocodingClient>(new NoGeocoder());
        Services.AddSingleton<UserFacingError>();
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
        Services.AddSingleton<GratisDnaStorage>();
        Services.AddSingleton<GratisDnaMergeService>();
        Services.AddSingleton<MeGetCache>();
        Services.AddSingleton<DiscoveryOverviewCache>();
    }

    [Fact]
    public void Values_v3_mini_fixture_satisfies_step_10_gate()
    {
        var json = JourneyBunitMiniResponses.CompletedMiniJson(AssessmentKind.Values);
        var state = JsonSerializer.Deserialize<CandidateValuesState>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(state);
        Assert.True(OnboardingMiniBlockCompletion.IsMiniBlockComplete(
            10,
            AssessmentTestVersions.V3,
            OnboardingMiniBlockCompletion.ScopeToMiniIds(AssessmentKind.Values, AssessmentTestVersions.V3, state!.Answers
                .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value))));
    }

    [Fact]
    public void Completed_shed_10_opens_the_end_screen_with_facts()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/candidate/ontdekkingsreis?stap=shed-10");

        var cut = Render<DiscoveryJourney>();
        cut.WaitForAssertion(() =>
            Assert.Contains("Deze test heb je helemaal gedaan", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("stap=klaar", nav.Uri, StringComparison.Ordinal);
            Assert.Contains("Dit ben jij, Sanne", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Afmaken", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Verbinding", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(15));
        Assert.DoesNotContain("Niet bewaard", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Er ging iets mis", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("stap=klaar", nav.Uri, StringComparison.Ordinal);
        Assert.DoesNotContain(
            _handler.Calls,
            c => c.Contains("PUT", StringComparison.Ordinal)
                 && c.Contains("/values", StringComparison.OrdinalIgnoreCase)
                 && c.Contains("\"answers\":{}", StringComparison.Ordinal));
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
            if (JourneyBunitMiniResponses.IsDiscoveryOverviewPath(path))
            {
                return JourneyBunitMiniResponses.DiscoveryOverviewPanelJson(25, 25, 25, 25);
            }

            if (JourneyBunitMiniResponses.IsOnboardingStatePath(path))
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
                return JourneyBunitMiniResponses.CompletedMiniJson(AssessmentKind.Values);
            }

            if (path.Contains("career-interest", StringComparison.OrdinalIgnoreCase))
            {
                return JourneyBunitMiniResponses.CompletedMiniJson(AssessmentKind.Career);
            }

            if (path.Contains("/culture", StringComparison.OrdinalIgnoreCase))
            {
                return JourneyBunitMiniResponses.CompletedMiniJson(AssessmentKind.Culture);
            }

            if (path.Contains("competenc", StringComparison.OrdinalIgnoreCase))
            {
                return JourneyBunitMiniResponses.CompletedMiniJson(AssessmentKind.Competence);
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
                PlatformFeature.TestsV2 => true,
                _ => false
            });

        public void Invalidate()
        {
        }
    }
}
