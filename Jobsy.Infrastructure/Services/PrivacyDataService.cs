using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class PrivacyDataService : IPrivacyDataService
{
    private const int UnsubscribeCodeTtlMinutes = 10;
    private const int UnsubscribeReasonOtherMaxLength = 1000;

    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;
    private readonly IEmailService _email;

    public PrivacyDataService(JobsyDbContext db, IUserLookupService users, IEmailService email)
    {
        _db = db;
        _users = users;
        _email = email;
    }

    public async Task<object> ExportAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPrincipalAsync(principal, cancellationToken)
            ?? throw new UnauthorizedAccessException("Gebruiker niet gevonden.");

        var applications = await _db.Applications.AsNoTracking()
            .Where(a => a.CandidateUserId == user.Id || a.CandidateEmail == user.Email)
            .Select(a => new
            {
                a.Id,
                a.VacancyId,
                a.PreferredTransport,
                a.EstimatedTravelMinutes,
                a.DistanceKm,
                a.Status,
                a.CreatedAt,
                a.RespondedAt,
                a.ConsentAcceptedAt,
                a.ConsentVersion,
                a.WorkPermitConfirmed,
                a.SnapshotAvailabilityJson,
                a.SnapshotDrivingLicenses,
                a.SnapshotEducations,
                a.SnapshotAboutMe,
                a.SnapshotCertificatesJson,
                a.SnapshotShowAddressOnCv,
                a.CandidateCity,
                a.CandidateAddress,
                a.Motivation,
                a.StudentNumber,
                a.SchoolEmail,
                a.StudyProgram,
                a.StudyYear,
                a.ExclusivityValidationStatus,
                a.CandidateEmployerCount,
                EmailVerified = a.EmailVerifiedAt != null
            })
            .ToListAsync(cancellationToken);

        var likes = await _db.VacancyLikes.AsNoTracking()
            .Where(l => l.UserId == user.Id)
            .Select(l => new { l.VacancyId, l.CreatedAt })
            .ToListAsync(cancellationToken);

        var shares = await _db.VacancyShares.AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .Select(s => new
            {
                s.Id,
                s.VacancyId,
                Channel = s.Channel.ToString(),
                s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var clicks = await _db.VacancyClicks.AsNoTracking()
            .Where(c => c.UserId == user.Id)
            .Select(c => new
            {
                c.Id,
                c.VacancyId,
                c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var impressions = await _db.VacancySearchImpressions.AsNoTracking()
            .Where(i => i.UserId == user.Id)
            .Select(i => new { i.Id, i.VacancyId, i.CreatedAt })
            .ToListAsync(cancellationToken);

        var siteVisits = await _db.SiteVisits.AsNoTracking()
            .Where(v => v.UserId == user.Id)
            .Select(v => new { v.Id, v.Path, v.CreatedAt })
            .ToListAsync(cancellationToken);

        var memberships = await _db.UserCompanies.AsNoTracking()
            .Where(m => m.UserId == user.Id)
            .Select(m => m.CompanyId)
            .ToListAsync(cancellationToken);

        var registrations = await _db.CompanyRegistrations.AsNoTracking()
            .Where(r => r.CreatedUserId == user.Id || r.ContactEmail == user.Email)
            .Select(r => new
            {
                r.Id,
                r.KvkNumber,
                r.EstablishmentName,
                Scope = r.Scope.ToString(),
                r.ContactName,
                r.ContactEmail,
                r.ContactPhone,
                Status = r.Status.ToString(),
                r.ConsentAcceptedAt,
                r.ConsentVersion,
                r.SalesManagerTrackingCode,
                r.PartnerTrackingCode,
                r.CreatedAt,
                r.ActivatedAt
            })
            .ToListAsync(cancellationToken);

        var salesProfile = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new
            {
                p.CompanyName,
                p.KvkNumber,
                p.VatNumber,
                p.Address,
                p.PostalCode,
                p.City,
                p.Country,
                p.Iban,
                p.TrackingCode,
                p.AgreementSignedAt,
                p.AgreementVersion,
                p.OnboardingCompletedAt,
                p.CreatedAt,
                p.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var partnerProfile = await _db.PartnerAffiliateProfiles.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new
            {
                p.CompanyName,
                p.KvkNumber,
                p.VatNumber,
                p.Address,
                p.PostalCode,
                p.City,
                p.Country,
                p.Iban,
                p.TrackingCode,
                p.AgreementSignedAt,
                p.AgreementVersion,
                p.CreatedAtUtc,
                p.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        var commissionEntries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == user.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                Kind = e.Kind.ToString(),
                e.AmountExVat,
                e.VatAmount,
                e.VatRate,
                e.Note,
                e.CompanyId,
                e.SourcePaymentId,
                e.SourceTokenCheckoutId,
                e.SelfBillingInvoiceId,
                e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var invoices = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == user.Id)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.SalesManagerCompanyName,
                i.SalesManagerKvkNumber,
                i.SalesManagerVatNumber,
                i.SalesManagerAddress,
                i.SubtotalExVat,
                i.VatAmount,
                i.TotalInclVat,
                Status = i.Status.ToString(),
                i.CreatedAt,
                i.IssuedAt,
                i.PaidAt,
                Lines = i.Lines.Select(l => new
                {
                    l.Id,
                    l.Description,
                    l.AmountExVat,
                    l.SourceLedgerEntryId
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var payouts = await _db.SalesManagerPayoutCheckouts.AsNoTracking()
            .Where(p => p.SalesManagerUserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.PaymentId,
                p.AmountEuro,
                p.AmountExVat,
                p.VatAmount,
                p.MaskedIban,
                Status = p.Status.ToString(),
                p.CreatedAt,
                p.CompletedAt
            })
            .ToListAsync(cancellationToken);

        // Portability: include in-app notifications (ActionUrl without bearer tokens).
        var notifications = await _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Body,
                n.Category,
                n.DeepLink,
                n.ActionLabel,
                n.ActionUrl,
                n.IsRead,
                n.CreatedAtUtc,
                n.ReadAtUtc,
                n.RelatedEntityType,
                n.RelatedEntityId
            })
            .ToListAsync(cancellationToken);

        return new
        {
            ExportedAtUtc = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            User = new
            {
                user.Id,
                user.Email,
                user.FullName,
                Role = user.Role.ToString(),
                user.CompanyId,
                user.DateOfBirth,
                user.OpenForWork,
                HomeLocation = user.HomeLocation is null
                    ? null
                    : new { user.HomeLocation.Latitude, user.HomeLocation.Longitude },
                user.PreferencesJson,
                user.TermsAcceptedAt,
                user.ConsentVersion,
                user.IsActive
            },
            CompanyMemberships = memberships,
            Applications = applications,
            Likes = likes,
            VacancyShares = shares,
            VacancyClicks = clicks,
            VacancySearchImpressions = impressions,
            SiteVisits = siteVisits,
            Notifications = notifications.Select(n => new
            {
                n.Id,
                n.Title,
                n.Body,
                n.Category,
                n.DeepLink,
                n.ActionLabel,
                ActionUrl = UserNotificationService.SanitizeActionUrl(n.ActionUrl),
                n.IsRead,
                n.CreatedAtUtc,
                n.ReadAtUtc,
                n.RelatedEntityType,
                n.RelatedEntityId
            }),
            CompanyRegistrations = registrations,
            SalesManagerProfile = salesProfile,
            PartnerAffiliateProfile = partnerProfile,
            CommissionLedger = commissionEntries,
            SelfBillingInvoices = invoices,
            SalesManagerPayouts = payouts
        };
    }

    public async Task<RequestUnsubscribeResponse> RequestUnsubscribeAsync(
        ClaimsPrincipal principal,
        string reasonCode,
        string? reasonOther,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveActiveUserAsync(principal, cancellationToken);

        var code = reasonCode?.Trim() ?? string.Empty;
        if (!AccountUnsubscribeReasons.IsKnown(code))
        {
            throw new ArgumentException("Kies een geldige reden voor uitschrijving.");
        }

        var other = string.IsNullOrWhiteSpace(reasonOther) ? null : reasonOther.Trim();
        if (AccountUnsubscribeReasons.RequiresOtherText(code))
        {
            if (string.IsNullOrWhiteSpace(other))
            {
                throw new ArgumentException("Vul een toelichting in bij ‘Anders’.");
            }
        }
        else
        {
            other = null;
        }

        if (other is { Length: > UnsubscribeReasonOtherMaxLength })
        {
            throw new ArgumentException($"Toelichting mag maximaal {UnsubscribeReasonOtherMaxLength} tekens zijn.");
        }

        var verificationCode = VerificationCodes.CreateNumericCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(UnsubscribeCodeTtlMinutes);

        user.UnsubscribeReasonCode = code;
        user.UnsubscribeReasonOther = other;
        user.UnsubscribeVerificationCode = VerificationCodes.Hash(verificationCode);
        user.UnsubscribeVerificationExpiresAt = expiresAt;
        user.UnsubscribeVerificationFailedAttempts = 0;

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Unsubscribe",
            Message = FormatUnsubscribeLogMessage("Uitschrijving aangevraagd", code, other, user.Id),
            DetailsJson = JsonSerializer.Serialize(new
            {
                UserId = user.Id,
                Email = EmailServiceStub.RedactEmail(user.Email),
                ReasonCode = code,
                ReasonLabel = AccountUnsubscribeReasons.GetLabel(code),
                // Free-text ReasonOther is not logged (AVG minimization).
                HasReasonOther = !string.IsNullOrWhiteSpace(other),
                Step = "request"
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _email.SendAsync(new EmailMessage(
            user.Email,
            "Verificatiecode voor uitschrijving bij Lobsy",
            $"""
             <p>Hoi {Html(user.FullName)},</p>
             <p>Je hebt gevraagd om je Lobsy-account af te melden.</p>
             <p>Gebruik deze 6-cijferige code om de uitschrijving te bevestigen:</p>
             <p style="font-size:1.6rem"><strong>{Html(verificationCode)}</strong></p>
             <p>De code is {UnsubscribeCodeTtlMinutes} minuten geldig. Heb je dit niet zelf aangevraagd? Negeer deze mail dan.</p>
             """,
            "AccountUnsubscribeVerification"), cancellationToken);

        return new RequestUnsubscribeResponse(
            "Er is een verificatiecode naar je e-mail gestuurd.",
            expiresAt);
    }

    public async Task ConfirmUnsubscribeAsync(
        ClaimsPrincipal principal,
        string verificationCode,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveActiveUserAsync(principal, cancellationToken);

        var code = verificationCode?.Trim() ?? string.Empty;
        if (code.Length != 6 || !code.All(char.IsDigit))
        {
            throw new ArgumentException("Vul de 6-cijferige verificatiecode in.");
        }

        if (string.IsNullOrWhiteSpace(user.UnsubscribeVerificationCode)
            || user.UnsubscribeVerificationExpiresAt is null
            || string.IsNullOrWhiteSpace(user.UnsubscribeReasonCode))
        {
            throw new InvalidOperationException("Vraag eerst een nieuwe verificatiecode aan.");
        }

        if (user.UnsubscribeVerificationExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Verificatiecode verlopen. Vraag een nieuwe code aan.");
        }

        if (user.UnsubscribeVerificationFailedAttempts >= VerificationCodes.MaxFailedAttempts)
        {
            user.UnsubscribeVerificationCode = null;
            user.UnsubscribeVerificationExpiresAt = null;
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Te veel onjuiste pogingen. Vraag een nieuwe verificatiecode aan.");
        }

        if (!VerificationCodes.MatchesHash(user.UnsubscribeVerificationCode, code))
        {
            var attempts = user.UnsubscribeVerificationFailedAttempts;
            var lockedOut = VerificationCodes.RegisterFailedAttempt(ref attempts);
            user.UnsubscribeVerificationFailedAttempts = attempts;
            if (lockedOut)
            {
                user.UnsubscribeVerificationCode = null;
                user.UnsubscribeVerificationExpiresAt = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new ArgumentException(lockedOut
                ? "Te veel onjuiste pogingen. Vraag een nieuwe verificatiecode aan."
                : "Onjuiste verificatiecode.");
        }

        var reasonCode = user.UnsubscribeReasonCode;
        var reasonOther = user.UnsubscribeReasonOther;
        await AnonymizeUserAsync(user, reasonCode, reasonOther, cancellationToken);
    }

    public async Task DeleteOrAnonymizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var user = await ResolveActiveUserAsync(principal, cancellationToken);
        await AnonymizeUserAsync(user, reasonCode: null, reasonOther: null, cancellationToken);
    }

    private async Task AnonymizeUserAsync(
        User user,
        string? reasonCode,
        string? reasonOther,
        CancellationToken cancellationToken)
    {
        // Ensure memberships are loaded for removal.
        if (!_db.Entry(user).Collection(u => u.CompanyMemberships).IsLoaded)
        {
            await _db.Entry(user).Collection(u => u.CompanyMemberships).LoadAsync(cancellationToken);
        }

        var originalEmail = user.Email;
        var anonymizedEmail = $"deleted-{user.Id:N}@anonymized.jobsy.local";
        var applications = await _db.Applications
            .Where(a => a.CandidateUserId == user.Id || a.CandidateEmail == originalEmail)
            .ToListAsync(cancellationToken);

        foreach (var app in applications)
        {
            app.CandidateName = "Verwijderde gebruiker";
            app.CandidateEmail = anonymizedEmail;
            app.CandidateCity = null;
            app.CandidateAddress = null;
            app.PreferencesSummary = null;
            app.CandidateUserId = null;
            app.DistanceKm = null;
            app.SnapshotAvailabilityJson = null;
            app.SnapshotDrivingLicenses = null;
            app.SnapshotEducations = null;
            app.SnapshotAboutMe = null;
            app.SnapshotPhoneNumber = null;
            app.SnapshotWhatsAppAllowed = false;
            app.SnapshotHomeLatitude = null;
            app.SnapshotHomeLongitude = null;
            app.SnapshotCertificatesJson = null;
            app.SnapshotShowAddressOnCv = false;
            app.SnapshotDateOfBirth = null;
            app.Motivation = null;
            app.StudentNumber = null;
            app.SchoolEmail = null;
            app.StudyProgram = null;
            app.StudyYear = null;
            app.ExclusivityValidationStatus = null;
            app.CandidateEmployerCount = 0;
            app.EmailVerificationCode = null;
            app.EmailVerificationExpiresAt = null;
        }

        var likes = await _db.VacancyLikes
            .Where(l => l.UserId == user.Id)
            .ToListAsync(cancellationToken);
        _db.VacancyLikes.RemoveRange(likes);

        var shares = await _db.VacancyShares
            .Where(s => s.UserId == user.Id)
            .ToListAsync(cancellationToken);
        _db.VacancyShares.RemoveRange(shares);

        var clicks = await _db.VacancyClicks
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);
        _db.VacancyClicks.RemoveRange(clicks);

        var impressions = await _db.VacancySearchImpressions
            .Where(i => i.UserId == user.Id)
            .ToListAsync(cancellationToken);
        _db.VacancySearchImpressions.RemoveRange(impressions);

        var siteVisits = await _db.SiteVisits
            .Where(v => v.UserId == user.Id)
            .ToListAsync(cancellationToken);
        _db.SiteVisits.RemoveRange(siteVisits);

        var credentials = await _db.LocalAuthCredentials
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);
        _db.LocalAuthCredentials.RemoveRange(credentials);

        _db.UserCompanies.RemoveRange(user.CompanyMemberships);

        var registrations = await _db.CompanyRegistrations
            .Where(r => r.CreatedUserId == user.Id || r.ContactEmail == originalEmail)
            .ToListAsync(cancellationToken);
        foreach (var registration in registrations)
        {
            registration.ContactName = "Verwijderde gebruiker";
            registration.ContactEmail = anonymizedEmail;
            registration.ContactPhone = null;
            registration.ActivationToken = string.Empty;
            registration.PasswordHash = null;
            registration.SalesManagerTrackingCode = null;
            registration.PartnerTrackingCode = null;
        }

        // AVG: anonymize salesmanager business PII; keep financial rows for fiscal retention
        // but strip personal identifiers from profile and invoice snapshots.
        var salesProfile = await _db.SalesManagerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (salesProfile is not null)
        {
            salesProfile.CompanyName = "Verwijderde salesmanager";
            salesProfile.KvkNumber = null;
            salesProfile.VatNumber = null;
            salesProfile.Address = null;
            salesProfile.PostalCode = null;
            salesProfile.City = null;
            salesProfile.Country = null;
            salesProfile.Iban = null;
            salesProfile.TrackingCode = null;
            salesProfile.AgreementSignedAt = null;
            salesProfile.AgreementVersion = null;
            salesProfile.OnboardingCompletedAt = null;
            salesProfile.UpdatedAt = DateTime.UtcNow;
        }

        var partnerProfile = await _db.PartnerAffiliateProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (partnerProfile is not null)
        {
            partnerProfile.CompanyName = "Verwijderde partner";
            partnerProfile.KvkNumber = null;
            partnerProfile.VatNumber = null;
            partnerProfile.Address = null;
            partnerProfile.PostalCode = null;
            partnerProfile.City = null;
            partnerProfile.Country = null;
            partnerProfile.Iban = null;
            partnerProfile.AgreementSignedAt = null;
            partnerProfile.AgreementVersion = null;
            partnerProfile.TrackingCode = $"DEL-{partnerProfile.Id:N}"[..32];
            partnerProfile.UpdatedAtUtc = DateTime.UtcNow;
        }

        var payouts = await _db.SalesManagerPayoutCheckouts
            .Where(p => p.SalesManagerUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var payout in payouts)
        {
            payout.MaskedIban = "ANON";
        }

        var invoices = await _db.SelfBillingInvoices
            .Where(i => i.SalesManagerUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var invoice in invoices)
        {
            invoice.SalesManagerCompanyName = "Verwijderde salesmanager";
            invoice.SalesManagerKvkNumber = "ANON";
            invoice.SalesManagerVatNumber = "ANON";
            invoice.SalesManagerAddress = "Geanonimiseerd";
        }

        var ledgerNotes = await _db.CommissionLedgerEntries
            .Where(e => e.SalesManagerUserId == user.Id && e.Note != null)
            .ToListAsync(cancellationToken);
        foreach (var entry in ledgerNotes)
        {
            entry.Note = entry.Kind switch
            {
                CommissionEntryKind.FounderBonus => "Founder-bonus (geanonimiseerd)",
                CommissionEntryKind.TokenCommission => "Tokencommissie (geanonimiseerd)",
                CommissionEntryKind.IndirectTokenCommission => "Indirecte commissie (geanonimiseerd)",
                CommissionEntryKind.Payout => "Self-billing uitbetaling",
                _ => "Aanpassing (geanonimiseerd)"
            };
        }

        // Detach referred companies from deleted salesmanager identity.
        var referred = await _db.Companies
            .Where(c => c.ReferredBySalesManagerUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var company in referred)
        {
            company.ReferredBySalesManagerUserId = null;
        }

        var partnerReferred = await _db.Companies
            .Where(c => c.ReferredByPartnerUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var company in partnerReferred)
        {
            company.ReferredByPartnerUserId = null;
            company.PartnerReferralStatus = PartnerReferralStatus.None;
            company.PartnerReferredAtUtc = null;
            company.PartnerReferralRewardedAtUtc = null;
        }

        // Detach SM→SM hierarchy links and scrub pending/closed applications.
        var referredProfiles = await _db.SalesManagerProfiles
            .Where(p => p.ReferredBySalesManagerUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var child in referredProfiles)
        {
            child.ReferredBySalesManagerUserId = null;
            child.CanRecruitSalesManagers = false;
            child.UpdatedAt = DateTime.UtcNow;
        }

        if (salesProfile is not null)
        {
            salesProfile.ReferredBySalesManagerUserId = null;
            salesProfile.CanRecruitSalesManagers = false;
        }

        var smApplications = await _db.SalesManagerApplications
            .Where(a => a.ReferrerSalesManagerUserId == user.Id
                        || a.ProvisionedUserId == user.Id
                        || a.ReviewedByAdminUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var smApp in smApplications)
        {
            smApp.CandidateFullName = "Verwijderde kandidaat";
            smApp.CandidateEmail = $"deleted-{smApp.Id:N}@anonymized.local";
            smApp.Motivation = "Geanonimiseerd";
            smApp.RejectionReason = null;
            if (smApp.ReviewedByAdminUserId == user.Id)
            {
                smApp.ReviewedByAdminUserId = null;
            }
        }

        // Drop external IdP bindings so OID/sub cannot re-attach to an anonymized account.
        var externalLogins = await _db.UserExternalLogins
            .Where(l => l.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (externalLogins.Count > 0)
        {
            _db.UserExternalLogins.RemoveRange(externalLogins);
        }

        // AVG: purge in-app notifications and single-use action tokens for this user.
        var notifications = await _db.UserNotifications
            .Where(n => n.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (notifications.Count > 0)
        {
            _db.UserNotifications.RemoveRange(notifications);
        }

        var actionTokens = await _db.CandidateActionTokens
            .Where(t => t.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (actionTokens.Count > 0)
        {
            _db.CandidateActionTokens.RemoveRange(actionTokens);
        }

        user.Email = anonymizedEmail;
        user.FullName = "Verwijderde gebruiker";
        user.FirstName = null;
        user.LastName = null;
        user.PhoneNumber = null;
        user.WhatsAppContactAllowed = false;
        user.DateOfBirth = null;
        user.HomeLocation = null;
        user.PreferencesJson = null;
        user.OpenForWork = false;
        user.CompanyId = null;
        user.IsActive = false;
        user.TermsAcceptedAt = null;
        user.ConsentVersion = null;
        user.CandidateHowToCompletedAt = null;
        user.LastLoginAtUtc = null;
        user.UnsubscribeVerificationCode = null;
        user.UnsubscribeVerificationExpiresAt = null;
        user.UnsubscribeVerificationFailedAttempts = 0;
        user.UnsubscribeReasonCode = null;
        user.UnsubscribeReasonOther = null;

        if (!string.IsNullOrWhiteSpace(reasonCode))
        {
            var reasonLabel = AccountUnsubscribeReasons.GetLabel(reasonCode);
            _db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Unsubscribe",
                Message = FormatUnsubscribeLogMessage("Uitschrijving bevestigd", reasonCode, reasonOther, user.Id),
                DetailsJson = JsonSerializer.Serialize(new
                {
                    UserId = user.Id,
                    Email = EmailServiceStub.RedactEmail(originalEmail),
                    ReasonCode = reasonCode,
                    ReasonLabel = reasonLabel,
                    // Free-text ReasonOther is not logged (AVG minimization).
                    HasReasonOther = !string.IsNullOrWhiteSpace(reasonOther),
                    Step = "confirmed"
                }),
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            _db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Privacy",
                Message = $"Account anonymized: {user.Id}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> ResolveActiveUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Gebruiker niet gevonden.");
        }

        return await _db.Users
                   .Include(u => u.CompanyMemberships)
                   .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken)
               ?? throw new UnauthorizedAccessException("Gebruiker niet gevonden.");
    }

    private static string FormatUnsubscribeLogMessage(
        string prefix,
        string reasonCode,
        string? reasonOther,
        Guid userId)
    {
        var label = AccountUnsubscribeReasons.GetLabel(reasonCode);
        // Do not append free-text ReasonOther to the message (AVG minimization).
        var hasOther = !string.IsNullOrWhiteSpace(reasonOther);
        return hasOther
            ? $"{prefix}: {label} (toelichting aanwezig; user {userId})"
            : $"{prefix}: {label} (user {userId})";
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
