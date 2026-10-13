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
        Assert.All(resolved, s => Assert.False(s.HeldBack));
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
        Assert.Equal(2, resolved[0].MatchedCourseCount);
        Assert.True(CareerStepStatusResolver.GoalReached(resolved));
    }

    [Fact]
    public void ManualUndo_blocks_auto_completion_while_fingerprint_matches()
    {
        var steps = new[] { Step("a", 1, "Leiderschap") };
        var certs = new[] { new CandidateCertificateDto("Leiderschap", 2024) };
        var fingerprint = CareerStepStatusResolver.FingerprintCertificates(["Leiderschap"], certs);
        var progress = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("a", null, CareerStepProgressSources.ManualUndo, fingerprint)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, progress, certs);
        Assert.Equal(HorizonCareerStepKind.Active, resolved[0].Status);
        Assert.False(resolved[0].AutoCompletable);
    }

    [Fact]
    public void ManualUndo_stops_blocking_once_a_new_certificate_changes_the_fingerprint()
    {
        var steps = new[] { Step("a", 1, "Leiderschap") };
        var certsAtUndo = new[] { new CandidateCertificateDto("Leiderschap Basis", 2024) };
        var fingerprintAtUndo = CareerStepStatusResolver.FingerprintCertificates(["Leiderschap"], certsAtUndo);
        var progress = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("a", null, CareerStepProgressSources.ManualUndo, fingerprintAtUndo)
        };

        // The fingerprint still matches: auto-complete stays blocked.
        var stillBlocked = CareerStepStatusResolver.Resolve(steps, progress, certsAtUndo);
        Assert.False(stillBlocked[0].AutoCompletable);

        // A new matching certificate changes the fingerprint: auto-complete is allowed again.
        var certsNow = new[]
        {
            new CandidateCertificateDto("Leiderschap Basis", 2024),
            new CandidateCertificateDto("Leiderschap Advanced", 2025)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, progress, certsNow);
        Assert.Equal(HorizonCareerStepKind.Completed, resolved[0].Status);
    }

    [Fact]
    public void Legacy_undo_without_fingerprint_blocks_forever()
    {
        var steps = new[] { Step("a", 1, "Leiderschap") };
        var certs = new[] { new CandidateCertificateDto("Leiderschap", 2024) };
        var progress = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("a", null, CareerStepProgressSources.ManualUndo, UndoFingerprint: null)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, progress, certs);
        Assert.False(resolved[0].AutoCompletable);
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
    public void Partial_course_match_does_not_complete_the_step()
    {
        var steps = new[] { Step("a", 1, "Leiderschap", "Roosteren") };
        var certs = new[] { new CandidateCertificateDto("Leiderschap", 2024) };
        var resolved = CareerStepStatusResolver.Resolve(steps, [], certs);
        Assert.Equal(HorizonCareerStepKind.Active, resolved[0].Status);
        Assert.Equal(1, resolved[0].MatchedCourseCount);
    }

    [Fact]
    public void Later_stamp_with_earlier_step_open_is_held_back_not_lost()
    {
        // Legacy data: step "b" carries a completion stamp even though "a" is still open.
        var steps = new[] { Step("a", 1, "Course A"), Step("b", 2) };
        var progress = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("b", DateTime.UtcNow, CareerStepProgressSources.Manual)
        };
        var resolved = CareerStepStatusResolver.Resolve(steps, progress, []);
        var b = resolved.Single(s => s.StepKey == "b");
        Assert.True(b.HeldBack);
        Assert.NotEqual(HorizonCareerStepKind.Completed, b.Status);

        // Once "a" completes, "b" becomes Completed and is no longer held back.
        var progress2 = new[]
        {
            new CareerStepStatusResolver.ProgressSignal("a", DateTime.UtcNow, CareerStepProgressSources.Manual),
            new CareerStepStatusResolver.ProgressSignal("b", DateTime.UtcNow, CareerStepProgressSources.Manual)
        };
        var resolved2 = CareerStepStatusResolver.Resolve(steps, progress2, []);
        var b2 = resolved2.Single(s => s.StepKey == "b");
        Assert.False(b2.HeldBack);
        Assert.Equal(HorizonCareerStepKind.Completed, b2.Status);
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
