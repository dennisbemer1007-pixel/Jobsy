namespace Jobsy.Core.Rules;

public static class MasterdataCategories
{
    public const string Branch = "Branch";
    public const string DrivingLicense = "DrivingLicense";
    public const string EducationLevel = "EducationLevel";
    public const string MinEmployers = "MinEmployers";

    public static readonly string[] All =
    [
        Branch,
        DrivingLicense,
        EducationLevel,
        MinEmployers
    ];

    public static string DisplayName(string category) => category switch
    {
        Branch => "Branche",
        DrivingLicense => "Rijbewijs",
        EducationLevel => "Opleidingsniveau",
        MinEmployers => "Minimaal aantal werkgevers",
        _ => category
    };

    public static bool IsKnown(string? category) =>
        !string.IsNullOrWhiteSpace(category)
        && All.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string category) =>
        All.First(c => string.Equals(c, category.Trim(), StringComparison.OrdinalIgnoreCase));
}
