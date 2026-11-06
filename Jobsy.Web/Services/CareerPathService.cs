using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Models;
using Jobsy.Web.Services.Careers;

namespace Jobsy.Web.Services;

/// <summary>
/// Career-path dashboard mapping. Persistence and status resolution live in the API.
/// Plan → steps/gaps/courses mapping lives in <see cref="CareerPlanViewBuilder"/>.
/// </summary>
public sealed class CareerPathService
{
    public const string DefaultDreamId = "teamleider-logistiek";
    public const string DefaultDreamTitle = "Teamleider logistiek";

    public IReadOnlyList<CareerDreamOption> GetDreamSuggestions() => [];

    /// <summary>Empty shell used before the first saved plan (dream input only).</summary>
    public CareerDashboardModel EmptyDashboard(string? dreamDraft = null)
        => new()
        {
            DreamRoleId = "custom",
            DreamRoleTitle = string.IsNullOrWhiteSpace(dreamDraft) ? "" : ClampTitle(dreamDraft),
            HasPlan = false,
            DreamOptions = [],
            Steps = []
        };

    public CareerDashboardModel GetDashboard(string? dreamRoleIdOrTitle = null)
    {
        var raw = string.IsNullOrWhiteSpace(dreamRoleIdOrTitle)
            ? DefaultDreamTitle
            : dreamRoleIdOrTitle.Trim();

        var title = ClampTitle(raw);
        var plan = HorizonCareerPathBuilder.BuildLocal(title);
        return MapLocalPreview(plan, "custom");
    }

    public CareerDashboardModel FromApi(CareerPathPlanApiModel plan)
        => CareerPlanViewBuilder.FromApi(plan, GetDreamSuggestions());

    /// <summary>Maps API error codes to localized user messages (never raw API text).</summary>
    public static string ErrorMessage(Func<string, string> localize, CareerApiErrorException error)
    {
        var key = CareerApiErrorException.LocalizationKey(error.Code);
        var text = localize(key);
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, key, StringComparison.Ordinal))
        {
            return localize("Common.Error");
        }

        return text;
    }

    private static CareerDashboardModel MapLocalPreview(HorizonCareerPathPlan plan, string dreamRoleId)
        => new()
        {
            DreamRoleId = dreamRoleId,
            DreamRoleTitle = plan.DreamTitle,
            MatchPercent = plan.MatchPercent,
            MatchSummary = plan.MatchSummary,
            HasPlan = false,
            DreamOptions = [],
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
