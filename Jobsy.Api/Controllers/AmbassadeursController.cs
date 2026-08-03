using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/ambassadeurs")]
public class AmbassadeursController : ControllerBase
{
    private readonly IAmbassadeurInviteService _invite;
    private readonly IAmbassadeurOnboardingService _onboarding;
    private readonly IAmbassadeurDashboardService _dashboard;
    private readonly IAmbassadeurSettingsService _settings;
    private readonly IAmbassadeurFlyerPdfService _flyers;
    private readonly ISelfBillingInvoiceService _invoices;
    private readonly ISalesManagerPayoutService _payouts;
    private readonly IUserLookupService _users;
    private readonly IHostEnvironment _environment;

    public AmbassadeursController(
        IAmbassadeurInviteService invite,
        IAmbassadeurOnboardingService onboarding,
        IAmbassadeurDashboardService dashboard,
        IAmbassadeurSettingsService settings,
        IAmbassadeurFlyerPdfService flyers,
        ISelfBillingInvoiceService invoices,
        ISalesManagerPayoutService payouts,
        IUserLookupService users,
        IHostEnvironment environment)
    {
        _invite = invite;
        _onboarding = onboarding;
        _dashboard = dashboard;
        _settings = settings;
        _flyers = flyers;
        _invoices = invoices;
        _payouts = payouts;
        _users = users;
        _environment = environment;
    }

    [HttpPost("invite")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<AmbassadeurInviteResult>> Invite(
        [FromBody] InviteAmbassadeurRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _invite.InviteAsync(request.Email, request.FullName, cancellationToken);
            return Ok(result with
            {
                TemporaryPassword = _environment.IsDevelopment() ? result.TemporaryPassword : string.Empty
            });
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
    public async Task<ActionResult<IEnumerable<AmbassadeurListItemDto>>> List(CancellationToken cancellationToken)
        => Ok(await _dashboard.ListAmbassadeursAsync(cancellationToken));

    [HttpGet("settings")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<AmbassadeurSettingsDto>> GetSettings(CancellationToken cancellationToken)
        => Ok(await _settings.GetAsync(cancellationToken));

    [HttpPut("settings")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<AmbassadeurSettingsDto>> UpdateSettings(
        [FromBody] AmbassadeurSettingsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _settings.UpdateAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{userId:guid}/commission-override")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<IActionResult> SetCommissionOverride(
        Guid userId,
        [FromBody] AmbassadeurCommissionOverrideRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _settings.SetCommissionOverrideAsync(userId, request.PercentageOverride, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/dashboard")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<ActionResult<AmbassadeurDashboardDto>> GetMyDashboard(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _dashboard.GetDashboardAsync(user.Id, cancellationToken);
        return dto is null
            ? NotFound(new { message = "Geen ambassadeur-dashboard gevonden." })
            : Ok(dto);
    }

    [HttpGet("{userId:guid}/dashboard")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrAmbassadeur)]
    public async Task<ActionResult<AmbassadeurDashboardDto>> GetDashboard(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        if (actor.Role != Core.Enums.UserRole.Admin && actor.Id != userId)
        {
            return Forbid();
        }

        var dto = await _dashboard.GetDashboardAsync(userId, cancellationToken);
        return dto is null
            ? NotFound(new { message = "Ambassadeur niet gevonden." })
            : Ok(dto);
    }

    [HttpGet("me/profile")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<ActionResult<AmbassadeurProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var profile = await _onboarding.GetProfileAsync(user.Id, cancellationToken);
        return profile is null
            ? NotFound(new { message = "Ambassadeur-profiel niet gevonden." })
            : Ok(profile);
    }

    [HttpPut("me/profile")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<ActionResult<AmbassadeurProfileDto>> UpdateMyProfile(
        [FromBody] UpdateAmbassadeurProfileRequest request,
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
                new AmbassadeurProfileUpdateRequest(
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
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<ActionResult<AmbassadeurProfileDto>> SignAgreement(
        [FromBody] SignAmbassadeurAgreementRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var version = request?.AgreementVersion ?? AmbassadeurCommissionRules.CurrentAgreementVersion;
            var profile = await _onboarding.SignAgreementAsync(user.Id, version, cancellationToken);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/invoices")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
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

    [HttpGet("me/payouts/preview")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<ActionResult<SalesManagerPayoutPreviewDto>> GetPayoutPreview(
        [FromQuery] decimal? amountExVat,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _payouts.GetPreviewAsync(user.Id, amountExVat, cancellationToken));
    }

    [HttpPost("me/payouts/checkout")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<ActionResult<SalesManagerPayoutCheckoutResult>> CreatePayoutCheckout(
        [FromBody] CreateAmbassadeurPayoutCheckoutRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var amount = request?.AmountExVat
                ?? throw new ArgumentException("Bedrag (excl. BTW) is verplicht.");
            return Ok(await _payouts.CreateCheckoutAsync(user.Id, amount, cancellationToken));
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

    [HttpPost("me/payouts/complete")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<ActionResult<SalesManagerPayoutCompleteResult>> CompletePayoutCheckout(
        [FromBody] CompleteAmbassadeurPayoutCheckoutRequest request,
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/flyers/{kind}")]
    [Authorize(Policy = JobsyPolicies.RequireAmbassadeur)]
    public async Task<IActionResult> DownloadFlyer(string kind, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var profile = await _onboarding.GetProfileAsync(user.Id, cancellationToken);
        if (profile is null || string.IsNullOrWhiteSpace(profile.TrackingCode))
        {
            return BadRequest(new { message = "Rond eerst onboarding af om flyers te downloaden." });
        }

        var flyerKind = kind.Trim().ToLowerInvariant() switch
        {
            "candidate" or "kandidaten" => AmbassadeurFlyerKind.Candidate,
            "entrepreneur" or "ondernemer" or "bedrijf" => AmbassadeurFlyerKind.Entrepreneur,
            _ => throw new ArgumentException("Onbekend flyertype. Gebruik candidate of entrepreneur.")
        };

        try
        {
            var pdf = await _flyers.RenderAsync(profile.TrackingCode, flyerKind, cancellationToken);
            var fileName = flyerKind == AmbassadeurFlyerKind.Candidate
                ? $"lobsy-kandidatenflyer-{profile.TrackingCode}.pdf"
                : $"lobsy-ondernemersflyer-{profile.TrackingCode}.pdf";
            return File(pdf, "application/pdf", fileName);
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

    private static SelfBillingInvoiceDto MapInvoice(Core.Entities.SelfBillingInvoice i) =>
        new(
            i.Id,
            i.InvoiceNumber,
            i.SubtotalExVat,
            i.VatAmount,
            i.TotalInclVat,
            i.Status.ToString(),
            i.CreatedAt,
            i.IssuedAt,
            i.PaidAt);
}

public sealed record InviteAmbassadeurRequest(string Email, string FullName);

public sealed record UpdateAmbassadeurProfileRequest(
    string CompanyName,
    string KvkNumber,
    string VatNumber,
    string Address,
    string PostalCode,
    string City,
    string? Country = "NL",
    string? Iban = null);

public sealed record SignAmbassadeurAgreementRequest(string? AgreementVersion = null);

public sealed record AmbassadeurCommissionOverrideRequest(decimal? PercentageOverride);

public sealed record CreateAmbassadeurPayoutCheckoutRequest(decimal AmountExVat);

public sealed record CompleteAmbassadeurPayoutCheckoutRequest(string PaymentId);
