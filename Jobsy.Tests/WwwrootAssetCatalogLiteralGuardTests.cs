using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Kreeft asset SSOT: paths served via <see cref="Jobsy.Web.Hosting.WwwrootAssetCatalog"/>
/// must not also appear as literal <c>?v=</c> strings in PageAssetCatalog or Razor hosts.
/// </summary>
public sealed class WwwrootAssetCatalogLiteralGuardTests
{
    private static readonly string[] CatalogManagedPaths =
    [
        "css/app.min.css",
        "css/features/banenkaart-auth-chrome.css",
        "css/features/candidate-no-blazor-chrome.css",
        "css/features/candidate-header-strip.css",
        "css/features/errors.css",
        "css/features/kreeft-reiskaart.css",
        "css/features/lobsy-assistant-panel.css",
        "css/features/lobsy-coach-pill.css",
        "css/features/onboarding-kreeft-mockup.css",
        "css/features/onboarding-start-strand.css",
        "js/lobsy-coach-ssr.js",
    ];

    [Fact]
    public void PageAssetCatalog_and_razor_do_not_use_literal_v_for_catalog_managed_assets()
    {
        var root = TestRepo.FindRoot();
        var files = new List<string>
        {
            Path.Combine(root, "Jobsy.Web", "Hosting", "PageAssetCatalog.cs"),
        };
        files.AddRange(Directory.EnumerateFiles(
            Path.Combine(root, "Jobsy.Web", "Components"),
            "*.razor",
            SearchOption.AllDirectories));

        var problems = new List<string>();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            foreach (var path in CatalogManagedPaths)
            {
                var pattern = Regex.Escape(path) + @"\?v=";
                if (Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant))
                {
                    problems.Add($"{rel}: literal ?v= for {path}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
