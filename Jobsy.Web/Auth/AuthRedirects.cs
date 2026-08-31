using System.Text.RegularExpressions;

namespace Jobsy.Web.Auth;

public static partial class AuthRedirects
{
    public const string CandidateHowToPath = "/candidate/hoe-werkt-lobsy";
    public const string BanenkaartPath = "/";

    /// <summary>Post-login landing for a candidate based on first-login how-to flag.</summary>
    public static string CandidatePostLoginUrl(bool showCandidateHowTo)
        => showCandidateHowTo ? CandidateHowToPath : BanenkaartPath;

    /// <summary>
    /// Generic landings that may be replaced by the candidate how-to / banenkaart.
    /// Vacancy (and other explicit) returnUrls are kept.
    /// </summary>
    public static bool IsGenericPostLoginLanding(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        var path = url.Split('?', '#')[0];
        return path is "/" or "/home" or "/banen" or "/login";
    }

    /// <summary>
    /// Preserves an explicit local returnUrl (e.g. <c>/vacancies/{id}</c>).
    /// First-login how-to and the banenkaart only apply for generic landings.
    /// </summary>
    public static string ResolveCandidateReturnUrl(string returnUrl, bool showCandidateHowTo)
    {
        if (!IsGenericPostLoginLanding(returnUrl))
        {
            return returnUrl;
        }

        return CandidatePostLoginUrl(showCandidateHowTo);
    }

    [GeneratedRegex(@"^/[A-Za-z0-9\-._~!$&'()*+,;=:@%/?]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeLocalPathRegex();

    /// <summary>
    /// Maps post-login landing paths. Anonymous landing pages redirect to the authenticated home.
    /// </summary>
    public static string PostLoginUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url is "/" or "/banen")
        {
            return "/home";
        }

        return url;
    }

    /// <summary>
    /// Picks the first non-empty candidate (<c>returnUrl</c>, <c>returnTo</c>, <c>redirect</c>)
    /// and maps it through <see cref="PostLoginUrl"/> + <see cref="SafeLocalUrl"/>.
    /// </summary>
    public static string ResolveRequestedReturnUrl(params string?[] candidates)
    {
        foreach (var raw in candidates)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            return SafeLocalUrl(PostLoginUrl(raw.Trim()));
        }

        return "/home";
    }

    /// <summary>Appends a sanitized local <c>returnUrl</c> query parameter.</summary>
    public static string AppendReturnUrl(string pathAndQuery, string? returnUrl)
    {
        var safe = ResolveRequestedReturnUrl(returnUrl);
        var separator = pathAndQuery.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return pathAndQuery + separator + "returnUrl=" + Uri.EscapeDataString(safe);
    }

    /// <summary>
    /// Returns a safe same-origin relative path, or <c>/home</c> when the value is unsafe.
    /// </summary>
    public static string SafeLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "/home";
        }

        if (!IsSafeLocalPath(url))
        {
            return "/home";
        }

        return url;
    }

    private static bool IsSafeLocalPath(string url)
    {
        // Must be a single-slash relative path (not protocol-relative //...).
        if (url[0] != '/' || url.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        if (url.Contains('\\', StringComparison.Ordinal)
            || url.Contains("//", StringComparison.Ordinal))
        {
            return false;
        }

        // Reject absolute URLs / scheme tricks before decoding.
        // Note: on Linux, Uri.TryCreate("/path", Absolute) can succeed as file:// — ignore those.
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            && absolute.IsAbsoluteUri
            && !string.Equals(absolute.Scheme, "file", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!SafeLocalPathRegex().IsMatch(url))
        {
            return false;
        }

        // Reject encoded open-redirect tricks (%2f%2f, %5c, schemes, etc.).
        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(url);
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (!string.Equals(decoded, url, StringComparison.Ordinal))
        {
            if (decoded.Contains('\\', StringComparison.Ordinal)
                || decoded.Contains("//", StringComparison.Ordinal)
                || decoded.StartsWith("//", StringComparison.Ordinal)
                || decoded.Contains('\0')
                || LooksLikeAbsoluteOrScheme(decoded)
                || ContainsEmbeddedScheme(decoded)
                || !SafeLocalPathRegex().IsMatch(decoded))
            {
                return false;
            }
        }

        return !LooksLikeAbsoluteOrScheme(url) && !ContainsEmbeddedScheme(url);
    }

    /// <summary>
    /// Rejects <c>/javascript:…</c> and similar scheme tricks that are still
    /// same-origin relative paths but unsafe in HTML attributes.
    /// </summary>
    private static bool ContainsEmbeddedScheme(string value)
    {
        var trimmed = value.TrimStart('/');
        return AbsoluteSchemeRegex().IsMatch(trimmed);
    }

    private static bool LooksLikeAbsoluteOrScheme(string value)
        => value.Contains("://", StringComparison.Ordinal)
           || AbsoluteSchemeRegex().IsMatch(value);

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:", RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteSchemeRegex();
}
