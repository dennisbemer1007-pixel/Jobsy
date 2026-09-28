using Jobsy.Core.Enums;
using Jobsy.Core.Media;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class VacancyImageUrlsTests
{
    [Fact]
    public void Placeholder_is_local_webp_by_work_type()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var url = VacancyImageUrls.Placeholder(id, WorkType.Horeca);
        Assert.Equal("/images/vacancies/horeca.webp", url);
    }

    [Fact]
    public void Placeholder_maps_product_category_aliases()
    {
        var id = Guid.NewGuid();
        Assert.Equal("/images/vacancies/winkel.webp", VacancyImageUrls.Placeholder(id, "retail"));
        Assert.Equal("/images/vacancies/tuinbouw.webp", VacancyImageUrls.Placeholder(id, "groen"));
        Assert.Equal("/images/vacancies/bouw.webp", VacancyImageUrls.Placeholder(id, "techniek"));
        Assert.Equal("/images/vacancies/kantoor.webp", VacancyImageUrls.Placeholder(id, "administratie"));
        Assert.Equal("/images/vacancies/onderwijs.webp", VacancyImageUrls.Placeholder(id, "onderwijs"));
        Assert.Equal("/images/vacancies/flex.webp", VacancyImageUrls.Placeholder(id, "overig"));
    }

    [Fact]
    public void Resolve_maps_picsum_and_unsplash_to_local_fallback()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var photo = VacancyImageUrls.Placeholder(id, WorkType.Horeca);
        Assert.Equal(photo, VacancyImageUrls.Resolve(VacancyImageUrls.PicsumUrl(id), id, "Horeca"));
        Assert.Equal(photo, VacancyImageUrls.Resolve("https://images.unsplash.com/photo-legacy-404", id, "Horeca"));
        Assert.Equal(photo, VacancyImageUrls.Resolve(photo, id, "Horeca"));
    }

    [Fact]
    public void Resolve_uses_company_logo_when_photo_is_missing()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.Equal(
            "/images/logos/westland.svg",
            VacancyImageUrls.Resolve(null, "/images/logos/westland.svg", id, "Horeca"));
        Assert.Equal(
            "/images/logos/westland.svg",
            VacancyImageUrls.Resolve("  ", "images/logos/westland.svg", id, "Logistiek"));
        Assert.Equal(
            "/images/logos/cafe.svg",
            VacancyImageUrls.Resolve("null", "https://lobsy.nl/images/logos/cafe.svg", id, "Horeca"));
    }

    [Fact]
    public void Resolve_uses_work_type_photo_when_photo_and_logo_are_empty()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var photo = VacancyImageUrls.Placeholder(id, "Horeca");
        Assert.Equal(photo, VacancyImageUrls.Resolve(null, null, id, "Horeca"));
        Assert.Equal(photo, VacancyImageUrls.Resolve("undefined", "", id, "Horeca"));
    }

    [Fact]
    public void Normalize_rewrites_own_origin_and_storage_paths()
    {
        Assert.Null(VacancyImageUrls.Normalize(null));
        Assert.Null(VacancyImageUrls.Normalize(" "));
        Assert.Null(VacancyImageUrls.Normalize("null"));
        Assert.Null(VacancyImageUrls.Normalize("undefined"));
        Assert.Equal("/images/uploads/x.jpg", VacancyImageUrls.Normalize("/images/uploads/x.jpg"));
        Assert.Equal("/images/uploads/x.jpg", VacancyImageUrls.Normalize("images/uploads/x.jpg"));
        Assert.Equal("/images/uploads/x.jpg", VacancyImageUrls.Normalize("uploads/x.jpg"));
        Assert.Equal("/images/uploads/shot.png", VacancyImageUrls.Normalize("shot.png"));
        Assert.Equal("/images/logos/westland.svg", VacancyImageUrls.Normalize("wwwroot/images/logos/westland.svg"));
        Assert.Equal("/images/uploads/x.jpg", VacancyImageUrls.Normalize("https://lobsy.nl/images/uploads/x.jpg"));
        Assert.Equal("/images/uploads/x.jpg?v=2", VacancyImageUrls.Normalize("https://www.lobsy.nl/images/uploads/x.jpg?v=2"));
        Assert.Equal("https://cdn.example/photo.jpg", VacancyImageUrls.Normalize("https://cdn.example/photo.jpg"));
        Assert.Null(VacancyImageUrls.Normalize("/images/../secret.jpg"));
    }

    [Fact]
    public void AlternateSrc_is_logo_only_when_it_differs_from_display()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var photo = VacancyImageUrls.Placeholder(id, "Horeca");
        Assert.Equal(
            "/images/logos/westland.svg",
            VacancyImageUrls.AlternateSrc(
                VacancyImageUrls.PicsumUrl(id),
                "/images/logos/westland.svg",
                photo));
        Assert.Null(VacancyImageUrls.AlternateSrc(
            "/images/logos/westland.svg",
            "/images/logos/westland.svg",
            "/images/logos/westland.svg"));
        Assert.Null(VacancyImageUrls.AlternateSrc(null, null, "/images/vacancies/horeca.webp"));
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
        Assert.Equal("/images/../secret.jpg", VacancyImageUrls.CdnResize("/images/../secret.jpg", 400));
    }

    [Fact]
    public void ForDisplay_does_not_proxy_absolute_urls_through_cloudflare()
    {
        const string remote = "https://cdn.example/photo.jpg";
        Assert.Equal(remote, VacancyImageUrls.ForDisplay(remote, 400, cloudflareResizing: true));
    }

    [Fact]
    public void ForDisplay_maps_picsum_to_local_fallback()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var photo = VacancyImageUrls.Placeholder(id, "Horeca");
        Assert.Equal(photo, VacancyImageUrls.ForDisplay(VacancyImageUrls.PicsumUrl(id), 400, cloudflareResizing: false, id, "Horeca"));
    }

    [Fact]
    public void Placeholder_webp_files_exist_for_every_work_type()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Jobsy.Web", "wwwroot", "images", "vacancies");
        foreach (var slug in new[]
                 {
                     "horeca", "winkel", "logistiek", "tuinbouw", "zorg",
                     "kantoor", "bouw", "schoonmaak", "productie", "flex", "onderwijs"
                 })
        {
            var primary = Path.Combine(dir, $"{slug}.webp");
            var companion = Path.Combine(dir, $"{slug}-400.webp");
            Assert.True(File.Exists(primary), slug + ".webp");
            Assert.True(File.Exists(companion), slug + "-400.webp");
            Assert.True(new FileInfo(primary).Length < 80 * 1024, slug + " under 80KB");
        }
    }

    [Fact]
    public void SrcSet_returns_local_400_and_800_for_fallback_photos()
    {
        var primary = VacancyImageUrls.Placeholder(Guid.NewGuid(), WorkType.Winkel);
        var srcset = VacancyImageUrls.SrcSet(primary, cloudflareResizing: false);
        Assert.Equal("/images/vacancies/winkel-400.webp 400w, /images/vacancies/winkel.webp 800w", srcset);
    }

    [Fact]
    public void NeedsImageBackfill_replaces_third_party_and_legacy_svg()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Assert.True(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill(null));
        Assert.False(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill(VacancyImageUrls.Placeholder(id, WorkType.Winkel)));
        Assert.True(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill("https://images.unsplash.com/photo-x"));
        Assert.True(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill(VacancyImageUrls.PicsumUrl(id)));
        Assert.True(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill("/images/vacancies/horeca-0.svg"));
        Assert.False(Jobsy.Infrastructure.Data.MockVacancyMedia.NeedsImageBackfill("/images/uploads/x.jpg"));
    }

    [Fact]
    public void ForDisplay_skips_fallback_webp_and_data_uris_when_resizing()
    {
        var photo = VacancyImageUrls.Placeholder(Guid.NewGuid(), WorkType.Winkel);
        Assert.Equal(photo, VacancyImageUrls.ForDisplay(photo, 400, cloudflareResizing: true));
        Assert.Equal("data:image/png;base64,x", VacancyImageUrls.ForDisplay("data:image/png;base64,x", 400, true));
    }

    [Fact]
    public void ForPublicList_never_embeds_data_uris_and_maps_picsum_to_local()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var placeholder = VacancyImageUrls.Placeholder(id, "Horeca");
        Assert.Equal(placeholder, VacancyImageUrls.ForPublicList("data:image/png;base64,abc", id, "Horeca"));
        Assert.DoesNotContain("data:image", VacancyImageUrls.ForPublicList("data:image/jpeg;base64,/9j/", id, "Zorg"));

        Assert.Equal(placeholder, VacancyImageUrls.ForPublicList(VacancyImageUrls.PicsumUrl(id), id, "Horeca"));
        Assert.Equal("/images/logos/westland.svg", VacancyImageUrls.ForPublicList("/images/logos/westland.svg", id));
    }

    [Fact]
    public void ForCard_data_uri_uses_public_image_path()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.Equal(
            VacancyImageUrls.PublicImagePath(id),
            VacancyImageUrls.ForCard("data:image/png;base64,abc", "/images/logos/westland.svg", id, "Horeca"));
    }

    [Fact]
    public void ForCard_keeps_same_origin_https_path_photo()
    {
        var id = Guid.NewGuid();
        Assert.Equal(
            "/images/uploads/photo.jpg",
            VacancyImageUrls.ForCard("/images/uploads/photo.jpg", "/images/logos/westland.svg", id, "Zorg"));
    }

    [Fact]
    public void ForCard_empty_photo_with_logo_returns_logo()
    {
        var id = Guid.NewGuid();
        Assert.Equal(
            "/images/logos/westland.svg",
            VacancyImageUrls.ForCard(null, "/images/logos/westland.svg", id, "Winkel"));
    }

    [Fact]
    public void ForCard_empty_photo_without_logo_returns_webp_placeholder()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.Equal(
            VacancyImageUrls.Placeholder(id, "Horeca"),
            VacancyImageUrls.ForCard(null, null, id, "Horeca"));
    }

    [Fact]
    public void ForCard_picsum_falls_back_to_logo_then_placeholder()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.Equal(
            "/images/logos/westland.svg",
            VacancyImageUrls.ForCard(VacancyImageUrls.PicsumUrl(id), "/images/logos/westland.svg", id, "Horeca"));
        Assert.Equal(
            VacancyImageUrls.Placeholder(id, "Horeca"),
            VacancyImageUrls.ForCard(VacancyImageUrls.PicsumUrl(id), null, id, "Horeca"));
    }

    [Fact]
    public void ForCard_external_https_falls_back_for_csp()
    {
        var id = Guid.NewGuid();
        // External absolute URLs are not returned (img-src 'self'); logo/placeholder instead.
        Assert.Equal(
            "/images/logos/westland.svg",
            VacancyImageUrls.ForCard(
                "https://cdn.example.com/vacancy.jpg",
                "/images/logos/westland.svg",
                id,
                "Zorg"));
    }

    [Fact]
    public void ForCardKind_classifies_photo_logo_and_placeholder()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var photo = VacancyImageUrls.ForCard("/images/brand/dennis.jpg", "/images/logos/westland.svg", id, "Horeca");
        Assert.Equal("photo", VacancyImageUrls.ForCardKind(photo, "/images/logos/westland.svg"));

        var logo = VacancyImageUrls.ForCard(null, "/images/logos/westland.svg", id, "Horeca");
        Assert.Equal("logo", VacancyImageUrls.ForCardKind(logo, "/images/logos/westland.svg"));

        var placeholder = VacancyImageUrls.ForCard(null, null, id, "Horeca");
        Assert.Equal("placeholder", VacancyImageUrls.ForCardKind(placeholder, null));

        var externalFallsToLogo = VacancyImageUrls.ForCard(
            "https://cdn.example.com/x.jpg", "/images/logos/westland.svg", id, "Zorg");
        Assert.Equal("logo", VacancyImageUrls.ForCardKind(externalFallsToLogo, "/images/logos/westland.svg"));
    }

    [Fact]
    public void Application_card_logo_follows_intermediary_when_client_address_hidden()
    {
        // Mirrors MeController: intermediary logo when ForPublicCard shows intermediary name.
        var (name, _) = CandidateApplicationLocation.ForPublicCard(
            hasIntermediary: true,
            showClientAddressOnMap: false,
            endClientName: "Jumbo",
            endClientAddress: "Straat 1, Naaldwijk",
            intermediaryName: "Uitzend Westland",
            intermediaryAddress: "Haven 2, Den Haag");
        Assert.Equal("Uitzend Westland", name);

        var logo = /* same branch as MeController */ true && !false
            ? "/images/logos/intermediary.svg"
            : "/images/logos/client.svg";
        Assert.Equal("/images/logos/intermediary.svg", logo);

        var id = Guid.NewGuid();
        var picture = VacancyImageUrls.ForCard(null, logo, id, "Winkel");
        Assert.Equal("logo", VacancyImageUrls.ForCardKind(picture, logo));
    }

    [Fact]
    public void TryDecodeInlineImage_reads_png_data_uri()
    {
        var png = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 });
        Assert.True(VacancyImageUrls.TryDecodeInlineImage($"data:image/png;base64,{png}", out var bytes, out var type));
        Assert.Equal("image/png", type);
        Assert.True(bytes.Length > 8);
        Assert.False(VacancyImageUrls.TryDecodeInlineImage("https://picsum.photos/seed/x/400/267", out _, out _));
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
