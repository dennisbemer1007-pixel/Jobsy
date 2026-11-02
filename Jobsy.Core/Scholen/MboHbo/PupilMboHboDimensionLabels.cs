namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>Dutch labels from CSV dimensie column (no English codes, no truncation).</summary>
public static class PupilMboHboDimensionLabels
{
    private static readonly Dictionary<char, string> RiasecShortLabels = new()
    {
        ['R'] = "R (Praktisch)",
        ['I'] = "I (Onderzoekend)",
        ['A'] = "A (Artistiek)",
        ['S'] = "S (Sociaal)",
        ['E'] = "E (Ondernemend)",
        ['C'] = "C (Conventioneel)",
    };

    public static string Display(string dimension)
    {
        if (string.IsNullOrWhiteSpace(dimension))
        {
            return string.Empty;
        }

        var s = dimension.Trim();
        if (s.Length == 1 && RiasecShortLabels.TryGetValue(char.ToUpperInvariant(s[0]), out var shortLabel))
        {
            return shortLabel;
        }
        var dash = s.IndexOf('–', StringComparison.Ordinal);
        if (dash < 0)
        {
            dash = s.IndexOf('-', StringComparison.Ordinal);
        }

        if (dash >= 0 && dash + 1 < s.Length)
        {
            s = s[(dash + 1)..].Trim();
        }

        var riasec = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^([RISCE])\s*\(([^)]+)\)\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (riasec.Success)
        {
            var letter = riasec.Groups[1].Value.ToUpperInvariant();
            var name = riasec.Groups[2].Value.Trim();
            return $"{letter} ({name})";
        }

        var paren = s.IndexOf('(', StringComparison.Ordinal);
        if (paren > 0)
        {
            s = s[..paren].Trim();
        }

        return s;
    }

    /// <summary>Grouping key for werkwaarden outcome bars (distinct where CSV repeats a short name).</summary>
    public static string BarGroupKey(string dimension)
    {
        if (string.IsNullOrWhiteSpace(dimension))
        {
            return string.Empty;
        }

        var raw = dimension.Trim();
        if (raw.StartsWith("Werkcontext", StringComparison.OrdinalIgnoreCase)
            && raw.Contains("Zekerheid", StringComparison.OrdinalIgnoreCase))
        {
            return "Vaste baan";
        }

        if (raw.StartsWith("Werkwaarde", StringComparison.OrdinalIgnoreCase)
            && raw.Contains("Zekerheid", StringComparison.OrdinalIgnoreCase))
        {
            return "Zekerheid (toekomst)";
        }

        return Display(dimension);
    }
}
