using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Public pages under PublicLayout / Components/Public must use LobsyMascot only —
/// no direct BrandImages.Mascot* or mascot-*.webp|png references.
/// </summary>
public class PublicMascotUsageGuardTests
{
    private static readonly Regex DirectMascotFile = new(
        @"mascot-\d+\.(?:webp|png)|mascot-[a-z]+(?:-\d+)?\.(?:webp|png|svg|avif)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BrandImagesMascot = new(
        @"BrandImages\.Mascot",
        RegexOptions.Compiled);

    [Fact]
    public void PublicLayout_pages_and_Public_components_use_LobsyMascot_only()
    {
        var root = TestRepo.FindRoot();
        var web = Path.Combine(root, "Jobsy.Web");
        var offenders = new List<string>();

        foreach (var file in EnumeratePublicRazor(web))
        {
            var text = File.ReadAllText(file);
            var rel = Path.GetRelativePath(root, file).Replace('\\', '/');

            // LobsyMascot itself and the Brand folder are the allowed implementation.
            if (rel.Contains("/Components/Brand/", StringComparison.Ordinal))
            {
                continue;
            }

            if (BrandImagesMascot.IsMatch(text))
            {
                offenders.Add($"{rel}: BrandImages.Mascot*");
            }

            // Allow comments / docs mentioning the contract path images/brand/mascot/
            foreach (Match m in DirectMascotFile.Matches(text))
            {
                // Skip if it's only inside a comment-like @* *@ block — still flag raw markup/src.
                offenders.Add($"{rel}: direct asset '{m.Value}'");
            }
        }

        Assert.True(offenders.Count == 0,
            "Public surfaces must use <LobsyMascot />, not direct mascot assets:\n" +
            string.Join('\n', offenders));
    }

    private static IEnumerable<string> EnumeratePublicRazor(string webRoot)
    {
        var pages = Path.Combine(webRoot, "Components", "Pages");
        if (Directory.Exists(pages))
        {
            foreach (var file in Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                if (text.Contains("@layout PublicLayout", StringComparison.Ordinal)
                    || text.Contains("Layout = typeof(PublicLayout)", StringComparison.Ordinal))
                {
                    yield return file;
                }
            }
        }

        var publicComponents = Path.Combine(webRoot, "Components", "Public");
        if (Directory.Exists(publicComponents))
        {
            foreach (var file in Directory.EnumerateFiles(publicComponents, "*.razor", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        var layoutPublic = Path.Combine(webRoot, "Components", "Layout", "Public");
        if (Directory.Exists(layoutPublic))
        {
            foreach (var file in Directory.EnumerateFiles(layoutPublic, "*.razor", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        var publicLayout = Path.Combine(webRoot, "Components", "Layout", "PublicLayout.razor");
        if (File.Exists(publicLayout))
        {
            yield return publicLayout;
        }
    }
}
