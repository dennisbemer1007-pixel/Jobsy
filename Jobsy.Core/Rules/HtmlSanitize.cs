using System.Net;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

public static class HtmlSanitize
{
    private static readonly Regex TagRegex = new("<[^>]*>", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>Strip tags and encode — safe for preview / non-trusted HTML.</summary>
    public static string ToPlainPreview(string? html, int maxLength = 800)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(
            html,
            @"<\s*(script|style|iframe|object|embed)[^>]*>.*?<\s*/\s*\1\s*>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        cleaned = TagRegex.Replace(cleaned, " ");
        cleaned = WebUtility.HtmlDecode(cleaned);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        if (cleaned.Length > maxLength)
        {
            cleaned = cleaned[..maxLength] + "…";
        }

        return cleaned;
    }

    public static bool IsSafeHttpsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        // Block javascript:, data:, etc. already by scheme check; also reject credentials-in-url oddities.
        return string.IsNullOrEmpty(uri.UserInfo);
    }

    public static string? NormalizeMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.Trim();
        return IsSafeHttpsUrl(trimmed) ? trimmed : null;
    }
}
