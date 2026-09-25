using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Career-path dashboard helpers: dream suggestions + mapping of API/local plans.
/// Persistence and status resolution live on the API (ICandidateCareerPlanService).
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

    /// <summary>Local preview used only as offline fallback while generating.</summary>
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
        var content = plan.Steps.Select(s => new CareerPlanStepContent(
            s.Id,
            s.Order,
            s.Title,
            s.Summary,
            s.SkillsGap,
            s.Courses,
            s.MinRequirements,
            s.YearsExperienceNeeded,
            s.ActionLabel,
            s.ActionHref)).ToList();
        var view = CareerStepStatusResolver.Resolve(
            Guid.Empty,
            plan.DreamTitle,
            CareerStepKey.ForDream(plan.DreamTitle),
            plan.MatchPercent,
            plan.MatchSummary,
            content,
            [],
            []);
        return MapView(view, id ?? "custom");
    }

    public CareerDashboardModel FromApi(CareerPathPlanApiModel plan)
        => MapView(
            new CareerPlanView(
                plan.PlanId ?? Guid.Empty,
                plan.DreamTitle,
                CareerStepKey.ForDream(plan.DreamTitle),
                plan.MatchPercent,
                plan.MatchSummary,
                plan.GoalReached,
                plan.Steps.Select(s => new CareerPlanStepView(
                    s.Id,
                    s.Order,
                    s.Title,
                    Enum.TryParse<HorizonCareerStepKind>(s.Status, true, out var kind) ? kind : HorizonCareerStepKind.Open,
                    s.Summary,
                    s.SkillsGap,
                    (s.Courses ?? []).Select(c => new CareerPlanCourseView(c.Name, c.OnProfile)).ToList(),
                    s.MinRequirements,
                    s.YearsExperienceNeeded,
                    s.ActionLabel,
                    s.ActionHref,
                    s.StepMatchPercent,
                    s.CoursesOnProfile,
                    s.CoursesTotal > 0 ? s.CoursesTotal : (s.Courses?.Count ?? 0))).ToList()),
            ResolveSuggestionId(plan.DreamTitle) ?? "custom");

    private static CareerDashboardModel MapView(CareerPlanView plan, string dreamRoleId)
        => new()
        {
            PlanId = plan.PlanId == Guid.Empty ? null : plan.PlanId,
            DreamRoleId = dreamRoleId,
            DreamRoleTitle = plan.DreamTitle,
            MatchPercent = plan.MatchPercent,
            MatchSummary = plan.MatchSummary,
            GoalReached = plan.GoalReached,
            DreamOptions = DreamSuggestions,
            Steps = plan.Steps.Select(s => new CareerPathDashboardStep
            {
                Id = s.StepKey,
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
                Courses = s.Courses.Select(c => new CareerPathDashboardCourse
                {
                    Name = c.Name,
                    OnProfile = c.OnProfile
                }).ToList(),
                MinRequirements = s.MinRequirements.ToList(),
                YearsExperienceNeeded = s.YearsExperienceNeeded,
                ActionLabel = s.ActionLabel,
                ActionHref = s.ActionHref,
                StepMatchPercent = s.StepMatchPercent,
                CoursesOnProfile = s.CoursesOnProfile,
                CoursesTotal = s.CoursesTotal
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
