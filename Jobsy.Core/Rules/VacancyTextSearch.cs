using System.Text;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Keyword matching for banenkaart / assistant vacancy search (title, description, requirements).
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
        var terms = ExpandTerms(query);
        if (terms.Count == 0)
        {
            return true;
        }

        var haystack = BuildHaystack(vacancy);
        return terms.Any(term => haystack.Contains(term, StringComparison.Ordinal));
    }

    public static bool MatchesText(string? title, string? description, string? workTypeLabels, string? license, string? education, string? query)
    {
        var terms = ExpandTerms(query);
        if (terms.Count == 0)
        {
            return true;
        }

        var haystack = Normalize($"{title} {description} {workTypeLabels} {license} {education}");
        return terms.Any(term => haystack.Contains(term, StringComparison.Ordinal));
    }

    /// <summary>
    /// Expand a user query into searchable terms (tokens, roots without job suffixes).
    /// </summary>
    public static IReadOnlyList<string> ExpandTerms(string? query)
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

        var terms = new HashSet<string>(StringComparer.Ordinal) { normalized };
        foreach (var token in Tokenize(normalized))
        {
            if (token.Length >= 3)
            {
                terms.Add(token);
            }

            foreach (var suffix in JobSuffixes)
            {
                if (token.EndsWith(suffix, StringComparison.Ordinal) && token.Length > suffix.Length + 2)
                {
                    var root = token[..^suffix.Length];
                    if (root.Length >= 3)
                    {
                        terms.Add(root);
                    }
                }
            }
        }

        return terms.OrderByDescending(t => t.Length).ToList();
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

    private static string BuildHaystack(Vacancy vacancy) =>
        Normalize(
            $"{vacancy.Title} {vacancy.Description} {vacancy.WorkTypeLabels} {vacancy.RequiredDrivingLicense} {vacancy.RequiredEducation}");

    private static IEnumerable<string> Tokenize(string normalized) =>
        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
