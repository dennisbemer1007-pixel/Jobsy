using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class CandidateProfileResetCopyTests
{
    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Profile_reset_strings_exist_in_all_languages(string lang)
    {
        foreach (var key in new[]
                 {
                     "Profile.Reset.Title",
                     "Profile.Reset.Dialog.Lead",
                     "Profile.Reset.Dialog.ApplicationsNote",
                     "Profile.Reset.Dialog.ResearchNote",
                     "Profile.Reset.Dialog.ConfirmLabel"
                 })
        {
            var value = UiStrings.Get(key, lang);
            Assert.NotEqual(key, value);
            Assert.False(string.IsNullOrWhiteSpace(value));
        }
    }
}
