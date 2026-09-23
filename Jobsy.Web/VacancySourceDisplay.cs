namespace Jobsy.Web;

/// <summary>
/// Display helpers for vacancy origin. Channel is the admin-facing binary:
/// ATS (scrape pipeline) vs Regulier (manual / API / CSV).
/// </summary>
public static class VacancySourceDisplay
{
    public static bool IsAts(string? createdVia)
        => string.Equals(createdVia?.Trim(), "ats", StringComparison.OrdinalIgnoreCase);

    public static string CssModifier(string? createdVia) => createdVia?.Trim().ToLowerInvariant() switch
    {
        "api" => "api",
        "csv" => "csv",
        "ats" => "ats",
        _ => "manual"
    };

    /// <summary>Detailed origin label (Handmatig / API / CSV / ATS).</summary>
    public static string Label(string? createdVia) => createdVia?.Trim().ToLowerInvariant() switch
    {
        "api" => "API",
        "csv" => "CSV",
        "ats" => "ATS",
        _ => "Handmatig"
    };

    /// <summary>Admin channel badge: ATS vs Regulier.</summary>
    public static string ChannelLabel(string? createdVia)
        => IsAts(createdVia) ? "ATS" : "Regulier";

    public static string ChannelCssModifier(string? createdVia)
        => IsAts(createdVia) ? "ats" : "regular";
}
