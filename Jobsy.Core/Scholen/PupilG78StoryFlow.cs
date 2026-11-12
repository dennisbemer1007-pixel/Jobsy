namespace Jobsy.Core.Scholen;

/// <summary>Connects canonical je-clauses into flowing B1 paragraphs.</summary>
internal static class PupilG78StoryFlow
{
    private static readonly string[] Openers =
    [
        "In je antwoorden zie je dat ",
        "Je gaf aan dat ",
        "Wat opvalt is dat ",
    ];

    public static string MainClause(G78AnswerClause clause)
    {
        if (clause.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
            || clause.Text.StartsWith("Juist hier zie je het anders", StringComparison.Ordinal))
        {
            return "";
        }

        return PupilG78QuestionJeForms.ForItem(clause.ItemId)
               ?? PupilG78JeNarrative.AsSentence(clause.Text);
    }

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

        var sb = new System.Text.StringBuilder(parts[0]);
        for (var i = 1; i < parts.Count; i++)
        {
            var connector = i switch
            {
                1 => ", en ",
                2 => ". Ook ",
                _ => ". Daarbij ",
            };
            var next = parts[i];
            if (connector == ", en ")
            {
                next = LowerFirst(next);
            }

            sb.Append(connector).Append(next);
        }

        sb.Append('.');
        return sb.ToString();
    }

    public static string OptionalOpenerOnce(ref OpenerUse used, int index, string datClause)
    {
        if (string.IsNullOrWhiteSpace(datClause))
        {
            return "";
        }

        var key = Math.Abs(index) % Openers.Length;
        if (key == 0 && used.Antwoorden)
        {
            key = 1;
        }

        if (key == 1 && used.GafAan)
        {
            key = 2;
        }

        if (key == 2 && used.Opvalt)
        {
            return "";
        }

        switch (key)
        {
            case 0:
                used.Antwoorden = true;
                return Openers[0] + datClause;
            case 1:
                used.GafAan = true;
                return Openers[1] + datClause;
            default:
                used.Opvalt = true;
                return Openers[2] + datClause;
        }
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

        if (sentence.Length >= 4 && char.IsUpper(sentence[0]))
        {
            return char.ToLowerInvariant(sentence[0]) + sentence[1..];
        }

        return sentence;
    }

    public static string PolishConnectors(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var s = text
            .Replace(", en Je ", ", en je ", StringComparison.Ordinal)
            .Replace(". Ook Je ", ". Ook je ", StringComparison.Ordinal)
            .Replace(". Daarbij Je ", ". Daarbij je ", StringComparison.Ordinal)
            .Replace(". Ook\n", ". ", StringComparison.Ordinal);
        if (s.EndsWith(" Ook", StringComparison.Ordinal)
            || s.EndsWith(". Ook", StringComparison.Ordinal))
        {
            s = s[..s.LastIndexOf(" Ook", StringComparison.Ordinal)].TrimEnd();
            if (!s.EndsWith('.'))
            {
                s += ".";
            }
        }

        return s;
    }

    internal struct OpenerUse
    {
        public bool Antwoorden;
        public bool GafAan;
        public bool Opvalt;
    }
}
