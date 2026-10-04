using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class CandidateJourneyPolishTests
{
    [Fact]
    public void Addable_languages_omit_dutch()
    {
        Assert.DoesNotContain("nl", DiscoveryCatalogs.AddableSpokenLanguageCodes);
        Assert.Contains("en", DiscoveryCatalogs.AddableSpokenLanguageCodes);
        Assert.Contains("nl", DiscoveryCatalogs.SpokenLanguageCodes);
    }

    [Fact]
    public void Competence_slug_canonicalizes_the_dutch_alias()
    {
        Assert.True(AssessmentKindLabels.TryParse("competentie", out var kind));
        Assert.Equal(AssessmentKind.Competence, kind);
        Assert.Equal("competence", AssessmentKindLabels.ToSlug(kind));
    }

    [Fact]
    public void Dialog_closes_on_escape_and_months_are_not_browser_locale_inputs()
    {
        var root = TestRepo.FindRoot();
        var dialog = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "LobsyFriendlyDialog.razor"));
        Assert.Contains("e.Key == \"Escape\"", dialog, StringComparison.Ordinal);

        var history = File.ReadAllText(Path.Combine(
            root, "Jobsy.Web", "Components", "Candidate", "Onboarding", "WorkHistoryStep.razor"));
        Assert.DoesNotContain("type=\"month\"", history, StringComparison.Ordinal);
        Assert.Contains("Profile.CurrentJob", history, StringComparison.Ordinal);

        var mfa = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Account", "MfaPrompt.razor"));
        Assert.Contains("Mfa.AlreadySignedIn", mfa, StringComparison.Ordinal);
        Assert.Contains("IsAuthenticated", mfa, StringComparison.Ordinal);
    }

    [Fact]
    public void Current_job_and_signed_in_mfa_copy_exist()
    {
        foreach (var language in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("Profile.CurrentJob", language)));
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("Mfa.AlreadySignedIn", language)));
            Assert.DoesNotContain("PL:", UiStrings.Get("Profile.CurrentJob", language), StringComparison.Ordinal);
        }

        Assert.Equal("Dit is mijn huidige baan", UiStrings.Get("Profile.CurrentJob", "nl"));
    }
}
