using System.Net;

namespace Jobsy.Tests;

[Collection("SequentialApi")]
public sealed class WerkgeverApplicationsListSsrRenderTests
{
    [Fact]
    public async Task List_view_includes_werkgever_stylesheets_in_head()
    {
        await using var web = new IllustratedPagesWebFactory();
        using var handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(web.ServerAddress) };
        var path = "/werkgever/sollicitaties?view=lijst";
        await http.GetAsync($"/__test/sign-in?role=employer&return={Uri.EscapeDataString(path)}");
        var response = await http.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-noblazor-route-css", html, StringComparison.Ordinal);
        Assert.Contains("css/features/werkgever.css", html, StringComparison.Ordinal);
        Assert.Contains("css/features/enterprise-ui.css", html, StringComparison.Ordinal);
        Assert.Contains("css/features/notifications.css", html, StringComparison.Ordinal);
        Assert.Contains("wg-applications-list-ssr.js", html, StringComparison.Ordinal);
        Assert.Contains("data-wg-list-ssr=\"1\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void List_ssr_shell_has_no_inline_event_handlers()
    {
        var shells = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Werkgever/EmployerApplicationsListSsrShells.cs"));
        Assert.DoesNotContain("onchange=", shells, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-wg-list-filter-form", shells, StringComparison.Ordinal);
    }

    [Fact]
    public void New_ssr_shells_avoid_inline_script_event_handlers()
    {
        var root = FindRepoRoot();
        var offenders = Directory
            .EnumerateFiles(Path.Combine(root, "Jobsy.Web"), "*Ssr*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "Jobsy.Web/Werkgever"), "*.cs", SearchOption.AllDirectories))
            .Select(path => (path, text: File.ReadAllText(path)))
            .Where(t => t.text.Contains("onchange=", StringComparison.OrdinalIgnoreCase)
                        || t.text.Contains("onclick=", StringComparison.OrdinalIgnoreCase))
            .Select(t => Path.GetRelativePath(root, t.path))
            .Distinct()
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        Assert.True(offenders.Count == 0, "Inline handlers (CSP script-src-attr): " + string.Join(", ", offenders));
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
