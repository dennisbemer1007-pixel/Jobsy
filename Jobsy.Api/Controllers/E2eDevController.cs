using System.Security.Claims;
using Jobsy.Core.Dev;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

/// <summary>Development-only helpers for Playwright (never enabled in Production).</summary>
[ApiController]
[Route("api/e2e")]
public sealed class E2eDevController : ControllerBase
{
    [HttpGet("last-email-code")]
    [AllowAnonymous]
    public IActionResult LastEmailCode([FromQuery] string? email)
    {
        if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        if (!DevEmailCodeCapture.Enabled)
        {
            return NotFound(new { message = "JOBSY_E2E_CAPTURE_EMAIL_CODES is off" });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email required" });
        }

        if (!DevEmailCodeCapture.TryGetEntry(email, out var entry))
        {
            return NotFound(new { message = "no code captured yet" });
        }

        return Ok(new
        {
            email = email.Trim().ToLowerInvariant(),
            code = entry.Code,
            sequence = entry.Sequence,
            challengeId = entry.ChallengeId
        });
    }

    [HttpGet("open-email-challenge")]
    [AllowAnonymous]
    public async Task<IActionResult> OpenEmailChallenge(
        [FromQuery] string? email,
        [FromServices] JobsyDbContext db,
        CancellationToken cancellationToken)
    {
        if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        if (!DevEmailCodeCapture.Enabled || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email required" });
        }

        var normalized = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var challenge = await db.EmailSignInChallenges.AsNoTracking()
            .Where(c => c.EmailNormalized == normalized
                        && c.ConsumedAtUtc == null
                        && c.ExpiresAtUtc > now)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new { c.Id, c.CreatedAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null)
        {
            return NotFound(new { message = "no open challenge" });
        }

        string? code = null;
        long? sequence = null;
        if (DevEmailCodeCapture.TryGetForChallenge(challenge.Id, out var forChallenge))
        {
            code = forChallenge;
            if (DevEmailCodeCapture.TryGetEntry(email, out var capture))
            {
                sequence = capture.Sequence;
            }
        }

        return Ok(new
        {
            email = normalized,
            challengeId = challenge.Id,
            code,
            sequence,
            createdAtUtc = challenge.CreatedAtUtc
        });
    }

    [HttpPost("clear-email-codes")]
    [AllowAnonymous]
    public IActionResult ClearEmailCodes()
    {
        if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        if (!DevEmailCodeCapture.Enabled)
        {
            return NotFound(new { message = "JOBSY_E2E_CAPTURE_EMAIL_CODES is off" });
        }

        DevEmailCodeCapture.Clear();
        return Ok(new { cleared = true });
    }

    [HttpPost("fixture/external-vacancy")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalVacancyFixture(
        [FromQuery] string? email,
        [FromServices] JobsyDbContext db,
        [FromServices] IHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment() || !DevEmailCodeCapture.Enabled)
        {
            return NotFound();
        }

        Guid userId;
        if (User.Identity?.IsAuthenticated == true
            && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var claimId))
        {
            userId = claimId;
        }
        else if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email required when unauthenticated" });
        }
        else
        {
            var normalized = email.Trim().ToLowerInvariant();
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
            if (user is null)
            {
                return NotFound(new { message = "user not found" });
            }

            userId = user.Id;
        }

        var id = Guid.NewGuid();
        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = id,
            CandidateUserId = userId,
            SourceUrl = "https://example.com/e2e-vacancy",
            SourceHost = "example.com",
            Title = "E2E Buitenbaan",
            CompanyName = "Demo Werkgever",
            Place = "Den Haag",
            HoursText = "32 uur",
            PayText = "€ 14/uur",
            StartText = "Per direct",
            TrainingText = "Inwerktraject",
            RequirementsBulletsJson = "[\"Rijbewijs B\"]",
            Status = CandidateExternalVacancyStatus.Saved,
            SavedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { id });
    }
}
