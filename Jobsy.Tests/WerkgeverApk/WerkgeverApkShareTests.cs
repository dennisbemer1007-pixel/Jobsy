using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Infrastructure.WerkgeverApk;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkShareTests
{
    [Fact]
    public async Task SetShare_rejects_incomplete_employee()
    {
        var (db, _, code, _) = await WerkgeverApkTestSupport.SeedTeamWithCodeAsync();
        var sut = new WerkgeverApkEmployeePortalService(
            db,
            new StubCodeService(),
            new WerkgeverApkTestSupport.FlagsOn(),
            new MemoryCache(new MemoryCacheOptions()));
        var principal = await PrincipalAsync(db, code);

        var (_, error, status) = await sut.SetShareAsync(principal, share: true);
        Assert.Equal(400, status);
        Assert.Equal("not_complete", error?.Error);
    }

    [Fact]
    public async Task SetShare_allows_completed_employee()
    {
        var (db, _, code, _) = await WerkgeverApkTestSupport.SeedTeamWithCodeAsync();
        code.Status = WerkgeverApkEmployeeCodeStatus.Completed;
        db.WerkgeverApkEmployeeProgress.Add(new Jobsy.Core.Entities.WerkgeverApk.WerkgeverApkEmployeeProgress
        {
            EmployeeCodeId = code.Id,
            ProgressJson = CompleteProgressJson(),
            WelcomeSeen = true,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new WerkgeverApkEmployeePortalService(
            db,
            new StubCodeService(),
            new WerkgeverApkTestSupport.FlagsOn(),
            new MemoryCache(new MemoryCacheOptions()));
        var principal = await PrincipalAsync(db, code);

        var (ok, error, status) = await sut.SetShareAsync(principal, share: true);
        Assert.Null(error);
        Assert.Equal(200, status);
        Assert.True(ok!.SharedWithManager);
    }

    private static async Task<ClaimsPrincipal> PrincipalAsync(
        Jobsy.Infrastructure.Data.JobsyDbContext db,
        Jobsy.Core.Entities.WerkgeverApk.WerkgeverApkEmployeeCode code)
    {
        var companyId = await db.WerkgeverApkTeams.Where(t => t.Id == code.TeamId).Select(t => t.CompanyId).FirstAsync();
        var claims = new[]
        {
            new Claim(WerkgeverApkClaimTypes.EmployeeCodeId, code.Id.ToString("D")),
            new Claim(WerkgeverApkClaimTypes.TeamId, code.TeamId.ToString("D")),
            new Claim(WerkgeverApkClaimTypes.CompanyId, companyId.ToString("D")),
            new Claim(WerkgeverApkClaimTypes.SessionVersion, code.SessionVersion.ToString()),
            new Claim(WerkgeverApkClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, WerkgeverApkAuthDefaults.Scheme));
    }

    private static string CompleteProgressJson()
    {
        var catalog = PupilMboHboCatalog.Default;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers[q.Id] = 3;
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(new { answers });
    }

    private sealed class StubCodeService : IWerkgeverApkEmployeeCodeService
    {
        public Task<IReadOnlyList<Jobsy.Core.Entities.WerkgeverApk.WerkgeverApkEmployeeCode>> GenerateAsync(
            int count,
            Jobsy.Core.Entities.WerkgeverApk.WerkgeverApkTeam team,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public string LookupHash(string normalizedCode) => normalizedCode;
        public string Protect(string normalizedCode) => normalizedCode;
        public string? Unprotect(string protectedPayload) => protectedPayload;
    }
}
