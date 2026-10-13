using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class RoleFitBandRulesTests
{
    [Theory]
    [InlineData(100, RoleFitBandRules.Band.Good, "Past goed")]
    [InlineData(75, RoleFitBandRules.Band.Good, "Past goed")]
    [InlineData(74, RoleFitBandRules.Band.Fair, "Past redelijk")]
    [InlineData(50, RoleFitBandRules.Band.Fair, "Past redelijk")]
    [InlineData(49, RoleFitBandRules.Band.NotYet, "Past nog niet")]
    [InlineData(0, RoleFitBandRules.Band.NotYet, "Past nog niet")]
    public void Bands_match_thresholds(int percent, RoleFitBandRules.Band expected, string dutch)
    {
        Assert.Equal(expected, RoleFitBandRules.FromPercent(percent));
        Assert.Equal(dutch, RoleFitBandRules.DutchLabel(percent));
        Assert.Equal(RoleFitBandRules.LabelKey(expected), RoleFitBandRules.LabelKey(percent));
    }

    [Fact]
    public void Label_keys_match_passport_nl_strings()
    {
        Assert.Equal("Past goed", Jobsy.Web.Localization.UiStrings.Get("Passport.Fit.Band.Good", "nl"));
        Assert.Equal("Past redelijk", Jobsy.Web.Localization.UiStrings.Get("Passport.Fit.Band.Fair", "nl"));
        Assert.Equal("Past nog niet", Jobsy.Web.Localization.UiStrings.Get("Passport.Fit.Band.NotYet", "nl"));
    }
}

public class RoleFitCheckSessionTests
{
    [Fact]
    public void Session_starts_loading_locked_until_api_fills_state()
    {
        var session = new RoleFitCheckSession();
        Assert.True(session.Loading);
        Assert.False(session.IsUnlocked);
        Assert.Null(session.LastResult);
        Assert.Equal("", session.JobTitle);
        Assert.False(session.Busy);
    }

    [Fact]
    public void Fit_tab_results_come_from_dto_without_percent_in_band_label()
    {
        var result = new RoleFitCheckResult
        {
            MatchPercent = 82,
            Strengths = ["Zorgzaam", "Geduldig", "Extra"],
            Gaps = ["Diploma niveau 2", "Ervaring"],
            ActionSteps = ["Leren en werken (BBL)", "Volgende"]
        };

        var band = RoleFitBandRules.DutchLabel(result.MatchPercent);
        Assert.Equal("Past goed", band);
        Assert.DoesNotContain("%", band, StringComparison.Ordinal);
        Assert.Equal("Zorgzaam, Geduldig", string.Join(", ", result.Strengths.Take(2)));
        Assert.Equal("Diploma niveau 2", result.Gaps[0]);
        Assert.Equal("Leren en werken (BBL)", result.ActionSteps[0]);
    }
}

public class CareerPlanViewBuilderTests
{
    [Fact]
    public void FromApi_parity_with_CareerPathService()
    {
        var api = SamplePlan();
        var svc = new CareerPathService();
        var viaService = svc.FromApi(api);
        var viaBuilder = CareerPlanViewBuilder.FromApi(api, svc.GetDreamSuggestions());

        Assert.Equal(viaService.DreamRoleTitle, viaBuilder.DreamRoleTitle);
        Assert.Equal(viaService.HasPlan, viaBuilder.HasPlan);
        Assert.Equal(viaService.Steps.Count, viaBuilder.Steps.Count);
        Assert.Equal(viaService.Steps[0].Status, viaBuilder.Steps[0].Status);
        Assert.Equal(viaService.Steps[1].Courses.Count, viaBuilder.Steps[1].Courses.Count);
        Assert.Equal(viaService.Steps[1].MatchedCourseCount, viaBuilder.Steps[1].MatchedCourseCount);
        Assert.Equal(viaService.Steps[1].SkillsGap, viaBuilder.Steps[1].SkillsGap);
    }

    [Fact]
    public void BuildPassport_empty_when_no_plan()
    {
        var view = CareerPlanViewBuilder.BuildPassport(new CareerPathService().EmptyDashboard());
        Assert.False(view.HasPlan);
        Assert.Empty(view.Shells);
        Assert.Null(view.FocusStep);
    }

