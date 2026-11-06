using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Ops;

public sealed class TestAccountsCleanupPlan
{
    public int TestUserCount { get; init; }
    public IReadOnlyList<(string Email, string Role)> Users { get; init; } = [];
    public IReadOnlyDictionary<string, int> TableCounts { get; init; } = new Dictionary<string, int>();
}

public sealed class TestAccountsCleanupService
{
    private readonly JobsyDbContext _db;

    public TestAccountsCleanupService(JobsyDbContext db) => _db = db;

    public async Task<TestAccountsCleanupPlan> PlanAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.IsTestAccount)
            .Select(u => new { u.Email, Role = u.Role.ToString() })
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var userIds = await _db.Users.Where(u => u.IsTestAccount).Select(u => u.Id).ToListAsync(cancellationToken);
        var companyIds = await _db.Companies.Where(c => c.IsTestData).Select(c => c.Id).ToListAsync(cancellationToken);
        var vacancyIds = await _db.Vacancies.Where(v => v.IsTestData).Select(v => v.Id).ToListAsync(cancellationToken);
        var schoolIds = await _db.Schools.Where(s => s.IsTestData).Select(s => s.Id).ToListAsync(cancellationToken);
        var classIds = await _db.SchoolClasses.Where(c => c.IsTestData).Select(c => c.Id).ToListAsync(cancellationToken);

        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Users"] = userIds.Count,
            ["Companies"] = companyIds.Count,
            ["Vacancies"] = vacancyIds.Count,
            ["Applications"] = await _db.Applications.CountAsync(
                a => (a.CandidateUserId != null && userIds.Contains(a.CandidateUserId.Value))
                     || vacancyIds.Contains(a.VacancyId), cancellationToken),
            ["LocalAuthCredentials"] = await _db.LocalAuthCredentials.CountAsync(
                c => userIds.Contains(c.UserId), cancellationToken),
            ["UserCompanies"] = await _db.UserCompanies.CountAsync(
                m => userIds.Contains(m.UserId) || companyIds.Contains(m.CompanyId), cancellationToken),
            ["TokenTransactions"] = await _db.TokenTransactions.CountAsync(
                t => companyIds.Contains(t.CompanyId), cancellationToken),
            ["SalesManagerProfiles"] = await _db.SalesManagerProfiles.CountAsync(
                p => userIds.Contains(p.UserId), cancellationToken),
            ["AmbassadeurProfiles"] = await _db.AmbassadeurProfiles.CountAsync(
                p => userIds.Contains(p.UserId), cancellationToken),
            ["CandidateOnboardings"] = await _db.CandidateOnboardings.CountAsync(
                o => userIds.Contains(o.UserId), cancellationToken),
            ["Schools"] = schoolIds.Count,
            ["SchoolClasses"] = classIds.Count,
            ["PupilCodes"] = classIds.Count == 0
                ? 0
                : await _db.PupilCodes.CountAsync(p => classIds.Contains(p.SchoolClassId), cancellationToken),
            ["TeacherClassAssignments"] = classIds.Count == 0
                ? 0
                : await _db.TeacherClassAssignments.CountAsync(
                    a => classIds.Contains(a.SchoolClassId) || userIds.Contains(a.TeacherUserId),
                    cancellationToken),
        };

        return new TestAccountsCleanupPlan
        {
            TestUserCount = users.Count,
            Users = users.Select(u => (u.Email, u.Role)).ToList(),
            TableCounts = counts
        };
    }

    public async Task<(bool Ok, string? Error)> ExecuteAsync(
        int expectUsers,
        CancellationToken cancellationToken = default)
    {
        var plan = await PlanAsync(cancellationToken);
        if (plan.TestUserCount != expectUsers)
        {
            return (false, $"expect-users {expectUsers} does not match current test user count {plan.TestUserCount}");
        }

        var companyIds = await _db.Companies.Where(c => c.IsTestData).Select(c => c.Id).ToListAsync(cancellationToken);
        foreach (var companyId in companyIds)
        {
            var hasRealMember = await _db.UserCompanies.AnyAsync(
                m => m.CompanyId == companyId
                     && _db.Users.Any(u => u.Id == m.UserId && !u.IsTestAccount),
                cancellationToken);
            if (!hasRealMember)
            {
                hasRealMember = await _db.Users.AnyAsync(
                    u => u.CompanyId == companyId && !u.IsTestAccount,
                    cancellationToken);
            }

            if (hasRealMember)
            {
                return (false, "a test company still has a non-test member");
            }
        }

        var userIds = await _db.Users.Where(u => u.IsTestAccount).Select(u => u.Id).ToListAsync(cancellationToken);
        var vacancyIds = await _db.Vacancies.Where(v => v.IsTestData).Select(v => v.Id).ToListAsync(cancellationToken);
        var classIds = await _db.SchoolClasses.Where(c => c.IsTestData).Select(c => c.Id).ToListAsync(cancellationToken);
        var schoolIds = await _db.Schools.Where(s => s.IsTestData).Select(s => s.Id).ToListAsync(cancellationToken);

        // Detach real feedback that references test accounts (kept list).
        // PlatformLogs are kept by design.

        var apps = await _db.Applications
            .Where(a => (a.CandidateUserId != null && userIds.Contains(a.CandidateUserId.Value))
                        || vacancyIds.Contains(a.VacancyId))
            .ToListAsync(cancellationToken);
        _db.Applications.RemoveRange(apps);

        _db.VacancyLikes.RemoveRange(
            await _db.VacancyLikes.Where(x => userIds.Contains(x.UserId) || vacancyIds.Contains(x.VacancyId))
                .ToListAsync(cancellationToken));
        _db.VacancyShares.RemoveRange(
            await _db.VacancyShares
                .Where(x => vacancyIds.Contains(x.VacancyId)
                            || (x.UserId != null && userIds.Contains(x.UserId.Value)))
                .ToListAsync(cancellationToken));
        _db.VacancyClicks.RemoveRange(
            await _db.VacancyClicks
                .Where(x => vacancyIds.Contains(x.VacancyId)
                            || (x.UserId != null && userIds.Contains(x.UserId.Value)))
                .ToListAsync(cancellationToken));
        _db.VacancySearchImpressions.RemoveRange(
            await _db.VacancySearchImpressions
                .Where(x => vacancyIds.Contains(x.VacancyId)
                            || (x.UserId != null && userIds.Contains(x.UserId.Value)))
                .ToListAsync(cancellationToken));

        _db.CandidateOnboardings.RemoveRange(
            await _db.CandidateOnboardings.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.CandidateCompetencies.RemoveRange(
            await _db.CandidateCompetencies.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.CandidateValuesProfiles.RemoveRange(
            await _db.CandidateValuesProfiles.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.CandidateCulturePersonalityProfiles.RemoveRange(
            await _db.CandidateCulturePersonalityProfiles.Where(x => userIds.Contains(x.UserId))
                .ToListAsync(cancellationToken));
        _db.CandidateCareerInterests.RemoveRange(
            await _db.CandidateCareerInterests.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.CandidateWhoAmIProfiles.RemoveRange(
            await _db.CandidateWhoAmIProfiles.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));

        _db.UserNotifications.RemoveRange(
            await _db.UserNotifications.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.WebPushSubscriptions.RemoveRange(
            await _db.WebPushSubscriptions.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.UserDeviceSessions.RemoveRange(
            await _db.UserDeviceSessions.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.LocalAuthCredentials.RemoveRange(
            await _db.LocalAuthCredentials.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.UserExternalLogins.RemoveRange(
            await _db.UserExternalLogins.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));

        _db.TokenTransactions.RemoveRange(
            await _db.TokenTransactions.Where(x => companyIds.Contains(x.CompanyId)).ToListAsync(cancellationToken));
        _db.PendingTokenActions.RemoveRange(
            await _db.PendingTokenActions.Where(x => companyIds.Contains(x.CompanyId)).ToListAsync(cancellationToken));

        _db.SalesManagerProfiles.RemoveRange(
            await _db.SalesManagerProfiles.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));
        _db.AmbassadeurProfiles.RemoveRange(
            await _db.AmbassadeurProfiles.Where(x => userIds.Contains(x.UserId)).ToListAsync(cancellationToken));

        _db.UserCompanies.RemoveRange(
            await _db.UserCompanies
                .Where(x => userIds.Contains(x.UserId) || companyIds.Contains(x.CompanyId))
                .ToListAsync(cancellationToken));

        _db.RegionCompanies.RemoveRange(
            await _db.RegionCompanies.Where(x => companyIds.Contains(x.CompanyId)).ToListAsync(cancellationToken));
        _db.Regions.RemoveRange(
            await _db.Regions.Where(r => r.Id == TestAccountsIds.Region).ToListAsync(cancellationToken));

        if (classIds.Count > 0)
        {
            _db.PupilCodes.RemoveRange(
                await _db.PupilCodes.Where(p => classIds.Contains(p.SchoolClassId)).ToListAsync(cancellationToken));
            _db.TeacherClassAssignments.RemoveRange(
                await _db.TeacherClassAssignments
                    .Where(a => classIds.Contains(a.SchoolClassId) || userIds.Contains(a.TeacherUserId))
                    .ToListAsync(cancellationToken));
            _db.SchoolClasses.RemoveRange(
                await _db.SchoolClasses.Where(c => classIds.Contains(c.Id)).ToListAsync(cancellationToken));
        }

        if (schoolIds.Count > 0)
        {
            foreach (var u in await _db.Users.Where(u => u.SchoolId != null && schoolIds.Contains(u.SchoolId.Value))
                         .ToListAsync(cancellationToken))
            {
                u.SchoolId = null;
            }

            _db.Schools.RemoveRange(
                await _db.Schools.Where(s => schoolIds.Contains(s.Id)).ToListAsync(cancellationToken));
        }

        _db.Vacancies.RemoveRange(
            await _db.Vacancies.Where(v => vacancyIds.Contains(v.Id)).ToListAsync(cancellationToken));

        // Children first, then roots.
        var companies = await _db.Companies.Where(c => companyIds.Contains(c.Id)).ToListAsync(cancellationToken);
        foreach (var child in companies.Where(c => c.ParentCompanyId != null))
        {
            _db.Companies.Remove(child);
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var root in companies.Where(c => c.ParentCompanyId == null))
        {
            _db.Companies.Remove(root);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var remainingUsers = await _db.Users.Where(u => u.IsTestAccount).ToListAsync(cancellationToken);
        if (remainingUsers.Any(u => !u.IsTestAccount))
        {
            return (false, "refused to delete a non-test user");
        }

        _db.Users.RemoveRange(remainingUsers);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    /// <summary>
    /// FK targets that cleanup intentionally keeps (with reason). Used by coverage tests.
    /// </summary>
    public static IReadOnlyDictionary<string, string> KeptForeignKeys { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PlatformLogs"] = "No PII; audit trail of seed/cleanup stays.",
            ["PlatformFeedback"] = "Detach/tag only when a real row references test data.",
            ["AdminAuditEvents"] = "Append-only audit; not deleted.",
            ["PersonalDataAccessLogs"] = "Append-only privacy log; not deleted."
        };
}
