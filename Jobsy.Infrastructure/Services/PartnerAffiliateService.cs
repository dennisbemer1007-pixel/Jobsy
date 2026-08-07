using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class PartnerAffiliateService : IPartnerAffiliateService
{
    public const string ReferralRewardNotePrefix = "Referralbonus welkomsttoken";

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokens;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<PartnerAffiliateService> _logger;

    public PartnerAffiliateService(
        JobsyDbContext db,
        ITokenLedgerService tokens,
        IPlatformFeatureService features,
        ILogger<PartnerAffiliateService>? logger = null)
    {
        _db = db;
        _tokens = tokens;
        _features = features;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PartnerAffiliateService>.Instance;
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

        if (await IsRelatedPartyReferralAsync(profile.UserId, company, cancellationToken))
        {
            return false;
        }

        company.ReferredByPartnerUserId = profile.UserId;
        company.PartnerReferralStatus = PartnerReferralStatus.Pending;
        company.PartnerReferredAtUtc = DateTime.UtcNow;
        company.PartnerReferralRewardedAtUtc = null;
        return true;
    }

    public async Task<bool> TryRewardOnWelcomeTokenSpendAsync(
        Guid referredCompanyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == referredCompanyId, cancellationToken);
        if (company is null
            || company.ReferredByPartnerUserId is null
            || company.PartnerReferralStatus != PartnerReferralStatus.Pending
            || !company.WelcomeTokenLedgerCredited)
        {
            return false;
        }

        var partnerUserId = company.ReferredByPartnerUserId.Value;
        var partner = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == partnerUserId && u.IsActive, cancellationToken);
        if (partner?.CompanyId is not Guid partnerCompanyId)
        {
            _logger.LogWarning(
                "Partner referral reward skipped for company {CompanyId}: partner {PartnerUserId} has no company wallet",
                referredCompanyId,
                partnerUserId);
            return false;
        }

        // Idempotency: already granted for this referred company.
        var rewardNote = $"{ReferralRewardNotePrefix} ({referredCompanyId:N})";
        var alreadyGranted = await _db.TokenTransactions.AsNoTracking()
            .AnyAsync(
                t => t.CompanyId == partnerCompanyId
                     && t.Kind == TokenTransactionKind.Grant
                     && t.Note == rewardNote,
                cancellationToken);
        if (alreadyGranted)
        {
            company.PartnerReferralStatus = PartnerReferralStatus.Rewarded;
            company.PartnerReferralRewardedAtUtc ??= DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        await _tokens.GrantAsync(
            partnerCompanyId,
            SalesCommissionRules.PartnerReferralRewardTokens,
            actorUserId: partnerUserId,
            note: rewardNote,
            cancellationToken);

        company.PartnerReferralStatus = PartnerReferralStatus.Rewarded;
        company.PartnerReferralRewardedAtUtc = DateTime.UtcNow;
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "PartnerReferral",
            Message =
                $"Referralbonus {SalesCommissionRules.PartnerReferralRewardTokens} token toegekend aan partner {partnerUserId} voor bedrijf {referredCompanyId}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Partner referral reward granted: referred company {CompanyId} → partner {PartnerUserId} (+{Tokens} tokens)",
            referredCompanyId,
            partnerUserId,
            SalesCommissionRules.PartnerReferralRewardTokens);
        return true;
    }

    public async Task<PartnerAffiliateMeDto?> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        var user = await _db.Users.AsNoTracking()
            .FirstAsync(u => u.Id == userId, cancellationToken);
        var referrals = await GetReferralsAsync(userId, cancellationToken);
        var tokensEarned = await SumReferralTokensEarnedAsync(userId, cancellationToken);

        return new PartnerAffiliateMeDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            profile.TrackingCode,
            tokensEarned,
            referrals.Count,
            referrals.Count(r => r.Status == nameof(PartnerReferralStatus.Pending)),
            referrals.Count(r => r.Status == nameof(PartnerReferralStatus.Rewarded)),
            referrals);
    }

    public async Task<IReadOnlyList<PartnerAffiliateReferralRowDto>> GetReferralsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProfileAsync(userId, cancellationToken);

        var companies = await _db.Companies.AsNoTracking()
            .Where(c => c.ReferredByPartnerUserId == userId)
            .OrderByDescending(c => c.PartnerReferredAtUtc ?? c.FirstYearStartedAt)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return companies.Select(MapReferralRow).ToList();
    }

    public async Task<PartnerAffiliateToolkitDto?> GetToolkitAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = features.PublicWebBaseUrl.TrimEnd('/');
        var escaped = Uri.EscapeDataString(profile.TrackingCode);

        return new PartnerAffiliateToolkitDto(
            profile.TrackingCode,
            $"{baseUrl}/partner/{escaped}",
            $"{baseUrl}/register?ref={escaped}",
            $"{baseUrl}/api/sales-commercial/flyer.pdf?trackingCode={escaped}");
    }

    public static bool IsPartnerTrackingCode(string? trackingCode) =>
        NormalizePartnerTrackingCode(trackingCode) is not null;

    public static string StatusLabel(PartnerReferralStatus status, bool welcomeTokenAvailable) =>
        status switch
        {
            PartnerReferralStatus.Rewarded => "Actief - Bonus toegekend",
            PartnerReferralStatus.Pending when welcomeTokenAvailable => "Welkomsttoken nog beschikbaar",
            PartnerReferralStatus.Pending => "Gekoppeld - wacht op welkomsttoken",
            _ => "—"
        };

    private static PartnerAffiliateReferralRowDto MapReferralRow(Company company)
    {
        var status = company.PartnerReferralStatus;
        if (status == PartnerReferralStatus.None && company.ReferredByPartnerUserId is not null)
        {
            // Legacy rows attributed before status column existed.
            status = PartnerReferralStatus.Pending;
        }

        var welcomeAvailable = status == PartnerReferralStatus.Pending && company.WelcomeTokenLedgerCredited;
        return new PartnerAffiliateReferralRowDto(
            company.Id,
            company.Name,
            status.ToString(),
            StatusLabel(status, welcomeAvailable),
            company.PartnerReferredAtUtc ?? company.FirstYearStartedAt,
            company.PartnerReferralRewardedAtUtc,
            welcomeAvailable);
    }

    private async Task<decimal> SumReferralTokensEarnedAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        var partnerCompanyId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == partnerUserId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (partnerCompanyId is null)
        {
            return 0m;
        }

        var prefix = ReferralRewardNotePrefix;
        return await _db.TokenTransactions.AsNoTracking()
            .Where(t => t.CompanyId == partnerCompanyId
                        && t.Kind == TokenTransactionKind.Grant
                        && t.Note != null
                        && t.Note.StartsWith(prefix))
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
    }

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
