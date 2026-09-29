using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Scholen;

public interface ISchoolStaffInviteService
{
    Task<SchoolStaffInviteResult> InviteSchoolAdminAsync(
        Guid schoolId,
        string fullName,
        string email,
        Guid invitedByUserId,
        CancellationToken cancellationToken = default);

    Task<SchoolStaffInviteResult> InviteTeacherAsync(
        Guid schoolId,
        string fullName,
        string email,
        IReadOnlyList<Guid> classIds,
        Guid invitedByUserId,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid inviteId, CancellationToken cancellationToken = default);

    Task<SchoolStaffInvite?> FindValidByTokenAsync(string rawToken, CancellationToken cancellationToken = default);
}

public sealed record SchoolStaffInviteResult(
    Guid InviteId,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string TemporaryPassword,
    DateTime ExpiresAtUtc,
    bool CreatedNewUser);

public sealed class SchoolStaffInviteService : ISchoolStaffInviteService
{
    private static readonly TimeSpan InviteTtl = TimeSpan.FromDays(7);

    private readonly JobsyDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SchoolStaffInviteService> _logger;

    public SchoolStaffInviteService(
        JobsyDbContext db,
        IEmailService email,
        ILogger<SchoolStaffInviteService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public Task<SchoolStaffInviteResult> InviteSchoolAdminAsync(
        Guid schoolId,
        string fullName,
        string email,
        Guid invitedByUserId,
        CancellationToken cancellationToken = default)
        => InviteAsync(schoolId, fullName, email, JobsyRoles.SchoolAdmin, [], invitedByUserId, cancellationToken);

    public Task<SchoolStaffInviteResult> InviteTeacherAsync(
        Guid schoolId,
        string fullName,
        string email,
        IReadOnlyList<Guid> classIds,
        Guid invitedByUserId,
        CancellationToken cancellationToken = default)
        => InviteAsync(schoolId, fullName, email, JobsyRoles.Teacher, classIds, invitedByUserId, cancellationToken);

    public async Task RevokeAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var invite = await _db.SchoolStaffInvites.FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken)
            ?? throw new InvalidOperationException("Uitnodiging niet gevonden.");
        invite.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SchoolStaffInvite?> FindValidByTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = HashToken(rawToken.Trim());
        var now = DateTime.UtcNow;
        return await _db.SchoolStaffInvites
            .FirstOrDefaultAsync(
                i => i.TokenHash == hash
                     && i.RevokedAtUtc == null
                     && i.AcceptedAtUtc == null
                     && i.ExpiresAtUtc > now,
                cancellationToken);
    }

    private async Task<SchoolStaffInviteResult> InviteAsync(
        Guid schoolId,
        string fullName,
        string email,
        string role,
        IReadOnlyList<Guid> classIds,
        Guid invitedByUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("E-mail en naam zijn verplicht.");
        }

        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken)
            ?? throw new InvalidOperationException("School niet gevonden.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var name = fullName.Trim();
        EnsureEmailDomainAllowed(school, normalizedEmail);

        if (role == JobsyRoles.Teacher)
        {
            if (classIds.Count == 0)
            {
                throw new ArgumentException("Koppel minstens één klas.");
            }

            var validCount = await _db.SchoolClasses.CountAsync(
                c => c.SchoolId == schoolId && classIds.Contains(c.Id),
                cancellationToken);
            if (validCount != classIds.Distinct().Count())
            {
                throw new InvalidOperationException("Een of meer klassen horen niet bij deze school.");
            }
        }

        var existing = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        string temporaryPassword;
        User user;
        bool createdNew;
        var targetRole = role == JobsyRoles.Teacher ? UserRole.Teacher : UserRole.SchoolAdmin;

        if (existing is not null)
        {
            if (existing.Role is not (UserRole.SchoolAdmin or UserRole.Teacher or UserRole.Candidate))
            {
                throw new InvalidOperationException(
                    "Dit e-mailadres hoort al bij een andere rol en kan niet voor scholen worden uitgenodigd.");
            }

            if (existing.SchoolId is Guid otherSchool && otherSchool != schoolId)
            {
                throw new InvalidOperationException("Dit account is al gekoppeld aan een andere school.");
            }

            existing.FullName = name;
            existing.Role = targetRole;
            existing.IsActive = true;
            existing.SchoolId = schoolId;
            existing.CompanyId = null;
            user = existing;
            createdNew = false;
            temporaryPassword = GenerateTemporaryPassword();
            await UpsertCredentialAsync(user.Id, normalizedEmail, temporaryPassword, cancellationToken);
        }
        else
        {
            temporaryPassword = GenerateTemporaryPassword();
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FullName = name,
                Role = targetRole,
                SchoolId = schoolId,
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

        if (role == JobsyRoles.Teacher)
        {
            foreach (var classId in classIds.Distinct())
            {
                var exists = await _db.TeacherClassAssignments.AnyAsync(
                    a => a.TeacherUserId == user.Id && a.SchoolClassId == classId,
                    cancellationToken);
                if (!exists)
                {
                    _db.TeacherClassAssignments.Add(new TeacherClassAssignment
                    {
                        TeacherUserId = user.Id,
                        SchoolClassId = classId,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        // Revoke prior open invites for same e-mail + school.
        var prior = await _db.SchoolStaffInvites
            .Where(i => i.SchoolId == schoolId
                        && i.Email == normalizedEmail
                        && i.RevokedAtUtc == null
                        && i.AcceptedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var p in prior)
        {
            p.RevokedAtUtc = DateTime.UtcNow;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var invite = new SchoolStaffInvite
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = user.Id,
            Email = normalizedEmail,
            FullName = name,
            Role = role,
            ClassIdsJson = JsonSerializer.Serialize(classIds),
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.Add(InviteTtl),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = invitedByUserId
        };
        _db.SchoolStaffInvites.Add(invite);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Scholen.Audit",
            Message = $"school.staff.invite role={role} school={schoolId:D}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                invite.Id,
                schoolId,
                role,
                email = EmailServiceStub.RedactEmail(normalizedEmail),
                invitedByUserId
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        var subject = $"Uitnodiging Lobsy voor scholen — {school.Name}";
        var html =
            $"<p>Hallo {System.Net.WebUtility.HtmlEncode(name)},</p>" +
            $"<p>Je bent uitgenodigd voor Lobsy voor scholen bij {System.Net.WebUtility.HtmlEncode(school.Name)}. " +
            "Tweestapsverificatie is verplicht.</p>" +
            $"<p>Log in met <strong>{System.Net.WebUtility.HtmlEncode(normalizedEmail)}</strong> " +
            $"en tijdelijk wachtwoord <code>{System.Net.WebUtility.HtmlEncode(temporaryPassword)}</code>. " +
            "Stel daarna een nieuw wachtwoord en 2FA in.</p>";
        await _email.SendAsync(new EmailMessage(normalizedEmail, subject, html, "Scholen.Invite"), cancellationToken);

        _logger.LogInformation(
            "Invited school staff {Role} {Email} for school {SchoolId}",
            role,
            EmailServiceStub.RedactEmail(normalizedEmail),
            schoolId);

        return new SchoolStaffInviteResult(
            invite.Id,
            user.Id,
            normalizedEmail,
            name,
            role,
            temporaryPassword,
            invite.ExpiresAtUtc,
            createdNew);
    }

    private async Task UpsertCredentialAsync(
        Guid userId,
        string email,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        var credential = await _db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (credential is null)
        {
            _db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword)
            });
        }
        else
        {
            credential.Email = email;
            credential.PasswordHash = JobsyPasswordHasher.Hash(temporaryPassword);
        }
    }

    internal static void EnsureEmailDomainAllowed(School school, string normalizedEmail)
    {
        var at = normalizedEmail.LastIndexOf('@');
        if (at < 0 || at == normalizedEmail.Length - 1)
        {
            throw new ArgumentException("Ongeldig e-mailadres.", nameof(normalizedEmail));
        }

        var domain = normalizedEmail[(at + 1)..];
        List<string> allowed;
        try
        {
            allowed = JsonSerializer.Deserialize<List<string>>(school.AllowedEmailDomains) ?? [];
        }
        catch (JsonException)
        {
            allowed = [];
        }

        if (allowed.Count == 0
            || !allowed.Any(d => string.Equals(d.Trim(), domain, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("email_domain_not_allowed");
        }
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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
