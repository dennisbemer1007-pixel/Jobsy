using System.Security.Claims;
using Jobsy.Api.Admin;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/beheer-todos")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminBeheerTodosController : ControllerBase
{
    private readonly IBeheerTodoService _todos;
    private readonly IAdminAuditContext _auditContext;

    public AdminBeheerTodosController(IBeheerTodoService todos, IAdminAuditContext auditContext)
    {
        _todos = todos;
        _auditContext = auditContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BeheerTodoDto>>> List(
        [FromQuery] BeheerTodoStatus? status,
        CancellationToken cancellationToken)
        => Ok(await _todos.ListAsync(status, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BeheerTodoDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _todos.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [AdminAudit(AdminAuditKeys.BeheerTodoCreate, TargetType = AdminAuditKeys.TargetTypes.BeheerTodo)]
    public async Task<ActionResult<BeheerTodoDto>> Create(
        [FromBody] BeheerTodoUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
        {
            return Unauthorized();
        }

        try
        {
            var created = await _todos.CreateAsync(ToUpsert(request), actorId, cancellationToken);
            _auditContext.TargetId = created.Id.ToString("D");
            _auditContext.TargetLabel = created.Title;
            _auditContext.SetDetailsObject(new { created.Title, created.Category, status = created.Status.ToString() });
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [AdminAudit(
        AdminAuditKeys.BeheerTodoUpdate,
        TargetType = AdminAuditKeys.TargetTypes.BeheerTodo,
        TargetRouteKey = "id")]
    public async Task<ActionResult<BeheerTodoDto>> Update(
        Guid id,
        [FromBody] BeheerTodoUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
        {
            return Unauthorized();
        }

        try
        {
            var updated = await _todos.UpdateAsync(id, ToUpsert(request), actorId, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            _auditContext.SetDetailsObject(new { updated.Title, status = updated.Status.ToString() });
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mark-done")]
    [AdminAudit(
        AdminAuditKeys.BeheerTodoMarkDone,
        TargetType = AdminAuditKeys.TargetTypes.BeheerTodo,
        TargetRouteKey = "id")]
    public async Task<ActionResult<BeheerTodoDto>> MarkDone(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
        {
            return Unauthorized();
        }

        var updated = await _todos.MarkDoneAsync(id, actorId, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [AdminAudit(
        AdminAuditKeys.BeheerTodoDelete,
        TargetType = AdminAuditKeys.TargetTypes.BeheerTodo,
        TargetRouteKey = "id")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _todos.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        _auditContext.SetDetailsObject(new { existing.Title });
        await _todos.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private bool TryGetActorId(out Guid actorId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(raw, out actorId))
        {
            return true;
        }

        actorId = default;
        return false;
    }

    private static BeheerTodoUpsert ToUpsert(BeheerTodoUpsertRequest request)
        => new(
            request.Title ?? string.Empty,
            request.Body ?? string.Empty,
            request.DueDate,
            request.Priority,
            request.Status,
            request.Category ?? string.Empty);
}

public sealed class BeheerTodoUpsertRequest
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateOnly? DueDate { get; set; }
    public BeheerTodoPriority Priority { get; set; } = BeheerTodoPriority.Normaal;
    public BeheerTodoStatus Status { get; set; } = BeheerTodoStatus.Open;
    public string? Category { get; set; }
}
