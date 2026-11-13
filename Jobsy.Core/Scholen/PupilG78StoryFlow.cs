namespace Jobsy.Core.Scholen;

/// <summary>Connects canonical je-clauses into flowing B1 paragraphs.</summary>
internal static class PupilG78StoryFlow
{
    public static string MainClause(G78AnswerClause clause, bool contrastV2 = false)
    {
        if (clause.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
            || clause.Text.StartsWith("Juist hier zie je het anders", StringComparison.Ordinal))
        {
            return "";
        }

        if (contrastV2)
        {
            if (ContrastLooksNegative(clause.Text))
            {
                return "";
            }

            return PupilG78JeNarrative.AsSentence(clause.Text);
        }

        return PupilG78QuestionJeForms.ForItem(clause.ItemId)
               ?? PupilG78JeNarrative.AsSentence(clause.Text);
    }

    internal static bool ContrastLooksNegative(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return text.Contains("vindt het lastig", StringComparison.OrdinalIgnoreCase)
               || text.Contains("vind je het lastig", StringComparison.OrdinalIgnoreCase)
               || text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
               || text.StartsWith("Juist hier zie je het anders", StringComparison.Ordinal);
    }

  /// <summary>At most one &quot;, en &quot; join; further sentences start new with &quot;. &quot;.</summary>
    public static string FlowJoin(IReadOnlyList<string> sentences)
    {
        var parts = sentences
            .Select(s => s.Trim().TrimEnd('.'))
            .Where(s => s.Length > 0)
            .ToList();
        if (parts.Count == 0)
        {
            return "";
        }

        if (parts.Count == 1)
        {
            return parts[0] + ".";
        }

        if (parts.Count == 2)
        {
            return parts[0] + ". " + LowerFirstIfNeeded(parts[1]) + ".";
        }

        var merged = new List<string> { parts[0] };
        merged.AddRange(parts.Skip(1).Select(LowerFirstIfNeeded));
        return string.Join(". ", merged) + ".";
    }

    private static string LowerFirstIfNeeded(string sentence)
    {
        if (sentence.StartsWith("Je ", StringComparison.Ordinal)
            || sentence.StartsWith("In je ", StringComparison.Ordinal)
            || sentence.StartsWith("Op school", StringComparison.OrdinalIgnoreCase)
            || sentence.StartsWith("Zo ", StringComparison.Ordinal)
            || sentence.StartsWith("Ook ", StringComparison.Ordinal))
        {
            return sentence;
        }

        return LowerFirst(sentence);
    }

    public static string LowerFirst(string sentence)
    {
        if (string.IsNullOrEmpty(sentence))
        {
            return sentence;
        }

        if (sentence.StartsWith("Je ", StringComparison.Ordinal))
        {
            return "je " + sentence[3..];
        }

        if (sentence.Length >= 2 && char.IsUpper(sentence[0]))
        {
            return char.ToLowerInvariant(sentence[0]) + sentence[1..];
        }

        return sentence;
    }

    public static string EnsureSentenceStartsCapitalized(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var parts = text.Split(". ", StringSplitOptions.None);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = CapitalizeSentenceStart(parts[i]);
        }

        return string.Join(". ", parts);
    }

    private static string CapitalizeSentenceStart(string sentence)
    {
        var s = sentence.Trim();
        if (s.Length == 0)
        {
            return s;
        }

        if (char.IsUpper(s[0]))
        {
            return s;
        }

        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    public static string PolishConnectors(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return text
            .Replace(", en Je ", ", en je ", StringComparison.Ordinal)
            .Replace(". Ook Je ", ". Ook je ", StringComparison.Ordinal)
            .Replace(". Daarbij Je ", ". Daarbij je ", StringComparison.Ordinal)
            .Replace(". In je vrije ", ". In je vrije ", StringComparison.Ordinal)
            .Replace(". in de beroepentest", ". In de beroepentest", StringComparison.OrdinalIgnoreCase);
    }
}
