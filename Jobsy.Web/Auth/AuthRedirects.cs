using System.Text.RegularExpressions;

namespace Jobsy.Web.Auth;

public static partial class AuthRedirects
{
    public const string CandidateHowToPath = "/candidate/hoe-werkt-lobsy";
    public const string BanenkaartPath = "/";

    /// <summary>Post-login landing for a candidate based on first-login how-to flag.</summary>
    public static string CandidatePostLoginUrl(bool showCandidateHowTo)
        => showCandidateHowTo ? CandidateHowToPath : BanenkaartPath;

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
                || !SafeLocalPathRegex().IsMatch(decoded))
            {
                return false;
            }
        }

        return !LooksLikeAbsoluteOrScheme(url);
    }

    private static bool LooksLikeAbsoluteOrScheme(string value)
        => value.Contains("://", StringComparison.Ordinal)
           || AbsoluteSchemeRegex().IsMatch(value);

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:", RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteSchemeRegex();
}
