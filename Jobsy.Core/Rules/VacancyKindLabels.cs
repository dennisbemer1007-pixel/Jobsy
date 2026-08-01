using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class VacancyKindLabels
{
    public const string Regular = "Regulier";
    public const string Internship = "Stageplek";
    public const string Volunteer = "Vrijwilligerswerk";

    public static string ToDutch(VacancyKind kind) => kind switch
    {
        VacancyKind.Internship => Internship,
        VacancyKind.Volunteer => Volunteer,
        _ => Regular
    };

    public static string ToDutch(string? kind) =>
        Enum.TryParse<VacancyKind>(kind, ignoreCase: true, out var parsed)
            ? ToDutch(parsed)
            : Regular;

    public static VacancyKind ParseOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return VacancyKind.Regular;
        }

        if (Enum.TryParse<VacancyKind>(value, ignoreCase: true, out var kind))
        {
            return kind;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "regulier" or "regular" or "vaste baan" => VacancyKind.Regular,
            "stage" or "stageplek" or "internship" => VacancyKind.Internship,
            "vrijwillig" or "vrijwilligerswerk" or "volunteer" => VacancyKind.Volunteer,
            _ => VacancyKind.Regular
        };
    }
}
