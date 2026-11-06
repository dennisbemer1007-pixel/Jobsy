namespace Jobsy.Core.Entities;

/// <summary>Persisted Horizon career plan for one candidate (at most one Active plan per user).</summary>
public class CandidateCareerPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DreamTitle { get; set; } = "";
    /// <summary>Normalized dream title for change detection.</summary>
    public string DreamKey { get; set; } = "";
    /// <summary>JSON array of step content (StepKey, Order, Title, Courses, …).</summary>
    public string PlanJson { get; set; } = "[]";
    public int MatchPercent { get; set; }
    public string MatchSummary { get; set; } = "";

    /// <summary>Active | Archived</summary>
    public string Status { get; set; } = CareerPlanStatuses.Active;

    public DateTime? ArchivedAtUtc { get; set; }

    /// <summary>UI language used when the plan text was generated (D13).</summary>
    public string PlanLanguage { get; set; } = "nl";

    /// <summary>True only when the OpenAI path produced the steps.</summary>
    public bool FromAi { get; set; }

    /// <summary>Suggestion | Catalog | FreeText | Wizard</summary>
    public string DreamSource { get; set; } = CareerDreamSources.Wizard;

    public string? DreamCatalogKey { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<CandidateCareerStepProgress> StepProgress { get; set; } = [];
}

public static class CareerPlanStatuses
{
    public const string Active = "Active";
    public const string Archived = "Archived";
}

public static class CareerDreamSources
{
    public const string Suggestion = "Suggestion";
    public const string Catalog = "Catalog";
    public const string FreeText = "FreeText";
    public const string Wizard = "Wizard";
}
