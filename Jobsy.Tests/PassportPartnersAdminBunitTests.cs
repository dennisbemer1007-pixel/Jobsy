using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Admin;
using Jobsy.Web.Components.Pages.Admin;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class PassportPartnersAdminBunitTests : BunitContext
{
    public PassportPartnersAdminBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuth(CreateAdmin())));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth(CreateAdmin()));
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9") }));
        Services.AddSingleton<IFeatureFlags>(new PartnersOnFlags());
    }

    [Fact]
    public void Admin_page_renders_the_partner_title()
    {
        var cut = Render<PassportPartnersAdmin>();
        Assert.Contains("Paspoortpartners", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Opslaan lukte niet", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_partner_form_asks_for_the_required_fields_in_dutch()
    {
        var cut = Render<PassportPartnersAdmin>();
        cut.Find("button.btn-compact--primary").Click();
        Assert.Contains("Vul een geldig bedrijfs-id in.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Vul een weergavenaam in.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Opslaan lukte niet", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logo_upload_rejects_svg_and_files_over_512kb()
    {
        var cut = Render<PassportPartnerLogoField>();
        await cut.InvokeAsync(() => cut.Instance.ApplySelection("merk.svg", "image/svg+xml", 120));
        Assert.Contains("SVG", cut.Markup, StringComparison.Ordinal);

        await cut.InvokeAsync(() => cut.Instance.ApplySelection("merk.png", "image/png", PassportPartnerLogoRules.MaxBytes + 1));
        Assert.Contains("512", cut.Markup, StringComparison.Ordinal);
    }

    private static ClaimsPrincipal CreateAdmin()
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "Admin")
        ], "test"));

    private sealed class FakeAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class PartnersOnFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                PassportPartnersEnabled: true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.PassportPartners);

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
