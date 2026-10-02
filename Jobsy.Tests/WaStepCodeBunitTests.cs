using Bunit;
using Jobsy.Web.Auth;
using Jobsy.Web.Components.Registration;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class WaStepCodeBunitTests : TestContext
{
    public WaStepCodeBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Expired_parameter_shows_expired_state_and_resets_when_cleared()
    {
        var state = new RegistrationWizardState
        {
            ContactEmail = "demo@jobsy.local",
            VerificationExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        var cut = RenderComponent<WaStepCode>(p => p
            .Add(c => c.State, state)
            .Add(c => c.Expired, true));

        Assert.Contains("wa-note--warn", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("wa-input--otp", cut.Markup, StringComparison.Ordinal);

        cut.SetParametersAndRender(p => p
            .Add(c => c.State, state)
            .Add(c => c.Expired, false));

        Assert.DoesNotContain("wa-note--warn", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("wa-input--otp", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
