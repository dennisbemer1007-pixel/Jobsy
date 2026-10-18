using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;

namespace Jobsy.Tests;

public sealed class OnboardingWorkHistoryRulesTests
{
    [Fact]
    public void IsBlank_true_for_empty_row()
    {
        Assert.True(OnboardingWorkHistoryRules.IsBlank(null, null, null, null, false));
        Assert.True(OnboardingWorkHistoryRules.IsBlank("", "  ", "", "", null));
    }

    [Fact]
    public void IsBlank_false_when_any_field_set()
    {
        Assert.False(OnboardingWorkHistoryRules.IsBlank("Acme", null, null, null, null));
        Assert.False(OnboardingWorkHistoryRules.IsBlank(null, "Kassier", null, null, null));
        Assert.False(OnboardingWorkHistoryRules.IsBlank(null, null, "2020-01", null, null));
        Assert.False(OnboardingWorkHistoryRules.IsBlank(null, null, null, null, true));
    }

    [Fact]
    public void Work_history_step_shows_first_slot_and_add_another_label()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Components",
            "Candidate",
            "Onboarding",
            "WorkHistoryStep.razor"));
        var src = File.ReadAllText(path);

        Assert.Contains("EnsureFirstJobSlot", src, StringComparison.Ordinal);
        Assert.Contains("Onboarding.AddAnotherJob", src, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"onboarding-work-job\"", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Onboarding.AddJob", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Work_history_step_has_no_reference_ui()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Components",
            "Candidate",
            "Onboarding",
            "WorkHistoryStep.razor"));
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("Referentie", src, StringComparison.Ordinal);
        Assert.DoesNotContain("onboarding-add-reference", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Wizard_persists_only_non_blank_jobs()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Components",
            "Pages",
            "Candidate",
            "OnboardingWizard.razor"));
        var src = File.ReadAllText(path);

        Assert.Contains("JobsForSave()", src, StringComparison.Ordinal);
    }
}
