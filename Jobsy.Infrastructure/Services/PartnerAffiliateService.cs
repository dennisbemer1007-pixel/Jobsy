using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class PartnerAffiliateService : IPartnerAffiliateService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly JobsyDbContext _db;
    private readonly ISalesCommercialService _commercial;
    private readonly ICommissionLedgerService _ledger;
    private readonly IPlatformFeatureService _features;

    public PartnerAffiliateService(
        JobsyDbContext db,
        ISalesCommercialService commercial,
        ICommissionLedgerService ledger,
        IPlatformFeatureService features)
    {
        _db = db;
        _commercial = commercial;
        _ledger = ledger;
        _features = features;
    }

    public async Task<PartnerAffiliateProfile> EnsureProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.PartnerAffiliateProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = await _db.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Gebruiker niet gevonden.");

        var prefix = PartnerAffiliateProfile.PrefixForRole(user.Role);
        var now = DateTime.UtcNow;
        var profile = new PartnerAffiliateProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyName = user.Company?.Name,
            KvkNumber = user.Company?.KvkNumber,
            Address = user.Company?.Address,
            Country = "NL",
            TrackingCode = await GenerateUniqueTrackingCodeAsync(prefix, cancellationToken),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.PartnerAffiliateProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<PartnerAffiliateProfile?> ResolveByTrackingCodeAsync(
        string? trackingCode,
        CancellationToken cancellationToken = default)
    {
        var code = NormalizePartnerTrackingCode(trackingCode);
        if (code is null)
        {
            return null;
        }

        return await _db.PartnerAffiliateProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(
                p => p.TrackingCode.ToUpper() == code
                     && p.User.IsActive
                     && (p.User.Role == UserRole.EnterpriseManager || p.User.Role == UserRole.Intermediary),
                cancellationToken);
    }

    public async Task<bool> ApplyReferralAsync(
        Company company,
        string? trackingCode,
        CancellationToken cancellationToken = default)
    {
        if (company.ReferredByPartnerUserId is not null
            || company.ReferredBySalesManagerUserId is not null
            || company.ReferredByAmbassadeurUserId is not null)
        {
            return false;
        }

        var profile = await ResolveByTrackingCodeAsync(trackingCode, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        // Block self-referral / related-party attribution (same user company or same KVK family).
        if (await IsRelatedPartyReferralAsync(profile.UserId, company, cancellationToken))
        {
            return false;
        }

        company.ReferredByPartnerUserId = profile.UserId;
        company.FirstYearStartedAt ??= DateTime.UtcNow;
        company.CommissionDurationDaysSnapshot ??=
            (await _commercial.GetSettingsAsync(cancellationToken)).CommissionDurationDays;
        return true;
    }

    public async Task<CommissionLedgerEntry?> TryCreditTokenCommissionAsync(
        Guid partnerUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountExVatEuro,
        CancellationToken cancellationToken = default)
    {
        if (purchaseAmountExVatEuro <= 0)
        {
            return null;
        }

        var profileExists = await _db.PartnerAffiliateProfiles.AsNoTracking()
            .AnyAsync(p => p.UserId == partnerUserId, cancellationToken);
        if (!profileExists)
        {
            return null;
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null || company.ReferredByPartnerUserId != partnerUserId)
        {
            return null;
        }

        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        return await _ledger.TryCreditPartnerTokenCommissionAsync(
            partnerUserId,
            companyId,
            tokenCheckoutId,
            purchaseAmountExVatEuro,
            company.FirstYearStartedAt,
            Math.Max(0m, settings.PartnerCommissionRate),
            company.CommissionDurationDaysSnapshot ?? settings.CommissionDurationDays,
            cancellationToken);
    }

    public async Task<PartnerAffiliateMeDto?> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        var user = await _db.Users.AsNoTracking()
            .FirstAsync(u => u.Id == userId, cancellationToken);
        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var balance = await _ledger.GetBalanceExVatAsync(userId, cancellationToken);
        var referredCount = await _db.Companies.AsNoTracking()
            .CountAsync(c => c.ReferredByPartnerUserId == userId, cancellationToken);
        var ledger = await _ledger.ListEntriesAsync(userId, cancellationToken);

        return new PartnerAffiliateMeDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            profile.TrackingCode,
            settings.PartnerCommissionRate,
            balance,
            SalesCommissionRules.InclVat(balance),
            referredCount,
            ledger.Take(20).Select(e => new PartnerAffiliateLedgerSummaryDto(
                e.Id,
                e.Kind.ToString(),
                e.AmountExVat,
                e.VatAmount,
                e.Note,
                e.CompanyId,
                e.Company?.Name,
                e.CreatedAt,
                e.SelfBillingInvoiceId)).ToList());
    }

    public async Task<IReadOnlyList<PartnerAffiliateTokenLogRowDto>> GetTokenLogAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProfileAsync(userId, cancellationToken);

        var entries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Include(e => e.Company)
            .Where(e => e.SalesManagerUserId == userId
                        && e.AmountExVat != 0)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var checkoutIds = entries
            .Where(e => e.SourceTokenCheckoutId is not null)
            .Select(e => e.SourceTokenCheckoutId!.Value)
            .Distinct()
            .ToList();

        var checkouts = await _db.TokenPurchaseCheckouts.AsNoTracking()
            .Where(c => checkoutIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        return entries.Select(entry =>
        {
            checkouts.TryGetValue(entry.SourceTokenCheckoutId ?? Guid.Empty, out var checkout);
            return new PartnerAffiliateTokenLogRowDto(
                entry.Id,
                entry.CompanyId,
                entry.Company?.Name,
                entry.CreatedAt,
                entry.AmountExVat > 0 ? checkout?.PackSize ?? 0 : 0,
                Math.Max(0m, entry.AmountExVat),
                Math.Max(0m, -entry.AmountExVat),
                entry.Kind.ToString(),
                entry.Note);
        }).ToList();
    }

    public async Task<PartnerAffiliateToolkitDto?> GetToolkitAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = features.PublicWebBaseUrl.TrimEnd('/');
        var escaped = Uri.EscapeDataString(profile.TrackingCode);

        return new PartnerAffiliateToolkitDto(
            profile.TrackingCode,
            settings.PartnerCommissionRate,
            $"{baseUrl}/partner/{escaped}",
            $"{baseUrl}/register?ref={escaped}",
            $"{baseUrl}/api/sales-commercial/flyer.pdf?trackingCode={escaped}");
    }

    public static bool IsPartnerTrackingCode(string? trackingCode) =>
        NormalizePartnerTrackingCode(trackingCode) is not null;

    public async Task<PartnerAffiliateBillingDto?> GetBillingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        return MapBilling(profile);
    }

    public async Task<PartnerAffiliateBillingDto> UpdateBillingAsync(
        Guid userId,
        PartnerAffiliateBillingUpdate update,
        CancellationToken cancellationToken = default)
    {
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        profile.CompanyName = NormalizeOptional(update.CompanyName, 200);
        profile.KvkNumber = NormalizeOptional(update.KvkNumber, 20);
        profile.VatNumber = NormalizeOptional(update.VatNumber, 32);
        profile.Address = NormalizeOptional(update.Address, 300);
        profile.PostalCode = NormalizeOptional(update.PostalCode, 20);
        profile.City = NormalizeOptional(update.City, 120);
        profile.Country = string.IsNullOrWhiteSpace(update.Country) ? "NL" : update.Country.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(update.Iban))
        {
            var iban = NormalizeIban(update.Iban);
            if (iban is null)
            {
                throw new ArgumentException("Ongeldig IBAN. Controleer het rekeningnummer en probeer opnieuw.");
            }

            profile.Iban = iban;
        }
        else if (update.ClearIban)
        {
            profile.Iban = null;
        }

        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapBilling(profile);
    }

    public async Task<PartnerAffiliateBillingDto> SignAgreementAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        // Always stamp server-controlled version; ignore any client-supplied value.
        profile.AgreementSignedAt = DateTime.UtcNow;
        profile.AgreementVersion = SalesCommissionRules.CurrentPartnerAgreementVersion;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapBilling(profile);
    }

    private static PartnerAffiliateBillingDto MapBilling(PartnerAffiliateProfile profile) =>
        new(
            profile.CompanyName,
            profile.KvkNumber,
            profile.VatNumber,
            profile.Address,
            profile.PostalCode,
            profile.City,
            profile.Country ?? "NL",
            ISalesManagerPayoutService.MaskIban(profile.Iban),
            !string.IsNullOrWhiteSpace(profile.Iban),
            profile.AgreementSignedAt.HasValue,
            profile.AgreementVersion);

    private async Task<bool> IsRelatedPartyReferralAsync(
        Guid partnerUserId,
        Company company,
        CancellationToken cancellationToken)
    {
        var partner = await _db.Users.AsNoTracking()
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == partnerUserId, cancellationToken);
        if (partner is null)
        {
            return true;
        }

        if (partner.CompanyId == company.Id
            || (company.ParentCompanyId is Guid parent && partner.CompanyId == parent)
            || (partner.Company?.ParentCompanyId is Guid partnerParent
                && (partnerParent == company.Id || partnerParent == company.ParentCompanyId)))
        {
            return true;
        }

        var partnerOrgIds = await _db.UserCompanies.AsNoTracking()
            .Where(uc => uc.UserId == partnerUserId)
            .Select(uc => uc.CompanyId)
            .ToListAsync(cancellationToken);
        if (partner.CompanyId is Guid primary)
        {
            partnerOrgIds.Add(primary);
        }

        if (partnerOrgIds.Contains(company.Id)
            || (company.ParentCompanyId is Guid companyParent && partnerOrgIds.Contains(companyParent)))
        {
            return true;
        }

        var partnerKvk = NormalizeKvk(partner.Company?.KvkNumber);
        var companyKvk = NormalizeKvk(company.KvkNumber);
        return partnerKvk is not null && companyKvk is not null && partnerKvk == companyKvk;
    }

    private static string? NormalizeKvk(string? kvk)
    {
        if (string.IsNullOrWhiteSpace(kvk))
        {
            return null;
        }

        var digits = new string(kvk.Where(char.IsDigit).ToArray());
        return digits.Length >= 8 ? digits[..8] : null;
    }

    private static string? NormalizeOptional(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }

    private static string? NormalizeIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return null;
        }

        var compact = new string(iban.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        return compact.Length is >= 15 and <= 34 ? compact : null;
    }

    private async Task<string> GenerateUniqueTrackingCodeAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var code = prefix + GenerateSuffix();
            var exists = await _db.PartnerAffiliateProfiles.AsNoTracking()
                .AnyAsync(p => p.TrackingCode == code, cancellationToken);
            if (!exists && _db.PartnerAffiliateProfiles.Local.All(p => p.TrackingCode != code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Kon geen unieke partnercode genereren.");
    }

    private static string GenerateSuffix()
    {
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    private static string? NormalizePartnerTrackingCode(string? trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var code = trackingCode.Trim().ToUpperInvariant();
        if (code.Length != 9)
        {
            return null;
        }

        if (!code.StartsWith("BM-", StringComparison.Ordinal)
            && !code.StartsWith("IM-", StringComparison.Ordinal))
        {
            return null;
        }

        return code.Skip(3).All(c => Alphabet.Contains(c))
            ? code
            : null;
    }
}
