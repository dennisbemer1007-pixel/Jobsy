namespace Jobsy.Core.Scholen;

/// <summary>Shared je-form sentence glue for G78 story and tiles.</summary>
internal static class PupilG78JeNarrative
{
    public static string MergeTwo(string a, string b)
    {
        var first = AsSentence(a).TrimEnd('.');
        var tail = JeContinuation(b);
        return $"{first}, en {tail}.";
    }

    public static string AsSentence(string text)
        => PupilQuestionStoryParaphrase.PolishJeForm(text.Trim().TrimEnd('.') + ".");

    public static string JeContinuation(string text)
    {
        var s = text.Trim().TrimEnd('.');
        if (s.StartsWith("Je ", StringComparison.Ordinal))
        {
            return "je " + s[3..];
        }

        return s;
    }

    public static bool TooSimilar(string a, string b)
    {
        static IEnumerable<string> Tokens(string t) => t
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4);

        var overlap = Tokens(a).Intersect(Tokens(b)).ToList();
        return overlap.Count >= 2
               || (a.Contains("help", StringComparison.OrdinalIgnoreCase)
                   && b.Contains("help", StringComparison.OrdinalIgnoreCase))
               || (a.Contains("niet snapt", StringComparison.OrdinalIgnoreCase)
                   && b.Contains("niet snapt", StringComparison.OrdinalIgnoreCase));
    }
}
