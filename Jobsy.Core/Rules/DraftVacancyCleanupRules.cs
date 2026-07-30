namespace Jobsy.Core.Rules;

/// <summary>Rules for never-published draft vacancy cleanup.</summary>
public static class DraftVacancyCleanupRules
{
    /// <summary>Days in Draft (never published) before warning e-mail.</summary>
    public const int WarningAfterDays = 30;

    /// <summary>Days after warning before hard delete (30 + 14 = 44 total).</summary>
    public const int DeleteAfterWarningDays = 14;

    public const int DeleteAfterDays = WarningAfterDays + DeleteAfterWarningDays;

    public const string WarningEmailCategory = "DraftVacancyCleanupWarning";
    public const string ReengagementEmailCategory = "CompanyReEngagement";
}
