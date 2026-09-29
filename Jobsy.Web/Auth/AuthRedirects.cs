using System.Text.RegularExpressions;
using Jobsy.Core.Features;

namespace Jobsy.Web.Auth;

public static partial class AuthRedirects
{
    /// <summary>First-login onboarding wizard (replaces hoe-werkt-lobsy redirect for new candidates).</summary>
    public const string CandidateHowToPath = "/candidate/start";

    /// <summary>Guide remains reachable from the (i) menu.</summary>
    public const string CandidateHowToGuidePath = "/candidate/hoe-werkt-lobsy";
    public const string BanenkaartPath = "/";

    /// <summary>Post-login landing for a candidate based on first-login how-to flag.</summary>
    public static string CandidatePostLoginUrl(bool showCandidateHowTo)
        => CandidatePostLoginUrl(showCandidateHowTo, FeatureFlagSnapshot.Defaults);

    public static string CandidatePostLoginUrl(bool showCandidateHowTo, FeatureFlagSnapshot flags)
    {
        if (showCandidateHowTo)
        {
            return CandidateHowToPath;
        }

        return flags.EmployersEnabled ? BanenkaartPath : FeatureRoutes.CandidateProfilePath;
    }

    /// <summary>
    /// Generic landings that may be replaced by the candidate how-to / banenkaart.
    /// Vacancy (and other explicit) returnUrls are kept when employers are ON.
    /// </summary>
    public static bool IsGenericPostLoginLanding(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        var path = url.Split('?', '#')[0];
        return path is "/" or "/home" or "/banen" or "/login" or "/ontdek" or "/candidate/profile";
    }

    /// <summary>
    /// True when the return URL points at an employer/vacancy surface that should fall back
    /// when employers are OFF.
    /// </summary>
    public static bool IsEmployerDependentReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var path = url.Split('?', '#')[0].ToLowerInvariant();
        return path is "/" or "/banen"
               || path.StartsWith("/vacancies/", StringComparison.Ordinal)
               || path.StartsWith("/candidate/match", StringComparison.Ordinal)
               || path.StartsWith("/candidate/liked", StringComparison.Ordinal)
               || path.StartsWith("/candidate/shared", StringComparison.Ordinal)
               || path.StartsWith("/candidate/applications", StringComparison.Ordinal)
               || path.StartsWith("/candidate/vacancies", StringComparison.Ordinal)
               || path.StartsWith("/employer/", StringComparison.Ordinal)
               || path.StartsWith("/branch/", StringComparison.Ordinal)
               || path.StartsWith("/register", StringComparison.Ordinal)
               || path.StartsWith("/vestiging/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Preserves an explicit local returnUrl (e.g. <c>/vacancies/{id}</c>).
    /// First-login how-to and the banenkaart only apply for generic landings.
    /// When employers are OFF, vacancy returnUrls fall back to the home path.
    /// </summary>
    public static string ResolveCandidateReturnUrl(string returnUrl, bool showCandidateHowTo)
        => ResolveCandidateReturnUrl(returnUrl, showCandidateHowTo, FeatureFlagSnapshot.Defaults);

    public static string ResolveCandidateReturnUrl(
        string returnUrl,
        bool showCandidateHowTo,
        FeatureFlagSnapshot flags)
    {
        if (!flags.EmployersEnabled && IsEmployerDependentReturnUrl(returnUrl))
        {
            return CandidatePostLoginUrl(showCandidateHowTo, flags);
        }

        if (!IsGenericPostLoginLanding(returnUrl))
        {
            return returnUrl;
        }

        return CandidatePostLoginUrl(showCandidateHowTo, flags);
    }

    [GeneratedRegex(@"^/[A-Za-z0-9\-._~!$&'()*+,;=:@%/?]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeLocalPathRegex();

    /// <summary>
    /// Maps post-login landing paths. Anonymous landing pages redirect to the authenticated home.
    /// </summary>
    public static string PostLoginUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "/home";
        }

        var path = url.Split('?', '#')[0];
        if (path is "/" or "/banen" or "/login" or "/ontdek"
            || path.StartsWith("/account/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account/logout", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account/demo-login", StringComparison.OrdinalIgnoreCase))
        {
            return "/home";
        }

        return url;
    }

    /// <summary>
    /// Picks the first <em>safe</em> non-empty candidate (<c>returnUrl</c>, <c>returnTo</c>, <c>redirect</c>).
    /// Unsafe values are skipped so a later local path still wins.
    /// </summary>
    public static string ResolveRequestedReturnUrl(params string?[] candidates)
    {
        foreach (var raw in candidates)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var mapped = PostLoginUrl(raw.Trim());
            if (!IsSafeLocalPath(mapped))
            {
                continue;
            }

            return mapped;
        }

        return "/home";
    }

    /// <summary>Path only — no query/fragment — so idle re-auth does not echo PII in the login URL.</summary>
    public static string PathOnly(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "";
        }

        var trimmed = url.Trim();
        var cut = trimmed.IndexOfAny(['?', '#']);
        return cut >= 0 ? trimmed[..cut] : trimmed;
    }

    /// <summary>Session-expiry destination: local path without query string.</summary>
    public static string ResolveSessionReturnUrl(params string?[] candidates)
    {
        var paths = new string?[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            paths[i] = PathOnly(candidates[i]);
        }

        return ResolveRequestedReturnUrl(paths);
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
        if (url[0] != '/' || url.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        if (url.Contains('\\', StringComparison.Ordinal)
            || url.Contains("//", StringComparison.Ordinal))
        {
            return false;
        }

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
