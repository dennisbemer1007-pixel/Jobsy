using Jobsy.Web.Security;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Auth;

/// <summary>
/// Limits how often we re-mint the encrypted auth cookie during idle beacons.
/// </summary>
public static class AuthCookieRefreshThrottle
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(45);

    public static bool ShouldRefresh(HttpContext http)
    {
        var subject = SessionActivityCookie.ResolveSubject(http.User);
        if (string.IsNullOrEmpty(subject))
        {
            return false;
        }

        var cache = http.RequestServices.GetRequiredService<IMemoryCache>();
        var key = "jobsy.auth-cookie-refresh:" + subject;
        if (cache.TryGetValue(key, out _))
        {
            return false;
        }

        cache.Set(key, true, MinInterval);
        return true;
    }
}
