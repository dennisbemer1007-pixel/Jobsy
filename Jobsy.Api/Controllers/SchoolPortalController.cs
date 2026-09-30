using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Api.Security;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/school")]
[Authorize(Policy = JobsyPolicies.RequireSchoolAdmin)]
[SchoolsFeatureGate]
public sealed class SchoolPortalController : ControllerBase
{
    private readonly ISchoolPortalService _portal;

    public SchoolPortalController(ISchoolPortalService portal) => _portal = portal;

    [HttpGet("dashboard")]
    public async Task<ActionResult<SchoolDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        var dto = await _portal.GetDashboardAsync(User, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("todos")]
    public async Task<ActionResult<IReadOnlyList<SchoolTodoItemDto>>> Todos(CancellationToken cancellationToken)
    {
        var items = await _portal.GetTodosAsync(User, cancellationToken);
        return Ok(items);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<SchoolProfileDto>> Profile(CancellationToken cancellationToken)
    {
        var profile = await _portal.GetProfileAsync(User, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("privacy")]
    public async Task<ActionResult<SchoolPrivacyDto>> Privacy(CancellationToken cancellationToken)
    {
        var privacy = await _portal.GetPrivacyAsync(User, cancellationToken);
        return privacy is null ? NotFound() : Ok(privacy);
    }

    [HttpPost("privacy/delete-year")]
    public async Task<ActionResult<SchoolEarlyDeleteResult>> DeleteYear(
        [FromBody] DeleteSchoolYearRequest request,
        CancellationToken cancellationToken)
    {
        var (result, error) = await _portal.DeleteCurrentSchoolYearDataAsync(
            User,
            request.ConfirmPhrase,
            cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(result);
    }
}
