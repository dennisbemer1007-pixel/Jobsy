using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure.Services;

public sealed class PassportPartnerService : IPassportPartnerService
{
    private const string DevKeyFileName = "passport-partners-code-hmac.key";

    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private byte[]? _hmacKey;

    public PassportPartnerService(
        JobsyDbContext db,
        IPlatformFeatureService features,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _features = features;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<PassportCodeSummary?> ResolveCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!await FlagOnAsync(cancellationToken))
        {
            return null;
        }

        var row = await FindActiveCodeAsync(code, cancellationToken);
        if (row is null || row.PassportPartner is not { IsActive: true } partner || partner.Company is null)
        {
            return null;
        }

        var branch = row.BranchCompany?.Name ?? partner.Company.Name;
        return new PassportCodeSummary(
            partner.Id,
            partner.DisplayName,
            PassportPartner.TypeFromCompany(partner.Company.Type).ToString(),
            branch,
            partner.LogoPng is { Length: > 0 });
    }

    public async Task<PassportLinkMutation> AttachByCodeAsync(
        Guid candidateId,
        string code,
        PassportPartnerLinkSource source,
        CancellationToken cancellationToken = default)
    {
        if (!await FlagOnAsync(cancellationToken))
        {
            return Fail("feature_disabled");
        }

        var row = await FindActiveCodeAsync(code, cancellationToken);
        if (row?.PassportPartner is not { IsActive: true } partner)
        {
            return Fail("unknown_code");
        }

        var existing = await _db.PassportPartnerCandidateLinks
            .FirstOrDefaultAsync(
                l => l.CandidateUserId == candidateId && l.PassportPartnerId == partner.Id,
                cancellationToken);
        if (existing is not null)
        {
            return new PassportLinkMutation(true, null, existing.Id);
        }

        var link = new PassportPartnerCandidateLink
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateId,
            PassportPartnerId = partner.Id,
            PartnerCodeId = row.Id,
            Source = source,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.PassportPartnerCandidateLinks.Add(link);
        await _db.SaveChangesAsync(cancellationToken);
        return new PassportLinkMutation(true, null, link.Id);
    }

    public async Task<PassportLinkMutation> GiveConsentAsync(
        Guid candidateId,
        Guid linkId,
        string? consentVersion,
        bool contactConsent,
        bool confirmAdult,
        CancellationToken cancellationToken = default)
    {
        if (!await FlagOnAsync(cancellationToken))
        {
            return Fail("feature_disabled");
        }

        var link = await OwnLinkAsync(candidateId, linkId, cancellationToken);
        if (link is null)
        {
            return Fail("not_found");
        }

        if (!PassportPartnerRules.IsCurrentConsent(consentVersion))
        {
            return Fail("Partners.Consent.Version");
        }

        var user = await _db.Users.FirstAsync(u => u.Id == candidateId, cancellationToken);
        var age = AgeRules.AgeYearsFromDateOfBirth(user.DateOfBirth);
        if (!PassportPartnerRules.IsOldEnough(age, confirmAdult))
        {
            return Fail("Partners.Consent.Age");
        }

        var alreadyActive = PassportPartnerRules.IsActiveLink(link);
        if (!alreadyActive)
        {
            var active = await _db.PassportPartnerCandidateLinks.CountAsync(
                l => l.CandidateUserId == candidateId
                     && l.Id != link.Id
                     && l.ConsentGivenAtUtc != null
                     && l.RevokedAtUtc == null,
                cancellationToken);
            if (active >= PassportPartnerRules.MaxActivePartners)
            {
                return Fail("Partners.Consent.Max");
            }
        }

        var now = DateTime.UtcNow;
        link.ConsentGivenAtUtc = now;
        link.ConsentVersion = consentVersion;
        link.ContactConsentAtUtc = contactConsent ? now : null;
        link.ReconfirmDueAtUtc = PassportPartnerRules.NextReconfirmDue(now);
        link.ReconfirmReminderSentAtUtc = null;
        link.SuspendedAtUtc = null;
        link.RevokedAtUtc = null;
        link.RevokedReason = null;
        await _db.SaveChangesAsync(cancellationToken);
        return new PassportLinkMutation(true, null, link.Id);
    }

    public async Task<PassportLinkMutation> RevokeAsync(
        Guid candidateId,
        Guid linkId,
        PassportPartnerRevokeReason reason,
        CancellationToken cancellationToken = default)
    {
        if (!await FlagOnAsync(cancellationToken))
        {
            return Fail("feature_disabled");
        }

        var link = await OwnLinkAsync(candidateId, linkId, cancellationToken);
        if (link is null)
        {
            return Fail("not_found");
        }

        link.RevokedAtUtc = DateTime.UtcNow;
        link.RevokedReason = reason;
        await _db.SaveChangesAsync(cancellationToken);
        return new PassportLinkMutation(true, null, link.Id);
    }

    public async Task<PassportLinkMutation> ReconfirmAsync(
        Guid candidateId,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        if (!await FlagOnAsync(cancellationToken))
        {
            return Fail("feature_disabled");
        }

        var link = await OwnLinkAsync(candidateId, linkId, cancellationToken);
        if (link is null || link.ConsentGivenAtUtc is null || link.RevokedAtUtc is not null)
        {
            return Fail("not_found");
        }

        var now = DateTime.UtcNow;
        link.ReconfirmDueAtUtc = PassportPartnerRules.NextReconfirmDue(now);
        link.ReconfirmReminderSentAtUtc = null;
        link.SuspendedAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
        return new PassportLinkMutation(true, null, link.Id);
    }

    public async Task<PassportPartnerViewGrant?> CanPartnerViewAsync(
        Guid partnerUserId,
        Guid candidateId,
        bool mfaSatisfied,
        CancellationToken cancellationToken = default)
    {
        if (!mfaSatisfied || !await FlagOnAsync(cancellationToken))
        {
            return null;
        }

        var viewer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == partnerUserId && u.IsActive, cancellationToken);
        var candidate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == candidateId, cancellationToken);
        if (viewer is null || candidate is null || !candidate.IsActive || IsAnonymized(candidate))
        {
            return null;
        }

        if (viewer.Role is not (UserRole.BranchManager or UserRole.RegionalManager or UserRole.EnterpriseManager or UserRole.Intermediary))
        {
            return null;
        }

        var companyIds = await ViewerCompanyIdsAsync(viewer, cancellationToken);
        if (companyIds.Count == 0)
        {
            return null;
        }

        var links = await _db.PassportPartnerCandidateLinks.AsNoTracking()
            .Include(l => l.PassportPartner)
            .Where(l => l.CandidateUserId == candidateId
                        && l.ConsentGivenAtUtc != null
                        && l.RevokedAtUtc == null
                        && l.SuspendedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var link in links)
        {
            var partner = link.PassportPartner;
            if (partner is not { IsActive: true })
            {
                continue;
            }

            if (companyIds.Contains(partner.CompanyId) || await BranchVisibleAsync(partner.CompanyId, companyIds, cancellationToken))
            {
                return new PassportPartnerViewGrant(link.Id, partner.Id);
            }
        }

        return null;
    }

    public async Task LogAccessAsync(
        Guid candidateId,
        Guid? passportPartnerId,
        Guid? viewerUserId,
        Guid? shareLinkId,
        PassportAccessKind kind,
        CancellationToken cancellationToken = default)
    {
        _db.PassportAccessLogs.Add(new PassportAccessLog
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateId,
            PassportPartnerId = passportPartnerId,
            ViewerUserId = viewerUserId,
            ShareLinkId = shareLinkId,
            Kind = kind,
            OccurredAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public string LookupHash(string normalizedCode)
    {
        var code = ShortCodeFormat.Normalize(normalizedCode);
        var hash = HMACSHA256.HashData(HmacKey(), Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<PassportPartnerCode> CreateCodeAsync(
        Guid partnerId,
        Guid branchCompanyId,
        string? vanityCode,
        CancellationToken cancellationToken = default)
    {
        var partner = await _db.PassportPartners
            .Include(p => p.Company)
            .FirstAsync(p => p.Id == partnerId, cancellationToken);
        var branch = await _db.Companies.FirstAsync(c => c.Id == branchCompanyId, cancellationToken);
        if (!await IsInOrganisationAsync(partner.CompanyId, branch, cancellationToken))
        {
            throw new InvalidOperationException("branch_not_in_organisation");
        }

        var active = await _db.PassportPartnerCodes.CountAsync(
            c => c.PassportPartnerId == partnerId && c.IsActive,
            cancellationToken);
        if (active >= partner.MaxBranches)
        {
            throw new InvalidOperationException("max_branches");
        }

        string plain;
        if (!string.IsNullOrWhiteSpace(vanityCode))
        {
            if (!ShortCodeFormat.IsWellFormed(vanityCode))
            {
                throw new InvalidOperationException("vanity_invalid");
            }

            plain = ShortCodeFormat.Normalize(vanityCode);
        }
        else
        {
            plain = await NextPlainCodeAsync(cancellationToken);
        }

        var hash = LookupHash(plain);
        if (await _db.PassportPartnerCodes.AnyAsync(c => c.CodeLookupHash == hash, cancellationToken))
        {
            throw new InvalidOperationException("code_taken");
        }

        var row = new PassportPartnerCode
        {
            Id = Guid.NewGuid(),
            PassportPartnerId = partnerId,
            BranchCompanyId = branchCompanyId,
            CodeLookupHash = hash,
            CodeDisplay = ShortCodeFormat.Display(plain),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.PassportPartnerCodes.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    private async Task<PassportPartnerCode?> FindActiveCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (!ShortCodeFormat.TryNormalize(code, out var normalized))
        {
            return null;
        }

        var hash = LookupHash(normalized);
        return await _db.PassportPartnerCodes
            .Include(c => c.PassportPartner!)
            .ThenInclude(p => p.Company)
            .Include(c => c.BranchCompany)
            .FirstOrDefaultAsync(c => c.CodeLookupHash == hash && c.IsActive, cancellationToken);
    }

    private async Task<PassportPartnerCandidateLink?> OwnLinkAsync(
        Guid candidateId,
        Guid linkId,
        CancellationToken cancellationToken)
        => await _db.PassportPartnerCandidateLinks
            .FirstOrDefaultAsync(l => l.Id == linkId && l.CandidateUserId == candidateId, cancellationToken);

    private async Task<bool> FlagOnAsync(CancellationToken cancellationToken)
        => (await _features.GetAsync(cancellationToken)).PassportPartnersEnabled;

    private async Task<HashSet<Guid>> ViewerCompanyIdsAsync(User viewer, CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();
        if (viewer.CompanyId is Guid home)
        {
            ids.Add(home);
        }

        var memberships = await _db.UserCompanies.AsNoTracking()
            .Where(m => m.UserId == viewer.Id)
            .Select(m => m.CompanyId)
            .ToListAsync(cancellationToken);
        foreach (var id in memberships)
        {
            ids.Add(id);
        }

        return ids;
    }

    private async Task<bool> BranchVisibleAsync(
        Guid partnerCompanyId,
        HashSet<Guid> viewerCompanyIds,
        CancellationToken cancellationToken)
    {
        if (viewerCompanyIds.Contains(partnerCompanyId))
        {
            return true;
        }

        foreach (var companyId in viewerCompanyIds)
        {
            var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
            if (company is not null && await IsInOrganisationAsync(partnerCompanyId, company, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsInOrganisationAsync(Guid rootId, Company branch, CancellationToken cancellationToken)
    {
        var current = branch;
        for (var hop = 0; hop < 12; hop++)
        {
            if (current.Id == rootId)
            {
                return true;
            }

            if (current.ParentCompanyId is not Guid parentId)
            {
                return false;
            }

            var parent = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            if (parent is null)
            {
                return false;
            }

            current = parent;
        }

        return false;
    }

    private async Task<string> NextPlainCodeAsync(CancellationToken cancellationToken)
    {
        var alphabet = ShortCodeFormat.Alphabet.ToCharArray();
        var used = new HashSet<string>(
            await _db.PassportPartnerCodes.AsNoTracking().Select(c => c.CodeLookupHash).ToListAsync(cancellationToken),
            StringComparer.Ordinal);
        Span<char> chars = stackalloc char[ShortCodeFormat.Length];
        for (var attempt = 0; attempt < 100; attempt++)
        {
            RandomNumberGenerator.GetItems(alphabet, chars);
            var plain = new string(chars);
            var hash = LookupHash(plain);
            if (used.Add(hash))
            {
                return plain;
            }
        }

        throw new InvalidOperationException("code_exhausted");
    }

    private byte[] HmacKey()
    {
        if (_hmacKey is not null)
        {
            return _hmacKey;
        }

        var configured = _configuration["PassportPartners:CodeHmacKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return _hmacKey = Encoding.UTF8.GetBytes(configured.Trim());
        }

        if (_environment.IsProduction())
        {
            throw new InvalidOperationException(
                "PassportPartners:CodeHmacKey ontbreekt. Stel de sleutel in voordat PassportPartnersEnabled aan gaat.");
        }

        var path = Path.Combine(_environment.ContentRootPath, DevKeyFileName);
        if (File.Exists(path))
        {
            return _hmacKey = Convert.FromBase64String(File.ReadAllText(path).Trim());
        }

        var key = RandomNumberGenerator.GetBytes(32);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, Convert.ToBase64String(key));
        return _hmacKey = key;
    }

    private static bool IsAnonymized(User user)
        => user.Email.EndsWith("@anonymized.jobsy.local", StringComparison.OrdinalIgnoreCase)
           || user.Email.EndsWith("@anonymized.local", StringComparison.OrdinalIgnoreCase);

    private static PassportLinkMutation Fail(string error) => new(false, error, null);
}
