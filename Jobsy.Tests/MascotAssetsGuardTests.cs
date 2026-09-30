using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Jobsy.Web.Media;

namespace Jobsy.Tests;

/// <summary>
/// Guards mascot art files against the contract in docs/brand/mascot-assets.md.
/// Fallback entries need no files (today's state). Svg/Raster entries must exist and stay safe/budgeted.
/// </summary>
public class MascotAssetsGuardTests
{
    private static readonly int[] RasterWidths = [64, 128, 256, 512];

    [Fact]
    public void Every_pose_has_a_manifest_entry()
    {
        foreach (MascotPose pose in Enum.GetValues<MascotPose>())
        {
            var art = MascotAssets.GetArt(pose);
            Assert.Equal(pose, art.Pose);
            Assert.False(string.IsNullOrWhiteSpace(art.Version));
        }
    }

    [Fact]
    public void Fallback_entries_need_no_pose_files()
    {
        var root = TestRepo.FindRoot();
        var folder = Path.Combine(root, "Jobsy.Web", "wwwroot", "images", "brand", "mascot");
        foreach (var art in MascotAssets.Arts)
        {
            if (art.Format != MascotArtFormat.Fallback)
            {
                continue;
            }

            var slug = MascotAssets.PoseSlug(art.Pose);
            Assert.False(File.Exists(Path.Combine(folder, $"mascot-{slug}.svg")),
                $"Fallback pose {slug} must not ship pose SVG yet (or flip Format to Svg).");
        }

        // Resolve still returns today's BrandImages for fallback.
        var waving = MascotAssets.Resolve(MascotPose.Waving);
        Assert.True(waving.IsFallback);
        Assert.Equal("pub-mascot--fb-waving", waving.FallbackTransformClass);
        Assert.Contains("mascot-64.webp", waving.SrcSet, StringComparison.Ordinal);
        Assert.Contains(BrandImages.MascotVersion, waving.Src, StringComparison.Ordinal);

        var shell = MascotAssets.Resolve(MascotPose.Shell);
        Assert.True(shell.IsFallback);
        Assert.Equal("pub-mascot--ghost", shell.FallbackTransformClass);
    }

    [Fact]
    public void Svg_and_raster_entries_match_asset_contract()
    {
        var root = TestRepo.FindRoot();
        var folder = Path.Combine(root, "Jobsy.Web", "wwwroot", "images", "brand", "mascot");

        foreach (var art in MascotAssets.Arts)
        {
            var slug = MascotAssets.PoseSlug(art.Pose);
            if (art.Format == MascotArtFormat.Svg)
            {
                AssertSvg(Path.Combine(folder, $"mascot-{slug}.svg"));
            }
            else if (art.Format == MascotArtFormat.Raster)
            {
                AssertRaster(folder, slug);
            }
        }
    }

    [Fact]
    public void Asset_contract_doc_exists()
    {
        var path = Path.Combine(TestRepo.FindRoot(), "docs", "brand", "mascot-assets.md");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("waving", text, StringComparison.Ordinal);
        Assert.Contains("mascot-{pose}.svg", text, StringComparison.Ordinal);
        Assert.Contains("For developers", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MascotAssets.cs", text, StringComparison.Ordinal);
    }

    private static void AssertSvg(string path)
    {
        Assert.True(File.Exists(path), $"Missing SVG: {path}");
        var bytes = File.ReadAllBytes(path);
        var gzLen = GzipLength(bytes);
        Assert.True(gzLen <= 30 * 1024, $"{path} gzipped is {gzLen} bytes (budget 30 KB)");

        var xml = XDocument.Parse(Encoding.UTF8.GetString(bytes));
        var root = xml.Root ?? throw new InvalidOperationException("SVG has no root");
        Assert.True(root.Attribute("viewBox") is not null, $"{path} needs a viewBox");

        var raw = File.ReadAllText(path);
        Assert.DoesNotContain("<script", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<image", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<foreignObject", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"http", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xlink:href=\"http", raw, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertRaster(string folder, string slug)
    {
        foreach (var w in RasterWidths)
        {
            var webp = Path.Combine(folder, $"mascot-{slug}-{w}.webp");
            var avif = Path.Combine(folder, $"mascot-{slug}-{w}.avif");
            Assert.True(File.Exists(webp), $"Missing {webp}");
            Assert.True(File.Exists(avif), $"Missing {avif}");
            AssertSquareWebp(webp);
            AssertAvifLooksValid(avif);

            var webpBudget = w >= 512 ? 40 * 1024 : w >= 256 ? 16 * 1024 : int.MaxValue;
            var avifBudget = w >= 512 ? 30 * 1024 : w >= 256 ? 12 * 1024 : int.MaxValue;
            if (webpBudget < int.MaxValue)
            {
                Assert.True(new FileInfo(webp).Length <= webpBudget, $"{webp} exceeds WebP budget");
            }

            if (avifBudget < int.MaxValue)
            {
                Assert.True(new FileInfo(avif).Length <= avifBudget, $"{avif} exceeds AVIF budget");
            }
        }

        var png = Path.Combine(folder, $"mascot-{slug}-256.png");
        Assert.True(File.Exists(png), $"Missing PNG fallback {png}");
        AssertSquarePng(png);
    }

    private static int GzipLength(byte[] bytes)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gz.Write(bytes);
        }

        return (int)ms.Length;
    }

    private static void AssertSquarePng(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24, path);
        // IHDR width/height at offset 16 (big-endian).
        var w = ReadBe32(bytes, 16);
        var h = ReadBe32(bytes, 20);
        Assert.Equal(w, h);
    }

    private static void AssertSquareWebp(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 30, path);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal("WEBP", Encoding.ASCII.GetString(bytes, 8, 4));
        var fourcc = Encoding.ASCII.GetString(bytes, 12, 4);
        int w, h;
        if (fourcc == "VP8X")
        {
            // Canvas width/height are 24-bit little-endian minus one at offset 24.
            w = 1 + bytes[24] + (bytes[25] << 8) + (bytes[26] << 16);
            h = 1 + bytes[27] + (bytes[28] << 8) + (bytes[29] << 16);
        }
        else if (fourcc == "VP8 ")
        {
            // Lossy bitstream: width/height 14-bit little-endian at offset 26.
            w = (bytes[26] | (bytes[27] << 8)) & 0x3FFF;
            h = (bytes[28] | (bytes[29] << 8)) & 0x3FFF;
        }
        else if (fourcc == "VP8L")
        {
            var bits = bytes[21] | (bytes[22] << 8) | (bytes[23] << 16) | (bytes[24] << 24);
            w = (bits & 0x3FFF) + 1;
            h = ((bits >> 14) & 0x3FFF) + 1;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported WebP fourcc {fourcc} in {path}");
        }

        Assert.Equal(w, h);
    }

    private static void AssertAvifLooksValid(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 12, path);
        // ftyp box at start of ISOBMFF.
        Assert.Equal("ftyp", Encoding.ASCII.GetString(bytes, 4, 4));
        var brand = Encoding.ASCII.GetString(bytes, 8, 4);
        Assert.True(
            brand.Contains("avif", StringComparison.OrdinalIgnoreCase)
            || brand.Contains("avis", StringComparison.OrdinalIgnoreCase)
            || brand.Contains("mif1", StringComparison.OrdinalIgnoreCase),
            $"Unexpected AVIF brand '{brand}' in {path}");
    }

    private static int ReadBe32(byte[] bytes, int offset)
        => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}
