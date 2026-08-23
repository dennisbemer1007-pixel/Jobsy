using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = JobsyPolicies.RequireDashboardAccess)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRefreshService _refresh;

    public DashboardController(IDashboardRefreshService refresh)
    {
        _refresh = refresh;
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<DashboardRefreshResultDto>> Refresh(
        [FromQuery] string period = "week",
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _refresh.RefreshAsync(User, period, companyId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
