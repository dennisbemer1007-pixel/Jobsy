using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public sealed class OnboardingNearbyPanelTests
{
    [Fact]
    public void Panel_component_has_no_navigation_or_apply_actions()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Components",
            "Candidate",
            "Onboarding",
            "OnboardingNearbyPanel.razor");
        path = Path.GetFullPath(path);
        var src = File.ReadAllText(path);

        Assert.DoesNotContain("<a ", src, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=", src, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("KbRoutes", src, StringComparison.Ordinal);
        Assert.DoesNotContain("banenkaart", src, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wizard_no_longer_mounts_full_page_nearby_early_reward()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Components",
            "Pages",
            "Candidate",
            "OnboardingWizard.razor"));
        var src = File.ReadAllText(path);

        Assert.DoesNotContain("EarlyRewardNearbyStep", src, StringComparison.Ordinal);
        Assert.DoesNotContain("EarlyRewardMoment.Nearby", src, StringComparison.Ordinal);
        Assert.Contains("OnboardingNearbyPanel", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_nearby_route_redirects_to_start_with_query()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Web",
            "Components",
            "Pages",
            "Candidate",
            "OnboardingStartNearbyRedirect.razor"));
        var src = File.ReadAllText(path);

        Assert.Contains("@page \"/candidate/start/nearby\"", src, StringComparison.Ordinal);
        Assert.Contains("from=nearby", src, StringComparison.Ordinal);
    }
}
