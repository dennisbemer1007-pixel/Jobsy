using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Delivered proactive match tip for a candidate (internal or external vacancy).</summary>
public class ProactiveMatchSuggestion
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    public Guid? VacancyId { get; set; }
    public Vacancy? Vacancy { get; set; }

    public Guid? ExternalVacancyId { get; set; }
    public CandidateExternalVacancy? ExternalVacancy { get; set; }

    public int MatchPercent { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CompanyLabel { get; set; }

    public DateTime DeliveredAtUtc { get; set; } = DateTime.UtcNow;
    public ProactiveMatchFeedback? Feedback { get; set; }
    public DateTime? FeedbackAtUtc { get; set; }

    public Guid? NotificationId { get; set; }
}
