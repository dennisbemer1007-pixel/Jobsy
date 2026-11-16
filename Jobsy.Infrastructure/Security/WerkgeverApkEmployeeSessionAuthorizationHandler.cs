using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.WerkgeverApk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Infrastructure.Security;

public sealed class WerkgeverApkEmployeeSessionAuthorizationHandler
    : AuthorizationHandler<WerkgeverApkEmployeeSessionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _clock;

    public WerkgeverApkEmployeeSessionAuthorizationHandler(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        TimeProvider? clock = null)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _clock = clock ?? TimeProvider.System;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WerkgeverApkEmployeeSessionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true
            || !string.Equals(user.Identity.AuthenticationType, WerkgeverApkAuthDefaults.Scheme, StringComparison.Ordinal))
        {
            return;
        }

        if (!Guid.TryParse(user.FindFirst(WerkgeverApkClaimTypes.EmployeeCodeId)?.Value, out var codeId)
            || !Guid.TryParse(user.FindFirst(WerkgeverApkClaimTypes.TeamId)?.Value, out _)
            || !Guid.TryParse(user.FindFirst(WerkgeverApkClaimTypes.CompanyId)?.Value, out var companyId)
            || !int.TryParse(user.FindFirst(WerkgeverApkClaimTypes.SessionVersion)?.Value, out var claimVersion)
            || !long.TryParse(user.FindFirst(WerkgeverApkClaimTypes.IssuedAt)?.Value, out var iatUnix))
        {
            return;
        }

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatUnix);
        if (_clock.GetUtcNow() - issuedAt > WerkgeverApkAuthDefaults.AbsoluteTimeout)
        {
            return;
        }

        var cacheKey = $"werkgever-apk-session:{codeId:D}";
        if (!_cache.TryGetValue(cacheKey, out SessionCacheEntry? entry) || entry is null)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var row = await db.WerkgeverApkEmployeeCodes.AsNoTracking()
                .Where(c => c.Id == codeId)
                .Select(c => new
                {
                    c.SessionVersion,
                    c.Status,
                    CompanyId = c.Team != null ? c.Team.CompanyId : Guid.Empty,
                    CompanyEnabled = c.Team != null
                                     && c.Team.Company != null
                                     && c.Team.Company.LoopbaanApkEnabled
                })
                .FirstOrDefaultAsync();
            if (row is null || row.Status == Core.Enums.WerkgeverApkEmployeeCodeStatus.Revoked)
            {
                return;
            }

            entry = new SessionCacheEntry(row.SessionVersion, row.CompanyId, row.CompanyEnabled);
            _cache.Set(cacheKey, entry, TimeSpan.FromSeconds(30));
        }

        if (entry.SessionVersion != claimVersion
            || entry.CompanyId != companyId
            || !entry.CompanyEnabled)
        {
            return;
        }

        context.Succeed(requirement);
    }

    private sealed record SessionCacheEntry(int SessionVersion, Guid CompanyId, bool CompanyEnabled);
}
