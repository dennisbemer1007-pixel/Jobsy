using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Onboarding;

namespace Jobsy.Tests;

public sealed class OnboardingDreamSuggestionPickerTests
{
    [Fact]
    public void BuildFromProfile_uses_sector_fallback_with_display_titles()
    {
        var draft = new OnboardingProfileDraft { Education = "Basisschool" };
        var items = OnboardingDreamSuggestionPicker.BuildFromProfile(draft, [], max: 4);
        Assert.True(items.Count >= 4);
        Assert.All(items, item => Assert.True(char.IsUpper(item.Title[0])));
        Assert.DoesNotContain("hoger onderwijs", items[0].Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildFromProfile_links_work_history_with_concrete_reason()
    {
        var draft = new OnboardingProfileDraft();
        draft.Jobs.Add(new Jobsy.Web.Models.CandidateEmployerHistory { Role = "kassamedewerker" });
        var items = OnboardingDreamSuggestionPicker.BuildFromProfile(draft, [], max: 4);
        Assert.Contains(items, i => i.Reason.Contains("kassamedewerker", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildFromProfile_adds_education_hints_for_hbo()
    {
        var draft = new OnboardingProfileDraft { Education = "HBO" };
        var items = OnboardingDreamSuggestionPicker.BuildFromProfile(draft, [], max: 4);
        Assert.Contains(items, i => i.Title.Contains("Accountmanager", StringComparison.OrdinalIgnoreCase));
    }
}
