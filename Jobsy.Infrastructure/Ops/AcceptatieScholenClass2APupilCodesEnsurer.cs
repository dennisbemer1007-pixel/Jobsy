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
/// Acceptatie: idempotent groep 7/8 demo for <c>leraar@jobsy.local</c> (≥10 pupil codes, testvenster open).
/// VMBO klas 2A blijft apart; G78 demo is o.a. <see cref="DemoSchoolIds.ClassGroep78Name"/> op demo college.
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

        var teacher = await db.Users
            .FirstOrDefaultAsync(
                u => u.Email == LeraarEmail && u.Role == UserRole.Teacher,
                cancellationToken);
        if (teacher?.SchoolId is null)
        {
            logger.LogInformation(
                "Acceptatie scholen G78 codes: skipped (teacher {Email} missing).",
                LeraarEmail);
            return false;
        }

        var schoolClass = await ResolveGroep78ClassAsync(db, teacher.SchoolId.Value, cancellationToken);
        if (schoolClass is null)
        {
            logger.LogInformation(
                "Acceptatie scholen G78 codes: skipped (no groep 7/8 class for school {SchoolId}).",
                teacher.SchoolId);
            return false;
        }

        var changed = false;
        if (schoolClass.PupilCount < MinimumPupilCodes)
        {
            schoolClass.PupilCount = MinimumPupilCodes;
            changed = true;
        }

        if (schoolClass.TestWindow != TestWindowState.Open)
        {
            schoolClass.TestWindow = TestWindowState.Open;
            changed = true;
        }

        if (schoolClass.QuestionSet != PupilQuestionSet.Groep78
            || schoolClass.Level != SchoolLevel.Groep78)
        {
            schoolClass.QuestionSet = PupilQuestionSet.Groep78;
            schoolClass.Level = SchoolLevel.Groep78;
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

        var total = await db.PupilCodes.CountAsync(c => c.SchoolClassId == schoolClass.Id, cancellationToken);
        if (total >= MinimumPupilCodes)
        {
            if (schoolClass.PupilCount < total)
            {
                schoolClass.PupilCount = total;
                await db.SaveChangesAsync(cancellationToken);
            }

            return changed;
        }

        var missing = MinimumPupilCodes - total;
        if (schoolClass.PupilCount < total + missing)
        {
            schoolClass.PupilCount = total + missing;
            await db.SaveChangesAsync(cancellationToken);
        }

        await pupilCodes.GenerateAsync(missing, schoolClass, cancellationToken);
        logger.LogInformation(
            "Acceptatie scholen G78 codes: generated {Count} codes for class {ClassId} ({Name}, total now {Target}).",
            missing,
            schoolClass.Id,
            schoolClass.Name,
            MinimumPupilCodes);
        return true;
    }

    internal static async Task<SchoolClass?> ResolveGroep78ClassAsync(
        JobsyDbContext db,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        if (schoolId == DemoSchoolIds.SchoolId)
        {
            var demo = await db.SchoolClasses
                .FirstOrDefaultAsync(c => c.Id == DemoSchoolIds.ClassGroep78Id, cancellationToken);
            if (demo is not null)
            {
                return demo;
            }
        }

        if (schoolId == TestAccountsIds.School)
        {
            var test = await db.SchoolClasses
                .FirstOrDefaultAsync(c => c.Id == TestAccountsIds.SchoolClassGroep78, cancellationToken);
            if (test is not null)
            {
                return test;
            }
        }

        var byName = await db.SchoolClasses
            .Where(c => c.SchoolId == schoolId && c.Level == SchoolLevel.Groep78)
            .OrderBy(c => c.Name == DemoSchoolIds.ClassGroep78Name ? 0 : 1)
            .ThenBy(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (byName is not null)
        {
            return byName;
        }

        if (schoolId != DemoSchoolIds.SchoolId)
        {
            return null;
        }

        var yearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow));
        var created = new SchoolClass
        {
            Id = DemoSchoolIds.ClassGroep78Id,
            SchoolId = schoolId,
            Name = DemoSchoolIds.ClassGroep78Name,
            Level = SchoolLevel.Groep78,
            QuestionSet = PupilQuestionSet.Groep78,
            Year = 8,
            SchoolYearStart = yearStart,
            PupilCount = MinimumPupilCodes,
            TestWindow = TestWindowState.Open,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(created);
        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    internal static async Task<int> CountUnusedCodesAsync(
        JobsyDbContext db,
        Guid schoolClassId,
        CancellationToken cancellationToken)
        => await db.PupilCodes.CountAsync(
            c => c.SchoolClassId == schoolClassId
                 && c.Status == PupilCodeStatus.NotStarted
                 && c.Progress == null,
            cancellationToken);
}
