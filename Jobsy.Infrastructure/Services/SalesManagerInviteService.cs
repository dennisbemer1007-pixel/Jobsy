using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerInviteService : ISalesManagerInviteService
{
    private readonly JobsyDbContext _db;
    private readonly IEmailService _email;
    private readonly IOneTimeLinkService _links;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<SalesManagerInviteService> _logger;

    public SalesManagerInviteService(
        JobsyDbContext db,
        IEmailService email,
        IOneTimeLinkService links,
        IPlatformFeatureService features,
        ILogger<SalesManagerInviteService> logger)
    {
        _db = db;
        _email = email;
        _links = links;
        _features = features;
        _logger = logger;
    }

    public async Task<SalesManagerInviteResult> InviteAsync(
        string email,
        string fullName,
        Guid? referredBySalesManagerUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("E-mail en naam zijn verplicht.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var name = fullName.Trim();
        var canRecruit = referredBySalesManagerUserId is null;

        if (referredBySalesManagerUserId is Guid referrerId)
        {
            var referrer = await _db.SalesManagerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == referrerId, cancellationToken)
                ?? throw new InvalidOperationException("Verwijzende salesmanager niet gevonden.");

            if (!referrer.CanRecruitSalesManagers)
            {
                throw new InvalidOperationException(
                    "Deze salesmanager mag geen nieuwe salesmanagers aanbrengen (maximaal één wervingslaag).");
            }

            if (referrer.ReferredBySalesManagerUserId is not null)
            {
                throw new InvalidOperationException(
                    "Doorverwezen salesmanagers kunnen zelf geen nieuwe salesmanagers werven.");
            }
        }

        var existing = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        User user;
        bool createdNew;

        if (existing is not null)
        {
            if (existing.Role != UserRole.SalesManager && existing.Role != UserRole.Candidate)
            {
                throw new InvalidOperationException(
                    "Dit e-mailadres hoort al bij een andere rol en kan niet als salesmanager worden uitgenodigd.");
            }

            existing.FullName = name;
            existing.Role = UserRole.SalesManager;
            existing.IsActive = true;
            existing.CompanyId = null;
            user = existing;
            createdNew = false;
        }
        else
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FullName = name,
                Role = UserRole.SalesManager,
                CompanyId = null,
                IsActive = true
            };
            _db.Users.Add(user);
            createdNew = true;
        }

        var profile = await _db.SalesManagerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        var now = DateTime.UtcNow;
        if (profile is null)
        {
            profile = new SalesManagerProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = now,
                UpdatedAt = now,
                CanRecruitSalesManagers = canRecruit,
                ReferredBySalesManagerUserId = referredBySalesManagerUserId
            };
            _db.SalesManagerProfiles.Add(profile);
        }
        else
        {
            if (referredBySalesManagerUserId is not null)
            {
                profile.ReferredBySalesManagerUserId ??= referredBySalesManagerUserId;
                profile.CanRecruitSalesManagers = false;
            }
            else if (profile.ReferredBySalesManagerUserId is null)
            {
                profile.CanRecruitSalesManagers = true;
            }

            profile.UpdatedAt = now;
        }

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "SalesManager",
            Message = referredBySalesManagerUserId is null
                ? $"Salesmanager invited (admin): {EmailServiceStub.RedactEmail(normalizedEmail)}"
                : $"Salesmanager invited (referral approved): {EmailServiceStub.RedactEmail(normalizedEmail)}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        var setPasswordUrl = await ResolveSetPasswordUrlAsync(user, normalizedEmail, cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var invite = TransactionalEmails.SalesManagerInvite(
            features.PublicWebBaseUrl, name, normalizedEmail, setPasswordUrl);
        await _email.SendAsync(new EmailMessage(
            normalizedEmail,
            invite.Subject,
            invite.Html,
            invite.Category), cancellationToken);

        _logger.LogInformation(
            "Invited salesmanager {Email} ({UserId}) canRecruit={CanRecruit} referredBy={ReferredBy}",
            EmailServiceStub.RedactEmail(normalizedEmail),
            user.Id,
            profile.CanRecruitSalesManagers,
            referredBySalesManagerUserId);

        return new SalesManagerInviteResult(
            user.Id,
            normalizedEmail,
            name,
            createdNew,
            profile.CanRecruitSalesManagers,
            profile.ReferredBySalesManagerUserId);
    }

    private async Task<string?> ResolveSetPasswordUrlAsync(
        User user,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var hasCredential = await _db.LocalAuthCredentials
            .AnyAsync(c => c.UserId == user.Id, cancellationToken);
        var hasExternal = await _db.UserExternalLogins
            .AnyAsync(l => l.UserId == user.Id, cancellationToken);
        if (hasCredential || hasExternal)
        {
            return null;
        }

        var created = await _links.CreateAsync(
            OneTimeLinkPurpose.SetPassword,
            user.Id,
            companyId: null,
            normalizedEmail,
            OneTimeLinkRules.SetPasswordLifetime,
            createdByUserId: null,
            cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return EmailLayout.Absolute(
            features.PublicWebBaseUrl,
            $"/account/wachtwoord-instellen?t={Uri.EscapeDataString(created.Token)}");
    }
}
