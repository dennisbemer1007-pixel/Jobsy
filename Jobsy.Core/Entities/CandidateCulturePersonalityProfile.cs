namespace Jobsy.Core.Entities;

/// <summary>
/// Candidate culture &amp; personality Quick-Scan (18 items: culture dims + IPIP-style facets).
/// Replaces the legacy DISC gedragsanalyse.
/// </summary>
public class CandidateCulturePersonalityProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Status { get; set; } = CandidateCompetencyStatuses.Draft;
    public string AnswersJson { get; set; } = "{}";

    public int? AutonomyPercent { get; set; }
    public int? InformalPercent { get; set; }
    public int? CollaborationPercent { get; set; }
    public int? FlexibilityPercent { get; set; }
    public int? InnovationPercent { get; set; }
    public int? PeopleFirstPercent { get; set; }

    public int? OpennessPercent { get; set; }
    public int? ConscientiousnessPercent { get; set; }
    public int? ExtraversionPercent { get; set; }
    public int? AgreeablenessPercent { get; set; }
    public int? EmotionalStabilityPercent { get; set; }

    public string MatchTagsJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Employer-declared objective culture profile (same culture dimensions as the candidate scan).
/// </summary>
public class CompanyCultureProfile
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Status { get; set; } = CandidateCompetencyStatuses.Draft;
    public string AnswersJson { get; set; } = "{}";

    public int? AutonomyPercent { get; set; }
    public int? InformalPercent { get; set; }
    public int? CollaborationPercent { get; set; }
    public int? FlexibilityPercent { get; set; }
    public int? InnovationPercent { get; set; }
    public int? PeopleFirstPercent { get; set; }

    /// <summary>Optional personality tone the company seeks (same facets as candidate).</summary>
    public int? OpennessPercent { get; set; }
    public int? ConscientiousnessPercent { get; set; }
    public int? ExtraversionPercent { get; set; }
    public int? AgreeablenessPercent { get; set; }
    public int? EmotionalStabilityPercent { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
