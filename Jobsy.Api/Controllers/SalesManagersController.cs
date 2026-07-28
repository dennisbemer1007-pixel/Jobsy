using System.Text;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/sales-managers")]
public class SalesManagersController : ControllerBase
{
    private readonly ISalesManagerInviteService _invite;
    private readonly ISalesManagerOnboardingService _onboarding;
    private readonly ISalesManagerDashboardService _dashboard;
    private readonly ISelfBillingInvoiceService _invoices;
    private readonly ISalesManagerPayoutService _payouts;
    private readonly IUserLookupService _users;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IHostEnvironment _environment;

    public SalesManagersController(
        ISalesManagerInviteService invite,
        ISalesManagerOnboardingService onboarding,
        ISalesManagerDashboardService dashboard,
        ISelfBillingInvoiceService invoices,
        ISalesManagerPayoutService payouts,
        IUserLookupService users,
        ICompanyAuthorizationService companyAuth,
        IHostEnvironment environment)
    {
        _invite = invite;
        _onboarding = onboarding;
        _dashboard = dashboard;
        _invoices = invoices;
        _payouts = payouts;
        _users = users;
        _companyAuth = companyAuth;
        _environment = environment;
    }

    [HttpPost("invite")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SalesManagerInviteResponse>> Invite(
        [FromBody] InviteSalesManagerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _invite.InviteAsync(request.Email, request.FullName, cancellationToken);
            // Temp password only returned in Development (also emailed via stub). Avoid leaking in prod HTTP logs.
            return Ok(new SalesManagerInviteResponse(
                result.UserId,
                result.Email,
                result.FullName,
                _environment.IsDevelopment() ? result.TemporaryPassword : null,
                result.CreatedNewUser));
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

    [HttpGet]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<IEnumerable<SalesManagerListItemDto>>> List(CancellationToken cancellationToken)
        => Ok(await _dashboard.ListSalesManagersAsync(cancellationToken));

    [HttpGet("me/dashboard")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SalesManagerDashboardDto>> GetMyDashboard(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _dashboard.GetDashboardAsync(user.Id, cancellationToken);
        if (dto is null)
        {
            return NotFound(new
            {
                message =
                    "Geen salesmanager-dashboard gevonden. Controleer of je account in de database de rol SalesManager heeft (seed/migratie)."
            });
        }

        return Ok(dto);
    }

    [HttpGet("me/profile")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SalesManagerProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var profile = await _onboarding.GetProfileAsync(user.Id, cancellationToken);
        return profile is null
            ? NotFound(new { message = "Salesmanager-profiel niet gevonden." })
            : Ok(profile);
    }

    [HttpPut("me/profile")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SalesManagerProfileDto>> UpdateMyProfile(
        [FromBody] UpdateSalesManagerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var profile = await _onboarding.UpdateProfileAsync(
                user.Id,
                new SalesManagerProfileUpdateRequest(
                    request.CompanyName,
                    request.KvkNumber,
                    request.VatNumber,
                    request.Address,
                    request.PostalCode,
                    request.City,
                    request.Country,
                    request.Iban),
                cancellationToken);
            return Ok(profile);
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

    [HttpPost("me/sign-agreement")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SalesManagerProfileDto>> SignAgreement(
        [FromBody] SignSalesManagerAgreementRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var version = request?.AgreementVersion ?? SalesCommissionRules.CurrentAgreementVersion;
            var profile = await _onboarding.SignAgreementAsync(user.Id, version, cancellationToken);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{userId:guid}/dashboard")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrSalesManager)]
    public async Task<ActionResult<SalesManagerDashboardDto>> GetDashboard(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessSalesManagerAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        var dto = await _dashboard.GetDashboardAsync(userId, cancellationToken);
        return dto is null
            ? NotFound(new { message = "Salesmanager niet gevonden." })
            : Ok(dto);
    }

    [HttpGet("me/invoices")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<IEnumerable<SelfBillingInvoiceDto>>> ListMyInvoices(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var invoices = await _invoices.ListForSalesManagerAsync(user.Id, cancellationToken);
        return Ok(invoices.Select(MapInvoice));
    }

    [HttpPost("me/invoices")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SelfBillingInvoiceDto>> CreateInvoice(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var invoice = await _invoices.CreateFromUninvoicedBalanceAsync(user.Id, cancellationToken);
            return Ok(MapInvoice(invoice));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/invoices/{invoiceId:guid}/download")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<IActionResult> DownloadMyInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var html = await _payouts.RenderInvoiceHtmlAsync(invoiceId, user.Id, cancellationToken);
            var invoice = await _invoices.GetAsync(invoiceId, cancellationToken);
            var fileName = $"{invoice?.InvoiceNumber ?? invoiceId.ToString("N")}.html";
            return File(Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("me/payouts/preview")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SalesManagerPayoutPreviewDto>> GetPayoutPreview(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _payouts.GetPreviewAsync(user.Id, cancellationToken));
    }

    [HttpPost("me/payouts/checkout")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SalesManagerPayoutCheckoutResult>> CreatePayoutCheckout(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _payouts.CreateCheckoutAsync(user.Id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/payouts/complete")]
    [Authorize(Policy = JobsyPolicies.RequireSalesManager)]
    public async Task<ActionResult<SalesManagerPayoutCompleteResult>> CompletePayoutCheckout(
        [FromBody] CompleteSalesManagerPayoutRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _payouts.CompleteCheckoutAsync(request.PaymentId, user.Id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    [HttpPost("{userId:guid}/invoices")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SelfBillingInvoiceDto>> CreateInvoiceFor(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoices.CreateFromUninvoicedBalanceAsync(userId, cancellationToken);
            return Ok(MapInvoice(invoice));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("invoices/{invoiceId:guid}/mark-paid")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SelfBillingInvoiceDto>> MarkPaid(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoices.MarkPaidAsync(invoiceId, cancellationToken);
            return Ok(MapInvoice(invoice));
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

    private async Task<bool> CanAccessSalesManagerAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (_companyAuth.IsAdmin(User))
        {
            return true;
        }

        var me = await _users.FindByPrincipalAsync(User, cancellationToken);
        return me is not null && me.Id == userId;
    }

    private static SelfBillingInvoiceDto MapInvoice(Core.Entities.SelfBillingInvoice i) =>
        new(i.Id, i.InvoiceNumber, i.SubtotalExVat, i.VatAmount, i.TotalInclVat,
            i.Status.ToString(), i.CreatedAt, i.IssuedAt, i.PaidAt);
}

public record InviteSalesManagerRequest(string Email, string FullName);

public record SalesManagerInviteResponse(
    Guid UserId,
    string Email,
    string FullName,
    string? TemporaryPassword,
    bool CreatedNewUser);

public record UpdateSalesManagerProfileRequest(
    string CompanyName,
    string KvkNumber,
    string VatNumber,
    string Address,
    string PostalCode,
    string City,
    string? Country = "NL",
    string? Iban = null);

public record SignSalesManagerAgreementRequest(string? AgreementVersion = null);

public record CompleteSalesManagerPayoutRequest(string PaymentId);
