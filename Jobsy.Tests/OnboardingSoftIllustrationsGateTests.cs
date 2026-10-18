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

    [Theory]
    [InlineData("kennismaken")]
    [InlineData("beschikbaarheid")]
    [InlineData("vervoer")]
    [InlineData("werkervaring")]
    [InlineData("opleiding")]
    [InlineData("droombaan")]
    public void Mobile_figure_crop_assets_exist(string slug)
    {
        var webRoot = Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/images/onboarding");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}-mobile-bg.webp")), $"{slug}-mobile-bg.webp");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}-mobile-bg.png")), $"{slug}-mobile-bg.png");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}-mobile.webp")), $"{slug}-mobile.webp");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}-mobile.png")), $"{slug}-mobile.png");
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
    public void Preprocess_script_exists_for_hero_seam_assets()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "tools/images/preprocess-onboarding-hero-seam.py")));
        var assets = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Onboarding/OnboardingIllustrationAssets.cs"));
        Assert.Contains("20261018-r10", assets, StringComparison.Ordinal);
    }

    [Fact]
    public void Transport_step_shows_vervoer_label_when_illustrated()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Candidate/Onboarding/TransportStep.razor"));
        Assert.Contains("data-testid=\"onboarding-transport-heading\"", src, StringComparison.Ordinal);
        Assert.Contains("Onboarding.Transport", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Hero_band_css_uses_desktop_seam_art_and_full_bleed_mobile_cover_below_768()
    {
        var css = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/css/features/onboarding-illustrated-step.css"));
        var step = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Candidate/Onboarding/OnboardingIllustratedStep.razor"));
        Assert.Contains("onboarding-mobile-hero", step, StringComparison.Ordinal);
        Assert.Contains("object-fit: contain", css, StringComparison.Ordinal);
        Assert.Contains("object-fit: cover", css, StringComparison.Ordinal);
        Assert.Matches(
            @"(?s)@media \(max-width: 767px\).*\.ob-illustrated-step__hero-band\s*\{[^}]*display:\s*none",
            css);
        Assert.Matches(
            @"(?s)@media \(max-width: 767px\).*\.ob-illustrated-step__mobile-cover\s*\{[^}]*display:\s*grid",
            css);
        Assert.True(File.Exists(Path.Combine(RepoRoot, "tools/images/preprocess-onboarding-mobile-r15.py")));
        Assert.Matches(
            @"(?s)\.ob-illustrated-step__mobile-cover-copy h1\s*\{[^}]*max-inline-size:\s*50%",
            css);
    }

    [Fact]
    public void Mobile_figure_preprocess_script_exists()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "tools/images/preprocess-onboarding-mobile-figure-crop.py")));
        Assert.True(File.Exists(Path.Combine(RepoRoot, "tools/images/preprocess-onboarding-mobile-r13.py")));
        var assets = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Onboarding/OnboardingIllustrationAssets.cs"));
        Assert.Contains("20261018-r18", assets, StringComparison.Ordinal);
        Assert.Contains("MobileHeroWebp", assets, StringComparison.Ordinal);
        Assert.Contains("mobile-hero", assets, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("kennismaken")]
    [InlineData("beschikbaarheid")]
    [InlineData("vervoer")]
    [InlineData("werkervaring")]
    [InlineData("opleiding")]
    [InlineData("droombaan")]
    public void Mobile_hero_full_bleed_assets_exist(string slug)
    {
        var webRoot = Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/images/onboarding");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}-mobile-hero.webp")), $"{slug}-mobile-hero.webp");
        Assert.True(File.Exists(Path.Combine(webRoot, $"{slug}-mobile-hero.jpg")), $"{slug}-mobile-hero.jpg");
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
