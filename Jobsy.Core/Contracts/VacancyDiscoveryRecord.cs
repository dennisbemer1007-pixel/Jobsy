using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Contracts;

/// <summary>
/// In-memory banenkaart snapshot. Public vacancy fields only — no candidate PII.
/// </summary>
public sealed record VacancyDiscoveryRecord(
    Guid Id,
    string Title,
    string Description,
    decimal HourlyWage,
    DateOnly StartDate,
    DateOnly EndDate,
    VacancyStatus Status,
    Guid CompanyId,
    string CompanyName,
    string CompanyAddress,
    string? CompanyLogoUrl,
    string? ImageUrl,
    string? VideoUrl,
    double Latitude,
    double Longitude,
    TransportMode RequiredTransport,
    string[] RequiredTransportLabels,
    WorkType WorkTypes,
    string? WorkTypeLabels,
    string[] WorkTypeLabelList,
    bool IsHighlighted,
    DateTime? HighlightedUntil,
    int ExtensionCount,
    Guid? SalaryTableId,
    IReadOnlyList<WageAgeBand> SalaryRates,
    string? RequiredDrivingLicense,
    string? RequiredEducation,
    int? MinimumEmployers,
    Guid? FulfilledByApplicationId,
    VacancySource CreatedVia,
    decimal? MinHoursPerWeek,
    decimal? MaxHoursPerWeek,
    bool FlexibleTimes,
    string? ScheduleJson,
    bool? LegalWorksAfter19,
    bool? LegalNightShift23To06,
    bool? LegalAdultSupervisorPresent,
    bool? LegalHandlesMoneyOrClosing,
    bool? LegalHeavyOrHazardousWork,
    string? OfferedByLabel,
    bool ShowClientAddressOnMap,
    Guid? IntermediaryCompanyId,
    VacancyKind Kind,
    Guid? ExclusivitySettingId,
    string? ExclusivityName,
    bool ExclusivityIsOpen,
    string? ExclusivitySchoolDomain,
    IReadOnlyList<string> ExclusivityEducations,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryColorHex,
    bool SuitableFor65Plus,
    string? KvkNumber,
    string? Vestigingsnummer,
    bool ContentModerationPassed,
    bool RequireEmailVerification,
    int? MinimumReferences = null,
    IReadOnlyList<string>? CulturePillars = null,
    /// <summary>
    /// True when the vacancy company (and intermediary, if any) is Verified.
    /// Set at index build; used by <see cref="Rules.PublicVisibility"/>.
    /// </summary>
    bool PublisherVerified = true,
    /// <summary>Non-removed engagement claims (ids + checked) for badges and match bonus.</summary>
    IReadOnlyList<VacancyEngagementItem>? EngagementItems = null,
    /// <summary>Acceptatie CLI test vacancy; filtered out for real/anonymous viewers.</summary>
    bool IsTestData = false);

/// <summary>Discovery snapshot of one engagement claim (no proof text / URLs).</summary>
public sealed record VacancyEngagementItem(string ItemId, bool Checked);
