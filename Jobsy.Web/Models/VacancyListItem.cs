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
    public int ExtensionCount { get; set; }
    public List<WageByAgeItem> WageByAge { get; set; } = [];
    public int? ResolvedForAge { get; set; }
    public string[] WorkTypes { get; set; } = [];
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
}

public sealed class CandidatePreferences
{
    public List<string> Roles { get; set; } = [];
    public int? MaxTravelMinutes { get; set; }
    public string? PreferredTransport { get; set; }
    public string? Language { get; set; }
    public int? AgeYears { get; set; }
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

public sealed class MetricCount
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class MetricDrilldownItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? Amount { get; set; }
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
