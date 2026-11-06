namespace Jobsy.Core.Scholen;

/// <summary>Kid-friendly RIASEC labels used in teacher group views (nl keys in UiStringsScholen).</summary>
public static class RiasecKidLabels
{
    public static readonly (string Letter, string LabelKey)[] Order =
    [
        ("R", "Leraar.Riasec.R"),
        ("I", "Leraar.Riasec.I"),
        ("A", "Leraar.Riasec.A"),
        ("S", "Leraar.Riasec.S"),
        ("E", "Leraar.Riasec.E"),
        ("C", "Leraar.Riasec.C"),
    ];

    public static string LabelKey(string letter)
    {
        var upper = (letter ?? "").Trim().ToUpperInvariant();
        foreach (var (l, key) in Order)
        {
            if (l == upper)
            {
                return key;
            }
        }

        return "Leraar.Riasec.Unknown";
    }
}
