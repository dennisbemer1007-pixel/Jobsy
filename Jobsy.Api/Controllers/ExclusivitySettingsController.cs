using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/exclusivity-settings")]
public class ExclusivitySettingsController : ControllerBase
{
    private readonly IExclusivitySettingService _service;

    public ExclusivitySettingsController(IExclusivitySettingService service)
    {
        _service = service;
    }

    /// <summary>Active exclusivity options for vacancy forms and public badges.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ExclusivitySettingDto>>> ListActive(CancellationToken cancellationToken)
        => Ok(await _service.ListAsync(activeOnly: true, cancellationToken));

    [HttpGet("admin")]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<ActionResult<IEnumerable<ExclusivitySettingDto>>> ListAdmin(CancellationToken cancellationToken)
        => Ok(await _service.ListAsync(activeOnly: false, cancellationToken));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ExclusivitySettingDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<ActionResult<ExclusivitySettingDto>> Create(
        [FromBody] ExclusivitySettingUpsertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateAsync(request, cancellationToken);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<ActionResult<ExclusivitySettingDto>> Update(
        Guid id,
        [FromBody] ExclusivitySettingUpsertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
