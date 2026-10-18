using Jobsy.Web.Onboarding;

namespace Jobsy.Tests;

public sealed class OnboardingDreamSuggestionPickerTests
{
    [Fact]
    public void BuildFitSuggestions_returns_at_least_two_without_api()
    {
        var items = OnboardingDreamSuggestionPicker.BuildFitSuggestions([], "HBO", max: 4);
        Assert.True(items.Count >= 2, $"expected suggestions, got {items.Count}");
    }
}
