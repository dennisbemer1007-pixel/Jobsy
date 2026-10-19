using Jobsy.Web.Media;

namespace Jobsy.Tests;

public sealed class CompactHeroPreviewCreamTests
{
    [Fact]
    public void Sampled_cream_hex_matches_css_sources()
    {
        Assert.Equal("#E4DDCE", CompactHeroPreviewCream.OnboardingKennismaken);
        Assert.Equal("#F9E7D2", CompactHeroPreviewCream.CarriereOpleiding);
        Assert.Equal("#F5E7D5", CompactHeroPreviewCream.PaspoortKompas);
        var css = File.ReadAllText(Path.Combine(
            RepoRoot,
            "Jobsy.Web/wwwroot/css/features/candidate-hero-compact-preview.css"));
        Assert.Contains("#e4ddce", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#f9e7d2", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#f5e7d5", css, StringComparison.OrdinalIgnoreCase);
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
