using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class OnboardingFinaleMapBodyTests
{
    private static readonly OnboardingFinaleCard BanenkaartCard =
        new(OnboardingFinaleCardId.Banenkaart, "Onboarding.FinaleV2.Do.Map.Title", OnboardingFinaleCards.MapBodyKey, false);

    [Theory]
    [InlineData("nl", "Zaandam")]
    [InlineData("en", "Zaandam")]
    [InlineData("pl", "Zaandam")]
    [InlineData("ro", "Zaandam")]
    [InlineData("ar", "Zaandam")]
    public void Banenkaart_body_with_home_formats_place(string lang, string home)
    {
        var text = Format(lang, home);
        Assert.Contains(home, text, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}", text, StringComparison.Ordinal);
        Assert.DoesNotContain("vanaf {0}", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Banenkaart_body_without_home_has_no_placeholder(string lang)
    {
        var text = Format(lang, homeLabel: null);
        Assert.DoesNotContain("{0}", text, StringComparison.Ordinal);
        Assert.False(text.TrimEnd().EndsWith("vanaf", StringComparison.OrdinalIgnoreCase));
        Assert.False(text.TrimEnd().EndsWith("from", StringComparison.OrdinalIgnoreCase));
    }

    private static string Format(string lang, string? homeLabel)
    {
        string Localize(string key) => UiStrings.Get(key, lang);
        string FormatKey(string key, params object[] args)
            => string.Format(System.Globalization.CultureInfo.InvariantCulture, Localize(key), args);

        return OnboardingFinaleCards.FormatBody(
            BanenkaartCard,
            homeLabel,
            dreamJob: null,
            Localize,
            FormatKey);
    }
}
