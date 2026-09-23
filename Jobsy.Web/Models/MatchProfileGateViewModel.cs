namespace Jobsy.Web.Models;

/// <summary>Central view-model for Match tab unlock status (checklist + IsProfileComplete).</summary>
public sealed class MatchProfileGateViewModel
{
    public bool IsAuthenticated { get; set; }

    public bool ProfileBasicsFilled { get; set; }
    public bool HasEducationLevel { get; set; }
    public bool CompetencyCompleted { get; set; }
    public bool CareerCompleted { get; set; }
    public bool CultureCompleted { get; set; }

    /// <summary>True when every required Match checklist item is done.</summary>
    public bool IsProfileComplete { get; set; }

    public int CompletedCount { get; set; }
    public int RequiredCount { get; set; } = 5;

    public string? PreferredTransport { get; set; }
    public int? MaxTravelMinutes { get; set; }
    public IReadOnlyList<string> Educations { get; set; } = [];
    public double? HomeLatitude { get; set; }
    public double? HomeLongitude { get; set; }
}
