using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

public static class VacancyCategorySeeder
{
    public static async Task SeedAsync(JobsyDbContext db, ILogger logger, IVacancyCategoryService? categories = null)
    {
        if (categories is not null)
        {
            await categories.EnsureDefaultsAsync();
            await categories.BackfillVacancyCategoriesAsync();
        }
        else
        {
            var service = new Services.VacancyCategoryService(db);
            await service.EnsureDefaultsAsync();
            await service.BackfillVacancyCategoriesAsync();
        }

        logger.LogInformation("Ensured vacancy categories (defaults + vacancy backfill).");
    }
}
