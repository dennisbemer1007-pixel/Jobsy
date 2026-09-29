using Bunit;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class Ontdekkingsreis07bSourceTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string JourneySource =>
        File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor"));

    [Fact]
    public void Journey_wires_steps_3_to_6_and_consent_and_mini_tests()
    {
        var src = JourneySource;
        Assert.Contains("WorkHistoryStep", src, StringComparison.Ordinal);
        Assert.Contains("Discovery.Step3.EmployerWant", src, StringComparison.Ordinal);
        Assert.Contains("EducationStep", src, StringComparison.Ordinal);
        Assert.Contains("Discovery.Step4.DreamTitle", src, StringComparison.Ordinal);
        Assert.Contains("Discovery.Step5.Hobbies", src, StringComparison.Ordinal);
        Assert.Contains("Discovery.Step6.Heading", src, StringComparison.Ordinal);
        Assert.Contains("Discovery.Dislike.PrivateNote", src, StringComparison.Ordinal);
        Assert.Contains("journey-private-note", src, StringComparison.Ordinal);
        Assert.Contains("ShowSkip", src, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Discovery.Skip", src, StringComparison.Ordinal);
        Assert.Contains("toestemming", src, StringComparison.Ordinal);
        Assert.Contains("TestConsentStep", src, StringComparison.Ordinal);
        Assert.Contains("OnboardingMiniTest", src, StringComparison.Ordinal);
        Assert.Contains("CompleteJourneyTransitionalAsync", src, StringComparison.Ordinal);
        Assert.Contains("CompleteMyOnboardingAsync", src, StringComparison.Ordinal);
        Assert.Contains("/candidate/paspoort", src, StringComparison.Ordinal);
        Assert.Contains("FeatureVisible", src, StringComparison.Ordinal);
        Assert.Contains("PlatformFeature.Employers", src, StringComparison.Ordinal);
        Assert.Contains("UpdateMyPrivatePreferencesAsync", src, StringComparison.Ordinal);
        Assert.Contains("TryAutoAdvanceEmptyMiniStepAsync", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Discovery.Todo.LaterSteps", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Journey_step6_skip_and_later_verder_present()
    {
        var src = JourneySource;
        Assert.Contains("SkipAsync", src, StringComparison.Ordinal);
        Assert.Contains("LaterAsync", src, StringComparison.Ordinal);
        Assert.Contains("stepSkipped: true", src, StringComparison.Ordinal);
        Assert.Contains("FlushPrivateAsync", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Mini_test_skip_logic_uses_locked_ids_from_catalog()
    {
        var src = JourneySource;
        Assert.Contains("OnboardingWizardCatalog.CompetencyQuestionIds", src, StringComparison.Ordinal);
        Assert.Contains("_lockedIds", src, StringComparison.Ordinal);
        Assert.Contains("!_lockedIds.Contains(item.Id)", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_step_components_support_journey_heading_flags()
    {
        var dir = Path.Combine(RepoRoot, "Jobsy.Web/Components/Candidate/Onboarding");
        var work = File.ReadAllText(Path.Combine(dir, "WorkHistoryStep.razor"));
        var edu = File.ReadAllText(Path.Combine(dir, "EducationStep.razor"));
        var dream = File.ReadAllText(Path.Combine(dir, "DreamJobStep.razor"));
        Assert.Contains("ShowHeading", work, StringComparison.Ordinal);
        Assert.Contains("ShowHeading", edu, StringComparison.Ordinal);
        Assert.Contains("AsSubsection", dream, StringComparison.Ordinal);
        Assert.Contains("TitleKey", dream, StringComparison.Ordinal);
    }

    [Fact]
    public void Discovery_strings_cover_steps_3_to_6_copy()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsDiscovery.MergeAll(nl, en, pl, ro, ar);

        foreach (var key in new[]
                 {
                     "Discovery.Step3.Heading", "Discovery.Step3.EmployerWantHint",
                     "Discovery.Step4.DreamTitle", "Discovery.Step4.LearnWant",
                     "Discovery.Step5.AboutMe", "Discovery.Step6.SkipBadge",
                     "Discovery.Step6.LeadEmployersOff", "Discovery.Skip",
                     "Discovery.Dislike.PrivateNoteEmployersOff", "Discovery.Consent.Eyebrow"
                 })
        {
            Assert.True(nl.ContainsKey(key), key);
            Assert.False(string.Equals(nl[key], en[key], StringComparison.Ordinal), key);
        }
    }
}

public class JourneyProgressRailBunitTests : TestContext
{
    public JourneyProgressRailBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Rail_marks_current_step_and_saved_text()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<JourneyProgressRail>(0);
            builder.AddAttribute(1, "CurrentStep", 3);
            builder.AddAttribute(2, "SaveStatus", "saved");
            builder.CloseComponent();
        });

        Assert.Contains("aria-current=\"step\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Alles is bewaard", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Stap 3 van 10", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Rail_shows_error_save_state()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<JourneyProgressRail>(0);
            builder.AddAttribute(1, "CurrentStep", 2);
            builder.AddAttribute(2, "SaveStatus", "error");
            builder.CloseComponent();
        });

        Assert.Contains("Niet bewaard", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("is-error", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}

public class OntdekkingsreisMiniSkipLogicTests
{
    [Fact]
    public void Competency_catalog_has_five_onboarding_ids()
        => Assert.Equal(5, OnboardingWizardCatalog.CompetencyQuestionIds.Length);

    [Fact]
    public void Locked_ids_leave_remaining_questions()
    {
        var locked = new HashSet<int> { 1, 6, 11 };
        var remaining = OnboardingWizardCatalog.CompetencyQuestionIds
            .Where(id => !locked.Contains(id))
            .ToArray();
        Assert.Equal([16, 21], remaining);
    }

    [Fact]
    public void All_five_locked_means_empty_step()
    {
        var locked = OnboardingWizardCatalog.CompetencyQuestionIds.ToHashSet();
        Assert.Empty(OnboardingWizardCatalog.CompetencyQuestionIds.Where(id => !locked.Contains(id)));
    }
}
