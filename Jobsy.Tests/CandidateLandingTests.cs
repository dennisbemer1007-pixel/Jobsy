using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Web.Auth;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class CandidateLandingTests
{
    [Fact]
    public void IsPassportReady_requires_CompletedAtUtc()
    {
        Assert.False(CandidateLanding.IsPassportReady((CandidateOnboarding?)null));
        Assert.False(CandidateLanding.IsPassportReady(new CandidateOnboarding()));
        Assert.False(CandidateLanding.IsPassportReady((DateTime?)null));

        Assert.True(CandidateLanding.IsPassportReady(new CandidateOnboarding
        {
            CompletedAtUtc = DateTime.UtcNow
        }));
        Assert.True(CandidateLanding.IsPassportReady(DateTime.UtcNow));
    }

    [Theory]
    [InlineData(true, true, false, "/candidate/ontdekkingsreis")]
    [InlineData(true, true, true, "/candidate/paspoort")]
    [InlineData(false, true, false, "/candidate/ontdekkingsreis")]
    [InlineData(false, true, true, "/candidate/paspoort")]
    [InlineData(true, false, false, "/")]
    [InlineData(true, false, true, "/")]
    [InlineData(false, false, false, "/candidate/profile")]
    [InlineData(false, false, true, "/candidate/profile")]
    public void HomeFor_candidate_landing_by_flags_and_readiness(
        bool employers,
        bool passport,
        bool ready,
        string expected)
    {
        var flags = new FeatureFlagSnapshot(employers, passport);
        var user = Principal(JobsyRoles.Candidate);
        Assert.Equal(expected, FeatureRoutes.HomeFor(user, flags, passportReady: ready));
        Assert.Equal(expected, FeatureRoutes.CandidateHome(flags, ready));
    }

    [Fact]
    public void CandidatePostLoginUrl_passport_on_uses_HomeFor()
    {
        var on = new FeatureFlagSnapshot(true, true);
        Assert.Equal(
            FeatureRoutes.CandidateDiscoveryPath,
            AuthRedirects.CandidatePostLoginUrl(showCandidateHowTo: true, on));
        Assert.Equal(
            FeatureRoutes.CandidatePassportPath,
            AuthRedirects.CandidatePostLoginUrl(showCandidateHowTo: false, on));
    }

    [Fact]
    public void CandidatePostLoginUrl_passport_off_unchanged()
    {
        Assert.Equal("/candidate/start", AuthRedirects.CandidatePostLoginUrl(true));
        Assert.Equal(AuthRedirects.BanenkaartPath, AuthRedirects.CandidatePostLoginUrl(false));
    }

    [Fact]
    public void ResolveCandidateReturnUrl_keeps_vacancy_over_landing()
    {
        var on = new FeatureFlagSnapshot(true, true);
        Assert.Equal(
            "/vacancies/abc",
            AuthRedirects.ResolveCandidateReturnUrl("/vacancies/abc", showCandidateHowTo: true, on));
        Assert.Equal(
            FeatureRoutes.CandidateDiscoveryPath,
            AuthRedirects.ResolveCandidateReturnUrl("/home", showCandidateHowTo: true, on));
        Assert.Equal(
            FeatureRoutes.CandidatePassportPath,
            AuthRedirects.ResolveCandidateReturnUrl("/", showCandidateHowTo: false, on));
    }

    [Fact]
    public void IsGenericPostLoginLanding_includes_passport_and_discovery()
    {
        Assert.True(AuthRedirects.IsGenericPostLoginLanding("/candidate/paspoort"));
        Assert.True(AuthRedirects.IsGenericPostLoginLanding("/candidate/ontdekkingsreis"));
        Assert.True(AuthRedirects.IsGenericPostLoginLanding("/candidate/start"));
        Assert.False(AuthRedirects.IsGenericPostLoginLanding("/vacancies/1"));
    }

    [Fact]
    public void PassportReadyFromClaims_maps_show_candidate_how_to()
    {
        var notReady = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, JobsyRoles.Candidate), new Claim("show_candidate_how_to", "1")],
            "t"));
        var ready = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, JobsyRoles.Candidate)],
            "t"));
        Assert.False(AuthRedirects.PassportReadyFromClaims(notReady));
        Assert.True(AuthRedirects.PassportReadyFromClaims(ready));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CandidateItems_order_for_all_flag_combinations(bool employers, bool passport)
    {
        var flags = new FeatureFlagSnapshot(employers, passport);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.True(items.Count <= 5);

        if (passport && employers)
        {
            Assert.Equal(
                ["Nav.Discovery", "Nav.Passport", "Nav.CareerPath", "Nav.Banenkaart", "Nav.Applications"],
                items.Select(i => i.TitleKey).ToArray());
        }
        else if (passport)
        {
            Assert.Equal(
                ["Nav.Discovery", "Nav.Passport", "Nav.CareerPath"],
                items.Select(i => i.TitleKey).ToArray());
        }
        else if (employers)
        {
            Assert.Equal(
                ["Nav.Search", "Nav.Saved", "Nav.Applications", "Nav.CareerPath", "Nav.Profile"],
                items.Select(i => i.TitleKey).ToArray());
        }
        else
        {
            Assert.Equal(
                ["Nav.CareerPath", "Nav.Profile"],
                items.Select(i => i.TitleKey).ToArray());
        }
    }

    [Fact]
    public void Nav_Banenkaart_localized_in_all_languages()
    {
        Assert.Equal("Banenkaart", UiStrings.Get("Nav.Banenkaart", "nl"));
        Assert.Equal("Job map", UiStrings.Get("Nav.Banenkaart", "en"));
        Assert.Equal("Mapa ofert", UiStrings.Get("Nav.Banenkaart", "pl"));
        Assert.Equal("Hartă joburi", UiStrings.Get("Nav.Banenkaart", "ro"));
        Assert.Equal("خريطة الوظائف", UiStrings.Get("Nav.Banenkaart", "ar"));
        // Legacy key kept for passport-OFF / other surfaces.
        Assert.Equal("Zoeken", UiStrings.Get("Nav.Search", "nl"));
    }

    private static ClaimsPrincipal Principal(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
        claims.Add(new Claim(ClaimTypes.Name, "test"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
