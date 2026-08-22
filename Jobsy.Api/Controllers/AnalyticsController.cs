using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private const int MaxImpressionsPerRequest = 200;

    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;

    public AnalyticsController(JobsyDbContext db, IUserLookupService users)
    {
        _db = db;
        _users = users;
    }

    [HttpPost("impressions")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> RecordImpressions(
        [FromBody] RecordImpressionsRequest? request,
        CancellationToken cancellationToken)
    {
        if (!AnalyticsConsent.IsGranted(Request))
        {
            return Ok(new { recorded = 0 });
        }

        var vacancyIds = (request?.VacancyIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(MaxImpressionsPerRequest)
            .ToList();

        if (vacancyIds.Count == 0)
        {
            return Ok(new { recorded = 0 });
        }

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            userId = user?.Id;
        }

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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var visibleIds = await _db.Vacancies.AsNoTracking()
            .Where(v => vacancyIds.Contains(v.Id)
                && v.Status == VacancyStatus.Active
                && v.StartDate <= today
                && v.EndDate >= today)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var rows = visibleIds
            .Select(id => new VacancySearchImpression
            {
                Id = Guid.NewGuid(),
                VacancyId = id,
                UserId = userId,
                AnonymousKey = anonymousKey,
                CreatedAt = now
            })
            .ToList();

        if (rows.Count > 0)
        {
            _db.VacancySearchImpressions.AddRange(rows);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { recorded = rows.Count, anonymousKey });
    }

    [HttpPost("site-visits")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> RecordSiteVisit(
        [FromBody] RecordSiteVisitRequest? request,
        CancellationToken cancellationToken)
    {
        if (!AnalyticsConsent.IsGranted(Request))
        {
            return Ok(new { recorded = false });
        }

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            userId = user?.Id;
        }

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

        var path = string.IsNullOrWhiteSpace(request?.Path)
            ? null
            : request!.Path!.Trim();
        if (path is { Length: > 512 })
        {
            path = path[..512];
        }

        _db.SiteVisits.Add(new SiteVisit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AnonymousKey = anonymousKey,
            Path = path,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { recorded = true, anonymousKey });
    }
}
