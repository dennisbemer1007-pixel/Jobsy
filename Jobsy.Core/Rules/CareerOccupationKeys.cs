using System.Globalization;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>
/// Search keys that map a general occupation onto live vacancy titles (e.g. Den Haag / Westland).
/// Matching is word-based so "medewerker" does not light up every *medewerker* ad.
/// </summary>
public static class CareerOccupationKeys
{
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "voor", "met", "naar", "plus", "een", "het", "van", "bij", "als", "and", "the",
        "medewerker", "werknemer", "collega", "functie", "algemeen", "werk", "baan",
        "of", "en", "de", "een"
    };

    public static IReadOnlyList<string> FromTitle(string? title)
        => Merge(title, extra: null);

    public static IReadOnlyList<string> Merge(string? title, IEnumerable<string>? extra)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddTokens(set, title);
        if (extra is not null)
        {
            foreach (var key in extra)
            {
                AddTokens(set, key);
            }
        }

        return set
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    public static string Fold(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                buffer.Append(ch);
            }
            else if (buffer.Length > 0 && buffer[^1] != ' ')
            {
                buffer.Append(' ');
            }
        }

        return buffer.ToString().Trim();
    }

    /// <summary>True when <paramref name="key"/> appears as a whole word/phrase in a folded blob.</summary>
    public static bool Hits(string foldedBlob, string key)
    {
        if (string.IsNullOrWhiteSpace(foldedBlob) || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return $" {foldedBlob} ".Contains($" {key.Trim()} ", StringComparison.Ordinal);
    }

    private static void AddTokens(HashSet<string> set, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var folded = Fold(text);
        if (folded.Length >= 3 && !Stop.Contains(folded) && !folded.Contains(' '))
        {
            set.Add(folded);
        }

        foreach (var token in folded.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Length < 3 || Stop.Contains(token))
            {
                continue;
            }

            set.Add(token);
        }
    }
}
