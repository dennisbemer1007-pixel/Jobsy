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

/// <summary>Choosing "Iets dieper" on the shed must open question 6 of 10, not the same shed.</summary>
public sealed class JourneyDiveDeeperBunitTests : BunitContext
{
    public JourneyDiveDeeperBunitTests()
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
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new Handler()) { BaseAddress = new Uri("http://localhost") }));
        Services.AddSingleton<GratisDnaStorage>();
        Services.AddSingleton<GratisDnaMergeService>();
    }

    [Fact]
    public void Shed_deeper_opens_question_six_of_ten()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/candidate/ontdekkingsreis?stap=shed-7");

        var cut = Render<DiscoveryJourney>();
        cut.WaitForAssertion(() => Assert.Contains("Iets dieper", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button").First(b => b.TextContent.Contains("Iets dieper", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("Duik dieper", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn-compact--primary").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Vraag 6 van 10", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("diepte=10", nav.Uri, StringComparison.Ordinal);
        Assert.DoesNotContain("Niet bewaard", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("career-path", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            var body = path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                ? """
                  {"currentStep":7,"isComplete":false,"wizardVersion":3,"shouldShow":true,"steps":[],"impression":{"strengths":[],"riasec":[],"matchCards":[]}}
                  """
                : path.Contains("/profile", StringComparison.OrdinalIgnoreCase)
                    ? """
                      {"id":"11111111-1111-1111-1111-111111111111","firstName":"Sanne","lastName":"Test","fullName":"Sanne Test","testAiConsentAt":"2026-10-01T00:00:00Z","preferences":{}}
                      """
                    : path.Contains("competenc", StringComparison.OrdinalIgnoreCase)
                        ? "{\"status\":\"Draft\",\"answers\":{" + string.Join(",", new[] { 1, 6, 11, 16, 21 }.Select(id => $"\"{id}\":4")) + "}}"
                        : "{\"status\":\"Draft\",\"answers\":{}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
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
