using Jobsy.Api.Privacy;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>Self-service portal APIs for the signed-in sales beneficiary (SalesManager).</summary>
[ApiController]
[Route("api/sales/me")]
[Authorize(Roles = JobsyRoles.SalesManager)]
public sealed class SalesMeController : ControllerBase
{
    private readonly ISalesBeneficiaryService _beneficiary;
    private readonly ISalesDashboardReadService _dashboard;
    private readonly ISalesEmployerPortalReadService _employers;
    private readonly ISalesLinkToolkitService _linkToolkit;
    private readonly ISalesMaterialsPdfService _materials;
    private readonly ISalesPayoutProfileService _profile;
    private readonly ISalesWalletPortalService _wallet;
    private readonly ISalesPayoutRequestService _payoutRequests;
    private readonly ISalesManagerPayoutService _legacyPayouts;
    private readonly ISalesManagerApplicationService _applications;

    public SalesMeController(
        ISalesBeneficiaryService beneficiary,
        ISalesDashboardReadService dashboard,
        ISalesEmployerPortalReadService employers,
        ISalesLinkToolkitService linkToolkit,
        ISalesMaterialsPdfService materials,
        ISalesPayoutProfileService profile,
        ISalesWalletPortalService wallet,
        ISalesPayoutRequestService payoutRequests,
        ISalesManagerPayoutService legacyPayouts,
        ISalesManagerApplicationService applications)
    {
        _beneficiary = beneficiary;
        _dashboard = dashboard;
        _employers = employers;
        _linkToolkit = linkToolkit;
        _materials = materials;
        _profile = profile;
        _wallet = wallet;
        _payoutRequests = payoutRequests;
        _legacyPayouts = legacyPayouts;
        _applications = applications;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<SalesDashboardDto>> GetDashboard(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _dashboard.GetAsync(me.UserId, period ?? "year", cancellationToken: cancellationToken);
        return Ok(dto);
    }

    [HttpGet("employers")]
    public async Task<ActionResult<SalesEmployerPageDto>> ListEmployers(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        SalesEmployerStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<SalesEmployerStatus>(status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var dto = await _employers.ListAsync(
            me.UserId,
            q,
            statusFilter,
            year,
            page,
            cancellationToken: cancellationToken);
        return Ok(dto);
    }

    [HttpGet("employers/{companyId:guid}")]
    public async Task<ActionResult<SalesEmployerDetailDto>> GetEmployer(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _employers.GetDetailAsync(me.UserId, companyId, cancellationToken: cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpGet("link")]
    public async Task<ActionResult<SalesLinkToolkitDto>> GetLinkToolkit(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _linkToolkit.GetAsync(me.UserId, cancellationToken);
        if (dto is null)
        {
            return NotFound(new { message = "Rond eerst onboarding af om je link te gebruiken." });
        }

        return Ok(dto);
    }

    [HttpGet("materials/qr.png")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadQrPng(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var toolkit = await _linkToolkit.GetAsync(me.UserId, cancellationToken);
        if (toolkit is null)
        {
            return NotFound(new { message = "Rond eerst onboarding af om je QR te downloaden." });
        }

        var bytes = SalesQr.PngForSize(toolkit.QrUrl, 1024);
        return File(bytes, "image/png", $"lobsy-qr-{toolkit.TrackingCode}.png");
    }

    [HttpGet("materials/{kind}.pdf")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadMaterial(string kind, CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var toolkit = await _linkToolkit.GetAsync(me.UserId, cancellationToken);
        if (toolkit is null)
        {
            return NotFound(new { message = "Rond eerst onboarding af om materiaal te downloaden." });
        }

        var code = toolkit.TrackingCode;
        try
        {
            var (bytes, fileName) = kind.Trim().ToLowerInvariant() switch
            {
                "flyer" => (
                    await _materials.FlyerA4Async(code, cancellationToken),
                    $"lobsy-flyer-{code}.pdf"),
                "visitekaartje" or "visitekaartjes" or "cards" => (
                    await _materials.BusinessCardsAsync(code, cancellationToken),
                    $"lobsy-visitekaartje-{code}.pdf"),
                "prijskaart" or "prices" => (
                    await _materials.PriceCardAsync(code, cancellationToken),
                    $"lobsy-prijskaart-{code}.pdf"),
                "presentatie" or "presentation" => (
                    await _materials.PresentationAsync(
                        code,
                        toolkit.DisplayName,
                        toolkit.CompanyName,
                        toolkit.AccountEmail,
                        cancellationToken),
                    $"lobsy-presentatie-{code}.pdf"),
                _ => throw new ArgumentException("Onbekend materiaal. Gebruik flyer, visitekaartje, prijskaart of presentatie.")
            };

            return File(bytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("profile")]
    public async Task<ActionResult<SalesPortalProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _profile.GetAsync(me.UserId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("profile/company")]
    public async Task<ActionResult<SalesPortalProfileDto>> UpdateCompany(
        [FromBody] SalesCompanyUpdateBody body,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        try
        {
            var dto = await _profile.UpdateCompanyAsync(
                me.UserId,
                new SalesCompanyUpdateRequest(
                    body.CompanyName ?? "",
                    body.KvkNumber ?? "",
                    body.VatNumber,
                    body.Address ?? "",
                    body.PostalCode ?? "",
                    body.City ?? "",
                    body.Country),
                cancellationToken);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("profile/vat")]
    public async Task<ActionResult<SalesPortalProfileDto>> UpdateVat(
        [FromBody] SalesVatUpdateBody body,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        if (!Enum.TryParse<SalesManagerVatTreatment>(body.Treatment, ignoreCase: true, out var treatment)
            && !TryParseVatAlias(body.Treatment, out treatment))
        {
            return BadRequest(new { message = "Kies Standard21 of SmallBusinessScheme (KOR)." });
        }

        try
        {
            var dto = await _profile.SetVatTreatmentAsync(
                me.UserId,
                new SalesVatUpdateRequest(treatment, body.VatNumber, body.KorConfirmed),
                cancellationToken);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("profile/email-prefs")]
    public async Task<ActionResult<SalesPortalProfileDto>> UpdateEmailPrefs(
        [FromBody] SalesEmailPrefs body,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _profile.SetEmailPrefsAsync(me.UserId, body ?? SalesEmailPrefs.Default, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("profile/consent")]
    public async Task<ActionResult<SalesPortalProfileDto>> GiveConsent(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _profile.GiveConsentAsync(me.UserId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("profile/consent/revoke")]
    public async Task<ActionResult<SalesPortalProfileDto>> RevokeConsent(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _profile.RevokeConsentAsync(me.UserId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("payout-account/change")]
    public async Task<IActionResult> BeginIbanChange(
        [FromBody] SalesIbanChangeBody body,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        try
        {
            var authMethod = User.FindFirst("auth_method")?.Value;
            var result = await _profile.BeginIbanChangeAsync(
                me.UserId,
                new SalesIbanChangeRequest(body.Iban ?? "", body.HolderName ?? ""),
                authMethod,
                cancellationToken);
            return Ok(new
            {
                method = result.Method,
                applied = result.Applied,
                message = result.Message,
                profile = result.Profile
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Rate limit → 429
            if (ex.Message.Contains("3 keer per 30 dagen", StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payout-account/change/confirm")]
    public async Task<ActionResult<SalesPortalProfileDto>> ConfirmIbanChange(
        [FromBody] SalesIbanConfirmBody body,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        try
        {
            var dto = await _profile.ConfirmIbanChangeAsync(me.UserId, body.Code ?? "", cancellationToken);
            return Ok(dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Public confirm via e-mail link (still requires signed-in SalesManager matching the token owner).</summary>
    [HttpPost("payout-account/change/confirm-email")]
    public async Task<ActionResult<SalesPortalProfileDto>> ConfirmIbanChangeEmail(
        [FromBody] SalesIbanEmailConfirmBody body,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        try
        {
            var dto = await _profile.ConfirmIbanChangeByEmailTokenAsync(
                body.Token ?? "",
                expectedUserId: me.UserId,
                cancellationToken);
            return Ok(dto);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("agreement.pdf")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadAgreement(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var bytes = await _profile.RenderAgreementPdfAsync(me.UserId, cancellationToken);
        return File(bytes, "application/pdf", "lobsy-overeenkomst.pdf");
    }

    [HttpGet("self-billing-consent.pdf")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadConsent(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var bytes = await _profile.RenderConsentPdfAsync(me.UserId, cancellationToken);
        return File(bytes, "application/pdf", "lobsy-self-billing-toestemming.pdf");
    }

    [HttpGet("wallet")]
    public async Task<ActionResult<SalesWalletOverviewDto>> GetWallet(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var overview = await _wallet.GetOverviewAsync(me.UserId, cancellationToken: cancellationToken);
        if (!PersonalDataAccessLogExtensions.IsMfaSatisfiedInSession(User)
            && overview.Blockers.All(b => b.Code != "mfa"))
        {
            var blockers = overview.Blockers.ToList();
            blockers.Add(new SalesPayoutBlockerDto("mfa", "Sales.Payout.Block.Mfa", "/account/beveiliging"));
            overview = new SalesWalletOverviewDto
            {
                Available = overview.Available,
                Pending = overview.Pending,
                Requested = overview.Requested,
                PaidThisYear = overview.PaidThisYear,
                InvoiceCountThisYear = overview.InvoiceCountThisYear,
                LocalYear = overview.LocalYear,
                NextRunDate = overview.NextRunDate,
                PayoutMinimumEuro = overview.PayoutMinimumEuro,
                CommissionHoldDays = overview.CommissionHoldDays,
                VatTreatment = overview.VatTreatment,
                IsKor = overview.IsKor,
                VatOnAvailable = overview.VatOnAvailable,
                MaskedIban = overview.MaskedIban,
                HolderName = overview.HolderName,
                CanRequestPayout = false,
                Blockers = blockers,
                LatestInvoices = overview.LatestInvoices,
                OpenRequestId = overview.OpenRequestId
            };
        }

        return Ok(overview);
    }

    [HttpGet("wallet/entries")]
    public async Task<ActionResult<SalesWalletEntriesPageDto>> ListWalletEntries(
        [FromQuery] int? period,
        [FromQuery] string? kind,
        [FromQuery] string? state,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _wallet.ListEntriesAsync(
            me.UserId, period, kind, state, page, cancellationToken: cancellationToken);
        return Ok(dto);
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<IReadOnlyList<SalesPayoutListItemDto>>> ListPayouts(
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        return Ok(await _wallet.ListPayoutsAsync(me.UserId, cancellationToken));
    }

    [HttpGet("payouts/preview")]
    public async Task<ActionResult<SalesPayoutPreviewDto>> PreviewPayout(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var mfa = PersonalDataAccessLogExtensions.IsMfaSatisfiedInSession(User);
        return Ok(await _payoutRequests.PreviewAsync(me.UserId, mfa, cancellationToken: cancellationToken));
    }

    [HttpPost("payouts")]
    public async Task<ActionResult<SalesPayoutRequestDto>> RequestPayout(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var mfa = PersonalDataAccessLogExtensions.IsMfaSatisfiedInSession(User);
        try
        {
            var dto = await _payoutRequests.RequestAsync(me.UserId, mfa, cancellationToken: cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payouts/{id:guid}/cancel")]
    public async Task<IActionResult> CancelPayout(Guid id, CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        try
        {
            await _payoutRequests.CancelAsync(me.UserId, id, cancellationToken);
            return Ok(new { cancelled = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<IReadOnlyList<SalesInvoiceListItemDto>>> ListInvoices(
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        return Ok(await _wallet.ListInvoicesAsync(me.UserId, cancellationToken));
    }

    [HttpGet("invoices/{id:guid}/pdf")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id, CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        try
        {
            var pdf = await _legacyPayouts.RenderInvoicePdfAsync(id, me.UserId, cancellationToken);
            return File(pdf, "application/pdf", $"factuur-{id:N}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    [HttpGet("jaaroverzicht/{year:int}.pdf")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadJaaroverzicht(int year, CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        if (year is < 2020 or > 2100)
        {
            return BadRequest(new { message = "Ongeldig jaar." });
        }

        try
        {
            var pdf = await _wallet.RenderJaaroverzichtPdfAsync(me.UserId, year, cancellationToken);
            return File(pdf, "application/pdf", $"jaaroverzicht-{year}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("recommend")]
    public async Task<ActionResult<SalesRecommendOverviewDto>> GetRecommend(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        return Ok(await _applications.GetRecommendOverviewAsync(me.UserId, cancellationToken));
    }

    [HttpPost("recommend")]
    public async Task<ActionResult<SalesManagerApplicationDto>> SubmitRecommend(
        [FromBody] SubmitSalesManagerApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        try
        {
            var dto = await _applications.SubmitAsync(
                me.UserId,
                request.CandidateEmail,
                request.CandidateFullName,
                request.Motivation,
                request.ReferrerConfirmedPermission,
                cancellationToken);
            return Ok(dto);
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

    private static bool TryParseVatAlias(string? raw, out SalesManagerVatTreatment treatment)
    {
        treatment = SalesManagerVatTreatment.Standard21;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        switch (raw.Trim().ToLowerInvariant())
        {
            case "standard21":
            case "21":
            case "btw":
                treatment = SalesManagerVatTreatment.Standard21;
                return true;
            case "smallbusinessscheme":
            case "kor":
            case "korregeling":
                treatment = SalesManagerVatTreatment.SmallBusinessScheme;
                return true;
            default:
                return false;
        }
    }
}

public sealed record SalesCompanyUpdateBody(
    string? CompanyName,
    string? KvkNumber,
    string? VatNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country);

public sealed record SalesVatUpdateBody(
    string? Treatment,
    string? VatNumber,
    bool KorConfirmed);

public sealed record SalesIbanChangeBody(string? Iban, string? HolderName);

public sealed record SalesIbanConfirmBody(string? Code);

public sealed record SalesIbanEmailConfirmBody(string? Token);
