using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Hosts must reference wwwroot css/js only via <see cref="Jobsy.Web.Hosting.WwwrootAssetCatalog"/>.
/// </summary>
public sealed class WwwrootAssetCatalogLiteralGuardTests
{
    private static readonly Regex LiteralWwwrootAsset = new(
        @"(?<path>(?:css|js)/[^\s""']+\.(?:css|js))\?v=",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedLiteralAssetPaths = new(StringComparer.Ordinal)
    {
        "_framework/blazor.web.js",
        "lib/maplibre/maplibre-gl.css",
        "lib/maplibre/maplibre-gl-csp.js",
    };

    private static readonly Regex DuplicateStylesheetFor = new(
        @"@foreach\s*\(\s*var\s+href\s+in\s+PageAssetCatalog\.StylesheetsFor",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static IEnumerable<string> HostFiles(string root)
    {
        yield return Path.Combine(root, "Jobsy.Web", "Hosting", "PageAssetCatalog.cs");
        foreach (var f in Directory.EnumerateFiles(
                     Path.Combine(root, "Jobsy.Web", "Hosting"),
                     "*.cs",
                     SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileName(f) is "WwwrootAssetCatalog.cs" or "VersionedAssetCacheMiddleware.cs")
            {
                continue;
            }

            yield return f;
        }

        foreach (var f in Directory.EnumerateFiles(
                     Path.Combine(root, "Jobsy.Web", "Components"),
                     "*.razor",
                     SearchOption.AllDirectories))
        {
            yield return f;
        }

        foreach (var f in new[]
                 {
                     Path.Combine(root, "Jobsy.Web", "Banenkaart", "BanenkaartSsrShells.cs"),
                     Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsSsrShells.cs"),
                     Path.Combine(root, "Jobsy.Web", "Discovery", "DiscoverySsrShells.cs"),
                 })
        {
            if (File.Exists(f))
            {
                yield return f;
            }
        }
    }

    [Fact]
    public void Razor_and_catalog_hosts_do_not_use_literal_css_js_v_query()
    {
        var root = TestRepo.FindRoot();
        var problems = new List<string>();
        foreach (var file in HostFiles(root))
        {
            var text = File.ReadAllText(file);
            var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            foreach (Match match in LiteralWwwrootAsset.Matches(text))
            {
                var path = match.Groups["path"].Value;
                if (AllowedLiteralAssetPaths.Contains(path))
                {
                    continue;
                }

                if (path.StartsWith("_framework/", StringComparison.Ordinal))
                {
                    continue;
                }

                problems.Add($"{rel}: literal ?v= on {path}");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void NoBlazor_SSR_pages_do_not_repeat_PageAssetCatalog_StylesheetsFor_in_HeadContent()
    {
        var root = TestRepo.FindRoot();
        var problems = new List<string>();
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(root, "Jobsy.Web", "Components"),
                     "*.razor",
                     SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("NoBlazorRuntime", StringComparison.Ordinal))
            {
                continue;
            }

            if (DuplicateStylesheetFor.IsMatch(text))
            {
                problems.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
