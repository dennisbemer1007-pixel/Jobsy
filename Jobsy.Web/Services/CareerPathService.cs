using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Career-path dashboard mapping. Persistence and status resolution live in the API.
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

    /// <summary>Empty shell used before the first saved plan (dream input only).</summary>
    public CareerDashboardModel EmptyDashboard(string? dreamDraft = null)
        => new()
        {
            DreamRoleId = ResolveSuggestionId(dreamDraft ?? "") ?? "custom",
            DreamRoleTitle = string.IsNullOrWhiteSpace(dreamDraft) ? "" : ClampTitle(dreamDraft),
            HasPlan = false,
            DreamOptions = DreamSuggestions,
            Steps = []
        };

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
        return MapLocalPreview(plan, id ?? "custom");
    }

    public CareerDashboardModel FromApi(CareerPathPlanApiModel plan)
    {
        return new CareerDashboardModel
        {
            DreamRoleId = ResolveSuggestionId(plan.DreamTitle) ?? "custom",
            DreamRoleTitle = plan.DreamTitle,
            MatchPercent = plan.MatchPercent,
            MatchSummary = plan.MatchSummary,
            GoalReached = plan.GoalReached,
            HasPlan = true,
            DreamOptions = DreamSuggestions,
            Steps = plan.Steps.Select(MapStep).ToList()
        };
    }

    private static CareerPathDashboardStep MapStep(CareerPathStepApiModel s)
    {
        var courses = s.CourseStatuses is { Count: > 0 }
            ? s.CourseStatuses.Select(c => new CareerPathCourseStatus { Name = c.Name, OnProfile = c.OnProfile }).ToList()
            : (s.Courses ?? []).Select(c => new CareerPathCourseStatus { Name = c, OnProfile = false }).ToList();

        return new CareerPathDashboardStep
        {
            Id = s.Id,
            Order = s.Order,
            Title = s.Title,
            Status = Enum.TryParse<CareerStepStatus>(s.Status, true, out var st) ? st : CareerStepStatus.Open,
            Summary = s.Summary,
            SkillsGap = s.SkillsGap ?? [],
            Courses = courses,
            MinRequirements = s.MinRequirements ?? [],
            YearsExperienceNeeded = s.YearsExperienceNeeded,
            ActionLabel = s.ActionLabel,
            ActionHref = s.ActionHref,
            StepMatchPercent = s.StepMatchPercent,
            MatchedCourseCount = s.MatchedCourseCount > 0
                ? s.MatchedCourseCount
                : courses.Count(c => c.OnProfile)
        };
    }

    private static CareerDashboardModel MapLocalPreview(HorizonCareerPathPlan plan, string dreamRoleId)
        => new()
        {
            DreamRoleId = dreamRoleId,
            DreamRoleTitle = plan.DreamTitle,
            MatchPercent = plan.MatchPercent,
            MatchSummary = plan.MatchSummary,
            HasPlan = false,
            DreamOptions = DreamSuggestions,
            Steps = plan.Steps.Select(s => new CareerPathDashboardStep
            {
                Id = s.Id,
                Order = s.Order,
                Title = s.Title,
                Status = CareerStepStatus.Open,
                Summary = s.Summary,
                SkillsGap = s.SkillsGap.ToList(),
                Courses = s.Courses.Select(c => new CareerPathCourseStatus { Name = c }).ToList(),
                MinRequirements = s.MinRequirements.ToList(),
                YearsExperienceNeeded = s.YearsExperienceNeeded,
                ActionLabel = s.ActionLabel,
                ActionHref = s.ActionHref,
                StepMatchPercent = 0
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
