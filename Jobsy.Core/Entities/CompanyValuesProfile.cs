namespace Jobsy.Core.Entities;

/// <summary>
/// Employer-declared kernwaarden (3 cards → Schwartz driver percents) on the root organisation.
/// Vestigingen fall back to the organisation profile.
/// </summary>
public class CompanyValuesProfile
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>JSON array of exactly 3 stable card ids from <c>CompanyValueCards</c>.</summary>
    public string CardIdsJson { get; set; } = "[]";

    public int AutonomyPercent { get; set; }
    public int ConnectionPercent { get; set; }
    public int AchievementPercent { get; set; }
    public int StabilityPercent { get; set; }
    public int ImpactPercent { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
