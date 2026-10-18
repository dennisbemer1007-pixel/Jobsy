using Bunit;
using Bunit.TestDoubles;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class ReferenceVerificationBunitTests : BunitContext
{
    public ReferenceVerificationBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<bool>("jobsyRefVerificationTip.isDismissed", _ => false);
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuth()));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
    }

    [Fact]
    public void Tip_renders_when_eligible()
    {
        var cut = Render<ReferenceVerificationTipCard>(p => p
            .Add(x => x.ScreenKey, "paspoort-home")
            .Add(x => x.Eligible, true)
            .Add(x => x.UserId, "aaaaaaaa-1111-1111-1111-111111111111"));
        Assert.Contains("Tip van Lobsy", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Referent vragen", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Tip_hidden_when_not_eligible()
    {
        var cut = Render<ReferenceVerificationTipCard>(p => p
            .Add(x => x.ScreenKey, "paspoort-home")
            .Add(x => x.Eligible, false)
            .Add(x => x.UserId, "aaaaaaaa-1111-1111-1111-111111111111"));
        Assert.DoesNotContain("Tip van Lobsy", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ref-verification-tip", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
