using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent backfill so admin/employer/candidate dashboards have non-zero metrics across day/week/month.
/// </summary>
internal static class Sprint8MetricsSeeder
{
    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SupermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid IntermediaryCompanyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid CandidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid OrderpickerVacancyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BaristaVacancyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid RetailVacancyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid IntermediaryVacancyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid DraftVacancyId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PendingVacancyId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid ArchivedVacancyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public static async Task SeedRichMetricsAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.PlatformLogs.AnyAsync(l =>
                l.Category == "Seed" && l.Message == "Sprint 8 rich metrics seed"))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        await EnsureIntermediaryVacancyAsync(db, today);
        await EnsureStatusVarietyVacanciesAsync(db, today);
        await EnrichVacancyFlagsAsync(db);
        await db.SaveChangesAsync();

        await SeedSpendLedgerAsync(db, now);
        await SeedTimeSpreadEngagementAsync(db, now);
        await EnrichApplicationsAsync(db, now);
        await EnrichPlatformLogsAsync(db, now);
        await SeedAllocationAsync(db, now);

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Seed",
            Message = "Sprint 8 rich metrics seed",
            CreatedAt = now
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Sprint 8 rich metrics/logs seeded for dashboards.");
    }

    private static async Task EnsureIntermediaryVacancyAsync(JobsyDbContext db, DateOnly today)
    {
        if (await db.Vacancies.AnyAsync(v =>
                v.CompanyId == IntermediaryCompanyId && v.Status == VacancyStatus.Active))
        {
            return;
        }

        if (!await db.Companies.AnyAsync(c => c.Id == IntermediaryCompanyId))
        {
            return;
        }

        db.Vacancies.Add(new Vacancy
        {
            Id = IntermediaryVacancyId,
            Title = "Flex medewerker retail (pool)",
            Description = MockVacancyMedia.BuildRichDescription(
                "Flex medewerker retail (pool)",
                "Intermediair-vacature voor meerdere retailopdrachtgevers in Den Haag.",
                "Demo Intermediair Flex BV",
                WorkType.Winkel,
                14.00m,
                10),
            HourlyWage = 14.00m,
            StartDate = today,
            EndDate = today.AddMonths(2),
            Status = VacancyStatus.Active,
            CompanyId = IntermediaryCompanyId,
            Location = new GeoPoint(52.0680, 4.3350),
            RequiredTransport = TransportMode.Bike | TransportMode.PublicTransport,
            WorkTypes = WorkType.Winkel,
            MaxApplications = 10,
            ImageUrl = MockVacancyMedia.ImageUrl(IntermediaryVacancyId),
            VideoUrl = MockVacancyMedia.VideoUrl(IntermediaryVacancyId)
        });
    }

    private static async Task EnsureStatusVarietyVacanciesAsync(JobsyDbContext db, DateOnly today)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == WestlandId))
        {
            return;
        }

        if (!await db.Vacancies.AnyAsync(v => v.Id == DraftVacancyId))
        {
            db.Vacancies.Add(new Vacancy
            {
                Id = DraftVacancyId,
                Title = "Seizoenshulp kas (concept)",
                Description = MockVacancyMedia.BuildRichDescription(
                    "Seizoenshulp kas (concept)",
                    "Concept-vacature voor demo van Draft-status.",
                    "Westland Fresh Logistics",
                    WorkType.Tuinbouw,
                    14.20m,
                    11),
                HourlyWage = 14.20m,
                StartDate = today.AddDays(7),
                EndDate = today.AddMonths(1),
                Status = VacancyStatus.Draft,
                CompanyId = WestlandId,
                Location = new GeoPoint(51.9812, 4.2235),
                RequiredTransport = TransportMode.Bike | TransportMode.Car,
                WorkTypes = WorkType.Tuinbouw,
                MaxApplications = 5,
                ImageUrl = MockVacancyMedia.ImageUrl(DraftVacancyId),
                VideoUrl = MockVacancyMedia.VideoUrl(DraftVacancyId)
            });
        }

        if (!await db.Vacancies.AnyAsync(v => v.Id == PendingVacancyId))
        {
            db.Vacancies.Add(new Vacancy
            {
                Id = PendingVacancyId,
                Title = "Avondploeg orderpicker",
                Description = MockVacancyMedia.BuildRichDescription(
                    "Avondploeg orderpicker",
                    "Wacht op token-goedkeuring (PendingApproval demo).",
                    "Westland Fresh Logistics",
                    WorkType.Logistiek,
                    15.00m,
                    12),
                HourlyWage = 15.00m,
                StartDate = today,
                EndDate = today.AddMonths(2),
                Status = VacancyStatus.PendingApproval,
                CompanyId = WestlandId,
                Location = new GeoPoint(51.9812, 4.2235),
                RequiredTransport = TransportMode.Car,
                WorkTypes = WorkType.Logistiek,
                RequestedHighlight = true,
                RequestedPushBom = true,
                MaxApplications = 5,
                ImageUrl = MockVacancyMedia.ImageUrl(PendingVacancyId),
                VideoUrl = MockVacancyMedia.VideoUrl(PendingVacancyId)
            });
        }

        if (!await db.Vacancies.AnyAsync(v => v.Id == ArchivedVacancyId)
            && await db.Companies.AnyAsync(c => c.Id == CafeId))
        {
            db.Vacancies.Add(new Vacancy
            {
                Id = ArchivedVacancyId,
                Title = "Zomerhulp (afgelopen)",
                Description = MockVacancyMedia.BuildRichDescription(
                    "Zomerhulp (afgelopen)",
                    "Gearchiveerde demo-vacature.",
                    "Boutique Café De Stad",
                    WorkType.Horeca,
                    13.50m,
                    13),
                HourlyWage = 13.50m,
                StartDate = today.AddMonths(-4),
                EndDate = today.AddMonths(-1),
                Status = VacancyStatus.Archived,
                CompanyId = CafeId,
                Location = new GeoPoint(52.0735, 4.3120),
                RequiredTransport = TransportMode.Bike | TransportMode.PublicTransport,
                WorkTypes = WorkType.Horeca,
                MaxApplications = 5,
                ImageUrl = MockVacancyMedia.ImageUrl(ArchivedVacancyId),
                VideoUrl = MockVacancyMedia.VideoUrl(ArchivedVacancyId)
            });
        }
    }

    private static async Task EnrichVacancyFlagsAsync(JobsyDbContext db)
    {
        var orderpicker = await db.Vacancies.FirstOrDefaultAsync(v => v.Id == OrderpickerVacancyId);
        if (orderpicker is not null)
        {
            orderpicker.IsHighlighted = true;
            orderpicker.HighlightedUntil ??= DateTime.UtcNow.AddDays(VacancyProductRules.HighlightDays);
            if (orderpicker.ExtensionCount < 1)
            {
                orderpicker.ExtensionCount = 1;
                orderpicker.EndDate = orderpicker.EndDate.AddDays(14);
            }
        }

        var barista = await db.Vacancies.FirstOrDefaultAsync(v => v.Id == BaristaVacancyId);
        if (barista is not null && barista.ExtensionCount < 1)
        {
            barista.ExtensionCount = 1;
            barista.EndDate = barista.EndDate.AddDays(14);
        }
    }

    private static async Task SeedSpendLedgerAsync(JobsyDbContext db, DateTime now)
    {
        // Outer Sprint-8 platform-log marker already idempotizes the full batch.
        // Do not skip when other Spend rows already exist (upgrade path).

        // Extra purchase so day/week/month tabs all have purchased tokens.
        await AppendTxAsync(db, WestlandId, 20m, TokenTransactionKind.Purchase, TokenSpendReason.None,
            "Stub Mollie pack 20", now.AddDays(-20), vacancyId: null);
        await AppendTxAsync(db, CafeId, 10m, TokenTransactionKind.Purchase, TokenSpendReason.None,
            "Stub Mollie pack 10", now.AddDays(-8), vacancyId: null);
        await AppendTxAsync(db, SupermarketId, 10m, TokenTransactionKind.Purchase, TokenSpendReason.None,
            "Stub Mollie pack 10", now.AddHours(-4), vacancyId: null);

        // Publish / highlight / pushbom / extend across periods.
        await AppendTxAsync(db, WestlandId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Publish,
            "Publish seed", now.AddDays(-22), OrderpickerVacancyId);
        await AppendTxAsync(db, WestlandId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Highlight,
            "Highlight seed", now.AddDays(-21), OrderpickerVacancyId);
        await AppendTxAsync(db, WestlandId, -2m, TokenTransactionKind.Spend, TokenSpendReason.PushBom,
            "PushBom seed", now.AddDays(-12), OrderpickerVacancyId);
        await AppendTxAsync(db, WestlandId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Extend,
            "Extend seed", now.AddDays(-11), OrderpickerVacancyId);

        await AppendTxAsync(db, CafeId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Publish,
            "Publish seed", now.AddDays(-6), BaristaVacancyId);
        await AppendTxAsync(db, CafeId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Extend,
            "Extend seed", now.AddDays(-5), BaristaVacancyId);
        await AppendTxAsync(db, CafeId, -1.5m, TokenTransactionKind.Spend, TokenSpendReason.PushBom,
            "PushBom seed", now.AddDays(-2), BaristaVacancyId);

        await AppendTxAsync(db, SupermarketId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Publish,
            "Publish seed", now.AddHours(-10), RetailVacancyId);
        await AppendTxAsync(db, SupermarketId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Highlight,
            "Highlight seed", now.AddHours(-8), RetailVacancyId);

        if (await db.Companies.AnyAsync(c => c.Id == IntermediaryCompanyId))
        {
            var intermediaryVacancyId = await db.Vacancies
                .Where(v => v.CompanyId == IntermediaryCompanyId && v.Status == VacancyStatus.Active)
                .Select(v => (Guid?)v.Id)
                .FirstOrDefaultAsync();

            await AppendTxAsync(db, IntermediaryCompanyId, -1m, TokenTransactionKind.Spend, TokenSpendReason.Publish,
                "Intermediair publish seed", now.AddDays(-3), intermediaryVacancyId);
            await AppendTxAsync(db, IntermediaryCompanyId, -2m, TokenTransactionKind.Spend, TokenSpendReason.PushBom,
                "Intermediair PushBom seed", now.AddDays(-1), intermediaryVacancyId);
        }
    }

    private static async Task AppendTxAsync(
        JobsyDbContext db,
        Guid companyId,
        decimal amount,
        TokenTransactionKind kind,
        TokenSpendReason reason,
        string note,
        DateTime createdAt,
        Guid? vacancyId)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return;
        }

        var balance = await db.TokenTransactions
            .Where(t => t.CompanyId == companyId)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        // Include not-yet-saved entries for the same company in this run.
        balance += db.TokenTransactions.Local
            .Where(t => t.CompanyId == companyId)
            .Sum(t => t.Amount);

        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = amount,
            Kind = kind,
            Reason = reason,
            OldBalance = balance,
            NewBalance = balance + amount,
            VacancyId = vacancyId,
            Note = note,
            CreatedAt = createdAt
        });
    }

    private static async Task SeedTimeSpreadEngagementAsync(JobsyDbContext db, DateTime now)
    {
        var vacancyIds = await db.Vacancies
            .Where(v => v.Status == VacancyStatus.Active)
            .Select(v => v.Id)
            .ToListAsync();
        if (vacancyIds.Count == 0)
        {
            return;
        }

        // Only add older samples when none exist beyond ~2 days (Sprint0 seeds last few hours).
        var hasOlder = await db.VacancyClicks.AnyAsync(c => c.CreatedAt < now.AddDays(-2));
        if (hasOlder)
        {
            return;
        }

        // Backdate existing likes so week/month tabs are non-zero (VacancyId+UserId is unique).
        var likeIndex = 0;
        foreach (var like in await db.VacancyLikes.ToListAsync())
        {
            like.CreatedAt = now.AddDays(-(3 + likeIndex * 4)).AddHours(-2);
            likeIndex++;
        }

        var denHaagId = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222");
        var farId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333");
        foreach (var (userId, vacancyId, daysAgo) in new[]
                 {
                     (denHaagId, OrderpickerVacancyId, 8),
                     (farId, BaristaVacancyId, 15),
                     (denHaagId, RetailVacancyId, 22)
                 })
        {
            if (!await db.Users.AnyAsync(u => u.Id == userId))
            {
                continue;
            }

            if (await db.VacancyLikes.AnyAsync(l => l.VacancyId == vacancyId && l.UserId == userId))
            {
                continue;
            }

            db.VacancyLikes.Add(new VacancyLike
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyId,
                UserId = userId,
                CreatedAt = now.AddDays(-daysAgo)
            });
        }

        var offsets = new[]
        {
            (Days: -25, Channel: ShareChannel.LinkedIn),
            (Days: -18, Channel: ShareChannel.Email),
            (Days: -10, Channel: ShareChannel.Facebook),
            (Days: -3, Channel: ShareChannel.Signal),
            (Days: -1, Channel: ShareChannel.WhatsApp)
        };

        foreach (var vacancyId in vacancyIds)
        {
            foreach (var (days, channel) in offsets)
            {
                var at = now.AddDays(days).AddHours(-days);
                db.VacancyClicks.Add(new VacancyClick
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancyId,
                    UserId = CandidateId,
                    CreatedAt = at
                });
                db.VacancyClicks.Add(new VacancyClick
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancyId,
                    AnonymousKey = $"anon-sprint8-{vacancyId:N}-{Math.Abs(days)}",
                    CreatedAt = at.AddMinutes(30)
                });
                db.VacancyShares.Add(new VacancyShare
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancyId,
                    UserId = CandidateId,
                    Channel = channel,
                    CreatedAt = at.AddHours(2)
                });
            }
        }
    }

    private static async Task EnrichApplicationsAsync(JobsyDbContext db, DateTime now)
    {
        var candidate = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == CandidateId);
        if (candidate is null)
        {
            return;
        }

        async Task<bool> VacancyExistsAsync(Guid id)
            => await db.Vacancies.AnyAsync(v => v.Id == id);

        // Ensure accepted + rejected samples for the demo candidate across periods.
        if (await VacancyExistsAsync(BaristaVacancyId)
            && !await db.Applications.AnyAsync(a =>
                a.CandidateUserId == candidate.Id && a.Status == ApplicationStatus.Accepted))
        {
            db.Applications.Add(new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = BaristaVacancyId,
                CandidateUserId = candidate.Id,
                CandidateName = candidate.FullName,
                CandidateEmail = candidate.Email,
                CandidateCity = "Den Haag",
                PreferredTransport = "OV",
                EstimatedTravelMinutes = 18,
                DistanceKm = 4.1,
                Status = ApplicationStatus.Accepted,
                PreferencesSummary = candidate.PreferencesJson,
                CreatedAt = now.AddDays(-4),
                    EmailVerifiedAt = DateTime.UtcNow,
                RespondedAt = now.AddDays(-3)
            });
        }

        if (await VacancyExistsAsync(RetailVacancyId)
            && !await db.Applications.AnyAsync(a =>
                a.CandidateUserId == candidate.Id && a.Status == ApplicationStatus.Rejected))
        {
            db.Applications.Add(new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = RetailVacancyId,
                CandidateUserId = candidate.Id,
                CandidateName = candidate.FullName,
                CandidateEmail = candidate.Email,
                CandidateCity = "Den Haag",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 22,
                DistanceKm = 5.5,
                Status = ApplicationStatus.Rejected,
                PreferencesSummary = candidate.PreferencesJson,
                CreatedAt = now.AddDays(-9),
                    EmailVerifiedAt = DateTime.UtcNow,
                RespondedAt = now.AddDays(-8)
            });
        }

        // Guest applications spread over month for employer/admin KPI's.
        if (!await db.Applications.AnyAsync(a => a.CandidateEmail == "lisa.sprint8@example.com"))
        {
            if (await VacancyExistsAsync(OrderpickerVacancyId))
            {
                db.Applications.Add(new Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = OrderpickerVacancyId,
                    CandidateName = "Lisa Vermeer",
                    CandidateEmail = "lisa.sprint8@example.com",
                    CandidateCity = "Naaldwijk",
                    PreferredTransport = "Auto",
                    EstimatedTravelMinutes = 16,
                    DistanceKm = 7.2,
                    Status = ApplicationStatus.Pending,
                    CreatedAt = now.AddDays(-20),
                    EmailVerifiedAt = DateTime.UtcNow
                });
            }

            if (await VacancyExistsAsync(BaristaVacancyId))
            {
                db.Applications.Add(new Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = BaristaVacancyId,
                    CandidateName = "Tom Bakker",
                    CandidateEmail = "tom.sprint8@example.com",
                    CandidateCity = "Scheveningen",
                    PreferredTransport = "Fiets",
                    EstimatedTravelMinutes = 25,
                    DistanceKm = 6.0,
                    Status = ApplicationStatus.Accepted,
                    CreatedAt = now.AddDays(-7),
                    EmailVerifiedAt = DateTime.UtcNow,
                    RespondedAt = now.AddDays(-6)
                });
            }

            if (await VacancyExistsAsync(RetailVacancyId))
            {
                db.Applications.Add(new Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = RetailVacancyId,
                    CandidateName = "Noor de Wit",
                    CandidateEmail = "noor.sprint8@example.com",
                    CandidateCity = "Voorburg",
                    PreferredTransport = "OV",
                    EstimatedTravelMinutes = 35,
                    DistanceKm = 9.4,
                    Status = ApplicationStatus.Pending,
                    CreatedAt = now.AddHours(-6),
                    EmailVerifiedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static async Task EnrichPlatformLogsAsync(JobsyDbContext db, DateTime now)
    {
        if (await db.PlatformLogs.CountAsync() >= 8)
        {
            return;
        }

        db.PlatformLogs.AddRange(
            new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Auth",
                Message = "Demo login: kandidaat@jobsy.local",
                CreatedAt = now.AddDays(-14)
            },
            new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Push",
                Message = "PushBom delivered to 3 OpenForWork candidates",
                CreatedAt = now.AddDays(-12)
            },
            new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Warning,
                Category = "Wages",
                Message = "Semi-annual WML reminder window opening soon",
                CreatedAt = now.AddDays(-5)
            },
            new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Warning,
                Category = "Integration",
                Message = "KVK stub latency elevated (demo)",
                CreatedAt = now.AddDays(-2)
            },
            new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Error,
                Category = "Mail",
                Message = "SMTP stub: failed to send invite (demo error)",
                CreatedAt = now.AddDays(-1)
            },
            new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Error,
                Category = "Integration",
                Message = "OpenAI moderation stub timeout",
                CreatedAt = now.AddHours(-3)
            },
            new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Tokens",
                Message = "Checkout stub completed pack 10 for Supermarkt De Fred",
                CreatedAt = now.AddHours(-4)
            });
    }

    private static async Task SeedAllocationAsync(JobsyDbContext db, DateTime now)
    {
        // Idempotency is the Sprint-8 platform-log marker on the outer seed method.

        if (!await db.Companies.AnyAsync(c => c.Id == SupermarketId)
            || !await db.Companies.AnyAsync(c => c.Id == CafeId))
        {
            return;
        }

        // Org → branch allocation for regional token UI demos.
        await AppendTxAsync(db, SupermarketId, -3m, TokenTransactionKind.Allocation, TokenSpendReason.None,
            "Allocatie naar Café De Stad", now.AddDays(-4), vacancyId: null);
        await AppendTxAsync(db, CafeId, 3m, TokenTransactionKind.Allocation, TokenSpendReason.None,
            "Ontvangen allocatie van De Fred", now.AddDays(-4).AddMinutes(1), vacancyId: null);
    }
}
