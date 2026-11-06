using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

public class LearnGrowMobileHeroPixelTests
{
    private static readonly byte[] Pearl = [247, 244, 240];

    [Fact]
    public void Mobile_hero_screenshot_right_band_has_enough_illustration()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Tests", "TestAssets", "leren-groeien-mobiel-390.png");
        Assert.True(File.Exists(path), $"Add {path} from opleidingen-r10 capture.");

        using var bmp = SkiaTestImage.LoadRgb(path);
        var w = bmp.Width;
        var x0 = (int)(w * 0.55);
        var nonPearl = 0;
        var total = 0;
        for (var y = 0; y < bmp.Height; y++)
        {
            for (var x = x0; x < w; x++)
            {
                var p = bmp.GetRgb(x, y);
                total++;
                if (!IsPearl(p.R, p.G, p.B))
                {
                    nonPearl++;
                }
            }
        }

        var ratio = total == 0 ? 0.0 : (double)nonPearl / total;
        Assert.True(ratio >= 0.35, $"Right 45% non-pearl ratio {ratio:P1} is below 35%.");
    }

    private static bool IsPearl(byte r, byte g, byte b) =>
        Math.Abs(r - Pearl[0]) <= 14 && Math.Abs(g - Pearl[1]) <= 14 && Math.Abs(b - Pearl[2]) <= 16;

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
