using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Layout;

namespace Jobsy.Tests;

/// <summary>Regression guards for acceptatie blockers: discovery autosave (V3 IDs) and banenkaart candidate shell.</summary>
public sealed class AcceptatieBlockerSaveAndNavTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Theory]
    [InlineData("banenkaart", true, false)]
    [InlineData("banen", true, false)]
    [InlineData("banenkaart", false, true)]
    [InlineData("candidate/start", true, false)]
    [InlineData("candidate/start", false, false)]
    public void Banenkaart_immersive_chrome_only_for_anonymous_visitors(
        string path,
        bool isAuthenticatedCandidate,
        bool expectedImmersive)
    {
        Assert.Equal(expectedImmersive, MainLayout.UsesBanenkaartImmersiveChrome(path, isAuthenticatedCandidate));
    }

    [Fact]
    public void Discovery_journey_loads_mini_questions_from_quick_scan_version()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor"));
        Assert.Contains("LoadQuickScanVersionAsync", src, StringComparison.Ordinal);
        Assert.Contains("AssessmentQuickScanCatalog.CareerQuestions(_quickScanVersion)", src, StringComparison.Ordinal);
        Assert.Contains(
            "TestDepthRules.QuestionIdsUpTo(assessmentKind, _targetLevel, _quickScanVersion)",
            src,
            StringComparison.Ordinal);
        Assert.DoesNotContain("OnboardingWizardCatalog.QuestionIdsUpTo", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Career_mini_five_on_v3_uses_free_tier_ids_not_legacy_v1_defaults()
    {
        var legacy = OnboardingWizardCatalog.QuestionIdsUpTo(
            OnboardingWizardCatalog.OnboardingTestKind.Career,
            OnboardingWizardCatalog.MiniLevelCount);
        var v3 = TestDepthRules.QuestionIdsUpTo(
            AssessmentKind.Career,
            OnboardingWizardCatalog.MiniLevelCount,
            AssessmentTestVersions.V3);

        Assert.Equal(OnboardingWizardCatalog.MiniLevelCount, legacy.Length);
        Assert.Equal(OnboardingWizardCatalog.MiniLevelCount, v3.Count);
        Assert.False(legacy.ToHashSet().SetEquals(v3));
        Assert.True(v3.All(id => AssessmentQuickScanCatalog.CareerQuestions(AssessmentTestVersions.V3)
            .Any(q => q.Id == id)));
    }

    [Fact]
    public void MainLayout_shows_bottom_nav_for_logged_in_candidates_on_banenkaart()
    {
        var layout = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("_banenkaartImmersive", layout, StringComparison.Ordinal);
        Assert.Contains("UsesBanenkaartImmersiveChrome", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("@if (!_isQuestionnairePage && !_isBanenkaartPage)", layout, StringComparison.Ordinal);
    }
}
