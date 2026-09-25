using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CareerCourseMatcherTests
{
    [Theory]
    [InlineData("Praktijkgericht leiderschap / coachmodule", "praktijkgericht leiderschap coachmodule")]
    [InlineData("Cursus Excel Basis", "excel")]
    [InlineData("Workshop Roosteren", "roosteren")]
    [InlineData("Opleiding Arbeidsrecht", "arbeidsrecht")]
    public void Normalize_strips_filler_words_and_diacritics(string input, string expectedContains)
    {
        var normalized = CareerCourseMatcher.Normalize(input);
        Assert.Contains(expectedContains, normalized, StringComparison.Ordinal);
        Assert.DoesNotContain(" cursus ", $" {normalized} ", StringComparison.Ordinal);
        Assert.DoesNotContain(" workshop ", $" {normalized} ", StringComparison.Ordinal);
        Assert.DoesNotContain(" opleiding ", $" {normalized} ", StringComparison.Ordinal);
        Assert.DoesNotContain(" basis ", $" {normalized} ", StringComparison.Ordinal);
        Assert.DoesNotContain(" module ", $" {normalized} ", StringComparison.Ordinal);
    }

    [Fact]
    public void Normalize_removes_diacritics()
    {
        Assert.Equal("cafe manager", CareerCourseMatcher.Normalize("Café-manager"));
    }

    [Fact]
    public void IsOnProfile_matches_equal_and_containment_with_min_length()
    {
        Assert.True(CareerCourseMatcher.IsOnProfile(
            "Praktijkgericht leiderschap / coachmodule",
            "Praktijkgericht leiderschap / coachmodule"));
        Assert.True(CareerCourseMatcher.IsOnProfile(
            "Korte workshop roosteren of teamsturing",
            "Roosteren of teamsturing"));
        Assert.False(CareerCourseMatcher.IsOnProfile("VCA", "VC"));
        Assert.False(CareerCourseMatcher.IsOnProfile("Excel", "PowerPoint"));
    }

    [Fact]
    public void StepMatchPercent_is_share_of_matched_courses_or_100_when_completed()
    {
        var courses = new[] { "Leiderschap", "Roosteren", "Excel" };
        var certs = new[] { "Leiderschap", "Roosteren" };
        Assert.Equal(67, CareerCourseMatcher.StepMatchPercent(courses, certs, stepCompleted: false));
        Assert.Equal(100, CareerCourseMatcher.StepMatchPercent(courses, certs, stepCompleted: true));
        Assert.Equal(0, CareerCourseMatcher.StepMatchPercent([], certs, stepCompleted: false));
    }
}

public class CareerStepStatusResolverTests
{
    private static CareerPlanStepContent Step(string key, int order, params string[] courses)
        => new(key, order, $"Stap {order}", "Samenvatting", [], courses, ["eis"], 0, "Actie", "/");

    [Fact]
    public void Manual_completion_marks_step_completed_and_next_active()
    {
        var steps = new[]
        {
            Step("a", 1),
            Step("b", 2, "Cursus A"),
            Step("c", 3, "Cursus B")
        };
        var progress = new[]
        {
            new CareerStepProgressSnapshot("a", 1, DateTime.UtcNow, CareerStepProgressSource.Manual, DateTime.UtcNow)
        };

        var view = CareerStepStatusResolver.Resolve(Guid.NewGuid(), "Dream", "dream", 40, "sam", steps, progress, []);
        Assert.Equal(HorizonCareerStepKind.Completed, view.Steps[0].Status);
        Assert.Equal(HorizonCareerStepKind.Active, view.Steps[1].Status);
        Assert.Equal(HorizonCareerStepKind.Open, view.Steps[2].Status);
        Assert.False(view.GoalReached);
    }

    [Fact]
    public void Auto_completes_when_all_courses_on_profile()
    {
        var steps = new[]
        {
            Step("a", 1, "Leiderschap", "Roosteren"),
            Step("b", 2, "Excel")
        };
        var view = CareerStepStatusResolver.Resolve(
            Guid.NewGuid(), "Dream", "dream", 40, "sam", steps, [],
            ["Leiderschap", "Roosteren"]);

        Assert.Equal(HorizonCareerStepKind.Completed, view.Steps[0].Status);
        Assert.Equal(100, view.Steps[0].StepMatchPercent);
        Assert.Equal(HorizonCareerStepKind.Active, view.Steps[1].Status);
        Assert.Equal(2, view.Steps[0].CoursesOnProfile);
    }

    [Fact]
    public void ManualUndo_blocks_auto_completion()
    {
        var steps = new[] { Step("a", 1, "Leiderschap") };
        var progress = new[]
        {
            new CareerStepProgressSnapshot("a", 1, null, CareerStepProgressSource.ManualUndo, DateTime.UtcNow)
        };
        var view = CareerStepStatusResolver.Resolve(
            Guid.NewGuid(), "Dream", "dream", 40, "sam", steps, progress, ["Leiderschap"]);

        Assert.Equal(HorizonCareerStepKind.Active, view.Steps[0].Status);
        Assert.Equal(100, view.Steps[0].StepMatchPercent); // wait - not completed so share is 100% of 1 course matched
    }

    [Fact]
    public void Steps_without_courses_only_complete_manually()
    {
        var steps = new[] { Step("a", 1), Step("b", 2, "Excel") };
        var view = CareerStepStatusResolver.Resolve(Guid.NewGuid(), "Dream", "dream", 40, "sam", steps, [], ["Excel"]);
        Assert.Equal(HorizonCareerStepKind.Active, view.Steps[0].Status);
        // Step B auto-completes via certificates, but step A (no courses) stays Active until manual.
        Assert.Equal(HorizonCareerStepKind.Completed, view.Steps[1].Status);
    }

    [Fact]
    public void All_completed_sets_goal_reached()
    {
        var steps = new[] { Step("a", 1, "A"), Step("b", 2, "B") };
        var view = CareerStepStatusResolver.Resolve(
            Guid.NewGuid(), "Dream", "dream", 40, "sam", steps, [], ["A", "B"]);
        Assert.True(view.GoalReached);
        Assert.All(view.Steps, s => Assert.Equal(HorizonCareerStepKind.Completed, s.Status));
    }

    [Fact]
    public void StepsNeedingAutoComplete_skips_manual_undo_and_already_stamped()
    {
        var steps = new[]
        {
            Step("a", 1, "A"),
            Step("b", 2, "B"),
            Step("c", 3, "C")
        };
        var progress = new[]
        {
            new CareerStepProgressSnapshot("a", 1, null, CareerStepProgressSource.ManualUndo, DateTime.UtcNow),
            new CareerStepProgressSnapshot("b", 2, DateTime.UtcNow, CareerStepProgressSource.Manual, DateTime.UtcNow)
        };
        var needing = CareerStepStatusResolver.StepsNeedingAutoComplete(steps, progress, ["A", "B", "C"]);
        Assert.Single(needing);
        Assert.Equal("c", needing[0].StepKey);
    }
}

public class CareerStepKeyTests
{
    [Fact]
    public void ForStep_is_stable_for_same_title_and_order()
    {
        var a = CareerStepKey.ForStep("Skills & competenties dichten", 2);
        var b = CareerStepKey.ForStep("Skills & competenties dichten", 2);
        var c = CareerStepKey.ForStep("Skills & competenties dichten", 3);
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(10, a.Length);
    }

    [Fact]
    public void ForDream_normalizes_title()
    {
        Assert.Equal(CareerStepKey.ForDream("Teamleider logistiek"), CareerStepKey.ForDream("  Teamleider   logistiek "));
    }
}
