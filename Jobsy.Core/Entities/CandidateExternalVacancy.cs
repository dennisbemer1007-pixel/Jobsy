using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Candidate-imported vacancy from an external URL (not a Lobsy vacancy row).</summary>
public class CandidateExternalVacancy
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    public string SourceUrl { get; set; } = string.Empty;
    public string SourceHost { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string HoursText { get; set; } = string.Empty;
    public string PayText { get; set; } = string.Empty;
    public string StartText { get; set; } = string.Empty;
    public string TrainingText { get; set; } = string.Empty;

    /// <summary>JSON string array of requirement bullets (B1 Dutch).</summary>
    public string RequirementsBulletsJson { get; set; } = "[]";

    /// <summary>Structured key/value facts extracted for sharing (JSON object).</summary>
    public string StructuredFactsJson { get; set; } = "{}";

    /// <summary>Candidate-only strengths/challenges (JSON).</summary>
    public string MatchInsightsJson { get; set; } = "{}";

    public int? TravelMinutesEstimate { get; set; }

    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    public CandidateExternalVacancyStatus Status { get; set; } = CandidateExternalVacancyStatus.Saved;

    public Guid? LinkedVacancyId { get; set; }
    public Vacancy? LinkedVacancy { get; set; }

    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }

    public ICollection<CandidateExternalVacancyOutbound> OutboundMessages { get; set; } = [];
}
