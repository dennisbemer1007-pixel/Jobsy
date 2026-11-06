namespace Jobsy.Core.Rules;

/// <summary>Fixed Lobsy brand colors for vacancy rich-text (no free color picker).</summary>
public static class VacancyRichtextPalette
{
    public sealed record ColorOption(string Key, string Hex, string LabelNl);

    public static readonly IReadOnlyList<ColorOption> All =
    [
        new("default", "", "Standaard"),
        new("brand", "#0f2d5c", "Donkerblauw"),
        new("muted", "#5a6a7d", "Grijs"),
        new("coral", "#f54a1b", "Koraal"),
        new("success", "#15803d", "Groen"),
        new("gold", "#c9a227", "Goud")
    ];

    private static readonly HashSet<string> AllowedHex = All
        .Where(c => !string.IsNullOrEmpty(c.Hex))
        .Select(c => c.Hex.ToLowerInvariant())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsAllowedHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var normalized = hex.Trim().ToLowerInvariant();
        if (!normalized.StartsWith('#'))
        {
            normalized = "#" + normalized;
        }

        return AllowedHex.Contains(normalized);
    }

    public static string NormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || !IsAllowedHex(hex))
        {
            return string.Empty;
        }

        var normalized = hex.Trim().ToLowerInvariant();
        return normalized.StartsWith('#') ? normalized : "#" + normalized;
    }
}
