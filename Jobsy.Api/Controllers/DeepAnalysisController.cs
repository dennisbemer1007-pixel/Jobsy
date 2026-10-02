using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/deep-analysis")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class DeepAnalysisController : ControllerBase
{
    private readonly IDeepAnalysisService _deep;
    private readonly IDeepTestPaymentService _payments;
    private readonly IConsumerInvoiceService _invoices;
    private readonly IAssessmentReportPdfService _reports;
    private readonly IUserLookupService _users;
    private readonly JobsyDbContext _db;

    public DeepAnalysisController(
        IDeepAnalysisService deep,
        IDeepTestPaymentService payments,
        IConsumerInvoiceService invoices,
        IAssessmentReportPdfService reports,
        IUserLookupService users,
        JobsyDbContext db)
    {
        _deep = deep;
        _payments = payments;
        _invoices = invoices;
        _reports = reports;
        _users = users;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<DeepAnalysisStateDto>> Get(
        [FromQuery] string? kind,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _deep.GetStateAsync(user.Id, ParseKind(kind), cancellationToken));
    }

    [HttpGet("payment-mode")]
    public async Task<ActionResult<object>> PaymentMode(CancellationToken cancellationToken)
    {
        var mode = await _payments.GetPaymentModeAsync(cancellationToken);
        return Ok(new { mode });
    }

    [HttpPost("checkout")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<object>> Checkout(
        [FromQuery] string? kind,
        [FromBody] DeepTestCheckoutRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!CanUseTests(user, out var message, out var consentCode))
        {
            return BadRequest(new { code = consentCode, message });
        }

        try
        {
            var locale = request?.Locale
                         ?? Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',').FirstOrDefault();
            var result = await _payments.CreateCheckoutAsync(
                user.Id,
                ParseKind(kind),
                request?.WaiverAccepted == true,
                locale,
                cancellationToken);

            return Ok(new DeepAnalysisCheckoutResult(
                result.CheckoutId,
                result.PaymentId,
                result.CheckoutUrl,
                TokenVatPricing.FromCents(result.TotalCents),
                result.IsStub,
                result.Kind));
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message switch
            {
                "waiver_required" => "waiver_required",
                "already_unlocked" => "already_unlocked",
                "payments_unavailable" => "payments_unavailable",
                "consent_required" => "consent_required",
                _ => "error"
            };
            if (code == "payments_unavailable")
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { code, message = code });
            }

            return BadRequest(new { code, message = code });
        }
    }

    [HttpGet("checkout/{checkoutId:guid}")]
    public async Task<ActionResult<DeepTestCheckoutStatusDto>> GetCheckout(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            return Ok(await _payments.GetStatusAsync(user.Id, checkoutId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("checkout/{checkoutId:guid}/stub-pay")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<DeepTestCheckoutStatusDto>> StubPay(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!string.Equals(await _payments.GetPaymentModeAsync(cancellationToken), "stub", StringComparison.Ordinal))
        {
            return NotFound();
        }

        var checkout = await _db.DeepAnalysisCheckouts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == checkoutId && c.UserId == user.Id, cancellationToken);
        if (checkout is null)
        {
            return NotFound();
        }

        await _payments.TryFulfillAsync(checkoutId, DeepTestFulfillSource.Stub, cancellationToken);
        try
        {
            return Ok(await _payments.GetStatusAsync(user.Id, checkoutId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Legacy shim: never marks paid. Looks up by PaymentId and returns status.</summary>
    [Obsolete("Use GET checkout/{checkoutId}. Removed in tests stack file 07.")]
    [HttpPost("checkout/{paymentId}/complete")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> CompleteCheckout(string paymentId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var checkout = await _db.DeepAnalysisCheckouts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PaymentId == paymentId && c.UserId == user.Id, cancellationToken);
        if (checkout is null)
        {
            return NotFound(new { code = "not_found", message = "Checkout niet gevonden." });
        }

        try
        {
            return Ok(await _payments.GetStatusAsync(user.Id, checkout.Id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("invoices/{invoiceId:guid}/pdf")]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> InvoicePdf(Guid invoiceId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var invoice = await _invoices.GetAsync(invoiceId, cancellationToken);
        if (invoice is null || invoice.UserId != user.Id)
        {
            return NotFound();
        }

        var pdf = await _invoices.RenderPdfAsync(invoiceId, cancellationToken: cancellationToken);
        return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<DeepAnalysisStateDto>> Save(
        [FromQuery] string? kind,
        [FromBody] SaveDeepAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!CanUseTests(user, out var message, out var consentCode))
        {
            return BadRequest(new { code = consentCode, message });
        }

        var answers = new Dictionary<int, int>();
        if (request.Answers is not null)
        {
            foreach (var (key, value) in request.Answers)
            {
                if (!int.TryParse(key, out var id))
                {
                    return BadRequest(new { code = "unknown_question", message = "Onbekend vraagnummer in de diepte-analyse." });
                }

                answers[id] = value;
            }
        }

        try
        {
            return Ok(await _deep.SaveAsync(user.Id, ParseKind(kind), answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message.Contains("ontgrendeld", StringComparison.OrdinalIgnoreCase)
                ? "not_found"
                : "invalid_answer";
            return BadRequest(new { code, message = ex.Message });
        }
        catch (AssessmentAdjustmentLimitException ex)
        {
            return Conflict(new { code = ex.Code, remaining = ex.Remaining, max = ex.Max });
        }
    }

    [HttpGet("report")]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> Report(
        [FromQuery] string? kind,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var pdf = await _reports.TryRenderAsync(user.Id, ParseKind(kind), lang, cancellationToken);
        if (pdf is null)
        {
            return NotFound(new { code = "not_found", message = "Rapport is nog niet beschikbaar. Rond de diepte-analyse eerst af." });
        }

        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    private static AssessmentKind ParseKind(string? kind)
        => AssessmentKindLabels.ParseOrDefault(kind);

    private static bool CanUseTests(Core.Entities.User user, out string message, out string code)
    {
        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            message = CandidateConsentRules.ParentalConsentRequiredMessage;
            code = "parental_consent_required";
            return false;
        }

        if (!CandidateConsentRules.HasCurrentTestAiConsent(user))
        {
            message = CandidateConsentRules.TestConsentRequiredMessage;
            code = "test_consent_required";
            return false;
        }

        message = string.Empty;
        code = string.Empty;
        return true;
    }
}

public sealed record SaveDeepAnalysisRequest(
    Dictionary<string, int>? Answers,
    bool Complete);

public sealed record DeepTestCheckoutRequest(
    bool WaiverAccepted,
    string? Locale = null);
