using Jobsy.Core.Localization;

namespace Jobsy.Web.Localization;

/// <summary>Server-side culture resolution for static SSR / first paint (no JS).</summary>
public static class CultureRequest
{
    /// <summary>Priority: <c>?lang=</c> (enabled) → <c>Jobsy.Culture</c> cookie (enabled) → Accept-Language → nl.</summary>
    public static string ResolveLanguage(HttpContext? http)
    {
        if (http is null)
        {
            return JobsyLanguages.Default;
        }

        var accept = http.Request.Headers.AcceptLanguage.ToString();

        if (http.Request.Query.TryGetValue("lang", out var langValues)
            && TrySupportedCode(langValues.ToString(), out var fromQuery))
        {
            return fromQuery;
        }

        if (http.Request.Cookies.TryGetValue(CultureState.CookieName, out var cookie))
        {
            if (TrySupportedCode(cookie, out var fromCookie))
            {
                return fromCookie;
            }

            return JobsyLanguages.ResolveActive(cookie, accept);
        }

        return JobsyLanguages.ResolveActiveFromAcceptLanguage(accept) ?? JobsyLanguages.Default;
    }

    /// <summary>True when the request itself chose a language via <c>?lang=</c> or the culture cookie.</summary>
    public static bool HasExplicitChoice(HttpContext? http)
    {
        if (http is null)
        {
            return false;
        }

        if (http.Request.Query.TryGetValue("lang", out var langValues)
            && TrySupportedCode(langValues.ToString(), out _))
        {
            return true;
        }

        return http.Request.Cookies.TryGetValue(CultureState.CookieName, out var cookie)
               && TrySupportedCode(cookie, out _);
    }

    /// <summary>
    /// True only for an explicitly enabled code (does not treat unknowns or disabled catalog langs as supported).
    /// </summary>
    public static bool TrySupportedCode(string? code, out string normalized)
        => JobsyLanguages.TryEnabledCode(code, out normalized);
}
