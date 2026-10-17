using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

public class PageAssetCatalogOnboardingTests
{
    [Fact]
    public void Candidate_start_includes_kb_and_onboarding_stylesheets()
    {
        var areas = PageAssetCatalog.ResolveAreas("/candidate/start");
        Assert.True(areas.HasFlag(PageAssetCatalog.Area.CandidateApp));
        Assert.True(areas.HasFlag(PageAssetCatalog.Area.CandidatePublic));

        var sheets = PageAssetCatalog.StylesheetsFor(areas);
        Assert.Contains(sheets, s => s.Contains("onboarding-wizard.css", StringComparison.Ordinal));
        Assert.Contains(sheets, s => s.Contains("onboarding-wizard-mobile-bar.css", StringComparison.Ordinal));
        Assert.Contains(sheets, s => s.Contains("onboarding-wizard-richting-a.css", StringComparison.Ordinal));
        Assert.Contains(sheets, s => s.Contains("kandidaat-banen.css", StringComparison.Ordinal));
        Assert.Contains(sheets, s => s.Contains("choice-tiles.css", StringComparison.Ordinal));
        Assert.Contains(sheets, s => s.Contains("form-fields.css", StringComparison.Ordinal));
    }
}
