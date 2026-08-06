using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Keyword matching for banenkaart / assistant vacancy search
/// (title, description, company name, requirements).
/// Matching is literal: every user search token must appear in the vacancy text.
/// </summary>
public static class VacancyTextSearch
{
    private static readonly string[] JobSuffixes =
    [
        "chauffeur", "medewerker", "hulp", "assistent", "operator", "picker", "plukker",
        "driver", "worker", "helper", "assistant"
    ];

    public static bool Matches(Vacancy vacancy, string? query)
    {
        // Align with banenkaart: never match on a masked end-client company name.
        var display = IntermediaryVacancyRules.ResolvePublicDisplay(
            vacancy,
            vacancy.Company,
            vacancy.IntermediaryCompany);
        return MatchesText(
            vacancy.Title,
            vacancy.Description,
            vacancy.WorkTypeLabels,
            vacancy.RequiredDrivingLicense,
            vacancy.RequiredEducation,
            query,
            companyName: display.DisplayName,
            intermediaryName: display.OfferedByLabel);
    }

    public static bool MatchesText(
        string? title,
        string? description,
        string? workTypeLabels,
        string? license,
        string? education,
        string? query,
        string? companyName = null,
        string? intermediaryName = null)
    {
        var tokens = GetRequiredTokens(query);
        if (tokens.Count == 0)
        {
            return true;
        }

        // Keep spaces so whole words like "chauffeur" match inside titles.
        var haystack = Normalize(
            $"{title} {description} {workTypeLabels} {license} {education} {companyName} {intermediaryName}");
        return tokens.All(token => TokenAppearsIn(haystack, token));
    }

    /// <summary>
    /// User-facing search tokens (AND). Compound titles also keep a root without job suffix.
    /// </summary>
    public static IReadOnlyList<string> GetRequiredTokens(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalized = Normalize(query);
        if (normalized.Length == 0)
        {
            return [];
        }

        var tokens = Tokenize(normalized)
            .Where(t => t.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return tokens.Count > 0 ? tokens : [normalized];
    }

    /// <summary>
    /// True when <paramref name="token"/> (or a useful compound root) appears in haystack.
    /// </summary>
    public static bool TokenAppearsIn(string haystack, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        if (haystack.Contains(token, StringComparison.Ordinal))
        {
            return true;
        }

        // "heftruckchauffeur" should also match titles containing "heftruck".
        foreach (var suffix in JobSuffixes)
        {
            if (token.EndsWith(suffix, StringComparison.Ordinal) && token.Length > suffix.Length + 2)
            {
                var root = token[..^suffix.Length];
                if (root.Length >= 3 && haystack.Contains(root, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '/' or '_' or '.')
            {
                if (sb.Length > 0 && sb[^1] != ' ')
                {
                    sb.Append(' ');
                }
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Extract a job keyword/phrase from a natural-language question.
    /// Example: "Ik zoek vacatures voor een chauffeur" → "chauffeur".
    /// </summary>
    public static string? ExtractSearchPhrase(string? raw, IEnumerable<string>? stopwords = null, IEnumerable<string>? dropLabels = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var stop = new HashSet<string>(stopwords ?? [], StringComparer.OrdinalIgnoreCase);
        var drop = new HashSet<string>(
            (dropLabels ?? []).Select(Normalize).Where(s => s.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        // Prefer the phrase after "voor (een)", "als", "naar".
        var patterned = Regex.Match(
            raw.Trim(),
            @"\b(?:voor(?:\s+een)?|als|naar)\s+(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (patterned.Success)
        {
            var phrase = CleanPhrase(patterned.Groups[1].Value, stop, drop);
            if (!string.IsNullOrWhiteSpace(phrase))
            {
                return phrase;
            }
        }

        // "zoek/vind/toon … chauffeur" — take the tail after search verbs / "vacature(s)".
        var searchTail = Regex.Match(
            raw.Trim(),
            @"\b(?:zoek(?:en)?|vind(?:en)?|toon(?:en)?|show|find|search)\b(?:\s+(?:een|de|het|naar|voor|alle|all))?(?:\s+vacature(?:s)?|\s+baan|\s+banen|\s+job(?:s)?)?(?:\s+(?:voor|als|naar)(?:\s+een)?)?\s+(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (searchTail.Success)
        {
            var phrase = CleanPhrase(searchTail.Groups[1].Value, stop, drop);
            if (!string.IsNullOrWhiteSpace(phrase))
            {
                return phrase;
            }
        }

        // Fallback: meaningful tokens from the whole sentence.
        return CleanPhrase(raw, stop, drop);
    }

    private static string? CleanPhrase(string raw, HashSet<string> stop, HashSet<string> drop)
    {
        var tokens = Normalize(raw)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Where(t => !stop.Contains(t))
            .Where(t => !drop.Contains(t))
            .ToList();

        if (tokens.Count == 0)
        {
            return null;
        }

        return string.Join(' ', tokens);
    }

    private static IEnumerable<string> Tokenize(string normalized) =>
        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
