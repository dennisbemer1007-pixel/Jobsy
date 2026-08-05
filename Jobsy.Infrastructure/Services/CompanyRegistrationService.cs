using System.Net;
using System.Security.Cryptography;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyRegistrationService : ICompanyRegistrationService
{
    public static readonly TimeSpan ActivationTokenTtl = TimeSpan.FromHours(48);

    /// <summary>Ledger note for the one-time registration welcome grant (1 token).</summary>
    public const string WelcomeTokenNote = "Welkomsttoken toegekend bij accountactivatie";

    public const decimal WelcomeTokenAmount = 1m;

    private readonly JobsyDbContext _db;
    private readonly IKvkService _kvk;
    private readonly IEmailService _email;
    private readonly ITokenLedgerService _ledger;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<CompanyRegistrationService> _logger;

    public CompanyRegistrationService(
        JobsyDbContext db,
        IKvkService kvk,
        IEmailService email,
        ITokenLedgerService ledger,
        IPlatformFeatureService features,
        ILogger<CompanyRegistrationService> logger)
    {
        _db = db;
        _kvk = kvk;
        _email = email;
        _ledger = ledger;
        _features = features;
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

        RegistrationPasswordRules.Validate(request.Password);

        string? trackingCode = null;
        if (!string.IsNullOrWhiteSpace(request.SalesManagerTrackingCode))
        {
            trackingCode = request.SalesManagerTrackingCode.Trim().ToUpperInvariant();
            var validSm = await _db.SalesManagerProfiles.AsNoTracking().AnyAsync(
                p => p.TrackingCode != null
                     && p.TrackingCode.ToUpper() == trackingCode
                     && p.OnboardingCompletedAt != null
                     && p.AgreementSignedAt != null,
                cancellationToken);
            var validAm = !validSm && await _db.AmbassadeurProfiles.AsNoTracking().AnyAsync(
                p => p.TrackingCode != null
                     && p.TrackingCode.ToUpper() == trackingCode
                     && p.OnboardingCompletedAt != null
                     && p.AgreementSignedAt != null,
                cancellationToken);
            if (!validSm && !validAm)
            {
                throw new ArgumentException(
                    "Deze trackingcode is onbekend of nog niet actief. Laat het veld leeg of vul een geldige code in.");
            }
        }

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

        var isIntermediarySbi = KvkSbiClassification.IsIntermediary(sbiCodes);
        var primarySbi = KvkSbiClassification.PrimarySbiCode(sbiCodes);

        // Soft enumeration resistance: same generic message for existing email.
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken)
            || await _db.LocalAuthCredentials.AnyAsync(c => c.Email == email, cancellationToken)
            || await _db.CompanyRegistrations.AnyAsync(
                r => r.ContactEmail == email
                     && (r.Status == CompanyRegistrationStatus.PendingActivation
                         || r.Status == CompanyRegistrationStatus.TakeoverPending),
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Als dit e-mailadres nog niet bekend is, ontvang je zo een bevestiging. Zo niet, probeer in te loggen of een ander adres.");
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
            PasswordHash = JobsyPasswordHasher.Hash(request.Password!),
            PrimarySbiCode = primarySbi,
            IsIntermediarySbi = isIntermediarySbi,
            KvkVerificationStatus = kvkVerificationStatus,
            ConsentAcceptedAt = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            SalesManagerTrackingCode = trackingCode,
            CreatedAt = DateTime.UtcNow
        };

        if (existing is not null || match.IsInUse)
        {
            if (existing is null)
            {
                throw new InvalidOperationException("Vestiging staat als in-gebruik gemarkeerd maar is niet gevonden.");
            }

            registration.Status = CompanyRegistrationStatus.TakeoverPending;
            var takeover = new EstablishmentTakeoverRequest
            {
                Id = Guid.NewGuid(),
                RegistrationId = registration.Id,
                TargetCompanyId = existing.Id,
                Status = TakeoverRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.CompanyRegistrations.Add(registration);
            _db.EstablishmentTakeoverRequests.Add(takeover);
            await _db.SaveChangesAsync(cancellationToken);

            // AVG/auth: verify requester e-mail before owners see the takeover or credentials are applied.
            var featuresTakeover = await _features.GetAsync(cancellationToken);
            var verifyUrl = BuildActivationUrl(registration.ActivationToken, featuresTakeover.PublicWebBaseUrl);
            var takeoverMail = await SendTakeoverEmailVerificationAsync(registration, existing, verifyUrl, cancellationToken);

            return new RegistrationSubmitResult(
                registration.Id,
                registration.Status,
                RequiresTakeover: true,
                Message: takeoverMail.DeliveredViaProvider
                    ? "Deze vestiging is al geregistreerd. Bevestig eerst je e-mailadres; daarna sturen we het overnameverzoek naar de huidige eigenaar."
                    : featuresTakeover.ExposeRegistrationActivationLinks
                        ? "Deze vestiging is al geregistreerd. We konden geen e-mail afleveren — gebruik de verificatielink hieronder."
                        : "Deze vestiging is al geregistreerd. Bevestig je e-mailadres via de mail (check ook spam). Lukt dat niet, neem contact op met support.",
                ActivationUrl: featuresTakeover.ExposeRegistrationActivationLinks ? verifyUrl : null);
        }

        registration.Status = CompanyRegistrationStatus.PendingActivation;
        _db.CompanyRegistrations.Add(registration);
        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var activationUrl = BuildActivationUrl(registration.ActivationToken, features.PublicWebBaseUrl);
        var mailDelivery = await SendActivationEmailAsync(registration, activationUrl, cancellationToken);

        var roleHint = isIntermediarySbi
            ? "Na verificatie kun je direct aan de slag als intermediair."
            : "Na verificatie kun je direct aan de slag — je eerste token is gratis.";

        var kvkHint = kvkVerificationStatus == KvkVerificationStatus.Pending
            ? " Je account staat intern op KVK-verificatie in afwachting; we controleren dit automatisch zodra de KVK-dienst weer bereikbaar is."
            : string.Empty;

        // AVG/auth: never return the activation URL on anonymous submit unless the
        // explicit demo/admin flag is on — otherwise email verification is bypassable.
        var exposeLink = features.ExposeRegistrationActivationLinks;

        string message;
        if (mailDelivery.DeliveredViaProvider)
        {
            message = $"Controleer je e-mail voor de verificatielink. {roleHint}{kvkHint}";
        }
        else if (exposeLink)
        {
            message = $"We konden geen e-mail afleveren — gebruik de verificatielink hieronder. {roleHint}{kvkHint}";
        }
        else
        {
            message =
                $"Controleer je e-mail voor de verificatielink (inclusief spam). {roleHint}{kvkHint}";
        }

        return new RegistrationSubmitResult(
            registration.Id,
            registration.Status,
            RequiresTakeover: false,
            Message: message,
            ActivationUrl: exposeLink ? activationUrl : null);
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

        if (DateTime.UtcNow - registration.CreatedAt > ActivationTokenTtl)
        {
            registration.Status = CompanyRegistrationStatus.Cancelled;
            ClearPendingSecrets(registration);
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Deze activatielink is verlopen. Registreer opnieuw.");
        }

        if (registration.Status == CompanyRegistrationStatus.TakeoverPending)
        {
            return await CompleteTakeoverEmailVerificationAsync(registration, cancellationToken);
        }

        if (registration.Status != CompanyRegistrationStatus.PendingActivation)
        {
            throw new InvalidOperationException(
                $"Registratie kan niet worden geactiveerd (status: {registration.Status}).");
        }

        if (await _db.Companies.AnyAsync(
                c => c.KvkEstablishmentId == registration.KvkEstablishmentId, cancellationToken))
        {
            throw new InvalidOperationException(
                "Deze vestiging is ondertussen al geregistreerd. Dien opnieuw een overnameverzoek in.");
        }

        var passwordHash = ResolvePasswordHash(registration, out var temporaryPassword);
        var usedChosenPassword = temporaryPassword is null;
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

        await GrantWelcomeTokenAsync(branchId, user.Id, cancellationToken);

        await SendActivatedCredentialsEmailAsync(registration, temporaryPassword, cancellationToken);

        _logger.LogInformation("Activated registration {Id} for {Email}", registration.Id, EmailServiceStub.RedactEmail(registration.ContactEmail));

        return await BuildActivationResultAsync(
            registration, temporaryPassword ?? string.Empty, usedChosenPassword, cancellationToken);
    }

    /// <summary>
    /// Credits 1 welcome token on the registered vestiging so the first publish is free.
    /// Idempotent via <see cref="Company.HasReceivedWelcomeToken"/>.
    /// </summary>
    private async Task GrantWelcomeTokenAsync(
        Guid branchCompanyId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstAsync(c => c.Id == branchCompanyId, cancellationToken);
        if (company.HasReceivedWelcomeToken)
        {
            return;
        }

        company.HasReceivedWelcomeToken = true;
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
    }

    public async Task<IReadOnlyList<TakeoverInboxItem>> ListPendingTakeoversAsync(
        IReadOnlyCollection<Guid> accessibleCompanyIds,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _db.EstablishmentTakeoverRequests
            .AsNoTracking()
            .Include(t => t.Registration)
            .Include(t => t.TargetCompany)
            .Where(t => t.Status == TakeoverRequestStatus.Pending);

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

        if (registration.Scope == RegistrationScope.Organization
            && !isAdmin
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
        var passwordHash = ResolvePasswordHash(registration, out var temporaryPassword);

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
                    Type = CompanyType.Employer
                };
                _db.Companies.Add(org);
                target.ParentCompanyId = org.Id;
                orgId = org.Id;
                await WmlSalaryTableService.EnsureForCompanyAsync(_db, org.Id, cancellationToken);
            }

            await ClaimSiblingEstablishmentsAsync(registration.KvkNumber, orgId.Value, target.Id, cancellationToken);
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

        // Transfer: prior employers lose access to the acquired vestiging.
        // Parent-org memberships stay intact when reusing an existing organization shell.
        await RevokePriorEmployerAccessAsync(
            companyIds: [branchId],
            exceptUserId: user.Id,
            cancellationToken);

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

        var features = await _features.GetAsync(cancellationToken);
        var loginUrl = features.PublicWebBaseUrl.TrimEnd('/') + "/login";
        var safeName = WebUtility.HtmlEncode(registration.ContactName);
        var passwordBlock = temporaryPassword is null
            ? """
              <p>Log in met het wachtwoord dat je bij registratie hebt gekozen, of via
              <strong>Microsoft Entra</strong> met hetzelfde geverifieerde e-mailadres.</p>
              """
            : $"""
               <p>Log in met <code>{WebUtility.HtmlEncode(registration.ContactEmail)}</code>.
               Je eenmalige tijdelijke wachtwoord (bewaar dit veilig; het wordt niet opnieuw getoond):</p>
               <p><code>{WebUtility.HtmlEncode(temporaryPassword)}</code></p>
               <p><em>Wijzig dit wachtwoord zo snel mogelijk.</em></p>
               """;
        await _email.SendAsync(new EmailMessage(
            registration.ContactEmail,
            "Overname goedgekeurd — Lobsy",
            $"""
             <p>Hoi {safeName},</p>
             <p>Je overnameverzoek voor <strong>{WebUtility.HtmlEncode(target.Name)}</strong> is goedgekeurd.</p>
             <p>Tokens, vacatures en geschiedenis blijven gekoppeld aan de vestiging
             {(orgId is not null ? "onder de organisatie" : "")}.</p>
             {passwordBlock}
             <p><a href="{WebUtility.HtmlEncode(loginUrl)}">Naar inloggen</a></p>
             <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
             """,
            "TakeoverApproved"), cancellationToken);

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

        var safeName = WebUtility.HtmlEncode(takeover.Registration.ContactName);
        await _email.SendAsync(new EmailMessage(
            takeover.Registration.ContactEmail,
            "Overname afgewezen — Lobsy",
            $"""
             <p>Hoi {safeName},</p>
             <p>Je overnameverzoek voor <strong>{WebUtility.HtmlEncode(takeover.TargetCompany.Name)}</strong> is afgewezen.</p>
             <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
             """,
            "TakeoverRejected"), cancellationToken);

        return new TakeoverDecisionResult(
            takeover.Id,
            TakeoverRequestStatus.Rejected,
            "Overname afgewezen.",
            null,
            null);
    }

    private async Task<(User User, Guid? OrgId, Guid BranchId)> ProvisionCompaniesAndUserAsync(
        CompanyRegistration registration,
        string passwordHash,
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
                Type = CompanyType.Intermediary
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
                Type = CompanyType.Employer
            };
            ApplyKvkVerificationState(org, registration);
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
                Type = CompanyType.Employer,
                ParentCompanyId = org.Id
            };
            ApplyKvkVerificationState(branch, registration);
            _db.Companies.Add(branch);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, branch.Id, cancellationToken);

            // Sibling claim only when KVK is verified — pending registrations skip auto-claim.
            if (registration.KvkVerificationStatus == KvkVerificationStatus.Verified)
            {
                await ClaimSiblingEstablishmentsAsync(
                    registration.KvkNumber, org.Id, branch.Id, cancellationToken);
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
                Type = CompanyType.Employer
            };
            ApplyKvkVerificationState(branch, registration);
            _db.Companies.Add(branch);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, branch.Id, cancellationToken);
        }

        await ApplySalesManagerReferralAsync(registration, branch, orgId, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        var establishments = await _kvk.GetEstablishmentsAsync(kvkNumber, cancellationToken);
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

        foreach (var est in establishments)
        {
            if (usedIds.Contains(est.KvkEstablishmentId, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            // Do not steal vestigingen that another registrant already started.
            if (pendingEstablishmentIds.Contains(est.KvkEstablishmentId, StringComparer.OrdinalIgnoreCase))
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
                Type = CompanyType.Employer,
                ParentCompanyId = orgId,
                KvkVerificationStatus = KvkVerificationStatus.Verified,
                KvkVerifiedAtUtc = DateTime.UtcNow
            };
            _db.Companies.Add(sibling);
            await WmlSalaryTableService.EnsureForCompanyAsync(_db, sibling.Id, cancellationToken);
            usedIds.Add(est.KvkEstablishmentId);
        }

        _ = excludeBranchId;
    }

    private async Task<User> CreateRegistrationUserAsync(
        CompanyRegistration registration,
        UserRole role,
        Guid primaryCompanyId,
        string passwordHash,
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

        _db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = email,
            PasswordHash = passwordHash
        });

        return user;
    }

    private async Task RevokePriorEmployerAccessAsync(
        IReadOnlyList<Guid> companyIds,
        Guid exceptUserId,
        CancellationToken cancellationToken)
    {
        var idSet = companyIds.ToHashSet();
        var affected = await _db.Users
            .Include(u => u.CompanyMemberships)
            .Where(u => u.Id != exceptUserId && u.IsActive && JobsyRoles.IsEmployer(u.Role))
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
        string temporaryPassword,
        bool usedChosenPassword,
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

        return new RegistrationActivationResult(
            registration.Id,
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            companyIds,
            temporaryPassword,
            registration.CreatedOrganizationCompanyId,
            registration.CreatedBranchCompanyId,
            usedChosenPassword);
    }

    private async Task<RegistrationActivationResult> CompleteTakeoverEmailVerificationAsync(
        CompanyRegistration registration,
        CancellationToken cancellationToken)
    {
        if (registration.ContactEmailVerifiedAt is not null)
        {
            throw new InvalidOperationException(
                "Dit e-mailadres is al bevestigd. Het overnameverzoek wacht op de huidige eigenaar.");
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

        await _db.SaveChangesAsync(cancellationToken);
        await NotifyTakeoverRequestedAsync(registration, takeover.TargetCompany, cancellationToken);

        _logger.LogInformation(
            "Takeover e-mail verified for registration {Id} ({Email})",
            registration.Id,
            EmailServiceStub.RedactEmail(registration.ContactEmail));

        return new RegistrationActivationResult(
            registration.Id,
            Guid.Empty,
            registration.ContactEmail,
            registration.ContactName,
            Role: string.Empty,
            CompanyId: null,
            CompanyIds: Array.Empty<Guid>(),
            TemporaryPassword: string.Empty,
            OrganizationCompanyId: null,
            BranchCompanyId: null,
            UsedChosenPassword: true,
            EmailVerifiedAwaitingTakeover: true);
    }

    private async Task<EmailDeliveryResult> SendTakeoverEmailVerificationAsync(
        CompanyRegistration registration,
        Company existing,
        string verifyUrl,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(registration.ContactName);
        return await _email.SendAsync(new EmailMessage(
            registration.ContactEmail,
            "Bevestig je e-mail voor overnameverzoek — Lobsy",
            $"""
             <p>Hoi {safeName},</p>
             <p>Vestiging <strong>{WebUtility.HtmlEncode(existing.Name)}</strong> is al geregistreerd.
             Bevestig eerst je e-mailadres. Daarna sturen we het overnameverzoek naar de huidige eigenaar.</p>
             <p><a href="{WebUtility.HtmlEncode(verifyUrl)}">E-mailadres bevestigen</a></p>
             <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
             """,
            "TakeoverEmailVerification"), cancellationToken);
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
        var inboxUrl = features.PublicWebBaseUrl.TrimEnd('/') + "/employer/takeovers";
        foreach (var ownerEmail in owners)
        {
            await _email.SendAsync(new EmailMessage(
                ownerEmail,
                "Overnameverzoek vestiging — Lobsy",
                $"""
                 <p>Er is een overnameverzoek voor <strong>{WebUtility.HtmlEncode(existing.Name)}</strong>
                 ({WebUtility.HtmlEncode(existing.KvkEstablishmentId ?? "")}).</p>
                 <p>Aanvrager: {WebUtility.HtmlEncode(registration.ContactName)}
                 ({WebUtility.HtmlEncode(registration.ContactEmail)}).</p>
                 <p><a href="{WebUtility.HtmlEncode(inboxUrl)}">Bekijk verzoeken</a></p>
                 <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
                 """,
                "TakeoverRequest"), cancellationToken);
        }

        var safeName = WebUtility.HtmlEncode(registration.ContactName);
        await _email.SendAsync(new EmailMessage(
            registration.ContactEmail,
            "Overnameverzoek ingediend — Lobsy",
            $"""
             <p>Hoi {safeName},</p>
             <p>Vestiging <strong>{WebUtility.HtmlEncode(existing.Name)}</strong> is al in gebruik.
             We hebben een overnameverzoek gestuurd naar de huidige eigenaar.</p>
             <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
             """,
            "TakeoverSubmitted"), cancellationToken);
    }

    private async Task<EmailDeliveryResult> SendActivationEmailAsync(
        CompanyRegistration registration,
        string activationUrl,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(registration.ContactName);
        return await _email.SendAsync(new EmailMessage(
            registration.ContactEmail,
            "Bevestig je e-mailadres — Lobsy",
            $"""
             <p>Hoi {safeName},</p>
             <p>Welkom bij Lobsy! Bevestig je e-mailadres om je bedrijfsregistratie voor
             <strong>{WebUtility.HtmlEncode(registration.EstablishmentName)}</strong> te activeren.</p>
             <p>Na bevestiging kun je direct aan de slag — je eerste token is helemaal gratis,
             zodat je meteen een vacature kunt plaatsen.</p>
             <p><a href="{WebUtility.HtmlEncode(activationUrl)}">E-mailadres bevestigen</a></p>
             <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
             """,
            "RegistrationActivation"), cancellationToken);
    }

    private async Task SendActivatedCredentialsEmailAsync(
        CompanyRegistration registration,
        string? temporaryPassword,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var loginUrl = features.PublicWebBaseUrl.TrimEnd('/') + "/login";
        var safeName = WebUtility.HtmlEncode(registration.ContactName);
        var passwordBlock = temporaryPassword is null
            ? """
              <p>Log in met het wachtwoord dat je bij registratie hebt gekozen, of via
              <strong>Microsoft Entra</strong> / Google met hetzelfde geverifieerde e-mailadres.</p>
              """
            : $"""
               <p>Gebruik dit eenmalige tijdelijke wachtwoord (niet opnieuw zichtbaar in de app):</p>
               <p><code>{WebUtility.HtmlEncode(temporaryPassword)}</code></p>
               <p><em>Wijzig dit wachtwoord zo snel mogelijk.</em></p>
               """;
        await _email.SendAsync(new EmailMessage(
            registration.ContactEmail,
            "Geslaagd — je Lobsy-account is actief!",
            $"""
             <p>Hoi {safeName},</p>
             <p>Geslaagd! Je account voor <strong>{WebUtility.HtmlEncode(registration.EstablishmentName)}</strong>
             is geactiveerd. Je kunt direct aan de slag.</p>
             <p>Je hebt van ons je eerste token helemaal gratis gekregen — daarmee plaats je meteen je eerste vacature.</p>
             <p>Je kunt inloggen met e-mail/wachtwoord of met <strong>Microsoft Entra</strong> /
             Google op <code>{WebUtility.HtmlEncode(registration.ContactEmail)}</code>.</p>
             {passwordBlock}
             <p><a href="{WebUtility.HtmlEncode(loginUrl)}">Naar inloggen</a></p>
             <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
             """,
            "RegistrationCredentials"), cancellationToken);
    }

    /// <summary>
    /// Uses the password hash stored at submit when present; otherwise generates a temporary password
    /// (legacy / takeover edge cases). Returns the hash and optional plaintext for e-mail only.
    /// </summary>
    private static string ResolvePasswordHash(CompanyRegistration registration, out string? temporaryPassword)
    {
        if (!string.IsNullOrWhiteSpace(registration.PasswordHash))
        {
            temporaryPassword = null;
            return registration.PasswordHash;
        }

        temporaryPassword = GenerateTemporaryPassword();
        return JobsyPasswordHasher.Hash(temporaryPassword);
    }

    /// <summary>
    /// AVG: drop one-time activation material and pending password hashes when a registration
    /// is activated, rejected, cancelled, or expired.
    /// </summary>
    private static void ClearPendingSecrets(CompanyRegistration registration)
    {
        registration.ActivationToken = string.Empty;
        registration.PasswordHash = null;
    }

    private static string GenerateTemporaryPassword()
        => "J!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8));

    private static string BuildActivationUrl(string token, string publicWebBaseUrl)
        => $"{publicWebBaseUrl.TrimEnd('/')}/register/activate?token={Uri.EscapeDataString(token)}";

    private async Task ApplySalesManagerReferralAsync(
        CompanyRegistration registration,
        Company branch,
        Guid? orgId,
        CancellationToken cancellationToken)
    {
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

        var profile = await _db.SalesManagerProfiles
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

        branch.ReferredBySalesManagerUserId = profile.UserId;
        branch.FirstYearStartedAt = DateTime.UtcNow;
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
                org.CommissionAmbassadeurRateSnapshot ??= rate;
                org.CommissionDurationDaysSnapshot ??= SalesCommissionRules.DefaultCommissionDurationDays;
                org.CommissionTermsSnapshottedAtUtc ??= DateTime.UtcNow;
            }
        }

        return true;
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

        var directRate = settings?.DirectCommissionRate
                         ?? SalesCommissionRules.DefaultDirectCommissionRate;
        var indirectRate = settings?.IndirectCommissionRate
                           ?? SalesCommissionRules.DefaultIndirectCommissionRate;
        var durationDays = settings?.CommissionDurationDays > 0
            ? settings.CommissionDurationDays
            : SalesCommissionRules.DefaultCommissionDurationDays;

        var upline = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => p.UserId == directSalesManagerUserId)
            .Select(p => p.ReferredBySalesManagerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        company.CommissionIndirectSalesManagerUserId = upline;
        company.CommissionDirectRateSnapshot = Math.Max(0m, directRate);
        company.CommissionIndirectRateSnapshot = Math.Max(0m, indirectRate);
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

        // Default NL centroid when the user cannot geocode during an outage.
        var lat = request.ManualLatitude ?? 52.1326;
        var lng = request.ManualLongitude ?? 5.2913;

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

    /// <summary>
    /// After a deferred KVK verification succeeds for an organisation branch,
    /// claim free sibling vestigingen under the parent (same as verified activation).
    /// </summary>
    internal async Task ClaimSiblingEstablishmentsForOrgAsync(
        string kvkNumber,
        Guid orgId,
        Guid excludeBranchId,
        CancellationToken cancellationToken)
        => await ClaimSiblingEstablishmentsAsync(kvkNumber, orgId, excludeBranchId, cancellationToken);
}
