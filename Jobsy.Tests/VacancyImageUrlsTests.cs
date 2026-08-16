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
    public void Resolve_keeps_picsum_seed_urls()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var picsum = VacancyImageUrls.PicsumUrl(id);
        Assert.Equal(picsum, VacancyImageUrls.Resolve(picsum, id, "Horeca"));
    }

    [Fact]
    public void Resolve_restores_svg_standin_and_unsplash_to_picsum()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var picsum = VacancyImageUrls.PicsumUrl(id);
        Assert.Equal(picsum, VacancyImageUrls.Resolve(VacancyImageUrls.Placeholder(id, WorkType.Horeca), id, "Horeca"));
        Assert.Equal(
            picsum,
            VacancyImageUrls.Resolve("https://images.unsplash.com/photo-legacy-404", id, "Horeca"));
    }

    [Fact]
    public void Resolve_keeps_uploaded_and_local_urls()
    {
        Assert.Equal("/images/uploads/x.jpg", VacancyImageUrls.Resolve("/images/uploads/x.jpg"));
        Assert.Equal("https://cdn.example/photo.jpg", VacancyImageUrls.Resolve("https://cdn.example/photo.jpg"));
        Assert.StartsWith("data:image/png", VacancyImageUrls.Resolve("data:image/png;base64,abc"));
    }

    [Fact]
    public void CdnResize_wraps_same_origin_paths_only()
    {
        Assert.Equal(
            "/cdn-cgi/image/width=400,quality=75,format=auto/images/a.jpg",
            VacancyImageUrls.CdnResize("/images/a.jpg", 400));
        Assert.Equal("https://cdn.example/a.jpg", VacancyImageUrls.CdnResize("https://cdn.example/a.jpg", 400));
        Assert.Equal("http://169.254.169.254/latest/meta-data/", VacancyImageUrls.CdnResize("http://169.254.169.254/latest/meta-data/", 400));
        Assert.Equal("//evil.example/x.jpg", VacancyImageUrls.CdnResize("//evil.example/x.jpg", 400));
    }

    [Fact]
    public void ForDisplay_does_not_proxy_absolute_urls_through_cloudflare()
    {
        const string remote = "https://cdn.example/photo.jpg";
        Assert.Equal(remote, VacancyImageUrls.ForDisplay(remote, 400, cloudflareResizing: true));
    }

    [Fact]
    public void ForDisplay_downsizes_picsum_for_cards_without_changing_the_seed()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var full = VacancyImageUrls.PicsumUrl(id);
        Assert.Equal(
            $"https://picsum.photos/seed/jobsy-{id:N}/400/267",
            VacancyImageUrls.ForDisplay(full, 400, cloudflareResizing: false, id, "Horeca"));
        Assert.Equal(full, VacancyImageUrls.ForDisplay(full, 600, cloudflareResizing: false, id, "Horeca"));
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
    public void NeedsImageBackfill_restores_svg_standins_and_keeps_picsum()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Assert.True(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill(null));
        Assert.True(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill(VacancyImageUrls.Placeholder(id, WorkType.Winkel)));
        Assert.True(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill("https://images.unsplash.com/photo-x"));
        Assert.False(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill(VacancyImageUrls.PicsumUrl(id)));
        Assert.False(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill("/images/uploads/x.jpg"));
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

    [Theory]
    [InlineData("www.lobsy.nl", true)]
    [InlineData("www.westland.lobsy.nl", true)]
    [InlineData("lobsy.nl", false)]
    [InlineData("www.example.com", false)]
    [InlineData("www.evil.example", false)]
    [InlineData("localhost", false)]
    public void ShouldRedirectWww_only_known_apex(string host, bool expected)
        => Assert.Equal(expected, CanonicalHost.ShouldRedirectWww(host));

    [Fact]
    public void ShouldRedirectWww_honors_configured_public_host()
        => Assert.True(CanonicalHost.ShouldRedirectWww("www.jobsy-demo.onrender.com", ["jobsy-demo.onrender.com"]));

    [Fact]
    public void Loopback_is_not_rewritten()
    {
        Assert.True(CanonicalHost.IsLoopback("localhost"));
        Assert.True(CanonicalHost.IsLoopback("127.0.0.1"));
    }
}
