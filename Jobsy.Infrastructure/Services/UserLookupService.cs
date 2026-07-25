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
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? principal.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
    }
}
