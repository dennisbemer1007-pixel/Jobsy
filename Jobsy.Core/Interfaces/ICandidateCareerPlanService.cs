using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateCareerPlanService
{
    Task<HorizonCareerPathPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Persists the dream job title without regenerating the plan.</summary>
    Task SaveDreamAsync(Guid userId, string? dreamTitle, CancellationToken cancellationToken = default);

    Task<string?> GetDreamTitleAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues background plan generation when a dream title exists but the plan has no steps yet.
    /// </summary>
    Task EnqueueGenerationIfNeededAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Generates the plan when a pending dream stub exists (called from insights worker).</summary>
    Task TryGeneratePendingAsync(Guid userId, HorizonCareerProfileSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive-aware generation (§2, §6). <paramref name="dreamTitle"/> is used when neither
    /// <paramref name="catalogKey"/> nor free text is supplied (e.g. the wizard/pending path).
    /// A <paramref name="catalogKey"/> wins over <paramref name="dreamTitle"/> when both are given.
    /// </summary>
    Task<HorizonCareerPathPlanView> GenerateAndSaveAsync(
        Guid userId,
        string? dreamTitle,
        HorizonCareerProfileSnapshot snapshot,
        string? catalogKey = null,
        string? dreamSource = null,
        string? planLanguage = null,
        bool force = false,
        CancellationToken cancellationToken = default);

    /// <summary>409 <c>complete_previous_first</c> unless <paramref name="stepKey"/> is the Active step (idempotent when already Completed).</summary>
    Task<HorizonCareerPathPlanView?> CompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default);

    /// <summary>409 <c>undo_last_first</c> unless <paramref name="stepKey"/> is the last completed step.</summary>
    Task<HorizonCareerPathPlanView?> UncompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default);

    /// <summary>Newest-first, max 3 (D11).</summary>
    Task<IReadOnlyList<ArchivedCareerPlanView>> ListArchivedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives the current active plan (if any) and activates <paramref name="planId"/> with its own
    /// progress. No carry-over, no AI call. Returns null for a foreign or already-purged plan id (404).
    /// </summary>
    Task<HorizonCareerPathPlanView?> RestoreArchivedAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default);

    /// <summary>Dream-job suggestions + search results for the dream picker (B10).</summary>
    Task<CareerDreamOptionsView> GetDreamOptionsAsync(
        Guid userId,
        string? query,
        CancellationToken cancellationToken = default);
}

/// <summary>Resolved plan for API/UI (status + course match computed on read).</summary>
public sealed record HorizonCareerPathPlanView(
    string DreamTitle,
    int MatchPercent,
    string MatchSummary,
    bool GoalReached,
    bool FromAi,
    string PlanLanguage,
    string DreamFitBand,
    int CarriedOverCount,
    IReadOnlyList<HorizonCareerPathStepView> Steps);

public sealed record HorizonCareerPathStepView(
    string Id,
    int Order,
    string Title,
    string Status,
    string Summary,
    IReadOnlyList<string> SkillsGap,
    IReadOnlyList<HorizonCareerCourseView> Courses,
    IReadOnlyList<string> MinRequirements,
    int YearsExperienceNeeded,
    string StepFitBand,
    bool HeldBack,
    int MatchedCourseCount,
    IReadOnlyList<string> ActionKinds);

public sealed record HorizonCareerCourseView(string Name, bool OnProfile);

public sealed record ArchivedCareerPlanView(
    Guid PlanId,
    string DreamTitle,
    DateTime ArchivedAtUtc,
    int CompletedSteps,
    int TotalSteps,
    DateTime ExpiresAtUtc);

public sealed record CareerDreamOptionView(string? CatalogKey, string Title, string? ReasonKey);

public sealed record CareerDreamOptionsView(
    IReadOnlyList<CareerDreamOptionView> Suggestions,
    IReadOnlyList<CareerDreamOptionView> Results);
