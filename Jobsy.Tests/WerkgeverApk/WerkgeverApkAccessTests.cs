using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Infrastructure.WerkgeverApk;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkAccessTests
{
    [Fact]
    public async Task Login_fails_when_global_feature_off()
    {
        var (db, _, code, plain) = await WerkgeverApkTestSupport.SeedTeamWithCodeAsync();
        var sut = new WerkgeverApkEmployeePortalService(
            db,
            new StubCodes(code.CodeLookupHash),
            new WerkgeverApkTestSupport.FlagsOff(),
            new MemoryCache(new MemoryCacheOptions()));

        var (ok, error, status) = await sut.LoginAsync(
            new WerkgeverApkEmployeeLoginRequest(plain));
        Assert.Null(ok);
        Assert.Equal(404, status);
        Assert.Equal(WerkgeverApkEmployeePortalService.FeatureOff, error?.Error);
    }

    [Fact]
    public async Task Login_with_code_only_resolves_team_when_guids_empty()
    {
        var (db, team, code, plain) = await WerkgeverApkTestSupport.SeedTeamWithCodeAsync();
        var codes = new StubCodes(code.CodeLookupHash);
        var sut = new WerkgeverApkEmployeePortalService(
            db,
            codes,
            new WerkgeverApkTestSupport.FlagsOn(),
            new MemoryCache(new MemoryCacheOptions()));

        var (ok, error, status) = await sut.LoginAsync(
            new WerkgeverApkEmployeeLoginRequest(plain));
        Assert.Null(error);
        Assert.Equal(200, status);
        Assert.NotNull(ok);
        Assert.Equal(code.Id, ok!.EmployeeCodeId);
        Assert.Equal(team.Id, ok.TeamId);
    }

    [Fact]
    public async Task Login_fails_when_company_loopbaan_apk_disabled()
    {
        var (db, _, code, plain) = await WerkgeverApkTestSupport.SeedTeamWithCodeAsync(companyEnabled: false);
        var sut = new WerkgeverApkEmployeePortalService(
            db,
            new StubCodes(code.CodeLookupHash),
            new WerkgeverApkTestSupport.FlagsOn(),
            new MemoryCache(new MemoryCacheOptions()));

        var (ok, _, status) = await sut.LoginAsync(
            new WerkgeverApkEmployeeLoginRequest(plain));
        Assert.Null(ok);
        Assert.Equal(401, status);
    }

  [Fact]
    public async Task Manager_team_overview_denies_other_company()
    {
        var (dbA, teamA, _, _) = await WerkgeverApkTestSupport.SeedTeamWithCodeAsync();

        var manager = TestPrincipal(teamA.CompanyId);
        var sut = new WerkgeverApkManagerPortalService(
            dbA,
            new StubCompanyAuth([teamA.CompanyId]),
            new StubCodes(),
            new WerkgeverApkTestSupport.FlagsOn(),
            TimeProvider.System);

        var wrongTeamId = Guid.NewGuid();
        var (ok, error, status) = await sut.GetTeamOverviewAsync(manager, wrongTeamId);
        Assert.Null(ok);
        Assert.Equal(404, status);
        Assert.Equal("not_found", error?.Error);
    }

    private static ClaimsPrincipal TestPrincipal(Guid companyId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
            new("company_id", companyId.ToString("D")),
            new(ClaimTypes.Role, JobsyRoles.EnterpriseManager)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private sealed class StubCompanyAuth(IReadOnlyCollection<Guid> ids) : Jobsy.Core.Interfaces.ICompanyAuthorizationService
    {
        public bool IsAdmin(ClaimsPrincipal user) => false;

        public bool IsEmployer(ClaimsPrincipal user) => true;

        public bool IsCandidate(ClaimsPrincipal user) => false;

        public Jobsy.Core.Enums.UserRole? GetPrimaryRole(ClaimsPrincipal user) => Jobsy.Core.Enums.UserRole.EnterpriseManager;

        public Task<IReadOnlyCollection<Guid>?> GetAccessibleCompanyIdsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Guid>?>(ids);

        public Task<bool> CanAccessCompanyAsync(ClaimsPrincipal user, Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult(ids.Contains(companyId));

        public Task EnsureCanAccessCompanyAsync(ClaimsPrincipal user, Guid companyId, CancellationToken cancellationToken = default)
        {
            if (!ids.Contains(companyId))
            {
                throw new Jobsy.Core.Exceptions.ForbiddenCompanyAccessException(companyId);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StubCodes(string? fixedHash = null) : IWerkgeverApkEmployeeCodeService
    {
        public Task<IReadOnlyList<Jobsy.Core.Entities.WerkgeverApk.WerkgeverApkEmployeeCode>> GenerateAsync(
            int count,
            Jobsy.Core.Entities.WerkgeverApk.WerkgeverApkTeam team,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public string LookupHash(string normalizedCode)
        {
            if (fixedHash is not null)
            {
                return fixedHash;
            }

            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(normalizedCode))).ToLowerInvariant();
        }

        public string Protect(string normalizedCode) => normalizedCode;

        public string? Unprotect(string protectedPayload) => protectedPayload;
    }
}