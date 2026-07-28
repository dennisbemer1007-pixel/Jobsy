using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class PrivacyDataService : IPrivacyDataService
{
    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;

    public PrivacyDataService(JobsyDbContext db, IUserLookupService users)
    {
        _db = db;
        _users = users;
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
            CompanyRegistrations = registrations,
            SalesManagerProfile = salesProfile,
            CommissionLedger = commissionEntries,
            SelfBillingInvoices = invoices,
            SalesManagerPayouts = payouts
        };
    }

    public async Task DeleteOrAnonymizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Gebruiker niet gevonden.");
        }

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Gebruiker niet gevonden.");

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
            registration.SalesManagerTrackingCode = null;
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

        user.Email = anonymizedEmail;
        user.FullName = "Verwijderde gebruiker";
        user.DateOfBirth = null;
        user.HomeLocation = null;
        user.PreferencesJson = null;
        user.OpenForWork = false;
        user.CompanyId = null;
        user.IsActive = false;
        user.TermsAcceptedAt = null;
        user.ConsentVersion = null;

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Privacy",
            Message = $"Account anonymized: {user.Id}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
