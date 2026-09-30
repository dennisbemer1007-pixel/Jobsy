using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Scholen;

public class SchoolScopeServiceTests
{
    [Fact]
    public async Task Scope_matrix_cells_for_admin_teacher_and_setting()
    {
        await using var db = CreateDb();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var foreignClassId = Guid.NewGuid();
        var schoolAdminId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var schoolAdminAsTeacherId = Guid.NewGuid();

        db.Schools.AddRange(
            new School { Id = schoolId, Name = "A", City = "X", AllowedEmailDomains = "[]", CreatedAtUtc = DateTime.UtcNow },
            new School { Id = otherSchoolId, Name = "B", City = "Y", AllowedEmailDomains = "[]", CreatedAtUtc = DateTime.UtcNow });
        db.SchoolClasses.AddRange(
            new SchoolClass { Id = classId, SchoolId = schoolId, Name = "2B", Year = 2, SchoolYearStart = 2026, PupilCount = 10, CreatedAtUtc = DateTime.UtcNow },
            new SchoolClass { Id = foreignClassId, SchoolId = otherSchoolId, Name = "3A", Year = 3, SchoolYearStart = 2026, PupilCount = 10, CreatedAtUtc = DateTime.UtcNow });
        db.Users.AddRange(
            new User { Id = schoolAdminId, Email = "sa@a.nl", FullName = "SA", Role = UserRole.SchoolAdmin, SchoolId = schoolId },
            new User { Id = teacherId, Email = "t@a.nl", FullName = "T", Role = UserRole.Teacher, SchoolId = schoolId },
            new User { Id = schoolAdminAsTeacherId, Email = "sat@a.nl", FullName = "SAT", Role = UserRole.SchoolAdmin, SchoolId = schoolId });
        db.TeacherClassAssignments.AddRange(
            new TeacherClassAssignment { TeacherUserId = teacherId, SchoolClassId = classId, CreatedAtUtc = DateTime.UtcNow },
            new TeacherClassAssignment { TeacherUserId = schoolAdminAsTeacherId, SchoolClassId = classId, CreatedAtUtc = DateTime.UtcNow });
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            SchoolPerCodeResultsEnabled = true
        });
        await db.SaveChangesAsync();

        var features = new StubFeatures(perCode: true);
        var scope = new SchoolScopeService(db, features);

        var sa = Principal(schoolAdminId, JobsyRoles.SchoolAdmin, schoolId);
        var te = Principal(teacherId, JobsyRoles.Teacher, schoolId);
        var sat = Principal(schoolAdminAsTeacherId, JobsyRoles.SchoolAdmin, schoolId);

        Assert.Equal(schoolId, scope.GetSchoolIdOrThrow(sa));
        Assert.True(await scope.CanManageClassAsync(sa, classId));
        Assert.False(await scope.CanManageClassAsync(sa, foreignClassId));
        Assert.False(await scope.CanTeachClassAsync(sa, classId)); // not assigned
        Assert.True(await scope.CanTeachClassAsync(te, classId));
        Assert.False(await scope.CanTeachClassAsync(te, foreignClassId));
        Assert.True(await scope.CanTeachClassAsync(sat, classId)); // D2 assigned SchoolAdmin
        Assert.True(await scope.CanSeeClassTotalsAsync(sa, classId));
        Assert.True(await scope.CanSeePerCodeShortResultAsync(sa, classId));
        Assert.True(await scope.CanSeePerCodeShortResultAsync(te, classId));
        Assert.False(await scope.CanSeePerCodeDetailAsync(sa, classId));
        Assert.True(await scope.CanSeePerCodeDetailAsync(te, classId));
        Assert.True(await scope.CanSeePerCodeDetailAsync(sat, classId));

        features.PerCode = false;
        Assert.False(await scope.CanSeePerCodeShortResultAsync(sa, classId));
        Assert.True(await scope.CanSeePerCodeShortResultAsync(te, classId)); // teacher unaffected
    }

    private static ClaimsPrincipal Principal(Guid userId, string role, Guid schoolId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
            new Claim(ClaimTypes.Role, role),
            new Claim(JobsyClaimTypes.SchoolId, schoolId.ToString("D")),
        ], "test");
        return new ClaimsPrincipal(identity);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("SchoolScope-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubFeatures : IPlatformFeatureService
    {
        public bool PerCode;

        public StubFeatures(bool perCode) => PerCode = perCode;

        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                true, true, false, "http://localhost", DateTime.UtcNow,
                SchoolPerCodeResultsEnabled: PerCode));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
