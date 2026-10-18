using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Microsoft.AspNetCore.Components;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// Step 6 dream validation must stay on step 6 and must not leak into step 7 after "Weet ik nog niet".
/// </summary>
public sealed class OnboardingWizardDreamValidationBunitTests : BunitContext
{
    public OnboardingWizardDreamValidationBunitTests()
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
        Services.AddSingleton<GratisDnaStorage>();
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton(sp => new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
        Services.AddSingleton(sp => new GratisDnaMergeService(
            sp.GetRequiredService<GratisDnaStorage>(),
            sp.GetRequiredService<JobsyApiClient>(),
            sp.GetRequiredService<ILogger<GratisDnaMergeService>>()));
    }

    [Fact]
    public void Dream_validation_clears_when_leaving_step_6_via_unknown()
    {
        _handler.CurrentStep = 6;
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/candidate/start");

        var cut = Render<OnboardingWizard>();
        cut.WaitForAssertion(() =>
            Assert.Contains("droombaan", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid=onboarding-next]").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains("Kies of typ een droombaan", cut.Markup, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("state-message error", cut.Markup, StringComparison.Ordinal);

        var unknown = cut.FindAll(".ob-wizard__footer-extra")
            .First(b => b.TextContent.Contains("Weet ik nog niet", StringComparison.OrdinalIgnoreCase));
        unknown.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Kies of typ een droombaan", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("state-message error", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Nu even over jou", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    private readonly Handler _handler = new();

    private sealed class Handler : HttpMessageHandler
    {
        public int CurrentStep { get; set; } = 6;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/onboarding/dream-job", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            if (path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                && request.Method == HttpMethod.Put)
            {
                return Task.FromResult(Json(OnboardingBody(CurrentStep)));
            }

            var body = path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                ? OnboardingBody(CurrentStep)
                : path.Contains("/profile", StringComparison.OrdinalIgnoreCase)
                    ? """
                      {"id":"11111111-1111-1111-1111-111111111111","firstName":"Sanne","lastName":"Test","fullName":"Sanne Test","dateOfBirth":"2000-01-01","preferences":{}}
                      """
                    : path.Contains("competenc", StringComparison.OrdinalIgnoreCase)
                        ? """{"status":"Draft","answers":{}}"""
                        : "{}";
            return Task.FromResult(Json(body));
        }

        private static string OnboardingBody(int step)
            => "{\"currentStep\":" + step
               + ",\"isComplete\":false,\"wizardVersion\":3,\"shouldShow\":true,\"steps\":[],\"impression\":{\"strengths\":[],\"riasec\":[],\"matchCards\":[]}}";

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
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
}
