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
/// assigned to <see cref="TestLeraarEmail"/> and demo <see cref="DemoLeraarEmail"/> when present.
/// </summary>
public static class AcceptatieScholenMboHboDemoEnsurer
{
    public const int MinimumPupilCodes = 10;
    public const string TestLeraarEmail = "test-leraar@lobsy.nl";
    public const string DemoLeraarEmail = AcceptatieScholenClass2APupilCodesEnsurer.LeraarEmail;

    private static readonly (Guid ClassId, string Name, SchoolLevel Level, int Year)[] DemoClasses =
    [
        (DemoSchoolIds.ClassMbo2Id, DemoSchoolIds.ClassMbo2Name, SchoolLevel.Mbo2, 2),
        (DemoSchoolIds.ClassHboId, DemoSchoolIds.ClassHboName, SchoolLevel.Hbo, 1)
    ];

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

        var teachers = await ResolveTeachersAsync(db, cancellationToken);
        if (teachers.Count == 0)
        {
            logger.LogInformation(
                "Acceptatie MBO/HBO demo: skipped (no teachers {TestEmail} / {DemoEmail}).",
                TestLeraarEmail,
                DemoLeraarEmail);
            return false;
        }

        var changed = false;
        var classIds = new List<Guid>();
        foreach (var spec in DemoClasses)
        {
            var (classChanged, schoolClass) = await EnsureClassAsync(
                db,
                pupilCodes,
                logger,
                spec.ClassId,
                spec.Name,
                spec.Level,
                spec.Year,
                cancellationToken);
            changed |= classChanged;
            if (schoolClass is null)
            {
                continue;
            }

            classIds.Add(schoolClass.Id);
            foreach (var teacher in teachers)
            {
                if (await EnsureTeacherAssignmentAsync(db, logger, teacher, schoolClass, cancellationToken))
                {
                    changed = true;
                }
            }
        }

        if (classIds.Count == DemoClasses.Length)
        {
            var linked = new List<string>();
            foreach (var teacher in teachers)
            {
                var onAll = true;
                foreach (var classId in classIds)
                {
                    if (!await db.TeacherClassAssignments.AnyAsync(
                            a => a.TeacherUserId == teacher.Id && a.SchoolClassId == classId,
                            cancellationToken))
                    {
                        onAll = false;
                        break;
                    }
                }

                if (onAll)
                {
                    linked.Add(teacher.Email);
                }
            }

            logger.LogInformation(
                "Acceptatie MBO/HBO demo: applied (demo college MBO/HBO classes; teacher assignments: {Emails}).",
                linked.Count > 0 ? string.Join(", ", linked) : "(none)");
        }

        return changed;
    }

    private static async Task<List<User>> ResolveTeachersAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken)
    {
        var emails = new[] { TestLeraarEmail, DemoLeraarEmail };
        return await db.Users
            .Where(u => u.Role == UserRole.Teacher && emails.Contains(u.Email))
            .ToListAsync(cancellationToken);
    }

    private static async Task<bool> EnsureTeacherAssignmentAsync(
        JobsyDbContext db,
        ILogger logger,
        User teacher,
        SchoolClass schoolClass,
        CancellationToken cancellationToken)
    {
        if (await db.TeacherClassAssignments.AnyAsync(
                a => a.TeacherUserId == teacher.Id && a.SchoolClassId == schoolClass.Id,
                cancellationToken))
        {
            return false;
        }

        db.TeacherClassAssignments.Add(new TeacherClassAssignment
        {
            TeacherUserId = teacher.Id,
            SchoolClassId = schoolClass.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Acceptatie MBO/HBO demo: assigned {Email} to class {ClassId} ({Name}).",
            teacher.Email,
            schoolClass.Id,
            schoolClass.Name);
        return true;
    }

    private static async Task<(bool Changed, SchoolClass? Class)> EnsureClassAsync(
        JobsyDbContext db,
        IPupilCodeService pupilCodes,
        ILogger logger,
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
            return (false, null);
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

        var total = await db.PupilCodes.CountAsync(c => c.SchoolClassId == schoolClass.Id, cancellationToken);
        if (total >= MinimumPupilCodes)
        {
            if (schoolClass.PupilCount < total)
            {
                schoolClass.PupilCount = total;
                await db.SaveChangesAsync(cancellationToken);
                changed = true;
            }

            return (changed, schoolClass);
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
        return (true, schoolClass);
    }
}
