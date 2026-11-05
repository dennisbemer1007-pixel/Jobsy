using Jobsy.Web.Hosting;

namespace Jobsy.Tests.Scholen;

public sealed class LeerlingMboHboThemeCssCatalogTests
{
    [Theory]
    [InlineData("/leerling/mbo-hbo/uitleg")]
    [InlineData("/leerling/mbo-hbo/overzicht")]
    [InlineData("/leerling/mbo-hbo/test/interesse")]
    public void RouteStylesheets_includes_mbo_hbo_theme_css(string path)
    {
        Assert.True(PageAssetCatalog.IncludesLeerlingMboHboStyles(path));
        var sheets = PageAssetCatalog.RouteStylesheets(path);
        var sheet = Assert.Single(sheets, s =>
            s.Contains("leerling-mbo-hbo.css", StringComparison.Ordinal));
        Assert.StartsWith("/", sheet, StringComparison.Ordinal);
    }
}

public sealed class PupilMboHboSsrHtmlTests
{
    [Fact]
    public void Mbo_hbo_ssr_includes_g78_parity_markers()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var html = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilMboHboSsrHtml.cs"));
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PupilLeerlingJourneyEndpoints.cs"));
        var catalog = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PageAssetCatalog.MboHbo.cs"));
        var client = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Services/ApiClient/JobsyApiClient.PupilMboHbo.cs"));

        Assert.Contains("UrlLeadingSlash(LeerlingMboHboStylesheetPath)", catalog, StringComparison.Ordinal);
        Assert.Contains("Groep78SpeakButton", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"mhbo-prev\"", html, StringComparison.Ordinal);
        Assert.Contains("mhbo-download-pdf", html, StringComparison.Ordinal);
        Assert.Contains("Leerling.Reis.RateLimited", endpoints, StringComparison.Ordinal);
        Assert.Contains("PupilMboHboWebProgressCache", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetMboHboQuestionResponseAsync", client, StringComparison.Ordinal);
        Assert.Contains("StatusCode", client, StringComparison.Ordinal);
    }

    [Fact]
    public void Playwright_mbo_hbo_console_smoke_test_exists()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var tests = File.ReadAllText(Path.Combine(root, "Jobsy.Tests/Scholen/PupilCircuitPlaywrightTests.cs"));
        Assert.Contains("Mbo_hbo_pupil_ssr_pages_have_no_console_errors", tests, StringComparison.Ordinal);
        Assert.Contains("jobsy-dom-safe.js", tests, StringComparison.Ordinal);
    }
}
