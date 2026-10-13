using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class PassportCandidateFlowTests
{
    [Fact]
    public void Split_keeps_mbo_level_without_the_parent_bucket()
    {
        Assert.Equal(["MBO 2"], EducationLevelLabels.Split("MBO 2"));
        Assert.Equal(["MBO 2"], EducationLevelLabels.Split("MBO 2 Logistiek"));
        Assert.DoesNotContain("MBO", EducationLevelLabels.Split("MBO 2"));
    }

    [Fact]
    public void Profile_editor_does_not_match_education_by_substring()
    {
        var source = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Candidate", "ProfileSections", "CandidateProfileEditor.cs"));
        Assert.DoesNotContain("education.Contains(level", source, StringComparison.Ordinal);
        Assert.Contains("EducationDirection", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 3)]
    [InlineData(67, 4)]
    [InlineData(100, 6)]
    public void Filled_steps_follow_the_six_step_profile(int percent, int expected)
        => Assert.Equal(expected, KompasProfileCompleteness.FilledSteps(percent));

    [Fact]
    public void Lobster_stays_in_the_egg_only_before_any_test()
    {
        Assert.Equal(0, PassportShellRules.LobsterStage(0, anyTestStarted: false));
        Assert.Equal(1, PassportShellRules.LobsterStage(0, anyTestStarted: true));
        Assert.Equal(4, PassportShellRules.LobsterStage(4, anyTestStarted: true));
    }

    [Theory]
    [InlineData("match", "fit")]
    [InlineData("MATCH", "fit")]
    public void Match_tab_opens_fit(string raw, string expected)
        => Assert.Equal(expected, PassportTabs.Normalize(raw));

    [Fact]
    public void Fit_tab_is_hidden_while_employers_are_off()
    {
        Assert.DoesNotContain(PassportTabs.Fit, PassportTabs.Visible(employersEnabled: false));
        Assert.Contains(PassportTabs.Fit, PassportTabs.Visible(employersEnabled: true));
        Assert.Equal(PassportTabs.Career, PassportTabs.Neighbor(PassportTabs.Tests, 1, employersEnabled: false));
    }

    [Fact]
    public void Passport_refetches_kompas_instead_of_keeping_the_persisted_snapshot()
    {
        var source = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Pages", "Candidate", "Passport.razor"));
        Assert.DoesNotContain("if (_dna is null)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("if (_kompas is null)", source, StringComparison.Ordinal);
        Assert.Contains("GetMyKompasResultAsync", source, StringComparison.Ordinal);
        Assert.Contains("Passport.Fit.Soon", source, StringComparison.Ordinal);
        Assert.Contains("TestsComplete", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Story_pending_and_fit_soon_are_translated(string language)
    {
        var pending = UiStrings.Get("Dna.StoryPending", language);
        var soon = UiStrings.Get("Passport.Fit.Soon", language);
        Assert.DoesNotContain("Dna.StoryPending", pending, StringComparison.Ordinal);
        Assert.DoesNotContain("Passport.Fit.Soon", soon, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(pending));
        Assert.False(string.IsNullOrWhiteSpace(soon));
    }

    [Fact]
    public void Header_uses_the_saved_profile_name()
    {
        string? seen = null;
        void Handler(string name) => seen = name;
        CandidateNameBroadcast.Changed += Handler;
        try
        {
            CandidateNameBroadcast.Publish("Sanne", "de Vries");
            Assert.Equal("Sanne de Vries", seen);
        }
        finally
        {
            CandidateNameBroadcast.Changed -= Handler;
        }
    }
}
