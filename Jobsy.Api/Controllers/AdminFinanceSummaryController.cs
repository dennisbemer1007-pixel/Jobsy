using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/finance")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminFinanceSummaryController : ControllerBase
{
    private readonly IAdminFinanceSummaryService _finance;

    public AdminFinanceSummaryController(IAdminFinanceSummaryService finance) => _finance = finance;

    [HttpGet("summary")]
    public async Task<ActionResult<AdminFinanceSummaryDto>> GetSummary(
        [FromQuery] string period = "week",
        CancellationToken cancellationToken = default)
    {
        var summary = await _finance.GetAsync(period, cancellationToken);
        return Ok(new AdminFinanceSummaryDto(
            summary.Period,
            summary.RevenueInclVatCents,
            summary.RevenueExVatCents,
            summary.PreviousRevenueInclVatCents,
            summary.TokensSold,
            summary.OpenAtMollieCents,
            summary.VatBufferPendingCents,
            summary.OpenPayoutsCents,
            summary.OpenPayoutsCount));
    }
}

public sealed record AdminFinanceSummaryDto(
    string Period,
    int RevenueInclVatCents,
    int RevenueExVatCents,
    int PreviousRevenueInclVatCents,
    int TokensSold,
    int OpenAtMollieCents,
    int VatBufferPendingCents,
    int OpenPayoutsCents,
    int OpenPayoutsCount);
