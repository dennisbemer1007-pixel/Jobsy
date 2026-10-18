namespace Jobsy.Tests;

public sealed class OnboardingSoftIllustrationsGateTests
{
    private static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Theory]
    [InlineData("Jobsy.Web/Components/Candidate/Onboarding/OnboardingIllustratedStep.razor")]
    [InlineData("Jobsy.Web/wwwroot/css/features/onboarding-illustrated-step.css")]
    [InlineData("Jobsy.Web/Onboarding/OnboardingIllustrationAssets.cs")]
    public void Required_files_exist(string relativePath)
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, relativePath)), relativePath);
    }

    [Fact]
    public void Wizard_wires_soft_illustrations_for_steps_1_through_5()
    {
        var wizard = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("onboarding-step-1-illustrated", wizard, StringComparison.Ordinal);
        Assert.Contains("onboarding-step-5-illustrated", wizard, StringComparison.Ordinal);
        Assert.Contains("OnboardingIllustrationAssets.KennismakenWebp", wizard, StringComparison.Ordinal);
        Assert.Contains("ob-wizard__content--illustrated", wizard, StringComparison.Ordinal);
    }

    [Fact]
    public void Sidebar_uses_page_background_token()
    {
        var css = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/css/features/onboarding-wizard.css"));
        Assert.Contains(".ob-wizard__sidebar{", css, StringComparison.Ordinal);
        Assert.Matches(@"(?s)\.ob-wizard__sidebar\{[^}]*background:var\(--bg\)", css);
    }

    [Theory]
    [InlineData("kennismaken")]
    [InlineData("beschikbaarheid")]
    [InlineData("vervoer")]
    [InlineData("werkervaring")]
    [InlineData("opleiding")]
    [InlineData("droombaan")]
    public void Webp_and_jpg_assets_exist(string slug)
    {
        var webRoot = Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/images/onboarding");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}.webp")), $"{slug}.webp");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}.jpg")), $"{slug}.jpg");
    }

    [Fact]
    public void Illustrated_sidebar_tips_avoid_em_dash_in_dutch()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Localization/UiStringsOnboardingV2.cs"));
        for (var step = 1; step <= 6; step++)
        {
            var key = $"Onboarding.IllustratedSidebar.Step{step}";
            var idx = src.IndexOf(key, StringComparison.Ordinal);
            Assert.True(idx >= 0, key);
            var slice = src[idx..Math.Min(src.Length, idx + 280)];
            Assert.DoesNotContain(" — ", slice);
            Assert.DoesNotContain("Denk groot —", slice, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Hero_band_css_uses_seamless_art_with_fade_and_mobile_scene_band()
    {
        var css = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/css/features/onboarding-illustrated-step.css"));
        Assert.Contains("object-fit: contain", css, StringComparison.Ordinal);
        Assert.Contains("mask-image: linear-gradient(to right, transparent 0, #000 100px)", css, StringComparison.Ordinal);
        Assert.Contains("block-size: 120px", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Nearby_panel_footnote_has_no_em_dash_in_dutch()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Localization/UiStringsOnboardingV2.cs"));
        var idx = src.IndexOf("Onboarding.Panel.Nearby.Footnote", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var slice = src[idx..Math.Min(src.Length, idx + 320)];
        Assert.Contains("Voorbeelden ter inspiratie.", slice, StringComparison.Ordinal);
        Assert.DoesNotContain(" — voorbeelden", slice, StringComparison.OrdinalIgnoreCase);
    }
}
