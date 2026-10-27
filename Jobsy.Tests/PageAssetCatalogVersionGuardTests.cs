using System.Text.RegularExpressions;
using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

public sealed class PageAssetCatalogVersionGuardTests
{
    private static readonly Regex CatalogUrl = new(
        @"WwwrootAssetCatalog\.Url(?:LeadingSlash)?\(\s*""([^""]+)""\s*\)",
        RegexOptions.Compiled);

    [Fact]
    public void Every_PageAssetCatalog_wwwroot_ref_has_manifest_version()
    {
        var catalogPath = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "PageAssetCatalog.cs");
        var text = File.ReadAllText(catalogPath);
        var paths = CatalogUrl.Matches(text).Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal).OrderBy(p => p).ToList();
        Assert.NotEmpty(paths);

        var missing = new List<string>();
        foreach (var path in paths)
        {
            var physical = Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", path.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physical), $"Catalog asset missing on disk: {path}");
            if (string.IsNullOrWhiteSpace(WwwrootAssetCatalog.Version(path)))
            {
                missing.Add(path);
            }
        }

        Assert.True(
            missing.Count == 0,
            "Add versions via python3 Jobsy.Tests/update-asset-versions.py: " + string.Join(", ", missing));
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
