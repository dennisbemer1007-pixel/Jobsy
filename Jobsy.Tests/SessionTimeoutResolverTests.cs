using Jobsy.Core.Options;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class SessionTimeoutResolverTests
{
    [Fact]
    public void ResolveInactivityMinutes_prefers_host_override_over_platform()
    {
        var opts = new SessionAuthOptions { InactivityTimeoutOverrideMinutes = 480 };
        Assert.Equal(480, SessionTimeoutResolver.ResolveInactivityMinutes(opts, 30));
    }

    [Fact]
    public void ResolveInactivityMinutes_uses_platform_when_no_override()
    {
        var opts = new SessionAuthOptions();
        Assert.Equal(45, SessionTimeoutResolver.ResolveInactivityMinutes(opts, 45));
    }

    [Fact]
    public void ResolveRememberDeviceLifetime_clamps_days()
    {
        var opts = new SessionAuthOptions { RememberDeviceLifetimeDays = 30 };
        Assert.Equal(TimeSpan.FromDays(30), SessionTimeoutResolver.ResolveRememberDeviceLifetime(opts));
    }
}
