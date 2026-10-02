using System.Security.Claims;
using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class CandidateFitGateBunitTests : BunitContext
{
    public CandidateFitGateBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void FitPill_closed_gate_has_no_percent_digits()
    {
        var cut = Render<KbFitPill>(p => p
            .Add(x => x.Fit, new KbFitView(false, null, null)));
        Assert.Contains("Maak je paspoort af", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("%", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mapping_closed_gate_view_has_no_percent()
    {
        var item = new VacancyListItem
        {
            FitGate = CandidateFitApply.FitGateClosed,
            MatchPercent = null,
            FitPercent = null
        };
        var fit = KbFitMapping.FromVacancy(item);
        Assert.NotNull(fit);
        Assert.False(fit!.GateOpen);
        Assert.Null(fit.Percent);
    }

    [Fact]
    public void Mapping_open_gate_formats_why_and_rank_lower()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var item = new VacancyListItem
        {
            FitGate = CandidateFitApply.FitGateOpen,
            FitPercent = 82,
            MatchPercent = 82,
            FitBand = "strong",
            FitWhyKinds = ["culture", "travel"],
            RankLowerReason = "Kb.Dislike.night-shifts"
        };
        var fit = KbFitMapping.FromVacancy(item);
        Assert.NotNull(fit);
        Assert.True(fit!.GateOpen);
        Assert.Equal(82, fit.Percent);
        Assert.Equal(KbFitBand.Strong, fit.Band);

        var why = KbFitMapping.WhyLine(item, culture);
        Assert.Contains("cultuur", why, StringComparison.OrdinalIgnoreCase);

        var rank = KbFitMapping.RankLowerLabel(item, culture);
        Assert.Contains("Staat lager", rank, StringComparison.Ordinal);
        Assert.Contains("nachtdienst", rank, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_payload_has_no_RankLowerReason_field_expectation()
    {
        var src = File.ReadAllText(Path.Combine(
            Jobsy.Tests.Uat.RepoRoot.Find(),
            "Jobsy.Api/Controllers/VacanciesController.cs"));
        Assert.Contains("RankLowerReason = rankLowerReason", src, StringComparison.Ordinal);
        Assert.Contains("LoadDislikeReasonsAsync", src, StringComparison.Ordinal);
        Assert.Contains("if (_companyAuth.IsCandidate(User))", src, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
