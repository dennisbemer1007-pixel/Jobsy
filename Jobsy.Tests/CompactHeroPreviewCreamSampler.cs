using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

/// <summary>Sample illustration band colour from the art file's left-edge column (x 0–10, full height).</summary>
public static class CompactHeroPreviewCreamSampler
{
    /// <summary>Mean RGB of the leftmost <paramref name="columns"/> (preview band fill).</summary>
    public static string AverageLeftColumnsHex(string absoluteImagePath, int columns = 4)
    {
        using var img = SkiaTestImage.LoadRgb(absoluteImagePath);
        var xs = Math.Min(columns, img.Width);
        long r = 0, g = 0, b = 0, n = 0;
        for (var x = 0; x < xs; x++)
        {
            for (var y = 0; y < img.Height; y++)
            {
                var p = img.GetRgb(x, y);
                r += p.R;
                g += p.G;
                b += p.B;
                n++;
            }
        }

        if (n == 0)
        {
            return "#000000";
        }

        return $"#{(byte)(r / n):X2}{(byte)(g / n):X2}{(byte)(b / n):X2}";
    }

    public static string MedianLeftEdgeHex(string absoluteImagePath)
    {
        using var img = SkiaTestImage.LoadRgb(absoluteImagePath);
        var xs = Math.Min(11, img.Width);
        var rs = new List<int>();
        var gs = new List<int>();
        var bs = new List<int>();
        for (var x = 0; x < xs; x++)
        {
            for (var y = 0; y < img.Height; y++)
            {
                var p = img.GetRgb(x, y);
                rs.Add(p.R);
                gs.Add(p.G);
                bs.Add(p.B);
            }
        }

        rs.Sort();
        gs.Sort();
        bs.Sort();
        var mid = rs.Count / 2;
        return $"#{rs[mid]:X2}{gs[mid]:X2}{bs[mid]:X2}";
    }
}
