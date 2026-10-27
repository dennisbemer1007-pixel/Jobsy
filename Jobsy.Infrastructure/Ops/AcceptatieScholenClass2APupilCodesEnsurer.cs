using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Hosting;
using Jobsy.Infrastructure.Scholen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Acceptatie: idempotent demo pupil codes for <c>leraar@jobsy.local</c> klas 2A (≥10 codes).
/// </summary>
public static class AcceptatieScholenClass2APupilCodesEnsurer
{
    public const int MinimumPupilCodes = 10;
    public const string LeraarEmail = "leraar@jobsy.local";

    public static async Task<bool> TryEnsureAsync(
        JobsyDbContext db,
        IPupilCodeService pupilCodes,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return false;
        }

        if (!configuration.GetValue("JobsyFeatures:SchoolsEnabled", false))
        {
            return false;
        }

        var teacher = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Email == LeraarEmail && u.Role == UserRole.Teacher,
                cancellationToken);
        if (teacher?.SchoolId is null)
        {
            logger.LogInformation(
                "Acceptatie scholen 2A codes: skipped (teacher {Email} missing).",
                LeraarEmail);
            return false;
        }

        var schoolClass = await db.SchoolClasses
            .FirstOrDefaultAsync(
                c => c.Id == TestAccountsIds.SchoolClassGroep78 && c.SchoolId == teacher.SchoolId,
                cancellationToken);
        if (schoolClass is null)
        {
            schoolClass = await db.SchoolClasses
                .FirstOrDefaultAsync(
                    c => c.SchoolId == teacher.SchoolId
                         && c.Name == "2A"
                         && c.Level == SchoolLevel.Groep78,
                    cancellationToken);
        }

        if (schoolClass is null)
        {
            logger.LogInformation(
                "Acceptatie scholen 2A codes: skipped (no Groep78 class 2A for school {SchoolId}).",
                teacher.SchoolId);
            return false;
        }

        var changed = false;
        if (!string.Equals(schoolClass.Name, "2A", StringComparison.Ordinal))
        {
            schoolClass.Name = "2A";
            changed = true;
        }

        if (schoolClass.PupilCount < MinimumPupilCodes)
        {
            schoolClass.PupilCount = MinimumPupilCodes;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.TeacherClassAssignments.AnyAsync(
                a => a.TeacherUserId == teacher.Id && a.SchoolClassId == schoolClass.Id,
                cancellationToken))
        {
            db.TeacherClassAssignments.Add(new TeacherClassAssignment
            {
                TeacherUserId = teacher.Id,
                SchoolClassId = schoolClass.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var existing = await db.PupilCodes.CountAsync(
            c => c.SchoolClassId == schoolClass.Id,
            cancellationToken);
        var missing = schoolClass.PupilCount - existing;
        if (missing <= 0)
        {
            return false;
        }

        await pupilCodes.GenerateAsync(missing, schoolClass, cancellationToken);
        logger.LogInformation(
            "Acceptatie scholen 2A codes: generated {Count} codes for class {ClassId} (total target {Target}).",
            missing,
            schoolClass.Id,
            schoolClass.PupilCount);
        return true;
    }
}
