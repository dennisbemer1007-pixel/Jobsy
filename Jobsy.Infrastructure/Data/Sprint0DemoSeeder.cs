using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class Sprint0DemoSeeder
{
    public static async Task SeedSprint0DemoAsync(JobsyDbContext db, ILogger logger)
    {
        // Never mass-reactivate users — admin deactivation must stick across restarts.

        var candidate = await db.Users.FirstOrDefaultAsync(u => u.Email == "kandidaat@jobsy.local");
        if (candidate is not null)
        {
            if (candidate.DateOfBirth is null)
            {
                candidate.DateOfBirth = new DateOnly(1998, 4, 12);
                candidate.OpenForWork = true;
                candidate.IsEarlyAdapter = true;
                candidate.PreferencesJson ??= """{"roles":["horeca","retail"],"maxTravelMinutes":30}""";
            }

            candidate.HomeLocation ??= new Core.ValueObjects.GeoPoint(51.9850, 4.2300);
        }

        await EnsurePushBomDemoCandidatesAsync(db);

        foreach (var vacancy in await db.Vacancies.Where(v => v.MaxApplications == 0).ToListAsync())
        {
            vacancy.MaxApplications = 5;
        }

        // Backfill branche/work types for demo vacancies that predate the column.
        foreach (var vacancy in await db.Vacancies.Where(v => v.WorkTypes == WorkType.None).ToListAsync())
        {
            vacancy.WorkTypes = vacancy.Id switch
            {
                var id when id == Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
                    => WorkType.Logistiek | WorkType.Tuinbouw,
                var id when id == Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                    => WorkType.Horeca,
                var id when id == Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
                    => WorkType.Winkel,
                _ => InferWorkTypeFromTitle(vacancy.Title)
            };
        }

        // Backfill company establishment ids + types (unique-safe).
        var usedEstablishmentIds = (await db.Companies
            .Where(c => c.KvkEstablishmentId != null)
            .Select(c => c.KvkEstablishmentId!)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var company in await db.Companies.Where(c => c.KvkEstablishmentId == null).ToListAsync())
        {
            if (string.IsNullOrWhiteSpace(company.KvkNumber))
            {
                continue;
            }

            var establishmentId = $"{company.KvkNumber}_0001";
            if (usedEstablishmentIds.Contains(establishmentId))
            {
                var suffix = 2;
                do
                {
                    establishmentId = $"{company.KvkNumber}_{suffix:D4}";
                    suffix++;
                } while (usedEstablishmentIds.Contains(establishmentId) && suffix < 1000);
            }

            company.KvkEstablishmentId = establishmentId;
            usedEstablishmentIds.Add(establishmentId);
        }

        // Backfill legacy token rows without Kind metadata
        foreach (var tx in await db.TokenTransactions.Where(t => t.NewBalance == 0 && t.OldBalance == 0 && t.Amount != 0).ToListAsync())
        {
            if (tx.Amount > 0 && tx.Kind == TokenTransactionKind.Purchase && tx.Reason == TokenSpendReason.None && tx.Note is null)
            {
                tx.Kind = TokenTransactionKind.Grant;
                tx.OldBalance = 0;
                tx.NewBalance = tx.Amount;
                tx.Note ??= "Backfilled grant";
            }
            else if (tx.Amount < 0)
            {
                tx.Kind = TokenTransactionKind.Spend;
                tx.Reason = tx.Reason == TokenSpendReason.None ? TokenSpendReason.Publish : tx.Reason;
                tx.OldBalance = Math.Abs(tx.Amount);
                tx.NewBalance = 0;
            }
        }

        if (!await db.VacancyClicks.AnyAsync())
        {
            var vacancies = await db.Vacancies.AsNoTracking().Select(v => v.Id).ToListAsync();
            var candidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
            var now = DateTime.UtcNow;
            foreach (var vacancyId in vacancies)
            {
                db.VacancyClicks.AddRange(
                    new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, UserId = candidateId, CreatedAt = now.AddHours(-3) },
                    new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "anon-demo", CreatedAt = now.AddHours(-1) });
                db.VacancyLikes.Add(new VacancyLike
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancyId,
                    UserId = candidateId,
                    CreatedAt = now.AddHours(-2)
                });
                db.VacancyShares.Add(new VacancyShare
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancyId,
                    UserId = candidateId,
                    Channel = ShareChannel.WhatsApp,
                    CreatedAt = now.AddHours(-4)
                });
            }
        }

        // Sprint 3: ensure demo candidate has at least one linked application for /home metrics.
        var demoCandidate = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "kandidaat@jobsy.local");
        if (demoCandidate is not null
            && !await db.Applications.AnyAsync(a => a.CandidateUserId == demoCandidate.Id))
        {
            var vacancyId = await db.Vacancies.AsNoTracking().Select(v => v.Id).FirstOrDefaultAsync();
            if (vacancyId != Guid.Empty)
            {
                db.Applications.Add(new Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancyId,
                    CandidateUserId = demoCandidate.Id,
                    CandidateName = demoCandidate.FullName,
                    CandidateEmail = demoCandidate.Email,
                    CandidateCity = "Den Haag",
                    PreferredTransport = "Fiets",
                    EstimatedTravelMinutes = 14,
                    DistanceKm = 3.2,
                    Status = ApplicationStatus.Pending,
                    PreferencesSummary = demoCandidate.PreferencesJson,
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    EmailVerifiedAt = DateTime.UtcNow
                });
            }
        }

        if (!await db.PlatformLogs.AnyAsync())
        {
            db.PlatformLogs.AddRange(
                new PlatformLog
                {
                    Id = Guid.NewGuid(),
                    Level = PlatformLogLevel.Info,
                    Category = "Seed",
                    Message = "Sprint 0 platform log seed",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new PlatformLog
                {
                    Id = Guid.NewGuid(),
                    Level = PlatformLogLevel.Error,
                    Category = "Integration",
                    Message = "Demo error: Mollie stub timeout simulation",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                });
        }

        var westlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Demo purchase + spend ledger rows for metrics
        if (!await db.TokenTransactions.AnyAsync(t => t.Kind == TokenTransactionKind.Purchase))
        {
            var balance = await db.TokenTransactions.Where(t => t.CompanyId == westlandId).SumAsync(t => t.Amount);
            db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = westlandId,
                Amount = 10m,
                Kind = TokenTransactionKind.Purchase,
                Reason = TokenSpendReason.None,
                OldBalance = balance,
                NewBalance = balance + 10m,
                Note = "Stub Mollie pack 10",
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            });
        }

        var supermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var cafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Demo retail group: vestigingen under Supermarkt De Fred.
        foreach (var branchId in new[] { cafeId, westlandId })
        {
            var branch = await db.Companies.FirstOrDefaultAsync(c => c.Id == branchId);
            if (branch is not null && branch.ParentCompanyId is null)
            {
                branch.ParentCompanyId = supermarketId;
            }

            if (branch is not null && !branch.TokensManagedByEnterprise)
            {
                branch.TokensManagedByEnterprise = true;
            }
        }

        // Consolidate leftover vestiging WML copies onto the org table.
        var orgWml = await db.CompanySalaryTables
            .FirstOrDefaultAsync(t => t.CompanyId == supermarketId && t.IsSystemWml);
        if (orgWml is not null)
        {
            var vestigingWmls = await db.CompanySalaryTables
                .Where(t => t.IsSystemWml && (t.CompanyId == cafeId || t.CompanyId == westlandId))
                .ToListAsync();
            foreach (var extra in vestigingWmls)
            {
                foreach (var vacancy in await db.Vacancies.Where(v => v.SalaryTableId == extra.Id).ToListAsync())
                {
                    vacancy.SalaryTableId = orgWml.Id;
                }

                db.CompanySalaryTables.Remove(extra);
            }
        }

        if (!await db.Regions.AnyAsync())
        {
            var region = new Region
            {
                Id = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
                OrganizationCompanyId = supermarketId,
                Name = "Den Haag Stad"
            };
            db.Regions.Add(region);
            db.RegionCompanies.AddRange(
                new RegionCompany { RegionId = region.Id, CompanyId = supermarketId },
                new RegionCompany { RegionId = region.Id, CompanyId = cafeId });
        }

        if (!await db.CompanySalaryTables.AnyAsync(t => t.Name == "De Fred CAO schaal"))
        {
            var table = new CompanySalaryTable
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                CompanyId = supermarketId,
                Name = "De Fred CAO schaal",
                IsActive = true,
                IsSystemWml = false
            };
            db.CompanySalaryTables.Add(table);
            db.CompanySalaryRates.AddRange(
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = table.Id, AgeYears = 15, HourlyRate = 4.50m, Label = "15" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = table.Id, AgeYears = 16, HourlyRate = 5.20m, Label = "16" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = table.Id, AgeYears = 17, HourlyRate = 5.90m, Label = "17" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = table.Id, AgeYears = 18, HourlyRate = 8.00m, Label = "18" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = table.Id, AgeYears = 19, HourlyRate = 9.50m, Label = "19" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = table.Id, AgeYears = 20, HourlyRate = 11.50m, Label = "20" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = table.Id, AgeYears = 21, HourlyRate = 14.50m, Label = "21+" });
            db.CompanySalaryTableAllowedBranches.Add(new CompanySalaryTableAllowedBranch
            {
                SalaryTableId = table.Id,
                CompanyId = supermarketId
            });
        }
        else
        {
            // Ensure demo CAO table remains usable by the supermarket vestiging/org.
            var cao = await db.CompanySalaryTables
                .Include(t => t.AllowedBranches)
                .FirstOrDefaultAsync(t => t.Name == "De Fred CAO schaal");
            if (cao is not null
                && cao.AllowedBranches.All(b => b.CompanyId != supermarketId))
            {
                db.CompanySalaryTableAllowedBranches.Add(new CompanySalaryTableAllowedBranch
                {
                    SalaryTableId = cao.Id,
                    CompanyId = supermarketId
                });
            }
        }

        var fredVacancyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var fredVacancy = await db.Vacancies.FirstOrDefaultAsync(v => v.Id == fredVacancyId);
        if (fredVacancy is not null && fredVacancy.SalaryTableId is null)
        {
            var tableId = await db.CompanySalaryTables
                .Where(t => t.CompanyId == supermarketId)
                .Select(t => (Guid?)t.Id)
                .FirstOrDefaultAsync();
            if (tableId is not null)
            {
                fredVacancy.SalaryTableId = tableId;
            }
        }

        var fredTable = await db.CompanySalaryTables
            .Include(t => t.Rates)
            .FirstOrDefaultAsync(t => t.CompanyId == supermarketId);
        if (fredTable is not null && fredTable.Rates.Count < 5)
        {
            var existingAges = fredTable.Rates.Select(r => r.AgeYears).ToHashSet();
            var extras = new (int Age, decimal Rate, string Label)[]
            {
                (15, 4.50m, "15"),
                (16, 5.20m, "16"),
                (17, 5.90m, "17"),
                (18, 8.00m, "18"),
                (19, 9.50m, "19"),
                (20, 11.50m, "20"),
                (21, 14.50m, "21+")
            };
            foreach (var (age, rate, label) in extras)
            {
                if (existingAges.Contains(age))
                {
                    continue;
                }

                db.CompanySalaryRates.Add(new CompanySalaryRate
                {
                    Id = Guid.NewGuid(),
                    SalaryTableId = fredTable.Id,
                    AgeYears = age,
                    HourlyRate = rate,
                    Label = label
                });
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Sprint 0 demo data ensured (engagement, logs, regions, salary tables).");
    }

    private static WorkType InferWorkTypeFromTitle(string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("barista") || t.Contains("horeca") || t.Contains("zomerhulp"))
        {
            return WorkType.Horeca;
        }

        if (t.Contains("retail") || t.Contains("vakken") || t.Contains("kassa"))
        {
            return WorkType.Winkel;
        }

        if (t.Contains("orderpicker") || t.Contains("logistiek"))
        {
            return WorkType.Logistiek;
        }

        if (t.Contains("kas") || t.Contains("tuinbouw") || t.Contains("seizoen"))
        {
            return WorkType.Tuinbouw;
        }

        return WorkType.Winkel;
    }

    private static async Task EnsurePushBomDemoCandidatesAsync(JobsyDbContext db)
    {
        async Task EnsureCandidateAsync(
            Guid id,
            string email,
            string fullName,
            DateOnly dob,
            Core.ValueObjects.GeoPoint home,
            string preferences)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing is null)
            {
                db.Users.Add(new User
                {
                    Id = id,
                    Email = email,
                    FullName = fullName,
                    Role = UserRole.Candidate,
                    DateOfBirth = dob,
                    OpenForWork = true,
                    HomeLocation = home,
                    PreferencesJson = preferences,
                    IsActive = true
                });
                return;
            }

            existing.OpenForWork = true;
            existing.HomeLocation ??= home;
            existing.PreferencesJson ??= preferences;
        }

        await EnsureCandidateAsync(
            Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
            "kandidaat.denhaag@jobsy.local",
            "Demo Kandidaat Den Haag",
            new DateOnly(1995, 8, 3),
            new Core.ValueObjects.GeoPoint(52.0780, 4.3100),
            """{"roles":["horeca"],"maxTravelMinutes":25}""");

        await EnsureCandidateAsync(
            Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333"),
            "kandidaat.ver@jobsy.local",
            "Demo Kandidaat Ver Weg",
            new DateOnly(2000, 1, 15),
            new Core.ValueObjects.GeoPoint(52.3700, 4.8950),
            """{"roles":["retail"],"maxTravelMinutes":40}""");

        // Ensure enterprise manager can approve Westland pending publishes.
        var westlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var enterprise = await db.Users.FirstOrDefaultAsync(u => u.Email == "enterprise@jobsy.local");
        if (enterprise is not null)
        {
            var hasWestland = await db.UserCompanies.AnyAsync(uc =>
                uc.UserId == enterprise.Id && uc.CompanyId == westlandId);
            if (!hasWestland)
            {
                db.UserCompanies.Add(new UserCompany { UserId = enterprise.Id, CompanyId = westlandId });
            }
        }
    }
}
