using Jobsy.Core.Privacy;
using Jobsy.Core.Security;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Api.Security;

public static class AnalyticsConsent
{
    public static bool IsGranted(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var presented = ReadPresented(request);
        if (string.IsNullOrWhiteSpace(presented))
        {
            return false;
        }

        var services = request.HttpContext.RequestServices;
        IHostEnvironment? env = null;
        IConfiguration? config = null;
        if (services is not null)
        {
            env = services.GetService<IHostEnvironment>();
            config = services.GetService<IConfiguration>();
        }

        if ((env is null || !env.IsProduction())
            && presented.Equals(CookieConsentNames.AnalyticsValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var key = JobsyLocalSessionToken.ResolveSigningKey(
            config?["JobsyAuth:LocalSessionSigningKey"],
            config?["JobsyAuth:DevelopmentAuthSecret"]);
        return CookieConsentToken.IsValid(presented, key);
    }

    private static string? ReadPresented(HttpRequest request)
    {
        if (request.Headers.TryGetValue(CookieConsentNames.HeaderName, out var header))
        {
            var value = header.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        if (request.Cookies.TryGetValue(CookieConsentNames.CookieName, out var cookie)
            && !string.IsNullOrWhiteSpace(cookie))
        {
            return cookie.Trim();
        }

        return null;
    }
}
