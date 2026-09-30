using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class KandidaatBanenFoundationGuardTests
{
    private static readonly string[] GuardedRazorRelPaths =
    [
        "Jobsy.Web/Components/VacancyDiscovery.razor",
        "Jobsy.Web/Components/Pages/VacancyDetail.razor",
        "Jobsy.Web/Components/Pages/Candidate/Applications.razor",
        "Jobsy.Web/Components/Pages/Candidate/Liked.razor",
        "Jobsy.Web/Components/Pages/Candidate/Shared.razor",
        "Jobsy.Web/Components/Pages/Candidate/MatchPage.razor",
    ];

    [Fact]
    public void Focus_css_hides_programmatic_h1_box_only()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains("h1[tabindex=\"-1\"]:focus:not(:focus-visible)", css, StringComparison.Ordinal);
        Assert.Contains("outline: none", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Inline_style_on_candidate_job_surfaces_only_uses_KbCategoryColor_helper()
    {
        var root = FindRepoRoot();
        var files = GuardedRazorRelPaths
            .Select(p => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar)))
            .Concat(Directory.EnumerateFiles(
                Path.Combine(root, "Jobsy.Web", "Components", "KandidaatBanen"),
                "*.razor",
                SearchOption.AllDirectories))
            .ToList();

        var styleAttr = new Regex("""style\s*=\s*"([^"]*)" """, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        foreach (var file in files)
        {
            Assert.True(File.Exists(file), file);
            var text = File.ReadAllText(file);
            foreach (Match m in styleAttr.Matches(text))
            {
                var value = m.Groups[1].Value.Trim();
                Assert.True(
                    value.StartsWith("@KbCategoryColor.Style(", StringComparison.Ordinal),
                    $"{Path.GetRelativePath(root, file)} has style=\"{value}\" — only KbCategoryColor.Style(...) is allowed.");
            }
        }
    }

    [Fact]
    public void No_hardcoded_dutch_literals_in_candidate_job_markup()
    {
        var root = FindRepoRoot();
        var dutch = new Regex(
            """(?:>|\b(?:title|aria-label)\s*=\s*")[^"<{]*\b(Legenda|Laden…|Verberg mijn|Toon mijn|Video laden|Geen vacatures)\b""",
            RegexOptions.CultureInvariant);

        foreach (var rel in GuardedRazorRelPaths)
        {
            var path = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
            var text = File.ReadAllText(path);
            var markupEnd = text.IndexOf("@code", StringComparison.Ordinal);
            var markup = markupEnd > 0 ? text[..markupEnd] : text;
            var m = dutch.Match(markup);
            Assert.False(m.Success, $"{rel} still has hardcoded Dutch near: {m.Value}");
        }
    }

    [Fact]
    public void Feature_css_and_docs_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "docs", "features", "kandidaat-banen.md")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "kandidaat-banen.css")));
        var appRazor = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("css/features/kandidaat-banen.css?v=20260930-kb7", appRazor, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Seed_photos_unique_per_company_and_nearby_same_category()
    {
        await using var db = CreateDb();
        SeedMinimumWageRates(db);
        var logger = NullLogger.Instance;

        await WestlandVacanciesSeeder.SeedWestlandBanenkaartAsync(db, logger);
        await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, logger);

        var vacancies = await db.Vacancies
            .AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active && v.ImageUrl != null && v.Location != null)
            .Select(v => new { v.Id, v.CompanyId, v.ImageUrl, v.WorkTypes, Lat = v.Location!.Latitude, Lng = v.Location.Longitude })
            .ToListAsync();

        Assert.NotEmpty(vacancies);

        foreach (var group in vacancies.GroupBy(v => v.CompanyId))
        {
            var urls = group.Select(v => v.ImageUrl!).ToList();
            if (urls.Count <= MockVacancyMedia.SeedPhotoSlugs.Length)
            {
                Assert.Equal(urls.Count, urls.Distinct(StringComparer.Ordinal).Count());
            }
        }

        static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            static double Rad(double d) => d * Math.PI / 180.0;
            var dLat = Rad(lat2 - lat1);
            var dLon = Rad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2))
                    * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        // Westland layout is sparse enough that same-category neighbours within 1 km
        // stay within the photo-pool pigeonhole; Haaglanden density can exceed it.
        var westland = vacancies
            .Where(v => v.Id.ToString().StartsWith("a1000000", StringComparison.Ordinal))
            .ToList();

        for (var i = 0; i < westland.Count; i++)
        {
            for (var j = i + 1; j < westland.Count; j++)
            {
                var a = westland[i];
                var b = westland[j];
                if (a.WorkTypes != b.WorkTypes)
                {
                    continue;
                }

                if (HaversineKm(a.Lat, a.Lng, b.Lat, b.Lng) > 1.0)
                {
                    continue;
                }

                Assert.False(
                    string.Equals(a.ImageUrl, b.ImageUrl, StringComparison.Ordinal),
                    $"Nearby same-category pair {a.Id} / {b.Id} share {a.ImageUrl}");
            }
        }
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static void SeedMinimumWageRates(JobsyDbContext db)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.AddRange(
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 15, HourlyRate = 4.22m, Label = "15", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 16, HourlyRate = 4.85m, Label = "16", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 17, HourlyRate = 5.55m, Label = "17", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 18, HourlyRate = 7.03m, Label = "18", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 19, HourlyRate = 8.44m, Label = "19", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 20, HourlyRate = 11.25m, Label = "20", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 21, HourlyRate = 14.06m, Label = "21+", EffectiveFrom = today });
        db.SaveChanges();
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
