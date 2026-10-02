using Jobsy.Core.Careers;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Career;
using Jobsy.Web.Components.Candidate.Journey;
using Jobsy.Web.Models;
using Jobsy.Web.Services.Careers;

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

    /// <summary>
    /// Everything <c>/carriere</c> (02) renders. <paramref name="nowLabel"/> is the candidate's
    /// current work when the paspoort knows it; pass an empty string to get "waar je nu bent"
    /// handling in the view (never invent a job title).
    /// </summary>
    public static CareerPlanViewModel BuildPage(
        CareerPathPlanApiModel? plan,
        string uiLanguage,
        string nowLabel = "")
    {
        if (plan is null || plan.Steps.Count == 0)
        {
            return CareerPlanViewModel.Empty;
        }

        var steps = plan.Steps
            .OrderBy(s => s.Order)
            .Select(MapStep)
            .Select(s => new CareerPlanStepView(
                s.Id,
                s.Order,
                s.Title,
                ShortTitle(s.Title),
                s.Status,
                s.SkillsGap.Count + s.MinRequirements.Count,
                StepBandLabelKey(s),
                s.Courses.Count,
                s.Summary))
            .ToList();

        var completed = steps.Count(s => s.Status == CareerStepStatus.Completed);
        var current = steps.FirstOrDefault(s => s.Status == CareerStepStatus.Active);
        var entry = CareerDreamCatalog.FindByTitleOrAlias(plan.DreamTitle);

        var stones = new List<CareerStoneModel>(steps.Count + 2)
        {
            new(nowLabel, ClimbStoneState.Done, "Career.Rail.Now", null)
        };

        foreach (var step in steps)
        {
            var state = step.Status switch
            {
                CareerStepStatus.Completed => ClimbStoneState.Done,
                CareerStepStatus.Active => ClimbStoneState.Now,
                _ => ClimbStoneState.Todo
            };
            var stateKey = state switch
            {
                ClimbStoneState.Done => "Career.Rail.NewShell",
                ClimbStoneState.Now => "Career.Rail.GrowingNow",
                _ => null
            };
            stones.Add(new CareerStoneModel(step.ShortTitle, state, stateKey, step.Order));
        }

        stones.Add(new CareerStoneModel(
            plan.DreamTitle,
            ClimbStoneState.Dream,
            "Career.Rail.Goal",
            null));

        var have = BuildAlreadyHave(plan, steps);

        return new CareerPlanViewModel
        {
            HasPlan = true,
            GoalReached = plan.GoalReached,
            DreamTitle = plan.DreamTitle,
            DreamLevel = entry?.Level ?? "",
            DreamBandLabelKey = FitBandLabelKey(plan.DreamFitBand),
            FromAi = plan.FromAi,
            PlanLanguage = string.IsNullOrWhiteSpace(plan.PlanLanguage) ? "nl" : plan.PlanLanguage,
            LanguageDiffers = !string.IsNullOrWhiteSpace(plan.PlanLanguage)
                              && !string.IsNullOrWhiteSpace(uiLanguage)
                              && !string.Equals(plan.PlanLanguage, uiLanguage, StringComparison.OrdinalIgnoreCase),
            CarriedOverCount = plan.CarriedOverCount,
            Steps = steps,
            CurrentStep = current,
            CompletedSteps = completed,
            CurrentStepNumber = plan.GoalReached ? 0 : current?.Order ?? 0,
            Stones = stones,
            CurrentStoneIndex = plan.GoalReached ? steps.Count + 1 : completed,
            PlatesShed = Math.Min(10, 4 + 2 * completed),
            LobsterSizeDesktop = Math.Min(190, 118 + 14 * completed),
            LobsterSizeMobile = Math.Min(100, 62 + 8 * completed),
            AlreadyHave = have,
            ProofCourseCount = plan.Steps
                .SelectMany(s => s.CourseStatuses)
                .Count(c => c.OnProfile),
            ProofHasExperience = !string.IsNullOrWhiteSpace(nowLabel)
        };
    }

    /// <summary>Short title for the stepper/stones: the first clause, capped on a word boundary.</summary>
    public static string ShortTitle(string title)
    {
        var text = (title ?? "").Trim();
        if (text.Length == 0)
        {
            return "";
        }

        var cut = text.IndexOfAny([':', '·', '–', '—', '(', ',', ';']);
        if (cut > 2)
        {
            text = text[..cut].Trim();
        }

        if (text.Length <= 22)
        {
            return text;
        }

        var space = text.LastIndexOf(' ', Math.Min(21, text.Length - 1));
        return space > 6 ? text[..space] : text[..22];
    }

    private static IReadOnlyList<string> BuildAlreadyHave(
        CareerPathPlanApiModel plan,
        IReadOnlyList<CareerPlanStepView> steps)
    {
        var have = new List<string>(3);

        foreach (var step in steps.Where(s => s.Status == CareerStepStatus.Completed))
        {
            if (have.Count == 3)
            {
                return have;
            }

            have.Add(step.ShortTitle);
        }

        foreach (var course in plan.Steps
            .SelectMany(s => s.CourseStatuses)
            .Where(c => c.OnProfile && !string.IsNullOrWhiteSpace(c.Name)))
        {
            if (have.Count == 3)
            {
                return have;
            }

            if (!have.Any(h => string.Equals(h, course.Name, StringComparison.OrdinalIgnoreCase)))
            {
                have.Add(course.Name.Trim());
            }
        }

        return have;
    }

    public static CareerDashboardModel FromApi(
        CareerPathPlanApiModel plan,
        IReadOnlyList<CareerDreamOption> dreamOptions)
    {
        return new CareerDashboardModel
        {
            DreamRoleId = ResolveSuggestionId(plan.DreamTitle, dreamOptions) ?? "custom",
            DreamRoleTitle = plan.DreamTitle,
            MatchPercent = plan.MatchPercent,
            MatchSummary = plan.MatchSummary ?? "",
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

#pragma warning disable CS0618
        var legacyHref = s.ActionHref;
#pragma warning restore CS0618

        return new CareerPathDashboardStep
        {
            Id = s.Id,
            Order = s.Order,
            Title = s.Title,
            Status = Enum.TryParse<CareerStepStatus>(s.Status, true, out var st) ? st : CareerStepStatus.Open,
            HeldBack = s.HeldBack,
            StepFitBand = s.StepFitBand ?? "",
            Summary = s.Summary,
            SkillsGap = s.SkillsGap ?? [],
            Courses = courses,
            MinRequirements = s.MinRequirements ?? [],
            YearsExperienceNeeded = s.YearsExperienceNeeded,
            ActionKinds = s.ActionKinds ?? [],
#pragma warning disable CS0618
            ActionLabel = s.ActionLabel,
            ActionHref = CareerStepActionLinks.PrimaryHref(s.ActionKinds, s.Title, legacyHref),
            StepMatchPercent = s.StepMatchPercent,
#pragma warning restore CS0618
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
                     ?? (model.Steps.Count > 0 ? model.Steps[^1] : null);

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
#pragma warning disable CS0618
        var pct = active?.StepMatchPercent ?? model.MatchPercent;
#pragma warning restore CS0618
        var bandKey = StepBandLabelKey(active);
        var href = VacanciesHref(active);

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

    public static string? StepBandLabelKey(CareerPathDashboardStep? step)
    {
        if (step is null)
        {
            return null;
        }

        var fromBand = FitBandLabelKey(step.StepFitBand);
        if (fromBand is not null)
        {
            return fromBand;
        }

#pragma warning disable CS0618
        if (step.StepMatchPercent > 0)
        {
            return RoleFitBandRules.LabelKey(step.StepMatchPercent);
        }
#pragma warning restore CS0618

        return null;
    }

    public static string? FitBandLabelKey(string? apiBand)
    {
        if (string.IsNullOrWhiteSpace(apiBand))
        {
            return null;
        }

        var band = apiBand.Trim() switch
        {
            "Good" => CareerFitBandRules.CareerFitBand.Good,
            "Fair" => CareerFitBandRules.CareerFitBand.Fair,
            "NotYet" => CareerFitBandRules.CareerFitBand.NotYet,
            _ => CareerFitBandRules.CareerFitBand.Unknown
        };

        return CareerFitBandRules.LabelKey(band);
    }

    public static string VacanciesHref(CareerPathDashboardStep? active)
    {
        if (active is null)
        {
            return "/carriere";
        }

        if (active.ActionKinds.Any(k => string.Equals(k, "Vacancies", StringComparison.OrdinalIgnoreCase)))
        {
            return CareerStepActionLinks.VacanciesSearchHref(active.Title);
        }

        if (!string.IsNullOrWhiteSpace(active.ActionHref))
        {
            return active.ActionHref;
        }

        return "/";
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
