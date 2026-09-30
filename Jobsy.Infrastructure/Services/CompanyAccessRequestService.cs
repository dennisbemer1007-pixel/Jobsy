using System.Security.Cryptography;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyAccessRequestService : ICompanyAccessRequestService
{
    private static readonly TimeSpan EmailCodeTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeZoneInfo Dutch = ResolveDutch();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly JobsyDbContext _db;
    private readonly IEmailService _email;
    private readonly IUserNotificationService _notifications;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<CompanyAccessRequestService> _logger;

    public CompanyAccessRequestService(
        JobsyDbContext db,
        IEmailService email,
        IUserNotificationService notifications,
        IPlatformFeatureService features,
        ILogger<CompanyAccessRequestService> logger)
    {
        _db = db;
        _email = email;
        _notifications = notifications;
        _features = features;
        _logger = logger;
    }

    public async Task<AccessRequestSubmitResult> SubmitAsync(
        AccessRequestSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RequesterName)
            || string.IsNullOrWhiteSpace(request.RequesterEmail))
        {
            throw new ArgumentException("Naam en e-mail zijn verplicht.");
        }

        if (request.RequestedRole is not (UserRole.BranchManager or UserRole.RegionalManager or UserRole.EnterpriseManager))
        {
            throw new ArgumentException("Alleen Vestigingsmanager, Regiomanager of Bedrijfsmanager mag worden aangevraagd.");
        }

        if (!string.IsNullOrWhiteSpace(request.Message) && request.Message.Trim().Length > 500)
        {
            throw new ArgumentException("Bericht mag maximaal 500 tekens zijn.");
        }

        var email = request.RequesterEmail.Trim().ToLowerInvariant();
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(request.KvkNumber)
                  ?? throw new ArgumentException("Ongeldig KvK-nummer.");

        Company? company = null;
        if (request.TargetCompanyId is Guid explicitId)
        {
            company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == explicitId, cancellationToken);
        }

        if (company is null && !string.IsNullOrWhiteSpace(request.KvkEstablishmentId))
        {
            var estId = request.KvkEstablishmentId.Trim();
            company = await _db.Companies.FirstOrDefaultAsync(
                c => c.KvkNumber == kvk && c.KvkEstablishmentId == estId, cancellationToken);
        }

        company ??= await _db.Companies
            .Where(c => c.KvkNumber == kvk)
            .OrderBy(c => c.ParentCompanyId == null ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            throw new KeyNotFoundException("Bedrijf niet gevonden op Lobsy.");
        }

        if (!string.Equals(company.KvkNumber, kvk, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("KvK-nummer komt niet overeen met het gekozen bedrijf.");
        }

        if (!await CompanyOccupancy.HasActiveManagingEmployersAsync(_db, company.Id, cancellationToken))
        {
            throw new InvalidOperationException(
                "Dit bedrijf heeft geen actieve beheerder. Registreer het via Bedrijf registreren.");
        }

        var openExists = await _db.CompanyAccessRequests.AnyAsync(
            r => r.TargetCompanyId == company.Id
                 && r.RequesterEmail == email
                 && (r.Status == CompanyAccessRequestStatus.AwaitingEmail
                     || r.Status == CompanyAccessRequestStatus.Open
                     || r.Status == CompanyAccessRequestStatus.Escalated),
            cancellationToken);
        if (openExists)
        {
            throw new InvalidOperationException("Je hebt al een openstaand toegangsverzoek voor dit bedrijf.");
        }

        var companyIds = (request.RequestedCompanyIds ?? []).Distinct().ToList();
        if (companyIds.Count == 0)
        {
            companyIds.Add(company.Id);
        }

        var code = VerificationCodes.CreateNumericCode();
        var row = new CompanyAccessRequest
        {
            Id = Guid.NewGuid(),
            TargetCompanyId = company.Id,
            KvkNumber = kvk,
            RequestedVestigingIdsJson = JsonSerializer.Serialize(companyIds, JsonOpts),
            RequestedRole = request.RequestedRole,
            RequesterName = request.RequesterName.Trim(),
            RequesterFunction = string.IsNullOrWhiteSpace(request.RequesterFunction)
                ? null
                : request.RequesterFunction.Trim(),
            RequesterEmail = email,
            RequesterPhone = string.IsNullOrWhiteSpace(request.RequesterPhone)
                ? null
                : request.RequesterPhone.Trim(),
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            EmailConfirmationCodeHash = VerificationCodes.Hash(code),
            EmailConfirmationExpiresAtUtc = DateTime.UtcNow.Add(EmailCodeTtl),
            EmailConfirmationFailedAttempts = 0,
            RequesterToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            Status = CompanyAccessRequestStatus.AwaitingEmail,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.CompanyAccessRequests.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var mail = TransactionalEmails.AccessRequestEmailVerification(
            features.PublicWebBaseUrl, row.RequesterName, company.Name, code);
        await _email.SendAsync(
            new EmailMessage(row.RequesterEmail, mail.Subject, mail.Html, mail.Category),
            cancellationToken);

        return new AccessRequestSubmitResult(
            row.Id,
            row.Status,
            "We hebben een bevestigingscode gestuurd. Vul die in om je verzoek te versturen.",
            row.EmailConfirmationExpiresAtUtc);
    }

    public async Task<AccessRequestConfirmResult> ConfirmEmailAsync(
        Guid requestId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyAccessRequests
            .Include(r => r.TargetCompany)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Verzoek niet gevonden.");

        if (row.Status != CompanyAccessRequestStatus.AwaitingEmail)
        {
            throw new InvalidOperationException("Dit verzoek is al bevestigd of afgehandeld.");
        }

        if (row.EmailConfirmationExpiresAtUtc is null
            || row.EmailConfirmationExpiresAtUtc < DateTime.UtcNow)
        {
            throw new InvalidOperationException("De bevestigingscode is verlopen. Vraag een nieuw verzoek aan.");
        }

        if (!VerificationCodes.MatchesHash(row.EmailConfirmationCodeHash, code))
        {
            var attempts = row.EmailConfirmationFailedAttempts;
            var dead = VerificationCodes.RegisterFailedAttempt(ref attempts);
            row.EmailConfirmationFailedAttempts = attempts;
            if (dead)
            {
                row.Status = CompanyAccessRequestStatus.Withdrawn;
                row.EmailConfirmationCodeHash = null;
                row.DecisionReason = "Te veel mislukte codepogingen.";
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(
                dead ? "Te veel mislukte pogingen. Vraag opnieuw toegang aan." : "Onjuiste bevestigingscode.");
        }

        row.EmailConfirmedAtUtc = DateTime.UtcNow;
        row.EmailConfirmationCodeHash = null;
        row.EmailConfirmationExpiresAtUtc = null;
        row.EmailConfirmationFailedAttempts = 0;
        row.Status = CompanyAccessRequestStatus.Open;
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyManagersAsync(row, isReminder: false, cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var confirmMail = TransactionalEmails.AccessRequestSubmitted(
            features.PublicWebBaseUrl, row.RequesterName, row.TargetCompany.Name);
        await _email.SendAsync(
            new EmailMessage(row.RequesterEmail, confirmMail.Subject, confirmMail.Html, confirmMail.Category),
            cancellationToken);

        return new AccessRequestConfirmResult(
            row.Id,
            row.Status,
            $"Je aanvraag is verstuurd. {row.TargetCompany.Name} beslist; na 5 werkdagen kijkt Lobsy mee.",
            row.TargetCompany.Name);
    }

    public async Task WithdrawAsync(
        Guid requestId,
        string requesterToken,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyAccessRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Verzoek niet gevonden.");

        if (!VerificationCodes.FixedTimeEquals(row.RequesterToken, requesterToken ?? ""))
        {
            throw new UnauthorizedAccessException("Ongeldige link.");
        }

        if (row.Status is CompanyAccessRequestStatus.Approved
            or CompanyAccessRequestStatus.Rejected
            or CompanyAccessRequestStatus.Expired
            or CompanyAccessRequestStatus.Withdrawn)
        {
            return;
        }

        row.Status = CompanyAccessRequestStatus.Withdrawn;
        row.DecidedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessRequestInboxItem>> ListInboxAsync(
        IReadOnlyCollection<Guid> accessibleCompanyIds,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CompanyAccessRequests
            .AsNoTracking()
            .Include(r => r.TargetCompany)
            .Where(r => r.Status == CompanyAccessRequestStatus.Open
                        || r.Status == CompanyAccessRequestStatus.Escalated);

        if (!isAdmin)
        {
            query = query.Where(r => accessibleCompanyIds.Contains(r.TargetCompanyId));
        }

        var rows = await query.OrderByDescending(r => r.CreatedAtUtc).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return rows.Select(r => new AccessRequestInboxItem(
            r.Id,
            r.TargetCompanyId,
            r.TargetCompany.Name,
            r.RequesterName,
            r.RequesterFunction,
            r.RequesterEmail,
            r.RequestedRole,
            ParseIds(r.RequestedVestigingIdsJson),
            r.Message,
            r.Status,
            r.CreatedAtUtc,
            WorkingDays.CountWorkingDaysBetween(r.CreatedAtUtc, now, Dutch))).ToList();
    }

    public async Task<AccessRequestDecisionResult> ApproveAsync(
        Guid requestId,
        Guid actorUserId,
        UserRole actorRole,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        UserRole? grantedRole,
        IReadOnlyList<Guid>? grantedCompanyIds,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyAccessRequests
            .Include(r => r.TargetCompany)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Verzoek niet gevonden.");

        EnsureDecidable(row);
        EnsureAccess(row, accessibleCompanyIds, isAdmin);

        var role = grantedRole ?? row.RequestedRole;
        if (!EmployerInviteRules.CanAssignRole(isAdmin ? UserRole.Admin : actorRole, role))
        {
            throw new UnauthorizedAccessException("Je mag deze rol niet toekennen.");
        }

        // Manager may lower, never raise above requested.
        if (EmployerInviteRules.Rank(role) > EmployerInviteRules.Rank(row.RequestedRole))
        {
            throw new InvalidOperationException("Je kunt geen hogere rol geven dan aangevraagd.");
        }

        var membershipIds = (grantedCompanyIds ?? ParseIds(row.RequestedVestigingIdsJson)).Distinct().ToList();
        if (membershipIds.Count == 0)
        {
            membershipIds.Add(row.TargetCompanyId);
        }

        if (!isAdmin && accessibleCompanyIds is not null
            && membershipIds.Any(id => !accessibleCompanyIds.Contains(id)))
        {
            throw new UnauthorizedAccessException("Je mag geen toegang geven buiten je eigen vestigingen.");
        }

        var primary = ResolvePrimaryCompany(row.TargetCompany, role, membershipIds);
        var user = await ProvisionInviteUserAsync(
            row.RequesterEmail,
            row.RequesterName,
            role,
            primary,
            membershipIds,
            cancellationToken);

        row.Status = CompanyAccessRequestStatus.Approved;
        row.DecidedByUserId = actorUserId;
        row.DecidedAtUtc = DateTime.UtcNow;
        row.GrantedRole = role;
        row.GrantedVestigingIdsJson = JsonSerializer.Serialize(membershipIds, JsonOpts);
        await _db.SaveChangesAsync(cancellationToken);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "company.access.approved",
            Message =
                $"Access request approved for company {row.TargetCompanyId}; requester {EmailServiceStub.RedactEmail(row.RequesterEmail)}; role {role}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new AccessRequestDecisionResult(
            row.Id, row.Status, "Toegang gegeven — uitnodiging verstuurd.", user.Id);
    }

    public async Task<AccessRequestDecisionResult> RejectAsync(
        Guid requestId,
        Guid actorUserId,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyAccessRequests
            .Include(r => r.TargetCompany)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Verzoek niet gevonden.");

        EnsureDecidable(row);
        EnsureAccess(row, accessibleCompanyIds, isAdmin);

        row.Status = CompanyAccessRequestStatus.Rejected;
        row.DecidedByUserId = actorUserId;
        row.DecidedAtUtc = DateTime.UtcNow;
        row.DecisionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var mail = TransactionalEmails.AccessRequestRejected(
            features.PublicWebBaseUrl, row.RequesterName, row.TargetCompany.Name, row.DecisionReason);
        await _email.SendAsync(
            new EmailMessage(row.RequesterEmail, mail.Subject, mail.Html, mail.Category),
            cancellationToken);

        return new AccessRequestDecisionResult(row.Id, row.Status, "Verzoek afgewezen.", null);
    }

    public async Task<IReadOnlyList<AccessRequestAdminItem>> ListEscalatedForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.CompanyAccessRequests
            .AsNoTracking()
            .Include(r => r.TargetCompany)
            .Where(r => r.Status == CompanyAccessRequestStatus.Escalated)
            .OrderByDescending(r => r.EscalatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<AccessRequestAdminItem>();
        foreach (var r in rows)
        {
            var contacts = await LoadManagerContactsAsync(r.TargetCompanyId, cancellationToken);
            result.Add(new AccessRequestAdminItem(
                r.Id,
                r.TargetCompanyId,
                r.TargetCompany.Name,
                r.KvkNumber,
                r.RequesterName,
                r.RequesterEmail,
                r.RequesterPhone,
                r.RequestedRole,
                r.Message,
                r.CreatedAtUtc,
                r.EscalatedAtUtc,
                contacts));
        }

        return result;
    }

    public async Task<IReadOnlyList<OwnershipTransferAdminItem>> ListOwnershipTransfersForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.EstablishmentTakeoverRequests
            .AsNoTracking()
            .Include(t => t.Registration)
            .Include(t => t.TargetCompany)
            .Where(t => t.Status == TakeoverRequestStatus.Pending
                        && t.Kind == TakeoverRequestKind.OwnershipTransfer
                        && t.Registration.ContactEmailVerifiedAt != null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(t => new OwnershipTransferAdminItem(
            t.Id,
            t.TargetCompanyId,
            t.TargetCompany.Name,
            t.Registration.KvkNumber,
            t.Registration.ContactName,
            t.Registration.ContactEmail,
            t.LetterVerifiedAtUtc != null,
            t.CreatedAt,
            t.LetterVerifiedAtUtc)).ToList();
    }

    public async Task<(int Reminders, int Escalations, int Expiries)> ProcessEscalationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var open = await _db.CompanyAccessRequests
            .Include(r => r.TargetCompany)
            .Where(r => r.Status == CompanyAccessRequestStatus.Open
                        || r.Status == CompanyAccessRequestStatus.Escalated)
            .ToListAsync(cancellationToken);

        var reminders = 0;
        var escalations = 0;
        var expiries = 0;
        var features = await _features.GetAsync(cancellationToken);

        foreach (var row in open)
        {
            var ageDays = (utcNow - row.CreatedAtUtc).TotalDays;
            var workingAge = WorkingDays.CountWorkingDaysBetween(row.CreatedAtUtc, utcNow, Dutch);

            if (row.Status == CompanyAccessRequestStatus.Open
                && workingAge >= 3
                && row.ReminderSentAtUtc is null)
            {
                await NotifyManagersAsync(row, isReminder: true, cancellationToken);
                row.ReminderSentAtUtc = utcNow;
                reminders++;
            }

            if (row.Status == CompanyAccessRequestStatus.Open
                && workingAge >= 5)
            {
                row.Status = CompanyAccessRequestStatus.Escalated;
                row.EscalatedAtUtc = utcNow;
                escalations++;
                _db.PlatformLogs.Add(new PlatformLog
                {
                    Id = Guid.NewGuid(),
                    Level = PlatformLogLevel.Info,
                    Category = "company.access.escalated",
                    Message = $"Access request {row.Id} escalated after {workingAge} working days",
                    CreatedAt = utcNow
                });
            }

            if (ageDays >= 30
                && row.Status is CompanyAccessRequestStatus.Open or CompanyAccessRequestStatus.Escalated)
            {
                row.Status = CompanyAccessRequestStatus.Expired;
                row.DecidedAtUtc = utcNow;
                expiries++;
                var mail = TransactionalEmails.AccessRequestExpired(
                    features.PublicWebBaseUrl, row.RequesterName, row.TargetCompany.Name);
                await _email.SendAsync(
                    new EmailMessage(row.RequesterEmail, mail.Subject, mail.Html, mail.Category),
                    cancellationToken);
            }
        }

        if (reminders + escalations + expiries > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return (reminders, escalations, expiries);
    }

    private async Task NotifyManagersAsync(
        CompanyAccessRequest row,
        bool isReminder,
        CancellationToken cancellationToken)
    {
        var managers = await LoadManagerContactsAsync(row.TargetCompanyId, cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var roleLabel = RoleLabel(row.RequestedRole);
        foreach (var manager in managers)
        {
            var mail = isReminder
                ? TransactionalEmails.AccessRequestReminder(
                    features.PublicWebBaseUrl, row.TargetCompany.Name, row.RequesterName)
                : TransactionalEmails.AccessRequestToManager(
                    features.PublicWebBaseUrl,
                    row.TargetCompany.Name,
                    row.RequesterName,
                    row.RequesterFunction,
                    row.RequesterEmail,
                    roleLabel);

            await _email.SendAsync(
                new EmailMessage(manager.Email, mail.Subject, mail.Html, mail.Category),
                cancellationToken);

            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    manager.UserId,
                    isReminder ? "Herinnering toegangsverzoek" : "Nieuw toegangsverzoek",
                    $"{row.RequesterName} vraagt toegang tot {row.TargetCompany.Name}.",
                    "company.access",
                    DeepLink: "/employer/takeovers",
                    RelatedEntityType: "CompanyAccessRequest",
                    RelatedEntityId: row.Id),
                cancellationToken);
        }

        if (managers.Count == 0)
        {
            _logger.LogWarning(
                "Access request {Id} has no bedrijfsmanagers to notify for company {CompanyId}",
                row.Id,
                row.TargetCompanyId);
        }
    }

    private async Task<IReadOnlyList<AccessRequestManagerContact>> LoadManagerContactsAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var tree = await CompanyOccupancy.ExpandCompanyTreeAsync(_db, companyId, cancellationToken);
        var rootId = tree.Count > 0
            ? await _db.Companies.AsNoTracking()
                .Where(c => tree.Contains(c.Id))
                .OrderBy(c => c.ParentCompanyId == null ? 0 : 1)
                .Select(c => c.Id)
                .FirstAsync(cancellationToken)
            : companyId;

        // Prefer bedrijfsmanagers of the root; fallback to managers of the requested vestiging.
        var managers = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.EnterpriseManager)
            .Where(u =>
                (u.CompanyId != null && (u.CompanyId == rootId || tree.Contains(u.CompanyId.Value)))
                || u.CompanyMemberships.Any(m => m.CompanyId == rootId || tree.Contains(m.CompanyId)))
            .Select(u => new AccessRequestManagerContact(u.Id, u.FullName, u.Email, null, u.Role))
            .ToListAsync(cancellationToken);

        if (managers.Count > 0)
        {
            return managers.DistinctBy(m => m.UserId).ToList();
        }

        return await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && CompanyOccupancy.ManagingEmployerRoles.Contains(u.Role))
            .Where(u =>
                (u.CompanyId != null && tree.Contains(u.CompanyId.Value))
                || u.CompanyMemberships.Any(m => tree.Contains(m.CompanyId)))
            .Select(u => new AccessRequestManagerContact(u.Id, u.FullName, u.Email, null, u.Role))
            .ToListAsync(cancellationToken);
    }

    private async Task<User> ProvisionInviteUserAsync(
        string email,
        string fullName,
        UserRole role,
        Guid primaryCompanyId,
        IReadOnlyList<Guid> membershipIds,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        User user;
        if (existing is not null)
        {
            if (EmployerInviteRules.BlocksInviteOverwrite(existing.Role))
            {
                throw new InvalidOperationException("Dit e-mailadres is al in gebruik met een andere rol.");
            }

            user = existing;
            user.FullName = fullName.Trim();
            user.Role = role;
            user.CompanyId = primaryCompanyId;
            user.IsActive = true;
            foreach (var companyId in membershipIds)
            {
                if (user.CompanyMemberships.All(m => m.CompanyId != companyId))
                {
                    _db.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = companyId });
                }
            }
        }
        else
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = fullName.Trim(),
                Role = role,
                CompanyId = primaryCompanyId,
                IsActive = true
            };
            _db.Users.Add(user);
            foreach (var companyId in membershipIds)
            {
                _db.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = companyId });
            }
        }

        if (role == UserRole.EnterpriseManager)
        {
            var orgChildren = await _db.Companies
                .Where(c => c.ParentCompanyId == primaryCompanyId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            foreach (var childId in orgChildren)
            {
                var has = existing?.CompanyMemberships.Any(m => m.CompanyId == childId) == true
                          || _db.UserCompanies.Local.Any(m => m.UserId == user.Id && m.CompanyId == childId);
                if (!has && !await _db.UserCompanies.AnyAsync(
                        m => m.UserId == user.Id && m.CompanyId == childId, cancellationToken))
                {
                    _db.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = childId });
                }
            }
        }

        var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12))
            .Replace("+", "A", StringComparison.Ordinal)
            .Replace("/", "B", StringComparison.Ordinal)[..12] + "!1a";
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        if (credential is null)
        {
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword)
            });
        }
        else
        {
            credential.Email = email;
            credential.PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var invite = TransactionalEmails.UserInvite(
            features.PublicWebBaseUrl,
            user.FullName,
            RoleLabel(role),
            user.Email,
            temporaryPassword,
            promotedFromCandidate: false);
        await _email.SendAsync(
            new EmailMessage(user.Email, invite.Subject, invite.Html, invite.Category),
            cancellationToken);

        return user;
    }

    private static Guid ResolvePrimaryCompany(Company target, UserRole role, IReadOnlyList<Guid> membershipIds)
    {
        if (role == UserRole.EnterpriseManager)
        {
            return target.ParentCompanyId ?? target.Id;
        }

        if (membershipIds.Contains(target.Id))
        {
            return target.Id;
        }

        return membershipIds.Count > 0 ? membershipIds[0] : target.Id;
    }

    private static void EnsureDecidable(CompanyAccessRequest row)
    {
        if (row.Status is not (CompanyAccessRequestStatus.Open or CompanyAccessRequestStatus.Escalated))
        {
            throw new InvalidOperationException("Dit verzoek is al afgehandeld.");
        }
    }

    private static void EnsureAccess(
        CompanyAccessRequest row,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin)
    {
        if (!isAdmin
            && (accessibleCompanyIds is null || !accessibleCompanyIds.Contains(row.TargetCompanyId)))
        {
            throw new UnauthorizedAccessException("Geen toegang tot dit verzoek.");
        }
    }

    private static IReadOnlyList<Guid> ParseIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json, JsonOpts) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string RoleLabel(UserRole role) => role switch
    {
        UserRole.EnterpriseManager => "Bedrijfsmanager",
        UserRole.RegionalManager => "Regiomanager",
        UserRole.BranchManager => "Vestigingsmanager",
        _ => role.ToString()
    };

    private static TimeZoneInfo ResolveDutch()
    {
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
