using System.Text.RegularExpressions;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/vacancies/{vacancyId:guid}")]
public partial class VacancyEngagementController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;

    public VacancyEngagementController(JobsyDbContext db, IUserLookupService users)
    {
        _db = db;
        _users = users;
    }

    [HttpPost("clicks")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> RecordClick(
        Guid vacancyId,
        [FromBody] RecordClickRequest? request,
        CancellationToken cancellationToken)
    {
        if (!await IsPubliclyVisibleAsync(vacancyId, cancellationToken))
        {
            return NotFound();
        }

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            userId = user?.Id;
        }

        // Prefer stable client key whenever we cannot attach a DB user (anon or unprovisioned auth).
        string? anonymousKey = null;
        if (userId is null)
        {
            anonymousKey = string.IsNullOrWhiteSpace(request?.AnonymousKey)
                ? null
                : request!.AnonymousKey!.Trim();

            if (string.IsNullOrWhiteSpace(anonymousKey))
            {
                return BadRequest(new { message = "anonymousKey is verplicht zonder bekende gebruiker." });
            }

            if (anonymousKey.Length > 128 || !IsValidAnonymousKey(anonymousKey))
            {
                return BadRequest(new { message = "anonymousKey is ongeldig." });
            }
        }

        _db.VacancyClicks.Add(new VacancyClick
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            UserId = userId,
            AnonymousKey = anonymousKey,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { recorded = true, anonymousKey });
    }

    [HttpGet("like")]
    [Authorize]
    public async Task<ActionResult<LikeStatusDto>> GetLike(Guid vacancyId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!await IsPubliclyVisibleAsync(vacancyId, cancellationToken))
        {
            return NotFound();
        }

        var liked = await _db.VacancyLikes.AsNoTracking()
            .AnyAsync(l => l.VacancyId == vacancyId && l.UserId == user.Id, cancellationToken);

        return Ok(new LikeStatusDto(liked));
    }

    [HttpPost("like")]
    [Authorize]
    public async Task<ActionResult<LikeStatusDto>> Like(Guid vacancyId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!await IsPubliclyVisibleAsync(vacancyId, cancellationToken))
        {
            return NotFound();
        }

        var existing = await _db.VacancyLikes
            .FirstOrDefaultAsync(l => l.VacancyId == vacancyId && l.UserId == user.Id, cancellationToken);

        if (existing is null)
        {
            _db.VacancyLikes.Add(new VacancyLike
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyId,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new LikeStatusDto(true));
    }

    [HttpDelete("like")]
    [Authorize]
    public async Task<ActionResult<LikeStatusDto>> Unlike(Guid vacancyId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var existing = await _db.VacancyLikes
            .FirstOrDefaultAsync(l => l.VacancyId == vacancyId && l.UserId == user.Id, cancellationToken);

        if (existing is not null)
        {
            _db.VacancyLikes.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new LikeStatusDto(false));
    }

    [HttpPost("shares")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<ShareRecordedDto>> Share(
        Guid vacancyId,
        [FromBody] ShareVacancyRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Channel))
        {
            return BadRequest(new { message = "Ongeldig deelkanaal." });
        }

        if (!await IsPubliclyVisibleAsync(vacancyId, cancellationToken))
        {
            return NotFound();
        }

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            userId = user?.Id;
        }

        var share = new VacancyShare
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            UserId = userId,
            Channel = request.Channel,
            CreatedAt = DateTime.UtcNow
        };

        _db.VacancyShares.Add(share);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ShareRecordedDto(share.Id, share.Channel, share.CreatedAt));
    }

    private async Task<bool> IsPubliclyVisibleAsync(Guid vacancyId, CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
        if (vacancy is null)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return VacancyVisibilityRules.IsPubliclyVisible(vacancy, today);
    }

    private static bool IsValidAnonymousKey(string anonymousKey)
    {
        if (AnonymousGuidKeyRegex().IsMatch(anonymousKey))
        {
            return true;
        }

        return anonymousKey.Length is >= 10 and <= 128
            && anonymousKey.StartsWith("anon-", StringComparison.Ordinal);
    }

    [GeneratedRegex("^anon-[0-9a-fA-F-]{36}$", RegexOptions.CultureInvariant)]
    private static partial Regex AnonymousGuidKeyRegex();
}
