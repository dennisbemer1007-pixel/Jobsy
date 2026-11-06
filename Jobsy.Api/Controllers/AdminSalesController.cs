using Jobsy.Api.Privacy;
using Jobsy.Core.Authorization;
using Jobsy.Core.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/sales")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminSalesController : ControllerBase
{
    private readonly ISalesParkedBalanceService _parked;
    private readonly ISalesCorrectionService _corrections;
    private readonly ISalesAttributionAdminService _attribution;
    private readonly ISalesPayoutRunService _runs;

    public AdminSalesController(
        ISalesParkedBalanceService parked,
        ISalesCorrectionService corrections,
        ISalesAttributionAdminService attribution,
        ISalesPayoutRunService runs)
    {
        _parked = parked;
        _corrections = corrections;
        _attribution = attribution;
        _runs = runs;
    }

    [HttpGet("parked-balances")]
    public async Task<ActionResult<IReadOnlyList<SalesParkedBalanceItem>>> ListParkedBalances(
        CancellationToken cancellationToken)
        => Ok(await _parked.ListAsync(cancellationToken));

    [HttpGet("payout-runs")]
    public async Task<ActionResult<IReadOnlyList<SalesPayoutRunListItemDto>>> ListPayoutRuns(
        CancellationToken cancellationToken)
        => Ok(await _runs.ListRunsAsync(cancellationToken));

    [HttpGet("payout-runs/{id:guid}")]
    public async Task<ActionResult<SalesPayoutRunDetailDto>> GetPayoutRun(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _runs.GetRunAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("payout-runs")]
    public async Task<ActionResult<SalesPayoutRunDto>> CreateExtraPayoutRun(
        CancellationToken cancellationToken)
    {
        if (!RequireMfa(out var forbid))
        {
            return forbid!;
        }

        if (!TryAdminId(out var adminId, out var unauthorized))
        {
            return unauthorized!;
        }

        var run = await _runs.CreateExtraRunAsync(adminId, cancellationToken);
        return Ok(run);
    }

    [HttpPost("payout-runs/{id:guid}/lines/{requestId:guid}/reject")]
    public async Task<IActionResult> RejectPayoutLine(
        Guid id,
        Guid requestId,
        [FromBody] SalesPayoutRejectRequest body,
        CancellationToken cancellationToken)
    {
        if (!RequireMfa(out var forbid))
        {
            return forbid!;
        }

        if (!TryAdminId(out var adminId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            await _runs.RejectLineAsync(adminId, id, requestId, body.Reason, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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

    [HttpPost("payout-runs/{id:guid}/approve")]
    public async Task<ActionResult<SalesPayoutRunDto>> ApprovePayoutRun(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!RequireMfa(out var forbid))
        {
            return forbid!;
        }

        if (!TryAdminId(out var adminId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            return Ok(await _runs.ApproveRunAsync(adminId, id, cancellationToken));
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

    [HttpGet("payout-runs/{id:guid}/export")]
    public async Task<IActionResult> ExportPayoutRun(
        Guid id,
        [FromQuery] string format = "sepa",
        CancellationToken cancellationToken = default)
    {
        if (!RequireMfa(out var forbid))
        {
            return forbid!;
        }

        if (!TryAdminId(out var adminId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var file = await _runs.ExportAsync(adminId, id, format, cancellationToken);
            return File(file.Bytes, file.ContentType, file.FileName);
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

    [HttpPost("payout-runs/{id:guid}/mark-paid")]
    public async Task<ActionResult<SalesPayoutRunDto>> MarkPayoutRunPaid(
        Guid id,
        [FromBody] SalesPayoutMarkPaidRequest? body,
        CancellationToken cancellationToken)
    {
        if (!RequireMfa(out var forbid))
        {
            return forbid!;
        }

        if (!TryAdminId(out var adminId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            return Ok(await _runs.MarkPaidAsync(adminId, id, body?.InvoiceIds, cancellationToken));
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

    /// <summary>Book a manual ledger correction (±). Requires MFA-verified session.</summary>
    [HttpPost("ledger/corrections")]
    public async Task<ActionResult<object>> BookCorrection(
        [FromBody] SalesLedgerCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!RequireMfa(out var forbid))
        {
            return forbid!;
        }

        if (!TryAdminId(out var adminId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var entry = await _corrections.BookManualCorrectionAsync(
                request.BeneficiaryUserId,
                request.CompanyId,
                request.AmountExVat,
                request.Reason,
                adminId,
                cancellationToken);
            return Ok(new
            {
                entry.Id,
                entry.SalesManagerUserId,
                Kind = entry.Kind.ToString(),
                entry.AmountExVat,
                entry.AvailableFromUtc,
                entry.Reason,
                entry.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Reassign sales attribution on an organisation root (future purchases only). MFA required.</summary>
    [HttpPost("attribution/{companyId:guid}")]
    public async Task<ActionResult<object>> ReassignAttribution(
        Guid companyId,
        [FromBody] SalesAttributionReassignRequest request,
        CancellationToken cancellationToken)
    {
        if (!RequireMfa(out var forbid))
        {
            return forbid!;
        }

        if (!TryAdminId(out var adminId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var change = await _attribution.ReassignAsync(
                companyId,
                request.ToBeneficiaryUserId,
                request.Reason,
                adminId,
                cancellationToken);
            return Ok(new
            {
                change.Id,
                change.CompanyId,
                change.FromUserId,
                change.ToUserId,
                Source = change.Source.ToString(),
                change.Reason,
                change.ChangedByUserId,
                change.ChangedAtUtc
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("attribution/{companyId:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<SalesAttributionHistoryItem>>> AttributionHistory(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _attribution.GetHistoryAsync(companyId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool RequireMfa(out ActionResult? forbid)
    {
        if (!PersonalDataAccessLogExtensions.IsMfaVerifiedInSession(User))
        {
            forbid = StatusCode(StatusCodes.Status403Forbidden, new { message = "MFA-sessie vereist." });
            return false;
        }

        forbid = null;
        return true;
    }

    private bool TryAdminId(out Guid adminId, out ActionResult? unauthorized)
    {
        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(adminIdClaim, out adminId))
        {
            unauthorized = Unauthorized();
            return false;
        }

        unauthorized = null;
        return true;
    }
}

public sealed record SalesLedgerCorrectionRequest(
    Guid BeneficiaryUserId,
    Guid? CompanyId,
    decimal AmountExVat,
    string Reason);

public sealed record SalesAttributionReassignRequest(
    Guid? ToBeneficiaryUserId,
    string Reason);

public sealed record SalesPayoutRejectRequest(string Reason);

public sealed record SalesPayoutMarkPaidRequest(IReadOnlyList<Guid>? InvoiceIds);
