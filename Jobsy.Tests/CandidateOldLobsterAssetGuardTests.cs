using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Candidate-facing UI, public pages, and email templates must not reference the legacy playful mascot raster (mascot-64/128/256).
/// Logo-E uses <see cref="Jobsy.Web.Media.BrandImages"/> marks or lobsy-* assets instead.
/// </summary>
public class CandidateOldLobsterAssetGuardTests
{
    private static readonly Regex LegacyPlayfulMascot = new(
        @"mascot-(?:64|128|256)\.(?:webp|png)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LegacyEmailMascot = new(
        @"mascot-celebrating-128\.png",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void Candidate_razor_has_no_legacy_playful_mascot_assets()
        => AssertNoLegacyInRazorDirs(
            Path.Combine("Components", "Candidate"),
            Path.Combine("Components", "Pages", "Candidate"));

    [Fact]
    public void Public_brand_razor_has_no_legacy_playful_mascot_assets()
        => AssertNoLegacyInRazorDirs(
            Path.Combine("Components", "Brand"),
            Path.Combine("Components", "Pages", "Public"),
            Path.Combine("Components", "Landing"));

    [Fact]
    public void Email_templates_use_logo_e_not_legacy_mascot_png()
    {
        var root = TestRepo.FindRoot();
        var offenders = new List<string>();

        foreach (var rel in new[]
                 {
                     Path.Combine("Jobsy.Core", "Email", "EmailBrand.cs"),
                     Path.Combine("Jobsy.Core", "Email", "EmailRenderer.cs"),
                 })
        {
            var path = Path.Combine(root, rel);
            var text = File.ReadAllText(path);
            foreach (Match m in LegacyEmailMascot.Matches(text))
            {
                offenders.Add($"{rel}: {m.Value}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Email brand must use logo-E email marks, not mascot-celebrating-128.png:\n" +
            string.Join('\n', offenders));

        var emailDir = Path.Combine(root, "Jobsy.Web", "wwwroot", "images", "email");
        Assert.False(File.Exists(Path.Combine(emailDir, "mascot-celebrating-128.png")),
            "Remove legacy mascot-celebrating-128.png; use lobsy-mark-128.png.");
        Assert.True(File.Exists(Path.Combine(emailDir, "lobsy-mark-128.png")));
    }

    private static void AssertNoLegacyInRazorDirs(params string[] relDirs)
    {
        var root = TestRepo.FindRoot();
        var web = Path.Combine(root, "Jobsy.Web");
        var offenders = new List<string>();

        foreach (var relDir in relDirs)
        {
            var dir = Path.Combine(web, relDir);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.razor", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (Match m in LegacyPlayfulMascot.Matches(text))
                {
                    offenders.Add($"{Path.GetRelativePath(root, file).Replace('\\', '/')}: {m.Value}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Replace legacy mascot-* raster with BrandImages logo-E (lobsy mark/webp):\n" +
            string.Join('\n', offenders));
    }
}
