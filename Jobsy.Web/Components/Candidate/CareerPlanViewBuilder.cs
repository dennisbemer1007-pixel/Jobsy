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
        bool GoalReached)
    {
        public IReadOnlyList<string> CourseNames { get; init; } = [];
    }

    /// <summary>
    /// Everything <c>/carriere</c> (02) renders. <paramref name="nowLabel"/> is the candidate's
    /// current work when the paspoort knows it; pass an empty string to get "waar je nu bent"
    /// handling in the view (never invent a job title).
    /// </summary>
    public static CareerPlanViewModel BuildPage(
        CareerPathPlanApiModel? plan,
        string uiLanguage,
        string nowLabel = "",
        string evidence = "")
    {
        if (plan is null || (plan.Steps.Count == 0 && string.IsNullOrWhiteSpace(plan.DreamTitle)))
        {
            return CareerPlanViewModel.Empty;
        }

        if (plan.Steps.Count == 0)
        {
            var saved = CareerDreamCatalog.FindByTitleOrAlias(plan.DreamTitle);
            return new CareerPlanViewModel
            {
                HasPlan = false,
                DreamTitle = plan.DreamTitle.Trim(),
                DreamLevel = saved?.Level ?? ""
            };
        }

        var mapped = plan.Steps.OrderBy(s => s.Order).Select(MapStep).ToList();
        OverlayEvidence(mapped, evidence);
        var steps = mapped
            .Select(s => new CareerPlanStepView(
                s.Id,
                s.Order,
                s.Title,
                ShortTitle(s.Title),
                s.Status,
                UnmetGapCount(s, evidence),
                StepBandLabelKey(s),
                s.Courses.Count,
                s.Summary,
                UnmetGapNames(s, evidence)))
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

        var evidenceGoal = steps.Count > 0 && steps.All(s => s.Status == CareerStepStatus.Completed);
        return new CareerPlanViewModel
        {
            HasPlan = true,
            GoalReached = plan.GoalReached || evidenceGoal,
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
            CurrentStepNumber = plan.GoalReached || evidenceGoal ? 0 : current?.Order ?? 0,
            Stones = stones,
            CurrentStoneIndex = plan.GoalReached || evidenceGoal ? steps.Count + 1 : completed,
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

    /// <summary>
    /// One step, fully explained (03 §1–§4). Returns null for an unknown order so the
    /// page falls back to the overview. <paramref name="order"/> is the 1-based step order.
    /// </summary>
    public static CareerStepDetailView? BuildStep(
        CareerPathPlanApiModel? plan,
        int order,
        string evidence = "",
        int experienceYears = 0)
    {
        if (plan is null || plan.Steps.Count == 0)
        {
            return null;
        }

        var ordered = plan.Steps.OrderBy(s => s.Order).Select(MapStep).ToList();
        OverlayEvidence(ordered, evidence);
        var index = ordered.FindIndex(s => s.Order == order);
        if (index < 0)
        {
            return null;
        }

        var step = ordered[index];
        var completed = ordered.Where(s => s.Status == CareerStepStatus.Completed).ToList();
        var current = ordered.FirstOrDefault(s => s.Status == CareerStepStatus.Active);
        var lastCompletedOrder = completed.Count == 0 ? 0 : completed.Max(s => s.Order);

        var missing = new List<CareerStepGapLine>();
        var present = new List<CareerStepGapLine>();
        foreach (var line in BuildGaps(step, evidence))
        {
            (line.Met ? present : missing).Add(new CareerStepGapLine(line.Text, line.Met));
        }

        var next = index + 1 < ordered.Count ? ordered[index + 1] : null;

        return new CareerStepDetailView
        {
            Id = step.Id,
            Order = step.Order,
            TotalSteps = ordered.Count,
            Title = TitleWithoutLevel(step.Title),
            ShortTitle = ShortTitle(step.Title),
            Level = LevelFromTitle(step.Title),
            Lead = LeadSentences(step.Summary),
            Status = step.Status,
            CurrentStepNumber = plan.GoalReached || ordered.All(s => s.Status == CareerStepStatus.Completed)
                ? 0
                : current?.Order ?? 0,
            CanUndo = step.Status == CareerStepStatus.Completed && step.Order == lastCompletedOrder,
            Missing = missing,
            Present = present,
            Years = Math.Max(0, step.YearsExperienceNeeded),
            YearsMet = step.YearsExperienceNeeded > 0 && experienceYears >= step.YearsExperienceNeeded,
            BandLabelKey = StepBandLabelKey(step),
            Band = NormalizeBand(step.StepFitBand),
            FirstGap = missing.Count > 0 ? missing[0].Text : "",
            CourseNames = step.Courses
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CourseSearchKeys = BuildCourseKeys(step),
            ProofName = ProofNameFor(step),
            VacanciesHref = CareerStepActionLinks.VacanciesSearchHref(ShortTitle(step.Title)),
            NextStep = next is null
                ? null
                : new CareerPlanStepView(
                    next.Id,
                    next.Order,
                    TitleWithoutLevel(next.Title),
                    ShortTitle(next.Title),
                    next.Status,
                    next.SkillsGap.Count + next.MinRequirements.Count,
                    StepBandLabelKey(next),
                    next.Courses.Count,
                    next.Summary,
                    GapNames(next))
        };
    }

    /// <summary>
    /// What the growth moment may claim (03 §5): only proof that really landed and only
    /// claws that were missing before and are covered now.
    /// </summary>
    public static CareerStepDoneView BuildDone(
        CareerStepDetailView? before,
        CareerPathPlanApiModel? after,
        int order)
    {
        if (before is null)
        {
            return new CareerStepDoneView();
        }

        var step = after?.Steps.FirstOrDefault(s => s.Order == order);
        var afterStep = step is null ? null : MapStep(step);

        var proof = afterStep is null
            ? ""
            : afterStep.Courses.FirstOrDefault(c => c.OnProfile && !string.IsNullOrWhiteSpace(c.Name))?.Name.Trim() ?? "";

        List<string> coveredNow = afterStep is null
            ? []
            : BuildGaps(afterStep).Where(l => l.Met).Select(l => l.Text).ToList();

        var grown = before.Missing
            .Select(m => m.Text)
            .Where(text => coveredNow.Any(c => string.Equals(c, text, StringComparison.OrdinalIgnoreCase)))
            .Take(2)
            .ToList();

        return new CareerStepDoneView
        {
            ProofName = proof,
            GrownClaws = grown
        };
    }

    /// <summary>Drops a trailing level in brackets: "Basisdiploma (MBO 2)" → "Basisdiploma".</summary>
    public static string TitleWithoutLevel(string title)
    {
        var text = (title ?? "").Trim();
        if (!text.EndsWith(')'))
        {
            return text;
        }

        var open = text.LastIndexOf('(');
        return open > 2 ? text[..open].TrimEnd() : text;
    }

    /// <summary>The level the catalog put in brackets, or an empty string.</summary>
    public static string LevelFromTitle(string title)
    {
        var text = (title ?? "").Trim();
        if (!text.EndsWith(')'))
        {
            return "";
        }

        var open = text.LastIndexOf('(');
        return open > 2 ? text[(open + 1)..^1].Trim() : "";
    }

    /// <summary>At most two sentences, cut at a sentence end (03 §1).</summary>
    public static string LeadSentences(string? summary)
    {
        var text = (summary ?? "").Trim();
        if (text.Length == 0)
        {
            return "";
        }

        var ends = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '.' or '!' or '?')
            {
                ends++;
                if (ends == 2)
                {
                    return text[..(i + 1)];
                }
            }
        }

        return text;
    }

    private static string NormalizeBand(string? apiBand) => (apiBand ?? "").Trim() switch
    {
        "Good" => "Good",
        "Fair" => "Fair",
        "NotYet" => "NotYet",
        _ => "Unknown"
    };

    /// <summary>First missing diploma/course name to prefill the Bewijzen form (Dependency F).</summary>
    private static string ProofNameFor(CareerPathDashboardStep step)
    {
        var course = step.Courses.FirstOrDefault(c => !c.OnProfile && !string.IsNullOrWhiteSpace(c.Name));
        if (course is not null)
        {
            return course.Name.Trim();
        }

        var requirement = step.MinRequirements.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
        return (requirement ?? TitleWithoutLevel(step.Title)).Trim();
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

        if (text.Length <= 36)
        {
            return text;
        }

        var space = text.LastIndexOf(' ', Math.Min(35, text.Length - 1));
        return space > 6 ? text[..space] : text[..36];
    }

    /// <summary>Gap names for the overview "Meer" subline (skills + min requirements).</summary>
    private static IReadOnlyList<string> GapNames(CareerPathDashboardStep step)
    {
        var names = new List<string>(step.SkillsGap.Count + step.MinRequirements.Count);
        foreach (var gap in step.SkillsGap)
        {
            if (!string.IsNullOrWhiteSpace(gap))
            {
                names.Add(gap.Trim());
            }
        }

        foreach (var req in step.MinRequirements)
        {
            if (!string.IsNullOrWhiteSpace(req))
            {
                names.Add(req.Trim());
            }
        }

        return names;
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

        var legacyHref = s.ActionHref;

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
            ActionLabel = s.ActionLabel,
            ActionHref = CareerStepActionLinks.PrimaryHref(s.ActionKinds, s.Title, legacyHref),
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
    public static PassportCareerView BuildPassport(CareerDashboardModel? model, string evidence = "")
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

        var editable = model.Steps.ToList();
        OverlayEvidence(editable, evidence);
        var goalReached = model.GoalReached || (editable.Count > 0 && editable.All(s => s.Status == CareerStepStatus.Completed));
        var active = editable.FirstOrDefault(s => s.Status == CareerStepStatus.Active)
                     ?? editable.FirstOrDefault(s => s.Status == CareerStepStatus.Open)
                     ?? (editable.Count > 0 ? editable[^1] : null);

        var shells = new List<ShellStep>(editable.Count + 1);
        foreach (var step in editable)
        {
            var kind = step.Status switch
            {
                CareerStepStatus.Completed => ShellKind.Done,
                CareerStepStatus.Active => ShellKind.Current,
                _ => ShellKind.Future
            };
            shells.Add(new ShellStep(step.Id, step.Order, ShortTitle(step.Title), kind, step.Status, IsGoal: false));
        }

        shells.Add(new ShellStep(
            "goal",
            editable.Count + 1,
            model.DreamRoleTitle,
            goalReached ? ShellKind.Done : ShellKind.Goal,
            goalReached ? CareerStepStatus.Completed : CareerStepStatus.Open,
            IsGoal: true));

        var gaps = BuildGaps(active, evidence);
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
            GoalReached: goalReached)
        {
            CourseNames = (active?.Courses ?? [])
                .Where(c => !c.OnProfile && !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
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

    public static IReadOnlyList<GapLine> BuildGaps(CareerPathDashboardStep? step, string evidence = "")
    {
        if (step is null)
        {
            return [];
        }

        var lines = new List<GapLine>();
        foreach (var gap in step.SkillsGap)
        {
            if (IsRealGap(gap))
            {
                lines.Add(new GapLine(gap.Trim(), Met: GapMatchesEvidence(gap, evidence)));
            }
        }

        foreach (var req in step.MinRequirements)
        {
            if (IsRealGap(req))
            {
                lines.Add(new GapLine(req.Trim(), Met: GapMatchesEvidence(req, evidence)));
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

    private static bool IsRealGap(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var folded = text.Trim();
        return !folded.Equals("Geen specifieke vereisten", StringComparison.OrdinalIgnoreCase)
               && !folded.Equals("Geen specifieke eisen", StringComparison.OrdinalIgnoreCase)
               && !folded.Equals("No specific requirements", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Marks a prefix of steps completed when every real gap is already in the profile.
    /// Later stamps stay open until the prefix reaches them (same rule as certificates).
    /// </summary>
    public static void OverlayEvidence(IList<CareerPathDashboardStep> steps, string evidence)
    {
        if (steps.Count == 0 || string.IsNullOrWhiteSpace(evidence))
        {
            return;
        }

        var ordered = steps.OrderBy(s => s.Order).ToList();
        var qualifies = new bool[ordered.Count];
        for (var i = 0; i < ordered.Count; i++)
        {
            qualifies[i] = ordered[i].Status == CareerStepStatus.Completed
                           || GapsAllMet(ordered[i], evidence);
        }

        var prefix = true;
        var assignedActive = false;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (prefix && qualifies[i])
            {
                ordered[i].Status = CareerStepStatus.Completed;
                continue;
            }

            prefix = false;
            if (!assignedActive)
            {
                ordered[i].Status = CareerStepStatus.Active;
                assignedActive = true;
            }
            else if (ordered[i].Status == CareerStepStatus.Completed)
            {
                ordered[i].Status = CareerStepStatus.Open;
                ordered[i].HeldBack = true;
            }
            else
            {
                ordered[i].Status = CareerStepStatus.Open;
            }
        }
    }

    public static string EvidenceFrom(CandidatePreferences? prefs)
    {
        if (prefs is null)
        {
            return "";
        }

        var bits = new List<string>();
        bits.AddRange(prefs.Employers.Select(e => $"{e.Role} {e.EmployerName}"));
        bits.AddRange(prefs.Roles);
        bits.AddRange(prefs.Educations);
        if (!string.IsNullOrWhiteSpace(prefs.EducationDirection))
        {
            bits.Add(prefs.EducationDirection);
        }

        bits.AddRange(prefs.Certificates.Select(c => c.Name));
        return string.Join(' ', bits.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public static int ExperienceYears(IEnumerable<CandidateEmployerHistory>? employers)
    {
        var total = 0;
        foreach (var job in employers ?? [])
        {
            if (job.Years is int years && years > 0)
            {
                total += years;
                continue;
            }

            if (MonthsBetween(job.StartMonth, job.EndMonth) is int months && months > 0)
            {
                total += Math.Max(1, months / 12);
            }
        }

        return total;
    }

    private static int UnmetGapCount(CareerPathDashboardStep step, string evidence)
        => UnmetGapNames(step, evidence).Count;

    private static IReadOnlyList<string> UnmetGapNames(CareerPathDashboardStep step, string evidence)
    {
        var names = new List<string>();
        foreach (var gap in step.SkillsGap.Concat(step.MinRequirements))
        {
            if (!IsRealGap(gap) || GapMatchesEvidence(gap, evidence))
            {
                continue;
            }

            names.Add(gap.Trim());
        }

        return names;
    }

    private static bool GapsAllMet(CareerPathDashboardStep step, string evidence)
    {
        var real = step.SkillsGap.Concat(step.MinRequirements).Where(IsRealGap).ToList();
        return real.Count > 0 && real.All(line => GapMatchesEvidence(line, evidence));
    }

    private static int? MonthsBetween(string? start, string? end)
    {
        if (!TryMonth(start, out var from))
        {
            return null;
        }

        var until = TryMonth(end, out var to) ? to : DateOnly.FromDateTime(DateTime.UtcNow);
        var months = (until.Year - from.Year) * 12 + until.Month - from.Month;
        return Math.Max(0, months);
    }

    private static bool TryMonth(string? value, out DateOnly month)
    {
        month = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length < 7)
        {
            return false;
        }

        if (!int.TryParse(value[..4], out var year) || !int.TryParse(value[5..7], out var m))
        {
            return false;
        }

        if (year < 1970 || m is < 1 or > 12)
        {
            return false;
        }

        month = new DateOnly(year, m, 1);
        return true;
    }

    /// <summary>A gap is already met when a meaningful word also appears in the profile evidence.</summary>
    public static bool GapMatchesEvidence(string gap, string evidence)
    {
        if (string.IsNullOrWhiteSpace(gap) || string.IsNullOrWhiteSpace(evidence))
        {
            return false;
        }

        var evidenceFold = CareerOccupationKeys.Fold(evidence);
        foreach (var token in CareerOccupationKeys.Fold(gap).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length >= 5 && CareerOccupationKeys.Hits(evidenceFold, token))
            {
                return true;
            }
        }

        return false;
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
