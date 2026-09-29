namespace Jobsy.Api.Models;

public record WageByAgeDto(int AgeYears, decimal HourlyRate, string Label);

public record VacancyListItemDto(
    Guid Id,
    string Title,
    string? Description,
    decimal? HourlyWage,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    Guid CompanyId,
    string CompanyName,
    string CompanyAddress,
    string? CompanyLogoUrl,
    string? ImageUrl,
    double Latitude,
    double Longitude,
    string[] RequiredTransport,
    bool WageVisible = true,
    int? TravelMinutes = null,
    double? DistanceKm = null,
    bool IsHighlighted = false,
    DateTime? HighlightedUntil = null,
    int ExtensionCount = 0,
    string? VideoUrl = null,
    Guid? SalaryTableId = null,
    IReadOnlyList<WageByAgeDto>? WageByAge = null,
    int? ResolvedForAge = null,
    string[]? WorkTypes = null,
    int ImpressionCount = 0,
    int ClickCount = 0,
    int ApplicationCount = 0,
    string? RequiredDrivingLicense = null,
    string? RequiredEducation = null,
    int? MinimumEmployers = null,
    Guid? FulfilledByApplicationId = null,
    string CreatedVia = "Manual",
    decimal? MinHoursPerWeek = null,
    decimal? MaxHoursPerWeek = null,
    bool FlexibleTimes = false,
    string? ScheduleJson = null,
    bool? LegalWorksAfter19 = null,
    bool? LegalNightShift23To06 = null,
    bool? LegalAdultSupervisorPresent = null,
    bool? LegalHandlesMoneyOrClosing = null,
    bool? LegalHeavyOrHazardousWork = null,
    int ShareCount = 0,
    int LikeCount = 0,
    string? OfferedByLabel = null,
    bool ShowClientAddressOnMap = false,
    Guid? IntermediaryCompanyId = null,
    string Kind = "Regular",
    Guid? ExclusivitySettingId = null,
    string? ExclusivityName = null,
    bool ExclusivityIsOpen = true,
    string? ExclusivitySchoolDomain = null,
    string? ExclusivityStudentNumberPattern = null,
    IReadOnlyList<string>? ExclusivityEducations = null,
    Guid? CategoryId = null,
    string? CategoryName = null,
    string? CategoryColorHex = null,
    bool CategoryHighlightAvailable = true,
    bool CategoryPushBomAvailable = true,
    decimal? CategoryPublishCostTokens = null,
    decimal? CategoryHighlightCostTokens = null,
    decimal? CategoryPushBomCostTokens = null,
    bool CategoryUseTierPushBomPricing = true,
    IReadOnlyDictionary<string, string>? CategoryFields = null,
    bool SuitableFor65Plus = false,
    string? KvkNumber = null,
    string? Vestigingsnummer = null,
    bool ContentModerationPassed = true,
    bool IsIncomplete = false,
    string? DisplayStatus = null,
    string? ModerationWarning = null,
    bool RequireEmailVerification = false,
    string? EngagementReminderTip = null,
    DateTime? EngagementReminderSentAtUtc = null,
    int? MinimumReferences = null,
    int? MatchPercent = null,
    string? MatchColorBand = null,
    string? MatchWhySummary = null,
    IReadOnlyList<string>? MatchWhy = null,
    IReadOnlyList<string>? MatchGaps = null,
    bool IsBroadMatch = false,
    string? MatchRationale = null,
    IReadOnlyList<string>? CulturePillars = null,
    int? CultureFitPercent = null,
    string? CultureFitBand = null,
    string? CultureFitLabel = null,
    string? CultureFitWhy = null,
    string CultureFitStatus = "Ready",
    string? BarrierKind = null,
    IReadOnlyList<string>? BarrierDiplomas = null,
    IReadOnlyList<string>? BarrierCertifications = null,
    int? BarrierMinExperienceYears = null,
    int? BarrierMinExperienceHours = null,
    IReadOnlyList<string>? BarrierHardChecks = null,
    /// <summary>PendingApproval options (manage list).</summary>
    bool RequestedHighlight = false,
    bool RequestedPushBom = false,
    bool RequestedExtend = false,
    /// <summary>Pending applications (new badge on manage list).</summary>
    int NewApplicationCount = 0,
    /// <summary>True when a PushBom spend exists for this vacancy.</summary>
    bool HasPushBom = false,
    /// <summary>Missing draft fields count when incompleteness is known.</summary>
    int IncompleteFieldCount = 0,
    /// <summary>Short requester name for publicatieaanvraag rows.</summary>
    string? RequesterDisplayName = null);

/// <summary>Public MapLibre opening camera. Coordinates only — no vacancy or employer PII.</summary>
public sealed record VacancyMapViewDto(double Lat, double Lng, double Zoom, int PinCount);

/// <summary>
/// Compact banenkaart pin for MapLibre. No titles/addresses — card loads on pin open.
/// <paramref name="Colour"/> is the category (or 65+) hex; <paramref name="MatchPercent"/> only for candidates.
/// Marker fields (<see cref="Highlighted"/>, <see cref="WorkType"/>, <see cref="MatchColorBand"/>) keep pins
/// looking the same before the card fetch completes.
/// </summary>
public sealed record VacancyPinDto(
    Guid Id,
    double Lat,
    double Lng,
    string? Colour = null,
    int? MatchPercent = null,
    bool Highlighted = false,
    uint HighlightRank = 0,
    string? WorkType = null,
    string? MatchColorBand = null);

/// <summary>
/// Lightweight popup/list card from the in-memory discovery index (no DB round-trip).
/// </summary>
public sealed record VacancyCardDto(
    Guid Id,
    string Title,
    string CompanyName,
    string? OfferedByLabel,
    string Place,
    string? ThumbnailUrl,
    string? LogoUrl,
    decimal? HourlyWage,
    bool WageVisible,
    string[]? WorkTypes,
    bool IsHighlighted,
    int? TravelMinutes = null,
    int? MatchPercent = null,
    string? MatchColorBand = null,
    string? CategoryColorHex = null,
    string? CompanyAddress = null,
    string? KvkNumber = null,
    string? Vestigingsnummer = null);

/// <summary>Exact origin→vacancy travel for the selected transport. No PII.</summary>
public sealed record VacancyTravelDto(int? TravelMinutes, double? DistanceKm);
