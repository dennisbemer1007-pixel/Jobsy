using System.Net;

namespace Jobsy.Tests;

[Collection("BanenkaartPlaywright")]
public sealed class BedrijvenkaartSsrRenderTests
{
    [Fact]
    public async Task Bedrijvenkaart_ssr_includes_stylesheets_and_root_marker()
    {
        await using var web = new IllustratedPagesWebFactory();
        using var http = new HttpClient { BaseAddress = new Uri(web.ServerAddress) };
        var response = await http.GetAsync("/bedrijvenkaart/ssr");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-bedrijvenkaart-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("css/features/bedrijvenkaart.css", html, StringComparison.Ordinal);
        Assert.Contains("css/features/banenkaart-page.css", html, StringComparison.Ordinal);
        Assert.Contains("css/features/kandidaat-banen.css", html, StringComparison.Ordinal);
        Assert.Contains("bedrijvenkaart-ssr-map.js", html, StringComparison.Ordinal);
        Assert.Contains("id=\"bedrijvenkaart-ssr-pins\"", html, StringComparison.Ordinal);
        Assert.Contains("bk-shell", html, StringComparison.Ordinal);
        Assert.Contains("bedrijvenkaart-map", html, StringComparison.Ordinal);
        Assert.Contains("data-bk-view-toggle", html, StringComparison.Ordinal);
        Assert.Contains("bedrijvenkaart-ssr-layout.js", html, StringComparison.Ordinal);
        Assert.Contains("bk-shell--desktop-split", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preview_ssr_ok_when_company_map_initiative_off()
    {
        await using var web = new IllustratedPagesWebFactory(companyMapInitiativeEnabled: false);
        using var http = new HttpClient { BaseAddress = new Uri(web.ServerAddress) };
        var response = await http.GetAsync("/bedrijvenkaart/ssr");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-bedrijvenkaart-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("bk-shell", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Main_bedrijvenkaart_route_stays_on_blazor_discovery()
    {
        await using var web = new IllustratedPagesWebFactory();
        using var http = new HttpClient { BaseAddress = new Uri(web.ServerAddress) };
        var response = await http.GetAsync("/bedrijvenkaart");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("data-bedrijvenkaart-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("bk-page", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Bedrijvenkaart_ssr_shell_has_no_inline_event_handlers()
    {
        var shells = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Bedrijvenkaart/BedrijvenkaartSsrShells.cs"));
        Assert.DoesNotContain("onclick=", shells, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onchange=", shells, StringComparison.OrdinalIgnoreCase);
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
}
