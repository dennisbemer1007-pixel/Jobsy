using System.Net;
using System.Text;
using Bunit;
using Jobsy.Core.Enums;
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
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
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
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Vraag 6 van 10", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("diepte=10", nav.Uri, StringComparison.Ordinal);
        Assert.DoesNotContain("Niet bewaard", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Finish_after_dive_opens_the_shed_when_earlier_answers_are_already_saved()
    {
        var saved = TestDepthRules.QuestionIdsUpTo(AssessmentKind.Competence, 10).Take(6).ToList();
        foreach (var id in saved)
        {
            _handler.CompetencyAnswers[id] = 4;
        }

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/candidate/ontdekkingsreis?stap=shed-7");
        var cut = Render<DiscoveryJourney>();
        cut.WaitForAssertion(() => Assert.Contains("Iets dieper", cut.Markup, StringComparison.Ordinal));
        cut.FindAll("button").First(b => b.TextContent.Contains("Iets dieper", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("Duik dieper", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Vraag 7 van 10", cut.Markup, StringComparison.Ordinal));

        for (var step = 0; step < 4; step++)
        {
            cut.Find("input[type=radio][value='4']").Change(new ChangeEventArgs { Value = "4" });
            cut.Find("button.test-flow__next").Click();
        }

        cut.WaitForAssertion(() => Assert.Contains("Wil je dieper", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("Niet bewaard", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Vraag 10 van 10", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("shed-7", nav.Uri, StringComparison.Ordinal);
    }

    private readonly Handler _handler = new();

    private sealed class Handler : HttpMessageHandler
    {
        public Dictionary<int, int> CompetencyAnswers { get; } = new()
        {
            [1] = 4,
            [6] = 4,
            [11] = 4,
            [16] = 4,
            [21] = 4
        };

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
                        ? "{\"status\":\"Draft\",\"answers\":{" + string.Join(",", CompetencyAnswers.Select(pair => $"\"{pair.Key}\":{pair.Value}")) + "}}"
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
