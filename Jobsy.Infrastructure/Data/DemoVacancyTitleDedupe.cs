using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class DemoVacancyTitleDedupe
{
    internal static async Task ArchiveDuplicateTitlesAsync(
        JobsyDbContext db,
        IReadOnlyList<Guid> companyIds,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var archived = 0;
        foreach (var title in new[] { "Vakantiewerk vitrine", "Hulp bakker" })
        {
            archived += await ArchiveDuplicatesForTitleAsync(db, companyIds, title, cancellationToken);
        }

        archived += await ArchiveAllDuplicateActiveTitlesAsync(db, companyIds, cancellationToken);

        if (archived > 0)
        {
            logger.LogInformation("Archived {Count} duplicate demo vacancies (title dedupe).", archived);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<int> ArchiveDuplicatesForTitleAsync(
        JobsyDbContext db,
        IReadOnlyList<Guid> companyIds,
        string title,
        CancellationToken cancellationToken)
    {
        var demoVacancyIds = AfterMatchDemoIds.AllDemoApplicationIds
            .Select(AfterMatchDemoIds.VacancyForApplication)
            .ToHashSet();
        var rows = await db.Vacancies
            .Where(v => companyIds.Contains(v.CompanyId) && v.Title == title && v.Status == VacancyStatus.Active)
            .OrderBy(v => v.Id)
            .ToListAsync(cancellationToken);
        rows = rows.Where(v => !demoVacancyIds.Contains(v.Id)).ToList();
        if (rows.Count <= 1)
        {
            return 0;
        }

        foreach (var duplicate in rows.Skip(1))
        {
            duplicate.Status = VacancyStatus.Archived;
        }

        return rows.Count - 1;
    }

    private static async Task<int> ArchiveAllDuplicateActiveTitlesAsync(
        JobsyDbContext db,
        IReadOnlyList<Guid> companyIds,
        CancellationToken cancellationToken)
    {
        var demoVacancyIds = AfterMatchDemoIds.AllDemoApplicationIds
            .Select(AfterMatchDemoIds.VacancyForApplication)
            .ToHashSet();
        var active = await db.Vacancies
            .Where(v => companyIds.Contains(v.CompanyId) && v.Status == VacancyStatus.Active)
            .OrderBy(v => v.Id)
            .Select(v => new { v.Id, v.CompanyId, v.Title })
            .ToListAsync(cancellationToken);
        active = active.Where(v => !demoVacancyIds.Contains(v.Id)).ToList();

        var seen = new HashSet<(Guid CompanyId, string Title)>();
        var toArchive = new List<Guid>();
        foreach (var row in active)
        {
            var key = (row.CompanyId, row.Title);
            if (!seen.Add(key))
            {
                toArchive.Add(row.Id);
            }
        }

        if (toArchive.Count == 0)
        {
            return 0;
        }

        var rows = await db.Vacancies
            .Where(v => toArchive.Contains(v.Id))
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            row.Status = VacancyStatus.Archived;
        }

        return rows.Count;
    }
}
