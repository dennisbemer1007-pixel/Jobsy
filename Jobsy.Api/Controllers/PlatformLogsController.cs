using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
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
        CancellationToken cancellationToken = default)
    {
        var query = _db.PlatformLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(l => l.Category.Contains(category));
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

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(500)
            .Select(l => new PlatformLogItemDto(
                l.Id,
                l.Level.ToString(),
                l.Category,
                l.Message,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
