using System.Globalization;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Infrastructure.Scholen;

public sealed class ParentPassportSessionAuthorizationHandler
    : AuthorizationHandler<ParentPassportSessionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _clock;

    public ParentPassportSessionAuthorizationHandler(
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
        ParentPassportSessionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true
            || !string.Equals(user.Identity.AuthenticationType, ParentPassportAuthDefaults.Scheme, StringComparison.Ordinal))
        {
            return;
        }

        if (!Guid.TryParse(user.FindFirst(ParentPassportClaimTypes.PupilCodeId)?.Value, out var codeId)
            || !long.TryParse(user.FindFirst(ParentPassportClaimTypes.AccessExpiresUtc)?.Value, out var expiresUnix))
        {
            return;
        }

        var now = _clock.GetUtcNow();
        if (now >= DateTimeOffset.FromUnixTimeSeconds(expiresUnix))
        {
            return;
        }

        var cacheKey = $"parent-passport:{codeId:D}";
        if (!_cache.TryGetValue(cacheKey, out bool active) || !active)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var row = await db.PupilCodes.AsNoTracking()
                .Where(c => c.Id == codeId)
                .Select(c => new
                {
                    c.CreatedAtUtc,
                    SchoolActive = c.SchoolClass != null
                                   && c.SchoolClass.School != null
                                   && c.SchoolClass.School.IsActive
                                   && c.SchoolClass.School.ProcessorAgreementSignedOn != null
                })
                .FirstOrDefaultAsync();
            if (row is null)
            {
                return;
            }

            var accessEnds = row.CreatedAtUtc.Add(ParentPassportAuthDefaults.AccessWindow);
            active = row.SchoolActive && now.UtcDateTime <= accessEnds;
            _cache.Set(cacheKey, active, TimeSpan.FromSeconds(30));
        }

        if (!active)
        {
            return;
        }

        context.Succeed(requirement);
    }
}
