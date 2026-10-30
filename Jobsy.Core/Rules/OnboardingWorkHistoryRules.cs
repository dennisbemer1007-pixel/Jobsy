namespace Jobsy.Core.Rules;

/// <summary>UI and persistence rules for optional onboarding work-history rows.</summary>
public static class OnboardingWorkHistoryRules
{
    public static bool IsBlank(
        string? employerName,
        string? role,
        string? startMonth,
        string? endMonth,
        bool? isCurrent)
        => string.IsNullOrWhiteSpace(employerName)
           && string.IsNullOrWhiteSpace(role)
           && string.IsNullOrWhiteSpace(startMonth)
           && string.IsNullOrWhiteSpace(endMonth)
           && isCurrent != true;

    public static bool HasEngagedEntry(
        IEnumerable<(string? Employer, string? Role, string? Start, string? End, bool? IsCurrent)> rows)
        => rows.Any(row => !IsBlank(row.Employer, row.Role, row.Start, row.End, row.IsCurrent));
}
