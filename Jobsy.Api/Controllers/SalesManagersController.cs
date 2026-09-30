using Jobsy.Api.Models;
using Jobsy.Api.Privacy;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/sales-managers")]
public class SalesManagersController : ControllerBase
{
    private readonly ISalesManagerInviteService _invite;
    private readonly ISalesManagerApplicationService _applications;
    private readonly ISalesManagerOnboardingService _onboarding;
    private readonly ISalesManagerDashboardService _dashboard;
    private readonly ISelfBillingInvoiceService _invoices;
    private readonly IUserLookupService _users;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IHostEnvironment _environment;
    private readonly ISupportAccessService _supportAccess;
    private readonly IPersonalDataAccessLogger _accessLog;

    public SalesManagersController(
        ISalesManagerInviteService invite,
        ISalesManagerApplicationService applications,
        ISalesManagerOnboardingService onboarding,
        ISalesManagerDashboardService dashboard,
        ISelfBillingInvoiceService invoices,
        IUserLookupService users,
        ICompanyAuthorizationService companyAuth,
        IHostEnvironment environment,
        ISupportAccessService supportAccess,
        IPersonalDataAccessLogger accessLog)
    {
        _invite = invite;
        _applications = applications;
        _onboarding = onboarding;
        _dashboard = dashboard;
        _invoices = invoices;
        _users = users;
        _companyAuth = companyAuth;
        _environment = environment;
        _supportAccess = supportAccess;
        _accessLog = accessLog;
    }

    [HttpPost("invite")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SalesManagerInviteResponse>> Invite(
        [FromBody] InviteSalesManagerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _invite.InviteAsync(
                request.Email,
                request.FullName,
                referredBySalesManagerUserId: null,
                cancellationToken);
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
    {
        var rows = await _dashboard.ListSalesManagersAsync(cancellationToken);
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = new List<SalesManagerListItemDto>(rows.Count);
        foreach (var r in rows)
        {
            Guid? grantId = null;
            if (actor is not null)
            {
                grantId = await _supportAccess.FindActiveGrantIdAsync(
                    actor.Id, r.UserId, null, SupportAccessScope.Contact, cancellationToken);
            }

            var reveal = grantId is not null;
            if (reveal && actor is not null)
            {
                await this.LogPersonalDataAccessAsync(
                    _accessLog,
                    actor.Id,
                    PersonalDataAccessLogExtensions.ResolveActorRole(User),
                    "salesmanager.contact.reveal",
                    "reveal",
                    subjectUserId: r.UserId,
                    reason: "support-access",
                    supportAccessGrantId: grantId,
                    cancellationToken: cancellationToken);
            }

            result.Add(r with
            {
                Email = reveal ? r.Email : PersonalDataMasker.MaskEmail(r.Email),
                FullName = reveal ? r.FullName : PersonalDataMasker.MaskName(r.FullName)
            });
        }

        return Ok(result);
    }

    [HttpGet("applications")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<IEnumerable<SalesManagerApplicationDto>>> ListApplications(
        [FromQuery] bool pendingOnly = true,
        CancellationToken cancellationToken = default)
    {
        var list = pendingOnly
            ? await _applications.ListPendingAsync(cancellationToken)
            : await _applications.ListAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost("applications/{applicationId:guid}/approve")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SalesManagerApplicationDto>> ApproveApplication(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var admin = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (admin is null)
        {
            return Unauthorized();
        }

        try
        {
            var dto = await _applications.ApproveAsync(applicationId, admin.Id, cancellationToken);
            if (!_environment.IsDevelopment())
            {
                dto = dto with { TemporaryPassword = null };
            }

            return Ok(dto);
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

    [HttpPost("applications/{applicationId:guid}/reject")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SalesManagerApplicationDto>> RejectApplication(
        Guid applicationId,
        [FromBody] RejectSalesManagerApplicationRequest? request,
        CancellationToken cancellationToken)
    {
        var admin = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (admin is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _applications.RejectAsync(
                applicationId, admin.Id, request?.Reason, cancellationToken));
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

    [HttpPost("{userId:guid}/invoices")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SelfBillingInvoiceDto>> CreateInvoiceFor(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoices.CreateFromUninvoicedBalanceAsync(userId, cancellationToken: cancellationToken);
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

public record SubmitSalesManagerApplicationRequest(
    string CandidateEmail,
    string CandidateFullName,
    string Motivation,
    bool ReferrerConfirmedPermission = false);

public record RejectSalesManagerApplicationRequest(string? Reason = null);

public record SignSalesManagerAgreementRequest(string? AgreementVersion = null);
