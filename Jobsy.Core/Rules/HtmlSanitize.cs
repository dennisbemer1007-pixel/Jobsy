using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Media;

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

    /// <summary>Max decoded bytes for vacancy images supplied as Base64 / data URIs.</summary>
    public const int MaxImageBytes = 400_000;

    /// <summary>Max stored length for ImageUrl including data:image base64 payloads.</summary>
    public const int MaxImageUrlLength = 600_000;

    private static readonly Regex DataImageRegex = new(
        @"^data:image/(png|jpe?g|gif|webp);base64,([A-Za-z0-9+/=\s]+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string? NormalizeMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.Trim();
        // Media URLs (video/image links) must be http(s) — Base64 is only for NormalizeImageInput.
        if (!IsSafeHttpsUrl(trimmed) || trimmed.Length > 1024)
        {
            return null;
        }

        return trimmed;
    }

    /// <summary>
    /// Accepts http(s) URLs, data:image Base64 URIs, or raw Base64 image payloads.
    /// Returns a normalized URL / data URI, or null when invalid.
    /// </summary>
    public static string? NormalizeImageInput(string? input, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim().Trim('"');
        if (IsSafeHttpsUrl(trimmed) && Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            if (trimmed.Length > 1024)
            {
                error = "Afbeelding-URL is te lang (max 1024 tekens).";
                return null;
            }

            var rewritten = VacancyImageUrls.Normalize(trimmed);
            if (rewritten is not null && VacancyImageUrls.IsLocalImagePath(rewritten))
            {
                return rewritten;
            }

            return rewritten ?? trimmed;
        }

        var local = VacancyImageUrls.Normalize(trimmed);
        if (local is not null && VacancyImageUrls.IsLocalImagePath(local) && local.Length <= 1024)
        {
            return local;
        }

        var dataUri = NormalizeImageData(trimmed);
        if (dataUri is null)
        {
            error = "Ongeldige afbeelding (alleen http/https of Base64/data:image).";
            return null;
        }

        if (dataUri.Length > MaxImageUrlLength)
        {
            error = "Afbeelding is te groot na Base64-encoding.";
            return null;
        }

        return dataUri;
    }

    private static string? NormalizeImageData(string trimmed)
    {
        var match = DataImageRegex.Match(trimmed);
        if (match.Success)
        {
            var payload = Regex.Replace(match.Groups[2].Value, @"\s+", "");
            if (!TryDecodeBase64Image(payload, out var bytes))
            {
                return null;
            }

            var sniffed = DetectImageMime(bytes);
            return sniffed is null ? null : $"data:{sniffed};base64,{payload}";
        }

        // Raw Base64 without data: prefix — sniff magic bytes after decode.
        var compact = Regex.Replace(trimmed, @"\s+", "");
        if (compact.Length < 32 || compact.Contains(':', StringComparison.Ordinal))
        {
            return null;
        }

        if (!TryDecodeBase64Image(compact, out var rawBytes))
        {
            return null;
        }

        var mimeType = DetectImageMime(rawBytes);
        return mimeType is null ? null : $"data:{mimeType};base64,{compact}";
    }

    private static bool TryDecodeBase64Image(string payload, out byte[] bytes)
    {
        bytes = [];
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            return false;
        }

        if (bytes.Length is 0 or > MaxImageBytes)
        {
            return false;
        }

        return DetectImageMime(bytes) is not null;
    }

    private static string? DetectImageMime(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 8
            && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
            && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 6
            && bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F'
            && bytes[3] == (byte)'8' && (bytes[4] == (byte)'7' || bytes[4] == (byte)'9') && bytes[5] == (byte)'a')
        {
            return "image/gif";
        }

        if (bytes.Length >= 12
            && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
        {
            return "image/webp";
        }

        return null;
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
