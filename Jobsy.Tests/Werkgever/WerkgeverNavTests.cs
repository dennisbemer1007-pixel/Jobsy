using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverNavTests
{
    [Theory]
    [InlineData(EmployerRole.Bedrijfsmanager)]
    [InlineData(EmployerRole.Regiomanager)]
    [InlineData(EmployerRole.Vestigingsmanager)]
    public void For_returns_groups_in_ia_order(EmployerRole role)
    {
        var ctx = new WerkgeverNavContext(HasTakeovers: true, CandidateInsightsEnabled: true, HasApiOrCsvImport: true);
        var groups = WerkgeverNav.For(role, ctx);
        Assert.NotEmpty(groups);
        var keys = groups.Select(g => g.Key).ToList();
        Assert.Equal("overview", keys[0]);
        Assert.Contains("recruitment", keys);
    }

    [Fact]
    public void Resolve_exact_alias_and_prefix()
    {
        Assert.Equal("vacancies", WerkgeverNav.Resolve("/werkgever/vacatures")!.Value.Item.Key);
        Assert.Equal("vacancies", WerkgeverNav.Resolve("/employer/vacancies")!.Value.Item.Key);
        Assert.Equal("salary", WerkgeverNav.Resolve("/werkgever/organisatie/salaristabellen/abc")!.Value.Item.Key);
    }

    [Fact]
    public void Crumbs_include_group_and_item()
    {
        var crumbs = WerkgeverNav.Crumbs("/werkgever/vacatures", EmployerRole.Bedrijfsmanager);
        Assert.Contains(crumbs, c => c.LabelKey == "WgNav.Vacancies");
    }

    [Theory]
    [InlineData(EmployerRole.Bedrijfsmanager)]
    [InlineData(EmployerRole.Regiomanager)]
    [InlineData(EmployerRole.Vestigingsmanager)]
    public void MobileItems_has_five(EmployerRole role)
    {
        var items = WerkgeverNav.MobileItems(role, new WerkgeverNavContext(CandidateInsightsEnabled: true));
        Assert.Equal(5, items.Count);
        Assert.Equal("meer", items[^1].Key);
    }

    [Fact]
    public void Conditional_takeovers_hidden_when_empty()
    {
        var with = WerkgeverNav.For(EmployerRole.Bedrijfsmanager, new WerkgeverNavContext(HasTakeovers: true));
        var without = WerkgeverNav.For(EmployerRole.Bedrijfsmanager, new WerkgeverNavContext(HasTakeovers: false));
        Assert.Contains(with.SelectMany(g => g.Items), i => i.Key == "takeovers");
        Assert.DoesNotContain(without.SelectMany(g => g.Items), i => i.Key == "takeovers");
    }

    [Fact]
    public void Rm_label_overrides()
    {
        var item = WerkgeverNav.Catalog.SelectMany(g => g.Items).First(i => i.Key == "todo");
        Assert.Equal("WgNav.Signals", item.LabelKeyFor(EmployerRole.Regiomanager));
    }

    [Fact]
    public void Resolve_meldingen_is_notifications_not_dashboard()
    {
        Assert.Equal("notifications", WerkgeverNav.Resolve("/werkgever/meldingen")!.Value.Item.Key);
    }

    [Fact]
    public void Crumbs_meldingen_shows_notifications_label()
    {
        var crumbs = WerkgeverNav.Crumbs("/werkgever/meldingen", EmployerRole.Bedrijfsmanager);
        Assert.Contains(crumbs, c => c is { LabelKey: "WgNav.Notifications", IsCurrent: true });
    }

    [Fact]
    public void Dashboard_not_active_on_meldingen()
    {
        var dashboard = WerkgeverNav.Catalog.SelectMany(g => g.Items).First(i => i.Key == "dashboard");
        Assert.False(WerkgeverNav.IsNavItemActive(dashboard, "/werkgever/meldingen"));
        Assert.True(WerkgeverNav.IsNavItemActive(dashboard, "/werkgever"));
    }
}
