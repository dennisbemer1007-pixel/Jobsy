using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>Deterministic ids for local demo school E2E (Development only).</summary>
public static class DemoSchoolIds
{
    public static readonly Guid SchoolId = Guid.Parse("99999999-1111-1111-1111-111111111111");
    public static readonly Guid Class1AId = Guid.Parse("99999999-2222-2222-2222-222222222222");
    public static readonly Guid SchoolAdminUserId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");
    public static readonly Guid TeacherUserId = Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333");
    public static readonly Guid WerkcoachUserId = Guid.Parse("bbbbbbbb-4444-4444-4444-444444444444");
}

public static class DemoGemeenteIds
{
    public static readonly Guid CompanyId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid RegionalUserId = Guid.Parse("cccccccc-2222-2222-2222-222222222222");
}

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

        try
        {
            await PassportPdfMartaDemoSeeder.EnsureAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Passport PDF Marta demo seed failed; continuing.");
        }

        added += await EnsureUserAsync(db, new User
        {
            Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            Email = "sanne@jobsy.local",
            FullName = "Sanne de Vries",
            FirstName = "Sanne",
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

        // Dedicated e2e candidate. The test fixture can reset this deterministic account
        // without altering the public candidate demo profile.
        added += await EnsureUserAsync(db, new User
        {
            Id = Guid.Parse("aaaaaaaa-4444-4444-4444-444444444444"),
            Email = "onboarding.e2e@jobsy.local",
            FullName = "Onboarding E2E",
            Role = UserRole.Candidate,
            CompanyId = null,
            OpenForWork = true,
            IsActive = true
        });

        // Named test candidate (login email + display name Twalieb).
        added += await EnsureUserAsync(
            db,
            new User
            {
                Id = Guid.Parse("aaaaaaaa-6666-6666-6666-666666666666"),
                Email = "twalieb@jobsy.local",
                FullName = "Twalieb",
                Role = UserRole.Candidate,
                CompanyId = null,
                DateOfBirth = new DateOnly(1996, 6, 15),
                OpenForWork = true,
                HomeLocation = new GeoPoint(51.9930, 4.2150),
                PreferencesJson = """{"roles":["horeca","retail","logistiek"],"maxTravelMinutes":35}""",
                IsActive = true
            },
            password: "Lobsy123!");

        // Named test candidate Valentine (Acceptatie login).
        added += await EnsureUserAsync(db, new User
        {
            Id = Guid.Parse("aaaaaaaa-8888-8888-8888-888888888888"),
            Email = "valentine@jobsy.local",
            FullName = "Valentine",
            FirstName = "Valentine",
            Role = UserRole.Candidate,
            CompanyId = null,
            DateOfBirth = new DateOnly(1999, 2, 14),
            OpenForWork = true,
            HomeLocation = new GeoPoint(51.9850, 4.2300),
            PreferencesJson = """{"roles":["horeca","retail"],"maxTravelMinutes":30}""",
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
            FullName = "Linda de Wit",
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

        added += await EnsureDemoSchoolStaffAsync(db);
        added += await EnsureDemoGemeentePartnerAsync(db);

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

    private static Task<int> EnsureUserAsync(JobsyDbContext db, User template)
        => EnsureUserAsync(db, template, DemoPassword);

    private static async Task<int> EnsureUserAsync(JobsyDbContext db, User template, string password)
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

            if (template.SchoolId is Guid schoolId && existing.SchoolId != schoolId)
            {
                existing.SchoolId = schoolId;
            }

            if (!string.Equals(existing.FullName, template.FullName, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(template.FullName))
            {
                existing.FullName = template.FullName;
            }

            // Demo accounts stay on the current consent version so version bumps don't block the public demo.
            StampDemoConsent(existing);
            await EnsurePasswordAsync(db, existing, password);

            return 0;
        }

        // Avoid PK collisions if the email is new but the deterministic id already exists.
        if (await db.Users.AnyAsync(u => u.Id == template.Id))
        {
            template.Id = Guid.NewGuid();
        }

        db.Users.Add(template);
        await EnsurePasswordAsync(db, template, password);
        return 1;
    }

    private static async Task EnsurePasswordAsync(JobsyDbContext db, User user, string password)
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
                PasswordHash = JobsyPasswordHasher.Hash(password)
            });
            return;
        }

        credential.UserId = user.Id;
        credential.Email = email;
        if (!JobsyPasswordHasher.Verify(password, credential.PasswordHash)
            || JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(password);
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

    private static async Task<int> EnsureDemoSchoolStaffAsync(JobsyDbContext db)
    {
        var added = 0;
        var schoolAdminId = DemoSchoolIds.SchoolAdminUserId;
        var teacherId = DemoSchoolIds.TeacherUserId;
        var coachId = DemoSchoolIds.WerkcoachUserId;

        added += await EnsureUserAsync(db, new User
        {
            Id = schoolAdminId,
            Email = "schoolbeheer@jobsy.local",
            FullName = "Demo Schoolbeheerder",
            Role = UserRole.SchoolAdmin,
            SchoolId = DemoSchoolIds.SchoolId,
            IsActive = true
        });

        added += await EnsureUserAsync(db, new User
        {
            Id = teacherId,
            Email = "leraar@jobsy.local",
            FullName = "Demo Leraar",
            Role = UserRole.Teacher,
            SchoolId = DemoSchoolIds.SchoolId,
            IsActive = true
        });

        added += await EnsureUserAsync(db, new User
        {
            Id = coachId,
            Email = "werkcoach@jobsy.local",
            FullName = "Demo Werkcoach",
            Role = UserRole.Teacher,
            SchoolId = DemoSchoolIds.SchoolId,
            IsActive = true
        });

        var now = DateTime.UtcNow;
        var school = await db.Schools.FirstOrDefaultAsync(s => s.Id == DemoSchoolIds.SchoolId);
        if (school is null)
        {
            school = new School
            {
                Id = DemoSchoolIds.SchoolId,
                Name = "Demo College Lobsy",
                City = "Naaldwijk",
                BrinCode = "99ZZ00",
                AllowedEmailDomains = """["jobsy.local"]""",
                IsActive = true,
                IsTestData = true,
                CreatedAtUtc = now,
                CreatedByUserId = schoolAdminId,
                ProcessorAgreementSignedOn = DateOnly.FromDateTime(now),
                ProcessorAgreementVersion = "demo-local"
            };
            db.Schools.Add(school);
            added++;
        }

        var yearStart = SchoolYear.Current(DateOnly.FromDateTime(now));
        var schoolClass = await db.SchoolClasses.FirstOrDefaultAsync(c => c.Id == DemoSchoolIds.Class1AId);
        if (schoolClass is null)
        {
            schoolClass = new SchoolClass
            {
                Id = DemoSchoolIds.Class1AId,
                SchoolId = DemoSchoolIds.SchoolId,
                Name = "2A",
                Level = SchoolLevel.VmboGt,
                QuestionSet = PupilQuestionSet.Vo,
                Year = 2,
                SchoolYearStart = yearStart,
                PupilCount = 5,
                IsTestData = true,
                CreatedAtUtc = now
            };
            db.SchoolClasses.Add(schoolClass);
            added++;
        }

        foreach (var uid in new[] { teacherId, coachId })
        {
            if (!await db.TeacherClassAssignments.AnyAsync(
                    a => a.TeacherUserId == uid && a.SchoolClassId == DemoSchoolIds.Class1AId))
            {
                db.TeacherClassAssignments.Add(new TeacherClassAssignment
                {
                    TeacherUserId = uid,
                    SchoolClassId = DemoSchoolIds.Class1AId,
                    CreatedAtUtc = now
                });
                added++;
            }
        }

        return added;
    }

    private static async Task<int> EnsureDemoGemeentePartnerAsync(JobsyDbContext db)
    {
        var added = 0;
        if (!await db.Companies.AnyAsync(c => c.Id == DemoGemeenteIds.CompanyId))
        {
            db.Companies.Add(new Company
            {
                Id = DemoGemeenteIds.CompanyId,
                Name = "Gemeente Westland (demo)",
                KvkNumber = "99887766",
                KvkEstablishmentId = "99887766_0001",
                Address = "Raadhuisplein 1, Naaldwijk",
                Type = CompanyType.Employer,
                Location = new GeoPoint(51.994, 4.209),
                VerificationStatus = CompanyVerificationStatus.Verified,
                VerificationMethod = CompanyVerificationMethod.Manual,
                VerifiedAtUtc = DateTime.UtcNow,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            });
            added++;
        }

        added += await EnsureUserAsync(db, new User
        {
            Id = DemoGemeenteIds.RegionalUserId,
            Email = "gemeente@jobsy.local",
            FullName = "Demo Gemeente Regio",
            Role = UserRole.RegionalManager,
            CompanyId = DemoGemeenteIds.CompanyId,
            IsActive = true
        });

        await EnsureMembershipAsync(db, DemoGemeenteIds.RegionalUserId, DemoGemeenteIds.CompanyId);
        return added;
    }
}
