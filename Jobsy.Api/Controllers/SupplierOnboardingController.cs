using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/onboarding")]
[Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
public class SupplierOnboardingController : ControllerBase
{
    private readonly ISupplierOnboardingPaymentService _onboarding;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;

    public SupplierOnboardingController(
        ISupplierOnboardingPaymentService onboarding,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users)
    {
        _onboarding = onboarding;
        _companyAuth = companyAuth;
        _users = users;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<SupplierOnboardingCheckoutResult>> CreateCheckout(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        try
        {
            var result = await _onboarding.CreateCheckoutAsync(companyId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("complete")]
    public async Task<ActionResult<SupplierOnboardingCompleteResult>> Complete(
        Guid companyId,
        [FromBody] CompleteOnboardingCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);

        try
        {
            var result = await _onboarding.CompleteCheckoutAsync(
                request.PaymentId,
                actor?.Id,
                expectedCompanyId: companyId,
                cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record CompleteOnboardingCheckoutRequest(string PaymentId);
