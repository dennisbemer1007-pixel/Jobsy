using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
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
[Route("api/settings")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class SettingsController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IIntegrationCredentialService _credentials;
    private readonly IPlatformFeatureService _features;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IAboutPageSettingsService _aboutPage;
    private readonly IMarketingFlyerSettingsService _marketingFlyer;
    private readonly IMarketingFlyerPdfService _marketingFlyerPdf;

    public SettingsController(
        JobsyDbContext db,
        IIntegrationCredentialService credentials,
        IPlatformFeatureService features,
        IPlatformCompanySettingsService companySettings,
        IAboutPageSettingsService aboutPage,
        IMarketingFlyerSettingsService marketingFlyer,
        IMarketingFlyerPdfService marketingFlyerPdf)
    {
        _db = db;
        _credentials = credentials;
        _features = features;
        _companySettings = companySettings;
        _aboutPage = aboutPage;
        _marketingFlyer = marketingFlyer;
        _marketingFlyerPdf = marketingFlyerPdf;
    }

    [HttpGet("token-pricing")]
    public async Task<ActionResult<object>> GetTokenPricing(CancellationToken cancellationToken)
    {
        var packs = await _db.TokenPricings.AsNoTracking()
            .OrderBy(p => p.PackSize)
            .Select(p => new { p.Id, p.PackSize, p.PriceEuro, p.IsActive })
            .ToListAsync(cancellationToken);
        var costs = await _db.TokenSpendCosts.AsNoTracking()
            .OrderBy(c => c.Reason)
            .Select(c => new { c.Id, Reason = c.Reason.ToString(), c.CostTokens, c.IsActive })
            .ToListAsync(cancellationToken);
        var early = await _db.EarlyAdapterRules.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.MonthlyGrantTokens,
                r.PurchaseDiscountPercent,
                r.IsActive
            })
            .ToListAsync(cancellationToken);
        var pushBomSettings = await _db.PushBomSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => new { s.Id, s.RadiusKm, s.MaxTravelMinutes, s.UpdatedAtUtc })
            .FirstOrDefaultAsync(cancellationToken);
        var pushBomTiers = await _db.PushBomPricingTiers.AsNoTracking()
            .OrderBy(t => t.MinCandidates)
            .Select(t => new { t.Id, t.MinCandidates, t.MaxCandidates, t.CostTokens, t.IsActive })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            packs,
            costs,
            earlyAdapterRules = early,
            pushBomSettings,
            pushBomPricingTiers = pushBomTiers
        });
    }

    [HttpPut("token-pricing/packs/{id:guid}")]
    public async Task<IActionResult> UpdatePack(
        Guid id,
        [FromBody] UpdateTokenPackRequest request,
        CancellationToken cancellationToken)
    {
        var pack = await _db.TokenPricings.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pack is null)
        {
            return NotFound();
        }

        if (request.PriceEuro < 0)
        {
            return BadRequest(new { message = "Prijs mag niet negatief zijn." });
        }

        pack.PriceEuro = request.PriceEuro;
        pack.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("token-pricing/costs/{id:guid}")]
    public async Task<IActionResult> UpdateCost(
        Guid id,
        [FromBody] UpdateTokenSpendCostRequest request,
        CancellationToken cancellationToken)
    {
        var cost = await _db.TokenSpendCosts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cost is null)
        {
            return NotFound();
        }

        if (request.CostTokens < 0)
        {
            return BadRequest(new { message = "Kosten mogen niet negatief zijn." });
        }

        cost.CostTokens = request.CostTokens;
        cost.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("token-pricing/pushbom-settings")]
    public async Task<ActionResult<object>> UpdatePushBomSettings(
        [FromBody] UpdatePushBomSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RadiusKm is < 1 or > 100)
        {
            return BadRequest(new { message = "Straal moet tussen 1 en 100 km liggen." });
        }

        if (request.MaxTravelMinutes is < 5 or > 180)
        {
            return BadRequest(new { message = "Reistijd moet tussen 5 en 180 minuten liggen." });
        }

        var settings = await _db.PushBomSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new PushBomSettings { Id = Guid.NewGuid() };
            _db.PushBomSettings.Add(settings);
        }

        settings.RadiusKm = request.RadiusKm;
        settings.MaxTravelMinutes = request.MaxTravelMinutes;
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            settings.Id,
            settings.RadiusKm,
            settings.MaxTravelMinutes,
            settings.UpdatedAtUtc
        });
    }

    [HttpPut("token-pricing/pushbom-tiers")]
    public async Task<ActionResult<object>> UpsertPushBomPricingTier(
        [FromBody] UpsertPushBomPricingTierRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MinCandidates < 0)
        {
            return BadRequest(new { message = "Min. kandidaten mag niet negatief zijn." });
        }

        if (request.MaxCandidates is int max && max < request.MinCandidates)
        {
            return BadRequest(new { message = "Max. kandidaten mag niet lager zijn dan min." });
        }

        if (request.CostTokens < 0)
        {
            return BadRequest(new { message = "Kosten mogen niet negatief zijn." });
        }

        PushBomPricingTier tier;
        if (request.Id is Guid id)
        {
            var existing = await _db.PushBomPricingTiers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            tier = existing;
        }
        else
        {
            tier = new PushBomPricingTier { Id = Guid.NewGuid() };
            _db.PushBomPricingTiers.Add(tier);
        }

        tier.MinCandidates = request.MinCandidates;
        tier.MaxCandidates = request.MaxCandidates;
        tier.CostTokens = request.CostTokens;
        tier.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            tier.Id,
            tier.MinCandidates,
            tier.MaxCandidates,
            tier.CostTokens,
            tier.IsActive
        });
    }

    [HttpDelete("token-pricing/pushbom-tiers/{id:guid}")]
    public async Task<IActionResult> DeletePushBomPricingTier(Guid id, CancellationToken cancellationToken)
    {
        var tier = await _db.PushBomPricingTiers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tier is null)
        {
            return NotFound();
        }

        _db.PushBomPricingTiers.Remove(tier);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("early-adapter-rules")]
    public async Task<ActionResult<object>> UpsertEarlyAdapterRule(
        [FromBody] UpsertEarlyAdapterRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Naam is verplicht." });
        }

        EarlyAdapterRule rule;
        if (request.Id is Guid id)
        {
            var existing = await _db.EarlyAdapterRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            rule = existing;
        }
        else
        {
            rule = new EarlyAdapterRule { Id = Guid.NewGuid() };
            _db.EarlyAdapterRules.Add(rule);
        }

        rule.Name = request.Name.Trim();
        rule.MonthlyGrantTokens = Math.Max(0, request.MonthlyGrantTokens);
        rule.PurchaseDiscountPercent = Math.Clamp(request.PurchaseDiscountPercent, 0, 100);
        rule.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            rule.Id,
            rule.Name,
            rule.MonthlyGrantTokens,
            rule.PurchaseDiscountPercent,
            rule.IsActive
        });
    }

    [HttpGet("platform-features")]
    public async Task<ActionResult<PlatformFeatureDto>> GetPlatformFeatures(CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        return Ok(ToFeatureDto(snap));
    }

    [HttpPut("platform-features")]
    public async Task<ActionResult<PlatformFeatureDto>> UpdatePlatformFeatures(
        [FromBody] UpdatePlatformFeatureRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var snap = await _features.UpdateAsync(
                new PlatformFeatureUpdate(
                    request.VacancyContentModerationEnabled,
                    request.AuthenticatorEnabled,
                    request.ExposeRegistrationActivationLinks,
                    request.PublicWebBaseUrl,
                    request.InactiveCompanyDays,
                    request.SessionInactivityTimeoutMinutes,
                    request.FreePublishUntil,
                    request.ClearFreePublishUntil),
                cancellationToken);
            return Ok(ToFeatureDto(snap));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Public promo status: whether vacancy publish is free (Highlight/PushBom remain paid).
    /// </summary>
    [HttpGet("free-publish")]
    [AllowAnonymous]
    public async Task<ActionResult<FreePublishStatusDto>> GetFreePublishStatus(CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        var active = FreePublishRules.IsActive(snap.FreePublishUntil, DateTime.UtcNow);
        return Ok(new FreePublishStatusDto(active, snap.FreePublishUntil));
    }

    /// <summary>
    /// Public session-security policy for web idle timers and cookie middleware.
    /// Timeout duration is not sensitive; kept anonymous so the UI can bootstrap before auth.
    /// </summary>
    [HttpGet("session-security")]
    [AllowAnonymous]
    public async Task<ActionResult<SessionSecurityDto>> GetSessionSecurity(CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        return Ok(new SessionSecurityDto(snap.SessionInactivityTimeoutMinutes));
    }

    [HttpGet("company")]
    public async Task<ActionResult<PlatformCompanyDto>> GetCompanySettings(CancellationToken cancellationToken)
    {
        var snap = await _companySettings.GetAsync(cancellationToken);
        return Ok(ToCompanyDto(snap));
    }

    [HttpPut("company")]
    public async Task<ActionResult<PlatformCompanyDto>> UpdateCompanySettings(
        [FromBody] UpdatePlatformCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var snap = await _companySettings.GetAsync(cancellationToken);
        var iban = IbanMasking.ResolveStoredIban(request.VatBufferIban, snap.VatBufferIban);
        snap = await _companySettings.UpdateAsync(
            new PlatformCompanyUpdate(
                request.CompanyName ?? "",
                request.Slogan,
                request.Address,
                request.PostalCode,
                request.City,
                request.Country,
                request.KvkNumber,
                request.VatNumber,
                request.Phone,
                request.Email,
                iban),
            cancellationToken);
        return Ok(ToCompanyDto(snap));
    }

    [HttpGet("about")]
    public async Task<ActionResult<AboutPageDto>> GetAboutPage(CancellationToken cancellationToken)
    {
        var snap = await _aboutPage.GetAsync(cancellationToken);
        return Ok(SiteController.ToDto(snap));
    }

    [HttpPut("about")]
    public async Task<ActionResult<AboutPageDto>> UpdateAboutPage(
        [FromBody] UpdateAboutPageRequest request,
        CancellationToken cancellationToken)
    {
        var snap = await _aboutPage.UpdateAsync(
            new AboutPageUpdate(
                request.Title ?? "",
                request.Lead,
                request.BodyHtml ?? ""),
            cancellationToken);
        return Ok(SiteController.ToDto(snap));
    }

    [HttpGet("marketing-flyer")]
    public async Task<ActionResult<MarketingFlyerDto>> GetMarketingFlyer(CancellationToken cancellationToken)
    {
        var snap = await _marketingFlyer.GetAsync(cancellationToken);
        return Ok(ToMarketingFlyerDto(snap));
    }

    [HttpPut("marketing-flyer")]
    public async Task<ActionResult<MarketingFlyerDto>> UpdateMarketingFlyer(
        [FromBody] UpdateMarketingFlyerRequest request,
        CancellationToken cancellationToken)
    {
        var snap = await _marketingFlyer.UpdateAsync(
            new MarketingFlyerUpdate(
                request.Headline ?? "",
                request.Subheadline ?? "",
                request.Intro ?? "",
                request.BulletPoints ?? "",
                request.PromoFreeText ?? "",
                request.PromoDiscountText ?? "",
                request.CtaTitle ?? "",
                request.CtaBody ?? "",
                request.QrCaption ?? "",
                request.QrPath ?? "",
                request.FooterNote ?? ""),
            cancellationToken);
        return Ok(ToMarketingFlyerDto(snap));
    }

    [HttpPost("marketing-flyer/reset")]
    public async Task<ActionResult<MarketingFlyerDto>> ResetMarketingFlyer(CancellationToken cancellationToken)
    {
        var snap = await _marketingFlyer.ResetToDefaultsAsync(cancellationToken);
        return Ok(ToMarketingFlyerDto(snap));
    }

    [HttpGet("marketing-flyer.pdf")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadMarketingFlyerPdf(CancellationToken cancellationToken)
    {
        var pdf = await _marketingFlyerPdf.RenderAsync(cancellationToken);
        return File(pdf, "application/pdf", "lobsy-werkgeversflyer.pdf");
    }

    private static MarketingFlyerDto ToMarketingFlyerDto(MarketingFlyerSnapshot snap) =>
        new(
            snap.Headline,
            snap.Subheadline,
            snap.Intro,
            string.Join('\n', snap.BulletPoints),
            snap.PromoFreeText,
            snap.PromoDiscountText,
            snap.CtaTitle,
            snap.CtaBody,
            snap.QrCaption,
            snap.QrPath,
            snap.FooterNote,
            snap.UpdatedAtUtc);

    [HttpGet("integration-credentials")]
    public async Task<ActionResult<IEnumerable<IntegrationCredentialDto>>> GetIntegrationCredentials(
        CancellationToken cancellationToken)
    {
        var items = await _credentials.GetConfigurableAsync(cancellationToken);
        return Ok(items.Select(ToDto));
    }

    [HttpGet("integration-credentials/{key}")]
    public async Task<ActionResult<IntegrationCredentialDto>> GetIntegrationCredential(
        IntegrationKey key,
        CancellationToken cancellationToken)
    {
        var item = await _credentials.GetAsync(key, cancellationToken);
        if (item is null)
        {
            return NotFound(new { message = "Deze integratie heeft geen settings-tegel." });
        }

        return Ok(ToDto(item));
    }

    [HttpPut("integration-credentials/{key}")]
    public async Task<ActionResult<IntegrationCredentialDto>> UpsertIntegrationCredential(
        IntegrationKey key,
        [FromBody] UpdateIntegrationCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var saved = await _credentials.UpsertAsync(
                key,
                new IntegrationCredentialUpdate(
                    request.ApiKey,
                    request.Model,
                    request.ClientId,
                    request.ClientSecret,
                    request.TenantId,
                    request.BaseUrl,
                    request.FromAddress,
                    request.ClearApiKey,
                    request.ClearClientSecret,
                    request.UseEnvironmentCredentials),
                cancellationToken);
            return Ok(ToDto(saved));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static PlatformFeatureDto ToFeatureDto(PlatformFeatureSnapshot snap) =>
        new(
            snap.VacancyContentModerationEnabled,
            snap.AuthenticatorEnabled,
            snap.ExposeRegistrationActivationLinks,
            snap.PublicWebBaseUrl,
            snap.UpdatedAtUtc,
            snap.InactiveCompanyDays,
            snap.SessionInactivityTimeoutMinutes,
            snap.FreePublishUntil);

    private static PlatformCompanyDto ToCompanyDto(PlatformCompanySnapshot snap) =>
        new(
            snap.CompanyName,
            snap.Slogan,
            snap.Address,
            snap.PostalCode,
            snap.City,
            snap.Country,
            snap.KvkNumber,
            snap.VatNumber,
            snap.Phone,
            snap.Email,
            IbanMasking.ForApi(snap.VatBufferIban),
            snap.UpdatedAtUtc);

    private static IntegrationCredentialDto ToDto(IntegrationCredentialView view) =>
        new(
            view.Key.ToString(),
            view.DisplayName,
            view.Description,
            view.HasApiKey,
            view.ApiKeyMasked,
            view.HasClientSecret,
            view.ClientSecretMasked,
            view.ClientId,
            view.TenantId,
            view.Model,
            view.BaseUrl,
            view.FromAddress,
            view.SupportsApiKey,
            view.SupportsModel,
            view.SupportsOAuth,
            view.SupportsTenantId,
            view.SupportsBaseUrl,
            view.SupportsFromAddress,
            view.LastPingOk,
            view.LastPingMessage,
            view.LastPingAtUtc,
            view.UpdatedAtUtc,
            view.IgnoresEnvironmentCredentials,
            view.UsesEnvironmentCredentials);
}

public sealed record PlatformCompanyDto(
    string CompanyName,
    string Slogan,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? KvkNumber,
    string? VatNumber,
    string? Phone,
    string? Email,
    string? VatBufferIban,
    DateTime? UpdatedAtUtc);

public sealed record UpdatePlatformCompanyRequest(
    string? CompanyName,
    string? Slogan,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? KvkNumber,
    string? VatNumber,
    string? Phone,
    string? Email,
    string? VatBufferIban = null);

public sealed record UpdateAboutPageRequest(
    string? Title,
    string? Lead,
    string? BodyHtml);

public sealed record MarketingFlyerDto(
    string Headline,
    string Subheadline,
    string Intro,
    string BulletPoints,
    string PromoFreeText,
    string PromoDiscountText,
    string CtaTitle,
    string CtaBody,
    string QrCaption,
    string QrPath,
    string FooterNote,
    DateTime? UpdatedAtUtc);

public sealed record UpdateMarketingFlyerRequest(
    string? Headline,
    string? Subheadline,
    string? Intro,
    string? BulletPoints,
    string? PromoFreeText,
    string? PromoDiscountText,
    string? CtaTitle,
    string? CtaBody,
    string? QrCaption,
    string? QrPath,
    string? FooterNote);
