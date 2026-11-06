using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Onboarding;

namespace Jobsy.Tests;

public sealed class OnboardingDreamSuggestionPickerTests
{
    [Fact]
    public void BuildFromProfile_uses_sector_fallback_with_dutch_profession_casing()
    {
        var draft = new OnboardingProfileDraft { Education = "Basisschool" };
        var items = OnboardingDreamSuggestionPicker.BuildFromProfile(draft, [], max: 4);
        Assert.True(items.Count >= 4);
        Assert.All(items, item => Assert.DoesNotContain(" Centrum ", item.Title, StringComparison.Ordinal));
        Assert.DoesNotContain(items, i => i.Title.Contains(' ') && char.IsUpper(i.Title.Split(' ')[1][0]));
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
    public void BuildFromProfile_maps_nearby_vacancy_to_profession_for_fit()
    {
        var draft = new OnboardingProfileDraft { Education = "HBO" };
        var items = OnboardingDreamSuggestionPicker.BuildFromProfile(
            draft,
            ["Barista centrum Naaldwijk"],
            max: 6);
        Assert.Contains(items, i => string.Equals(i.Title, "Barista", StringComparison.Ordinal));
        Assert.DoesNotContain(items, i => i.Title.Contains("Naaldwijk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FormatProfessionFromVacancy_and_regional_use_dutch_norms()
    {
        Assert.Equal("Barista", OnboardingDreamSuggestionPicker.FormatProfessionFromVacancy("Barista centrum Naaldwijk"));
        Assert.Equal("Teamleider kas", OnboardingDreamSuggestionPicker.FormatRegionalVacancyLine("Teamleider kas"));
        Assert.Equal("Magazijnmedewerker", OnboardingDreamSuggestionPicker.FormatRegionalVacancyLine("Magazijn Naaldwijk"));
        Assert.Equal("Horecamedewerker", OnboardingDreamSuggestionPicker.FormatRegionalVacancyLine("Flex horeca weekend"));
        Assert.Equal("Monteur", OnboardingDreamSuggestionPicker.FormatRegionalVacancyLine("Senior monteur kas"));
        Assert.Equal("", OnboardingDreamSuggestionPicker.FormatRegionalVacancyLine("Flex"));
        Assert.Equal("HR-adviseur", OnboardingDreamSuggestionPicker.FormatProfessionFromVacancy("hr adviseur"));
    }

    [Fact]
    public void BuildRegionalTitles_skips_titles_already_in_fit_list()
    {
        var nearby = new[]
        {
            "Barista centrum Naaldwijk",
            "Teamleider kas",
            "Klantenservice winkel",
            "Bediening lunchroom",
        };
        var regional = OnboardingDreamSuggestionPicker.BuildRegionalTitles(
            nearby,
            max: 4,
            excludeTitles: ["Barista", "Kassamedewerker"]);
        Assert.DoesNotContain(regional, t => string.Equals(t, "Barista", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(regional, t => string.Equals(t, "Teamleider kas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildRegionalTitles_skips_profession_prefix_overlap_with_fit()
    {
        var regional = OnboardingDreamSuggestionPicker.BuildRegionalTitles(
            ["Kassamedewerker Naaldwijk", "Teamleider kas"],
            max: 4,
            excludeTitles: ["Kassamedewerker"]);
        Assert.DoesNotContain(regional, t => t.Contains("Kassamedewerker", StringComparison.OrdinalIgnoreCase));
    }
}
