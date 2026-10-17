using Jobsy.Web.Pwa;

namespace Jobsy.Tests;

public sealed class PwaInstallRulesTests
{
    private const long Now = 1_700_000_000_000;

    [Theory]
    [InlineData(true, false, false, false, false, false, false, true)]
    [InlineData(false, true, false, false, false, false, false, true)]
    [InlineData(false, false, false, false, true, false, false, true)]
    [InlineData(false, false, false, false, false, true, false, true)]
    [InlineData(false, false, false, true, false, false, false, true)]
    [InlineData(false, false, false, false, false, false, true, true)]
    [InlineData(false, false, false, false, false, false, false, false)]
    public void IsInstalledApp_detects_all_standalone_signals(
        bool standalone,
        bool minimalUi,
        bool fullscreen,
        bool navStandalone,
        bool androidReferrer,
        bool pwaStart,
        bool related,
        bool expected)
    {
        var actual = PwaInstallRules.IsInstalledApp(
            standalone,
            minimalUi,
            fullscreen,
            navStandalone,
            androidReferrer,
            pwaStart,
            related);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ShouldShowEntry_false_when_installed_or_dismissed()
    {
        var dismissedUntil = Now + 86_400_000;
        Assert.False(PwaInstallRules.ShouldShowEntry(true, null, PwaEntryPlatform.ChromePrompt, Now));
        Assert.False(PwaInstallRules.ShouldShowEntry(
            false,
            dismissedUntil,
            PwaEntryPlatform.ChromePrompt,
            Now));
        Assert.True(PwaInstallRules.ShouldShowEntry(
            false,
            dismissedUntil,
            PwaEntryPlatform.ChromePrompt,
            dismissedUntil + 1));
    }

    [Theory]
    [InlineData(true, false, false, true, PwaEntryPlatform.Ios)]
    [InlineData(false, false, false, true, PwaEntryPlatform.ChromePrompt)]
    [InlineData(false, true, true, false, PwaEntryPlatform.Samsung)]
    [InlineData(false, false, true, false, PwaEntryPlatform.Samsung)]
    [InlineData(false, false, false, false, PwaEntryPlatform.None)]
    public void DetectPlatform_maps_user_agent_context(
        bool ios,
        bool samsung,
        bool android,
        bool canPrompt,
        PwaEntryPlatform expected)
    {
        Assert.Equal(expected, PwaInstallRules.DetectPlatform(ios, samsung, android, canPrompt));
    }

    [Fact]
    public void MainLayout_suppresses_pwa_entry_on_share_in_and_questionnaires()
    {
        Assert.True(Jobsy.Web.Components.Layout.MainLayout.SuppressesPwaInstallEntry("candidate/share-in"));
        Assert.True(Jobsy.Web.Components.Layout.MainLayout.SuppressesPwaInstallEntry("candidate/ontdekkingsreis"));
        Assert.False(Jobsy.Web.Components.Layout.MainLayout.SuppressesPwaInstallEntry("candidate/applications"));
    }
}
