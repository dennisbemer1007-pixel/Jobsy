namespace Jobsy.Web.Models;

public sealed class AnonymousTalentCard
{
    public Guid CandidateUserId { get; set; }
    public List<string> MatchTags { get; set; } = [];
    public List<string> RiasecTags { get; set; } = [];
    public CompetencyScoreSet? CompetencyScores { get; set; }
    public RiasecScoreSet? CareerScores { get; set; }
    public string? HollandCode { get; set; }
    public string? AvailabilitySummary { get; set; }
    public List<string> DrivingLicenses { get; set; } = [];
    public int? TravelMinutes { get; set; }
    public string? RegionLabel { get; set; }
    public bool CompetenceDeepCompleted { get; set; }
    public bool CareerDeepCompleted { get; set; }

    public bool DeepAnalysisCompleted => CompetenceDeepCompleted || CareerDeepCompleted;
}

public sealed class TalentContactRequestModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CandidateUserId { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime RespondByUtc { get; set; }
    public bool PiiRevealed { get; set; }
    public string? CandidateFullName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? CandidatePhone { get; set; }
}
