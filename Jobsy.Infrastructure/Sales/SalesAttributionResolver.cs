using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesAttributionResolver : ISalesAttributionResolver
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<SalesAttributionResolver> _logger;

    public SalesAttributionResolver(
        JobsyDbContext db,
        IPlatformFeatureService features,
        ILogger<SalesAttributionResolver> logger)
    {
        _db = db;
        _features = features;
        _logger = logger;
    }

    public async Task<SalesAttributionResolution> ResolveAtRegistrationAsync(
        string? typedCode,
        string? cookieCode,
        string registeringEmail,
        string? registeringKvkNumber,
        Guid? registeringUserId = null,
        CancellationToken cancellationToken = default)
    {
        var typed = await TryResolveCandidateAsync(typedCode, SalesAttributionSource.TypedCode, cancellationToken);
        if (typed is not null)
        {
            return await ApplySelfReferralGuardAsync(
                typed, registeringEmail, registeringKvkNumber, registeringUserId, cancellationToken);
        }

        var cookie = await TryResolveCandidateAsync(cookieCode, SalesAttributionSource.LinkCookie, cancellationToken);
        if (cookie is not null)
        {
            return await ApplySelfReferralGuardAsync(
                cookie, registeringEmail, registeringKvkNumber, registeringUserId, cancellationToken);
        }

        return SalesAttributionResolution.None;
    }

    public async Task<SalesActiveReferral?> ResolveActiveReferralAsync(
        string? code,
        CancellationToken cancellationToken = default)
    {
        var normalized = SalesTrackingCodes.Normalize(code);
        if (normalized is null)
        {
            return null;
        }

        if (SalesTrackingCodes.IsAmbassadeur(normalized)
            && !await AmbassadorsFeatureGate.IsEnabledAsync(_features, cancellationToken))
        {
            return null;
        }

        if (SalesTrackingCodes.IsPartner(normalized))
        {
            var partner = await _db.PartnerAffiliateProfiles.AsNoTracking()
                .Where(p => p.TrackingCode.ToUpper() == normalized
                            && p.User.IsActive
                            && (p.User.Role == UserRole.EnterpriseManager
                                || p.User.Role == UserRole.Intermediary))
                .Select(p => new { p.UserId })
                .FirstOrDefaultAsync(cancellationToken);
            return partner is null
                ? null
                : new SalesActiveReferral(normalized, partner.UserId, SalesResolvedCodeKind.Partner);
        }

        if (SalesTrackingCodes.IsSalesManager(normalized))
        {
            var sm = await _db.SalesManagerProfiles.AsNoTracking()
                .Where(p => p.TrackingCode != null
                            && p.TrackingCode.ToUpper() == normalized
                            && p.OnboardingCompletedAt != null
                            && p.AgreementSignedAt != null)
                .Select(p => new { p.UserId })
                .FirstOrDefaultAsync(cancellationToken);
            return sm is null
                ? null
                : new SalesActiveReferral(normalized, sm.UserId, SalesResolvedCodeKind.SalesManager);
        }

        if (SalesTrackingCodes.IsAmbassadeur(normalized))
        {
            // Parked: already returned null above when disabled. When enabled, landing is Ambassadeur-only.
            return null;
        }

        return null;
    }

    private async Task<SalesAttributionResolution?> TryResolveCandidateAsync(
        string? raw,
        SalesAttributionSource source,
        CancellationToken cancellationToken)
    {
        var active = await ResolveActiveReferralAsync(raw, cancellationToken);
        if (active is null)
        {
            return null;
        }

        return new SalesAttributionResolution(
            active.Kind,
            active.Code,
            active.BeneficiaryUserId,
            source);
    }

    private async Task<SalesAttributionResolution> ApplySelfReferralGuardAsync(
        SalesAttributionResolution candidate,
        string registeringEmail,
        string? registeringKvkNumber,
        Guid? registeringUserId,
        CancellationToken cancellationToken)
    {
        if (candidate.Kind != SalesResolvedCodeKind.SalesManager
            || candidate.BeneficiaryUserId is not Guid beneficiaryId
            || candidate.Code is null)
        {
            return candidate;
        }

        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => p.UserId == beneficiaryId)
            .Select(p => new { p.UserId, p.KvkNumber, Email = p.User!.Email })
            .FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            return SalesAttributionResolution.None;
        }

        var rule = SalesSelfReferralRule.None;
        if (registeringUserId is Guid uid && uid == profile.UserId)
        {
            rule = SalesSelfReferralRule.SameUser;
        }
        else if (EmailsMatch(registeringEmail, profile.Email))
        {
            rule = SalesSelfReferralRule.SameEmail;
        }
        else if (KvksMatch(registeringKvkNumber, profile.KvkNumber))
        {
            rule = SalesSelfReferralRule.SameKvk;
        }
        else if (SameNonFreemailDomain(registeringEmail, profile.Email))
        {
            rule = SalesSelfReferralRule.SameEmailDomain;
        }

        if (rule == SalesSelfReferralRule.None)
        {
            return candidate;
        }

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "sales.attribution.self-referral-blocked",
            Message = "Self-referral attribution blocked",
            DetailsJson = JsonSerializer.Serialize(new
            {
                code = candidate.Code,
                rule = rule.ToString(),
                beneficiaryUserId = beneficiaryId
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Self-referral blocked for code {Code} rule {Rule}",
            candidate.Code, rule);

        return new SalesAttributionResolution(
            SalesResolvedCodeKind.None,
            null,
            null,
            null,
            rule);
    }

    private static bool EmailsMatch(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return false;
        }

        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool KvksMatch(string? a, string? b)
    {
        var left = DigitsOnly(a);
        var right = DigitsOnly(b);
        return left is { Length: 8 } && right is { Length: 8 }
               && string.Equals(left, right, StringComparison.Ordinal);
    }

    private static bool SameNonFreemailDomain(string? a, string? b)
    {
        var da = FreemailDomains.TryGetDomain(a);
        var db = FreemailDomains.TryGetDomain(b);
        if (da is null || db is null)
        {
            return false;
        }

        if (FreemailDomains.IsFreemail(da) || FreemailDomains.IsFreemail(db))
        {
            return false;
        }

        return string.Equals(da, db, StringComparison.OrdinalIgnoreCase);
    }

    private static string? DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Span<char> buffer = stackalloc char[16];
        var n = 0;
        foreach (var c in value)
        {
            if (char.IsDigit(c) && n < buffer.Length)
            {
                buffer[n++] = c;
            }
        }

        return n == 0 ? null : new string(buffer[..n]);
    }
}
