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
        Assert.Contains("_banenkaartCandidateShell", layout, StringComparison.Ordinal);
        Assert.Contains("app-shell--banenkaart-shell", layout, StringComparison.Ordinal);
        Assert.Contains("UsesBanenkaartImmersiveChrome", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("@if (!_isQuestionnairePage && !_isBanenkaartPage)", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Onboarding_wizard_first_mini_test_uses_quick_scan_catalog_version()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("ResolveQuickScanVersion", src, StringComparison.Ordinal);
        Assert.Contains("TestDepthRules.MiniIds(AssessmentKind.Competence, compVersion)", src, StringComparison.Ordinal);
        Assert.Contains("AssessmentQuickScanCatalog.CompetenceQuestions(compVersion)", src, StringComparison.Ordinal);
        Assert.DoesNotContain("OnboardingWizardCatalog.QuestionIdsUpTo", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_candidate_header_shows_primary_nav_links()
    {
        var layout = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Layout/MainLayout.razor"));
        var shellHeader = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Layout/AppShellHeader.razor"));
        var nav = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Layout/CandidateHeaderNav.razor"));
        var program = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Program.cs"));
        var css = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/css/app.css"));

        Assert.Contains("AppShellHeader", layout, StringComparison.Ordinal);
        Assert.Contains("CandidateHeaderNav", shellHeader, StringComparison.Ordinal);
        Assert.Contains("RoleNavItemsState", nav, StringComparison.Ordinal);
        Assert.Contains("RoleNavCatalog.IsActive", nav, StringComparison.Ordinal);
        Assert.Contains("AddScoped<Jobsy.Web.Services.RoleNavItemsState>", program, StringComparison.Ordinal);
        Assert.Contains(".app-header:has(.candidate-header-nav) .candidate-header-nav", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Banenkaart_hides_external_vacancy_promo_while_location_prompt_open()
    {
        var discovery = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/VacancyDiscovery.razor"));
        var css = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/css/features/kandidaat-banen.css"));
        Assert.Contains("kb-start-prompt-open", discovery, StringComparison.Ordinal);
        Assert.Contains("_showStartPrompt", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-start-prompt-open .kb-ext__promo", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Banenkaart_uses_shared_app_header_not_duplicate_discovery_chrome()
    {
        var discovery = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/VacancyDiscovery.razor"));
        var header = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Layout/AppShellHeader.razor"));
        Assert.DoesNotContain("jobsy-chrome__header", discovery, StringComparison.Ordinal);
        Assert.Contains("CandidateHeaderNav", header, StringComparison.Ordinal);
    }
}
