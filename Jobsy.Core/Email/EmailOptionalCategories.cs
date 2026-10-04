namespace Jobsy.Core.Email;

/// <summary>Optional (§M kind O) mail categories that support opt-out.</summary>
public static class EmailOptionalCategories
{
    public const string PushBom = "PushBom";
    public const string VacancyEngagementReminder = "VacancyEngagementReminder";
    public const string CompanyReEngagement = "CompanyReEngagement";

    /// <summary>
    /// Come-back reminders. Unlike the other optional categories this one starts off
    /// until the candidate opts in.
    /// </summary>
    public const string ComebackReminder = "ComebackReminder";

    public static readonly IReadOnlyList<string> All =
    [
        PushBom,
        VacancyEngagementReminder,
        CompanyReEngagement,
        ComebackReminder
    ];

    public static bool IsOptional(string? key)
        => All.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
}
