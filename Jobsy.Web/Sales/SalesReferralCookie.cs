using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Sales;

/// <summary>
/// First-click sales/partner referral cookie (<c>lobsy_sales_ref</c>).
/// Value is the normalized code only; a companion day cookie supports same-day click dedupe.
/// </summary>
public static class SalesReferralCookie
{
    public const string CookieName = "lobsy_sales_ref";
    public const string DayCookieName = "lobsy_sales_ref_day";
    public const string AmbassadeurCookieName = "lobsy_ambassadeur_ref";
    public const string AuthPropertiesKey = "sales_ref";

    public static string? TryRead(HttpContext http, bool ambassadorsEnabled)
    {
        ArgumentNullException.ThrowIfNull(http);
        if (http.Request.Cookies.TryGetValue(CookieName, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            var code = value.Trim().ToUpperInvariant();
            if (IsAmbassadeurCode(code) && !ambassadorsEnabled)
            {
                return null;
            }

            return code;
        }

        // Parked: ignore legacy ambassadeur cookie for sales attribution while the role is off.
        if (!ambassadorsEnabled
            && http.Request.Cookies.TryGetValue(AmbassadeurCookieName, out var am)
            && IsAmbassadeurCode(am))
        {
            return null;
        }

        return null;
    }

    public static bool AlreadyHoldsCodeToday(HttpContext http, string code)
    {
        ArgumentNullException.ThrowIfNull(http);
        if (!http.Request.Cookies.TryGetValue(CookieName, out var existing)
            || !string.Equals(existing?.Trim(), code, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!http.Request.Cookies.TryGetValue(DayCookieName, out var day)
            || !DateOnly.TryParse(day, out var setDay))
        {
            return false;
        }

        return setDay == Jobsy.Core.Sales.SalesClock.Today();
    }

    public static void TrySetFirstClick(
        HttpContext http,
        string code,
        int maxAgeDays,
        DateOnly? today = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        var normalized = code.Trim().ToUpperInvariant();
        if (http.Request.Cookies.TryGetValue(CookieName, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            // First click wins — do not overwrite a still-present code.
            return;
        }

        var days = Math.Clamp(maxAgeDays, 1, 90);
        var localToday = today ?? Jobsy.Core.Sales.SalesClock.Today();
        var opts = BuildOptions(http, TimeSpan.FromDays(days));
        http.Response.Cookies.Append(CookieName, normalized, opts);
        http.Response.Cookies.Append(DayCookieName, localToday.ToString("yyyy-MM-dd"), opts);
    }

    public static void Delete(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        var opts = BuildOptions(http, TimeSpan.FromDays(0));
        http.Response.Cookies.Delete(CookieName, opts);
        http.Response.Cookies.Delete(DayCookieName, opts);
    }

    private static CookieOptions BuildOptions(HttpContext http, TimeSpan maxAge) => new()
    {
        HttpOnly = true,
        Secure = JobsyCookie.ShouldMarkSecure(http),
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
        MaxAge = maxAge
    };

    private static bool IsAmbassadeurCode(string? code)
        => !string.IsNullOrWhiteSpace(code)
           && code.Trim().StartsWith("AM-", StringComparison.OrdinalIgnoreCase);
}
