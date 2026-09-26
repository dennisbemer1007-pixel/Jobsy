using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateCareerPlanService
{
    Task<HorizonCareerPathPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<HorizonCareerPathPlanView> GenerateAndSaveAsync(
        Guid userId,
        string dreamTitle,
        HorizonCareerProfileSnapshot snapshot,
        CancellationToken cancellationToken = default);

    Task<HorizonCareerPathPlanView?> CompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default);

    Task<HorizonCareerPathPlanView?> UncompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default);

    Task<HorizonCareerPathPlanView?> ClaimCourseAsync(
        Guid userId,
        string courseName,
        CancellationToken cancellationToken = default);
}

/// <summary>Resolved plan for API/UI (status + course match computed on read).</summary>
public sealed record HorizonCareerPathPlanView(
    string DreamTitle,
    int MatchPercent,
    string MatchSummary,
    bool GoalReached,
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
    string ActionLabel,
    string ActionHref,
    int StepMatchPercent,
    int MatchedCourseCount);

public sealed record HorizonCareerCourseView(string Name, bool OnProfile);
