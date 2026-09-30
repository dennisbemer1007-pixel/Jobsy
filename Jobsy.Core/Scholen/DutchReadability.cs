using System.Globalization;
using System.Text;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Lightweight Dutch readability check for the pupil question bank:
/// average sentence length (words) and share of words with more than 3 syllables.
/// </summary>
public static class DutchReadability
{
    public sealed record Report(double AverageWordsPerSentence, double LongWordShare, int WordCount, int SentenceCount);

    public static Report Analyze(IEnumerable<string> texts)
    {
        var words = 0;
        var longWords = 0;
        var sentences = 0;
        foreach (var text in texts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var parts = SplitSentences(text);
            sentences += Math.Max(1, parts.Count);
            foreach (var sentence in parts)
            {
                foreach (var word in TokenizeWords(sentence))
                {
                    words++;
                    if (CountSyllables(word) > 3)
                    {
                        longWords++;
                    }
                }
            }
        }

        if (words == 0)
        {
            return new Report(0, 0, 0, 0);
        }

        var avg = (double)words / Math.Max(1, sentences);
        var share = (double)longWords / words;
        return new Report(avg, share, words, sentences);
    }

    public static int CountWords(string text) => TokenizeWords(text).Count();

    public static IEnumerable<string> TokenizeWords(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text.Normalize(NormalizationForm.FormC))
        {
            if (char.IsLetter(ch) || ch is '\'' or '’' or '-')
            {
                sb.Append(ch);
            }
            else if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            yield return sb.ToString();
        }
    }

    public static int CountSyllables(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return 0;
        }

        var w = word.ToLowerInvariant().Replace("ij", "i", StringComparison.Ordinal);
        var count = 0;
        var prevVowel = false;
        foreach (var ch in w)
        {
            var vowel = IsVowel(ch);
            if (vowel && !prevVowel)
            {
                count++;
            }

            prevVowel = vowel;
        }

        return Math.Max(1, count);
    }

    private static bool IsVowel(char ch) => ch is 'a' or 'e' or 'i' or 'o' or 'u' or 'y' or 'á' or 'é' or 'í' or 'ó' or 'ú' or 'ä' or 'ë' or 'ï' or 'ö' or 'ü';

    private static List<string> SplitSentences(string text)
    {
        var parts = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? [text] : parts.ToList();
    }

    public static string FormatPercent(double share)
        => (share * 100).ToString("0.0", CultureInfo.InvariantCulture);
}
