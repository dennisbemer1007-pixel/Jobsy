namespace Jobsy.Tests;

public sealed class UwvAdviseurDeelSsrTests
{
    [Fact]
    public void Deel_page_skips_blazor_runtime()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Public/UwvAdviseurDeel.razor"));
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IJSRuntime", source, StringComparison.Ordinal);
        Assert.Contains("data-uwv-adviseur-print", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PublicLangMenu", source, StringComparison.Ordinal);
        Assert.Contains("pub-btn pub-btn--primary", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_layout_hides_lang_menu_on_uwv_deel_route()
    {
        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/PublicLayout.razor"));
        Assert.Contains("HideLangMenu", layout, StringComparison.Ordinal);
        Assert.Contains("/deel/uwv-adviseur/", layout, StringComparison.Ordinal);
        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/Public/PublicHeader.razor"));
        Assert.Contains("pub-header--no-lang", header, StringComparison.Ordinal);
    }

    [Fact]
    public void Share_document_html_includes_ondernemer_section_when_requested()
    {
        var catalog = new StubKansberoepenCatalog();
        var html = Jobsy.Web.UwvMockups.UwvAdviseurShareDocumentHtml.Build(
            "nl",
            "Test Kandidaat",
            [Jobsy.Core.Uwv.UwvAdviseurSectionKeys.Career],
            "explore",
            [],
            [],
            catalog);
        Assert.Contains("Ondernemerscheck", html, StringComparison.Ordinal);
        Assert.Contains("Test Kandidaat", html, StringComparison.Ordinal);
        Assert.Contains("lobsy-logo-horizontaal.svg", html, StringComparison.Ordinal);
        Assert.Contains("lobsy-logo--horizontal", html, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class StubKansberoepenCatalog : Jobsy.Web.UwvFeatures.IUwvKansberoepenCatalog
    {
        public Jobsy.Core.Uwv.UwvKansberoepenDataset GetDefault() =>
            new("stub", "regio", "region", "Bron NL", "Source EN", []);
    }
}
