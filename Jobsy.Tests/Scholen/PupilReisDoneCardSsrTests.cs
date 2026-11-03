using Bunit;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public sealed class PupilReisDoneCardSsrTests : BunitContext
{
    public PupilReisDoneCardSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Groep78_done_card_includes_scene_card_and_coach()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var registry = new PupilQuestionSetRegistry();
        var html = PupilReisSsrHtml.DoneCard(culture, true, PupilJourneyTheme.ZeeExpeditie, registry);

        Assert.Contains("data-testid=\"leerling-reis-klaar\"", html, StringComparison.Ordinal);
        Assert.Contains("ll-g78-scene", html, StringComparison.Ordinal);
        Assert.Contains("class=\"card ll-reis-done-card\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"ll-g78-coach\"", html, StringComparison.Ordinal);
        Assert.Contains("btn btn-main", html, StringComparison.Ordinal);
        Assert.True(PageAssetCatalog.IncludesLeerlingGroep78ThemeStyles("/leerling/reis"));
    }

    [Fact]
    public void Groep78_stop_pause_page_includes_g78_theme_stylesheet_route()
    {
        Assert.True(PageAssetCatalog.IncludesLeerlingGroep78ThemeStyles("/leerling/stop?done=1&g78=1"));
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
