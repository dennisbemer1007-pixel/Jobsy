using Bunit;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class PassportBunitTests : BunitContext
{
    public PassportBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: true, passport: true));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 1, 0, 0)]
    [InlineData(1, 1, 1, 1)]
    public void DnaRing_renders_svg_with_aria(double a, double b, double c, double d)
    {
        var cut = Render<DnaRing>(p => p
            .Add(x => x.CompetenceProgress, a)
            .Add(x => x.CareerProgress, b)
            .Add(x => x.CultureProgress, c)
            .Add(x => x.ValuesProgress, d)
            .Add(x => x.AriaLabel, "Dit ben jij"));

        Assert.Contains("passport-dna-ring", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("role=\"img\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Dit ben jij", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("mascot-128.webp", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PassportCard_empty_facts_show_dash_no_share_no_photo()
    {
        var cut = Render<PassportCard>(p => p
            .Add(x => x.DisplayName, "Samira")
            .Add(x => x.Initials, "SE")
            .Add(x => x.MemberNumber, "LB-12345")
            .Add(x => x.CompletenessPercent, 40)
            .Add(x => x.ShowOpenForWork, false)
            .Add(x => x.Keywords, Array.Empty<string>())
            .Add(x => x.LookingFor, Array.Empty<string>()));

        Assert.Contains("—", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("SE", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Deel mijn paspoort", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Beschikbaar voor werk", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PassportCard_hides_open_for_work_when_employers_off_flag_simulated()
    {
        var cut = Render<PassportCard>(p => p
            .Add(x => x.DisplayName, "Samira")
            .Add(x => x.Initials, "SE")
            .Add(x => x.MemberNumber, "LB-1")
            .Add(x => x.ShowOpenForWork, false)
            .Add(x => x.LookingFor, Array.Empty<string>()));

        Assert.DoesNotContain("Beschikbaar voor werk", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PassportOverview_not_yet_state_links_to_tests()
    {
        var cut = Render<PassportOverview>(p => p
            .Add(x => x.CompletedCount, 0));

        Assert.Contains("Nog niet ontdekt", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Doe de test", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("nog in het ei", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CandidateItems_passport_on_first_item_is_discovery()
    {
        var flags = new FeatureFlagSnapshot(true, true);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal("Nav.Discovery", items[0].TitleKey);
        Assert.Equal("/candidate/ontdekkingsreis", items[0].Href);
        Assert.Equal("Nav.Passport", items[1].TitleKey);
        Assert.Equal("Nav.CareerPath", items[2].TitleKey);
        Assert.Equal("Nav.Banenkaart", items[3].TitleKey);
        Assert.Equal("Nav.Applications", items[4].TitleKey);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "t"), new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }

    private sealed class FixedFlags(bool employers, bool passport) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, passport));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature switch
            {
                PlatformFeature.Employers => employers,
                PlatformFeature.CandidatePassport => passport,
                _ => false
            });

        public void Invalidate()
        {
        }
    }
}
