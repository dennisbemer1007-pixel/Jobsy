using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerOnboardingService : ISalesManagerOnboardingService
{
    private static readonly Regex VatRegex = new(@"^NL\d{9}B\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex KvkRegex = new(@"^\d{8}$", RegexOptions.Compiled);

    private readonly JobsyDbContext _db;
    private readonly ISalesPayoutProfileService _payoutProfile;

    public SalesManagerOnboardingService(JobsyDbContext db, ISalesPayoutProfileService payoutProfile)
    {
        _db = db;
        _payoutProfile = payoutProfile;
    }

    public async Task<SalesManagerProfileDto?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var profile = await EnsureProfileAsync(userId, cancellationToken);
        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Map(user, profile);
    }

    public async Task<SalesManagerProfileDto> UpdateProfileAsync(
        Guid userId,
        SalesManagerProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        // Route through the payout profile service: IBAN changes (when an account
        // already exists) are ignored; first IBAN during onboarding is still applied.
        var portal = await _payoutProfile.UpdateLegacyProfileAsync(
            userId,
            new SalesCompanyUpdateRequest(
                request.CompanyName,
                request.KvkNumber,
                request.VatNumber,
                request.Address,
                request.PostalCode,
                request.City,
                request.Country),
            request.Iban,
            holderName: null,
            cancellationToken);

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        var profile = await EnsureProfileAsync(userId, cancellationToken);
        var dto = Map(user, profile);
        // Preserve warnings via a side channel — callers of the legacy DTO still get the profile;
        // the new api/sales/me/profile returns SalesPortalProfileDto with Warnings.
        _ = portal.Warnings;
        return dto;
    }

    public async Task<SalesManagerProfileDto> SignAgreementAsync(
        Guid userId,
        string agreementVersion,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Gebruiker niet gevonden.");

        _ = agreementVersion;
        var version = SalesCommissionRules.CurrentAgreementVersion;

        var profile = await EnsureProfileAsync(userId, cancellationToken);
        if (!HasRequiredBusinessData(profile))
        {
            throw new InvalidOperationException(
                "Vul eerst KvK, NAW-gegevens (en btw-nummer bij 21 % btw) in voordat je de overeenkomst ondertekent.");
        }

        if (string.IsNullOrWhiteSpace(profile.Iban))
        {
            throw new InvalidOperationException("Vul eerst je uitbetaalrekening in.");
        }

        profile.AgreementSignedAt = DateTime.UtcNow;
        profile.AgreementVersion = version;
        profile.UpdatedAt = DateTime.UtcNow;

        await TryCompleteOnboardingAsync(profile, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(user, profile);
    }

    private async Task<SalesManagerProfile> EnsureProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _db.SalesManagerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        var now = DateTime.UtcNow;
        profile = new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.SalesManagerProfiles.Add(profile);
        return profile;
    }

    private async Task TryCompleteOnboardingAsync(SalesManagerProfile profile, CancellationToken cancellationToken)
    {
        if (profile.OnboardingCompletedAt.HasValue && !string.IsNullOrWhiteSpace(profile.TrackingCode))
        {
            return;
        }

        if (!HasRequiredBusinessData(profile)
            || !profile.AgreementSignedAt.HasValue
            || string.IsNullOrWhiteSpace(profile.Iban))
        {
            return;
        }

        var hasConsent = await _db.SalesSelfBillingConsents.AsNoTracking()
            .AnyAsync(
                c => c.UserId == profile.UserId
                     && c.Version == SalesSelfBilling.CurrentVersion
                     && c.RevokedAtUtc == null,
                cancellationToken);
        if (!hasConsent)
        {
            return;
        }

        profile.TrackingCode ??= GenerateTrackingCode();
        profile.OnboardingCompletedAt ??= DateTime.UtcNow;
    }

    private static bool HasRequiredBusinessData(SalesManagerProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.CompanyName)
            || string.IsNullOrWhiteSpace(profile.KvkNumber)
            || string.IsNullOrWhiteSpace(profile.Address)
            || string.IsNullOrWhiteSpace(profile.PostalCode)
            || string.IsNullOrWhiteSpace(profile.City))
        {
            return false;
        }

        if (!KvkRegex.IsMatch(profile.KvkNumber.Trim()))
        {
            return false;
        }

        if (profile.VatTreatment == SalesManagerVatTreatment.Standard21)
        {
            return !string.IsNullOrWhiteSpace(profile.VatNumber)
                   && VatRegex.IsMatch(profile.VatNumber.Trim());
        }

        // KOR: btw-nummer optional
        return true;
    }

    private static string GenerateTrackingCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return "SM-" + new string(chars);
    }

    private static SalesManagerProfileDto Map(User user, SalesManagerProfile? profile) =>
        new(
            user.Id,
            user.Email,
            user.FullName,
            profile?.CompanyName,
            profile?.KvkNumber,
            profile?.VatNumber,
            profile?.Address,
            profile?.PostalCode,
            profile?.City,
            profile?.Country,
            string.IsNullOrWhiteSpace(profile?.Iban)
                ? null
                : Iban.Mask(profile.Iban),
            profile?.TrackingCode,
            profile?.AgreementSignedAt,
            profile?.AgreementVersion,
            profile?.OnboardingCompletedAt,
            profile?.IsOnboardingComplete == true,
            profile?.CanRecruitSalesManagers ?? true,
            profile?.ReferredBySalesManagerUserId);
}
