using System.Security.Claims;
using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class KandidaatBanenCardPartsBunitTests : BunitContext
{
    public KandidaatBanenCardPartsBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void FitPill_gate_closed_links_to_paspoort_without_percent()
    {
        var cut = Render<KbFitPill>(p => p
            .Add(x => x.Fit, new KbFitView(GateOpen: false, Percent: null, Band: null)));

        Assert.Contains("Maak je paspoort af", cut.Markup, StringComparison.Ordinal);
        Assert.Contains($"href=\"{KbRoutes.PaspoortTests}\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("kb-fit--gate", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("%", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Sterke match", cut.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(74, KbFitBand.Good, "kb-fit--good")]
    [InlineData(75, KbFitBand.Strong, "kb-fit--strong")]
    [InlineData(60, KbFitBand.Some, "kb-fit--some")]
    public void FitPill_open_shows_percent_and_band_class(int percent, KbFitBand band, string css)
    {
        var cut = Render<KbFitPill>(p => p
            .Add(x => x.Fit, new KbFitView(GateOpen: true, Percent: percent, Band: band)));

        Assert.Contains($"{percent}% past bij jou", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(css, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Maak je paspoort af", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WhyLine_empty_renders_nothing_nonempty_shows_text()
    {
        var empty = Render<KbWhyLine>(p => p.Add(x => x.Text, "  "));
        Assert.True(string.IsNullOrWhiteSpace(empty.Markup) || empty.Markup.Trim().Length == 0);

        var cut = Render<KbWhyLine>(p => p.Add(x => x.Text, "Je helpt graag mensen"));
        Assert.Contains("Je helpt graag mensen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("kb-why", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BadgeRow_overflow_shows_plus_n_with_accessible_label()
    {
        var badges = new List<KbBadgeItem>
        {
            new("fit", "fit", IsFitPill: true),
            new("a", "Badge A"),
            new("b", "Badge B"),
            new("c", "Badge C"),
        };
        var fit = new KbFitView(true, 80, KbFitBand.Strong);
        var cut = Render<KbBadgeRow>(p => p
            .Add(x => x.Badges, badges)
            .Add(x => x.MaxVisible, 2)
            .Add(x => x.Fit, fit));

        Assert.Contains("+2", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Badge B", cut.Markup, StringComparison.Ordinal); // in aria/title of +n
        Assert.Contains("Badge C", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">Badge B<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TravelTime_variants_approx_bureau_and_no_data()
    {
        var none = Render<KbTravelTime>(p => p.Add(x => x.Minutes, (int?)null));
        Assert.True(string.IsNullOrWhiteSpace(none.Markup) || none.Markup.Trim().Length == 0);

        var approx = Render<KbTravelTime>(p => p
            .Add(x => x.Minutes, 20)
            .Add(x => x.Transport, "Fiets")
            .Add(x => x.Approx, true));
        Assert.Contains("ongeveer", approx.Markup, StringComparison.Ordinal);
        Assert.Contains("20 min", approx.Markup, StringComparison.Ordinal);

        var bureau = Render<KbTravelTime>(p => p
            .Add(x => x.Minutes, 15)
            .Add(x => x.Transport, "Fiets")
            .Add(x => x.ToBureau, true)
            .Add(x => x.BureauRegion, "Wateringen"));
        Assert.Contains("kb-travel--bureau", bureau.Markup, StringComparison.Ordinal);
        Assert.Contains("Wateringen", bureau.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Card_parts_never_render_raw_enum_member_names()
    {
        var cut = Render<KbFitPill>(p => p
            .Add(x => x.Fit, new KbFitView(true, 75, KbFitBand.Strong)));
        Assert.DoesNotContain("KbFitBand", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(KbFitBand.Strong), cut.Markup, StringComparison.Ordinal);

        var travel = Render<KbTravelTime>(p => p
            .Add(x => x.Minutes, 10)
            .Add(x => x.Transport, "Fiets"));
        Assert.DoesNotContain("TransportMode", travel.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationStatus", travel.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
