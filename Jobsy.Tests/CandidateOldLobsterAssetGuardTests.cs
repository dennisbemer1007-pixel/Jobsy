using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Candidate-facing Razor must not reference the legacy playful mascot raster (mascot-64/128/256).
/// Logo-E uses <see cref="Jobsy.Web.Media.BrandImages"/> marks or lobsy-* assets instead.
/// </summary>
public class CandidateOldLobsterAssetGuardTests
{
    private static readonly Regex LegacyPlayfulMascot = new(
        @"mascot-(?:64|128|256)\.(?:webp|png)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void Candidate_razor_has_no_legacy_playful_mascot_assets()
    {
        var root = TestRepo.FindRoot();
        var web = Path.Combine(root, "Jobsy.Web");
        var offenders = new List<string>();

        foreach (var relDir in new[]
                 {
                     Path.Combine("Components", "Candidate"),
                     Path.Combine("Components", "Pages", "Candidate"),
                 })
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
