using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountSetupController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IOneTimeLinkService _links;

    public AccountSetupController(JobsyDbContext db, IOneTimeLinkService links)
    {
        _db = db;
        _links = links;
    }

    public sealed record SetupLinkPreviewResponse(bool Valid, string? MaskedEmail = null, DateTime? ExpiresAtUtc = null);

    public sealed record SetupPasswordRequest(string? Token, string? Password);

    [AllowAnonymous]
    [HttpGet("setup-link")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<SetupLinkPreviewResponse>> PreviewSetupLink(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        var peek = await _links.PeekAsync(OneTimeLinkPurpose.SetPassword, token ?? "", cancellationToken);
        if (!peek.Valid)
        {
            return Ok(new SetupLinkPreviewResponse(Valid: false));
        }

        return Ok(new SetupLinkPreviewResponse(
            Valid: true,
            MaskedEmail: peek.MaskedEmail,
            ExpiresAtUtc: peek.ExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("setup-password")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> SetupPassword(
        [FromBody] SetupPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            RegistrationPasswordRules.Validate(request.Password, required: true);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var link = await _links.ConsumeAsync(
            OneTimeLinkPurpose.SetPassword,
            request.Token ?? "",
            cancellationToken);
        if (link is null || link.UserId is not Guid userId)
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return BadRequest(new { message = "invalid_or_expired" });
        }

        var email = string.IsNullOrWhiteSpace(link.Email) ? user.Email.Trim().ToLowerInvariant() : link.Email;
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        var hadCredential = credential is not null;
        var hash = JobsyPasswordHasher.Hash(request.Password!);

        if (credential is null)
        {
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = hash,
                FailedLoginCount = 0,
                LockoutUntil = null
            });
        }
        else
        {
            credential.Email = email;
            credential.PasswordHash = hash;
            credential.FailedLoginCount = 0;
            credential.LockoutUntil = null;
            user.SessionVersion++;
        }

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "account.password-set",
            Message = $"account.password-set user={user.Id:N} link={link.Id:N}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { ok = true, hadCredential });
    }
}
