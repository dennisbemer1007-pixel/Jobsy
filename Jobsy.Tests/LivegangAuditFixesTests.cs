using System.Net;
using System.Text.RegularExpressions;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobsy.Tests;

public class LivegangAuditFixesTests
{
    [Fact]
    public void Codebase_does_not_use_minutes_token_in_dutch_date_format_strings()
    {
        var root = FindRepoRoot();
        var pattern = new Regex("""ToString\s*\(\s*"dd-mm-yyyy""");
        var scanRoots = new[] { "Jobsy.Web", "Jobsy.Api", "Jobsy.Infrastructure", "Jobsy.Core" };
        foreach (var scanRoot in scanRoots)
        {
            var dir = Path.Combine(root, scanRoot);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                         .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                                     || p.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                         .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                     && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            {
                var text = File.ReadAllText(file);
                Assert.False(pattern.IsMatch(text), $"Invalid date format (mm = minutes) in {file}");
            }
        }
    }

    [Fact]
    public void Logging_admin_renders_dates_with_month_MM_not_minutes_mm()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Admin", "LoggingAdmin.razor");
        var text = File.ReadAllText(path);
        Assert.Contains("ToString(\"dd-MM-yyyy HH:mm\")", text, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex("""ToString\s*\(\s*"dd-mm-yyyy"""), text);
    }

    [Fact]
    public async Task Notifications_page_returns_ok_for_anonymous_prerender()
    {
        await using var factory = new PublicPagesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/notifications");
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Found or HttpStatusCode.Unauthorized,
            $"/notifications returned {(int)response.StatusCode} (must not be 404)");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Werkgever_page_assets_include_choice_tiles_and_notifications_css()
    {
        var catalog = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "PageAssetCatalog.cs"));
        Assert.Contains("WerkgeverCss", catalog, StringComparison.Ordinal);
        Assert.Contains("choice-tiles.css", catalog, StringComparison.Ordinal);
        Assert.Contains("notifications.css", catalog, StringComparison.Ordinal);
        var sheets = PageAssetCatalog.StylesheetsFor(PageAssetCatalog.Area.Werkgever);
        Assert.Contains(sheets, s => s.Contains("choice-tiles.css", StringComparison.Ordinal));
        var notifSheets = PageAssetCatalog.StylesheetsFor(PageAssetCatalog.Area.CandidateApp);
        Assert.Contains(notifSheets, s => s.Contains("notifications.css", StringComparison.Ordinal));
    }

    [Fact]
    public void Werkgever_vacatures_desktop_css_clips_table_overflow()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "werkgever.css"));
        Assert.Contains(".wg-vac--dense .ent-data-table-wrap", css, StringComparison.Ordinal);
        Assert.Contains("table-layout: auto", css, StringComparison.Ordinal);
        Assert.Contains("wg-vac__col-views", css, StringComparison.Ordinal);
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
