using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Web.Media;

/// <summary>Sample illustration band colour from the art file's left-edge column (x 0–10, full height).</summary>
public static class CompactHeroPreviewCreamSampler
{
    public static string MedianLeftEdgeHex(string absoluteImagePath)
    {
        using var img = Image.Load<Rgb24>(absoluteImagePath);
        var xs = Math.Min(11, img.Width);
        var rs = new List<int>();
        var gs = new List<int>();
        var bs = new List<int>();
        for (var x = 0; x < xs; x++)
        {
            for (var y = 0; y < img.Height; y++)
            {
                var p = img[x, y];
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
