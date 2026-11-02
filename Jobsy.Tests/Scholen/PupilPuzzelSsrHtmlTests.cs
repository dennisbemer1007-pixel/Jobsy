using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public sealed class PupilPuzzelSsrHtmlTests
{
    [Fact]
    public async Task Groep78_schelpenrij_play_markup_uses_pattern_choices_not_sort_stub()
    {
        var culture = await NlCultureAsync();
        var registry = new PupilQuestionSetRegistry();
        var html = PupilPuzzelSsrHtml.BuildBody(
            "schelpenrij",
            culture,
            antiforgery: "",
            savePath: "/leerling/puzzel/schelpenrij/save",
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie,
            registry,
            error: null);

        Assert.Contains("data-ll-puzzle-mode=\"pattern\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-schelp-choice=\"2\"", html, StringComparison.Ordinal);
        Assert.Contains("/leerling/werelden/img/schelp-2.png", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-ll-shell-index", html, StringComparison.Ordinal);
        Assert.Contains("Vind het patroon in de rij schelpen op het rif.", html, StringComparison.Ordinal);
    }

    private static async Task<CultureState> NlCultureAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime, NoJs>();
        services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var culture = services.BuildServiceProvider().GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        return culture;
    }

    private sealed class NoJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => default;
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
}
