using Jobsy.Web.Navigation;
using Jobsy.Web.Werkgever;

namespace Jobsy.Tests.Werkgever;

public class EmployerScopeStateTests
{
    [Fact]
    public void Defaults_per_role()
    {
        var org = new EmployerScopeOption(EmployerScopeKind.Organisation, null, "Org");
        var region = new EmployerScopeOption(EmployerScopeKind.Region, Guid.NewGuid(), "Regio");
        var vest = new EmployerScopeOption(EmployerScopeKind.Vestiging, Guid.NewGuid(), "Vestiging");
        var all = new[] { org, region, vest };

        Assert.Equal(EmployerScopeKind.Organisation, EmployerScopeState.DefaultFor(EmployerRole.Bedrijfsmanager, all)!.Kind);
        Assert.Equal(EmployerScopeKind.Region, EmployerScopeState.DefaultFor(EmployerRole.Regiomanager, all)!.Kind);
        Assert.Equal(EmployerScopeKind.Vestiging, EmployerScopeState.DefaultFor(EmployerRole.Vestigingsmanager, all)!.Kind);
    }

    [Fact]
    public void Foreign_scope_falls_back_and_sets_toast()
    {
        var state = new EmployerScopeState();
        var orgId = Guid.NewGuid();
        var org = new EmployerScopeOption(EmployerScopeKind.Organisation, null, "Org");
        var map = new Dictionary<string, IReadOnlyList<Guid>> { [org.Key] = [orgId] };
        state.Initialize(EmployerRole.Bedrijfsmanager, [org], map, "vestiging:" + Guid.NewGuid());
        Assert.True(state.ScopeDeniedToast);
        Assert.Equal(org.Key, state.Current!.Key);
    }

    [Fact]
    public void Rm_is_readonly()
    {
        var state = new EmployerScopeState();
        var region = new EmployerScopeOption(EmployerScopeKind.Region, Guid.NewGuid(), "R");
        state.Initialize(EmployerRole.Regiomanager, [region], new Dictionary<string, IReadOnlyList<Guid>>());
        Assert.True(state.IsReadOnly);
    }
}
