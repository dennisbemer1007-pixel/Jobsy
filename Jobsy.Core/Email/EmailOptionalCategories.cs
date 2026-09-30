namespace Jobsy.Core.Email;

/// <summary>The three optional (§M kind O) mail categories that support opt-out.</summary>
public static class EmailOptionalCategories
{
    public const string PushBom = "PushBom";
    public const string VacancyEngagementReminder = "VacancyEngagementReminder";
    public const string CompanyReEngagement = "CompanyReEngagement";

    public static readonly IReadOnlyList<string> All =
    [
        PushBom,
        VacancyEngagementReminder,
        CompanyReEngagement
    ];

    public static bool IsOptional(string? key)
        => All.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
}
