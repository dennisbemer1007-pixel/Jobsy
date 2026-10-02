using Bunit;
using Jobsy.Web.Auth;
using Jobsy.Web.Components.Pages.Public;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class AccountMakenBunitTests : TestContext
{
    public AccountMakenBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        Services.AddSingleton<IAntiforgery>(new FakeAntiforgery());
        Services.AddSingleton<IExternalAuthCredentialSource>(new FakeExternalAuth(false, false));
        Services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch(true));
        Services.AddSingleton<NavigationManager>(new FakeNavigation("/account-maken"));
        Services.AddSingleton(new GratisDnaStorage(JSInterop.JSRuntime));
    }

    [Fact]
    public void Hides_providers_when_unconfigured_and_shows_email_form()
    {
        var cut = RenderComponent<AccountMaken>();
        Assert.DoesNotContain("Doorgaan met Google", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Doorgaan met Microsoft", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Stuur mijn code", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("/account/email-code/start", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Maak je gratis account", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_register_box_for_van_ontdek()
    {
        Services.AddSingleton<NavigationManager>(new FakeNavigation("/account-maken?van=ontdek"));
        var cut = RenderComponent<AccountMaken>();
        Assert.Contains("gd-register-box", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Bedrijf registreren", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_employer_register_when_employers_off()
    {
        Services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch(false));
        var cut = RenderComponent<AccountMaken>();
        Assert.DoesNotContain("Bedrijf registreren", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Login_page_source_contains_create_account_cta()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Login.razor");
        var text = File.ReadAllText(path);
        Assert.Contains("Login.CreateAccountCta", text, StringComparison.Ordinal);
        Assert.Contains("PublicRoutes.CreateAccount", text, StringComparison.Ordinal);
        Assert.Contains("Nieuw bij Lobsy", UiStrings.Get("Login.CreateAccountLead", "nl"), StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public void ValidateRequest(HttpContext _) { }
        public Task ValidateRequestAsync(HttpContext _) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }

    private sealed class FakeExternalAuth(bool entra, bool google) : IExternalAuthCredentialSource
    {
        public Task<bool> IsEntraConfiguredAsync(CancellationToken ct = default) => Task.FromResult(entra);
        public Task<bool> IsGoogleConfiguredAsync(CancellationToken ct = default) => Task.FromResult(google);
        public Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
        public Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
    }

    private sealed class FixedEmployersSwitch(bool enabled) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(enabled);
        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(enabled ? LandingVariant.On : LandingVariant.Zw);
    }

    private sealed class FakeNavigation : NavigationManager
    {
        public FakeNavigation(string uri) => Initialize("http://localhost/", "http://localhost" + uri);
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
}
