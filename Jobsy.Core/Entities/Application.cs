using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
    public Guid? CandidateUserId { get; set; }
    public User? CandidateUser { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidateCity { get; set; }
    public string? CandidateAddress { get; set; }
    public string PreferredTransport { get; set; } = string.Empty;
    public int EstimatedTravelMinutes { get; set; }
    public double? DistanceKm { get; set; }
    public string? PreferencesSummary { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime? ConsentAcceptedAt { get; set; }
    public string? ConsentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
