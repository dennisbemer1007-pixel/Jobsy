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

        if (http.Request.Cookies.TryGetValue(CultureState.CookieName, out var cookie)
            && CultureRequest.TrySupportedCode(cookie, out var fromCookie))
        {
            return fromCookie;
        }

        if (FromAcceptLanguage(http.Request.Headers.AcceptLanguage.ToString()) is { } fromHeader)
        {
            return fromHeader;
        }

        return JobsyLanguages.Default;
    }

    public static bool IsRightToLeft(string language) => JobsyLanguages.Get(language).IsRightToLeft;

    /// <summary>Highest-quality supported language in an <c>Accept-Language</c> header, else null.</summary>
    public static string? FromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var best = (Code: (string?)null, Quality: -1d);
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segments = part.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0 || !CultureRequest.TrySupportedCode(segments[0], out var code))
            {
                continue;
            }

            var quality = 1d;
            foreach (var segment in segments.Skip(1))
            {
                if (segment.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(
                        segment[2..],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var parsed))
                {
                    quality = parsed;
                }
            }

            if (quality > best.Quality)
            {
                best = (code, quality);
            }
        }

        return best.Quality > 0 ? best.Code : null;
    }
}
