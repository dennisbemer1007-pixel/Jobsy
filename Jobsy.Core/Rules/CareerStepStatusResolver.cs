namespace Jobsy.Core.Rules;

public enum CareerStepProgressSource
{
    Manual = 0,
    Auto = 1,
    ManualUndo = 2
}

public sealed record CareerStepProgressSnapshot(
    string StepKey,
    int StepOrder,
    DateTime? CompletedAtUtc,
    CareerStepProgressSource Source,
    DateTime UpdatedAtUtc);

/// <summary>Stored step content inside CandidateCareerPlan.PlanJson (no runtime status).</summary>
public sealed record CareerPlanStepContent(
    string StepKey,
    int Order,
    string Title,
    string Summary,
    IReadOnlyList<string> SkillsGap,
    IReadOnlyList<string> Courses,
    IReadOnlyList<string> MinRequirements,
    int YearsExperienceNeeded,
    string ActionLabel,
    string ActionHref);

public sealed record CareerPlanCourseView(
    string Name,
    bool OnProfile);

public sealed record CareerPlanStepView(
    string StepKey,
    int Order,
    string Title,
    HorizonCareerStepKind Status,
    string Summary,
    IReadOnlyList<string> SkillsGap,
    IReadOnlyList<CareerPlanCourseView> Courses,
    IReadOnlyList<string> MinRequirements,
    int YearsExperienceNeeded,
    string ActionLabel,
    string ActionHref,
    int StepMatchPercent,
    int CoursesOnProfile,
    int CoursesTotal);

public sealed record CareerPlanView(
    Guid PlanId,
    string DreamTitle,
    string DreamKey,
    int MatchPercent,
    string MatchSummary,
    bool GoalReached,
    IReadOnlyList<CareerPlanStepView> Steps);

/// <summary>
/// Single source of truth for career-step Completed / Active / Open from progress rows + profile certificates.
/// </summary>
public static class CareerStepStatusResolver
{
    public static CareerPlanView Resolve(
        Guid planId,
        string dreamTitle,
        string dreamKey,
        int matchPercent,
        string matchSummary,
        IReadOnlyList<CareerPlanStepContent> steps,
        IReadOnlyList<CareerStepProgressSnapshot> progress,
        IReadOnlyList<string?> certificateNames)
    {
        var progressByKey = progress
            .GroupBy(p => p.StepKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAtUtc).First(), StringComparer.OrdinalIgnoreCase);

        var certs = certificateNames.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        var resolved = new List<CareerPlanStepView>(steps.Count);
        var completedFlags = new bool[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            progressByKey.TryGetValue(step.StepKey, out var row);
            var hasManualUndo = row is { Source: CareerStepProgressSource.ManualUndo };
            var hasCompletedStamp = row?.CompletedAtUtc is not null
                                    && row.Source is CareerStepProgressSource.Manual or CareerStepProgressSource.Auto;

            var allCoursesMatched = step.Courses.Count > 0
                                    && step.Courses.All(c => CareerCourseMatcher.IsOnProfile(c, certs));

            var completed = hasCompletedStamp
                            || (allCoursesMatched && !hasManualUndo && step.Courses.Count > 0);

            completedFlags[i] = completed;
        }

        var firstOpenIndex = -1;
        for (var i = 0; i < completedFlags.Length; i++)
        {
            if (!completedFlags[i])
            {
                firstOpenIndex = i;
                break;
            }
        }

        var goalReached = completedFlags.Length > 0 && completedFlags.All(c => c);

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var completed = completedFlags[i];
            var status = completed
                ? HorizonCareerStepKind.Completed
                : i == firstOpenIndex
                    ? HorizonCareerStepKind.Active
                    : HorizonCareerStepKind.Open;

            var courseViews = step.Courses
                .Select(c => new CareerPlanCourseView(c, CareerCourseMatcher.IsOnProfile(c, certs)))
                .ToList();
            var onProfile = courseViews.Count(c => c.OnProfile);

            resolved.Add(new CareerPlanStepView(
                step.StepKey,
                step.Order,
                step.Title,
                status,
                step.Summary,
                step.SkillsGap,
                courseViews,
                step.MinRequirements,
                step.YearsExperienceNeeded,
                step.ActionLabel,
                step.ActionHref,
                CareerCourseMatcher.StepMatchPercent(step.Courses, certs, completed),
                onProfile,
                courseViews.Count));
        }

        return new CareerPlanView(
            planId,
            dreamTitle,
            dreamKey,
            matchPercent,
            matchSummary,
            goalReached,
            resolved);
    }

    /// <summary>
    /// Detects steps that should receive an Auto progress row (all courses matched, no ManualUndo, not already stamped).
    /// </summary>
    public static IReadOnlyList<CareerPlanStepContent> StepsNeedingAutoComplete(
        IReadOnlyList<CareerPlanStepContent> steps,
        IReadOnlyList<CareerStepProgressSnapshot> progress,
        IReadOnlyList<string?> certificateNames)
    {
        var progressByKey = progress
            .GroupBy(p => p.StepKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAtUtc).First(), StringComparer.OrdinalIgnoreCase);

        var certs = certificateNames.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        var result = new List<CareerPlanStepContent>();
        foreach (var step in steps)
        {
            if (step.Courses.Count == 0)
            {
                continue;
            }

            progressByKey.TryGetValue(step.StepKey, out var row);
            if (row is { Source: CareerStepProgressSource.ManualUndo })
            {
                continue;
            }

            if (row?.CompletedAtUtc is not null
                && row.Source is CareerStepProgressSource.Manual or CareerStepProgressSource.Auto)
            {
                continue;
            }

            if (step.Courses.All(c => CareerCourseMatcher.IsOnProfile(c, certs)))
            {
                result.Add(step);
            }
        }

        return result;
    }
}
