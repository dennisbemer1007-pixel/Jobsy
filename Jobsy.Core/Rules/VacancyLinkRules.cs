using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Vacancy title/description must not contain outbound links (URLs or anchor tags).
/// Candidates apply via Lobsy — external links in copy are not allowed.
/// </summary>
public static class VacancyLinkRules
{
    private static readonly Regex UrlLike = new(
        @"((https?://|www\.)[^\s<>""']+)|(\b[a-z0-9][-a-z0-9]*\.(nl|com|net|org|eu|io|app|dev|be|de|uk|co)\b[^\s<>""']*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AnchorTag = new(
        @"<\s*a\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public const string ErrorMessage =
        "Links in de vacaturetitel of -tekst zijn niet toegestaan. Verwijder URL's en hyperlinks.";

    public static bool ContainsForbiddenLink(string? title, string? description)
        => ContainsForbiddenLink(title) || ContainsForbiddenLink(description);

    public static bool ContainsForbiddenLink(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (AnchorTag.IsMatch(text))
        {
            return true;
        }

        // Strip tags so plain-text URLs inside markup still match.
        var plain = HtmlSanitize.ToPlainPreview(text, maxLength: 50_000);
        return UrlLike.IsMatch(plain);
    }

    public static string? ValidateNoLinks(string? title, string? description)
        => ContainsForbiddenLink(title, description) ? ErrorMessage : null;
}
