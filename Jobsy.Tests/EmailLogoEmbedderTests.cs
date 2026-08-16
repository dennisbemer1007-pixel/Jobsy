using Jobsy.Core.Email;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class EmailLogoEmbedderTests
{
    [Fact]
    public void RewriteToCid_replaces_remote_brand_mark()
    {
        var html = EmailLayout.Wrap("<p>Hallo</p>", "https://lobsy.nl");
        var embedded = EmailLogoEmbedder.RewriteToCid(html);

        Assert.Contains("cid:" + EmailLayout.LogoContentId, embedded);
        Assert.DoesNotContain("/images/brand/lobsy", embedded, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Embedded_png_is_a_small_valid_png()
    {
        var bytes = EmailLogoEmbedder.PngBytes();
        Assert.True(bytes.Length is > 1000 and < 40_000);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Wwwroot_email_logo_file_exists()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "images", "brand", "lobsy-128.png");
        Assert.True(File.Exists(path), path);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
