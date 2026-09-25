using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Career-path dashboard: free-text horizon → deep steps (local builder; API/OpenAI when available).
/// No hardcoded "huidige rol".
/// </summary>
public sealed class CareerPathService
{
    public const string DefaultDreamId = "teamleider-logistiek";
    public const string DefaultDreamTitle = "Teamleider logistiek";

    private static readonly CareerDreamOption[] DreamSuggestions =
    [
        new() { Id = "teamleider-logistiek", Title = "Teamleider logistiek" },
        new() { Id = "filiaalmanager", Title = "Filiaalmanager" },
        new() { Id = "hr-adviseur", Title = "HR-adviseur" },
        new() { Id = "assistent-manager", Title = "Assistent-manager" },
        new() { Id = "planner", Title = "Planner" },
        new() { Id = "coach", Title = "Teamcoach" }
    ];

    public IReadOnlyList<CareerDreamOption> GetDreamSuggestions() => DreamSuggestions;

    public CareerDashboardModel GetDashboard(string? dreamRoleIdOrTitle = null)
    {
        var raw = string.IsNullOrWhiteSpace(dreamRoleIdOrTitle)
            ? DefaultDreamTitle
            : dreamRoleIdOrTitle.Trim();

        var id = ResolveSuggestionId(raw);
        var title = id is not null
            ? DreamSuggestions.First(o => o.Id == id).Title
            : ClampTitle(raw);
        var plan = HorizonCareerPathBuilder.BuildLocal(title);
        return Map(plan, id ?? "custom");
    }

    public CareerDashboardModel FromApi(CareerPathPlanApiModel plan)
    {
        var mapped = new HorizonCareerPathPlan(
            plan.DreamTitle,
            plan.MatchPercent,
            plan.MatchSummary,
            plan.Steps.Select(s => new HorizonCareerPathStep(
                s.Id,
                s.Order,
                s.Title,
                Enum.TryParse<HorizonCareerStepKind>(s.Status, true, out var kind) ? kind : HorizonCareerStepKind.Open,
                s.Summary,
                s.SkillsGap,
                s.Courses,
                s.MinRequirements,
                s.YearsExperienceNeeded,
                s.ActionLabel,
                s.ActionHref,
                s.StepMatchPercent)).ToList());
        return Map(mapped, ResolveSuggestionId(plan.DreamTitle) ?? "custom");
    }

    private static CareerDashboardModel Map(HorizonCareerPathPlan plan, string dreamRoleId)
        => new()
        {
            CurrentRoleTitle = "",
            DreamRoleId = dreamRoleId,
            DreamRoleTitle = plan.DreamTitle,
            MatchPercent = plan.MatchPercent,
            MatchSummary = plan.MatchSummary,
            DreamOptions = DreamSuggestions,
            Steps = plan.Steps.Select(s => new CareerPathDashboardStep
            {
                Id = s.Id,
                Order = s.Order,
                Title = s.Title,
                Status = s.Status switch
                {
                    HorizonCareerStepKind.Completed => CareerStepStatus.Completed,
                    HorizonCareerStepKind.Active => CareerStepStatus.Active,
                    _ => CareerStepStatus.Open
                },
                Summary = s.Summary,
                SkillsGap = s.SkillsGap.ToList(),
                Courses = s.Courses.ToList(),
                MinRequirements = s.MinRequirements.ToList(),
                YearsExperienceNeeded = s.YearsExperienceNeeded,
                ActionLabel = s.ActionLabel,
                ActionHref = s.ActionHref,
                StepMatchPercent = s.StepMatchPercent
            }).ToList()
        };

    private static string? ResolveSuggestionId(string raw)
    {
        foreach (var option in DreamSuggestions)
        {
            if (string.Equals(option.Id, raw, StringComparison.OrdinalIgnoreCase)
                || string.Equals(option.Title, raw, StringComparison.OrdinalIgnoreCase))
            {
                return option.Id;
            }
        }

        return null;
    }

    private static string ClampTitle(string title)
    {
        var t = title.Trim();
        if (t.Length > 80)
        {
            t = t[..80].Trim();
        }

        return string.IsNullOrWhiteSpace(t) ? DefaultDreamTitle : t;
    }
}
