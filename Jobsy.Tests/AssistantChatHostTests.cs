using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class AssistantChatHostTests
{
    [Fact]
    public void IsAvailableFor_matches_assistant_roles_and_skips_guests()
    {
        Assert.False(AssistantChatHost.IsAvailableFor(null));
        Assert.False(AssistantChatHost.IsAvailableFor(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.False(AssistantChatHost.IsAvailableFor(Principal(JobsyRoles.Ambassadeur)));

        Assert.True(AssistantChatHost.IsAvailableFor(Principal(JobsyRoles.Candidate)));
        Assert.True(AssistantChatHost.IsAvailableFor(Principal(JobsyRoles.Admin)));
        Assert.True(AssistantChatHost.IsAvailableFor(Principal(JobsyRoles.SalesManager)));
        Assert.True(AssistantChatHost.IsAvailableFor(Principal(JobsyRoles.BranchManager)));
        Assert.True(AssistantChatHost.IsAvailableFor(Principal(JobsyRoles.EnterpriseManager)));
    }

    [Fact]
    public void NotifyOpen_toggles_and_raises_changed()
    {
        var host = new AssistantChatHost();
        var changed = 0;
        host.Changed += () => changed++;

        host.NotifyOpen(true);
        Assert.True(host.IsOpen);
        Assert.Equal(1, changed);

        host.NotifyOpen(true);
        Assert.Equal(1, changed);

        host.NotifyOpen(false);
        Assert.False(host.IsOpen);
        Assert.Equal(2, changed);
    }

    private static ClaimsPrincipal Principal(string role)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(identity);
    }
}
