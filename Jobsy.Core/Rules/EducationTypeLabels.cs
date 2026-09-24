namespace Jobsy.Core.Rules;

/// <summary>Canonical opleiding types for candidate education entries.</summary>
public static class EducationTypeLabels
{
    public const string Secondary = "Middelbare school";
    public const string Mbo = "MBO";
    public const string Hbo = "HBO";
    public const string Wo = "WO";
    public const string Course = "Cursus";
    public const string Training = "Training";
    public const string Other = "Anders";

    public static readonly string[] All =
    [
        Secondary,
        Mbo,
        Hbo,
        Wo,
        Course,
        Training,
        Other
    ];

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return All.FirstOrDefault(a => string.Equals(a, trimmed, StringComparison.OrdinalIgnoreCase))
               ?? (trimmed.Length <= 80 ? trimmed : trimmed[..80]);
    }
}
