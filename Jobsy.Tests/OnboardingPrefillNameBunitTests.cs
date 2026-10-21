using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Microsoft.AspNetCore.Components;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class OnboardingPrefillNameBunitTests : BunitContext
{
    public OnboardingPrefillNameBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new FixedFlags());
        Services.AddSingleton<ITestJourneyIllustrationGate>(new FixedTestJourneyGate());
        Services.AddSingleton<IGeocodingClient>(new NoGeocoder());
        Services.AddSingleton<UserFacingError>();
        Services.AddSingleton<GratisDnaStorage>();
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddScoped<ICandidateHeroCompactPreviewGate>(_ => CandidateHeroCompactPreviewGateStub.None);
        Services.AddSingleton(sp => new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
        Services.AddSingleton(sp => new GratisDnaMergeService(
            sp.GetRequiredService<GratisDnaStorage>(),
            sp.GetRequiredService<JobsyApiClient>(),
            sp.GetRequiredService<ILogger<GratisDnaMergeService>>()));
        Services.AddSingleton<IReferenceVerificationFeatureGate>(new StubReferenceGate());
        Services.AddSingleton<IOnboardingFinaleV2Gate>(new FixedFinaleGate());
        Services.AddSingleton<IOnboardingRustigGate>(new FixedRustigGate());
    }

    [Fact]
    public void Step1_leaves_voornaam_empty_when_profile_first_name_is_email()
    {
        _handler.ProfileJson = """
            {
              "id":"11111111-1111-1111-1111-111111111111",
              "email":"dbemer@live.com",
              "firstName":"dbemer@live.com",
              "lastName":"",
              "fullName":"dbemer@live.com",
              "dateOfBirth":"2000-01-01",
              "preferences":{}
            }
            """;
        _handler.CurrentStep = 1;
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/candidate/start");

        var cut = Render<OnboardingWizard>();
        cut.WaitForAssertion(() => cut.Find("#onboarding-field-firstname input"));

        var firstNameInput = cut.Find("#onboarding-field-firstname input");
        Assert.Equal(string.Empty, firstNameInput.GetAttribute("value") ?? string.Empty);
    }

    private readonly Handler _handler = new();

    private sealed class Handler : HttpMessageHandler
    {
        public int CurrentStep { get; set; } = 1;
        public string ProfileJson { get; set; } = """
            {"id":"11111111-1111-1111-1111-111111111111","firstName":"Sanne","lastName":"Test","fullName":"Sanne Test","dateOfBirth":"2000-01-01","preferences":{}}
            """;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var body = path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                ? OnboardingBody(CurrentStep)
                : path.Contains("/profile", StringComparison.OrdinalIgnoreCase)
                    ? ProfileJson
                    : path.Contains("competenc", StringComparison.OrdinalIgnoreCase)
                        ? """{"status":"Draft","answers":{}}"""
                        : "{}";
            return Task.FromResult(Json(body));
        }

        private static string OnboardingBody(int step)
            => "{\"currentStep\":" + step
               + ",\"isComplete\":false,\"wizardVersion\":3,\"shouldShow\":true,\"steps\":[{\"step\":2,\"completedAtUtc\":null}],\"impression\":{\"strengths\":[],\"riasec\":[],\"matchCards\":[]}}";

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
    }

    private sealed class FixedFinaleGate : IOnboardingFinaleV2Gate
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);
    }

    private sealed class FixedRustigGate : IOnboardingRustigGate
    {
        public bool Enabled => false;
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

    private sealed class FixedTestJourneyGate : ITestJourneyIllustrationGate
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);
    }

    private sealed class FixedFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(false, false));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);

        public void Invalidate()
        {
        }
    }

    private sealed class FakeHostEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class StubReferenceGate : IReferenceVerificationFeatureGate
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);
    }
}
