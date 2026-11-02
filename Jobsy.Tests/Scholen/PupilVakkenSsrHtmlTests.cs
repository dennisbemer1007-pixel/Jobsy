using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public class PupilVakkenSsrHtmlTests
{
    [Fact]
    public void Groep78_body_includes_mockup_shell_markers_and_g78_foot_controls()
    {
        var culture = NlCulture();
        var html = PupilVakkenSsrHtml.BuildBody(
            null!,
            culture,
            page: 1,
            scores: new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase),
            antiforgery: "",
            savePath: "/leerling/vakken/save",
            error: null,
            groep78: true,
            themeKey: PupilJourneyTheme.Sterrenkust);

        Assert.Contains("data-testid=\"ll-vt-scene-bar\"", html, StringComparison.Ordinal);
        Assert.Contains("ll-vt-foot", html, StringComparison.Ordinal);
        Assert.Contains("ll-vt-next", html, StringComparison.Ordinal);
        Assert.Contains("ll-vt-scale-btns", html, StringComparison.Ordinal);
        Assert.Contains("Sterrenkust", html, StringComparison.Ordinal);
        Assert.Contains("disabled", html, StringComparison.Ordinal);
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

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => default;
    }
}
