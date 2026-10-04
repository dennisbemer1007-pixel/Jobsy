using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/platform-logs")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class PlatformLogsController : ControllerBase
{
    private readonly JobsyDbContext _db;

    public PlatformLogsController(JobsyDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlatformLogItemDto>>> GetLogs(
        [FromQuery] string? category = null,
        [FromQuery] string? level = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PlatformLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(l => l.Category.ToLower().Contains(cat));
        }

        if (Enum.TryParse<PlatformLogLevel>(level, true, out var parsedLevel))
        {
            query = query.Where(l => l.Level == parsedLevel);
        }

        if (from is not null)
        {
            query = query.Where(l => l.CreatedAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(l => l.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(l =>
                l.Message.ToLower().Contains(term)
                || l.Category.ToLower().Contains(term));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(cancellationToken);
        Response.Headers["X-Total-Count"] = total.ToString();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new PlatformLogItemDto(
                l.Id,
                l.Level.ToString(),
                l.Category,
                l.Message,
                l.CreatedAt,
                l.DetailsJson))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    /// <summary>Web circuit logger. Authorized by the internal secret, not an admin cookie.</summary>
    [HttpPost("errors")]
    [AllowAnonymous]
    public async Task<IActionResult> ReportError(
        [FromBody] PlatformErrorReport? body,
        [FromServices] IConfiguration configuration,
        [FromServices] IPlatformErrorLog log,
        CancellationToken cancellationToken)
    {
        var expected = configuration[InternalClientIpHeaders.ConfigKey];
        if (string.IsNullOrWhiteSpace(expected)
            || !Request.Headers.TryGetValue(InternalClientIpHeaders.InternalSecretHeader, out var got)
            || !string.Equals(got.ToString().Trim(), expected.Trim(), StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Message))
        {
            return BadRequest();
        }

        await log.WriteAsync(
            body.Category ?? "Interactive",
            body.Message,
            body.SupportCode,
            body.Detail,
            cancellationToken);
        return NoContent();
    }
}

public sealed record PlatformErrorReport(string? Category, string? Message, string? SupportCode, string? Detail);
