using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

public sealed class TestBlockHeroAssetTests
{
    private static string FiguresDir()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "Jobsy.Web", "wwwroot", "images", "tests", "figures");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("figures dir not found");
    }

    [Theory]
    [InlineData("hoe-je-werkt")]
    [InlineData("leuk")]
    [InlineData("sfeer")]
    [InlineData("waarden")]
    public void Hero_figure_png_has_no_opaque_frame_border(string slug)
    {
        var path = Path.Combine(FiguresDir(), $"{slug}-hero.png");
        using var image = SkiaTestImage.LoadRgba(path);
        var w = image.Width;
        var h = image.Height;
        foreach (var (x, y) in new (int, int)[]
                 {
                     (0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1),
                     (2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3)
                 })
        {
            var px = image.GetRgba(x, y);
            Assert.True(px.A < 32,
                $"{slug}-hero.png corner ({x},{y}) should stay transparent, alpha={px.A}.");
        }
    }
}
