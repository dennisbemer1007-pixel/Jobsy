using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class DemoUsersSeeder
{
    public static async Task SeedUsersAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var westlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var supermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var candidate = new User
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
        };

        var candidateDenHaag = new User
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
        };

        var candidateFar = new User
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
        };

        var branchManager = new User
        {
            Id = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"),
            Email = "ondernemer@jobsy.local",
            FullName = "Demo Filiaalmanager",
            Role = UserRole.BranchManager,
            CompanyId = westlandId,
            IsActive = true
        };

        var regionalManager = new User
        {
            Id = Guid.Parse("cccccccc-1111-1111-1111-111111111111"),
            Email = "regio@jobsy.local",
            FullName = "Regiomanager Den Haag",
            Role = UserRole.RegionalManager,
            CompanyId = cafeId,
            IsActive = true
        };

        var enterpriseManager = new User
        {
            Id = Guid.Parse("dddddddd-1111-1111-1111-111111111111"),
            Email = "enterprise@jobsy.local",
            FullName = "Bedrijfsmanager Jobsy Retail",
            Role = UserRole.EnterpriseManager,
            CompanyId = supermarketId,
            IsActive = true
        };

        var intermediary = new User
        {
            Id = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111"),
            Email = "intermediair@jobsy.local",
            FullName = "Intermediair Demo",
            Role = UserRole.Intermediary,
            CompanyId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            IsActive = true
        };

        var admin = new User
        {
            Id = Guid.Parse("ffffffff-1111-1111-1111-111111111111"),
            Email = "admin@jobsy.local",
            FullName = "Platform Admin",
            Role = UserRole.Admin,
            CompanyId = null,
            IsActive = true
        };

        db.Users.AddRange(
            candidate, candidateDenHaag, candidateFar,
            branchManager, regionalManager, enterpriseManager, intermediary, admin);

        db.UserCompanies.AddRange(
            new UserCompany { UserId = branchManager.Id, CompanyId = westlandId },
            new UserCompany { UserId = regionalManager.Id, CompanyId = cafeId },
            new UserCompany { UserId = regionalManager.Id, CompanyId = supermarketId },
            new UserCompany { UserId = enterpriseManager.Id, CompanyId = supermarketId },
            new UserCompany { UserId = enterpriseManager.Id, CompanyId = cafeId },
            new UserCompany { UserId = enterpriseManager.Id, CompanyId = westlandId },
            new UserCompany { UserId = intermediary.Id, CompanyId = westlandId },
            new UserCompany { UserId = intermediary.Id, CompanyId = cafeId },
            new UserCompany { UserId = intermediary.Id, CompanyId = supermarketId });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded role users (candidates with HomeLocation, managers, intermediary, admin).");
    }
}
