namespace Jobsy.Web.Models;

public class VacancyListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? HourlyWage { get; set; }
    public bool WageVisible { get; set; } = true;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public Guid? SalaryTableId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string[] RequiredTransport { get; set; } = [];
    public int? TravelMinutes { get; set; }
    public double? DistanceKm { get; set; }
    public bool IsHighlighted { get; set; }
    public DateTime? HighlightedUntil { get; set; }
    public int ExtensionCount { get; set; }
    public List<WageByAgeItem> WageByAge { get; set; } = [];
    public int? ResolvedForAge { get; set; }
    public string[] WorkTypes { get; set; } = [];
    public int ImpressionCount { get; set; }
    public int ClickCount { get; set; }
    public int ShareCount { get; set; }
    public int ApplicationCount { get; set; }
    public int LikeCount { get; set; }
    public string? OfferedByLabel { get; set; }
    public bool ShowClientAddressOnMap { get; set; }
    public Guid? IntermediaryCompanyId { get; set; }
    public string Kind { get; set; } = "Regular";
    public string? RequiredDrivingLicense { get; set; }
    public string? RequiredEducation { get; set; }
    public int? MinimumEmployers { get; set; }
    public Guid? FulfilledByApplicationId { get; set; }
    public string CreatedVia { get; set; } = "Manual";
    public decimal? MinHoursPerWeek { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }
    public bool FlexibleTimes { get; set; }
    public string? ScheduleJson { get; set; }
    public bool? LegalWorksAfter19 { get; set; }
    public bool? LegalNightShift23To06 { get; set; }
    public bool? LegalAdultSupervisorPresent { get; set; }
    public bool? LegalHandlesMoneyOrClosing { get; set; }
    public bool? LegalHeavyOrHazardousWork { get; set; }
}

public sealed class WageByAgeItem
{
    public int AgeYears { get; set; }
    public decimal HourlyRate { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class MeProfile
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public bool HasDateOfBirth { get; set; }
    public bool OpenForWork { get; set; }
    public CandidatePreferences Preferences { get; set; } = new();
    public bool AuthenticatorEnabled { get; set; }
    public double? HomeLatitude { get; set; }
    public double? HomeLongitude { get; set; }
    public string? ConsentVersion { get; set; }
    public bool NeedsConsentReaccept { get; set; }
    public string CurrentConsentVersion { get; set; } = string.Empty;
}

public sealed class CandidatePreferences
{
    public List<string> Roles { get; set; } = [];
    public int? MaxTravelMinutes { get; set; }
    public string? PreferredTransport { get; set; }
    public string? Language { get; set; }
    public int? AgeYears { get; set; }
    public string? AboutMe { get; set; }
    public List<string> DrivingLicenses { get; set; } = [];
    public Dictionary<string, List<string>> Availability { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<CandidateEmployerHistory> Employers { get; set; } = [];
    public List<string> Educations { get; set; } = [];
    public string? HomeAddress { get; set; }
    public decimal? MinHoursPerWeek { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }
    public bool? FlexibleTimes { get; set; }
}

public sealed class CandidateEmployerHistory
{
    public string EmployerName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int? Years { get; set; }
    public string? Description { get; set; }
}

public sealed class VacancyProductActionResult
{
    public VacancyListItem Vacancy { get; set; } = new();
    public bool PendingApproval { get; set; }
    public string? Message { get; set; }
    public int PushBomRecipientCount { get; set; }
}

public sealed class LikeStatus
{
    public bool Liked { get; set; }
}

public sealed class ClickRecordResult
{
    public bool Recorded { get; set; }
    public string? AnonymousKey { get; set; }
}

public sealed class OriginPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed record MockInterviewChatMessage(string Role, string Content);

public sealed class MockInterviewReply
{
    public string Reply { get; set; } = string.Empty;
    public bool UsedAi { get; set; }
    public string Disclaimer { get; set; } = string.Empty;
}

public sealed record AssistantChatMessage(string Role, string Content);

public sealed class AssistantChatReply
{
    public string Reply { get; set; } = string.Empty;
    public bool UsedAi { get; set; }
    public List<AssistantChatActionItem> Actions { get; set; } = [];
}

public sealed class AssistantChatActionItem
{
    public string Type { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? WorkType { get; set; }
    public string? SearchQuery { get; set; }
    public int? Count { get; set; }
    public string? Label { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid? VacancyId { get; set; }
    public int? MaxTravelMinutes { get; set; }
    public string? Transport { get; set; }
}

public sealed class MetricCount
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public List<decimal>? Sparkline { get; set; }
}

public sealed class MetricDrilldownItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? Amount { get; set; }
}

public sealed class VacancyPerformanceItem
{
    public Guid VacancyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public int Applications { get; set; }
}

public sealed class VacancyPerformanceBoard
{
    public string Period { get; set; } = string.Empty;
    public List<VacancyPerformanceItem> Top { get; set; } = [];
    public List<VacancyPerformanceItem> Flop { get; set; } = [];
}

public sealed class ClientPerformanceRow
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int ActiveVacancies { get; set; }
    public int ApplicationsPending { get; set; }
    public int Clicks { get; set; }
    public int Applications { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal AvgTravelMinutes { get; set; }
    public string? TopTransportMode { get; set; }
    public decimal TopTransportShare { get; set; }
    public decimal TokenBalance { get; set; }
    public int ActiveBoosts { get; set; }
    public int ExpiringWithin5Days { get; set; }
}

public sealed class ClientPerformanceBoard
{
    public string Period { get; set; } = string.Empty;
    public List<ClientPerformanceRow> Clients { get; set; } = [];
}

public sealed class CandidateEngagementItem
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public string VacancyTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Channel { get; set; }
    public string? ImageUrl { get; set; }
}
