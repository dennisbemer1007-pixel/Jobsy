using Microsoft.AspNetCore.Http;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public sealed class PupilStopSsrHtmlTests
{
    [Fact]
    public void Groep78_paused_stop_omits_coach_aside()
    {
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var journey = PupilGroep78JourneySnapshot.FromPauseQuery(
            new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["g78"] = "1",
                ["jn"] = "koraalrif",
                ["ja"] = "5",
                ["jt"] = "60",
            }),
            culture,
            registry,
            PupilJourneyTheme.ZeeExpeditie);

        var html = PupilStopSsrHtml.BuildBody(
            culture,
            done: "1",
            antiforgeryField: "",
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie,
            registry: registry,
            journey: journey);

        Assert.Contains("Goed gedaan", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"ll-g78-coach\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ll-g78-coach-mobile", html, StringComparison.Ordinal);
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
