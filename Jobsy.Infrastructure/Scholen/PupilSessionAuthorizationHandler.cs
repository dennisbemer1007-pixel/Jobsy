using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Infrastructure.Scholen;

public sealed class PupilSessionAuthorizationHandler : AuthorizationHandler<PupilSessionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _clock;

    public PupilSessionAuthorizationHandler(
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
        PupilSessionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true
            || !string.Equals(user.Identity.AuthenticationType, PupilAuthDefaults.Scheme, StringComparison.Ordinal))
        {
            return;
        }

        if (!Guid.TryParse(user.FindFirst(PupilClaimTypes.PupilCodeId)?.Value, out var codeId)
            || !Guid.TryParse(user.FindFirst(PupilClaimTypes.ClassId)?.Value, out _)
            || !Guid.TryParse(user.FindFirst(PupilClaimTypes.SchoolId)?.Value, out var schoolId)
            || !int.TryParse(user.FindFirst(PupilClaimTypes.SessionVersion)?.Value, out var claimVersion)
            || !long.TryParse(user.FindFirst(PupilClaimTypes.IssuedAt)?.Value, out var iatUnix))
        {
            return;
        }

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatUnix);
        if (_clock.GetUtcNow() - issuedAt > PupilAuthDefaults.AbsoluteTimeout)
        {
            return;
        }

        var cacheKey = $"pupil-session:{codeId:D}";
        if (!_cache.TryGetValue(cacheKey, out PupilSessionCacheEntry? entry) || entry is null)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var row = await db.PupilCodes.AsNoTracking()
                .Where(c => c.Id == codeId)
                .Select(c => new
                {
                    c.SessionVersion,
                    SchoolActive = c.SchoolClass != null && c.SchoolClass.School != null && c.SchoolClass.School.IsActive,
                    SchoolId = c.SchoolClass != null ? c.SchoolClass.SchoolId : Guid.Empty
                })
                .FirstOrDefaultAsync();
            if (row is null)
            {
                return;
            }

            entry = new PupilSessionCacheEntry(row.SessionVersion, row.SchoolActive, row.SchoolId);
            _cache.Set(cacheKey, entry, TimeSpan.FromSeconds(30));
        }

        if (entry.SessionVersion != claimVersion
            || !entry.SchoolActive
            || entry.SchoolId != schoolId)
        {
            return;
        }

        context.Succeed(requirement);
    }

    private sealed record PupilSessionCacheEntry(int SessionVersion, bool SchoolActive, Guid SchoolId);
}
