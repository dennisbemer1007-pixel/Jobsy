using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services.LenderRegistration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/lender-registrations")]
[Authorize(Roles = "Admin")]
public sealed class AdminLenderRegistrationController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ILenderRegistrationCheck _lender;
    private readonly IUserLookupService _users;

    public AdminLenderRegistrationController(
        JobsyDbContext db,
        ILenderRegistrationCheck lender,
        IUserLookupService users)
    {
        _db = db;
        _lender = lender;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminLenderRegistrationItem>>> List(
        CancellationToken cancellationToken)
    {
        var bureauIds = await _db.Companies
            .AsNoTracking()
            .Where(c => c.Type == CompanyType.Intermediary)
            .Select(c => new { c.Id, c.Name, c.KvkNumber })
            .ToListAsync(cancellationToken);

        var items = new List<AdminLenderRegistrationItem>();
        foreach (var bureau in bureauIds)
        {
            var state = await _lender.GetStateAsync(bureau.Id, cancellationToken);
            if (state.Status is not (LenderRegistrationStatuses.Pending or LenderRegistrationStatuses.NotChecked))
            {
                continue;
            }

            items.Add(new AdminLenderRegistrationItem(
                bureau.Id,
                bureau.Name,
                bureau.KvkNumber,
                state.Status,
                state.Source,
                state.Reference,
                state.CheckedAtUtc,
                state.ValidUntil,
                state.Note,
                state.WaadiCheckUrl
                    ?? WaadiKvkProvider.BuildDeepLink(bureau.KvkNumber)));
        }

        return Ok(items.OrderBy(i => i.CompanyName).ToList());
    }

    [HttpPost("{bureauId:guid}/decision")]
    public async Task<IActionResult> Decide(
        Guid bureauId,
        [FromBody] AdminLenderDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var admin = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (admin is null)
        {
            return Unauthorized();
        }

        try
        {
            await _lender.RecordDecisionAsync(
                bureauId,
                new LenderRegistrationDecision(
                    request.Approve,
                    string.IsNullOrWhiteSpace(request.Source)
                        ? LenderRegistrationSources.AdminManual
                        : request.Source.Trim(),
                    request.Reference,
                    request.ValidUntil,
                    request.Note),
                admin.Id,
                cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record AdminLenderRegistrationItem(
    Guid CompanyId,
    string CompanyName,
    string KvkNumber,
    string Status,
    string? Source,
    string? Reference,
    DateTime? CheckedAtUtc,
    DateTime? ValidUntil,
    string? Note,
    string? WaadiCheckUrl);

public sealed class AdminLenderDecisionRequest
{
    public bool Approve { get; set; }
    public string? Source { get; set; }
    public string? Reference { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Note { get; set; }
}
