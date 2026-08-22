using Jobsy.Core.Privacy;

namespace Jobsy.Api.Security;

public static class AnalyticsConsent
{
    public static bool IsGranted(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.TryGetValue(CookieConsentNames.HeaderName, out var header)
            && header.ToString().Equals(CookieConsentNames.AnalyticsValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return request.Cookies.TryGetValue(CookieConsentNames.CookieName, out var cookie)
               && cookie.Equals(CookieConsentNames.AnalyticsValue, StringComparison.OrdinalIgnoreCase);
    }
}
