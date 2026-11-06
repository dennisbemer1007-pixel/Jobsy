using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Lightweight script/heuristic checks for localized deep-item prompts (no ML).
/// </summary>
public static class AssessmentLanguageHeuristics
{
    private static readonly Regex ArabicScript = new(@"[\u0600-\u06FF]", RegexOptions.Compiled);
    private static readonly Regex LatinLetters = new(@"[A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex PolishDiacritics = new(@"[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]", RegexOptions.Compiled);
    private static readonly Regex RomanianDiacritics = new(@"[ăâîșțĂÂÎȘȚ]", RegexOptions.Compiled);

    private static readonly string[] DutchLeadMarkers = ["ik ", "mijn ", "collega's", "collega’s", "werk ", "klus"];

    public static bool LooksDutch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lower = text.ToLowerInvariant();
        var hits = DutchLeadMarkers.Count(m => lower.Contains(m, StringComparison.Ordinal));
        return hits >= 2 || lower.StartsWith("ik ", StringComparison.Ordinal);
    }

    public static bool LooksEnglish(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (LooksDutch(text))
        {
            return false;
        }

        return LatinLetters.IsMatch(text);
    }

    public static bool LooksPolish(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (LooksDutch(text))
        {
            return false;
        }

        return PolishDiacritics.IsMatch(text) || LooksEnglish(text);
    }

    public static bool LooksRomanian(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (LooksDutch(text))
        {
            return false;
        }

        return RomanianDiacritics.IsMatch(text) || LooksEnglish(text);
    }

    public static bool LooksArabic(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var matches = ArabicScript.Matches(text).Count;
        return matches >= Math.Max(2, text.Length / 50);
    }

    public static bool MatchesLanguage(string lang, string text) => NormalizeLang(lang) switch
    {
        "en" => LooksEnglish(text),
        "pl" => LooksPolish(text),
        "ro" => LooksRomanian(text),
        "ar" => LooksArabic(text),
        _ => true
    };

    private static string NormalizeLang(string lang)
        => string.IsNullOrWhiteSpace(lang) ? "nl" : lang.Trim().ToLowerInvariant().Split('-')[0];
}
