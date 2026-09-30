using Jobsy.Core.Localization;

namespace Jobsy.Web.Localization;

/// <summary>Server-side culture resolution for static SSR / first paint (no JS).</summary>
public static class CultureRequest
{
    /// <summary>Priority: <c>?lang=</c> (supported) → <c>Jobsy.Culture</c> cookie → nl.</summary>
    public static string ResolveLanguage(HttpContext? http)
    {
        if (http is null)
        {
            return JobsyLanguages.Default;
        }

        if (http.Request.Query.TryGetValue("lang", out var langValues)
            && TrySupportedCode(langValues.ToString(), out var fromQuery))
        {
            return fromQuery;
        }

        if (http.Request.Cookies.TryGetValue(CultureState.CookieName, out var cookie)
            && TrySupportedCode(cookie, out var fromCookie))
        {
            return fromCookie;
        }

        return JobsyLanguages.Default;
    }

    /// <summary>
    /// True only for an explicitly supported code (does not treat unknowns as nl).
    /// <see cref="JobsyLanguages.IsSupported"/> maps unknowns to nl via Normalize — avoid it here.
    /// </summary>
    public static bool TrySupportedCode(string? code, out string normalized)
    {
        normalized = JobsyLanguages.Default;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var primary = code.Trim().Replace('_', '-').Split('-', 2)[0].ToLowerInvariant();
        if (!JobsyLanguages.All.Any(l => l.Code == primary))
        {
            return false;
        }

        normalized = primary;
        return true;
    }
}
