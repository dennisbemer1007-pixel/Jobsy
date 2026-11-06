using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Normalization and near-duplicate detection for assessment item stems (Quick-Scan + deep banks).
/// </summary>
public static class AssessmentStemQuality
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ik", "je", "jij", "u", "we", "wij", "ze", "zij", "het", "de", "een", "en", "of", "als", "dat", "die",
        "dit", "er", "om", "op", "in", "van", "voor", "met", "naar", "te", "is", "zijn", "was", "wordt", "niet",
        "meer", "ook", "al", "nog", "maar", "dan", "bij", "uit", "aan", "over", "door", "tot", "zo", "wel",
        "the", "a", "an", "and", "or", "to", "of", "in", "on", "for", "with", "at", "by", "from", "is", "are",
        "i", "you", "we", "they", "it", "my", "your", "our"
    };

    /// <summary>Lowercase, strip diacritics/punctuation, collapse whitespace.</summary>
    public static string Normalize(string stem)
    {
        if (string.IsNullOrWhiteSpace(stem))
        {
            return "";
        }

        var formD = stem.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
            else if (char.IsWhiteSpace(ch))
            {
                sb.Append(' ');
            }
        }

        return Regex.Replace(sb.ToString().Trim(), @"\s+", " ", RegexOptions.CultureInvariant);
    }

    public static IReadOnlySet<string> TokenSet(string stem)
    {
        var normalized = Normalize(stem);
        if (normalized.Length == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tokens;
    }

    /// <summary>Jaccard similarity on content tokens (0–1).</summary>
    public static double JaccardSimilarity(string a, string b)
    {
        var setA = TokenSet(a);
        var setB = TokenSet(b);
        if (setA.Count == 0 && setB.Count == 0)
        {
            return 1;
        }

        if (setA.Count == 0 || setB.Count == 0)
        {
            return 0;
        }

        var intersection = setA.Intersect(setB, StringComparer.OrdinalIgnoreCase).Count();
        var union = setA.Union(setB, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    public static bool IsNearDuplicate(string a, string b, double threshold = 0.82)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return false;
        }

        if (string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal))
        {
            return true;
        }

        return JaccardSimilarity(a, b) >= threshold;
    }

    public static IReadOnlyList<(string A, string B, double Score)> FindNearDuplicatePairs(
        IReadOnlyList<string> stems,
        double threshold = 0.82)
    {
        var pairs = new List<(string, string, double)>();
        for (var i = 0; i < stems.Count; i++)
        {
            for (var j = i + 1; j < stems.Count; j++)
            {
                var score = JaccardSimilarity(stems[i], stems[j]);
                if (score >= threshold)
                {
                    pairs.Add((stems[i], stems[j], score));
                }
            }
        }

        return pairs;
    }
}
