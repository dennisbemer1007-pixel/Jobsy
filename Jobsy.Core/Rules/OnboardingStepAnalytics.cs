using System.Text.Json;

namespace Jobsy.Core.Rules;

/// <summary>Serialize/parse per-step timestamps on <c>CandidateOnboarding.StepsJson</c>.</summary>
public static class OnboardingStepAnalytics
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public sealed record StepEvent(
        int Step,
        DateTime? StartedAtUtc = null,
        DateTime? CompletedAtUtc = null,
        DateTime? SkippedAtUtc = null);

    public static IReadOnlyList<StepEvent> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<StepEvent>>(json, Json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Serialize(IEnumerable<StepEvent> events)
        => JsonSerializer.Serialize(events.OrderBy(e => e.Step).ToList(), Json);

    public static string MarkStarted(string? json, int step, DateTime utc)
    {
        var list = Parse(json).ToList();
        var existing = list.FirstOrDefault(e => e.Step == step);
        if (existing is null)
        {
            list.Add(new StepEvent(step, StartedAtUtc: utc));
        }
        else if (existing.StartedAtUtc is null)
        {
            list.Remove(existing);
            list.Add(existing with { StartedAtUtc = utc });
        }

        return Serialize(list);
    }

    public static string MarkCompleted(string? json, int step, DateTime utc)
    {
        var list = Parse(json).ToList();
        var existing = list.FirstOrDefault(e => e.Step == step);
        if (existing is null)
        {
            list.Add(new StepEvent(step, StartedAtUtc: utc, CompletedAtUtc: utc));
        }
        else
        {
            list.Remove(existing);
            list.Add(existing with
            {
                StartedAtUtc = existing.StartedAtUtc ?? utc,
                CompletedAtUtc = utc,
                SkippedAtUtc = null
            });
        }

        return Serialize(list);
    }

    public static string MarkSkipped(string? json, int step, DateTime utc)
    {
        var list = Parse(json).ToList();
        var existing = list.FirstOrDefault(e => e.Step == step);
        if (existing is null)
        {
            list.Add(new StepEvent(step, StartedAtUtc: utc, SkippedAtUtc: utc));
        }
        else
        {
            list.Remove(existing);
            list.Add(existing with
            {
                StartedAtUtc = existing.StartedAtUtc ?? utc,
                SkippedAtUtc = utc
            });
        }

        return Serialize(list);
    }
}
