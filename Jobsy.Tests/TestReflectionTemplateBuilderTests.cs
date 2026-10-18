using Jobsy.Core.Enums;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;
using Xunit;

namespace Jobsy.Tests;

public sealed class TestReflectionTemplateBuilderTests
{
    [Fact]
    public void BuildFull_competence_nl_has_three_themes_and_sections()
    {
        var snapshot = new TestReflectionScoreSnapshot(
            AssessmentKind.Competence,
            AssessmentTestVersions.V3,
            25,
            [
                new(CompetencyTestCatalog.Samenwerken, 73, "Samen"),
                new(CompetencyTestCatalog.Resultaatgerichtheid, 72, "Afmaken"),
                new(CompetencyTestCatalog.Stressbestendigheid, 71, "Kalm"),
                new(CompetencyTestCatalog.Innovatie, 41, "Nieuw"),
                new(CompetencyTestCatalog.Extraversie, 60, "Mensen")
            ],
            DreamTitle: "dierenarts");

        var mirror = TestReflectionTemplateBuilder.BuildFull(snapshot, "hoe je werkt", "nl");

        Assert.Equal(3, mirror.Themes.Count);
        Assert.Equal(3, mirror.PracticeExamples.Count);
        Assert.True(mirror.WatchPoints.Count > 0);
        Assert.True(mirror.EnergyDrains.Count >= 2);
        Assert.All(mirror.Themes, t => Assert.DoesNotContain("groeit nog", t.Summary, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("dierenarts", mirror.DreamCallout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("score", mirror.EnvironmentBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("—", mirror.HeroSubtitle + string.Join(' ', mirror.WatchPoints));
        Assert.Contains("vaste ploeg", mirror.EnvironmentTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Null(CandidateFactGuard.MirrorCopyRejection(TestReflectionJson.FlattenForGuard(mirror)));
        Assert.False(mirror.FromOpenAi);
    }

    [Fact]
    public void BuildFull_analytical_profile_has_concrete_environment_and_dream()
    {
        var snapshot = new TestReflectionScoreSnapshot(
            AssessmentKind.Competence,
            AssessmentTestVersions.V3,
            25,
            [
                new(CompetencyTestCatalog.Innovatie, 88, "Nieuw"),
                new(CompetencyTestCatalog.Resultaatgerichtheid, 84, "Afmaken"),
                new(CompetencyTestCatalog.Stressbestendigheid, 76, "Kalm"),
                new(CompetencyTestCatalog.Samenwerken, 42, "Samen"),
                new(CompetencyTestCatalog.Extraversie, 32, "Mensen")
            ],
            "softwareontwikkelaar");

        var mirror = TestReflectionTemplateBuilder.BuildFull(snapshot, "hoe je werkt", "nl");

        Assert.Contains("focussen", mirror.EnvironmentTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("softwareontwikkelaar", mirror.DreamCallout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sterke kanten gebruikt", mirror.DreamCallout ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Null(CandidateFactGuard.MirrorCopyRejection(TestReflectionJson.FlattenForGuard(mirror)));
    }

    [Fact]
    public void BuildFull_competence_en_localizes_hero()
    {
        var snapshot = new TestReflectionScoreSnapshot(
            AssessmentKind.Competence,
            AssessmentTestVersions.V3,
            25,
            [new(CompetencyTestCatalog.Samenwerken, 80, "Together")],
            null);

        var mirror = TestReflectionTemplateBuilder.BuildFull(snapshot, "how you work", "en");

        Assert.StartsWith("Your mirror:", mirror.HeroTitle, StringComparison.Ordinal);
        Assert.Equal("en", mirror.Language);
    }

    [Fact]
    public void ComputeSourceHash_changes_when_scores_change()
    {
        var a = new TestReflectionScoreSnapshot(
            AssessmentKind.Competence, 3, 25,
            [new(CompetencyTestCatalog.Samenwerken, 70, "x")], null);
        var b = a with
        {
            Traits = [new(CompetencyTestCatalog.Samenwerken, 71, "x")]
        };

        Assert.NotEqual(
            TestReflectionTemplateBuilder.ComputeSourceHash(a),
            TestReflectionTemplateBuilder.ComputeSourceHash(b));
    }
}
