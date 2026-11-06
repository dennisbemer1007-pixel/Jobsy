using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Jobsy.Tests.Auth;

public sealed class AuthCookieRefreshThrottleTests
{
    [Fact]
    public void ShouldRefresh_allows_first_call_then_blocks_until_ttl()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext
        {
            RequestServices = provider,
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Email, "test@example.com"),
            ],
            authenticationType: "Cookies"))
        };

        Assert.True(AuthCookieRefreshThrottle.ShouldRefresh(http));
        Assert.False(AuthCookieRefreshThrottle.ShouldRefresh(http));
    }
}
