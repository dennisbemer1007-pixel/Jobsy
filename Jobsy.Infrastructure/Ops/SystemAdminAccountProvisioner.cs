using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Core.Privacy;
using Jobsy.Core.Security;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

public static class SystemAdminAccountProvisioner
{
    public const string DisplayName = "Lobsy System Administrator";

    public static async Task ProvisionAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var options = configuration.GetSection(SystemAdminOptions.SectionName).Get<SystemAdminOptions>()
                      ?? new SystemAdminOptions();

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        if (!options.Enabled)
        {
            await DisableExistingAsync(db, logger, cancellationToken);
            return;
        }

        ValidateEnabledConfiguration(options);

        var email = LoginIdentity.Normalize(options.Email!);
        var existingByFlag = await db.Users
            .Where(u => u.IsSystemAdminAccount)
            .ToListAsync(cancellationToken);

        var holder = existingByFlag.FirstOrDefault();
        if (holder is null)
        {
            var emailTaken = await db.Users.AsNoTracking()
                .WhereEmailEquals(email)
                .AnyAsync(u => !u.IsSystemAdminAccount, cancellationToken);
            if (emailTaken)
            {
                throw new InvalidOperationException(
                    $"SystemAdmin startup failed: e-mail {email} is already used by a non-system account.");
            }

            holder = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = DisplayName,
                Role = UserRole.Admin,
                IsActive = true,
                IsSystemAdminAccount = true,
                TermsAcceptedAt = DateTime.UtcNow,
                ConsentVersion = PrivacyConstants.CurrentConsentVersion,
                EmailVerifiedAtUtc = DateTime.UtcNow
            };
            db.Users.Add(holder);
            logger.LogInformation("SystemAdmin account created for {Email}.", email);
        }
        else
        {
            if (existingByFlag.Count > 1)
            {
                foreach (var extra in existingByFlag.Skip(1))
                {
                    extra.IsActive = false;
                    extra.SessionVersion++;
                }

                logger.LogWarning(
                    "SystemAdmin startup: disabled {Count} duplicate system-admin rows.",
                    existingByFlag.Count - 1);
            }

            if (!string.Equals(holder.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await db.Users.AsNoTracking()
                    .AnyAsync(
                        u => u.EmailLookupHash == EmailLookupHash.Compute(email) && u.Id != holder.Id,
                        cancellationToken);
                if (emailTaken)
                {
                    throw new InvalidOperationException(
                        $"SystemAdmin startup failed: cannot move system admin to {email}; address is taken.");
                }

                holder.Email = email;
            }

            holder.FullName = DisplayName;
            holder.Role = UserRole.Admin;
            holder.IsSystemAdminAccount = true;
            holder.IsActive = true;
            if (holder.EmailVerifiedAtUtc is null)
            {
                holder.EmailVerifiedAtUtc = DateTime.UtcNow;
            }
        }

        await EnsurePasswordAsync(db, holder, options.Password!, logger, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "SystemAdmin account provisioned (RequireTwoFactor={RequireTwoFactor}).",
            options.RequireTwoFactor);
    }

    private static void ValidateEnabledConfiguration(SystemAdminOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Email))
        {
            throw new InvalidOperationException(
                "SystemAdmin startup failed: SystemAdmin:Email is required when SystemAdmin:Enabled is true.");
        }

        try
        {
            SystemAdminPasswordRules.Validate(options.Password);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("SystemAdmin startup failed: " + ex.Message, ex);
        }
    }

    private static async Task DisableExistingAsync(
        JobsyDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var rows = await db.Users.Where(u => u.IsSystemAdminAccount && u.IsActive).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        foreach (var user in rows)
        {
            user.IsActive = false;
            user.SessionVersion++;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SystemAdmin disabled: locked {Count} break-glass account(s).", rows.Count);
    }

    private static async Task EnsurePasswordAsync(
        JobsyDbContext db,
        User user,
        string password,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var email = user.Email.Trim().ToLowerInvariant();
        var credential = await db.LocalAuthCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken)
            ?? await db.LocalAuthCredentials
                .WhereCredentialEmailEquals(email)
                .FirstOrDefaultAsync(cancellationToken);

        if (credential is null)
        {
            db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(password)
            });
            return;
        }

        credential.UserId = user.Id;
        credential.Email = email;
        credential.FailedLoginCount = 0;
        credential.LockoutUntil = null;

        if (!JobsyPasswordHasher.Verify(password, credential.PasswordHash)
            || JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(password);
            user.SessionVersion++;
            logger.LogInformation("SystemAdmin password hash updated from environment.");
        }
    }
}
