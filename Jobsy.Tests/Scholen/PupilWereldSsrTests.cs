using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public class PupilWereldSsrTests
{
    [Fact]
    public void BuildBody_without_saved_theme_never_marks_a_card_selected()
    {
        var culture = NlCulture();
        var html = PupilWereldSsrHtml.BuildBody(
            culture,
            antiforgeryHtml: "",
            savedTheme: null,
            savePath: "/leerling/wereld/save");

        Assert.DoesNotContain("class=\"wcard sel", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Gekozen", html, StringComparison.Ordinal);
        Assert.Contains(PupilGroep78SsrParts.CheckSvg, html, StringComparison.Ordinal);
        Assert.Contains("class=\"same\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBody_with_saved_theme_marks_matching_card_and_info_check()
    {
        var culture = NlCulture();
        var html = PupilWereldSsrHtml.BuildBody(
            culture,
            antiforgeryHtml: "",
            savedTheme: PupilJourneyTheme.Eilandtuin,
            savePath: "/leerling/wereld/save");

        Assert.Contains("data-testid=\"ll-wereld-tuin\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"wcard sel", html, StringComparison.Ordinal);
        Assert.Contains("Gekozen", html, StringComparison.Ordinal);
        Assert.Contains(
            """<span class="pick">Gekozen</span></div><div class="wbody">""",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"ll-wereld-zee\" class=\"wcard sel", html, StringComparison.Ordinal);
    }

    private static CultureState NlCulture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        services.AddSingleton<IJSRuntime, NoJsRuntime>();
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        return services.BuildServiceProvider().GetRequiredService<CultureState>();
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class NoJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => default;

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => default;
    }
}
