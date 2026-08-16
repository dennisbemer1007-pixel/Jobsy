using Jobsy.Core.Enums;
using Jobsy.Core.Media;
using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

public class VacancyImageUrlsTests
{
    [Fact]
    public void Placeholder_is_local_svg_by_work_type()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var url = VacancyImageUrls.Placeholder(id, WorkType.Horeca);
        Assert.StartsWith("/images/vacancies/horeca-", url);
        Assert.EndsWith(".svg", url);
    }

    [Fact]
    public void Resolve_rewrites_picsum_to_local_placeholder()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var picsum = $"https://picsum.photos/seed/jobsy-{id:N}/600/400";
        var resolved = VacancyImageUrls.Resolve(picsum, id, "Horeca");
        Assert.Equal(VacancyImageUrls.Placeholder(id, WorkType.Horeca), resolved);
    }

    [Fact]
    public void Resolve_keeps_uploaded_and_local_urls()
    {
        Assert.Equal("/images/uploads/x.jpg", VacancyImageUrls.Resolve("/images/uploads/x.jpg"));
        Assert.Equal("https://cdn.example/photo.jpg", VacancyImageUrls.Resolve("https://cdn.example/photo.jpg"));
        Assert.StartsWith("data:image/png", VacancyImageUrls.Resolve("data:image/png;base64,abc"));
    }

    [Fact]
    public void CdnResize_wraps_absolute_and_root_paths()
    {
        Assert.Equal(
            "/cdn-cgi/image/width=400,quality=75,format=auto/https://cdn.example/a.jpg",
            VacancyImageUrls.CdnResize("https://cdn.example/a.jpg", 400));
        Assert.Equal(
            "/cdn-cgi/image/width=400,quality=75,format=auto/images/a.jpg",
            VacancyImageUrls.CdnResize("/images/a.jpg", 400));
    }

    [Fact]
    public void Placeholder_svg_files_exist_for_every_work_type()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Jobsy.Web", "wwwroot", "images", "vacancies");
        foreach (var slug in new[]
                 {
                     "horeca", "winkel", "logistiek", "tuinbouw", "zorg",
                     "kantoor", "bouw", "schoonmaak", "productie", "flex"
                 })
        {
            Assert.True(File.Exists(Path.Combine(dir, $"{slug}-0.svg")), slug + "-0");
            Assert.True(File.Exists(Path.Combine(dir, $"{slug}-1.svg")), slug + "-1");
        }
    }

    [Fact]
    public void ForDisplay_skips_svg_and_data_uris_when_resizing()
    {
        var svg = VacancyImageUrls.Placeholder(Guid.NewGuid(), WorkType.Winkel);
        Assert.Equal(svg, VacancyImageUrls.ForDisplay(svg, 400, cloudflareResizing: true));
        Assert.Equal("data:image/png;base64,x", VacancyImageUrls.ForDisplay("data:image/png;base64,x", 400, true));
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

public class CanonicalHostTests
{
    [Theory]
    [InlineData("www.lobsy.nl", true, "lobsy.nl")]
    [InlineData("lobsy.nl", false, "lobsy.nl")]
    [InlineData("www.example.com", true, "example.com")]
    public void TryStripWww(string host, bool expected, string canonical)
    {
        Assert.Equal(expected, CanonicalHost.TryStripWww(host, out var actual));
        Assert.Equal(canonical, actual);
    }

    [Fact]
    public void Loopback_is_not_rewritten()
    {
        Assert.True(CanonicalHost.IsLoopback("localhost"));
        Assert.True(CanonicalHost.IsLoopback("127.0.0.1"));
    }
}
