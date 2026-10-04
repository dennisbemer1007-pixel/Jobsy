using Jobsy.Api.Admin;
using Jobsy.Api.Models;
using Jobsy.Core.Admin;
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
using Microsoft.Extensions.DependencyInjection;

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
    private readonly IMarketingFlyerSettingsService _marketingFlyer;
    private readonly IMarketingFlyerPdfService _marketingFlyerPdf;
    private readonly IFlexCommercialService _flexCommercial;
    private readonly IAdminAuditLog _audit;
    private readonly IAdminAuditContext _auditContext;
    private readonly IUserLookupService _users;

    public SettingsController(
        JobsyDbContext db,
        IIntegrationCredentialService credentials,
        IPlatformFeatureService features,
        IPlatformCompanySettingsService companySettings,
        IMarketingFlyerSettingsService marketingFlyer,
        IMarketingFlyerPdfService marketingFlyerPdf,
        IFlexCommercialService flexCommercial,
        IAdminAuditLog audit,
        IAdminAuditContext auditContext,
        IUserLookupService users)
    {
        _db = db;
        _credentials = credentials;
        _features = features;
        _companySettings = companySettings;
        _marketingFlyer = marketingFlyer;
        _marketingFlyerPdf = marketingFlyerPdf;
        _flexCommercial = flexCommercial;
        _audit = audit;
        _auditContext = auditContext;
        _users = users;
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
        var lobsyCommercial = await _flexCommercial.GetAsync(cancellationToken);

        return Ok(new
        {
            packs,
            costs,
            earlyAdapterRules = early,
            pushBomSettings,
            pushBomPricingTiers = pushBomTiers,
            lobsyCommercial
        });
    }

    [HttpGet("lobsy-commercial")]
    public async Task<ActionResult<FlexCommercialSettingsDto>> GetLobsyCommercial(CancellationToken cancellationToken)
        => Ok(await _flexCommercial.GetAsync(cancellationToken));

    [HttpPut("lobsy-commercial")]
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
    public async Task<ActionResult<FlexCommercialSettingsDto>> UpdateLobsyCommercial(
        [FromBody] UpdateLobsyCommercialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _flexCommercial.UpdateAsync(
                new FlexCommercialSettingsUpdate(
                    request.MarginPerHourEuro,
                    request.BackofficePartnerName ?? "",
                    request.DeepTestPriceCompetenceEuro > 0
                        ? request.DeepTestPriceCompetenceEuro
                        : request.DeepAnalysisPriceEuro,
                    request.DeepTestPriceCareerEuro > 0
                        ? request.DeepTestPriceCareerEuro
                        : (request.DeepAnalysisPriceEuro > 0
                            ? request.DeepAnalysisPriceEuro
                            : request.DeepTestPriceCompetenceEuro),
                    request.DeepTestPriceValuesEuro > 0
                        ? request.DeepTestPriceValuesEuro
                        : (request.DeepAnalysisPriceEuro > 0
                            ? request.DeepAnalysisPriceEuro
                            : request.DeepTestPriceCompetenceEuro),
                    request.DeepTestPriceCultureEuro > 0
                        ? request.DeepTestPriceCultureEuro
                        : (request.DeepAnalysisPriceEuro > 0
                            ? request.DeepAnalysisPriceEuro
                            : request.DeepTestPriceCompetenceEuro),
                    request.AgencyAnnualPriceEuro,
                    request.ContactUnlockCostTokens),
                cancellationToken));
        }
        catch (ArgumentException ex)
        {
            var code = string.Equals(ex.Message, "invalid_price", StringComparison.Ordinal)
                ? "invalid_price"
                : null;
            return BadRequest(code is null
                ? new { message = ex.Message }
                : new { code, message = ex.Message });
        }
    }

    [HttpPut("token-pricing/packs/{id:guid}")]
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting, TargetRouteKey = "id")]
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
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting, TargetRouteKey = "id")]
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

        var before = new { cost.Reason, cost.CostTokens, cost.IsActive };
        cost.CostTokens = request.CostTokens;
        cost.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        // TODO(admin-07): migrate to IAdminAuditLog when admin-redesign 07 lands.
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "AdminSettings",
            Message = "settings.pricing.update",
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                actorUserId = User.FindFirst("sub")?.Value
                              ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                key = cost.Reason.ToString(),
                before,
                after = new { cost.Reason, cost.CostTokens, cost.IsActive }
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("token-pricing/pushbom-settings")]
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
    [AdminAudit(AdminAuditKeys.SettingsPricingDelete, TargetType = AdminAuditKeys.TargetTypes.Setting, TargetRouteKey = "id")]
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
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
    [AdminAudit(AdminAuditKeys.SettingsPlatformUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
    public async Task<ActionResult<PlatformFeatureDto>> UpdatePlatformFeatures(
        [FromBody] UpdatePlatformFeatureRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var before = await _features.GetAsync(cancellationToken);
            var snap = await _features.UpdateAsync(
                new PlatformFeatureUpdate(
                    request.VacancyContentModerationEnabled,
                    request.AuthenticatorEnabled,
                    request.PublicWebBaseUrl,
                    request.InactiveCompanyDays,
                    request.SessionInactivityTimeoutMinutes,
                    request.FreePublishUntil,
                    request.ClearFreePublishUntil,
                    SupportAccessNotifyAdmins: request.SupportAccessNotifyAdmins,
                    SupportAccessNotifySubject: request.SupportAccessNotifySubject,
CandidateInsightsEnabled: request.CandidateInsightsEnabled,
                    CandidateInsightsUnlockDays: request.CandidateInsightsUnlockDays,
                    CandidateInsightsUnlockPerBranch: request.CandidateInsightsUnlockPerBranch,
                    SchoolsEnabled: request.SchoolsEnabled,
                    SchoolPerCodeResultsEnabled: request.SchoolPerCodeResultsEnabled,
                    SchoolRetentionCutoffMonth: request.SchoolRetentionCutoffMonth,
                    SchoolRetentionCutoffDay: request.SchoolRetentionCutoffDay,
                    AmbassadorsEnabled: request.AmbassadorsEnabled,
                    EmployersEnabled: request.EmployersEnabled,
                    CandidatePassportEnabled: request.CandidatePassportEnabled,
                    PassportPartnersEnabled: request.PassportPartnersEnabled,
                    PassportPdfV2Enabled: request.PassportPdfV2Enabled,
                    PhoneVerificationEnabled: request.PhoneVerificationEnabled,
                    WhatsAppRemindersEnabled: request.WhatsAppRemindersEnabled),
                cancellationToken);

            var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
            var changes = CollectPlatformFeatureChanges(before, snap);
            _auditContext.SuppressAutoWrite = true;
            var correlation = HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
            var ip = HttpContext?.Connection.RemoteIpAddress?.ToString();
            foreach (var change in changes)
            {
                await _audit.WriteAsync(
                    new AdminAuditEntry(
                        Action: AdminAuditKeys.SettingsPlatformUpdate,
                        TargetType: AdminAuditKeys.TargetTypes.Setting,
                        TargetId: change.Field,
                        TargetLabel: change.Field,
                        Reason: request.Reason,
                        DetailsJson: System.Text.Json.JsonSerializer.Serialize(new
                        {
                            field = change.Field,
                            from = change.From,
                            to = change.To
                        }),
                        Result: AdminAuditKeys.Results.Success,
                        ActorUserId: actor?.Id,
                        ActorRole: "Admin",
                        CorrelationId: correlation,
                        IpAddress: ip),
                    cancellationToken);
            }

            return Ok(ToFeatureDto(snap));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static List<(string Field, string? From, string? To)> CollectPlatformFeatureChanges(
        PlatformFeatureSnapshot before,
        PlatformFeatureSnapshot after)
    {
        var list = new List<(string, string?, string?)>();
        void Add(string field, string? from, string? to)
        {
            if (!string.Equals(from, to, StringComparison.Ordinal))
            {
                list.Add((field, from, to));
            }
        }

        Add("VacancyContentModerationEnabled", before.VacancyContentModerationEnabled.ToString(), after.VacancyContentModerationEnabled.ToString());
        Add("AuthenticatorEnabled", before.AuthenticatorEnabled.ToString(), after.AuthenticatorEnabled.ToString());
        Add("PublicWebBaseUrl", before.PublicWebBaseUrl, after.PublicWebBaseUrl);
        Add("InactiveCompanyDays", before.InactiveCompanyDays.ToString(), after.InactiveCompanyDays.ToString());
        Add("SessionInactivityTimeoutMinutes", before.SessionInactivityTimeoutMinutes.ToString(), after.SessionInactivityTimeoutMinutes.ToString());
        Add("FreePublishUntil", before.FreePublishUntil?.ToString("yyyy-MM-dd"), after.FreePublishUntil?.ToString("yyyy-MM-dd"));
        Add("SupportAccessNotifyAdmins", before.SupportAccessNotifyAdmins.ToString(), after.SupportAccessNotifyAdmins.ToString());
        Add("SupportAccessNotifySubject", before.SupportAccessNotifySubject.ToString(), after.SupportAccessNotifySubject.ToString());
        Add("CandidateInsightsEnabled", before.CandidateInsightsEnabled.ToString(), after.CandidateInsightsEnabled.ToString());
        Add("CandidateInsightsUnlockDays", before.CandidateInsightsUnlockDays.ToString(), after.CandidateInsightsUnlockDays.ToString());
        Add("CandidateInsightsUnlockPerBranch", before.CandidateInsightsUnlockPerBranch.ToString(), after.CandidateInsightsUnlockPerBranch.ToString());
        Add("SchoolsEnabled", before.SchoolsEnabled.ToString(), after.SchoolsEnabled.ToString());
        Add("SchoolPerCodeResultsEnabled", before.SchoolPerCodeResultsEnabled.ToString(), after.SchoolPerCodeResultsEnabled.ToString());
        Add("SchoolRetentionCutoffMonth", before.SchoolRetentionCutoffMonth.ToString(), after.SchoolRetentionCutoffMonth.ToString());
        Add("SchoolRetentionCutoffDay", before.SchoolRetentionCutoffDay.ToString(), after.SchoolRetentionCutoffDay.ToString());
        Add("AmbassadorsEnabled", before.AmbassadorsEnabled.ToString(), after.AmbassadorsEnabled.ToString());
        Add("PassportPartnersEnabled", before.PassportPartnersEnabled.ToString(), after.PassportPartnersEnabled.ToString());
        Add("PassportPdfV2Enabled", before.PassportPdfV2Enabled.ToString(), after.PassportPdfV2Enabled.ToString());
        Add("PhoneVerificationEnabled", before.PhoneVerificationEnabled.ToString(), after.PhoneVerificationEnabled.ToString());
        Add("WhatsAppRemindersEnabled", before.WhatsAppRemindersEnabled.ToString(), after.WhatsAppRemindersEnabled.ToString());
        return list;
    }

    /// <summary>
    /// Public feature-flag snapshot for Web/UI bootstrap (not secrets).
    /// </summary>
    [HttpGet("feature-flags")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        return Ok(new
        {
            employersEnabled = snap.EmployersEnabled,
            candidatePassportEnabled = snap.CandidatePassportEnabled,
            passportPartnersEnabled = snap.PassportPartnersEnabled,
            passportPdfV2Enabled = snap.PassportPdfV2Enabled,
            phoneVerificationEnabled = snap.PhoneVerificationEnabled,
            schoolsEnabled = snap.SchoolsEnabled,
            ambassadorsEnabled = snap.AmbassadorsEnabled,
            whatsAppRemindersEnabled = snap.WhatsAppRemindersEnabled
        });
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
    [AdminAudit(AdminAuditKeys.SettingsCompanyUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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

    [HttpGet("marketing-flyer")]
    public async Task<ActionResult<MarketingFlyerDto>> GetMarketingFlyer(CancellationToken cancellationToken)
    {
        var snap = await _marketingFlyer.GetAsync(cancellationToken);
        return Ok(ToMarketingFlyerDto(snap));
    }

    [HttpPut("marketing-flyer")]
    [AdminAudit(AdminAuditKeys.SettingsFlyerUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
    [AdminAudit(AdminAuditKeys.SettingsFlyerUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
    [AdminAudit(AdminAuditKeys.SettingsIntegrationUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting, TargetRouteKey = "key")]
    public async Task<ActionResult<IntegrationCredentialDto>> UpsertIntegrationCredential(
        IntegrationKey key,
        [FromBody] UpdateIntegrationCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Field names only — never secret values in the audit log.
            var fields = new List<string>();
            if (request.ApiKey is not null || request.ClearApiKey) fields.Add("ApiKey");
            if (request.Model is not null) fields.Add("Model");
            if (request.ClientId is not null) fields.Add("ClientId");
            if (request.ClientSecret is not null || request.ClearClientSecret) fields.Add("ClientSecret");
            if (request.TenantId is not null) fields.Add("TenantId");
            if (request.BaseUrl is not null) fields.Add("BaseUrl");
            if (request.FromAddress is not null) fields.Add("FromAddress");
            fields.Add("UseEnvironmentCredentials");
            _auditContext.TargetId = key.ToString();
            _auditContext.TargetLabel = key.ToString();
            _auditContext.SetDetailsObject(new { fields });

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
            snap.PublicWebBaseUrl,
            snap.UpdatedAtUtc,
            snap.InactiveCompanyDays,
            snap.SessionInactivityTimeoutMinutes,
            snap.FreePublishUntil,
            snap.SupportAccessNotifyAdmins,
            snap.SupportAccessNotifySubject,
snap.CandidateInsightsEnabled,
            snap.CandidateInsightsUnlockDays,
            snap.CandidateInsightsUnlockPerBranch,
            snap.SchoolsEnabled,
            snap.SchoolPerCodeResultsEnabled,
            snap.SchoolRetentionCutoffMonth,
            snap.SchoolRetentionCutoffDay,
            snap.AmbassadorsEnabled,
            snap.EmployersEnabled,
            snap.CandidatePassportEnabled,
            snap.PassportPartnersEnabled,
            snap.PassportPdfV2Enabled,
            snap.PhoneVerificationEnabled,
            snap.WhatsAppRemindersEnabled);

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

public sealed record UpdateLobsyCommercialRequest(
    decimal MarginPerHourEuro,
    string? BackofficePartnerName,
    decimal DeepAnalysisPriceEuro = 0,
    decimal DeepTestPriceCompetenceEuro = 0,
    decimal DeepTestPriceCareerEuro = 0,
    decimal DeepTestPriceValuesEuro = 0,
    decimal DeepTestPriceCultureEuro = 0,
    decimal AgencyAnnualPriceEuro = 0,
    decimal ContactUnlockCostTokens = 0);
