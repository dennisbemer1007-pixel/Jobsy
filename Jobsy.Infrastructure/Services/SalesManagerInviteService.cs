using System.Security.Cryptography;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerInviteService : ISalesManagerInviteService
{
    private readonly JobsyDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SalesManagerInviteService> _logger;

    public SalesManagerInviteService(
        JobsyDbContext db,
        IEmailService email,
        ILogger<SalesManagerInviteService> logger)
    {
        _db = db;
        _email = email;
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

        string temporaryPassword;
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

            temporaryPassword = GenerateTemporaryPassword();
            var credential = await _db.LocalAuthCredentials
                .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
            if (credential is null)
            {
                _db.LocalAuthCredentials.Add(new LocalAuthCredential
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Email = normalizedEmail,
                    PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword)
                });
            }
            else
            {
                credential.Email = normalizedEmail;
                credential.PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword);
            }
        }
        else
        {
            temporaryPassword = GenerateTemporaryPassword();
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
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = normalizedEmail,
                PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword)
            });
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
            // Preserve an existing hierarchy link; Admin re-invite of a referred SM stays non-recruiting.
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

        await _email.SendAsync(new EmailMessage(
            normalizedEmail,
            "Uitnodiging Lobsy salesmanager",
            EmailLayout.Wrap(
                $"""
                 {EmailLayout.Heading("Uitnodiging salesmanager")}
                 {EmailLayout.Paragraph($"Hallo {System.Net.WebUtility.HtmlEncode(name)},")}
                 {EmailLayout.Paragraph("Je bent uitgenodigd als salesmanager op Lobsy.")}
                 {EmailLayout.Paragraph(
                     $"Log in met <strong>{System.Net.WebUtility.HtmlEncode(normalizedEmail)}</strong> " +
                     "en dit tijdelijke wachtwoord:")}
                 <p style="margin:16px 0;font-size:20px;letter-spacing:0.06em;font-weight:700;color:{EmailLayout.BrandNavy};text-align:center;"><code>{System.Net.WebUtility.HtmlEncode(temporaryPassword)}</code></p>
                 {EmailLayout.Paragraph(
                     "Vul daarna je KvK/BTW/NAW-gegevens in en onderteken de bemiddelingsovereenkomst om je trackingcode te ontvangen.")}
                 {EmailLayout.MutedNote("Wijzig het wachtwoord zo snel mogelijk na je eerste login.")}
                 """,
                publicWebBaseUrl: null,
                preheader: "Uitnodiging Lobsy salesmanager"),
            "SalesManagerInvite"), cancellationToken);

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
            temporaryPassword,
            createdNew,
            profile.CanRecruitSalesManagers,
            profile.ReferredBySalesManagerUserId);
    }

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#";
        Span<char> chars = stackalloc char[12];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }
}
