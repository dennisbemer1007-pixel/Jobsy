using Bunit;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchMomentCopyTests : BunitContext
{
    public AfterMatchMomentCopyTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new()));
    }

    [Fact]
    public void FormatLeadPick_uses_singular_for_one_slot()
    {
        var text = AfterMatchMomentCopy.FormatLeadPick(Culture, "Linda", "Bakkerij De Korenaar", 1);
        Assert.Contains("één moment", text, StringComparison.Ordinal);
        Assert.DoesNotContain("1 momenten", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Employer_demo_slots_span_distinct_days()
    {
        var slots = AfterMatchEmployerDemoSlots.InitialUtc();
        Assert.Equal(3, slots.Length);
        var days = slots.Select(s => s.Date).Distinct().Count();
        Assert.Equal(3, days);
    }

    [Fact]
    public void Employer_demo_slots_are_never_in_the_past()
    {
        var notBefore = DateTime.UtcNow;
        foreach (var slot in AfterMatchEmployerDemoSlots.InitialUtc(notBefore))
        {
            Assert.True(slot > notBefore);
        }
    }
}
