using Jobsy.Web.Media;

namespace Jobsy.Tests;

public sealed class CompactHeroPreviewCreamTests
{
    [Fact]
    public void Sampled_cream_hex_matches_css_sources()
    {
        Assert.Equal(
            CompactHeroPreviewCream.OnboardingFinaleHero,
            CompactHeroPreviewCreamSampler.AverageLeftColumnsHex(Abs(CompactHeroPreviewArt.OnboardingKennismakenDesktop), 4));
        Assert.Equal(
            CompactHeroPreviewCream.CarriereLearnGrowWide,
            CompactHeroPreviewCreamSampler.MedianLeftEdgeHex(Abs(CandidateIllustratedImages.LearnGrowHeroWide)));
        Assert.Equal(
            CompactHeroPreviewCream.PaspoortKompasWide,
            CompactHeroPreviewCreamSampler.MedianLeftEdgeHex(Abs(CandidateIllustratedImages.KompasHeroWide)));
        var css = File.ReadAllText(Path.Combine(
            RepoRoot,
            "Jobsy.Web/wwwroot/css/features/candidate-hero-compact-preview.css"));
        Assert.Contains(CompactHeroPreviewBand.Token, css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--cand-compact-hero-band", css, StringComparison.Ordinal);
    }

    private static string Abs(string webPath)
    {
        var rel = webPath.Split('?', 2)[0].TrimStart('/');
        return Path.GetFullPath(Path.Combine(RepoRoot, "Jobsy.Web/wwwroot", rel));
    }

    private static string RepoRoot
    {
        get
        {
            var dir = AppContext.BaseDirectory;
            while (dir is not null && !File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }

            return dir ?? throw new InvalidOperationException("repo root not found");
        }
    }
}
