using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public sealed class PupilReisSsrImagineTests
{
    [Fact]
    public void Groep78_reis_renders_imagine_panel_for_current_item()
    {
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(PupilQuestionSet.Groep78);
        var imagine = culture["LeerlingQ.9001.Voorbeeld"];
        var state = Groep78State("9001");

        var html = PupilReisSsrHtml.BuildBody(
            state,
            def,
            culture,
            registry,
            antiforgeryHtml: "",
            answerSavePath: "/leerling/reis/answer",
            error: null,
            groep78: true,
            themeKey: PupilJourneyTheme.ZeeExpeditie);

        Assert.Contains("id=\"ll-imagine-9001\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"ll-question-imagine\"", html, StringComparison.Ordinal);
        Assert.Contains("ll-question__head", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-imagine-toggle", html, StringComparison.Ordinal);
        Assert.Contains(imagine, html, StringComparison.Ordinal);
        Assert.Contains(culture["Leerling.Reis.Imagine"], html, StringComparison.Ordinal);
    }

    [Fact]
    public void Vo_reis_renders_imagine_panel_for_current_item()
    {
        var culture = NlCulture();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(PupilQuestionSet.Vo);
        var imagine = culture["LeerlingQ.9101.Voorbeeld"];
        var state = VoState("9101");

        var html = PupilReisSsrHtml.BuildBody(
            state,
            def,
            culture,
            registry,
            antiforgeryHtml: "",
            answerSavePath: "/leerling/reis/answer",
            error: null,
            groep78: false,
            themeKey: null);

        Assert.Contains("id=\"ll-imagine-9101\"", html, StringComparison.Ordinal);
        Assert.Contains(imagine, html, StringComparison.Ordinal);
    }

    private static PupilProgressStateDto Groep78State(string itemId)
        => new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "7A",
            "TEST-1",
            PupilCodeStatus.InProgress,
            CurrentIndex: 0,
            TotalItems: 60,
            AnsweredCount: 0,
            PlatesShed: 0,
            NewShell: false,
            CurrentItemId: itemId,
            CurrentWorldKey: "koraalrif",
            Answers: new Dictionary<string, int>(),
            WindowOpen: true,
            Completed: false,
            NeedsIsland: false,
            IslandDone: false,
            VakkenDone: false,
            Likes: [],
            Dislikes: [],
            LikeOtherWord: null,
            DislikeOtherWord: null,
            QuestionSet: PupilQuestionSet.Groep78,
            JourneyThemeKey: PupilJourneyTheme.ZeeExpeditie);

    private static PupilProgressStateDto VoState(string itemId)
        => Groep78State(itemId) with
        {
            QuestionSet = PupilQuestionSet.Vo,
            JourneyThemeKey = null,
            TotalItems = 100,
        };

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
