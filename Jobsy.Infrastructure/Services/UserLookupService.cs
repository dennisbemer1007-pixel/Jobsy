using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class UserLookupService : IUserLookupService
{
    private readonly JobsyDbContext _db;

    public UserLookupService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var idRaw = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (Guid.TryParse(idRaw, out var userId))
        {
            var byId = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            email = principal.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(email) && Guid.TryParse(email, out _))
            {
                email = null;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower() && u.IsActive, cancellationToken);
    }
}
