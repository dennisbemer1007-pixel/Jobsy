namespace Jobsy.Core.Entities;

/// <summary>Stored culture-fit result for one candidate × vacancy (AI refine on write/queue).</summary>
public class CandidateVacancyCultureFit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;

    /// <summary>JSON of <see cref="Jobsy.Core.Rules.CultureFitResult"/>.</summary>
    public string ResultJson { get; set; } = "{}";
    public string InputFingerprint { get; set; } = "";
    public bool FromOpenAi { get; set; }
    public DateTime ComputedAtUtc { get; set; }
}
