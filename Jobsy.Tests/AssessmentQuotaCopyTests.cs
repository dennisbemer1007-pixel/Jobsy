using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class AssessmentQuotaCopyTests
{
    [Theory]
    [InlineData("nl", 3, "TestResult.Quota.Line")]
    [InlineData("en", 2, "TestResult.Quota.Line")]
    [InlineData("nl", 1, "TestResult.Quota.LineOne")]
    [InlineData("en", 1, "TestResult.Quota.LineOne")]
    [InlineData("nl", 0, "TestResult.Quota.Zero")]
    [InlineData("en", 0, "TestResult.Quota.Zero")]
    [InlineData("pl", 2, "TestResult.Quota.Line")]
    [InlineData("ro", 1, "TestResult.Quota.LineOne")]
    [InlineData("ar", 0, "TestResult.Quota.Zero")]
    public void Quota_keys_exist_for_all_languages(string lang, int remaining, string key)
    {
        var template = UiStrings.Get(key, lang);
        Assert.False(string.IsNullOrWhiteSpace(template));
        Assert.DoesNotContain("TestResult.", template);

        var text = remaining switch
        {
            0 => string.Format(System.Globalization.CultureInfo.InvariantCulture, template, 3),
            1 => string.Format(System.Globalization.CultureInfo.InvariantCulture, template, 3),
            _ => string.Format(System.Globalization.CultureInfo.InvariantCulture, template, remaining, 3)
        };
        Assert.Contains("3", text);
        if (remaining is > 0 and not 1)
        {
            Assert.Contains(remaining.ToString(), text);
        }
    }
}
