using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesPayoutProfileService : ISalesPayoutProfileService
{
    public static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(30);
    public const int MaxFailedAttempts = 5;
    public const int MaxIbanChangesPer30Days = 3;

    private static readonly Regex VatRegex = new(@"^NL\d{9}B\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex KvkRegex = new(@"^\d{8}$", RegexOptions.Compiled);

    private readonly JobsyDbContext _db;
    private readonly ISalesCommercialService _commercial;
    private readonly IPlatformFeatureService _features;
    private readonly IEmailService _email;
    private readonly ISecretProtector _secrets;
    private readonly IIbanProtector _ibanProtector;

    static SalesPayoutProfileService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SalesPayoutProfileService(
        JobsyDbContext db,
        ISalesCommercialService commercial,
        IPlatformFeatureService features,
        IEmailService email,
        ISecretProtector secrets,
        IIbanProtector ibanProtector)
    {
        _db = db;
        _commercial = commercial;
        _features = features;
        _email = email;
        _secrets = secrets;
        _ibanProtector = ibanProtector;
    }

    public async Task<SalesPortalProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var profile = await EnsureSalesManagerProfileAsync(userId, save: true, cancellationToken);
        return await MapAsync(user, profile, warnings: null, cancellationToken);
    }

    public async Task<SalesPortalProfileDto> UpdateCompanyAsync(
        Guid userId,
        SalesCompanyUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var profile = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);
        ApplyCompany(profile, request, requireVatForStandard: profile.VatTreatment == SalesManagerVatTreatment.Standard21);
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(user, profile, null, cancellationToken);
    }

    public async Task<SalesPortalProfileDto> SetVatTreatmentAsync(
        Guid userId,
        SalesVatUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var profile = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);

        if (request.Treatment is not (SalesManagerVatTreatment.Standard21 or SalesManagerVatTreatment.SmallBusinessScheme))
        {
            throw new ArgumentException("Kies 21 % btw of de KOR.");
        }

        if (request.Treatment == SalesManagerVatTreatment.Standard21)
        {
            var vat = (request.VatNumber ?? profile.VatNumber ?? "").Trim().ToUpperInvariant();
            if (!VatRegex.IsMatch(vat))
            {
                throw new ArgumentException("BTW-nummer moet het formaat NL123456789B01 hebben.");
            }

            profile.VatNumber = vat;
        }
        else
        {
            if (!request.KorConfirmed)
            {
                throw new ArgumentException(
                    "Bevestig dat je bent aangemeld voor de KOR bij de Belastingdienst.");
            }

            if (!string.IsNullOrWhiteSpace(request.VatNumber))
            {
                var vat = request.VatNumber.Trim().ToUpperInvariant();
                if (!VatRegex.IsMatch(vat))
                {
                    throw new ArgumentException("BTW-nummer moet het formaat NL123456789B01 hebben.");
                }

                profile.VatNumber = vat;
            }
        }

        profile.VatTreatment = request.Treatment;
        profile.VatTreatmentChangedAtUtc = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        await RecalculateOpenRequestedAsync(userId, profile.VatTreatment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(user, profile, null, cancellationToken);
    }

    public async Task<SalesIbanChangeBeginResult> BeginIbanChangeAsync(
        Guid userId,
        SalesIbanChangeRequest request,
        string? authMethod,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var profile = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);

        var iban = Iban.Normalize(request.Iban);
        if (!Iban.IsValid(iban))
        {
            throw new ArgumentException("Dit IBAN is ongeldig. Controleer het nummer (mod-97).");
        }

        var holder = (request.HolderName ?? "").Trim();
        if (holder.Length is < 2 or > 70)
        {
            throw new ArgumentException("De naam op de rekening moet 2 tot 70 tekens zijn.");
        }

        // First IBAN (onboarding): no step-up, no hold.
        if (string.IsNullOrWhiteSpace(profile.Iban))
        {
            ApplyIban(profile, iban, holder, hold: false);
            profile.UpdatedAt = DateTime.UtcNow;
            await RefreshOpenRequestMaskedIbanAsync(userId, iban, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            var dto = await MapAsync(user, profile, null, cancellationToken);
            return new SalesIbanChangeBeginResult("none", true, dto, null);
        }

        await EnforceChangeRateLimitAsync(userId, cancellationToken);

        // Drop any previous pending change for this user.
        var stale = await _db.SalesIbanChangePendings
            .Where(p => p.UserId == userId && p.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);
        _db.SalesIbanChangePendings.RemoveRange(stale);

        var external = PersonalDataAccessLogExtensionsCompat.IsExternal(authMethod);
        var method = external ? "email" : "totp";
        var now = DateTime.UtcNow;
        var pending = new SalesIbanChangePending
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedIban = _ibanProtector.Protect(iban) ?? iban,
            HolderName = holder,
            Method = method,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(PendingLifetime)
        };

        string? plaintextToken = null;
        if (method == "email")
        {
            plaintextToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            pending.EmailTokenHash = VerificationCodes.Hash(plaintextToken);
        }
        else if (string.IsNullOrWhiteSpace(user.AuthenticatorSecret) || !user.AuthenticatorEnabled)
        {
            throw new InvalidOperationException(
                "Schakel eerst tweestapsverificatie in om je uitbetaalrekening te wijzigen.");
        }

        _db.SalesIbanChangePendings.Add(pending);
        await _db.SaveChangesAsync(cancellationToken);

        if (method == "email" && plaintextToken is not null)
        {
            await SendConfirmLinkMailAsync(user, plaintextToken, cancellationToken);
        }

        return new SalesIbanChangeBeginResult(
            method,
            Applied: false,
            Profile: null,
            Message: method == "email"
                ? "We hebben een bevestigingslink naar je e-mail gestuurd."
                : "Vul je authenticatorcode in om te bevestigen.");
    }

    public async Task<SalesPortalProfileDto> ConfirmIbanChangeAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var pending = await ActivePendingAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Er is geen openstaande IBAN-wijziging. Start opnieuw.");

        if (!string.Equals(pending.Method, "totp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bevestig via de link in je e-mail.");
        }

        var trimmed = (code ?? "").Trim();
        if (trimmed.Length != 6 || !trimmed.All(char.IsAsciiDigit))
        {
            await RegisterFailedAsync(pending, cancellationToken);
            throw new UnauthorizedAccessException("De authenticatorcode is onjuist.");
        }

        var codeHash = VerificationCodes.Hash(trimmed);
        if (!string.IsNullOrEmpty(pending.LastAcceptedTotpCodeHash)
            && VerificationCodes.FixedTimeEquals(pending.LastAcceptedTotpCodeHash, codeHash))
        {
            throw new UnauthorizedAccessException("Deze code is al gebruikt. Wacht op een nieuwe code.");
        }

        var secret = _secrets.Unprotect(user.AuthenticatorSecret);
        if (!TotpAuthenticator.VerifyCode(secret, trimmed, DateTime.UtcNow))
        {
            await RegisterFailedAsync(pending, cancellationToken);
            throw new UnauthorizedAccessException("De authenticatorcode is onjuist.");
        }

        pending.LastAcceptedTotpCodeHash = codeHash;
        return await ConsumePendingAsync(user, pending, cancellationToken);
    }

    public async Task<SalesPortalProfileDto> ConfirmIbanChangeByEmailTokenAsync(
        string plaintextToken,
        Guid? expectedUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plaintextToken))
        {
            throw new ArgumentException("Ongeldige of verlopen link.");
        }

        var hash = VerificationCodes.Hash(plaintextToken.Trim());
        var now = DateTime.UtcNow;
        var pending = await _db.SalesIbanChangePendings
            .FirstOrDefaultAsync(
                p => p.EmailTokenHash == hash
                     && p.ConsumedAtUtc == null
                     && p.ExpiresAtUtc >= now,
                cancellationToken)
            ?? throw new InvalidOperationException("Ongeldige of verlopen link.");

        if (expectedUserId is Guid uid && pending.UserId != uid)
        {
            throw new UnauthorizedAccessException("Deze bevestigingslink hoort bij een ander account.");
        }

        var user = await RequireUserAsync(pending.UserId, cancellationToken);
        return await ConsumePendingAsync(user, pending, cancellationToken);
    }

    public async Task<SalesPortalProfileDto> GiveConsentAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        _ = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);

        var existing = await _db.SalesSelfBillingConsents
            .Where(c => c.UserId == userId
                        && c.Version == SalesSelfBilling.CurrentVersion
                        && c.RevokedAtUtc == null)
            .OrderByDescending(c => c.AcceptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            _db.SalesSelfBillingConsents.Add(new SalesSelfBillingConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Version = SalesSelfBilling.CurrentVersion,
                TextSha256 = SalesSelfBilling.CurrentTextSha256,
                AcceptedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        var profile = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);
        return await MapAsync(user, profile, null, cancellationToken);
    }

    public async Task<SalesPortalProfileDto> RevokeConsentAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var open = await _db.SalesSelfBillingConsents
            .Where(c => c.UserId == userId && c.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var c in open)
        {
            c.RevokedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var profile = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);
        return await MapAsync(user, profile, null, cancellationToken);
    }

    public async Task<SalesPortalProfileDto> SetEmailPrefsAsync(
        Guid userId,
        SalesEmailPrefs prefs,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var profile = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);
        profile.EmailPrefsJson = (prefs ?? SalesEmailPrefs.Default).ToJson();
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(user, profile, null, cancellationToken);
    }

    public async Task<byte[]> RenderAgreementPdfAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Profiel niet gevonden.");

        var version = profile.AgreementVersion ?? SalesCommissionRules.CurrentAgreementVersion;
        var signed = profile.AgreementSignedAt;
        var body =
            "Bemiddelingsovereenkomst Lobsy Partner (salesmanager).\n\n"
            + $"Versie: {version}\n"
            + (signed is DateTime s
                ? $"Ondertekend op: {SalesClock.ToLocal(s):dd-MM-yyyy HH:mm} (Europe/Amsterdam)\n\n"
                : "Nog niet ondertekend.\n\n")
            + "Deze overeenkomst regelt de bemiddeling van werkgevers via jouw persoonlijke link of code, "
            + "de commissiestructuur (25 % · 10 % · 5 % over 3 jaar per werkgever, of 20 % in jaar 1 bij aanbeveling) "
            + "en de samenwerking met Lobsy. Self-billing staat apart vastgelegd.";

        return RenderSimplePdf("Bemiddelingsovereenkomst", body);
    }

    public async Task<byte[]> RenderConsentPdfAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var consent = await _db.SalesSelfBillingConsents.AsNoTracking()
            .Where(c => c.UserId == userId && c.RevokedAtUtc == null)
            .OrderByDescending(c => c.AcceptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var body = SalesSelfBilling.ConsentText + "\n\n"
            + (consent is null
                ? "Nog geen toestemming gegeven."
                : $"Versie: {consent.Version}\nAkkoord op: {SalesClock.ToLocal(consent.AcceptedAtUtc):dd-MM-yyyy HH:mm} (Europe/Amsterdam)\nSHA-256: {consent.TextSha256}");

        return RenderSimplePdf("Self-billing toestemming", body);
    }

    public async Task<SalesPortalProfileDto> UpdateLegacyProfileAsync(
        Guid userId,
        SalesCompanyUpdateRequest company,
        string? iban,
        string? holderName,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var dto = await UpdateCompanyAsync(userId, company, cancellationToken);

        var profile = await EnsureSalesManagerProfileAsync(userId, save: false, cancellationToken);
        var incoming = Iban.Normalize(iban);
        if (!string.IsNullOrWhiteSpace(incoming) && !incoming.Contains('*', StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(profile.Iban))
            {
                // First IBAN via legacy path still allowed (onboarding clients).
                if (!Iban.IsValid(incoming))
                {
                    throw new ArgumentException("Dit IBAN is ongeldig. Controleer het nummer (mod-97).");
                }

                var holder = string.IsNullOrWhiteSpace(holderName)
                    ? (await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken)).FullName
                    : holderName.Trim();
                ApplyIban(profile, incoming, holder, hold: false);
                profile.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                var user = await RequireUserAsync(userId, cancellationToken);
                return await MapAsync(user, profile, warnings, cancellationToken);
            }

            warnings.Add(
                "IBAN is genegeerd. Wijzig je uitbetaalrekening via de beveiligde IBAN-stappen (2FA).");
        }

        var user2 = await RequireUserAsync(userId, cancellationToken);
        return await MapAsync(user2, profile, warnings, cancellationToken);
    }

    private async Task<SalesPortalProfileDto> ConsumePendingAsync(
        User user,
        SalesIbanChangePending pending,
        CancellationToken cancellationToken)
    {
        var profile = await EnsureSalesManagerProfileAsync(user.Id, save: false, cancellationToken);
        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var holdDays = Math.Clamp(settings.IbanChangeHoldDays, 0, 14);
        if (holdDays <= 0)
        {
            holdDays = 3;
        }

        var plaintext = _ibanProtector.Unprotect(pending.EncryptedIban)
                        ?? throw new InvalidOperationException("Opgeslagen IBAN kon niet worden gelezen.");
        var oldLast4 = Iban.Last4(profile.Iban);
        var newLast4 = Iban.Last4(plaintext);

        ApplyIban(profile, plaintext, pending.HolderName, hold: true, holdDays);
        pending.ConsumedAtUtc = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        await RefreshOpenRequestMaskedIbanAsync(user.Id, plaintext, cancellationToken);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "sales.iban.changed",
            Message = $"user={user.Id:D}; oldLast4={oldLast4}; newLast4={newLast4}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await SendIbanChangedMailAsync(user, Iban.Mask(plaintext), cancellationToken);
        return await MapAsync(user, profile, null, cancellationToken);
    }

    private async Task RegisterFailedAsync(SalesIbanChangePending pending, CancellationToken cancellationToken)
    {
        pending.FailedAttempts++;
        if (pending.FailedAttempts >= MaxFailedAttempts)
        {
            _db.SalesIbanChangePendings.Remove(pending);
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (pending.FailedAttempts >= MaxFailedAttempts)
        {
            throw new InvalidOperationException(
                "Te veel onjuiste codes. Start de IBAN-wijziging opnieuw.");
        }
    }

    private async Task EnforceChangeRateLimitAsync(Guid userId, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var needle = userId.ToString("D");
        var logCount = await _db.PlatformLogs.AsNoTracking()
            .CountAsync(
                l => l.Category == "sales.iban.changed"
                     && l.CreatedAt >= since
                     && l.Message.Contains(needle),
                cancellationToken);

        if (logCount >= MaxIbanChangesPer30Days)
        {
            throw new InvalidOperationException(
                "Je mag je uitbetaalrekening maximaal 3 keer per 30 dagen wijzigen. Probeer het later opnieuw.");
        }
    }

    private async Task<SalesIbanChangePending?> ActivePendingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await _db.SalesIbanChangePendings
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.ConsumedAtUtc == null && p.ExpiresAtUtc >= now,
                cancellationToken);
    }

    private static void ApplyIban(
        SalesManagerProfile profile,
        string iban,
        string holder,
        bool hold,
        int holdDays = 3)
    {
        var now = DateTime.UtcNow;
        profile.Iban = iban;
        profile.PayoutAccountHolderName = holder;
        profile.IbanChangedAtUtc = now;
        if (hold)
        {
            var localToday = SalesClock.Today(now);
            profile.IbanPayoutHoldUntilUtc = SalesClock.EndOfLocalDayUtc(localToday.AddDays(holdDays));
        }
        else
        {
            profile.IbanPayoutHoldUntilUtc = null;
        }
    }

    private async Task RefreshOpenRequestMaskedIbanAsync(
        Guid userId,
        string iban,
        CancellationToken cancellationToken)
    {
        var open = await _db.SalesPayoutRequests
            .Where(r => r.BeneficiaryUserId == userId && r.Status == SalesPayoutRequestStatus.Requested)
            .ToListAsync(cancellationToken);
        var masked = Iban.Mask(iban);
        foreach (var r in open)
        {
            r.MaskedIban = masked.Length > 34 ? masked[..34] : masked;
        }
    }

    private async Task RecalculateOpenRequestedAsync(
        Guid userId,
        SalesManagerVatTreatment treatment,
        CancellationToken cancellationToken)
    {
        var open = await _db.SalesPayoutRequests
            .Where(r => r.BeneficiaryUserId == userId && r.Status == SalesPayoutRequestStatus.Requested)
            .ToListAsync(cancellationToken);
        foreach (var r in open)
        {
            r.VatTreatment = treatment;
            r.VatAmount = treatment == SalesManagerVatTreatment.Standard21
                ? SalesCommissionRules.VatOn(r.AmountExVat)
                : 0m;
            r.TotalInclVat = r.AmountExVat + r.VatAmount;
        }
    }

    private static void ApplyCompany(
        SalesManagerProfile profile,
        SalesCompanyUpdateRequest request,
        bool requireVatForStandard)
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
            throw new ArgumentException("KvK-nummer moet 8 cijfers zijn. Lobsy werkt samen met ondernemers.");
        }

        profile.CompanyName = request.CompanyName.Trim();
        profile.KvkNumber = kvk;
        profile.Address = request.Address.Trim();
        profile.PostalCode = request.PostalCode.Trim();
        profile.City = request.City.Trim();
        profile.Country = string.IsNullOrWhiteSpace(request.Country) ? "NL" : request.Country.Trim();

        if (!string.IsNullOrWhiteSpace(request.VatNumber))
        {
            var vat = request.VatNumber.Trim().ToUpperInvariant();
            if (!VatRegex.IsMatch(vat))
            {
                throw new ArgumentException("BTW-nummer moet het formaat NL123456789B01 hebben.");
            }

            profile.VatNumber = vat;
        }
        else if (requireVatForStandard)
        {
            throw new ArgumentException("BTW-nummer is verplicht bij 21 % btw.");
        }
    }

    private async Task<SalesManagerProfile> EnsureSalesManagerProfileAsync(
        Guid userId,
        bool save,
        CancellationToken cancellationToken)
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
        if (save)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return profile;
    }

    private async Task<User> RequireUserAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
           ?? throw new KeyNotFoundException("Gebruiker niet gevonden.");

    private async Task<SalesPortalProfileDto> MapAsync(
        User user,
        SalesManagerProfile profile,
        IReadOnlyList<string>? warnings,
        CancellationToken cancellationToken)
    {
        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var consent = await _db.SalesSelfBillingConsents.AsNoTracking()
            .Where(c => c.UserId == user.Id
                        && c.Version == SalesSelfBilling.CurrentVersion
                        && c.RevokedAtUtc == null)
            .OrderByDescending(c => c.AcceptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new SalesPortalProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CompanyName = profile.CompanyName,
            KvkNumber = profile.KvkNumber,
            VatNumber = profile.VatNumber,
            Address = profile.Address,
            PostalCode = profile.PostalCode,
            City = profile.City,
            Country = profile.Country,
            VatTreatment = profile.VatTreatment.ToString(),
            VatTreatmentChangedAtUtc = profile.VatTreatmentChangedAtUtc,
            MaskedIban = string.IsNullOrWhiteSpace(profile.Iban) ? null : Iban.Mask(profile.Iban),
            PayoutAccountHolderName = profile.PayoutAccountHolderName,
            IbanChangedAtUtc = profile.IbanChangedAtUtc,
            IbanPayoutHoldUntilUtc = profile.IbanPayoutHoldUntilUtc,
            TrackingCode = profile.TrackingCode,
            AgreementSignedAt = profile.AgreementSignedAt,
            AgreementVersion = profile.AgreementVersion,
            OnboardingCompletedAt = profile.OnboardingCompletedAt,
            IsOnboardingComplete = profile.IsOnboardingComplete,
            CanRecruitSalesManagers = profile.CanRecruitSalesManagers,
            ReferredBySalesManagerUserId = profile.ReferredBySalesManagerUserId,
            HasSelfBillingConsent = consent is not null,
            SelfBillingConsentAtUtc = consent?.AcceptedAtUtc,
            SelfBillingConsentVersion = consent?.Version,
            EmailPrefs = SalesEmailPrefs.Parse(profile.EmailPrefsJson),
            MfaEnabled = user.AuthenticatorEnabled,
            IbanChangeHoldDays = settings.IbanChangeHoldDays <= 0 ? 3 : settings.IbanChangeHoldDays,
            Warnings = warnings ?? []
        };
    }

    private async Task SendIbanChangedMailAsync(User user, string masked, CancellationToken cancellationToken)
    {
        var when = SalesClock.ToLocal(DateTime.UtcNow).ToString("dd-MM-yyyy HH:mm");
        var subject = "Je uitbetaalrekening is gewijzigd";
        var html =
            $"<p>Hallo {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>"
            + $"<p>Je uitbetaalrekening is gewijzigd naar <strong>{System.Net.WebUtility.HtmlEncode(masked)}</strong> "
            + $"(op {when} Europe/Amsterdam).</p>"
            + "<p>Was jij dit niet? Neem direct contact op met Lobsy.</p>";
        await _email.SendAsync(new EmailMessage(user.Email, subject, html, "SalesMail.IbanChanged"), cancellationToken);
    }

    private async Task SendConfirmLinkMailAsync(User user, string token, CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');
        var link = $"{baseUrl}/sales/profiel/iban-bevestigen?token={Uri.EscapeDataString(token)}";
        var subject = "Bevestig je nieuwe uitbetaalrekening";
        var html =
            $"<p>Hallo {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>"
            + "<p>Bevestig je nieuwe uitbetaalrekening via deze link (30 minuten geldig):</p>"
            + $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">{System.Net.WebUtility.HtmlEncode(link)}</a></p>"
            + "<p>Heb je dit niet aangevraagd? Negeer deze mail of neem contact op met Lobsy.</p>";
        await _email.SendAsync(new EmailMessage(user.Email, subject, html, "SalesMail.IbanConfirm"), cancellationToken);
    }

    private static byte[] RenderSimplePdf(string title, string body)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));
                page.Header().Text(title).SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);
                page.Content().PaddingTop(16).Text(body).LineHeight(1.4f);
                page.Footer().AlignCenter().Text("Lobsy Partner").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }
}

/// <summary>Avoid referencing Api Privacy helpers from Infrastructure; mirror the external check.</summary>
file static class PersonalDataAccessLogExtensionsCompat
{
    public static bool IsExternal(string? authMethod)
        => !string.IsNullOrWhiteSpace(authMethod)
           && authMethod.StartsWith("external", StringComparison.OrdinalIgnoreCase);
}
