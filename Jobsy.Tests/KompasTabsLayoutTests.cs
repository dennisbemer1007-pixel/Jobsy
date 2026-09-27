namespace Jobsy.Tests;

/// <summary>Guards Acc 27-09 §2: Kompas tab ribbon must not float on mobile.</summary>
public class KompasTabsLayoutTests
{
    [Fact]
    public void Kompas_tabs_are_static_below_1024px()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains("@media (max-width: 1023px)", css, StringComparison.Ordinal);
        Assert.Contains(".kompas-tabs.admin-sublinks", css, StringComparison.Ordinal);
        Assert.Contains("position: static", css, StringComparison.Ordinal);

        var min = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.min.css"));
        Assert.Contains("position:static", min, StringComparison.Ordinal);
        Assert.Contains("kompas-tabs.admin-sublinks", min, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
