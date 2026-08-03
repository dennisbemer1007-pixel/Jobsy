using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class AmbassadeurInviteService : IAmbassadeurInviteService
{
    private readonly JobsyDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<AmbassadeurInviteService> _logger;

    public AmbassadeurInviteService(
        JobsyDbContext db,
        IEmailService email,
        ILogger<AmbassadeurInviteService> logger)
    {
        _db = db;
        _email = email;
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

        string temporaryPassword;
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
                Role = UserRole.Ambassadeur,
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

        await _email.SendAsync(new EmailMessage(
            normalizedEmail,
            "Uitnodiging Lobsy ambassadeur",
            $"""
             <p>Hallo {System.Net.WebUtility.HtmlEncode(name)},</p>
             <p>Je bent uitgenodigd als ambassadeur op Lobsy.</p>
             <p>Log in met <strong>{System.Net.WebUtility.HtmlEncode(normalizedEmail)}</strong>
             en tijdelijk wachtwoord <strong>{System.Net.WebUtility.HtmlEncode(temporaryPassword)}</strong>.</p>
             <p>Vul daarna je KvK/BTW/NAW-gegevens in en onderteken de bemiddelingsovereenkomst om je trackingcode te ontvangen.</p>
             <p><em>Invite stub — geen echte mail.</em></p>
             """,
            "AmbassadeurInvite"), cancellationToken);

        _logger.LogInformation(
            "Invited ambassadeur {Email} ({UserId})",
            EmailServiceStub.RedactEmail(normalizedEmail),
            user.Id);

        return new AmbassadeurInviteResult(
            user.Id,
            normalizedEmail,
            name,
            temporaryPassword,
            createdNew);
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
