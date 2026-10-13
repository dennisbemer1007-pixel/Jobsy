using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Pure carry-over of completed steps when a dream job changes (D3, D9).
/// Only a prefix of matching steps is carried.
/// </summary>
public static class CareerPlanCarryOver
{
    public sealed record OldStep(string StepKey, int Order, string Title, IReadOnlyList<string> Courses);

    public sealed record NewStep(string StepKey, int Order, string Title, IReadOnlyList<string> Courses);

    public sealed record OldProgress(string StepKey, DateTime? CompletedAtUtc, string Source);

    public sealed record CarriedStep(string StepKey, int Order, DateTime CompletedAtUtc);

    public sealed record Result(IReadOnlyList<CarriedStep> Carried, int CarriedCount);

    public static Result Apply(
        IReadOnlyList<NewStep> newSteps,
        IReadOnlyList<OldStep> archivedSteps,
        IReadOnlyList<OldProgress> archivedProgress,
        IEnumerable<CandidateCertificateDto>? certificates)
    {
        var completedKeys = archivedProgress
            .Where(p => p.CompletedAtUtc is not null
                        && CareerStepProgressSources.IsCompletionStamp(p.Source))
            .GroupBy(p => p.StepKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CompletedAtUtc).First(),
                StringComparer.OrdinalIgnoreCase);

        var archivedByNormTitle = archivedSteps
            .Where(s => completedKeys.ContainsKey(s.StepKey))
            .GroupBy(s => CareerStepKey.NormalizeDreamKey(s.Title), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var carried = new List<CarriedStep>();
        foreach (var step in newSteps.OrderBy(s => s.Order))
        {
            var titleKey = CareerStepKey.NormalizeDreamKey(step.Title);
            DateTime? at = null;

            if (archivedByNormTitle.TryGetValue(titleKey, out var oldByTitle)
                && completedKeys.TryGetValue(oldByTitle.StepKey, out var progByTitle))
            {
                at = progByTitle.CompletedAtUtc;
            }
            else if (CareerCourseMatcher.AllCoursesMatched(step.Courses, certificates)
                     && step.Courses.Count > 0)
            {
                // Certificate match: use now as carry timestamp when no old stamp.
                at = DateTime.UtcNow;
            }

            if (at is null)
            {
                break; // prefix only
            }

            carried.Add(new CarriedStep(step.StepKey, step.Order, at.Value));
        }

        return new Result(carried, carried.Count);
    }
}
