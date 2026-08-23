using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class DemoUsersSeeder
{
    /// <summary>Documented public-demo password for every @jobsy.local seed account.</summary>
    public const string DemoPassword = "Jobsy123!";

    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SupermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid IntermediaryCompanyId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static async Task SeedUsersAsync(JobsyDbContext db, ILogger logger)
    {
        var added = 0;

        added += await EnsureUserAsync(db, new User
        {
            Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            Email = "kandidaat@jobsy.local",
            FullName = "Demo Kandidaat",
            Role = UserRole.Candidate,
            CompanyId = null,
            DateOfBirth = new DateOnly(1998, 4, 12),
            OpenForWork = true,
            HomeLocation = new GeoPoint(51.9850, 4.2300),
            PreferencesJson = """{"roles":["horeca","retail"],"maxTravelMinutes":30}""",
            IsEarlyAdapter = true,
            IsActive = true
        });

        added += await EnsureUserAsync(db, new User
        {
            Id = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
            Email = "kandidaat.denhaag@jobsy.local",
            FullName = "Demo Kandidaat Den Haag",
            Role = UserRole.Candidate,
            CompanyId = null,
            DateOfBirth = new DateOnly(1995, 8, 3),
            OpenForWork = true,
            HomeLocation = new GeoPoint(52.0780, 4.3100),
            PreferencesJson = """{"roles":["horeca"],"maxTravelMinutes":25}""",
            IsActive = true
        });

        added += await EnsureUserAsync(db, new User
        {
            Id = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333"),
            Email = "kandidaat.ver@jobsy.local",
            FullName = "Demo Kandidaat Ver Weg",
            Role = UserRole.Candidate,
            CompanyId = null,
            DateOfBirth = new DateOnly(2000, 1, 15),
            OpenForWork = true,
            HomeLocation = new GeoPoint(52.3700, 4.8950),
            PreferencesJson = """{"roles":["retail"],"maxTravelMinutes":40}""",
            IsActive = true
        });

        var branchManagerId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
        added += await EnsureUserAsync(db, new User
        {
            Id = branchManagerId,
            Email = "ondernemer@jobsy.local",
            FullName = "Demo Filiaalmanager",
            Role = UserRole.BranchManager,
            CompanyId = WestlandId,
            IsActive = true
        });

        var regionalManagerId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");
        added += await EnsureUserAsync(db, new User
        {
            Id = regionalManagerId,
            Email = "regio@jobsy.local",
            FullName = "Regiomanager Den Haag",
            Role = UserRole.RegionalManager,
            CompanyId = CafeId,
            IsActive = true
        });

        var enterpriseManagerId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
        added += await EnsureUserAsync(db, new User
        {
            Id = enterpriseManagerId,
            Email = "enterprise@jobsy.local",
            FullName = "Bedrijfsmanager Jobsy Retail",
            Role = UserRole.EnterpriseManager,
            CompanyId = SupermarketId,
            IsActive = true
        });

        var intermediaryId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
        added += await EnsureUserAsync(db, new User
        {
            Id = intermediaryId,
            Email = "intermediair@jobsy.local",
            FullName = "Intermediair Demo",
            Role = UserRole.Intermediary,
            CompanyId = IntermediaryCompanyId,
            IsActive = true
        });

        var adminId = Guid.Parse("ffffffff-1111-1111-1111-111111111111");
        added += await EnsureUserAsync(db, new User
        {
            Id = adminId,
            Email = "admin@jobsy.local",
            FullName = "Platform Admin",
            Role = UserRole.Admin,
            CompanyId = null,
            IsActive = true
        });

        var salesManagerId = Guid.Parse("aaaaaaaa-5555-5555-5555-555555555555");
        added += await EnsureUserAsync(db, new User
        {
            Id = salesManagerId,
            Email = "sales@jobsy.local",
            FullName = "Demo Salesmanager",
            Role = UserRole.SalesManager,
            CompanyId = null,
            IsActive = true
        });

        await EnsureSalesManagerProfileAsync(db, salesManagerId);

        var ambassadeurId = Guid.Parse("aaaaaaaa-7777-7777-7777-777777777777");
        added += await EnsureUserAsync(db, new User
        {
            Id = ambassadeurId,
            Email = "ambassadeur@jobsy.local",
            FullName = "Demo Ambassadeur",
            Role = UserRole.Ambassadeur,
            CompanyId = null,
            IsActive = true
        });

        await EnsureAmbassadeurProfileAsync(db, ambassadeurId);

        await EnsureMembershipAsync(db, branchManagerId, WestlandId);
        await EnsureMembershipAsync(db, regionalManagerId, CafeId);
        await EnsureMembershipAsync(db, regionalManagerId, SupermarketId);
        await EnsureMembershipAsync(db, enterpriseManagerId, SupermarketId);
        await EnsureMembershipAsync(db, enterpriseManagerId, CafeId);
        await EnsureMembershipAsync(db, enterpriseManagerId, WestlandId);
        await EnsureMembershipAsync(db, intermediaryId, WestlandId);
        await EnsureMembershipAsync(db, intermediaryId, CafeId);
        await EnsureMembershipAsync(db, intermediaryId, SupermarketId);

        if (added > 0 || db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync();
        }

        if (added > 0)
        {
            logger.LogInformation("Ensured {Count} demo role user(s) (candidates, managers, intermediary, admin).", added);
        }
    }

    private static async Task<int> EnsureUserAsync(JobsyDbContext db, User template)
    {
        StampDemoConsent(template);

        var email = template.Email.ToLowerInvariant();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (existing is not null)
        {
            // Keep accounts active and roles aligned with demo expectations.
            if (!existing.IsActive)
            {
                existing.IsActive = true;
            }

            if (existing.Role != template.Role)
            {
                existing.Role = template.Role;
            }

            if (template.CompanyId is Guid companyId && existing.CompanyId != companyId)
            {
                existing.CompanyId = companyId;
            }

            // Demo accounts stay on the current consent version so version bumps don't block the public demo.
            StampDemoConsent(existing);
            await EnsureDemoPasswordAsync(db, existing);

            return 0;
        }

        // Avoid PK collisions if the email is new but the deterministic id already exists.
        if (await db.Users.AnyAsync(u => u.Id == template.Id))
        {
            template.Id = Guid.NewGuid();
        }

        db.Users.Add(template);
        await EnsureDemoPasswordAsync(db, template);
        return 1;
    }

    /// <summary>
    /// Creates or resets the local-login hash so demo accounts always accept <see cref="DemoPassword"/>.
    /// Production web login uses <c>POST api/auth/local-login</c> (demo-store is Development-only unless allowed).
    /// </summary>
    private static async Task EnsureDemoPasswordAsync(JobsyDbContext db, User user)
    {
        var email = user.Email.Trim().ToLowerInvariant();
        var credential = await db.LocalAuthCredentials.FirstOrDefaultAsync(c => c.UserId == user.Id)
            ?? await db.LocalAuthCredentials.FirstOrDefaultAsync(c => c.Email == email);

        if (credential is null)
        {
            db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(DemoPassword)
            });
            return;
        }

        credential.UserId = user.Id;
        credential.Email = email;
        if (!JobsyPasswordHasher.Verify(DemoPassword, credential.PasswordHash)
            || JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(DemoPassword);
        }
    }

    private static void StampDemoConsent(User user)
    {
        if (user.Role == UserRole.Candidate)
        {
            return;
        }

        user.TermsAcceptedAt ??= DateTime.UtcNow;
        user.ConsentVersion = PrivacyConstants.CurrentConsentVersion;
    }

    private static async Task EnsureMembershipAsync(JobsyDbContext db, Guid userId, Guid companyId)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return;
        }

        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        var exists = await db.UserCompanies.AnyAsync(m => m.UserId == userId && m.CompanyId == companyId);
        if (!exists)
        {
            db.UserCompanies.Add(new UserCompany { UserId = userId, CompanyId = companyId });
        }
    }

    private static async Task EnsureSalesManagerProfileAsync(JobsyDbContext db, Guid userId)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        if (await db.SalesManagerProfiles.AnyAsync(p => p.UserId == userId))
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.Parse("aaaaaaaa-5555-6666-7777-555555555555"),
            UserId = userId,
            CompanyName = "Demo Sales BV",
            KvkNumber = "87654321",
            VatNumber = "NL87654321B01",
            Address = "Voorbeeldstraat 1",
            PostalCode = "2671AB",
            City = "Naaldwijk",
            Country = "NL",
            Iban = "NL91ABNA0417164300",
            TrackingCode = "SM-DEMO01",
            AgreementSignedAt = now,
            AgreementVersion = "2026-07-27-sm-mediation",
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private static async Task EnsureAmbassadeurProfileAsync(JobsyDbContext db, Guid userId)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        if (await db.AmbassadeurProfiles.AnyAsync(p => p.UserId == userId))
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.Parse("aaaaaaaa-7777-8888-9999-777777777777"),
            UserId = userId,
            CompanyName = "Demo Ambassadeur BV",
            KvkNumber = "11223344",
            VatNumber = "NL11223344B01",
            Address = "Ambassadeurlaan 5",
            PostalCode = "2671CD",
            City = "Naaldwijk",
            Country = "NL",
            Iban = "NL91ABNA0417164300",
            TrackingCode = "AM-DEMO01",
            BaseCommissionPercentage = 5.0m,
            AgreementSignedAt = now,
            AgreementVersion = AmbassadeurCommissionRules.CurrentAgreementVersion,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        if (!await db.AmbassadeurSettings.AnyAsync())
        {
            db.AmbassadeurSettings.Add(new AmbassadeurSettings
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                CandidateThreshold = AmbassadeurCommissionRules.DefaultCandidateThreshold,
                PercentPerThreshold = AmbassadeurCommissionRules.DefaultPercentPerThreshold,
                MaxCommissionPercentage = AmbassadeurCommissionRules.DefaultMaxCommissionPercentage,
                UpdatedAtUtc = now
            });
        }
    }
}
