using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class AmbassadeurInviteService : IAmbassadeurInviteService
{
    private readonly JobsyDbContext _db;
    private readonly ITransactionalMailer _mailer;
    private readonly IOneTimeLinkService _links;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<AmbassadeurInviteService> _logger;

    public AmbassadeurInviteService(
        JobsyDbContext db,
        ITransactionalMailer mailer,
        IOneTimeLinkService links,
        IPlatformFeatureService features,
        ILogger<AmbassadeurInviteService> logger)
    {
        _db = db;
        _mailer = mailer;
        _links = links;
        _features = features;
        _logger = logger;
    }

    public async Task<AmbassadeurInviteResult> InviteAsync(
        string email,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("E-mail en naam zijn verplicht.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var name = fullName.Trim();

        var existing = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        User user;
        bool createdNew;

        if (existing is not null)
        {
            if (existing.Role != UserRole.Ambassadeur && existing.Role != UserRole.Candidate)
            {
                throw new InvalidOperationException(
                    "Dit e-mailadres hoort al bij een andere rol en kan niet als ambassadeur worden uitgenodigd.");
            }

            existing.FullName = name;
            existing.Role = UserRole.Ambassadeur;
            existing.IsActive = true;
            existing.CompanyId = null;
            existing.TermsAcceptedAt = null;
            existing.ConsentVersion = null;
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
                Role = UserRole.Ambassadeur,
                CompanyId = null,
                IsActive = true
            };
            _db.Users.Add(user);
            createdNew = true;
        }

        var profile = await _db.AmbassadeurProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        var now = DateTime.UtcNow;
        if (profile is null)
        {
            _db.AmbassadeurProfiles.Add(new AmbassadeurProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BaseCommissionPercentage = AmbassadeurCommissionRules.DefaultBaseCommissionPercentage,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            profile.UpdatedAt = now;
        }

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Ambassadeur",
            Message = $"Ambassadeur invited (admin): {EmailServiceStub.RedactEmail(normalizedEmail)}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        var setPasswordUrl = await ResolveSetPasswordUrlAsync(user, normalizedEmail, cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var invite = TransactionalEmails.AmbassadeurInvite(
            features.PublicWebBaseUrl, name, normalizedEmail, setPasswordUrl);
        await _mailer.SendAsync(invite, normalizedEmail, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Invited ambassadeur {Email} ({UserId})",
            EmailServiceStub.RedactEmail(normalizedEmail),
            user.Id);

        return new AmbassadeurInviteResult(
            user.Id,
            normalizedEmail,
            name,
            createdNew);
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
