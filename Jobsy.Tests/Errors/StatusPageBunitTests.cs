using Bunit;
using Jobsy.Web.Components.Pages.Status;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Errors;

public class StatusPageBunitTests : TestContext
{
    public StatusPageBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    }

    [Fact]
    public void NotFound_has_exactly_three_actions_with_the_employers_on_hrefs()
    {
        Services.AddSingleton<IEmployersSwitch>(new FixedSwitch(true));

        var cut = RenderComponent<StatusPage>(p => p.Add(x => x.Code, 404));
        var actions = cut.FindAll(".err-actions a").ToList();

        Assert.Equal(3, actions.Count);
        Assert.Equal("/banenkaart", actions[0].GetAttribute("href"));
        Assert.Equal("/ontdek", actions[1].GetAttribute("href"));
        Assert.Equal("/hoe-werkt-lobsy", actions[2].GetAttribute("href"));
        Assert.Contains("Banenkaart", actions[0].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void NotFound_swaps_the_primary_action_when_employers_are_off()
    {
        Services.AddSingleton<IEmployersSwitch>(new FixedSwitch(false));

        var cut = RenderComponent<StatusPage>(p => p.Add(x => x.Code, 404));
        var actions = cut.FindAll(".err-actions a").ToList();

        Assert.Equal(3, actions.Count);
        Assert.Equal("/ontdek", actions[0].GetAttribute("href"));
        Assert.Contains("Mijn Paspoort", actions[0].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Support_line_links_the_support_mailbox()
    {
        Services.AddSingleton<IEmployersSwitch>(new FixedSwitch(true));

        var cut = RenderComponent<StatusPage>(p => p.Add(x => x.Code, 404));

        Assert.Equal("mailto:support@lobsy.nl", cut.Find(".err-support__link").GetAttribute("href"));
    }

    private sealed class FixedSwitch(bool enabled) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(enabled);

        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(enabled ? LandingVariant.On : LandingVariant.Zw);
    }

    private sealed class AnonymousAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity())));
    }
}
