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
/// Acceptatie: idempotent MBO/HBO demo classes on <see cref="DemoSchoolIds.SchoolId"/> only,
/// assigned to <see cref="TestLeraarEmail"/> (cross-school assignment allowed).
/// </summary>
public static class AcceptatieScholenMboHboDemoEnsurer
{
    public const int MinimumPupilCodes = 10;
    public const string TestLeraarEmail = "test-leraar@lobsy.nl";

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

        var school = await db.Schools
            .FirstOrDefaultAsync(s => s.Id == DemoSchoolIds.SchoolId, cancellationToken);
        if (school is null || !school.IsTestData)
        {
            logger.LogInformation(
                "Acceptatie MBO/HBO demo: skipped (demo school {SchoolId} missing or not test data).",
                DemoSchoolIds.SchoolId);
            return false;
        }

        var teacher = await db.Users
            .FirstOrDefaultAsync(
                u => u.Email == TestLeraarEmail && u.Role == UserRole.Teacher,
                cancellationToken);
        if (teacher is null)
        {
            logger.LogInformation(
                "Acceptatie MBO/HBO demo: skipped (teacher {Email} missing).",
                TestLeraarEmail);
            return false;
        }

        var changed = false;
        changed |= await EnsureClassAsync(
            db,
            pupilCodes,
            logger,
            teacher,
            DemoSchoolIds.ClassMbo2Id,
            DemoSchoolIds.ClassMbo2Name,
            SchoolLevel.Mbo2,
            year: 2,
            cancellationToken);
        changed |= await EnsureClassAsync(
            db,
            pupilCodes,
            logger,
            teacher,
            DemoSchoolIds.ClassHboId,
            DemoSchoolIds.ClassHboName,
            SchoolLevel.Hbo,
            year: 1,
            cancellationToken);

        return changed;
    }

    private static async Task<bool> EnsureClassAsync(
        JobsyDbContext db,
        IPupilCodeService pupilCodes,
        ILogger logger,
        User teacher,
        Guid classId,
        string name,
        SchoolLevel level,
        int year,
        CancellationToken cancellationToken)
    {
        var questionSet = SchoolLevelRules.QuestionSetFor(level);
        var schoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow));
        var changed = false;

        var schoolClass = await db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (schoolClass is null)
        {
            schoolClass = new SchoolClass
            {
                Id = classId,
                SchoolId = DemoSchoolIds.SchoolId,
                Name = name,
                Level = level,
                QuestionSet = questionSet,
                Year = year,
                SchoolYearStart = schoolYearStart,
                PupilCount = MinimumPupilCodes,
                TestWindow = TestWindowState.Open,
                IsTestData = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.SchoolClasses.Add(schoolClass);
            await db.SaveChangesAsync(cancellationToken);
            changed = true;
        }
        else if (schoolClass.SchoolId != DemoSchoolIds.SchoolId)
        {
            logger.LogWarning(
                "Acceptatie MBO/HBO demo: class {ClassId} exists on another school; skipped.",
                classId);
            return false;
        }
        else
        {
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

            if (schoolClass.Level != level || schoolClass.QuestionSet != questionSet)
            {
                schoolClass.Level = level;
                schoolClass.QuestionSet = questionSet;
                changed = true;
            }

            if (!string.Equals(schoolClass.Name, name, StringComparison.Ordinal))
            {
                schoolClass.Name = name;
                changed = true;
            }

            if (!schoolClass.IsTestData)
            {
                schoolClass.IsTestData = true;
                changed = true;
            }

            if (changed)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
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
            changed = true;
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
            "Acceptatie MBO/HBO demo: generated {Count} codes for class {ClassId} ({Name}, level {Level}).",
            missing,
            schoolClass.Id,
            schoolClass.Name,
            level);
        return true;
    }
}
