using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class JobSearchActivity
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    /// <summary>Calendar date in Europe/Amsterdam.</summary>
    public DateOnly ActivityDate { get; set; }

    public JobSearchActivityKind Kind { get; set; }
    public string Employer { get; set; } = string.Empty;
    public string FunctionTitle { get; set; } = string.Empty;
    public string? ContactMethod { get; set; }
    public string? Result { get; set; }

    public JobSearchActivitySource Source { get; set; } = JobSearchActivitySource.Manual;

    /// <summary>e.g. Application, Interview, ExternalVacancy</summary>
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }

    public Guid? AttachmentId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
