using System.Security.Claims;
using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class OnboardingFinaleV2BunitTests : BunitContext
{
    public OnboardingFinaleV2BunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Soon_section_absent_when_all_three_features_on()
    {
        var input = new OnboardingFinaleCardsInput(true, true, true, true, true, true);
        var cut = Render<OnboardingFinaleV2>(p => p
            .Add(x => x.FirstName, "Sanne")
            .Add(x => x.CardsInput, input));

        Assert.DoesNotContain("onboarding-finale-soon", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gate_is_off_by_default()
    {
        var gate = new OnboardingFinaleV2Gate(new FixedFinaleFlags(false));
        Assert.False(await gate.IsEnabledAsync());
    }

    [Fact]
    public async Task Gate_reads_feature_flags_when_enabled()
    {
        var gate = new OnboardingFinaleV2Gate(new FixedFinaleFlags(true));
        Assert.True(await gate.IsEnabledAsync());
    }

    private sealed class FixedFinaleFlags(bool on) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(false, true, OnboardingFinaleV2Enabled: on));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);

        public void Invalidate()
        {
        }
    }

    [Fact]
    public void Match_bar_hidden_when_count_is_zero()
    {
        var cut = Render<OnboardingFinaleV2>(p => p
            .Add(x => x.FirstName, "Sanne")
            .Add(x => x.MatchCount, 0)
            .Add(x => x.MaxTravelMinutes, 30)
            .Add(x => x.HomeLabel, "Zaandam")
            .Add(x => x.CardsInput, DefaultCards()));

        Assert.DoesNotContain("onboarding-finale-match-bar", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Match_bar_shows_real_count_when_positive()
    {
        var cut = Render<OnboardingFinaleV2>(p => p
            .Add(x => x.FirstName, "Sanne")
            .Add(x => x.MatchCount, 7)
            .Add(x => x.MaxTravelMinutes, 30)
            .Add(x => x.HomeLabel, "Zaandam")
            .Add(x => x.CardsInput, DefaultCards()));

        Assert.Contains("ob-finale__hl-n\">7", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Insights_never_render_score_numbers()
    {
        var impression = new OnboardingImpression
        {
            Strengths =
            [
                new() { Sentence = "Je wilt dingen afmaken.", Percent = 88 }
            ],
            Riasec =
            [
                new() { Sentence = "Je past bij praktisch werk.", Percent = 72 }
            ],
            TopValue = new() { Sentence = "Je vindt eigen regie belangrijk.", Percent = 91, Label = "Autonomy" }
        };

        var cut = Render<OnboardingFinaleV2>(p => p
            .Add(x => x.FirstName, "Sanne")
            .Add(x => x.Impression, impression)
            .Add(x => x.CardsInput, DefaultCards()));

        foreach (var paragraph in cut.FindAll("[data-testid=onboarding-finale-insights] p"))
        {
            Assert.DoesNotMatch(new Regex(@"\d"), paragraph.TextContent);
        }
    }

    private static OnboardingFinaleCardsInput DefaultCards()
        => new(false, false, false, false, false, false);

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }
}
