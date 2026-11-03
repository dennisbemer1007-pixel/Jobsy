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
    public void Groep78_schelpenrij_play_markup_uses_pattern_choices_not_sort_stub()
    {
        var culture = NlCulture();
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

    [Fact]
    public void Groep78_schatkaart_intro_uses_match_preview_not_schelpen_copy()
    {
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var html = PupilPuzzelSsrHtml.BuildBody(
            "schatkaart",
            culture,
            antiforgery: "",
            savePath: "/leerling/puzzel/schatkaart/save",
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie,
            registry,
            error: null);

        Assert.Contains("Draai kaarten om en vind de paren.", html, StringComparison.Ordinal);
        Assert.Contains("prev-row--cards", html, StringComparison.Ordinal);
        Assert.Contains("/puzzles/zee/match-1.jpg", html, StringComparison.Ordinal);
        Assert.Contains("/puzzles/zee/match-2.jpg", html, StringComparison.Ordinal);
        Assert.Contains("/puzzles/zee/match-3.jpg", html, StringComparison.Ordinal);
        Assert.Contains("slot--card-face", html, StringComparison.Ordinal);
        Assert.Contains("Bekijk de kaarten op tafel.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Vind het patroon in de rij schelpen", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-match-src", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-puzzle-kind=\"schatkaart\"", html, StringComparison.Ordinal);
        Assert.Contains("/leerling/werelden/img/kreeft-kaart.png", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Groep78_vuurtorenlampen_intro_uses_lamp_preview()
    {
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var html = PupilPuzzelSsrHtml.BuildBody(
            "vuurtorenlampen",
            culture,
            antiforgery: "",
            savePath: "/leerling/puzzel/vuurtorenlampen/save",
            groep78: true,
            themeKey: PupilJourneyTheme.Sterrenkust,
            registry,
            error: null);

        Assert.Contains("Onthoud het sterrenlicht en tik het patroon na.", html, StringComparison.Ordinal);
        Assert.Contains("prev-row--lamps", html, StringComparison.Ordinal);
        Assert.Contains("slot--beacon", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-lamp-tone=\"1\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/puzzles/ster/lamp-", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-lamp-watch", html, StringComparison.Ordinal);
        Assert.DoesNotContain("prev-row--cards", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Groep78_vuurtorenlampen_play_uses_beacon_lamps()
    {
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var html = PupilPuzzelSsrHtml.BuildBody(
            "vuurtorenlampen",
            culture,
            antiforgery: "",
            savePath: "/leerling/puzzel/vuurtorenlampen/save",
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie,
            registry,
            error: null);

        Assert.Contains("ll-puzzle__beacon", html, StringComparison.Ordinal);
        Assert.Contains("Onthoud het lichtpatroon en tik het na.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/puzzles/zee/lamp-", html, StringComparison.Ordinal);
    }

    private static CultureState NlCulture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime, NoJs>();
        services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var culture = services.BuildServiceProvider().GetRequiredService<CultureState>();
        culture.SetLanguageAsync("nl").GetAwaiter().GetResult();
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
