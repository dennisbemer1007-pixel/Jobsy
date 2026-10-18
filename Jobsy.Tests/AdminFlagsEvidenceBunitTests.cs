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

/// <summary>Exports admin feature settings markup for visual review (admin-flags-r1).</summary>
public sealed class AdminFlagsEvidenceBunitTests : BunitContext
{
    [Fact]
    public void Export_admin_settings_markup_for_desktop_and_mobile()
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

        var cut = Render<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));
        cut.WaitForElement(".admin-settings-group");

        var dir = Path.Combine("/opt/cursor/artifacts", "admin-flags-r1");
        Directory.CreateDirectory(dir);
        var beforeSensitive = cut.Markup;
        File.WriteAllText(Path.Combine(dir, "admin-settings-desktop.html"), WrapHtml(beforeSensitive, 1440));
        File.WriteAllText(Path.Combine(dir, "admin-settings-mobile.html"), WrapHtml(beforeSensitive, 390));
        File.WriteAllText(Path.Combine(dir, "flag-before-sensitive-off.html"), WrapHtml(beforeSensitive, 1440));

        cut.Instance.SetDraftForTests("SensitiveExperienceEnabled", true);
        cut.Render();
        var afterSensitive = cut.Markup;
        File.WriteAllText(Path.Combine(dir, "admin-settings-sensitive-on-draft.html"), WrapHtml(afterSensitive, 1440));
        File.WriteAllText(Path.Combine(dir, "flag-after-sensitive-on.html"), WrapHtml(afterSensitive, 1440));
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

    private static string WrapHtml(string body, int width)
        => "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>"
           + $"<style>body{{max-width:{width}px;margin:0 auto;font-family:system-ui,sans-serif;}}</style>"
           + "</head><body>" + body + "</body></html>";

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
