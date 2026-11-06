using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Ops;
using Jobsy.Core.Rules;
using Jobsy.Core.Hosting;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Jobsy.Core.Features;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/vacancies/{vacancyId:guid}")]
[RequiresFeature(PlatformFeature.Employers)]
public class VacancyEngagementController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;
    private readonly DeploymentEnvironmentLabel _deployment;
    private readonly ILogger<VacancyEngagementController> _logger;

    public VacancyEngagementController(
        JobsyDbContext db,
        IUserLookupService users,
        DeploymentEnvironmentLabel deployment,
        ILogger<VacancyEngagementController> logger)
    {
        _db = db;
        _users = users;
        _deployment = deployment;
        _logger = logger;
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

        if (!AnalyticsConsent.IsGranted(Request))
        {
            return Ok(new { recorded = false });
        }

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            if (PublicStatsExclusion.ShouldSkip(user, User))
            {
                return Ok(new { recorded = false });
            }

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

            if (!AnonymousKeyRules.IsValid(anonymousKey))
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
        try
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

            await SetLikedAsync(vacancyId, user.Id, liked: true, cancellationToken);
            return Ok(new LikeStatusDto(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST like failed for vacancy {VacancyId}", vacancyId);
            AcceptatieDiagnosticHeaders.TrySet(Response, _deployment, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("like")]
    [Authorize]
    public async Task<ActionResult<LikeStatusDto>> Unlike(Guid vacancyId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            if (user is null)
            {
                return Unauthorized();
            }

            await SetLikedAsync(vacancyId, user.Id, liked: false, cancellationToken);
            return Ok(new LikeStatusDto(false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE like failed for vacancy {VacancyId}", vacancyId);
            AcceptatieDiagnosticHeaders.TrySet(Response, _deployment, ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task SetLikedAsync(
        Guid vacancyId,
        Guid userId,
        bool liked,
        CancellationToken cancellationToken)
    {
        if (!liked)
        {
            var existing = await _db.VacancyLikes
                .FirstOrDefaultAsync(l => l.VacancyId == vacancyId && l.UserId == userId, cancellationToken);
            if (existing is not null)
            {
                _db.VacancyLikes.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var row = await _db.VacancyLikes
            .FirstOrDefaultAsync(l => l.VacancyId == vacancyId && l.UserId == userId, cancellationToken);
        if (row is not null)
        {
            return;
        }

        _db.VacancyLikes.Add(new VacancyLike
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateVacancyLike(ex))
        {
            _db.ChangeTracker.Clear();
        }
    }

    private static bool IsDuplicateVacancyLike(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return true;
        }

        return ex.Entries.Any(e => e.Entity is VacancyLike)
               && ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    [HttpPost("shares")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Share(
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

        if (!AnalyticsConsent.IsGranted(Request))
        {
            return NoContent();
        }

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            if (PublicStatsExclusion.ShouldSkip(user, User))
            {
                return Ok(new { recorded = false });
            }

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
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
        if (vacancy is null)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return VacancyVisibilityRules.IsPubliclyVisible(vacancy, today);
    }
}
