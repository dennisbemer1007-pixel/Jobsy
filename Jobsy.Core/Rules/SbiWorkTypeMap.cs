namespace Jobsy.Core.Rules;

/// <summary>
/// Maps Dutch SBI codes to Lobsy branche labels (<see cref="WorkTypeLabels"/>).
/// Longest matching prefix wins; unknown / intermediair (78) codes map to nothing.
/// </summary>
public static class SbiWorkTypeMap
{
    public const int MaxBranches = 4;

    private static readonly (string Prefix, string Label)[] Rules =
    [
        ("01.1", WorkTypeLabels.Tuinbouw),
        ("01.2", WorkTypeLabels.Tuinbouw),
        ("01.3", WorkTypeLabels.Tuinbouw),
        ("81.3", WorkTypeLabels.Tuinbouw),
        ("81.2", WorkTypeLabels.Schoonmaak),
        ("55", WorkTypeLabels.Horeca),
        ("56", WorkTypeLabels.Horeca),
        ("47", WorkTypeLabels.Winkel),
        ("49", WorkTypeLabels.Logistiek),
        ("50", WorkTypeLabels.Logistiek),
        ("51", WorkTypeLabels.Logistiek),
        ("52", WorkTypeLabels.Logistiek),
        ("53", WorkTypeLabels.Logistiek),
        ("86", WorkTypeLabels.Zorg),
        ("87", WorkTypeLabels.Zorg),
        ("88", WorkTypeLabels.Zorg),
        ("69", WorkTypeLabels.Kantoor),
        ("70", WorkTypeLabels.Kantoor),
        ("71", WorkTypeLabels.Kantoor),
        ("72", WorkTypeLabels.Kantoor),
        ("73", WorkTypeLabels.Kantoor),
        ("74", WorkTypeLabels.Kantoor),
        ("75", WorkTypeLabels.Kantoor),
        ("82", WorkTypeLabels.Kantoor),
        ("84", WorkTypeLabels.Kantoor),
        ("64", WorkTypeLabels.Kantoor),
        ("65", WorkTypeLabels.Kantoor),
        ("66", WorkTypeLabels.Kantoor),
        ("62", WorkTypeLabels.Kantoor),
        ("63", WorkTypeLabels.Kantoor),
        ("41", WorkTypeLabels.Bouw),
        ("42", WorkTypeLabels.Bouw),
        ("43", WorkTypeLabels.Bouw),
        // Productie 10–33: listed as two-digit prefixes so longest-prefix rules above still win.
        ("10", WorkTypeLabels.Productie),
        ("11", WorkTypeLabels.Productie),
        ("12", WorkTypeLabels.Productie),
        ("13", WorkTypeLabels.Productie),
        ("14", WorkTypeLabels.Productie),
        ("15", WorkTypeLabels.Productie),
        ("16", WorkTypeLabels.Productie),
        ("17", WorkTypeLabels.Productie),
        ("18", WorkTypeLabels.Productie),
        ("19", WorkTypeLabels.Productie),
        ("20", WorkTypeLabels.Productie),
        ("21", WorkTypeLabels.Productie),
        ("22", WorkTypeLabels.Productie),
        ("23", WorkTypeLabels.Productie),
        ("24", WorkTypeLabels.Productie),
        ("25", WorkTypeLabels.Productie),
        ("26", WorkTypeLabels.Productie),
        ("27", WorkTypeLabels.Productie),
        ("28", WorkTypeLabels.Productie),
        ("29", WorkTypeLabels.Productie),
        ("30", WorkTypeLabels.Productie),
        ("31", WorkTypeLabels.Productie),
        ("32", WorkTypeLabels.Productie),
        ("33", WorkTypeLabels.Productie),
    ];

    /// <summary>
    /// Distinct <see cref="WorkTypeLabels"/> values in SBI-input order, max <see cref="MaxBranches"/>.
    /// SBI 78 (uitzend) and unknown codes contribute nothing.
    /// </summary>
    public static IReadOnlyList<string> Map(IEnumerable<string>? sbiCodes)
    {
        if (sbiCodes is null)
        {
            return [];
        }

        var result = new List<string>(MaxBranches);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var raw in sbiCodes)
        {
            var label = MapOne(raw);
            if (label is null || !seen.Add(label))
            {
                continue;
            }

            result.Add(label);
            if (result.Count >= MaxBranches)
            {
                break;
            }
        }

        return result;
    }

    public static string? MapOne(string? sbiCode)
    {
        if (string.IsNullOrWhiteSpace(sbiCode))
        {
            return null;
        }

        // Keep digits and dots so dotted prefixes (01.1, 81.2) still match.
        var normalized = new string(sbiCode.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (normalized.Length == 0)
        {
            return null;
        }

        // Intermediair uitzend — never a work-type branche.
        var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());
        if (digitsOnly.StartsWith("78", StringComparison.Ordinal))
        {
            return null;
        }

        string? bestLabel = null;
        var bestLen = -1;
        foreach (var (prefix, label) in Rules)
        {
            if (prefix.Length <= bestLen)
            {
                continue;
            }

            if (MatchesPrefix(normalized, digitsOnly, prefix))
            {
                bestLen = prefix.Length;
                bestLabel = label;
            }
        }

        return bestLabel;
    }

    private static bool MatchesPrefix(string withDots, string digitsOnly, string prefix)
    {
        if (prefix.Contains('.', StringComparison.Ordinal))
        {
            // Dotted rule: compare digit-only forms so "88101" still matches "88",
            // and "01.1" / "011" / "01.13" match "01.1".
            var prefixDigits = new string(prefix.Where(char.IsDigit).ToArray());
            return digitsOnly.StartsWith(prefixDigits, StringComparison.Ordinal)
                   || withDots.StartsWith(prefix, StringComparison.Ordinal);
        }

        return digitsOnly.StartsWith(prefix, StringComparison.Ordinal);
    }
}
