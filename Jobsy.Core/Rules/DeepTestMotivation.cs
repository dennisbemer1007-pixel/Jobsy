using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Quiet inline motivation cues for the uitgebreide test (no modals).
/// Returns a stable string key, or null when nothing should show.
/// </summary>
public static class DeepTestMotivation
{
    public const string PartStart = "Deep.Motivation.PartStart";
    public const string Halfway = "Deep.Motivation.Halfway";
    public const string LastPart = "Deep.Motivation.LastPart";
    public const string BeforePause = "Deep.Motivation.BeforePause";

    /// <summary>
    /// Pick at most one cue for the current progress.
    /// <paramref name="partIndex"/> is 1-based (matches <see cref="TestDepthPart.Index"/>).
    /// <paramref name="answered"/> is the count of saved answers (sequential fill assumed).
    /// </summary>
    public static string? ForProgress(AssessmentKind kind, int answered, int total, int partIndex)
    {
        if (total <= 0 || partIndex < 1)
        {
            return null;
        }

        var parts = TestDepthRules.Parts(kind);
        if (parts.Count == 0 || partIndex > parts.Count)
        {
            return null;
        }

        var part = parts[partIndex - 1];
        var before = 0;
        for (var i = 0; i < partIndex - 1; i++)
        {
            before += parts[i].Size;
        }

        // Index of the current question within the part (0 = first of part).
        var indexInPart = Math.Clamp(answered - before, 0, part.Size - 1);
        var half = total / 2;

        // Last question of a part (except the final part): pause hint.
        if (partIndex < parts.Count && indexInPart == part.Size - 1)
        {
            return BeforePause;
        }

        // Exactly halfway through the whole test (first moment at half).
        if (answered == half && half > 0)
        {
            return Halfway;
        }

        // First question of the last part.
        if (partIndex == parts.Count && indexInPart == 0)
        {
            return LastPart;
        }

        // First question of a part.
        if (indexInPart == 0)
        {
            return PartStart;
        }

        return null;
    }

    /// <summary>True when the just-completed answer finished this part (pause after parts 1–4).</summary>
    public static bool IsPausePoint(AssessmentKind kind, int answeredAfter)
    {
        var parts = TestDepthRules.Parts(kind);
        var running = 0;
        for (var i = 0; i < parts.Count - 1; i++)
        {
            running += parts[i].Size;
            if (answeredAfter == running)
            {
                return true;
            }
        }

        return false;
    }

    public static int PartIndexForGlobalIndex(AssessmentKind kind, int globalIndex)
    {
        var parts = TestDepthRules.Parts(kind);
        if (parts.Count == 0) return 1;
        var running = 0;
        foreach (var p in parts)
        {
            if (globalIndex < running + p.Size)
            {
                return p.Index;
            }

            running += p.Size;
        }

        return parts[^1].Index;
    }

    public static string PartDisplayName(AssessmentKind kind, TestDepthPart part)
    {
        var labels = part.Domains
            .Select(DeepAnalysisQuestionHelp.DomainLabel)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (kind is AssessmentKind.Competence or AssessmentKind.Values)
        {
            return labels.FirstOrDefault() ?? $"Deel {part.Index}";
        }

        if (labels.Count is 1 or 2)
        {
            return $"Deel {part.Index}: {string.Join(" en ", labels)}";
        }

        return $"Deel {part.Index} van 5";
    }
}
