using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CareerStepStatusResolverTests
{
    private static CareerStepStatusResolver.StepInput Step(string key, int order, params string[] courses)
        => new(key, order, courses);

    [Fact]
    public void First_non_completed_is_active_rest_open()
    {
        var steps = new[]
        {
            Step("a", 1, "Course A"),
            Step("b", 2, "Course B"),
            Step("c", 3)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, [], []);
        Assert.Equal(HorizonCareerStepKind.Active, resolved[0].Status);
        Assert.Equal(HorizonCareerStepKind.Open, resolved[1].Status);
        Assert.Equal(HorizonCareerStepKind.Open, resolved[2].Status);
        Assert.False(CareerStepStatusResolver.GoalReached(resolved));
    }

    [Fact]
    public void Manual_completion_marks_completed_and_advances_active()
    {
        var steps = new[] { Step("a", 1), Step("b", 2), Step("c", 3) };
        var progress = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("a", DateTime.UtcNow, CareerStepProgressSources.Manual)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, progress, []);
        Assert.Equal(HorizonCareerStepKind.Completed, resolved[0].Status);
        Assert.Equal(100, resolved[0].StepMatchPercent);
        Assert.Equal(HorizonCareerStepKind.Active, resolved[1].Status);
    }

    [Fact]
    public void Auto_completes_when_all_courses_matched()
    {
        var steps = new[] { Step("a", 1, "Leiderschap", "Roosteren") };
        var certs = new[]
        {
            new CandidateCertificateDto("Cursus leiderschap", 2024),
            new CandidateCertificateDto("Workshop roosteren", 2025)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, [], certs);
        Assert.Equal(HorizonCareerStepKind.Completed, resolved[0].Status);
        Assert.True(resolved[0].AutoCompletable);
        Assert.Equal(100, resolved[0].StepMatchPercent);
        Assert.Equal(2, resolved[0].MatchedCourseCount);
        Assert.True(CareerStepStatusResolver.GoalReached(resolved));
    }

    [Fact]
    public void ManualUndo_blocks_auto_completion()
    {
        var steps = new[] { Step("a", 1, "Leiderschap") };
        var certs = new[] { new CandidateCertificateDto("Leiderschap", 2024) };
        var progress = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("a", null, CareerStepProgressSources.ManualUndo)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, progress, certs);
        Assert.Equal(HorizonCareerStepKind.Active, resolved[0].Status);
        Assert.False(resolved[0].AutoCompletable);
        Assert.Equal(100, resolved[0].StepMatchPercent);
    }

    [Fact]
    public void Step_without_courses_only_completes_manually()
    {
        var steps = new[] { Step("a", 1) };
        var resolvedOpen = CareerStepStatusResolver.Resolve(steps, [], []);
        Assert.Equal(HorizonCareerStepKind.Active, resolvedOpen[0].Status);
        Assert.False(resolvedOpen[0].AutoCompletable);

        var progress = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("a", DateTime.UtcNow, CareerStepProgressSources.Manual)
        };
        var resolvedDone = CareerStepStatusResolver.Resolve(steps, progress, []);
        Assert.Equal(HorizonCareerStepKind.Completed, resolvedDone[0].Status);
        Assert.True(CareerStepStatusResolver.GoalReached(resolvedDone));
    }

    [Fact]
    public void Partial_course_match_sets_step_match_percent()
    {
        var steps = new[] { Step("a", 1, "Leiderschap", "Roosteren") };
        var certs = new[] { new CandidateCertificateDto("Leiderschap", 2024) };
        var resolved = CareerStepStatusResolver.Resolve(steps, [], certs);
        Assert.Equal(HorizonCareerStepKind.Active, resolved[0].Status);
        Assert.Equal(50, resolved[0].StepMatchPercent);
        Assert.Equal(1, resolved[0].MatchedCourseCount);
    }

    [Fact]
    public void StepKey_is_stable_for_title_and_order()
    {
        var a = CareerStepKey.Create(2, "Skills & competenties dichten");
        var b = CareerStepKey.Create(2, "Skills & competenties dichten");
        var c = CareerStepKey.Create(3, "Skills & competenties dichten");
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(8, a.Length);
    }
}
