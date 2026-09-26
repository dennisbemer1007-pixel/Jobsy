using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Single source of truth for career-step Completed / Active / Open.
/// </summary>
public static class CareerStepStatusResolver
{
    public sealed record ProgressSignal(
        string StepKey,
        DateTime? CompletedAtUtc,
        string Source);

    public sealed record StepInput(
        string StepKey,
        int Order,
        IReadOnlyList<string> Courses);

    public sealed record ResolvedStep(
        string StepKey,
        int Order,
        HorizonCareerStepKind Status,
        int StepMatchPercent,
        int MatchedCourseCount,
        bool AutoCompletable);

    public static IReadOnlyList<ResolvedStep> Resolve(
        IReadOnlyList<StepInput> steps,
        IReadOnlyList<ProgressSignal> progress,
        IEnumerable<CandidateCertificateDto>? certificates)
    {
        var byKey = progress
            .GroupBy(p => p.StepKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CompletedAtUtc ?? DateTime.MinValue).First(), StringComparer.OrdinalIgnoreCase);

        var ordered = steps.OrderBy(s => s.Order).ToList();
        var resolved = new List<ResolvedStep>(ordered.Count);
        foreach (var step in ordered)
        {
            byKey.TryGetValue(step.StepKey, out var signal);
            var matched = CareerCourseMatcher.MatchedCount(step.Courses, certificates);
            var matchPercent = step.Courses.Count == 0
                ? (signal?.CompletedAtUtc is not null ? 100 : 0)
                : CareerCourseMatcher.StepMatchPercent(step.Courses, certificates);

            var blockedByUndo = signal is not null
                && string.Equals(signal.Source, CareerStepProgressSources.ManualUndo, StringComparison.OrdinalIgnoreCase);
            var hasCompletedStamp = signal?.CompletedAtUtc is not null;
            var autoCompletable = !blockedByUndo
                                  && step.Courses.Count > 0
                                  && CareerCourseMatcher.AllCoursesMatched(step.Courses, certificates);
            var completed = hasCompletedStamp || autoCompletable;

            resolved.Add(new ResolvedStep(
                step.StepKey,
                step.Order,
                completed ? HorizonCareerStepKind.Completed : HorizonCareerStepKind.Open,
                completed ? 100 : matchPercent,
                matched,
                autoCompletable && !hasCompletedStamp));
        }

        var firstOpen = resolved.FirstOrDefault(s => s.Status != HorizonCareerStepKind.Completed);
        if (firstOpen is null)
        {
            return resolved;
        }

        return resolved
            .Select(s => s.StepKey == firstOpen.StepKey
                ? s with { Status = HorizonCareerStepKind.Active }
                : s)
            .ToList();
    }

    public static bool GoalReached(IEnumerable<ResolvedStep> steps)
        => steps.Any() && steps.All(s => s.Status == HorizonCareerStepKind.Completed);
}
