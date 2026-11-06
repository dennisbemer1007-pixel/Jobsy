using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent perf seed: grid of Active vacancies for local banenkaart load tests (3000+).
/// Enabled via configuration <c>Seed:BanenkaartPerfCount</c> (Development / CI only).
/// </summary>
internal static class BanenkaartPerfVacanciesSeeder
{
    public const string TitlePrefix = "Perf banenkaart ";
    private static readonly Guid WestlandFreshId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Guid VacancyId(int n) => Guid.Parse($"b2000000-0000-4000-8000-{n:D12}");

    public static async Task SeedAsync(JobsyDbContext db, ILogger logger, int targetCount, CancellationToken ct = default)
    {
        if (targetCount <= 0)
        {
            return;
        }

        var existing = await db.Vacancies.CountAsync(
            v => v.Title.StartsWith(TitlePrefix) && v.Status == VacancyStatus.Active,
            ct);
        if (existing >= targetCount)
        {
            logger.LogInformation(
                "Banenkaart perf seed skipped: already {Existing} active vacancies (target {Target}).",
                existing,
                targetCount);
            return;
        }

        var companyId = await db.Companies.AsNoTracking()
            .Where(c => c.Id == WestlandFreshId
                        || c.VerificationStatus == CompanyVerificationStatus.Verified)
            .OrderBy(c => c.Id == WestlandFreshId ? 0 : 1)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);
        if (companyId == Guid.Empty)
        {
            companyId = await db.Companies.AsNoTracking().Select(c => c.Id).FirstAsync(ct);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = today.AddMonths(6);
        var toAdd = targetCount - existing;
        var startIndex = existing + 1;
        logger.LogInformation(
            "Banenkaart perf seed: adding {Count} vacancies (company {CompanyId})…",
            toAdd,
            companyId);

        const int cols = 60;
        const double minLat = 51.88;
        const double minLng = 4.05;
        const double latStep = 0.004;
        const double lngStep = 0.006;

        var batch = new List<Vacancy>(512);
        for (var i = 0; i < toAdd; i++)
        {
            var n = startIndex + i;
            var row = (n - 1) / cols;
            var col = (n - 1) % cols;
            var lat = minLat + row * latStep;
            var lng = minLng + col * lngStep;
            batch.Add(new Vacancy
            {
                Id = VacancyId(n),
                Title = TitlePrefix + n,
                Description = "Perf seed vacature voor banenkaart load test.",
                HourlyWage = 14.50m,
                StartDate = today,
                EndDate = endDate,
                Status = VacancyStatus.Active,
                CompanyId = companyId,
                Location = new GeoPoint(lat, lng),
                RequiredTransport = TransportMode.Bike | TransportMode.Car | TransportMode.PublicTransport,
                WorkTypes = WorkType.Horeca | WorkType.Winkel | WorkType.Logistiek,
                PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
                ContentModerationPassed = true,
                CategoryId = VacancyCategoryDefaults.RegulierId,
                MaxApplications = 8
            });

            if (batch.Count >= 500)
            {
                db.Vacancies.AddRange(batch);
                await db.SaveChangesAsync(ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            db.Vacancies.AddRange(batch);
            await db.SaveChangesAsync(ct);
        }

        var total = await db.Vacancies.CountAsync(v => v.Status == VacancyStatus.Active, ct);
        logger.LogInformation(
            "Banenkaart perf seed done. Active vacancies total: {Total}.",
            total);
    }
}
