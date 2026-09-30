using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
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
    DateTime ExpiresAtUtc,
    bool CreatedNewUser);

public sealed class SchoolStaffInviteService : ISchoolStaffInviteService
{
    private static readonly TimeSpan InviteTtl = TimeSpan.FromDays(7);

    private readonly JobsyDbContext _db;
    private readonly ITransactionalMailer _mailer;
    private readonly IOneTimeLinkService _links;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<SchoolStaffInviteService> _logger;

    public SchoolStaffInviteService(
        JobsyDbContext db,
        ITransactionalMailer mailer,
        IOneTimeLinkService links,
        IPlatformFeatureService features,
        ILogger<SchoolStaffInviteService> logger)
    {
        _db = db;
        _mailer = mailer;
        _links = links;
        _features = features;
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
        }
        else
        {
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
            InvitedUserId = user.Id,
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

        var features = await _features.GetAsync(cancellationToken);
        string? setPasswordUrl = null;
        var hasCredential = await _db.LocalAuthCredentials.AnyAsync(c => c.UserId == user.Id, cancellationToken);
        var hasExternal = await _db.UserExternalLogins.AnyAsync(l => l.UserId == user.Id, cancellationToken);
        if (!hasCredential && !hasExternal)
        {
            var created = await _links.CreateAsync(
                OneTimeLinkPurpose.SetPassword,
                user.Id,
                companyId: null,
                normalizedEmail,
                OneTimeLinkRules.SetPasswordLifetime,
                invitedByUserId,
                cancellationToken);
            setPasswordUrl = EmailLinks.For(features.PublicWebBaseUrl).SetPassword(created.Token);
        }

        var links = EmailLinks.For(features.PublicWebBaseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? "Inloggen" : "Kies je wachtwoord";
        var subject = $"Uitnodiging Lobsy voor scholen — {school.Name}";
        var bodyText = setPasswordUrl is null
            ? $"Log in met {normalizedEmail} om verder te gaan."
            : $"Kies een wachtwoord voor {normalizedEmail} via de knop hieronder.";
        var inviteMail = TransactionalEmails.AdHoc(
            "Scholen.Invite",
            "Scholen.Invite",
            EmailKind.Essential,
            features.PublicWebBaseUrl,
            subject,
            "Je bent uitgenodigd voor Lobsy voor scholen.",
            "Uitnodiging voor scholen",
            [
                new ParagraphBlock(EmailText.Format(
                    "Je bent uitgenodigd voor Lobsy voor scholen bij {0}. Tweestapsverificatie is verplicht.",
                    EmailArg.Bold(school.Name))),
                new ParagraphBlock(EmailText.Plain(bodyText))
            ],
            new EmailCta(ctaLabel, ctaUrl),
            greeting: $"Hallo {name},");
        await _mailer.SendAsync(inviteMail, normalizedEmail, cancellationToken: cancellationToken);

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
            invite.ExpiresAtUtc,
            createdNew);
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

}
