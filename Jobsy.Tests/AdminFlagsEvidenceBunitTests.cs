using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Core.Hosting;
using Jobsy.Web.Admin;
using Jobsy.Web.Components.Admin.Sections;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>Smoke: PlatformSettingsEditor renders switches (styled captures: AdminFlagsPlaywrightTests r2).</summary>
public sealed class AdminFlagsEvidenceBunitTests : BunitContext
{
    [Fact]
    public void Renders_admin_switches_without_raw_resource_keys()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuthStateProvider(CreateAdmin())));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(CreateAdmin()));
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton(new DeploymentEnvironmentLabel(DeploymentEnvironment.Lokaal));
        var http = new HttpClient(new CapturingFeaturesHandler { Features = SampleFeatures() })
        {
            BaseAddress = new Uri("http://localhost")
        };
        Services.AddSingleton(new JobsyApiClient(http));
        Services.AddSingleton(new Jobsy.Web.Navigation.BottomNavRefreshService());
        Services.AddSingleton<Jobsy.Core.Features.IFeatureFlags>(new AlwaysOffFlags());
        AdminEditorBunitServices.AddUserFacingError(Services);

        var cut = Render<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));
        cut.WaitForElement(".admin-settings-group");

        Assert.Contains("admin-switch", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminSettings.EmployerFitQuestionnaires", cut.Markup, StringComparison.Ordinal);
    }

    private static PlatformFeatureItem SampleFeatures() => new()
    {
        VacancyContentModerationEnabled = true,
        EmployersEnabled = true,
        CandidatePassportEnabled = true,
        SensitiveExperienceEnabled = false,
        TrainingPartnersEnabled = true,
        KandidatenkaartEnabled = false,
        PublicWebBaseUrl = "http://localhost:5201",
        UpdatedAtUtc = DateTime.UtcNow,
        AdminOverriddenFlagKeys = ["TrainingPartnersEnabled"]
    };

    private static ClaimsPrincipal CreateAdmin()
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test"));

    private sealed class AlwaysOffFlags : Jobsy.Core.Features.IFeatureFlags
    {
        public ValueTask<Jobsy.Core.Features.FeatureFlagSnapshot> GetAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Jobsy.Core.Features.FeatureFlagSnapshot.Defaults);
        public ValueTask<bool> IsEnabledAsync(Jobsy.Core.Features.PlatformFeature feature, CancellationToken ct = default)
            => ValueTask.FromResult(false);
        public void Invalidate() { }
    }

    private sealed class CapturingFeaturesHandler : HttpMessageHandler
    {
        public PlatformFeatureItem Features { get; set; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.Contains("platform-features", StringComparison.Ordinal) == true)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(Features);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
            }

            if (request.RequestUri?.AbsolutePath.Contains("admin/audit", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"items":[],"totalCount":0}""", System.Text.Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }
}
