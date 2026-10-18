using System.Text.RegularExpressions;
using Bunit;
using System.Security.Claims;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Components.Match;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// Blocks match/score percentages in rendered candidate and employer UI (words only).
/// </summary>
public sealed class MatchScoreNoPercentGuardTests : BunitContext
{
    private static readonly Regex MatchScorePercent = new(
        @"(\d{1,3}(?:[.,]\d+)?)\s*%\s*(match|past|fit|score|dopasow|potrivir)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public MatchScoreNoPercentGuardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth("Candidate"));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void SwipeCard_with_match_shows_band_words_not_percent()
    {
        var model = new SwipeViewModel
        {
            VacancyId = Guid.NewGuid(),
            JobTitle = "Magazijnmedewerker",
            CompanyName = "Demo BV",
            MatchPercentage = 83,
            ShowMatchPercentage = true,
            FitBand = nameof(Jobsy.Core.Rules.KbFitBand.Strong),
            FitDimensions = new CandidateFitDimensionsModel { Culture = 78 },
            FitWhyKinds = ["culture"]
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<SwipeCard>(0);
            builder.AddAttribute(1, "Model", model);
            builder.CloseComponent();
        });

        AssertNoMatchScorePercent(cut.Markup);
        Assert.Contains("kb-fit", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TopMatchTile_uses_band_label_not_percent()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TopMatchTile>(0);
            builder.AddAttribute(1, "Model", new SwipeViewModel
            {
                VacancyId = Guid.NewGuid(),
                JobTitle = "Kok",
                CompanyName = "Horeca",
                MatchPercentage = 92,
                ShowMatchPercentage = true
            });
            builder.CloseComponent();
        });

        AssertNoMatchScorePercent(cut.Markup);
        Assert.DoesNotContain("92%", cut.Markup, StringComparison.Ordinal);
    }

    private static void AssertNoMatchScorePercent(string markup)
    {
        var text = System.Net.WebUtility.HtmlDecode(Regex.Replace(markup, "<[^>]+>", " "));
        Assert.False(MatchScorePercent.IsMatch(text), $"Match/score percent leaked: {MatchScorePercent.Match(text).Value}");
        Assert.DoesNotContain("% match", text, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAuth(string role) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, role)],
                authenticationType: "test"))));
    }
}
