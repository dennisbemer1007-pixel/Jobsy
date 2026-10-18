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
}
