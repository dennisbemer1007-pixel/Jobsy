using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class MasterdataSeeder
{
    public static async Task SeedAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.MasterdataOptions.AnyAsync())
        {
            return;
        }

        var options = new List<MasterdataOption>();
        var order = 0;
        foreach (var label in WorkTypeLabels.All)
        {
            options.Add(New(MasterdataCategories.Branch, label, label, order++, showCandidate: true, showVacancy: true));
        }

        order = 0;
        foreach (var label in DrivingLicenseLabels.All)
        {
            options.Add(New(MasterdataCategories.DrivingLicense, label, label, order++, showCandidate: true, showVacancy: true));
        }

        order = 0;
        foreach (var label in EducationLevelLabels.ProfileAll)
        {
            var onVacancy = EducationLevelLabels.VacancyAll.Contains(label, StringComparer.OrdinalIgnoreCase);
            options.Add(New(MasterdataCategories.EducationLevel, label, label, order++, showCandidate: true, showVacancy: onVacancy));
        }

        order = 0;
        foreach (var n in new[] { 1, 2, 3 })
        {
            options.Add(New(MasterdataCategories.MinEmployers, n.ToString(), n.ToString(), order++, showCandidate: false, showVacancy: true));
        }

        db.MasterdataOptions.AddRange(options);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} masterdata options.", options.Count);
    }

    private static MasterdataOption New(
        string category,
        string value,
        string label,
        int sortOrder,
        bool showCandidate,
        bool showVacancy) => new()
    {
        Id = Guid.NewGuid(),
        Category = category,
        Value = value,
        Label = label,
        SortOrder = sortOrder,
        IsActive = true,
        ShowOnCandidate = showCandidate,
        ShowOnVacancy = showVacancy
    };
}
