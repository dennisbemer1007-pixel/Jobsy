namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>Dutch labels from CSV dimensie column (no English codes, no truncation).</summary>
public static class PupilMboHboDimensionLabels
{
    public static string Display(string dimension)
    {
        if (string.IsNullOrWhiteSpace(dimension))
        {
            return string.Empty;
        }

        var s = dimension.Trim();
        var dash = s.IndexOf('–', StringComparison.Ordinal);
        if (dash < 0)
        {
            dash = s.IndexOf('-', StringComparison.Ordinal);
        }

        if (dash >= 0 && dash + 1 < s.Length)
        {
            s = s[(dash + 1)..].Trim();
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
