using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyRegistrationService : ICompanyRegistrationService
{
    /// <summary>Confirmation OTP / pending registration window (10 minutes).</summary>
    public static readonly TimeSpan ActivationTokenTtl =
        TimeSpan.FromMinutes(PrivacyConstants.UnconfirmedRegistrationRetentionMinutes);

    /// <summary>Ledger note for the one-time registration welcome grant (1 token).</summary>
    public const string WelcomeTokenNote = "Welkomsttoken toegekend bij bedrijfsverificatie";

    public const decimal WelcomeTokenAmount = 1m;

    private readonly JobsyDbContext _db;
    private readonly IKvkService _kvk;
    private readonly ITransactionalMailer _mailer;
    private readonly ITokenLedgerService _ledger;
    private readonly IPlatformFeatureService _features;
    private readonly IPartnerAffiliateService _partnerAffiliates;
    private readonly IRegistrationReferralResolver _referralResolver;
    private readonly IGeocodingService? _geocoder;
    private readonly ILenderRegistrationCheck? _lenderRegistration;
    private readonly IOneTimeLinkService _links;
    private readonly ILogger<CompanyRegistrationService> _logger;

    public CompanyRegistrationService(
        JobsyDbContext db,
        IKvkService kvk,
        ITransactionalMailer mailer,
        ITokenLedgerService ledger,
        IPlatformFeatureService features,
        ILogger<CompanyRegistrationService> logger)
        : this(
            db,
            kvk,
            mailer,
            ledger,
            features,
            new PartnerAffiliateService(
                db,
                ledger,
                features),
            null,
            null,
            null,
            logger)
    {
    }

    public CompanyRegistrationService(
        JobsyDbContext db,
        IKvkService kvk,
        ITransactionalMailer mailer,
        ITokenLedgerService ledger,
        IPlatformFeatureService features,
        IPartnerAffiliateService partnerAffiliates,
        ILogger<CompanyRegistrationService> logger)
        : this(db, kvk, mailer, ledger, features, partnerAffiliates, null, null, null, logger)
    {
    }

    public CompanyRegistrationService(
        JobsyDbContext db,
        IKvkService kvk,
        ITransactionalMailer mailer,
        ITokenLedgerService ledger,
        IPlatformFeatureService features,
        IPartnerAffiliateService partnerAffiliates,
        IRegistrationReferralResolver? referralResolver,
        IGeocodingService? geocoder,
        ILogger<CompanyRegistrationService> logger)
        : this(db, kvk, mailer, ledger, features, partnerAffiliates, referralResolver, geocoder, null, logger)
    {
    }

    public CompanyRegistrationService(
        JobsyDbContext db,
        IKvkService kvk,
        ITransactionalMailer mailer,
        ITokenLedgerService ledger,
        IPlatformFeatureService features,
        IPartnerAffiliateService partnerAffiliates,
        IRegistrationReferralResolver? referralResolver,
        IGeocodingService? geocoder,
        ILenderRegistrationCheck? lenderRegistration,
        ILogger<CompanyRegistrationService> logger)
    {
        _db = db;
        _kvk = kvk;
        _mailer = mailer;
        _ledger = ledger;
        _features = features;
        _partnerAffiliates = partnerAffiliates;
        _referralResolver = referralResolver
            ?? new DefaultRegistrationReferralResolver(db, partnerAffiliates);
        _geocoder = geocoder;
        _lenderRegistration = lenderRegistration;
        _links = new OneTimeLinkService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<OneTimeLinkService>.Instance);
        _logger = logger;
    }

    public async Task<RegistrationSubmitResult> SubmitAsync(
        RegistrationSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        var kvkNumber = request.KvkNumber.Trim();
        var establishmentId = request.KvkEstablishmentId.Trim();
        var email = request.ContactEmail.Trim().ToLowerInvariant();
        var name = request.ContactName.Trim();

        if (string.IsNullOrWhiteSpace(kvkNumber)
            || string.IsNullOrWhiteSpace(establishmentId)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("KVK, vestiging, naam en e-mail zijn verplicht.");
        }

        if (!request.AcceptedTerms)
        {
            throw new ArgumentException("Je moet akkoord gaan met de voorwaarden en privacyverklaring.");
        }

        var externalLogin = RegistrationPasswordRules.IsExternalLoginProvider(request.PreferredLoginProvider);
        RegistrationPasswordRules.Validate(request.Password, required: !externalLogin);

        string? trackingCode = null;
        string? partnerTrackingCode = null;
        Guid? salesManagerUserId = request.SalesManagerUserId;

        // Soft referral: unknown codes never block submit (UI shows inline message).
        SalesAttributionSource? attributionSource = null;
        var referral = await _referralResolver.ResolveAsync(
            request.SalesManagerTrackingCode ?? request.PartnerTrackingCode,
            linkCode: request.CookieTrackingCode,
            cancellationToken);
        if (referral.IsKnown && !string.IsNullOrWhiteSpace(referral.Code))
        {
            if (referral.IsPartnerCode)
            {
                partnerTrackingCode = referral.Code;
            }
            else
            {
                trackingCode = referral.Code;
                salesManagerUserId ??= referral.SalesManagerUserId;
                attributionSource = referral.Source switch
                {
                    RegistrationReferralSource.TypedCode => SalesAttributionSource.TypedCode,
                    RegistrationReferralSource.LinkCode => SalesAttributionSource.LinkCookie,
                    RegistrationReferralSource.Cookie => SalesAttributionSource.LinkCookie,
                    _ => null
                };
            }
        }
        // Unknown codes are ignored (never block submit; never store unverified text).
        _ = request.PartnerTrackingCode;

        var lookup = await _kvk.LookupEstablishmentsAsync(kvkNumber, cancellationToken);
        KvkEstablishmentResult match;
        var kvkVerificationStatus = KvkVerificationStatus.Verified;
        IReadOnlyList<string> sbiCodes;

        if (lookup.Status == KvkLookupStatus.Unavailable)
        {
            if (!request.AllowPendingKvkVerification)
            {
                throw new InvalidOperationException(
                    lookup.Message
                    ?? "KVK-dienst is tijdelijk niet beschikbaar. Probeer later opnieuw of kies doorgaan met verificatie in afwachting.");
            }

            // Never trust client-declared SBI/intermediary during an outage — always Employer
            // until the retry job confirms SBI 78* from KVK.
            request = await GeocodeManualRequestAsync(request, cancellationToken);
            match = BuildPendingEstablishmentSnapshot(request, kvkNumber);
            kvkVerificationStatus = KvkVerificationStatus.Pending;
            sbiCodes = [];
        }
        else if (lookup.Status == KvkLookupStatus.NotFound)
        {
            throw new KeyNotFoundException("Vestiging niet gevonden in KVK.");
        }
        else
        {
            var found = lookup.Establishments.FirstOrDefault(e =>
                e.KvkEstablishmentId.Equals(establishmentId, StringComparison.OrdinalIgnoreCase));
            if (found is null)
            {
                throw new KeyNotFoundException("Vestiging niet gevonden in KVK.");
            }

            match = found;
            var kvkCompany = await _kvk.GetByKvkNumberAsync(kvkNumber, cancellationToken);
            sbiCodes = kvkCompany?.EffectiveSbiCodes.Count > 0
                ? kvkCompany.EffectiveSbiCodes
                : match.EffectiveSbiCodes;
        }

        var isIntermediarySbi = ResolveIsIntermediarySbi(sbiCodes, request.ManualIsIntermediarySbi);
        var primarySbi = KvkSbiClassification.PrimarySbiCode(sbiCodes);

        // Soft enumeration: never show a red conflict for known e-mails.
        // Pending same address → resend OTP; known account → decoy success (no mail).
        var pendingSameEmail = await _db.CompanyRegistrations
            .Where(r => r.ContactEmail == email
                        && (r.Status == CompanyRegistrationStatus.PendingActivation
                            || r.Status == CompanyRegistrationStatus.TakeoverPending))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingSameEmail is not null)
        {
            return await ResendConfirmationCodeAsync(pendingSameEmail, cancellationToken);
        }

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken)
            || await _db.LocalAuthCredentials.AnyAsync(c => c.Email == email, cancellationToken))
        {
            return new RegistrationSubmitResult(
                Guid.NewGuid(),
                CompanyRegistrationStatus.PendingActivation,
                RequiresTakeover: false,
                Message:
                "We hebben een bevestigingscode gestuurd als dit e-mailadres nog niet bekend is. Bevestig binnen 10 minuten.",
                ActivationUrl: null,
                VerificationExpiresAt: DateTime.UtcNow.Add(ActivationTokenTtl));
        }

        var existing = await _db.Companies
            .FirstOrDefaultAsync(c => c.KvkEstablishmentId == match.KvkEstablishmentId, cancellationToken);

        var pendingSameEstablishment = await _db.CompanyRegistrations.AnyAsync(
            r => r.KvkEstablishmentId == match.KvkEstablishmentId
                 && (r.Status == CompanyRegistrationStatus.PendingActivation
                     || r.Status == CompanyRegistrationStatus.TakeoverPending),
            cancellationToken);

        if (existing is null && pendingSameEstablishment)
        {
            throw new InvalidOperationException(
                "Er loopt al een openstaande registratie voor deze vestiging.");
        }

        // Intermediairs (SBI 78*) provision a single Intermediary company (no employer org tree).
        // Employers keep the chosen scope: Organization = org tree; BranchOnly = vestiging-as-company.
        // Both employer scopes get Bedrijfsmanager (can invite vestigingsmanagers).
        var scope = isIntermediarySbi ? RegistrationScope.BranchOnly : request.Scope;

        var registration = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            KvkNumber = match.KvkNumber,
            KvkEstablishmentId = match.KvkEstablishmentId,
            EstablishmentName = match.Name,
            EstablishmentAddress = match.Address,
            Latitude = match.Latitude,
            Longitude = match.Longitude,
            Scope = scope,
            ContactName = name,
            ContactEmail = email,
            ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone)
                ? null
                : request.ContactPhone.Trim(),
            ActivationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            PasswordHash = externalLogin
                ? JobsyPasswordHasher.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)))
                : JobsyPasswordHasher.Hash(request.Password!),
            PrimarySbiCode = primarySbi,
            IsIntermediarySbi = isIntermediarySbi,
            KvkVerificationStatus = kvkVerificationStatus,
            ConsentAcceptedAt = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            SalesManagerTrackingCode = trackingCode,
            SalesManagerUserId = salesManagerUserId,
            PartnerTrackingCode = partnerTrackingCode,
            SalesAttributionSource = attributionSource,
            SelectedEstablishmentIdsJson = SerializeSelectedIds(request.SelectedEstablishmentIds),
            RepresentationConsentAtUtc = request.RepresentationConsentAtUtc,
            RepresentationConsentVersion = string.IsNullOrWhiteSpace(request.RepresentationConsentVersion)
                ? null
                : request.RepresentationConsentVersion.Trim(),
            PreferredLoginProvider = string.IsNullOrWhiteSpace(request.PreferredLoginProvider)
                ? null
                : request.PreferredLoginProvider.Trim().ToLowerInvariant(),
            LocationUnknown = request.LocationUnknown
                || (kvkVerificationStatus == KvkVerificationStatus.Pending
                    && request.ManualLatitude is null
                    && request.ManualLongitude is null),
            CreatedAt = DateTime.UtcNow
        };

        var plaintextCode = AssignConfirmationCode(registration);

        var isManaged = existing is not null
            && await CompanyOccupancy.HasActiveManagingEmployersAsync(_db, existing.Id, cancellationToken);

        if (isManaged)
        {
            // Ownership transfer (07.5): letter + admin — not a peer takeover the BM can approve alone.
            registration.Status = CompanyRegistrationStatus.TakeoverPending;
            var takeover = new EstablishmentTakeoverRequest
            {
                Id = Guid.NewGuid(),
                RegistrationId = registration.Id,
                TargetCompanyId = existing!.Id,
                Status = TakeoverRequestStatus.Pending,
                Kind = TakeoverRequestKind.OwnershipTransfer,
                CreatedAt = DateTime.UtcNow
            };

            _db.CompanyRegistrations.Add(registration);
            _db.EstablishmentTakeoverRequests.Add(takeover);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                await SendTakeoverEmailVerificationAsync(registration, existing, plaintextCode, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await DeleteRegistrationCascadeAsync(registration.Id, cancellationToken);
                _logger.LogError(ex, "Ownership-transfer confirmation e-mail failed for {Id}", registration.Id);
                throw new InvalidOperationException(
                    "Kon de bevestigingsmail niet versturen. Controleer de e-mailinstellingen of probeer later opnieuw.");
            }

            return new RegistrationSubmitResult(
                registration.Id,
                registration.Status,
                RequiresTakeover: true,
                Message:
                "Deze vestiging heeft al een beheerder. Bevestig je e-mail; daarna sturen we een brief met code naar het KvK-adres. Lobsy-support keurt de eigendomsoverdracht goed.",
                ActivationUrl: null,
                VerificationExpiresAt: registration.EmailVerificationExpiresAt);
        }

        // Claim (07.6): company row exists (e.g. intermediair client) but no active managing employers —
        // treat as free; activation attaches the new owner without cutting the intermediary.
        if (existing is not null && match.IsInUse)
        {
            // Defensive: IsInUse should already be false when unmanaged; never invent a second shell.
            throw new InvalidOperationException(
                "Vestiging staat als in-gebruik gemarkeerd maar heeft geen actieve beheerder. Vernieuw de pagina.");
        }

        registration.Status = CompanyRegistrationStatus.PendingActivation;
        _db.CompanyRegistrations.Add(registration);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await SendActivationEmailAsync(registration, plaintextCode, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DeleteRegistrationCascadeAsync(registration.Id, cancellationToken);
            _logger.LogError(ex, "Registration confirmation e-mail failed for {Id}", registration.Id);
            throw new InvalidOperationException(
                "Kon de bevestigingsmail niet versturen. Controleer de e-mailinstellingen of probeer later opnieuw.");
        }

        var roleHint = isIntermediarySbi
            ? "Na bevestiging krijg je de rol Intermediair (SBI 78) en kun je direct aan de slag."
            : scope == RegistrationScope.Organization
                ? "Na bevestiging krijg je de rol Bedrijfsmanager — je eerste token is gratis."
                : "Na bevestiging krijg je de rol Filiaalmanager — je eerste token is gratis.";

        var kvkHint = kvkVerificationStatus == KvkVerificationStatus.Pending
            ? " Je account staat intern op KVK-verificatie in afwachting; we controleren dit automatisch zodra de KVK-dienst weer bereikbaar is."
            : string.Empty;

        return new RegistrationSubmitResult(
            registration.Id,
            registration.Status,
            RequiresTakeover: false,
            Message: $"We hebben een bevestigingscode naar je e-mail gestuurd. Vul die hieronder in (geldig 10 minuten). {roleHint}{kvkHint}",
            ActivationUrl: null,
            VerificationExpiresAt: registration.EmailVerificationExpiresAt);
    }

    public async Task<RegistrationActivationResult> ConfirmAsync(
        Guid registrationId,
        string verificationCode,
        CancellationToken cancellationToken = default)
    {
        if (registrationId == Guid.Empty)
        {
            throw new ArgumentException("Registratie ontbreekt.");
        }

        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            throw new ArgumentException("Bevestigingscode ontbreekt.");
        }

        var registration = await _db.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);

        if (registration is null)
        {
            throw new KeyNotFoundException(
                "Ongeldige of verlopen bevestigingscode. Registreer opnieuw.");
        }

        if (registration.Status == CompanyRegistrationStatus.Activated
            || registration.Status == CompanyRegistrationStatus.TakeoverApproved)
        {
            throw new InvalidOperationException("Deze registratie is al bevestigd.");
        }

        if (IsConfirmationExpired(registration))
        {
            await DeleteRegistrationCascadeAsync(registration.Id, cancellationToken);
            throw new InvalidOperationException(
                "De bevestigingstermijn is verlopen. De aanvraag is verwijderd — registreer opnieuw.");
        }

        if (string.IsNullOrWhiteSpace(registration.EmailVerificationCode))
        {
            throw new InvalidOperationException(
                "Geen openstaande bevestigingscode. Registreer opnieuw.");
        }

        if (registration.EmailVerificationFailedAttempts >= VerificationCodes.MaxFailedAttempts)
        {
            await DeleteRegistrationCascadeAsync(registration.Id, cancellationToken);
            throw new InvalidOperationException(
                "Te veel onjuiste pogingen. De aanvraag is verwijderd — registreer opnieuw.");
        }

        if (!VerificationCodes.MatchesHash(registration.EmailVerificationCode, verificationCode.Trim()))
        {
            var attempts = registration.EmailVerificationFailedAttempts;
            var lockedOut = VerificationCodes.RegisterFailedAttempt(ref attempts);
            registration.EmailVerificationFailedAttempts = attempts;
            if (lockedOut)
            {
                await DeleteRegistrationCascadeAsync(registration.Id, cancellationToken);
                throw new InvalidOperationException(
                    "Te veel onjuiste pogingen. De aanvraag is verwijderd — registreer opnieuw.");
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Onjuiste bevestigingscode. Probeer opnieuw.");
        }

        return await CompleteConfirmationAsync(registration, cancellationToken);
    }

    public async Task<RegistrationActivationResult> ActivateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Activatie-token ontbreekt.");
        }

        var trimmed = token.Trim();
        var registration = await _db.CompanyRegistrations
            .FirstOrDefaultAsync(
                r => r.ActivationToken == trimmed && r.ActivationToken != "",
                cancellationToken)
            ?? throw new KeyNotFoundException("Ongeldige of verlopen activatielink.");

        if (registration.Status == CompanyRegistrationStatus.Activated
            || registration.Status == CompanyRegistrationStatus.TakeoverApproved)
        {
            throw new InvalidOperationException("Deze activatielink is al gebruikt.");
        }

        if (IsConfirmationExpired(registration))
        {
            await DeleteRegistrationCascadeAsync(registration.Id, cancellationToken);
            throw new InvalidOperationException(
                "Deze activatielink is verlopen. De aanvraag is verwijderd — registreer opnieuw.");
        }

        return await CompleteConfirmationAsync(registration, cancellationToken);
    }

    public async Task<int> PurgeExpiredUnconfirmedAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        var expiredIds = await _db.CompanyRegistrations
            .Where(r =>
                (r.Status == CompanyRegistrationStatus.PendingActivation
                 || r.Status == CompanyRegistrationStatus.TakeoverPending)
                && ((r.EmailVerificationExpiresAt != null && r.EmailVerificationExpiresAt < cutoff)
                    || (r.EmailVerificationExpiresAt == null
                        && r.CreatedAt < cutoff - ActivationTokenTtl)))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in expiredIds)
        {
            await DeleteRegistrationCascadeAsync(id, cancellationToken);
        }

        return expiredIds.Count;
    }

    private async Task<RegistrationActivationResult> CompleteConfirmationAsync(
        CompanyRegistration registration,
        CancellationToken cancellationToken)
    {
        if (registration.Status == CompanyRegistrationStatus.TakeoverPending)
        {
            return await CompleteTakeoverEmailVerificationAsync(registration, cancellationToken);
        }

        if (registration.Status != CompanyRegistrationStatus.PendingActivation)
        {
            throw new InvalidOperationException(
                $"Registratie kan niet worden geactiveerd (status: {registration.Status}).");
        }

        var existingCompany = await _db.Companies
            .FirstOrDefaultAsync(c => c.KvkEstablishmentId == registration.KvkEstablishmentId, cancellationToken);
        if (existingCompany is not null)
        {
            if (await CompanyOccupancy.HasActiveManagingEmployersAsync(_db, existingCompany.Id, cancellationToken))
            {
                throw new InvalidOperationException(
                    "Deze vestiging is ondertussen al geregistreerd. Vraag toegang aan of start een eigendomsoverdracht.");
            }

            return await CompleteClaimAsync(registration, existingCompany, cancellationToken);
        }

        var passwordHash = ResolvePasswordHash(registration, out var needsSetPassword);
        var usedChosenPassword = !needsSetPassword;
        var (user, orgId, branchId) = await ProvisionCompaniesAndUserAsync(
            registration, passwordHash, cancellationToken);

        registration.Status = CompanyRegistrationStatus.Activated;
        registration.ActivatedAt = DateTime.UtcNow;
        registration.ContactEmailVerifiedAt = DateTime.UtcNow;
        registration.CreatedUserId = user.Id;
        registration.CreatedOrganizationCompanyId = orgId;
        registration.CreatedBranchCompanyId = branchId;
        // One-time token + pending password material: clear so replay cannot re-use them.
        ClearPendingSecrets(registration);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Registration",
            Message =
                $"Company registration activated: {registration.KvkEstablishmentId} ({registration.Scope}, SBI {registration.PrimarySbiCode ?? "-"}, intermediary={registration.IsIntermediarySbi})",
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(
                "Kon vestiging niet activeren (mogelijk al geregistreerd). Probeer opnieuw of dien een overnameverzoek in.",
                ex);
        }

        await _partnerAffiliates.EnsureProfileAsync(user.Id, cancellationToken);

        // Welcome token is granted on verification (CompanyVerificationService), not at activation.
        const bool welcomeGranted = false;

        if (registration.IsIntermediarySbi && _lenderRegistration is not null)
        {
            var bureauId = branchId;
            await _lenderRegistration.StartForNewBureauAsync(
                bureauId,
                registration.KvkNumber,
                cancellationToken);
        }

        await SendActivatedCredentialsEmailAsync(registration, user.Id, needsSetPassword, cancellationToken);

        _logger.LogInformation(
            "Activated registration {Id} for {Email}",
            registration.Id,
            EmailServiceStub.RedactEmail(registration.ContactEmail));

        return await BuildActivationResultAsync(
            registration, usedChosenPassword, welcomeGranted, cancellationToken);
    }

    /// <summary>
    /// Credits 1 welcome token on the registered vestiging so the first publish is free.
    /// Skipped during the free-publish promo (publish is already free until that date).
    /// Idempotent via <see cref="Company.HasReceivedWelcomeToken"/>.
    /// Called from <see cref="CompanyVerificationService"/> after verification — not at activation.
    /// Returns whether a ledger credit was granted.
    /// </summary>
    internal async Task<bool> GrantWelcomeTokenAsync(
        Guid branchCompanyId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstAsync(c => c.Id == branchCompanyId, cancellationToken);
        if (company.HasReceivedWelcomeToken)
        {
            return false;
        }

        // Welcome only after the root organisation is verified.
        var rootStatus = company.ParentCompanyId is Guid parentId
            ? await _db.Companies.AsNoTracking()
                .Where(c => c.Id == parentId)
                .Select(c => c.VerificationStatus)
                .FirstOrDefaultAsync(cancellationToken)
            : company.VerificationStatus;
        if (!CompanyVerificationRules.CanUseWelcomeToken(rootStatus))
        {
            return false;
        }

        var features = await _features.GetAsync(cancellationToken);
        if (FreePublishRules.IsActive(features.FreePublishUntil, DateTime.UtcNow))
        {
            // Mark as received without granting — no delayed welcome token after the promo ends.
            company.HasReceivedWelcomeToken = true;
            company.WelcomeTokenLedgerCredited = false;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Skipped welcome token for company {CompanyId} (free publish until {Until})",
                branchCompanyId,
                features.FreePublishUntil);
            return false;
        }

        company.HasReceivedWelcomeToken = true;
        company.WelcomeTokenLedgerCredited = true;
        await _ledger.GrantAsync(
            branchCompanyId,
            WelcomeTokenAmount,
            actorUserId,
            WelcomeTokenNote,
            cancellationToken);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Tokens",
            Message = $"{WelcomeTokenNote} (company {branchCompanyId})",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Granted welcome token to company {CompanyId} for user {UserId}",
            branchCompanyId,
            actorUserId);
        return true;
    }

    public async Task<IReadOnlyList<TakeoverInboxItem>> ListPendingTakeoversAsync(
        IReadOnlyCollection<Guid> accessibleCompanyIds,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        // Only legacy colleague takeovers appear in the employer inbox.
        // Ownership transfers are admin-only (tab Toegang).
        var query = _db.EstablishmentTakeoverRequests
            .AsNoTracking()
            .Include(t => t.Registration)
            .Include(t => t.TargetCompany)
            .Where(t => t.Status == TakeoverRequestStatus.Pending
                        && t.Kind == TakeoverRequestKind.Colleague);

        if (!isAdmin)
        {
            query = query.Where(t => accessibleCompanyIds.Contains(t.TargetCompanyId));
        }

        // Only show takeovers whose requester confirmed ownership of the contact e-mail.
        var rows = await query
            .Where(t => t.Registration.ContactEmailVerifiedAt != null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new TakeoverInboxItem(
            t.Id,
            t.RegistrationId,
            t.TargetCompanyId,
            t.TargetCompany.Name,
            t.Registration.KvkEstablishmentId,
            t.Registration.ContactName,
            t.Registration.ContactEmail,
            t.Registration.Scope,
            t.CreatedAt)).ToList();
    }

    public async Task<TakeoverDecisionResult> ApproveTakeoverAsync(
        Guid takeoverId,
        Guid actorUserId,
        UserRole actorRole,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var takeover = await _db.EstablishmentTakeoverRequests
            .Include(t => t.Registration)
            .Include(t => t.TargetCompany)
            .FirstOrDefaultAsync(t => t.Id == takeoverId, cancellationToken)
            ?? throw new KeyNotFoundException("Overnameverzoek niet gevonden.");

        if (takeover.Status != TakeoverRequestStatus.Pending)
        {
            throw new InvalidOperationException("Dit verzoek is al afgehandeld.");
        }

        if (!isAdmin
            && (accessibleCompanyIds is null || !accessibleCompanyIds.Contains(takeover.TargetCompanyId)))
        {
            throw new UnauthorizedAccessException("Geen toegang tot deze vestiging.");
        }

        var registration = takeover.Registration;
        if (registration.ContactEmailVerifiedAt is null)
        {
            throw new InvalidOperationException(
                "De aanvrager heeft het e-mailadres nog niet bevestigd; goedkeuren is niet mogelijk.");
        }

        if (takeover.Kind == TakeoverRequestKind.OwnershipTransfer)
        {
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Eigendomsoverdracht kan alleen door Lobsy-support worden goedgekeurd.");
            }

            if (takeover.LetterVerifiedAtUtc is null)
            {
                throw new InvalidOperationException(
                    "Eerst moet de briefcode zijn bevestigd voordat eigendomsoverdracht kan worden goedgekeurd.");
            }
        }
        else if (!isAdmin
                 && registration.Scope == RegistrationScope.Organization
                 && actorRole != UserRole.EnterpriseManager)
        {
            throw new UnauthorizedAccessException(
                "Alleen een bedrijfsmanager of admin mag een organisatie-overname goedkeuren.");
        }

        // Another approve already merged this vestiging.
        var alreadyMerged = await _db.EstablishmentTakeoverRequests.AnyAsync(
            t => t.TargetCompanyId == takeover.TargetCompanyId
                 && t.Id != takeover.Id
                 && t.Status == TakeoverRequestStatus.Approved,
            cancellationToken);
        if (alreadyMerged)
        {
            takeover.Status = TakeoverRequestStatus.Cancelled;
            takeover.DecidedAt = DateTime.UtcNow;
            takeover.DecidedByUserId = actorUserId;
            takeover.DecisionNote = "Geannuleerd: vestiging is al overgenomen.";
            registration.Status = CompanyRegistrationStatus.Cancelled;
            ClearPendingSecrets(registration);
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Deze vestiging is al overgenomen via een ander verzoek.");
        }

        var target = takeover.TargetCompany;
        var passwordHash = ResolvePasswordHash(registration, out var needsSetPassword);

        Guid? orgId;
        Guid branchId = target.Id;

        if (registration.IsIntermediarySbi)
        {
            // Detach from any employer org tree and promote the vestiging to a standalone Intermediary.
            target.Type = CompanyType.Intermediary;
            target.ParentCompanyId = null;
            if (!string.IsNullOrWhiteSpace(registration.EstablishmentName))
            {
                var kvkCompany = await _kvk.GetByKvkNumberAsync(registration.KvkNumber, cancellationToken);
                target.Name = kvkCompany?.Name ?? registration.EstablishmentName;
            }

            orgId = null;
        }
        else if (registration.Scope == RegistrationScope.Organization)
        {
            if (target.ParentCompanyId is Guid existingParentId)
            {
                // Reuse existing org shell — do not create a competing parent.
                orgId = existingParentId;
            }
            else
            {
                var kvkCompany = await _kvk.GetByKvkNumberAsync(registration.KvkNumber, cancellationToken);
                var org = new Company
                {
                    Id = Guid.NewGuid(),
                    Name = kvkCompany?.Name ?? $"{registration.EstablishmentName} Organisatie",
                    KvkNumber = registration.KvkNumber,
                    KvkEstablishmentId = null,
                    Address = kvkCompany?.Address ?? registration.EstablishmentAddress,
                    Location = target.Location,
                    Type = CompanyType.Employer,
                    VerificationStatus = target.VerificationStatus,
                    VerificationMethod = target.VerificationMethod,
                    VerifiedAtUtc = target.VerifiedAtUtc,
                    VerificationUpdatedAtUtc = DateTime.UtcNow
                };
                _db.Companies.Add(org);
                target.ParentCompanyId = org.Id;
                orgId = org.Id;
                await WmlSalaryTableService.EnsureForCompanyAsync(_db, org.Id, cancellationToken);
            }

            await ClaimSiblingEstablishmentsAsync(registration.KvkNumber, orgId.Value, target.Id, registration, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var balance = await _ledger.GetBalanceAsync(target.Id, cancellationToken);
            if (balance > 0)
            {
                await _ledger.AllocateAsync(
                    target.Id,
                    orgId.Value,
                    balance,
                    actorUserId,
                    "Org-merge: tokens naar organisatie",
                    cancellationToken);
            }
        }
        else
        {
            orgId = target.ParentCompanyId;
        }

        var role = ResolveRegistrationRole(registration);

        // Intermediary takeover always anchors on the standalone vestiging (never a former employer org).
        var primaryCompanyId = registration.IsIntermediarySbi ? branchId : (orgId ?? branchId);
        var user = await CreateRegistrationUserAsync(registration, role, primaryCompanyId, passwordHash, cancellationToken);
        await EnsureMembershipAsync(user.Id, branchId, cancellationToken);
        if (!registration.IsIntermediarySbi && orgId is Guid oid)
        {
            await EnsureMembershipAsync(user.Id, oid, cancellationToken);
            if (role == UserRole.EnterpriseManager)
            {
                user.CompanyId = oid;
                var children = await _db.Companies
                    .Where(c => c.ParentCompanyId == oid)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);
                foreach (var childId in children)
                {
                    await EnsureMembershipAsync(user.Id, childId, cancellationToken);
                }
            }
        }
        else
        {
            user.CompanyId = branchId;
        }

        // Transfer: prior employers lose access to the acquired vestiging — intermediaries keep their link (07.7).
        // Parent-org memberships stay intact when reusing an existing organization shell.
        await RevokePriorEmployerAccessAsync(
            companyIds: [branchId],
            exceptUserId: user.Id,
            cancellationToken);
        await NotifyIntermediariesOfSelfManagedAsync(branchId, target.Name, cancellationToken);

        // Cancel other pending takeovers for the same target.
        var otherPending = await _db.EstablishmentTakeoverRequests
            .Include(t => t.Registration)
            .Where(t => t.TargetCompanyId == target.Id
                        && t.Id != takeover.Id
                        && t.Status == TakeoverRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var other in otherPending)
        {
            other.Status = TakeoverRequestStatus.Cancelled;
            other.DecidedAt = DateTime.UtcNow;
            other.DecidedByUserId = actorUserId;
            other.DecisionNote = "Geannuleerd door andere goedgekeurde overname.";
            other.Registration.Status = CompanyRegistrationStatus.Cancelled;
            ClearPendingSecrets(other.Registration);
        }

        takeover.Status = TakeoverRequestStatus.Approved;
        takeover.DecidedAt = DateTime.UtcNow;
        takeover.DecidedByUserId = actorUserId;
        takeover.DecisionNote = "Goedgekeurd — org-merge uitgevoerd.";

        registration.Status = CompanyRegistrationStatus.TakeoverApproved;
        registration.ActivatedAt = DateTime.UtcNow;
        registration.CreatedUserId = user.Id;
        registration.CreatedOrganizationCompanyId = orgId;
        registration.CreatedBranchCompanyId = branchId;
        ClearPendingSecrets(registration);

        // Preserve salesmanager referral captured at submit time.
        await ApplySalesManagerReferralAsync(registration, target, orgId, cancellationToken);
        await ApplyPartnerReferralAsync(registration, target, orgId, cancellationToken);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "OrgMerge",
            Message =
                $"Takeover approved: {target.Name} ({target.KvkEstablishmentId}) → requester {EmailServiceStub.RedactEmail(registration.ContactEmail)} ({registration.Scope})",
            DetailsJson =
                $"{{\"takeoverId\":\"{takeover.Id}\",\"orgId\":\"{orgId}\",\"branchId\":\"{branchId}\"}}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await _partnerAffiliates.EnsureProfileAsync(user.Id, cancellationToken);

        if (registration.IsIntermediarySbi && _lenderRegistration is not null)
        {
            await _lenderRegistration.StartForNewBureauAsync(
                branchId,
                registration.KvkNumber,
                cancellationToken);
        }

        var features = await _features.GetAsync(cancellationToken);
        string? setPasswordUrl = needsSetPassword
            ? await CreateSetPasswordUrlAsync(user.Id, registration.ContactEmail, cancellationToken)
            : null;
        var approved = TransactionalEmails.TakeoverApproved(
            features.PublicWebBaseUrl,
            registration.ContactName,
            target.Name,
            registration.ContactEmail,
            setPasswordUrl,
            orgId is not null);
        await _mailer.SendAsync(approved, registration.ContactEmail, cancellationToken: cancellationToken);

        return new TakeoverDecisionResult(
            takeover.Id,
            TakeoverRequestStatus.Approved,
            "Overname goedgekeurd; tokens/vacatures/geschiedenis migreren naar de organisatie.",
            orgId,
            branchId);
    }

    public async Task<TakeoverDecisionResult> RejectTakeoverAsync(
        Guid takeoverId,
        Guid actorUserId,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var takeover = await _db.EstablishmentTakeoverRequests
            .Include(t => t.Registration)
            .Include(t => t.TargetCompany)
            .FirstOrDefaultAsync(t => t.Id == takeoverId, cancellationToken)
            ?? throw new KeyNotFoundException("Overnameverzoek niet gevonden.");

        if (takeover.Status != TakeoverRequestStatus.Pending)
        {
            throw new InvalidOperationException("Dit verzoek is al afgehandeld.");
        }

        if (!isAdmin
            && (accessibleCompanyIds is null || !accessibleCompanyIds.Contains(takeover.TargetCompanyId)))
        {
            throw new UnauthorizedAccessException("Geen toegang tot deze vestiging.");
        }

        takeover.Status = TakeoverRequestStatus.Rejected;
        takeover.DecidedAt = DateTime.UtcNow;
        takeover.DecidedByUserId = actorUserId;
        takeover.DecisionNote = string.IsNullOrWhiteSpace(note) ? "Afgewezen" : note.Trim();

        takeover.Registration.Status = CompanyRegistrationStatus.TakeoverRejected;
        ClearPendingSecrets(takeover.Registration);

        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var rejected = TransactionalEmails.TakeoverRejected(
            features.PublicWebBaseUrl,
            takeover.Registration.ContactName,
            takeover.TargetCompany.Name);
        await _mailer.SendAsync(rejected, takeover.Registration.ContactEmail, cancellationToken: cancellationToken);

        return new TakeoverDecisionResult(
            takeover.Id,
            TakeoverRequestStatus.Rejected,
            "Overname afgewezen.",
            null,
            null);
    }

    private async Task<(User User, Guid? OrgId, Guid BranchId)> ProvisionCompaniesAndUserAsync(
        CompanyRegistration registration,
        string? passwordHash,
        CancellationToken cancellationToken)
    {
        Guid? orgId = null;
        Company branch;

        if (registration.IsIntermediarySbi)
        {
            // Single Intermediary company (no employer parent/sibling hierarchy).
            // Multi-client linking happens via intermediary-client KVK flows after onboarding.
            var kvkCompany = await _kvk.GetByKvkNumberAsync(registration.KvkNumber, cancellationToken);
            branch = new Company
            {
                Id = Guid.NewGuid(),
                Name = kvkCompany?.Name ?? registration.EstablishmentName,
                KvkNumber = registration.KvkNumber,
                KvkEstablishmentId = registration.KvkEstablishmentId,
                Address = registration.EstablishmentAddress,
                Location = new GeoPoint(registration.Latitude, registration.Longitude),
                LocationSource = ResolveLocationSource(registration),
                Type = CompanyType.Intermediary,
                VerificationStatus = CompanyVerificationStatus.Unverified,
                VerificationMethod = CompanyVerificationMethod.None,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            };
            ApplyKvkVerificationState(branch, registration);
            _db.Companies.Add(branch);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, branch.Id, cancellationToken);
        }
        else if (registration.Scope == RegistrationScope.Organization)
        {
            var kvkCompany = await _kvk.GetByKvkNumberAsync(registration.KvkNumber, cancellationToken);
            var org = new Company
            {
                Id = Guid.NewGuid(),
                Name = kvkCompany?.Name ?? $"{registration.EstablishmentName} Organisatie",
                KvkNumber = registration.KvkNumber,
                KvkEstablishmentId = null,
                Address = kvkCompany?.Address ?? registration.EstablishmentAddress,
                Location = new GeoPoint(registration.Latitude, registration.Longitude),
                LocationSource = ResolveLocationSource(registration),
                Type = CompanyType.Employer,
                VerificationStatus = CompanyVerificationStatus.Unverified,
                VerificationMethod = CompanyVerificationMethod.None,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            };
            ApplyKvkVerificationState(org, registration);
            await PrefillWorkTypesFromSbiAsync(org, registration, kvkCompany?.EffectiveSbiCodes, cancellationToken);
            _db.Companies.Add(org);
            orgId = org.Id;
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, org.Id, cancellationToken);

            branch = new Company
            {
                Id = Guid.NewGuid(),
                Name = registration.EstablishmentName,
                KvkNumber = registration.KvkNumber,
                KvkEstablishmentId = registration.KvkEstablishmentId,
                Address = registration.EstablishmentAddress,
                Location = new GeoPoint(registration.Latitude, registration.Longitude),
                LocationSource = ResolveLocationSource(registration),
                Type = CompanyType.Employer,
                ParentCompanyId = org.Id,
                VerificationStatus = CompanyVerificationStatus.Unverified,
                VerificationMethod = CompanyVerificationMethod.None,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            };
            ApplyKvkVerificationState(branch, registration);
            _db.Companies.Add(branch);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, branch.Id, cancellationToken);

            // Sibling claim only when KVK is verified — pending registrations skip auto-claim.
            if (registration.KvkVerificationStatus == KvkVerificationStatus.Verified)
            {
                await ClaimSiblingEstablishmentsAsync(
                    registration.KvkNumber, org.Id, branch.Id, registration, cancellationToken);
            }
        }
        else
        {
            branch = new Company
            {
                Id = Guid.NewGuid(),
                Name = registration.EstablishmentName,
                KvkNumber = registration.KvkNumber,
                KvkEstablishmentId = registration.KvkEstablishmentId,
                Address = registration.EstablishmentAddress,
                Location = new GeoPoint(registration.Latitude, registration.Longitude),
                LocationSource = ResolveLocationSource(registration),
                Type = CompanyType.Employer,
                VerificationStatus = CompanyVerificationStatus.Unverified,
                VerificationMethod = CompanyVerificationMethod.None,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            };
            ApplyKvkVerificationState(branch, registration);
            await PrefillWorkTypesFromSbiAsync(branch, registration, sbiCodes: null, cancellationToken);
            _db.Companies.Add(branch);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, branch.Id, cancellationToken);
        }

        await ApplySalesManagerReferralAsync(registration, branch, orgId, cancellationToken);
        await ApplyPartnerReferralAsync(registration, branch, orgId, cancellationToken);

        var role = ResolveRegistrationRole(registration);

        var primaryCompanyId = orgId ?? branch.Id;
        var user = await CreateRegistrationUserAsync(registration, role, primaryCompanyId, passwordHash, cancellationToken);
        await EnsureMembershipAsync(user.Id, branch.Id, cancellationToken);
        if (orgId is Guid oid)
        {
            await EnsureMembershipAsync(user.Id, oid, cancellationToken);
            var childIds = _db.Companies.Local
                .Where(c => c.ParentCompanyId == oid)
                .Select(c => c.Id)
                .Distinct()
                .ToList();
            foreach (var childId in childIds)
            {
                await EnsureMembershipAsync(user.Id, childId, cancellationToken);
            }
        }

        return (user, orgId, branch.Id);
    }

    private static UserRole ResolveRegistrationRole(CompanyRegistration registration)
    {
        if (registration.IsIntermediarySbi)
        {
            return UserRole.Intermediary;
        }

        // Non-SBI 78: always Bedrijfsmanager (Organization = org tree; BranchOnly = vestiging-as-company).
        return UserRole.EnterpriseManager;
    }

    private async Task ClaimSiblingEstablishmentsAsync(
        string kvkNumber,
        Guid orgId,
        Guid excludeBranchId,
        CompanyRegistration registration,
        CancellationToken cancellationToken)
    {
        var establishments = await _kvk.GetEstablishmentsAsync(kvkNumber, cancellationToken);
        var selected = ParseSelectedIds(registration.SelectedEstablishmentIdsJson);
        var usedIds = await _db.Companies
            .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
            .Select(c => c.KvkEstablishmentId!)
            .ToListAsync(cancellationToken);

        usedIds.AddRange(_db.Companies.Local
            .Where(c => c.KvkEstablishmentId != null)
            .Select(c => c.KvkEstablishmentId!));

        var pendingEstablishmentIds = await _db.CompanyRegistrations
            .Where(r => r.KvkNumber == kvkNumber
                        && (r.Status == CompanyRegistrationStatus.PendingActivation
                            || r.Status == CompanyRegistrationStatus.TakeoverPending))
            .Select(r => r.KvkEstablishmentId)
            .ToListAsync(cancellationToken);

        var excludeEstId = _db.Companies.Local.FirstOrDefault(c => c.Id == excludeBranchId)?.KvkEstablishmentId
            ?? (await _db.Companies.AsNoTracking()
                .Where(c => c.Id == excludeBranchId)
                .Select(c => c.KvkEstablishmentId)
                .FirstOrDefaultAsync(cancellationToken));

        foreach (var est in establishments)
        {
            if (usedIds.Contains(est.KvkEstablishmentId, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            // Do not steal vestigingen that another registrant already started / owned.
            if (pendingEstablishmentIds.Contains(est.KvkEstablishmentId, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(excludeEstId)
                && est.KvkEstablishmentId.Equals(excludeEstId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // D7: only claim exactly the ticked set when the wizard sent one.
            if (selected is { Count: > 0 }
                && !selected.Contains(est.KvkEstablishmentId, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var sibling = new Company
            {
                Id = Guid.NewGuid(),
                Name = est.Name,
                KvkNumber = est.KvkNumber,
                KvkEstablishmentId = est.KvkEstablishmentId,
                Address = est.Address,
                Location = new GeoPoint(est.Latitude, est.Longitude),
                LocationSource = CompanyLocationSource.Kvk,
                Type = CompanyType.Employer,
                ParentCompanyId = orgId,
                KvkVerificationStatus = KvkVerificationStatus.Verified,
                KvkVerifiedAtUtc = DateTime.UtcNow,
                VerificationStatus = CompanyVerificationStatus.Unverified,
                VerificationMethod = CompanyVerificationMethod.None,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            };
            _db.Companies.Add(sibling);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, sibling.Id, cancellationToken);
            usedIds.Add(est.KvkEstablishmentId);
        }
    }

    private async Task<User> CreateRegistrationUserAsync(
        CompanyRegistration registration,
        UserRole role,
        Guid primaryCompanyId,
        string? passwordHash,
        CancellationToken cancellationToken)
    {
        var email = registration.ContactEmail.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
        {
            throw new InvalidOperationException(
                "Dit e-mailadres hoort al bij een bestaande gebruiker en kan niet via registratie worden overgenomen.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = registration.ContactName,
            Role = role,
            CompanyId = primaryCompanyId,
            IsActive = true,
            TermsAcceptedAt = registration.ConsentAcceptedAt ?? DateTime.UtcNow,
            ConsentVersion = registration.ConsentVersion ?? PrivacyConstants.CurrentConsentVersion
        };
        _db.Users.Add(user);

        if (!string.IsNullOrWhiteSpace(passwordHash))
        {
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = passwordHash
            });
        }

        return user;
    }

    private async Task RevokePriorEmployerAccessAsync(
        IReadOnlyList<Guid> companyIds,
        Guid exceptUserId,
        CancellationToken cancellationToken)
    {
        var idSet = companyIds.ToHashSet();
        // Intermediaries keep their client link on takeover/claim (07.7 / Dependencies G Absent).
        var affected = await _db.Users
            .Include(u => u.CompanyMemberships)
            .Where(u => u.Id != exceptUserId && u.IsActive && CompanyOccupancy.IsRevocableOnTakeover(u.Role))
            .Where(u =>
                (u.CompanyId != null && idSet.Contains(u.CompanyId.Value))
                || u.CompanyMemberships.Any(m => idSet.Contains(m.CompanyId)))
            .ToListAsync(cancellationToken);

        foreach (var user in affected)
        {
            var keepMemberships = user.CompanyMemberships
                .Where(m => !idSet.Contains(m.CompanyId))
                .ToList();
            var toRemove = user.CompanyMemberships
                .Where(m => idSet.Contains(m.CompanyId))
                .ToList();
            foreach (var membership in toRemove)
            {
                _db.UserCompanies.Remove(membership);
            }

            if (user.CompanyId is Guid primary && idSet.Contains(primary))
            {
                user.CompanyId = keepMemberships.Count > 0 ? keepMemberships[0].CompanyId : null;
            }

            var stillHasAccess = user.CompanyId is not null || keepMemberships.Count > 0;
            if (!stillHasAccess && user.Role != UserRole.Admin)
            {
                user.IsActive = false;
            }
        }
    }

    private async Task NotifyIntermediariesOfSelfManagedAsync(
        Guid companyId,
        string companyName,
        CancellationToken cancellationToken)
    {
        var intermediaries = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Intermediary)
            .Where(u =>
                (u.CompanyId != null && u.CompanyId == companyId)
                || u.CompanyMemberships.Any(m => m.CompanyId == companyId))
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (intermediaries.Count == 0)
        {
            return;
        }

        var features = await _features.GetAsync(cancellationToken);
        var mail = TransactionalEmails.IntermediaryClientSelfManaged(features.PublicWebBaseUrl, companyName);
        foreach (var email in intermediaries)
        {
            await _mailer.SendAsync(mail, email, cancellationToken: cancellationToken);
        }
    }

    private async Task EnsureMembershipAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        var exists = await _db.UserCompanies.AnyAsync(
            uc => uc.UserId == userId && uc.CompanyId == companyId, cancellationToken);
        if (exists)
        {
            return;
        }

        if (_db.UserCompanies.Local.Any(uc => uc.UserId == userId && uc.CompanyId == companyId))
        {
            return;
        }

        _db.UserCompanies.Add(new UserCompany { UserId = userId, CompanyId = companyId });
    }

    private async Task<RegistrationActivationResult> BuildActivationResultAsync(
        CompanyRegistration registration,
        bool usedChosenPassword,
        bool welcomeTokenGranted,
        CancellationToken cancellationToken)
    {
        var user = registration.CreatedUserId is Guid uid
            ? await _db.Users.Include(u => u.CompanyMemberships).FirstOrDefaultAsync(u => u.Id == uid, cancellationToken)
            : null;

        if (user is null)
        {
            throw new InvalidOperationException("Gebruiker ontbreekt na activatie.");
        }

        var companyIds = user.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
        if (user.CompanyId is Guid primary && !companyIds.Contains(primary))
        {
            companyIds.Insert(0, primary);
        }

        var features = await _features.GetAsync(cancellationToken);
        var freeUntil = FreePublishRules.IsActive(features.FreePublishUntil, DateTime.UtcNow)
            ? features.FreePublishUntil
            : null;

        return new RegistrationActivationResult(
            registration.Id,
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            companyIds,
            registration.CreatedOrganizationCompanyId,
            registration.CreatedBranchCompanyId,
            usedChosenPassword,
            WelcomeTokenGranted: welcomeTokenGranted,
            FreePublishUntil: freeUntil);
    }

    private async Task<RegistrationActivationResult> CompleteTakeoverEmailVerificationAsync(
        CompanyRegistration registration,
        CancellationToken cancellationToken)
    {
        if (registration.ContactEmailVerifiedAt is not null)
        {
            throw new InvalidOperationException(
                takeoverAlreadyMessage(registration));
        }

        var takeover = await _db.EstablishmentTakeoverRequests
            .Include(t => t.TargetCompany)
            .FirstOrDefaultAsync(
                t => t.RegistrationId == registration.Id && t.Status == TakeoverRequestStatus.Pending,
                cancellationToken)
            ?? throw new InvalidOperationException("Overnameverzoek niet gevonden voor deze registratie.");

        registration.ContactEmailVerifiedAt = DateTime.UtcNow;
        // One-time verification token; password hash stays until approve/reject.
        registration.ActivationToken = string.Empty;
        registration.EmailVerificationCode = null;
        registration.EmailVerificationExpiresAt = null;
        registration.EmailVerificationFailedAttempts = 0;

        if (takeover.Kind == TakeoverRequestKind.OwnershipTransfer)
        {
            var plain = LetterVerificationCodes.Create();
            takeover.LetterCodeHash = VerificationCodes.Hash(plain);
            takeover.LetterExpiresAtUtc = DateTime.UtcNow.AddDays(30);
            takeover.LetterSentAtUtc = DateTime.UtcNow;
            takeover.LetterFailedAttempts = 0;
            await _db.SaveChangesAsync(cancellationToken);
            await NotifyOwnershipTransferManagersAsync(takeover.TargetCompany, cancellationToken);
            // Stub/dev: plaintext is only in the physical letter PDF (not persisted). Tests overwrite the hash.
        }
        else
        {
            await _db.SaveChangesAsync(cancellationToken);
            await NotifyTakeoverRequestedAsync(registration, takeover.TargetCompany, cancellationToken);
        }

        _logger.LogInformation(
            "Takeover e-mail verified for registration {Id} ({Email}) kind={Kind}",
            registration.Id,
            EmailServiceStub.RedactEmail(registration.ContactEmail),
            takeover.Kind);

        return new RegistrationActivationResult(
            registration.Id,
            Guid.Empty,
            registration.ContactEmail,
            registration.ContactName,
            Role: string.Empty,
            CompanyId: null,
            CompanyIds: Array.Empty<Guid>(),
            OrganizationCompanyId: null,
            BranchCompanyId: null,
            UsedChosenPassword: true,
            EmailVerifiedAwaitingTakeover: true);

        static string takeoverAlreadyMessage(CompanyRegistration _) =>
            "Dit e-mailadres is al bevestigd. Het overnameverzoek wacht op goedkeuring.";
    }

    private async Task NotifyOwnershipTransferManagersAsync(
        Company company,
        CancellationToken cancellationToken)
    {
        var tree = await CompanyOccupancy.ExpandCompanyTreeAsync(_db, company.Id, cancellationToken);
        var managers = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && CompanyOccupancy.ManagingEmployerRoles.Contains(u.Role))
            .Where(u =>
                (u.CompanyId != null && tree.Contains(u.CompanyId.Value))
                || u.CompanyMemberships.Any(m => tree.Contains(m.CompanyId)))
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var mail = TransactionalEmails.OwnershipTransferManagersNotify(features.PublicWebBaseUrl, company.Name);
        foreach (var email in managers)
        {
            await _mailer.SendAsync(mail, email, cancellationToken: cancellationToken);
        }

        var takeover = await _db.EstablishmentTakeoverRequests
            .FirstOrDefaultAsync(
                t => t.TargetCompanyId == company.Id
                     && t.Kind == TakeoverRequestKind.OwnershipTransfer
                     && t.Status == TakeoverRequestStatus.Pending
                     && t.ManagersNotifiedAtUtc == null,
                cancellationToken);
        if (takeover is not null)
        {
            takeover.ManagersNotifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<RegistrationActivationResult> CompleteClaimAsync(
        CompanyRegistration registration,
        Company existingCompany,
        CancellationToken cancellationToken)
    {
        var passwordHash = ResolvePasswordHash(registration, out var needsSetPassword);
        var usedChosenPassword = !needsSetPassword;
        var role = ResolveRegistrationRole(registration);
        var orgId = existingCompany.ParentCompanyId;
        var branchId = existingCompany.Id;
        var primaryCompanyId = registration.IsIntermediarySbi
            ? branchId
            : (orgId ?? branchId);

        var user = await CreateRegistrationUserAsync(
            registration, role, primaryCompanyId, passwordHash, cancellationToken);
        await EnsureMembershipAsync(user.Id, branchId, cancellationToken);
        if (!registration.IsIntermediarySbi && orgId is Guid oid)
        {
            await EnsureMembershipAsync(user.Id, oid, cancellationToken);
            if (role == UserRole.EnterpriseManager)
            {
                user.CompanyId = oid;
                var children = await _db.Companies
                    .Where(c => c.ParentCompanyId == oid)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);
                foreach (var childId in children)
                {
                    await EnsureMembershipAsync(user.Id, childId, cancellationToken);
                }
            }
        }
        else
        {
            user.CompanyId = branchId;
        }

        // Do NOT revoke intermediaries (07.7). Public verification state stays until the new owner verifies.
        registration.Status = CompanyRegistrationStatus.Activated;
        registration.ActivatedAt = DateTime.UtcNow;
        registration.ContactEmailVerifiedAt = DateTime.UtcNow;
        registration.CreatedUserId = user.Id;
        registration.CreatedOrganizationCompanyId = orgId;
        registration.CreatedBranchCompanyId = branchId;
        ClearPendingSecrets(registration);

        await ApplySalesManagerReferralAsync(registration, existingCompany, orgId, cancellationToken);
        await ApplyPartnerReferralAsync(registration, existingCompany, orgId, cancellationToken);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Registration",
            Message =
                $"Claimed unmanaged company {existingCompany.Id} ({existingCompany.KvkEstablishmentId}) by {EmailServiceStub.RedactEmail(registration.ContactEmail)}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await NotifyIntermediariesOfSelfManagedAsync(branchId, existingCompany.Name, cancellationToken);
        await _partnerAffiliates.EnsureProfileAsync(user.Id, cancellationToken);

        if (registration.IsIntermediarySbi && _lenderRegistration is not null)
        {
            await _lenderRegistration.StartForNewBureauAsync(
                branchId,
                registration.KvkNumber,
                cancellationToken);
        }

        await SendActivatedCredentialsEmailAsync(registration, user.Id, needsSetPassword, cancellationToken);

        return await BuildActivationResultAsync(
            registration, usedChosenPassword, welcomeTokenGranted: false, cancellationToken);
    }

    /// <summary>
    /// Confirms the ownership-transfer letter code (07.5). Does not approve the transfer —
    /// admin still must call <see cref="ApproveTakeoverAsync"/>.
    /// </summary>
    public async Task ConfirmOwnershipTransferLetterAsync(
        Guid takeoverId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var takeover = await _db.EstablishmentTakeoverRequests
            .FirstOrDefaultAsync(t => t.Id == takeoverId, cancellationToken)
            ?? throw new KeyNotFoundException("Eigendomsoverdracht niet gevonden.");

        if (takeover.Kind != TakeoverRequestKind.OwnershipTransfer)
        {
            throw new InvalidOperationException("Dit verzoek is geen eigendomsoverdracht.");
        }

        if (takeover.Status != TakeoverRequestStatus.Pending)
        {
            throw new InvalidOperationException("Dit verzoek is al afgehandeld.");
        }

        if (takeover.LetterVerifiedAtUtc is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(takeover.LetterCodeHash)
            || takeover.LetterExpiresAtUtc is null
            || takeover.LetterExpiresAtUtc < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Briefcode ontbreekt of is verlopen.");
        }

        var normalized = LetterVerificationCodes.Normalize(code);
        if (!LetterVerificationCodes.IsWellFormed(normalized)
            || !VerificationCodes.MatchesHash(takeover.LetterCodeHash, normalized))
        {
            var attempts = takeover.LetterFailedAttempts;
            var dead = VerificationCodes.RegisterFailedAttempt(ref attempts);
            takeover.LetterFailedAttempts = attempts;
            if (dead)
            {
                takeover.LetterCodeHash = null;
                takeover.DecisionNote = "Brief geblokkeerd na 5 pogingen.";
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(
                dead ? "Brief geblokkeerd na 5 pogingen." : "Onjuiste briefcode.");
        }

        takeover.LetterVerifiedAtUtc = DateTime.UtcNow;
        takeover.LetterCodeHash = null;
        await _db.SaveChangesAsync(cancellationToken);
    }


    private async Task SendTakeoverEmailVerificationAsync(
        CompanyRegistration registration,
        Company existing,
        string plaintextCode,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var verify = TransactionalEmails.TakeoverEmailVerification(
            features.PublicWebBaseUrl, registration.ContactName, existing.Name, plaintextCode);
        await _mailer.SendAsync(verify, registration.ContactEmail, cancellationToken: cancellationToken);
    }

    private async Task NotifyTakeoverRequestedAsync(
        CompanyRegistration registration,
        Company existing,
        CancellationToken cancellationToken)
    {
        var owners = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && JobsyRoles.IsEmployer(u.Role))
            .Where(u =>
                u.CompanyId == existing.Id
                || u.CompanyMemberships.Any(m => m.CompanyId == existing.Id)
                || (existing.ParentCompanyId != null && (
                    u.CompanyId == existing.ParentCompanyId
                    || u.CompanyMemberships.Any(m => m.CompanyId == existing.ParentCompanyId.Value))))
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var requestMail = TransactionalEmails.TakeoverRequest(
            features.PublicWebBaseUrl,
            existing.Name,
            existing.KvkEstablishmentId,
            registration.ContactName,
            registration.ContactEmail);
        foreach (var ownerEmail in owners)
        {
            await _mailer.SendAsync(requestMail, ownerEmail, cancellationToken: cancellationToken);
        }

        var submitted = TransactionalEmails.TakeoverSubmitted(
            features.PublicWebBaseUrl, registration.ContactName, existing.Name);
        await _mailer.SendAsync(submitted, registration.ContactEmail, cancellationToken: cancellationToken);
    }

    private async Task SendActivationEmailAsync(
        CompanyRegistration registration,
        string plaintextCode,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var activation = TransactionalEmails.RegistrationActivation(
            features.PublicWebBaseUrl,
            registration.ContactName,
            registration.EstablishmentName,
            plaintextCode);
        await _mailer.SendAsync(activation, registration.ContactEmail, cancellationToken: cancellationToken);
    }

    private async Task SendActivatedCredentialsEmailAsync(
        CompanyRegistration registration,
        Guid userId,
        bool needsSetPassword,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        string? setPasswordUrl = null;
        if (needsSetPassword)
        {
            var created = await _links.CreateAsync(
                OneTimeLinkPurpose.SetPassword,
                userId,
                companyId: null,
                registration.ContactEmail.Trim().ToLowerInvariant(),
                OneTimeLinkRules.SetPasswordLifetime,
                createdByUserId: null,
                cancellationToken);
            setPasswordUrl = EmailLayout.Absolute(
                features.PublicWebBaseUrl,
                $"/account/wachtwoord-instellen?t={Uri.EscapeDataString(created.Token)}");
        }

        var credentials = TransactionalEmails.RegistrationCredentials(
            features.PublicWebBaseUrl,
            registration.ContactName,
            registration.EstablishmentName,
            registration.ContactEmail,
            setPasswordUrl);
        await _mailer.SendAsync(credentials, registration.ContactEmail, cancellationToken: cancellationToken);
    }

    private async Task<string?> CreateSetPasswordUrlAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken)
    {
        var created = await _links.CreateAsync(
            OneTimeLinkPurpose.SetPassword,
            userId,
            companyId: null,
            email.Trim().ToLowerInvariant(),
            OneTimeLinkRules.SetPasswordLifetime,
            createdByUserId: null,
            cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return EmailLayout.Absolute(
            features.PublicWebBaseUrl,
            $"/account/wachtwoord-instellen?t={Uri.EscapeDataString(created.Token)}");
    }

    /// <summary>
    /// Uses the password hash stored at submit when present; otherwise generates a temporary password
    /// (legacy / takeover edge cases). Returns the hash and optional plaintext for e-mail only.
    /// </summary>
    /// <summary>
    /// Returns the password hash chosen at submit, or null when the user must set a password via link.
    /// </summary>
    private static string? ResolvePasswordHash(CompanyRegistration registration, out bool needsSetPassword)
    {
        if (!string.IsNullOrWhiteSpace(registration.PasswordHash))
        {
            needsSetPassword = false;
            return registration.PasswordHash;
        }

        needsSetPassword = true;
        return null;
    }

    /// <summary>
    /// AVG: drop one-time activation material and pending password hashes when a registration
    /// is activated, rejected, cancelled, or expired.
    /// </summary>
    private static void ClearPendingSecrets(CompanyRegistration registration)
    {
        registration.ActivationToken = string.Empty;
        registration.PasswordHash = null;
        registration.EmailVerificationCode = null;
        registration.EmailVerificationExpiresAt = null;
        registration.EmailVerificationFailedAttempts = 0;
    }

    private static string AssignConfirmationCode(CompanyRegistration registration)
    {
        var code = VerificationCodes.CreateNumericCode();
        registration.EmailVerificationCode = VerificationCodes.Hash(code);
        registration.EmailVerificationExpiresAt = DateTime.UtcNow.Add(ActivationTokenTtl);
        registration.EmailVerificationFailedAttempts = 0;
        return code;
    }

    private static bool IsConfirmationExpired(CompanyRegistration registration)
    {
        if (registration.EmailVerificationExpiresAt is DateTime expires)
        {
            return DateTime.UtcNow > expires;
        }

        return DateTime.UtcNow - registration.CreatedAt > ActivationTokenTtl;
    }

    private async Task<RegistrationSubmitResult> ResendConfirmationCodeAsync(
        CompanyRegistration registration,
        CancellationToken cancellationToken)
    {
        var plaintextCode = AssignConfirmationCode(registration);
        // Refresh opaque token so old e-mail links (if any) stop working.
        registration.ActivationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            if (registration.Status == CompanyRegistrationStatus.TakeoverPending)
            {
                var takeover = await _db.EstablishmentTakeoverRequests
                    .Include(t => t.TargetCompany)
                    .FirstOrDefaultAsync(t => t.RegistrationId == registration.Id, cancellationToken);
                if (takeover?.TargetCompany is not null)
                {
                    await SendTakeoverEmailVerificationAsync(
                        registration, takeover.TargetCompany, plaintextCode, cancellationToken);
                }
                else
                {
                    await SendActivationEmailAsync(registration, plaintextCode, cancellationToken);
                }
            }
            else
            {
                await SendActivationEmailAsync(registration, plaintextCode, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Resend confirmation e-mail failed for {Id}", registration.Id);
            throw new InvalidOperationException(
                "Kon de bevestigingsmail niet versturen. Probeer later opnieuw.");
        }

        return new RegistrationSubmitResult(
            registration.Id,
            registration.Status,
            RequiresTakeover: registration.Status == CompanyRegistrationStatus.TakeoverPending,
            Message:
            "We hebben een nieuwe bevestigingscode naar je e-mail gestuurd. Vul die hieronder in (geldig 10 minuten).",
            ActivationUrl: null,
            VerificationExpiresAt: registration.EmailVerificationExpiresAt);
    }

    private async Task DeleteRegistrationCascadeAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        var takeovers = await _db.EstablishmentTakeoverRequests
            .Where(t => t.RegistrationId == registrationId)
            .ToListAsync(cancellationToken);
        if (takeovers.Count > 0)
        {
            _db.EstablishmentTakeoverRequests.RemoveRange(takeovers);
        }

        var registration = await _db.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);
        if (registration is not null)
        {
            _db.CompanyRegistrations.Remove(registration);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted unconfirmed registration {Id}", registrationId);
    }

    private async Task ApplySalesManagerReferralAsync(
        CompanyRegistration registration,
        Company branch,
        Guid? orgId,
        CancellationToken cancellationToken)
    {
        SalesManagerProfile? profile = null;
        if (registration.SalesManagerUserId is Guid smUserId)
        {
            profile = await _db.SalesManagerProfiles
                .FirstOrDefaultAsync(
                    p => p.UserId == smUserId && p.OnboardingCompletedAt != null,
                    cancellationToken);
            if (profile is not null)
            {
                await ApplyResolvedSalesManagerAsync(profile, branch, orgId, cancellationToken);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(registration.SalesManagerTrackingCode))
        {
            return;
        }

        var code = registration.SalesManagerTrackingCode.Trim().ToUpperInvariant();

        if (code.StartsWith(AmbassadeurCommissionRules.TrackingCodePrefix, StringComparison.Ordinal))
        {
            await ApplyAmbassadeurReferralAsync(code, branch, orgId, cancellationToken);
            return;
        }

        profile = await _db.SalesManagerProfiles
            .FirstOrDefaultAsync(
                p => p.TrackingCode != null
                     && p.TrackingCode.ToUpper() == code
                     && p.OnboardingCompletedAt != null,
                cancellationToken);

        if (profile is null)
        {
            // Ambassadeur codes may also be stored without AM- prefix edge cases — try Ambassadeur.
            if (await ApplyAmbassadeurReferralAsync(code, branch, orgId, cancellationToken))
            {
                return;
            }

            _logger.LogWarning(
                "Unknown or incomplete salesmanager tracking code {Code} on registration {Id}",
                code, registration.Id);
            return;
        }

        await ApplyResolvedSalesManagerAsync(profile, branch, orgId, cancellationToken);
    }

    private async Task ApplyResolvedSalesManagerAsync(
        SalesManagerProfile profile,
        Company branch,
        Guid? orgId,
        CancellationToken cancellationToken)
    {
        branch.ReferredBySalesManagerUserId = profile.UserId;
        branch.FirstYearStartedAt = DateTime.UtcNow;
        branch.SalesAttributedAtUtc ??= DateTime.UtcNow;
        // Only the publishing vestiging gets the one-time start-highlight (not the org pot).
        branch.PendingStartHighlightBonus = true;
        await SnapshotCommissionTermsAsync(branch, profile.UserId, cancellationToken);

        if (orgId is Guid oid)
        {
            var org = await _db.Companies.FirstOrDefaultAsync(c => c.Id == oid, cancellationToken)
                      ?? _db.Companies.Local.FirstOrDefault(c => c.Id == oid);
            if (org is not null)
            {
                org.ReferredBySalesManagerUserId = profile.UserId;
                org.FirstYearStartedAt ??= DateTime.UtcNow;
                org.SalesAttributedAtUtc ??= DateTime.UtcNow;
                await SnapshotCommissionTermsAsync(org, profile.UserId, cancellationToken);
            }
        }

        // Reserve founder slot early (1–10) so the bonus can credit after €2500 payment.
        if (branch.FirstYearSupplierSlot is null)
        {
            var usedSlots = await _db.Companies
                .Where(c => c.FirstYearSupplierSlot != null)
                .Select(c => c.FirstYearSupplierSlot!.Value)
                .ToListAsync(cancellationToken);

            usedSlots.AddRange(_db.Companies.Local
                .Where(c => c.FirstYearSupplierSlot != null && c.Id != branch.Id)
                .Select(c => c.FirstYearSupplierSlot!.Value));

            var next = Enumerable.Range(1, SalesCommissionRules.MaxFounderSlots)
                .FirstOrDefault(s => !usedSlots.Contains(s));
            if (next > 0)
            {
                branch.FirstYearSupplierSlot = next;
            }
        }
    }

    private async Task<bool> ApplyAmbassadeurReferralAsync(
        string code,
        Company branch,
        Guid? orgId,
        CancellationToken cancellationToken)
    {
        var profile = await _db.AmbassadeurProfiles
            .FirstOrDefaultAsync(
                p => p.TrackingCode != null
                     && p.TrackingCode.ToUpper() == code
                     && p.OnboardingCompletedAt != null,
                cancellationToken);
        if (profile is null)
        {
            return false;
        }

        var settings = await _db.AmbassadeurSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var threshold = settings?.CandidateThreshold ?? AmbassadeurCommissionRules.DefaultCandidateThreshold;
        var percentPer = settings?.PercentPerThreshold ?? AmbassadeurCommissionRules.DefaultPercentPerThreshold;
        var maxPct = settings?.MaxCommissionPercentage ?? AmbassadeurCommissionRules.DefaultMaxCommissionPercentage;
        var candidateCount = await _db.Users.AsNoTracking()
            .CountAsync(u => u.ReferredByAmbassadeurUserId == profile.UserId, cancellationToken);
        var percentage = AmbassadeurCommissionRules.ResolveCurrentPercentage(
            candidateCount,
            profile.BaseCommissionPercentage,
            threshold,
            percentPer,
            maxPct,
            profile.CommissionPercentageOverride);
        var rate = AmbassadeurCommissionRules.PercentageToRate(percentage);

        branch.ReferredByAmbassadeurUserId = profile.UserId;
        branch.FirstYearStartedAt ??= DateTime.UtcNow;
        branch.SalesAttributedAtUtc ??= DateTime.UtcNow;
        branch.PendingStartHighlightBonus = true;
        branch.CommissionAmbassadeurRateSnapshot = rate;
        branch.CommissionDurationDaysSnapshot ??= SalesCommissionRules.DefaultCommissionDurationDays;
        branch.CommissionTermsSnapshottedAtUtc ??= DateTime.UtcNow;

        if (orgId is Guid oid)
        {
            var org = await _db.Companies.FirstOrDefaultAsync(c => c.Id == oid, cancellationToken)
                      ?? _db.Companies.Local.FirstOrDefault(c => c.Id == oid);
            if (org is not null)
            {
                org.ReferredByAmbassadeurUserId = profile.UserId;
                org.FirstYearStartedAt ??= DateTime.UtcNow;
                org.SalesAttributedAtUtc ??= DateTime.UtcNow;
                org.CommissionAmbassadeurRateSnapshot ??= rate;
                org.CommissionDurationDaysSnapshot ??= SalesCommissionRules.DefaultCommissionDurationDays;
                org.CommissionTermsSnapshottedAtUtc ??= DateTime.UtcNow;
            }
        }

        return true;
    }

    private async Task ApplyPartnerReferralAsync(
        CompanyRegistration registration,
        Company branch,
        Guid? orgId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(registration.PartnerTrackingCode))
        {
            return;
        }

        var applied = await _partnerAffiliates.ApplyReferralAsync(
            branch, registration.PartnerTrackingCode, cancellationToken);
        if (!applied)
        {
            _logger.LogWarning(
                "Unknown or already claimed partner tracking code {Code} on registration {Id}",
                registration.PartnerTrackingCode, registration.Id);
            return;
        }

        if (orgId is Guid oid)
        {
            var org = await _db.Companies.FirstOrDefaultAsync(c => c.Id == oid, cancellationToken)
                      ?? _db.Companies.Local.FirstOrDefault(c => c.Id == oid);
            if (org is not null)
            {
                await _partnerAffiliates.ApplyReferralAsync(org, registration.PartnerTrackingCode, cancellationToken);
            }
        }
    }

    private async Task SnapshotCommissionTermsAsync(
        Company company,
        Guid directSalesManagerUserId,
        CancellationToken cancellationToken)
    {
        if (company.CommissionTermsSnapshottedAtUtc is not null)
        {
            return;
        }

        var settings = await _db.SalesCommercialSettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var standardYear1 = settings?.DirectCommissionRate
                            ?? SalesCommissionRules.DefaultDirectCommissionRate;
        var referredYear1 = settings?.ReferredYear1DirectCommissionRate
                            ?? SalesCommissionRules.DefaultReferredYear1DirectCommissionRate;
        var indirectRate = settings?.IndirectCommissionRate
                           ?? SalesCommissionRules.DefaultIndirectCommissionRate;
        var durationDays = settings?.CommissionDurationDays > 0
            ? settings.CommissionDurationDays
            : SalesCommissionRules.DefaultCommissionDurationDays;

        var upline = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => p.UserId == directSalesManagerUserId)
            .Select(p => p.ReferredBySalesManagerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        var directRate = SalesCommissionRules.Year1RateForSalesManager(
            wasReferred: upline is not null,
            standardYear1,
            referredYear1);

        company.CommissionIndirectSalesManagerUserId = upline;
        company.CommissionDirectRateSnapshot = Math.Max(0m, directRate);
        company.CommissionIndirectRateSnapshot = upline is not null ? Math.Max(0m, indirectRate) : 0m;
        company.CommissionDurationDaysSnapshot = durationDays;
        company.CommissionTermsSnapshottedAtUtc = DateTime.UtcNow;
    }

    private static void ApplyKvkVerificationState(Company company, CompanyRegistration registration)
    {
        company.KvkVerificationStatus = registration.KvkVerificationStatus;
        if (registration.KvkVerificationStatus == KvkVerificationStatus.Verified)
        {
            company.KvkVerifiedAtUtc = DateTime.UtcNow;
        }
        else
        {
            company.KvkLastVerificationAttemptAtUtc = DateTime.UtcNow;
            company.KvkVerificationAttempts = 0;
        }

        company.LocationSource = ResolveLocationSource(registration);
    }

    /// <summary>
    /// Prefills company branches from KVK SBI codes when empty (D12). Stored on the root organisation.
    /// </summary>
    private async Task PrefillWorkTypesFromSbiAsync(
        Company root,
        CompanyRegistration registration,
        IReadOnlyList<string>? sbiCodes,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(root.WorkTypeLabels))
        {
            return;
        }

        var codes = sbiCodes;
        if (codes is null || codes.Count == 0)
        {
            try
            {
                var kvk = await _kvk.GetByKvkNumberAsync(registration.KvkNumber, cancellationToken);
                codes = kvk?.EffectiveSbiCodes;
            }
            catch
            {
                codes = null;
            }
        }

        if ((codes is null || codes.Count == 0) && !string.IsNullOrWhiteSpace(registration.PrimarySbiCode))
        {
            codes = [registration.PrimarySbiCode!];
        }

        var labels = SbiWorkTypeMap.Map(codes);
        if (labels.Count > 0)
        {
            root.WorkTypeLabels = WorkTypeLabels.CombineStoredForCompany(labels);
        }
    }

    private static CompanyLocationSource ResolveLocationSource(CompanyRegistration registration)
    {
        if (registration.LocationUnknown
            || (registration.Latitude == 0d && registration.Longitude == 0d
                && registration.KvkVerificationStatus == KvkVerificationStatus.Pending))
        {
            return CompanyLocationSource.Unknown;
        }

        return registration.KvkVerificationStatus == KvkVerificationStatus.Pending
            ? CompanyLocationSource.Pdok
            : CompanyLocationSource.Kvk;
    }

    private static KvkEstablishmentResult BuildPendingEstablishmentSnapshot(
        RegistrationSubmitRequest request,
        string kvkNumber)
    {
        var name = request.ManualEstablishmentName?.Trim();
        var address = request.ManualEstablishmentAddress?.Trim();
        var establishmentNumber = request.ManualEstablishmentNumber?.Trim();

        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(address)
            || string.IsNullOrWhiteSpace(establishmentNumber))
        {
            throw new ArgumentException(
                "Bij KVK-storing zijn vestigingsnaam, adres en vestigingsnummer verplicht.");
        }

        // Normalize establishment number to 4 digits when numeric.
        var digits = new string(establishmentNumber.Where(char.IsDigit).ToArray());
        if (digits.Length is > 0 and <= 4)
        {
            establishmentNumber = digits.PadLeft(4, '0');
        }

        var normalizedKvk = new string(kvkNumber.Where(char.IsDigit).ToArray());
        if (normalizedKvk.Length != 8)
        {
            throw new ArgumentException("KVK-nummer moet 8 cijfers zijn.");
        }

        // Always derive establishment id server-side — never accept a client override
        // that could squat on another vestiging during an outage.
        var composedId = $"{normalizedKvk}_{establishmentNumber}";

        // Never pin at the NL centre. Missing coordinates → 0,0 + LocationUnknown on the company.
        var lat = request.ManualLatitude ?? 0d;
        var lng = request.ManualLongitude ?? 0d;

        return new KvkEstablishmentResult(
            normalizedKvk,
            establishmentNumber,
            composedId,
            name,
            address,
            lat,
            lng,
            IsInUse: false,
            SbiCodes: null);
    }


    /// <summary>D15: whether the confirmed contact e-mail matches a KVK website domain.</summary>
    public async Task<bool> MatchesBusinessEmailDomainAsync(
        string kvkNumber,
        string contactEmail,
        CancellationToken cancellationToken = default)
    {
        if (FreeMailDomains.IsFreeMail(contactEmail))
        {
            return false;
        }

        var profile = await _kvk.GetProfileAsync(kvkNumber, cancellationToken);
        if (profile.Status != KvkLookupStatus.Ok || profile.Websites.Count == 0)
        {
            return false;
        }

        return DomainMatch.EmailMatchesAnyWebsite(contactEmail, profile.Websites);
    }

    private async Task<RegistrationSubmitRequest> GeocodeManualRequestAsync(
        RegistrationSubmitRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ManualLatitude is not null && request.ManualLongitude is not null)
        {
            return request with { LocationUnknown = false };
        }

        var query = string.Join(" ", new[]
        {
            request.ManualEstablishmentAddress,
            request.ManualEstablishmentName
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (_geocoder is null || string.IsNullOrWhiteSpace(query))
        {
            return request with { ManualLatitude = null, ManualLongitude = null, LocationUnknown = true };
        }

        var geo = await _geocoder.GeocodeAsync(query, cancellationToken);
        if (geo is null)
        {
            return request with { ManualLatitude = null, ManualLongitude = null, LocationUnknown = true };
        }

        return request with
        {
            ManualLatitude = geo.Latitude,
            ManualLongitude = geo.Longitude,
            LocationUnknown = false
        };
    }

    private static bool ResolveIsIntermediarySbi(
        IReadOnlyList<string> sbiCodes,
        bool? manualChoice)
    {
        var has78 = KvkSbiClassification.IsIntermediary(sbiCodes);
        if (!has78)
        {
            return false;
        }

        if (!KvkSbiClassification.HasNonIntermediary(sbiCodes))
        {
            // Only SBI 78* → always intermediair.
            return true;
        }

        // Mixed: honour wizard choice; default to main activity when absent.
        if (manualChoice is bool chosen)
        {
            return chosen;
        }

        return KvkSbiClassification.IsMainActivityIntermediary(sbiCodes);
    }

    private static string? SerializeSelectedIds(IReadOnlyList<string>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return null;
        }

        var cleaned = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned);
    }

    private static HashSet<string>? ParseSelectedIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return new HashSet<string>(
                list.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// After a deferred KVK verification succeeds for an organisation branch,
    /// claim free sibling vestigingen under the parent (same as verified activation).
    /// </summary>
    internal async Task ClaimSiblingEstablishmentsForOrgAsync(
        string kvkNumber,
        Guid orgId,
        Guid excludeBranchId,
        CancellationToken cancellationToken)
    {
        // Retry path has no registration selection — claim all free siblings (legacy).
        var synthetic = new CompanyRegistration
        {
            KvkNumber = kvkNumber,
            SelectedEstablishmentIdsJson = null
        };
        await ClaimSiblingEstablishmentsAsync(kvkNumber, orgId, excludeBranchId, synthetic, cancellationToken);
    }
}
