using Jobsy.Api.Models;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly JobsyDbContext _db;

    public AuthController(JobsyDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Validates a local registration credential (hashed password).
    /// Used by the Web login page when the email is not a seeded DemoUser.
    /// </summary>
    [HttpPost("local-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LocalLoginResponse>> LocalLogin(
        [FromBody] LocalLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "E-mail en wachtwoord zijn verplicht." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        if (credential is null
            || !JobsyPasswordHasher.Verify(request.Password, credential.PasswordHash))
        {
            return Unauthorized(new { message = "Ongeldige e-mail of wachtwoord." });
        }

        if (JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(request.Password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Id == credential.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Account is niet actief." });
        }

        var companyIds = user.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
        if (user.CompanyId is Guid primary && !companyIds.Contains(primary))
        {
            companyIds.Insert(0, primary);
        }

        return Ok(new LocalLoginResponse(
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            companyIds));
    }
}
