using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Branding;
using Jobsy.Web.Localization;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class PlatformSloganAndJobMapWordingTests
{
    [Fact]
    public async Task Company_settings_persist_a_custom_slogan()
    {
        await using var db = CreateDb();
        var sut = new PlatformCompanySettingsService(db);

        var updated = await sut.UpdateAsync(new PlatformCompanyUpdate(
            "Lobsy",
            "Onze eigen slogan",
            null,
            null,
            null,
            "NL",
            null,
            null,
            null,
            null));

        Assert.Equal("Onze eigen slogan", updated.Slogan);
        Assert.Equal("Onze eigen slogan", (await sut.GetAsync()).Slogan);
    }

    [Fact]
    public async Task Empty_slogan_falls_back_to_the_default_tagline()
    {
        await using var db = CreateDb();
        var sut = new PlatformCompanySettingsService(db);

        await sut.UpdateAsync(new PlatformCompanyUpdate(
            "Lobsy",
            "Tijdelijk",
            null,
            null,
            null,
            "NL",
            null,
            null,
            null,
            null));
        var cleared = await sut.UpdateAsync(new PlatformCompanyUpdate(
            "Lobsy",
            "   ",
            null,
            null,
            null,
            "NL",
            null,
            null,
            null,
            null));

        Assert.Equal(PlatformCompanySettingsService.DefaultSlogan, cleared.Slogan);
        Assert.Equal(
            "Dichtbij genoeg om het pantser te laten vallen",
            PlatformCompanySettingsService.DefaultSlogan);
    }

    [Fact]
    public void Header_prefers_region_then_company_then_fallback()
    {
        Assert.Equal(
            "Westland werkt",
            PlatformBrandingState.ResolveTagline("Westland werkt", "Bedrijfsslogan", "fallback"));
        Assert.Equal(
            "Bedrijfsslogan",
            PlatformBrandingState.ResolveTagline("  ", "Bedrijfsslogan", "fallback"));
        Assert.Equal(
            "fallback",
            PlatformBrandingState.ResolveTagline(null, null, "fallback"));
    }

    [Fact]
    public void Job_map_count_uses_vacatures_not_bijbanen()
    {
        var nl = UiStrings.Get("Discovery.ResultCount", "nl");
        Assert.Equal("{0} vacatures op Lobsy", nl);
        Assert.DoesNotContain("bijbanen", nl, StringComparison.OrdinalIgnoreCase);

        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("PlatformBrandingState", layout);
        Assert.Contains("HeaderTagline", layout);
        Assert.DoesNotContain("RegionHost.Current?.Slogan ?? Culture[\"Brand.Tagline\"]", layout);

        var admin = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Admin", "CompanySettingsAdmin.razor"));
        Assert.Contains("maxlength=\"240\"", admin);
        Assert.Contains("Branding.RefreshAsync()", admin);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
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
