using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate;

/// <summary>
/// Maps career-plan API → dashboard steps / gaps / courses for both
/// <c>CareerDashboard</c> and passport Carrière tab. No new endpoints.
/// </summary>
public static class CareerPlanViewBuilder
{
    public enum ShellKind
    {
        Done,
        Current,
        Future,
        Goal
    }

    public sealed record GapLine(string Text, bool Met);

    public sealed record ShellStep(
        string Id,
        int Order,
        string Title,
        ShellKind Kind,
        CareerStepStatus Status,
        bool IsGoal);

    public sealed record PassportCareerView(
        bool HasPlan,
        string DreamRoleTitle,
        IReadOnlyList<ShellStep> Shells,
        CareerPathDashboardStep? FocusStep,
        IReadOnlyList<GapLine> Gaps,
        IReadOnlyList<string> CourseSearchKeys,
        string? StepBandLabelKey,
        int StepMatchPercent,
        string VacanciesHref,
        bool GoalReached);

    public static CareerDashboardModel FromApi(
        CareerPathPlanApiModel plan,
        IReadOnlyList<CareerDreamOption> dreamOptions)
    {
        return new CareerDashboardModel
        {
            DreamRoleId = ResolveSuggestionId(plan.DreamTitle, dreamOptions) ?? "custom",
            DreamRoleTitle = plan.DreamTitle,
            MatchPercent = plan.MatchPercent,
            MatchSummary = plan.MatchSummary,
            GoalReached = plan.GoalReached,
            HasPlan = true,
            DreamOptions = dreamOptions,
            Steps = plan.Steps.Select(MapStep).ToList()
        };
    }

    public static CareerPathDashboardStep MapStep(CareerPathStepApiModel s)
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

    /// <summary>
    /// Builds the passport Carrière compact view from a dashboard model.
    /// Completing steps stays on <c>/carriere</c>.
    /// </summary>
    public static PassportCareerView BuildPassport(CareerDashboardModel? model)
    {
        if (model is null || !model.HasPlan || model.Steps.Count == 0)
        {
            return new PassportCareerView(
                HasPlan: false,
                DreamRoleTitle: model?.DreamRoleTitle ?? "",
                Shells: [],
                FocusStep: null,
                Gaps: [],
                CourseSearchKeys: [],
                StepBandLabelKey: null,
                StepMatchPercent: 0,
                VacanciesHref: "/carriere",
                GoalReached: false);
        }

        var active = model.Steps.FirstOrDefault(s => s.Status == CareerStepStatus.Active)
                     ?? model.Steps.FirstOrDefault(s => s.Status == CareerStepStatus.Open)
                     ?? model.Steps.LastOrDefault();

        var shells = new List<ShellStep>(model.Steps.Count + 1);
        foreach (var step in model.Steps)
        {
            var kind = step.Status switch
            {
                CareerStepStatus.Completed => ShellKind.Done,
                CareerStepStatus.Active => ShellKind.Current,
                _ => ShellKind.Future
            };
            shells.Add(new ShellStep(step.Id, step.Order, step.Title, kind, step.Status, IsGoal: false));
        }

        shells.Add(new ShellStep(
            "goal",
            model.Steps.Count + 1,
            model.DreamRoleTitle,
            model.GoalReached ? ShellKind.Done : ShellKind.Goal,
            model.GoalReached ? CareerStepStatus.Completed : CareerStepStatus.Open,
            IsGoal: true));

        var gaps = BuildGaps(active);
        var courseKeys = BuildCourseKeys(active);
        var pct = active?.StepMatchPercent ?? model.MatchPercent;
        var bandKey = active is null ? null : RoleFitBandRules.LabelKey(pct);
        var href = string.IsNullOrWhiteSpace(active?.ActionHref) ? "/" : active!.ActionHref;

        return new PassportCareerView(
            HasPlan: true,
            DreamRoleTitle: model.DreamRoleTitle,
            Shells: shells,
            FocusStep: active,
            Gaps: gaps,
            CourseSearchKeys: courseKeys,
            StepBandLabelKey: bandKey,
            StepMatchPercent: pct,
            VacanciesHref: href,
            GoalReached: model.GoalReached);
    }

    public static IReadOnlyList<GapLine> BuildGaps(CareerPathDashboardStep? step)
    {
        if (step is null)
        {
            return [];
        }

        var lines = new List<GapLine>();
        foreach (var gap in step.SkillsGap)
        {
            if (!string.IsNullOrWhiteSpace(gap))
            {
                lines.Add(new GapLine(gap.Trim(), Met: false));
            }
        }

        foreach (var req in step.MinRequirements)
        {
            if (!string.IsNullOrWhiteSpace(req))
            {
                lines.Add(new GapLine(req.Trim(), Met: false));
            }
        }

        foreach (var course in step.Courses.Where(c => c.OnProfile))
        {
            if (!string.IsNullOrWhiteSpace(course.Name)
                && !lines.Any(l => string.Equals(l.Text, course.Name, StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add(new GapLine(course.Name.Trim(), Met: true));
            }
        }

        return lines;
    }

    public static IReadOnlyList<string> BuildCourseKeys(CareerPathDashboardStep? step)
    {
        if (step is null)
        {
            return [];
        }

        var keys = new List<string>();
        foreach (var course in step.Courses)
        {
            if (!string.IsNullOrWhiteSpace(course.Name))
            {
                keys.Add(course.Name.Trim());
            }
        }

        foreach (var gap in step.SkillsGap.Take(2))
        {
            if (!string.IsNullOrWhiteSpace(gap))
            {
                keys.Add(gap.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(step.Title))
        {
            keys.Add(step.Title.Trim());
        }

        return keys;
    }

    private static string? ResolveSuggestionId(string raw, IReadOnlyList<CareerDreamOption> options)
    {
        foreach (var option in options)
        {
            if (string.Equals(option.Id, raw, StringComparison.OrdinalIgnoreCase)
                || string.Equals(option.Title, raw, StringComparison.OrdinalIgnoreCase))
            {
                return option.Id;
            }
        }

        return null;
    }
}
