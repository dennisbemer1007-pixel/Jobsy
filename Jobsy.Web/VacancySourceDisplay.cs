namespace Jobsy.Web;

public static class VacancySourceDisplay
{
    public static string CssModifier(string? createdVia) => createdVia?.Trim().ToLowerInvariant() switch
    {
        "api" => "api",
        "csv" => "csv",
        "ats" => "ats",
        _ => "manual"
    };

    public static string Label(string? createdVia) => createdVia?.Trim().ToLowerInvariant() switch
    {
        "api" => "API",
        "csv" => "CSV",
        "ats" => "ATS",
        _ => "Handmatig"
    };
}
