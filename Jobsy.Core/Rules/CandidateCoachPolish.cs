namespace Jobsy.Core.Rules;

/// <summary>Plain coach text: no markup leftovers, no known typo, and a capital first letter.</summary>
public static class CandidateCoachPolish
{
    public static string Apply(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var cleaned = text.Replace("horecaberoven", "horecaberoepen", StringComparison.OrdinalIgnoreCase).Trim();
        var index = 0;
        while (index < cleaned.Length && char.IsWhiteSpace(cleaned[index]))
        {
            index++;
        }

        if (index >= cleaned.Length || !char.IsLetter(cleaned[index]) || char.IsUpper(cleaned[index]))
        {
            return cleaned;
        }

        return cleaned[..index] + char.ToUpper(cleaned[index]) + cleaned[(index + 1)..];
    }
}
