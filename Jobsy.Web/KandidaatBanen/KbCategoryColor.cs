using System.Text.RegularExpressions;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>
/// Sanctions the only allowed inline <c>style=</c> on candidate job surfaces:
/// a single CSS custom property for a DB category colour.
/// </summary>
public static partial class KbCategoryColor
{
    public const string FallbackToken = "var(--muted)";

    /// <summary>
    /// Returns <c>--category-color:#rrggbb</c> for a valid 6-digit hex, otherwise
    /// <c>--category-color:var(--muted)</c>.
    /// </summary>
    public static string Style(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex) && HexPattern().IsMatch(hex.Trim()))
        {
            return $"--category-color:{hex.Trim()}";
        }

        return $"--category-color:{FallbackToken}";
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexPattern();
}
