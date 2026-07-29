using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

public static class HtmlSanitize
{
    private static readonly Regex TagRegex = new("<[^>]*>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex DangerousBlockRegex = new(
        @"<\s*(script|style|iframe|object|embed|form|input|button|textarea|select|svg|math)[^>]*>.*?<\s*/\s*\1\s*>|<\s*(script|style|iframe|object|embed|form|input|button|textarea|select|svg|math|link|meta)\b[^>]*/?\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TagTokenRegex = new(
        @"</?\s*([a-zA-Z0-9]+)([^>]*)>",
        RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "section", "h2", "h3", "p", "ul", "ol", "li", "strong", "b", "em", "i", "u", "a", "br"
    };

    /// <summary>Strip tags and encode — safe for preview / non-trusted HTML.</summary>
    public static string ToPlainPreview(string? html, int maxLength = 800)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var cleaned = DangerousBlockRegex.Replace(html, " ");
        cleaned = TagRegex.Replace(cleaned, " ");
        cleaned = WebUtility.HtmlDecode(cleaned);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        if (cleaned.Length > maxLength)
        {
            cleaned = cleaned[..maxLength] + "…";
        }

        return cleaned;
    }

    /// <summary>
    /// Allow a small subset of markup for admin-edited public pages (about / legal-style copy).
    /// Strips scripts, event handlers and disallowed tags/attributes.
    /// </summary>
    public static string ToSafeMarkup(string? html, int maxLength = 100_000)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var input = html.Length > maxLength ? html[..maxLength] : html;
        input = DangerousBlockRegex.Replace(input, string.Empty);
        input = Regex.Replace(input, @"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", string.Empty, RegexOptions.IgnoreCase);

        var sb = new StringBuilder(input.Length);
        var last = 0;
        foreach (Match match in TagTokenRegex.Matches(input))
        {
            sb.Append(input, last, match.Index - last);
            last = match.Index + match.Length;

            var name = match.Groups[1].Value;
            if (!AllowedTags.Contains(name))
            {
                continue;
            }

            var isClosing = match.Value.StartsWith("</", StringComparison.Ordinal);
            if (isClosing)
            {
                sb.Append("</").Append(name.ToLowerInvariant()).Append('>');
                continue;
            }

            if (name.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<br />");
                continue;
            }

            if (name.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                var href = ExtractAttribute(match.Groups[2].Value, "href");
                if (!IsSafeLinkHref(href))
                {
                    continue;
                }

                sb.Append("<a class=\"auth-link\" href=\"")
                    .Append(WebUtility.HtmlEncode(href!.Trim()))
                    .Append("\">");
                continue;
            }

            sb.Append('<').Append(name.ToLowerInvariant()).Append('>');
        }

        sb.Append(input, last, input.Length - last);
        return sb.ToString().Trim();
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

    public static bool IsSafeLinkHref(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            var address = trimmed["mailto:".Length..].Trim();
            return address.Contains('@') &&
                   !address.Contains('<') &&
                   !address.Contains('>') &&
                   !address.Contains('"');
        }

        return IsSafeHttpsUrl(trimmed);
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

    private static string? ExtractAttribute(string attributes, string name)
    {
        var match = Regex.Match(
            attributes,
            $@"\b{Regex.Escape(name)}\s*=\s*(""(?<v>[^""]*)""|'(?<v>[^']*)'|(?<v>[^\s>]+))",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["v"].Value : null;
    }
}
