using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Rules;

/// <summary>Serialize / deserialize persisted Horizon plan steps (content only).</summary>
public static class CareerPlanJson
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public sealed record StoredStep(
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

    public static string Serialize(IReadOnlyList<StoredStep> steps)
        => JsonSerializer.Serialize(steps, Json);

    public static IReadOnlyList<StoredStep> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<StoredStep>>(json, Json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<StoredStep> FromGenerated(HorizonCareerPathPlan plan)
    {
        var steps = new List<StoredStep>(plan.Steps.Count);
        foreach (var step in plan.Steps.OrderBy(s => s.Order))
        {
            var title = string.IsNullOrWhiteSpace(step.Title) ? $"Stap {step.Order}" : step.Title.Trim();
            var key = string.IsNullOrWhiteSpace(step.Id)
                ? CareerStepKey.Create(step.Order, title)
                : step.Id.Trim();
            steps.Add(new StoredStep(
                key,
                step.Order,
                title,
                step.Summary ?? "",
                step.SkillsGap.ToList(),
                step.Courses.ToList(),
                step.MinRequirements.ToList(),
                step.YearsExperienceNeeded,
                step.ActionLabel ?? "Verder",
                step.ActionHref ?? "/"));
        }

        return steps;
    }

    /// <summary>Assign stable StepKeys to generated content steps.</summary>
    public static HorizonCareerPathPlan WithStableKeys(HorizonCareerPathPlan plan)
    {
        var steps = plan.Steps
            .OrderBy(s => s.Order)
            .Select((s, index) =>
            {
                var order = s.Order > 0 ? s.Order : index + 1;
                var title = string.IsNullOrWhiteSpace(s.Title) ? $"Stap {order}" : s.Title.Trim();
                var key = CareerStepKey.Create(order, title);
                return s with
                {
                    Id = key,
                    Order = order,
                    Title = title,
                    Status = HorizonCareerStepKind.Open,
                    StepMatchPercent = 0
                };
            })
            .ToList();
        return plan with { Steps = steps };
    }
}
