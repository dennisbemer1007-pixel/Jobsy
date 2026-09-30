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

    public AdminSalesController(ISalesParkedBalanceService parked) => _parked = parked;

    [HttpGet("parked-balances")]
    public async Task<ActionResult<IReadOnlyList<SalesParkedBalanceItem>>> ListParkedBalances(
        CancellationToken cancellationToken)
        => Ok(await _parked.ListAsync(cancellationToken));
}
