using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Single source of truth for career-step Completed / Active / Open (prefix rule, D9).
/// </summary>
public static class CareerStepStatusResolver
{
    public sealed record ProgressSignal(
        string StepKey,
        DateTime? CompletedAtUtc,
        string Source,
        string? UndoFingerprint = null);

    public sealed record StepInput(
        string StepKey,
        int Order,
        IReadOnlyList<string> Courses);

    public sealed record ResolvedStep(
        string StepKey,
        int Order,
        HorizonCareerStepKind Status,
        int MatchedCourseCount,
        bool AutoCompletable,
        bool HeldBack = false);

    public static IReadOnlyList<ResolvedStep> Resolve(
        IReadOnlyList<StepInput> steps,
        IReadOnlyList<ProgressSignal> progress,
        IEnumerable<CandidateCertificateDto>? certificates)
    {
        var certList = certificates?.ToList() ?? [];
        var byKey = progress
            .GroupBy(p => p.StepKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CompletedAtUtc ?? DateTime.MinValue).First(),
                StringComparer.OrdinalIgnoreCase);

        var ordered = steps.OrderBy(s => s.Order).ToList();
        var stampCompleted = new bool[ordered.Count];
        var autoCompletable = new bool[ordered.Count];
        var matched = new int[ordered.Count];
        var heldBack = new bool[ordered.Count];

        for (var i = 0; i < ordered.Count; i++)
        {
            var step = ordered[i];
            byKey.TryGetValue(step.StepKey, out var signal);
            matched[i] = CareerCourseMatcher.MatchedCount(step.Courses, certList);

            var undoBlocks = IsUndoBlocking(signal, step.Courses, certList);
            var hasStamp = signal?.CompletedAtUtc is not null
                           && CareerStepProgressSources.IsCompletionStamp(signal.Source);
            stampCompleted[i] = hasStamp;
            autoCompletable[i] = !undoBlocks
                                 && !hasStamp
                                 && step.Courses.Count > 0
                                 && CareerCourseMatcher.AllCoursesMatched(step.Courses, certList);
        }

        // Prefix rule: a step is Completed only when it qualifies and every earlier step is Completed.
        var prefixCompleted = new bool[ordered.Count];
        for (var i = 0; i < ordered.Count; i++)
        {
            var qualifies = stampCompleted[i] || autoCompletable[i];
            var earlierOk = i == 0 || prefixCompleted[i - 1];
            if (qualifies && earlierOk)
            {
                prefixCompleted[i] = true;
            }
            else if (stampCompleted[i] && !earlierOk)
            {
                heldBack[i] = true;
            }
        }

        var resolved = new List<ResolvedStep>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var status = prefixCompleted[i]
                ? HorizonCareerStepKind.Completed
                : HorizonCareerStepKind.Open;
            resolved.Add(new ResolvedStep(
                ordered[i].StepKey,
                ordered[i].Order,
                status,
                matched[i],
                autoCompletable[i] && !prefixCompleted[i],
                heldBack[i]));
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

    public static string FingerprintCertificates(
        IReadOnlyList<string> courses,
        IEnumerable<CandidateCertificateDto>? certificates)
    {
        var names = (certificates ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Where(c => courses.Any(course => CareerCourseMatcher.IsMatch(course, c.Name!)))
            .Select(c => CareerCourseMatcher.Normalize(c.Name!))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        if (names.Count == 0)
        {
            return "";
        }

        var joined = string.Join('|', names);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static bool IsUndoBlocking(
        ProgressSignal? signal,
        IReadOnlyList<string> courses,
        IReadOnlyList<CandidateCertificateDto> certificates)
    {
        if (signal is null
            || !string.Equals(signal.Source, CareerStepProgressSources.ManualUndo, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrEmpty(signal.UndoFingerprint))
        {
            // Legacy undo without fingerprint: keep old forever-block behaviour.
            return true;
        }

        var current = FingerprintCertificates(courses, certificates);
        return string.Equals(current, signal.UndoFingerprint, StringComparison.Ordinal);
    }
}