    [Fact]
    public void BuildPassport_shell_states_and_gaps()
    {
        var model = new CareerPathService().FromApi(SamplePlan());
        var view = CareerPlanViewBuilder.BuildPassport(model);

        Assert.True(view.HasPlan);
        Assert.Equal("MBO-verpleegkundige", view.DreamRoleTitle);
        Assert.Contains(view.Shells, s => s.Kind == CareerPlanViewBuilder.ShellKind.Done);
        Assert.Contains(view.Shells, s => s.Kind == CareerPlanViewBuilder.ShellKind.Current);
        Assert.Contains(view.Shells, s => s.Kind == CareerPlanViewBuilder.ShellKind.Future);
        Assert.Contains(view.Shells, s => s.IsGoal && s.Kind == CareerPlanViewBuilder.ShellKind.Goal);
        Assert.NotNull(view.FocusStep);
        Assert.Equal(CareerStepStatus.Active, view.FocusStep!.Status);
        Assert.Contains(view.Gaps, g => !g.Met && g.Text.Contains("Diploma", StringComparison.Ordinal));
        Assert.Contains(view.Gaps, g => g.Met);
        Assert.NotEmpty(view.CourseSearchKeys);
        Assert.Equal("CareerFit.Fair", view.StepBandLabelKey);
    }

    [Fact]
    public void BuildGaps_marks_on_profile_courses_as_met()
    {
        var step = new CareerPathDashboardStep
        {
            SkillsGap = ["Diploma Helpende"],
            MinRequirements = ["1 jaar ervaring"],
            Courses =
            [
                new CareerPathCourseStatus { Name = "Zorgzaam", OnProfile = true },
                new CareerPathCourseStatus { Name = "Nog te doen", OnProfile = false }
            ]
        };

        var gaps = CareerPlanViewBuilder.BuildGaps(step);
        Assert.Contains(gaps, g => g is { Met: false, Text: "Diploma Helpende" });
        Assert.Contains(gaps, g => g is { Met: false, Text: "1 jaar ervaring" });
        Assert.Contains(gaps, g => g is { Met: true, Text: "Zorgzaam" });
        Assert.DoesNotContain(gaps, g => g.Text == "Nog te doen");
    }

    private static CareerPathPlanApiModel SamplePlan()
        => new()
        {
            DreamTitle = "MBO-verpleegkundige",
            MatchPercent = 40,
            MatchSummary = "test",
            GoalReached = false,
            Steps =
            [
                new CareerPathStepApiModel
                {
                    Id = "s1",
                    Order = 1,
                    Title = "Zorghulp",
                    Status = "Completed",
                    SkillsGap = ["Basis"],
                    Courses = ["Kennismaken"],
                    CourseStatuses = [new CareerPathCourseApiModel { Name = "Kennismaken", OnProfile = true }],
                    MinRequirements = [],
                    StepMatchPercent = 70,
                    MatchedCourseCount = 1
                },
                new CareerPathStepApiModel
                {
                    Id = "s2",
                    Order = 2,
                    Title = "Helpende niveau 2",
                    Status = "Active",
                    StepFitBand = "Fair",
                    SkillsGap = ["Diploma Helpende (niveau 2)"],
                    Courses = ["Helpende (BBL)", "Zorgzaam"],
                    CourseStatuses =
                    [
                        new CareerPathCourseApiModel { Name = "Helpende (BBL)", OnProfile = false },
                        new CareerPathCourseApiModel { Name = "Zorgzaam", OnProfile = true }
                    ],
                    MinRequirements = ["1 jaar ervaring in de zorg"],
                    ActionKinds = ["Vacancies"],
                    ActionHref = "/?q=helpende",
                    ActionLabel = "Zoek",
                    StepMatchPercent = 72,
                    MatchedCourseCount = 1
                },
                new CareerPathStepApiModel
                {
                    Id = "s3",
                    Order = 3,
                    Title = "Verzorgende IG",
                    Status = "Open",
                    SkillsGap = ["IG"],
                    Courses = [],
                    MinRequirements = [],
                    StepMatchPercent = 30
                }
            ]
        };
}
