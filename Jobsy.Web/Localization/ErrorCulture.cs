using Jobsy.Core.Localization;

namespace Jobsy.Web.Localization;

/// <summary>
/// Culture for error/status pages (E5). Cookie <c>Jobsy.Culture</c> → <c>Accept-Language</c> → nl.
/// Never touches the API or the database: the thing that broke may be exactly that.
/// </summary>
public static class ErrorCulture
{
    public static string Resolve(HttpContext? http)
    {
        if (http is null)
        {
            return JobsyLanguages.Default;
        }

        var accept = http.Request.Headers.AcceptLanguage.ToString();

        if (http.Request.Cookies.TryGetValue(CultureState.CookieName, out var cookie))
        {
            if (CultureRequest.TrySupportedCode(cookie, out var fromCookie))
            {
                return fromCookie;
            }

            return JobsyLanguages.ResolveActive(cookie, accept);
        }

        return JobsyLanguages.ResolveActiveFromAcceptLanguage(accept) ?? JobsyLanguages.Default;
    }

    public static bool IsRightToLeft(string language) => JobsyLanguages.Get(language).IsRightToLeft;

    /// <summary>Highest-quality enabled language in an <c>Accept-Language</c> header, else null.</summary>
    public static string? FromAcceptLanguage(string? header)
        => JobsyLanguages.ResolveActiveFromAcceptLanguage(header);
}
