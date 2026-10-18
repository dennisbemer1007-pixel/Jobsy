using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Web.Admin;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public sealed class ContextualTipsFeatureTests
{
    [Fact]
    public void Flag_defaults_off_and_admin_copy_exists()
    {
        Assert.False(FeatureFlagSnapshot.Defaults.ContextualTipsEnabled);
        Assert.False(new FeatureFlagSnapshot(false, true).IsEnabled(PlatformFeature.ContextualTips));
        Assert.True(new FeatureFlagSnapshot(false, true, ContextualTipsEnabled: true)
            .IsEnabled(PlatformFeature.ContextualTips));

        var entry = PlatformSettingsCatalog.Entries.Single(item => item.Key == "ContextualTipsEnabled");
        Assert.False(entry.Read(new PlatformFeatureSnapshot(false, true, "http://localhost", null)) is true);
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var nav = UiStrings.Get("Nav.Tips", lang);
            var admin = UiStrings.Get("AdminSettings.ContextualTips.Enabled.Title", lang);
            Assert.False(string.IsNullOrWhiteSpace(nav));
            Assert.False(string.IsNullOrWhiteSpace(admin));
        }
    }

    [Fact]
    public void Tips_pages_use_requires_feature_gate()
    {
        var root = TestRepo.FindRoot();
        foreach (var page in new[]
                 {
                     "Jobsy.Web/Components/Pages/Candidate/ContextualTipsIndex.razor",
                     "Jobsy.Web/Components/Pages/Candidate/ContextualTipDetail.razor",
                     "Jobsy.Web/Components/Pages/Candidate/ApplicationInterviewPage.razor",
                     "Jobsy.Web/Components/Pages/Candidate/ApplicationOfferPage.razor"
                 })
        {
            var source = File.ReadAllText(Path.Combine(root, page));
            Assert.Contains("RequiresFeature(PlatformFeature.ContextualTips)", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Illustrated_stub_serves_contextual_tips_state()
    {
        var json = IllustratedPagesApiStub.BuildJson("/api/me/contextual-tips");
        Assert.Contains("momentTipsEnabled", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hiddenTipIds", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Discovery_nav_aliases_include_tips_hub()
    {
        Assert.Contains("/candidate/tips", RoleNavCatalog.DiscoveryItem.ExtraActivePaths ?? [], StringComparer.OrdinalIgnoreCase);
    }
}
