using System.Text.RegularExpressions;

namespace Jobsy.Tests;

public class GratisDnaScriptGateTests
{
    [Theory]
    [InlineData("/ontdek")]
    [InlineData("/dna")]
    [InlineData("/gratis-dna")]
    public void Public_gratis_dna_routes_include_gratis_dna_script(string path)
    {
        Assert.True(Jobsy.Web.Hosting.PageAssetCatalog.IncludeGratisDnaScript(path));
    }

    [Fact]
    public void GratisDna_razor_routes_are_covered_by_IncludeGratisDnaScript()
    {
        var razor = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Pages", "Public", "GratisDna.razor"));
        var routes = Regex.Matches(razor, @"@page\s+""([^""]+)""", RegexOptions.Multiline)
            .Select(m => Jobsy.Web.Seo.PageSeoCatalog.Normalize(m.Groups[1].Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Assert.NotEmpty(routes);
        foreach (var route in routes)
        {
            Assert.True(
                Jobsy.Web.Hosting.PageAssetCatalog.IncludeGratisDnaScript(route),
                $"Route {route} must ship js/gratis-dna.js (IncludeGratisDnaScript).");
        }
    }

    [Fact]
    public void App_core_exposes_jobsyEnsureGratisDna_for_blazor_interop()
    {
        var core = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("window.jobsyEnsureGratisDna", core, StringComparison.Ordinal);
    }

    [Fact]
    public void Gratis_dna_module_registers_jobsyEnsureGratisDna()
    {
        var js = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "wwwroot", "js", "gratis-dna.js"));
        Assert.Contains("window.jobsyEnsureGratisDna", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_question_nav_is_not_hidden()
    {
        var css = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "wwwroot", "css", "features", "gratis-dna.css"));
        Assert.DoesNotContain(".gd-mnav{display:none}", css, StringComparison.Ordinal);
    }
}
