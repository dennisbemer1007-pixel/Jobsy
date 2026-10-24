using Jobsy.Core.Authorization;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Ops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Api.Controllers;

public partial class MeController
{
    public sealed record AcceptatieSollicitatiesDemoEnsureResponse(
        bool Applied,
        int VerifiedApplicationCount,
        string? SkipReason);

    /// <summary>
    /// Acceptatie only: idempotent mockup sollicitaties rows for the allowlisted CLI test candidate.
    /// Used when startup seed on lobsy-acc-api did not run or the account was created after deploy.
    /// </summary>
    [HttpPost("acceptatie-sollicitaties-demo-ensure")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<AcceptatieSollicitatiesDemoEnsureResponse>> EnsureAcceptatieSollicitatiesDemo(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!TestAccountsSollicitatiesDemoSeeder.IsAcceptatieDemoCandidateEmail(user.Email))
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

        var applied = await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
            _db,
            configuration,
            guard,
            _logger,
            cancellationToken);

        var count = await _db.Applications.CountAsync(
            a => a.CandidateUserId == user.Id && a.EmailVerifiedAt != null,
            cancellationToken);

        return Ok(new AcceptatieSollicitatiesDemoEnsureResponse(
            applied,
            count,
            applied ? null : "skipped_no_manager_or_candidate"));
    }
}
