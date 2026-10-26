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
        foreach (var title in new[] { "Vakantiewerk vitrine", "Hulp bakker" })
        {
            var rows = await db.Vacancies
                .Where(v => companyIds.Contains(v.CompanyId) && v.Title == title && v.Status == VacancyStatus.Active)
                .OrderBy(v => v.Id)
                .ToListAsync(cancellationToken);
            if (rows.Count <= 1)
            {
                continue;
            }

            foreach (var duplicate in rows.Skip(1))
            {
                duplicate.Status = VacancyStatus.Archived;
            }

            logger.LogInformation(
                "Archived {Count} duplicate demo vacancies titled {Title}.",
                rows.Count - 1,
                title);
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
