using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AmbassadeurOnboardingService : IAmbassadeurOnboardingService
{
    private static readonly Regex VatRegex = new(@"^NL\d{9}B\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex KvkRegex = new(@"^\d{8}$", RegexOptions.Compiled);

    private readonly JobsyDbContext _db;
    private readonly IAmbassadeurSettingsService _settings;

    public AmbassadeurOnboardingService(JobsyDbContext db, IAmbassadeurSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<AmbassadeurProfileDto?> GetProfileAsync(
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

        return await MapAsync(user, profile, cancellationToken);
    }

    public async Task<AmbassadeurProfileDto> UpdateProfileAsync(
        Guid userId,
        AmbassadeurProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Gebruiker niet gevonden.");

        ValidateBusinessFields(request);

        var profile = await EnsureProfileAsync(userId, cancellationToken);
        profile.CompanyName = request.CompanyName.Trim();
        profile.KvkNumber = request.KvkNumber.Trim();
        profile.VatNumber = request.VatNumber.Trim().ToUpperInvariant();
        profile.Address = request.Address.Trim();
        profile.PostalCode = request.PostalCode.Trim();
        profile.City = request.City.Trim();
        profile.Country = string.IsNullOrWhiteSpace(request.Country) ? "NL" : request.Country.Trim();
        profile.Iban = ResolveIbanUpdate(profile.Iban, request.Iban);
        profile.UpdatedAt = DateTime.UtcNow;

        TryCompleteOnboarding(profile);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(user, profile, cancellationToken);
    }

    public async Task<AmbassadeurProfileDto> SignAgreementAsync(
        Guid userId,
        string agreementVersion,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Gebruiker niet gevonden.");

        _ = agreementVersion;
        var version = AmbassadeurCommissionRules.CurrentAgreementVersion;

        var profile = await EnsureProfileAsync(userId, cancellationToken);
        if (!HasRequiredBusinessData(profile))
        {
            throw new InvalidOperationException(
                "Vul eerst KvK, BTW-nummer en NAW-gegevens in voordat je de overeenkomst ondertekent.");
        }

        profile.AgreementSignedAt = DateTime.UtcNow;
        profile.AgreementVersion = version;
        profile.UpdatedAt = DateTime.UtcNow;

        TryCompleteOnboarding(profile);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(user, profile, cancellationToken);
    }

    private async Task<AmbassadeurProfile> EnsureProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _db.AmbassadeurProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        var now = DateTime.UtcNow;
        profile = new AmbassadeurProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BaseCommissionPercentage = AmbassadeurCommissionRules.DefaultBaseCommissionPercentage,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.AmbassadeurProfiles.Add(profile);
        return profile;
    }

    private static void TryCompleteOnboarding(AmbassadeurProfile profile)
    {
        if (profile.OnboardingCompletedAt.HasValue && !string.IsNullOrWhiteSpace(profile.TrackingCode))
        {
            return;
        }

        if (!HasRequiredBusinessData(profile) || !profile.AgreementSignedAt.HasValue)
        {
            return;
        }

        profile.TrackingCode ??= GenerateTrackingCode();
        profile.OnboardingCompletedAt ??= DateTime.UtcNow;
    }

    private static bool HasRequiredBusinessData(AmbassadeurProfile profile) =>
        !string.IsNullOrWhiteSpace(profile.CompanyName)
        && !string.IsNullOrWhiteSpace(profile.KvkNumber)
        && !string.IsNullOrWhiteSpace(profile.VatNumber)
        && !string.IsNullOrWhiteSpace(profile.Address)
        && !string.IsNullOrWhiteSpace(profile.PostalCode)
        && !string.IsNullOrWhiteSpace(profile.City);

    private static void ValidateBusinessFields(AmbassadeurProfileUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName)
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.PostalCode)
            || string.IsNullOrWhiteSpace(request.City))
        {
            throw new ArgumentException("Bedrijfsnaam en NAW-gegevens zijn verplicht.");
        }

        var kvk = request.KvkNumber.Trim();
        if (!KvkRegex.IsMatch(kvk))
        {
            throw new ArgumentException("KvK-nummer moet 8 cijfers zijn.");
        }

        var vat = request.VatNumber.Trim().ToUpperInvariant();
        if (!VatRegex.IsMatch(vat))
        {
            throw new ArgumentException("BTW-nummer moet het formaat NL123456789B01 hebben.");
        }
    }

    private static string? ResolveIbanUpdate(string? current, string? incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming))
        {
            return current;
        }

        var compact = incoming.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (compact.Contains('*', StringComparison.Ordinal))
        {
            return current;
        }

        return compact.ToUpperInvariant();
    }

    private static string GenerateTrackingCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return AmbassadeurCommissionRules.TrackingCodePrefix + new string(chars);
    }

    private async Task<AmbassadeurProfileDto> MapAsync(
        User user,
        AmbassadeurProfile profile,
        CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        var candidateCount = await _db.Users.AsNoTracking()
            .CountAsync(u => u.ReferredByAmbassadeurUserId == user.Id, cancellationToken);
        var current = AmbassadeurCommissionRules.ResolveCurrentPercentage(
            candidateCount,
            profile.BaseCommissionPercentage,
            settings.CandidateThreshold,
            settings.PercentPerThreshold,
            settings.MaxCommissionPercentage,
            profile.CommissionPercentageOverride);

        return new AmbassadeurProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            profile.CompanyName,
            profile.KvkNumber,
            profile.VatNumber,
            profile.Address,
            profile.PostalCode,
            profile.City,
            profile.Country,
            string.IsNullOrWhiteSpace(profile.Iban)
                ? null
                : ISalesManagerPayoutService.MaskIban(profile.Iban),
            profile.TrackingCode,
            profile.BaseCommissionPercentage,
            current,
            settings.MaxCommissionPercentage,
            profile.CommissionPercentageOverride,
            profile.AgreementSignedAt,
            profile.AgreementVersion,
            profile.OnboardingCompletedAt,
            profile.IsOnboardingComplete);
    }
}
