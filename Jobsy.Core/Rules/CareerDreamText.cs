using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Sanitize free-text dream job titles before AI generation (D1).</summary>
public static partial class CareerDreamText
{
    public const int MaxLength = 60;
    public const int MaxWords = 6;

    public static string? Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        text = ControlChars().Replace(text, " ");
        text = Whitespace().Replace(text, " ").Trim();
        text = ForbiddenChars().Replace(text, "");
        text = Urls().Replace(text, "");
        text = Whitespace().Replace(text, " ").Trim();

        if (text.Length == 0 || text.Length > MaxLength)
        {
            return null;
        }

        if (!HasLetter().IsMatch(text))
        {
            return null;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || words.Length > MaxWords)
        {
            return null;
        }

        return text;
    }

    [GeneratedRegex(@"\p{C}+")]
    private static partial Regex ControlChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[<>{}\[\]]")]
    private static partial Regex ForbiddenChars();

    [GeneratedRegex(@"https?://\S+|www\.\S+", RegexOptions.IgnoreCase)]
    private static partial Regex Urls();

    [GeneratedRegex(@"\p{L}")]
    private static partial Regex HasLetter();
}
