namespace Jobsy.Tests;

public sealed class CandidateHeaderStripHeightTests
{
    [Fact]
    public void Shared_css_defines_mockup_strip_heights_and_cover()
    {
        var root = TestRepo.FindRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/candidate-header-strip.css"));
        Assert.Contains("--cand-header-strip-h-desktop: 225px", css, StringComparison.Ordinal);
        Assert.Contains("--cand-header-strip-h-mobile: 105px", css, StringComparison.Ordinal);
        Assert.Contains("object-fit: cover", css, StringComparison.Ordinal);
        Assert.Contains(".sollicitaties-hero-strip", css, StringComparison.Ordinal);
        Assert.Contains(".cand-hero", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Blazor_core_and_applications_route_load_shared_strip_css()
    {
        Assert.Contains(
            "css/features/candidate-header-strip.css?v=20261025-cand-strip3",
            Jobsy.Web.Hosting.PageAssetCatalog.BlazorCoreCss,
            StringComparer.Ordinal);
        var appsCss = Jobsy.Web.Hosting.PageAssetCatalog.RouteStylesheets("/candidate/applications");
        Assert.Equal(
            "css/features/candidate-header-strip.css?v=20261025-cand-strip3",
            appsCss[0]);
    }

    [Fact]
    public void Sollicitaties_and_banenkaart_strip_styles_use_shared_variables()
    {
        var root = TestRepo.FindRoot();
        var sollic = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/sollicitaties-page.css"));
        var kb = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/banenkaart-illustrated.css"));
        Assert.Contains("--cand-header-strip-h-desktop", sollic, StringComparison.Ordinal);
        Assert.Contains("--cand-header-strip-h-mobile", sollic, StringComparison.Ordinal);
        Assert.Contains("--cand-header-strip-h-desktop", kb, StringComparison.Ordinal);
        Assert.Contains("--cand-header-strip-h-mobile", kb, StringComparison.Ordinal);
    }

    [Fact]
    public void Sollicitaties_mobile_override_targets_wrap_selector()
    {
        var root = TestRepo.FindRoot();
        var sollic = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/sollicitaties-page.css"));
        Assert.Contains(
            ".sollicitaties-page-wrap .sollicitaties-hero-strip",
            sollic,
            StringComparison.Ordinal);
        Assert.Contains("max-width: 639px", sollic, StringComparison.Ordinal);
    }

    [Fact]
    public void Kompas_illustrated_frame_uses_shared_strip_tokens()
    {
        var root = TestRepo.FindRoot();
        var rustig = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/onboarding-rustig.css"));
        Assert.Contains("data-ob-rustig-screen=\"kompas\"]", rustig, StringComparison.Ordinal);
        Assert.Contains("--cand-header-strip-h-desktop", rustig, StringComparison.Ordinal);
        Assert.Contains("--cand-header-strip-h-mobile", rustig, StringComparison.Ordinal);
    }
}
