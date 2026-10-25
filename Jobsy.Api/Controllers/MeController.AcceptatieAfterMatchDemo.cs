using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Api.Controllers;

public partial class MeController
{
    public sealed record AcceptatieAfterMatchDemoEnsureResponse(bool Applied, string? SkipReason);

    /// <summary>
    /// Acceptatie only: refresh werkgever after-match demo apps (77777771-0001 … 0006) with fixed Oct slots.
    /// </summary>
    [HttpPost("acceptatie-after-match-demo-ensure")]
    [Authorize(Roles = "BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin")]
    public async Task<ActionResult<AcceptatieAfterMatchDemoEnsureResponse>> EnsureAcceptatieAfterMatchDemo(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!IsAcceptatieAfterMatchDemoEmployerEmail(user.Email))
        {
            return NotFound();
        }

        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var input = TestAccountGuardInputFactory.FromConfiguration(configuration, _environment, isWebRuntime: false);
        var guard = TestAccountEnvironmentGuard.Evaluate(input);
        if (!guard.Allowed)
        {
            return NotFound();
        }

        try
        {
            await JobsyDbSeeder.EnsureAfterMatchDemoApplicationsAsync(_db, _logger);
            return Ok(new AcceptatieAfterMatchDemoEnsureResponse(true, null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Acceptatie after-match demo ensure via API failed.");
            return Ok(new AcceptatieAfterMatchDemoEnsureResponse(false, ex.GetType().Name));
        }
    }

    private static bool IsAcceptatieAfterMatchDemoEmployerEmail(string? email)
        => string.Equals(email?.Trim(), "ondernemer@jobsy.local", StringComparison.OrdinalIgnoreCase);
}
