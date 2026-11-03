using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public sealed class PupilEilandSsrHtmlTests
{
    [Fact]
    public void Groep78_island_renders_two_steps_journey_meta_and_coach_copy()
    {
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(PupilQuestionSet.Groep78);
        var progress = new PupilProgressStateDto(
            Guid.NewGuid(),
            "8B",
            "ABC-DEF",
            PupilCodeStatus.InProgress,
            30,
            def.Bank.AllItems.Count,
            30,
            0,
            false,
            def.Bank.AllItems[29].Id,
            "pauze-eiland",
            new Dictionary<string, int>(),
            true,
            false,
            true,
            false,
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null,
            PupilQuestionSet.Groep78,
            "island",
            null,
            PupilJourneyTheme.ZeeExpeditie,
            DateTime.UtcNow);

        var html = PupilEilandSsrHtml.BuildBody(
            progress,
            def,
            culture,
            registry,
            antiforgery: "",
            savePath: "/leerling/eiland/save",
            error: null,
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie);

        Assert.Contains("data-ll-island-step=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-island-step=\"2\"", html, StringComparison.Ordinal);
        Assert.Contains("Stap 1 van 2", html, StringComparison.Ordinal);
        Assert.Contains("Stap 2 van 2", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-island-jmeta=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-island-jmeta=\"2\"", html, StringComparison.Ordinal);
        Assert.Contains("stap 1 van 2", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stap 2 van 2", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Even rust. Waar word jij blij van?", html, StringComparison.Ordinal);
        Assert.Contains("Wat jij niet leuk vindt", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-island-step=\"2\" hidden", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-island-coach-step=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-island-coach-step=\"2\"", html, StringComparison.Ordinal);
        Assert.Contains("Kies wat jij niet leuk vindt", html, StringComparison.Ordinal);
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
        var culture = services.BuildServiceProvider().GetRequiredService<CultureState>();
        culture.SetLanguageAsync("nl").GetAwaiter().GetResult();
        return culture;
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
