using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
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
/// Deep (25/18) answers autosave as Draft. Leaving the shed must still POST complete=true
/// so paspoort/Kompas treat the free scan as afgerond.
/// </summary>
public sealed class JourneyShedFinalizeTests : BunitContext
{
    private readonly FinalizeHandler _handler = new();

    public JourneyShedFinalizeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new FixedFlags());
        Services.AddSingleton<IGeocodingClient>(new NoGeocoder());
        Services.AddSingleton<UserFacingError>();
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
        Services.AddSingleton<GratisDnaStorage>();
        Services.AddSingleton<GratisDnaMergeService>();
        Services.AddSingleton<MeGetCache>();
        Services.AddSingleton<DiscoveryOverviewCache>();
    }

    [Fact]
    public void Fully_done_draft_competency_shed_posts_complete_true()
    {
        foreach (var id in TestDepthRules.QuestionIdsUpTo(Jobsy.Core.Enums.AssessmentKind.Competence, 25))
        {
            _handler.CompetencyAnswers[id] = 4;
        }

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/candidate/ontdekkingsreis?stap=shed-7");

        var cut = Render<DiscoveryJourney>();
        cut.WaitForAssertion(() =>
            Assert.Contains("Deze test heb je helemaal gedaan", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                _handler.Calls,
                c => c.Method == HttpMethod.Put
                     && c.Path.Contains("competenc", StringComparison.OrdinalIgnoreCase)
                     && TryReadCompleteFlag(c.Body, out var complete)
                     && complete);
        });
    }

    private static bool TryReadCompleteFlag(string? json, out bool complete)
    {
        complete = false;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("complete", out var prop)
               && prop.ValueKind == JsonValueKind.True
               && (complete = true);
    }

    private sealed class FinalizeHandler : HttpMessageHandler
    {
        public Dictionary<int, int> CompetencyAnswers { get; } = new();

        public List<(HttpMethod Method, string Path, string? Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            string? body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request.Method, path, body));

            if (path.Contains("career-path", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            var responseBody = path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                ? """
                  {"currentStep":7,"isComplete":false,"wizardVersion":3,"shouldShow":true,"steps":[],"impression":{"strengths":[],"riasec":[],"matchCards":[]}}
                  """
                : path.Contains("/profile", StringComparison.OrdinalIgnoreCase)
                    ? """
                      {"id":"11111111-1111-1111-1111-111111111111","firstName":"Sanne","lastName":"Test","fullName":"Sanne Test","testAiConsentAt":"2026-10-01T00:00:00Z","preferences":{}}
                      """
                    : path.Contains("competenc", StringComparison.OrdinalIgnoreCase)
                        ? "{\"status\":\"Draft\",\"answers\":{" + string.Join(",", CompetencyAnswers.Select(pair => $"\"{pair.Key}\":{pair.Value}")) + "}}"
                        : "{\"status\":\"Draft\",\"answers\":{}}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
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
                    [new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }

    private sealed class FixedFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(false, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.CandidatePassport);

        public void Invalidate()
        {
        }
    }
}
