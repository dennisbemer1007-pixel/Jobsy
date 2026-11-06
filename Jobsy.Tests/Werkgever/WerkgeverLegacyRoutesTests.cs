using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverLegacyRoutesTests
{
    [Theory]
    [InlineData("/employer/vacancies", "/werkgever/vacatures", null)]
    [InlineData("/branch/vacancies", "/werkgever/vacatures", null)]
    [InlineData("/employer/talent-contacts", "/werkgever/talentpool", "contact")]
    [InlineData("/employer/culture", "/werkgever/organisatie/profiel", "cultuur")]
    [InlineData("/regional/tokens", "/werkgever/tokens", null)]
    [InlineData("/branch", "/werkgever", null)]
    public void TryMap_known_routes(string old, string neu, string? tab)
    {
        Assert.True(WerkgeverLegacyRoutes.TryMap(old, out var mapped, out var t));
        Assert.Equal(neu, mapped);
        Assert.Equal(tab, t);
    }

    [Fact]
    public void BuildTarget_keeps_query_and_adds_tab()
    {
        var target = WerkgeverLegacyRoutes.BuildTarget("/werkgever/vacatures", "?status=active", null);
        Assert.Equal("/werkgever/vacatures?status=active", target);
        var withTab = WerkgeverLegacyRoutes.BuildTarget("/werkgever/talentpool", null, "contact");
        Assert.Equal("/werkgever/talentpool?tab=contact", withTab);
    }

    [Fact]
    public void Salary_table_id_maps()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.True(WerkgeverLegacyRoutes.TryMap($"/employer/salary-tables/{id}", out var neu, out _));
        Assert.Equal($"/werkgever/organisatie/salaristabellen/{id}", neu);
    }

    [Fact]
    public void Payment_paths_untouched()
    {
        Assert.True(WerkgeverLegacyRoutes.IsUntouchedPaymentPath("/employer/onboarding-checkout"));
        Assert.True(WerkgeverLegacyRoutes.IsUntouchedPaymentPath("/tokens/checkout-return"));
        Assert.False(WerkgeverLegacyRoutes.TryMap("/employer/onboarding-checkout", out _, out _));
    }
}
