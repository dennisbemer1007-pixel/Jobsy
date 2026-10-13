using Jobsy.Core.Authorization;
using Jobsy.Infrastructure.Reminders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>Anonymous counts only. No names, e-mail addresses or phone numbers.</summary>
[ApiController]
[Route("api/admin/comeback-reminders")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class ComebackReminderAdminController : ControllerBase
{
    private readonly ComebackReminderService _reminders;

    public ComebackReminderAdminController(ComebackReminderService reminders)
    {
        _reminders = reminders;
    }

    public sealed record ComebackReminderStatsDto(int Sent, int ReturnedWithin7Days, int ReturnedWithin30Days);

    [HttpGet("stats")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<ComebackReminderStatsDto>> Stats(CancellationToken cancellationToken)
    {
        var stats = await _reminders.GetAnonymousStatsAsync(cancellationToken);
        return Ok(new ComebackReminderStatsDto(stats.Sent, stats.ReturnedWithin7Days, stats.ReturnedWithin30Days));
    }
}
