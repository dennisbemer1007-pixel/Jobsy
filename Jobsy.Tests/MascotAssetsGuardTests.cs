using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Jobsy.Web.Media;

namespace Jobsy.Tests;

/// <summary>
/// Guards mascot art files against the contract in docs/brand/mascot-assets.md.
/// Inline entries need no files. Svg/Raster entries must exist and stay safe/budgeted.
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
    public void Production_poses_use_inline_svg()
    {
        foreach (var art in MascotAssets.Arts)
        {
            Assert.Equal(MascotArtFormat.Inline, art.Format);
            Assert.Equal(MascotAssets.InlineVersion, art.Version);
        }

        var uitleggen = MascotAssets.Resolve(MascotPose.Uitleggen);
        Assert.True(uitleggen.IsInline);
        Assert.Equal("pub-mascot--pose-uitleggen", uitleggen.FallbackTransformClass);

        var shell = MascotAssets.Resolve(MascotPose.Shell);
        Assert.True(shell.IsInline);
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
        Assert.Contains("rustig", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LobsyMascotArt", text, StringComparison.Ordinal);
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
    }

    private static void AssertRaster(string folder, string slug)
    {
        foreach (var w in RasterWidths)
        {
            var webp = Path.Combine(folder, $"mascot-{slug}-{w}.webp");
            Assert.True(File.Exists(webp), $"Missing {webp}");
        }

        var png = Path.Combine(folder, $"mascot-{slug}-256.png");
        Assert.True(File.Exists(png), $"Missing PNG fallback {png}");
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
}
