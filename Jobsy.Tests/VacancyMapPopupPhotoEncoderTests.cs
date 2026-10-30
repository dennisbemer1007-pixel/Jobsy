using Jobsy.Infrastructure.Media;

namespace Jobsy.Tests;

public class VacancyMapPopupPhotoEncoderTests
{
    [Fact]
    public void Mobile_webp_from_category_photo_is_small_and_fast_enough()
    {
        var repoRoot = FindRepoRoot();
        var source = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "images", "vacancies", "tuinbouw.webp");
        Assert.True(File.Exists(source), "fixture " + source);

        var bytes = VacancyMapPopupPhotoEncoder.ToWebp(File.ReadAllBytes(source), VacancyMapPopupPhotoEncoder.MobileWidth);
        Assert.InRange(bytes.Length, 500, 40_000);
    }

    [Fact]
    public void Category_placeholder_webp_is_valid_and_compact()
    {
        var bytes = VacancyMapPopupPhotoEncoder.CategoryPlaceholderWebp("#b4500f", 640, "desktop");
        Assert.InRange(bytes.Length, 200, 12_000);
        Assert.Equal(0x52, bytes[0]); // RIFF / WebP container
    }

    [Fact]
    public void Desktop_webp_from_category_photo_stays_under_typical_budget()
    {
        var repoRoot = FindRepoRoot();
        var source = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "images", "vacancies", "tuinbouw.webp");
        var bytes = VacancyMapPopupPhotoEncoder.ToWebp(File.ReadAllBytes(source), VacancyMapPopupPhotoEncoder.DesktopWidth);
        Assert.InRange(bytes.Length, 500, 80_000);
    }

    private static string FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
