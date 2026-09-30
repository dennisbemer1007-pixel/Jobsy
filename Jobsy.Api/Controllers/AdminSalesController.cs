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

    public AdminSalesController(
        ISalesParkedBalanceService parked,
        ISalesCorrectionService corrections)
    {
        _parked = parked;
        _corrections = corrections;
    }

    [HttpGet("parked-balances")]
    public async Task<ActionResult<IReadOnlyList<SalesParkedBalanceItem>>> ListParkedBalances(
        CancellationToken cancellationToken)
        => Ok(await _parked.ListAsync(cancellationToken));

    /// <summary>Book a manual ledger correction (±). Requires MFA-verified session.</summary>
    [HttpPost("ledger/corrections")]
    public async Task<ActionResult<object>> BookCorrection(
        [FromBody] SalesLedgerCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!PersonalDataAccessLogExtensions.IsMfaVerifiedInSession(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "MFA-sessie vereist." });
        }

        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(adminIdClaim, out var adminId))
        {
            return Unauthorized();
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
}

public sealed record SalesLedgerCorrectionRequest(
    Guid BeneficiaryUserId,
    Guid? CompanyId,
    decimal AmountExVat,
    string Reason);
