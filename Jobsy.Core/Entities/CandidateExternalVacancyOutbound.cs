namespace Jobsy.Core.Entities;

/// <summary>Employer e-mail sent for an external vacancy application (one primary + optional reminder).</summary>
public class CandidateExternalVacancyOutbound
{
    public Guid Id { get; set; }

    public Guid ExternalVacancyId { get; set; }
    public CandidateExternalVacancy ExternalVacancy { get; set; } = null!;

    public string EmployerEmailNormalized { get; set; } = string.Empty;
    public string Motivation { get; set; } = string.Empty;

    /// <summary>Shared fact keys/values sent to the employer (JSON).</summary>
    public string SharedFactsJson { get; set; } = "{}";

    public DateTime InitialSentAtUtc { get; set; }
    public DateTime? ReminderSentAtUtc { get; set; }

    /// <summary>Employer opened the signed invite link (CTA “Bekijk sollicitatie”).</summary>
    public DateTime? ClickedAtUtc { get; set; }
    public DateTime? EmployerAccountCreatedAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }

    public Guid? OneTimeLinkId { get; set; }
    public OneTimeLink? OneTimeLink { get; set; }
}
