using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Idempotent acceptatie sollicitaties mockup rows for CLI / primary test candidates.
/// </summary>
public static class TestAccountsSollicitatiesDemoSeeder
{
    public const string AcceptatiePrimaryCandidateEmail = "lobsy-test@mail.grokbot.com";

    private static readonly DateTime DemoAnchorUtc = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private sealed record CompanySeed(Guid Id, string Name, WorkType WorkType, GeoPoint Location, string City, int KvkOffset);

    private sealed record VacancySeed(Guid Id, Guid CompanyId, string Title, WorkType WorkType, GeoPoint Location);

    private enum DemoApplicationScenario
    {
        Sent,
        Seen,
        PickSlots,
        Scheduled,
        UnreadMessages,
        Done
    }

    public static async Task<bool> TryEnsureForAcceptatieAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        TestAccountGuardResult guard,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!guard.Allowed)
        {
            return false;
        }

        var candidate = await ResolveAcceptatieDemoCandidateAsync(db, configuration, logger, cancellationToken);
        if (candidate is null)
        {
            logger.LogInformation(
                "Acceptatie sollicitaties demo skipped: no user for {Primary} or catalog candidate (create the account via acc login or run test-accounts seed on lobsy-acc-api).",
                AcceptatiePrimaryCandidateEmail);
            return false;
        }

        var branchManager = await FindBranchManagerAsync(db, configuration, cancellationToken);
        if (branchManager is null)
        {
            logger.LogInformation(
                "Acceptatie sollicitaties demo skipped: no branch/regional manager in DB (run test-accounts seed once on lobsy-acc-api).");
            return false;
        }

        var beforeApps = await db.Applications.CountAsync(
            a => a.CandidateUserId == candidate.Id && a.EmailVerifiedAt != null,
            cancellationToken);

        try
        {
            await JobsyDbSeeder.EnsureAfterMatchDemoApplicationsAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Acceptatie after-match demo ensure failed during sollicitaties reset; continuing.");
        }

        await RemoveStaleDemoApplicationsAsync(db, candidate.Id, logger, cancellationToken);
        await EnsureForCandidateAsync(db, candidate.Id, branchManager.Id, logger, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var afterApps = await db.Applications.CountAsync(
            a => a.CandidateUserId == candidate.Id && a.EmailVerifiedAt != null,
            cancellationToken);
        logger.LogInformation(
            "Acceptatie sollicitaties demo ensured for {Email} userId={UserId} verifiedApplications={After} (was {Before}); SSR loads the same rows via ApiBaseUrl → GET /me/applications.",
            candidate.Email,
            candidate.Id,
            afterApps,
            beforeApps);
        return true;
    }

    public static Task<User?> FindAcceptatieDemoCandidateAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
        => ResolveAcceptatieDemoCandidateAsync(db, configuration, logger, cancellationToken);

    /// <summary>Health / diagnostics: verified applications and distinct apps with ≥1 message.</summary>
    public static async Task<(string Outcome, int Apps, int Threads)> BuildDemoHealthCountsAsync(
        JobsyDbContext db,
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        var apps = await db.Applications.CountAsync(
            a => a.CandidateUserId == candidateUserId && a.EmailVerifiedAt != null,
            cancellationToken);
        var threads = await db.ApplicationMessages.AsNoTracking()
            .Where(m => db.Applications.Any(a =>
                a.Id == m.ApplicationId
                && a.CandidateUserId == candidateUserId
                && a.EmailVerifiedAt != null))
            .Select(m => m.ApplicationId)
            .Distinct()
            .CountAsync(cancellationToken);
        return ($"applied:apps={apps},threads={threads}", apps, threads);
    }

    public static async Task<string> BuildDemoHealthOutcomeAsync(
        JobsyDbContext db,
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        var counts = await BuildDemoHealthCountsAsync(db, candidateUserId, cancellationToken);
        return counts.Outcome;
    }

    private static readonly Guid[] DemoApplicationIds =
    [
        TestAccountsIds.SollicDemoAppSent,
        TestAccountsIds.SollicDemoAppSeen,
        TestAccountsIds.SollicDemoAppPick,
        TestAccountsIds.SollicDemoAppScheduled,
        TestAccountsIds.SollicDemoAppMessages,
        TestAccountsIds.SollicDemoAppDone,
    ];

    private static async Task RemoveStaleDemoApplicationsAsync(
        JobsyDbContext db,
        Guid candidateUserId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var keep = DemoApplicationIds.ToHashSet();
        var staleIds = await db.Applications.AsNoTracking()
            .Where(a => a.CandidateUserId == candidateUserId && !keep.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
        if (staleIds.Count == 0)
        {
            return;
        }

        await AcceptatieApplicationDemoGraphRemover.RemoveApplicationsAsync(db, staleIds, cancellationToken);
        logger.LogInformation(
            "Acceptatie sollicitaties demo reset: removed {Count} stale applications for candidate {CandidateId}.",
            staleIds.Count,
            candidateUserId);
    }

    public static async Task EnsureForCandidateAsync(
        JobsyDbContext db,
        Guid candidateId,
        Guid branchManagerId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var candidate = await db.Users.FirstOrDefaultAsync(u => u.Id == candidateId, cancellationToken);
        var allowlistedPrimary = IsAllowlistedPrimaryCandidate(candidate);
        if (candidate is null || (!candidate.IsTestAccount && !allowlistedPrimary))
        {
            logger.LogDebug(
                "Sollicitaties demo skipped: candidate {CandidateId} missing or not a test account.",
                candidateId);
            return;
        }

        if (allowlistedPrimary && !candidate.IsTestAccount)
        {
            candidate.IsTestAccount = true;
            logger.LogInformation(
                "Acceptatie sollicitaties demo: marked allowlisted candidate {Email} as test account.",
                candidate.Email);
        }

        await RemoveStaleDemoApplicationsAsync(db, candidateId, logger, cancellationToken);

        try
        {
            await new VacancyCategoryService(db).EnsureDefaultsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "VacancyCategory EnsureDefaults failed; resolving regulier category for sollicitaties demo.");
        }

        var regulierCategoryId = await ResolveRegulierCategoryIdAsync(db, cancellationToken);
        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var companies = BuildCompanies();
        foreach (var company in companies)
        {
            await UpsertCompanyAsync(db, company, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var vacancies = BuildVacancies(companies);
        foreach (var vacancy in vacancies)
        {
            await UpsertVacancyAsync(db, vacancy, regulierCategoryId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        await EnsureVacancyApplicationAsync(
            db,
            candidate,
            branchManagerId,
            TestAccountsIds.SollicDemoAppSent,
            TestAccountsIds.SollicDemoVacancySent,
            DemoApplicationScenario.Sent,
            cancellationToken);

        await EnsureVacancyApplicationAsync(
            db,
            candidate,
            branchManagerId,
            TestAccountsIds.SollicDemoAppSeen,
            TestAccountsIds.SollicDemoVacancySeen,
            DemoApplicationScenario.Seen,
            cancellationToken);

        await EnsureVacancyApplicationAsync(
            db,
            candidate,
            branchManagerId,
            TestAccountsIds.SollicDemoAppPick,
            TestAccountsIds.SollicDemoVacancyPick,
            DemoApplicationScenario.PickSlots,
            cancellationToken);

        await EnsureVacancyApplicationAsync(
            db,
            candidate,
            branchManagerId,
            TestAccountsIds.SollicDemoAppScheduled,
            TestAccountsIds.SollicDemoVacancyScheduled,
            DemoApplicationScenario.Scheduled,
            cancellationToken);

        await EnsureVacancyApplicationAsync(
            db,
            candidate,
            branchManagerId,
            TestAccountsIds.SollicDemoAppMessages,
            TestAccountsIds.SollicDemoVacancyMessages,
            DemoApplicationScenario.UnreadMessages,
            cancellationToken);

        await EnsureVacancyApplicationAsync(
            db,
            candidate,
            branchManagerId,
            TestAccountsIds.SollicDemoAppDone,
            TestAccountsIds.SollicDemoVacancyDone,
            DemoApplicationScenario.Done,
            cancellationToken);

        await EnsureExternalAppliedAsync(db, candidate, cancellationToken);
        await EnsureInitiativesAsync(db, candidate, companies, cancellationToken);

        logger.LogDebug("Sollicitaties demo rows ensured for candidate {CandidateId}.", candidateId);
    }

    /// <summary>
    /// Acceptatie-only allowlist: primary Grokbot login, else catalog CLI candidate. Marks <see cref="User.IsTestAccount"/>.
    /// </summary>
    private static async Task<User?> ResolveAcceptatieDemoCandidateAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var primary = AcceptatiePrimaryCandidateEmail.ToLowerInvariant();
        var candidate = await db.Users.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == primary,
            cancellationToken);
        if (candidate is not null)
        {
            await EnsureDemoCandidateTestFlagAsync(candidate, logger);
            return candidate;
        }

        var entry = TestAccountCatalog.FindByKey("Candidate");
        if (entry is null)
        {
            return null;
        }

        var domain = configuration["TestAccounts:EmailDomain"] ?? "lobsy.nl";
        var catalogEmail = TestAccountCatalog.BuildEmail(entry.EmailSlug, domain).ToLowerInvariant();
        candidate = await db.Users.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == catalogEmail,
            cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        await EnsureDemoCandidateTestFlagAsync(candidate, logger);
        return candidate;
    }

    private static Task EnsureDemoCandidateTestFlagAsync(
        User candidate,
        ILogger logger)
    {
        if (candidate.IsTestAccount)
        {
            return Task.CompletedTask;
        }

        candidate.IsTestAccount = true;
        logger.LogInformation(
            "Acceptatie sollicitaties demo: marked allowlisted candidate {Email} as test account.",
            candidate.Email);
        return Task.CompletedTask;
    }

    private static async Task<User?> FindBranchManagerAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var entry = TestAccountCatalog.FindByKey("BranchManager");
        if (entry is null)
        {
            return null;
        }

        var domain = configuration["TestAccounts:EmailDomain"] ?? "lobsy.nl";
        var email = TestAccountCatalog.BuildEmail(entry.EmailSlug, domain).ToLowerInvariant();
        var manager = await db.Users.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == email,
            cancellationToken);
        if (manager is not null && !manager.IsTestAccount)
        {
            manager.IsTestAccount = true;
            return manager;
        }

        if (manager is not null)
        {
            return manager;
        }

        return await db.Users
            .Where(u => u.IsActive
                        && (u.Role == UserRole.BranchManager
                            || u.Role == UserRole.RegionalManager
                            || u.Role == UserRole.EnterpriseManager))
            .OrderByDescending(u => u.IsTestAccount)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static bool IsAcceptatieDemoCandidateEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && string.Equals(email, AcceptatiePrimaryCandidateEmail, StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowlistedPrimaryCandidate(User? candidate)
        => IsAcceptatieDemoCandidateEmail(candidate?.Email);

    private static CompanySeed[] BuildCompanies() =>
    [
        new(
            TestAccountsIds.SollicDemoCompanyHoreca,
            "Testbedrijf Horeca (test)",
            WorkType.Horeca,
            new GeoPoint(52.0780, 4.3100),
            "Den Haag",
            0),
        new(
            TestAccountsIds.SollicDemoCompanyRetail,
            "Testbedrijf Retail (test)",
            WorkType.Winkel,
            new GeoPoint(52.0790, 4.3120),
            "Den Haag",
            1),
        new(
            TestAccountsIds.SollicDemoCompanyGroen,
            "Testbedrijf Groen (test)",
            WorkType.Tuinbouw,
            new GeoPoint(52.0550, 4.3950),
            "Leidschendam",
            2),
        new(
            TestAccountsIds.SollicDemoCompanyBlauw,
            "Testbedrijf Blauw (test)",
            WorkType.Kantoor,
            new GeoPoint(52.0710, 4.2980),
            "Den Haag",
            3),
        new(
            TestAccountsIds.SollicDemoCompanyGrijs,
            "Testbedrijf Grijs (test)",
            WorkType.Logistiek,
            new GeoPoint(52.0610, 4.3280),
            "Rijswijk",
            4)
    ];

    private static VacancySeed[] BuildVacancies(CompanySeed[] companies)
    {
        CompanySeed ById(Guid id) => companies.First(c => c.Id == id);
        return
        [
            new(
                TestAccountsIds.SollicDemoVacancySent,
                TestAccountsIds.SollicDemoCompanyHoreca,
                "Medewerker bediening (test)",
                ById(TestAccountsIds.SollicDemoCompanyHoreca).WorkType,
                ById(TestAccountsIds.SollicDemoCompanyHoreca).Location),
            new(
                TestAccountsIds.SollicDemoVacancySeen,
                TestAccountsIds.SollicDemoCompanyRetail,
                "Kassamedewerker weekend (test)",
                ById(TestAccountsIds.SollicDemoCompanyRetail).WorkType,
                ById(TestAccountsIds.SollicDemoCompanyRetail).Location),
            new(
                TestAccountsIds.SollicDemoVacancyPick,
                TestAccountsIds.SollicDemoCompanyGroen,
                "Hovenier helper (test)",
                ById(TestAccountsIds.SollicDemoCompanyGroen).WorkType,
                ById(TestAccountsIds.SollicDemoCompanyGroen).Location),
            new(
                TestAccountsIds.SollicDemoVacancyScheduled,
                TestAccountsIds.SollicDemoCompanyBlauw,
                "Administratief medewerker (test)",
                ById(TestAccountsIds.SollicDemoCompanyBlauw).WorkType,
                ById(TestAccountsIds.SollicDemoCompanyBlauw).Location),
            new(
                TestAccountsIds.SollicDemoVacancyMessages,
                TestAccountsIds.SollicDemoCompanyGrijs,
                "Magazijnmedewerker ochtend (test)",
                ById(TestAccountsIds.SollicDemoCompanyGrijs).WorkType,
                ById(TestAccountsIds.SollicDemoCompanyGrijs).Location),
            new(
                TestAccountsIds.SollicDemoVacancyDone,
                TestAccountsIds.SollicDemoCompanyHoreca,
                "Barista parttime (test)",
                WorkType.Horeca,
                ById(TestAccountsIds.SollicDemoCompanyHoreca).Location)
        ];
    }

    private static int CreatedDaysAgo(DemoApplicationScenario scenario) => scenario switch
    {
        DemoApplicationScenario.Sent => 1,
        DemoApplicationScenario.Seen => 2,
        DemoApplicationScenario.PickSlots => 3,
        DemoApplicationScenario.Scheduled => 4,
        DemoApplicationScenario.UnreadMessages => 5,
        DemoApplicationScenario.Done => 12,
        _ => 1
    };

    private static string TruncateEstablishmentId(string value)
        => value.Length <= 40 ? value : value[..40];

    private static string DemoKvk(int offset)
    {
        var baseNumber = int.Parse(TestAccountsIds.SollicDemoKvkBase, NumberStyles.None, CultureInfo.InvariantCulture);
        return (baseNumber + 10 + offset).ToString("D8", CultureInfo.InvariantCulture);
    }

    private static async Task<Guid> ResolveRegulierCategoryIdAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken)
    {
        var byId = await db.VacancyCategories.AsNoTracking()
            .Where(c => c.Id == VacancyCategoryDefaults.RegulierId)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (byId != Guid.Empty)
        {
            return byId;
        }

        var bySlug = await db.VacancyCategories.AsNoTracking()
            .Where(c => c.Slug == "regulier")
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (bySlug != Guid.Empty)
        {
            return bySlug;
        }

        var seed = VacancyCategoryDefaults.All.FirstOrDefault(c => c.Id == VacancyCategoryDefaults.RegulierId)
                   ?? VacancyCategoryDefaults.All.First(c => c.Slug == "regulier");
        db.VacancyCategories.Add(new VacancyCategory
        {
            Id = seed.Id,
            Slug = seed.Slug,
            Name = seed.Name,
            ColorHex = seed.ColorHex,
            PublishCostTokens = seed.PublishCostTokens,
            HighlightAvailable = seed.HighlightAvailable,
            HighlightCostTokens = seed.HighlightCostTokens,
            PushBomAvailable = seed.PushBomAvailable,
            PushBomCostTokens = seed.PushBomCostTokens,
            IsAlwaysFree = seed.IsAlwaysFree,
            ExtraFieldsJson = VacancyCategoryExtraFields.SerializeKeys(seed.ExtraFields),
            PlacementKind = seed.PlacementKind,
            SortOrder = seed.SortOrder,
            IsActive = true,
            ShowInMapFilter = true,
            ShowInLegend = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        return seed.Id;
    }

    private static async Task<string> ResolveUniqueKvkEstablishmentIdAsync(
        JobsyDbContext db,
        Guid companyId,
        string kvk,
        CancellationToken cancellationToken)
    {
        var candidates = new[] { $"{kvk}_0001", TruncateEstablishmentId($"demo-{companyId:N}") };

        foreach (var establishmentId in candidates.Distinct(StringComparer.Ordinal))
        {
            var taken = await db.Companies.AsNoTracking().AnyAsync(
                c => c.KvkEstablishmentId == establishmentId && c.Id != companyId,
                cancellationToken);
            if (!taken)
            {
                return establishmentId;
            }
        }

        return TruncateEstablishmentId($"demo-{companyId:N}");
    }

    private static async Task UpsertCompanyAsync(
        JobsyDbContext db,
        CompanySeed seed,
        CancellationToken cancellationToken)
    {
        var kvk = DemoKvk(seed.KvkOffset);
        var establishmentId = await ResolveUniqueKvkEstablishmentIdAsync(db, seed.Id, kvk, cancellationToken);
        var existing = await db.Companies.FirstOrDefaultAsync(c => c.Id == seed.Id, cancellationToken);
        if (existing is not null)
        {
            existing.Name = seed.Name;
            existing.IsTestData = true;
            existing.KvkNumber = kvk;
            existing.KvkEstablishmentId = establishmentId;
            existing.Type = CompanyType.Employer;
            existing.ParentCompanyId = null;
            existing.Location = seed.Location;
            existing.Address = $"Teststraat 1, {seed.City}";
            existing.VerificationStatus = CompanyVerificationStatus.Verified;
            existing.VerificationMethod = CompanyVerificationMethod.AdminCreated;
            existing.VerifiedAtUtc ??= DemoAnchorUtc;
            existing.KvkVerificationStatus = KvkVerificationStatus.Verified;
            existing.HasReceivedWelcomeToken = true;
            return;
        }

        db.Companies.Add(new Company
        {
            Id = seed.Id,
            Name = seed.Name,
            KvkNumber = kvk,
            KvkEstablishmentId = establishmentId,
            Type = CompanyType.Employer,
            Location = seed.Location,
            Address = $"Teststraat 1, {seed.City}",
            IsTestData = true,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DemoAnchorUtc,
            VerificationUpdatedAtUtc = DemoAnchorUtc,
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            KvkVerifiedAtUtc = DemoAnchorUtc,
            HasReceivedWelcomeToken = true,
            WelcomeTokenLedgerCredited = false
        });
    }

    private static async Task UpsertVacancyAsync(
        JobsyDbContext db,
        VacancySeed seed,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!await EntityExistsAsync(db.Companies, seed.CompanyId, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Sollicitaties demo company {seed.CompanyId:D} missing before vacancy {seed.Id:D}.");
        }

        if (!await EntityExistsAsync(db.VacancyCategories, categoryId, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Sollicitaties demo vacancy category {categoryId:D} missing before vacancy {seed.Id:D}.");
        }

        var today = DateOnly.FromDateTime(DemoAnchorUtc);
        var existing = await db.Vacancies.FirstOrDefaultAsync(v => v.Id == seed.Id, cancellationToken);
        existing ??= await db.Vacancies.FirstOrDefaultAsync(
            v => v.IsTestData && v.CompanyId == seed.CompanyId && v.Title == seed.Title,
            cancellationToken);
        if (existing is not null)
        {
            existing.Title = seed.Title;
            existing.CompanyId = seed.CompanyId;
            existing.Status = VacancyStatus.Active;
            existing.IsTestData = true;
            existing.Location = seed.Location;
            existing.WorkTypes = seed.WorkType;
            existing.CategoryId = categoryId;
            existing.PublishedAtUtc ??= DemoAnchorUtc;
            return;
        }

        db.Vacancies.Add(new Vacancy
        {
            Id = seed.Id,
            Title = seed.Title,
            Description = "Acceptatie sollicitaties mockup. Alleen zichtbaar voor testaccounts.",
            HourlyWage = 14.50m,
            StartDate = today,
            EndDate = today.AddMonths(3),
            Status = VacancyStatus.Active,
            CompanyId = seed.CompanyId,
            Location = seed.Location,
            RequiredTransport = TransportMode.Bike | TransportMode.PublicTransport,
            WorkTypes = seed.WorkType,
            Kind = VacancyKind.Regular,
            CategoryId = categoryId,
            IsTestData = true,
            ContentModerationPassed = true,
            CreatedAtUtc = DemoAnchorUtc.AddDays(-14),
            PublishedAtUtc = DemoAnchorUtc.AddDays(-7)
        });
    }

    private static async Task EnsureVacancyApplicationAsync(
        JobsyDbContext db,
        User candidate,
        Guid branchManagerId,
        Guid applicationId,
        Guid vacancyId,
        DemoApplicationScenario scenario,
        CancellationToken cancellationToken)
    {
        if (!await EntityExistsAsync(db.Vacancies, vacancyId, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Sollicitaties demo vacancy {vacancyId:D} missing before application {applicationId:D}.");
        }

        if (!await EntityExistsAsync(db.Users, candidate.Id, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Sollicitaties demo candidate user {candidate.Id:D} missing before application {applicationId:D}.");
        }

        if (!await EntityExistsAsync(db.Users, branchManagerId, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Sollicitaties demo branch manager {branchManagerId:D} missing before application {applicationId:D}.");
        }

        var createdAt = DemoAnchorUtc.AddDays(-CreatedDaysAgo(scenario));

        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
        if (app is null)
        {
            app = new Application
            {
                Id = applicationId,
                VacancyId = vacancyId,
                CandidateUserId = candidate.Id,
                CandidateName = candidate.FullName,
                CandidateEmail = candidate.Email,
                CandidateCity = "Den Haag",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 14,
                DistanceKm = 3.4,
                PreferencesSummary = candidate.PreferencesJson,
                ConsentAcceptedAt = DemoAnchorUtc,
                ConsentVersion = PrivacyConstants.CurrentConsentVersion,
                WorkPermitConfirmed = true,
                EmailVerifiedAt = createdAt,
                CreatedAt = createdAt
            };
            db.Applications.Add(app);
            db.ApplicationStatusHistories.Add(
                ApplicationStatusTransitions.CreateCreated(
                    app,
                    ApplicationStatusActorKind.Candidate,
                    candidate.Id,
                    createdAt));
        }
        else
        {
            app.VacancyId = vacancyId;
            app.CandidateUserId = candidate.Id;
            app.CandidateName = candidate.FullName;
            app.CandidateEmail = candidate.Email;
            app.EmailVerifiedAt ??= createdAt;
        }

        switch (scenario)
        {
            case DemoApplicationScenario.Sent:
                app.Status = ApplicationStatus.Pending;
                app.RespondedAt = null;
                await RemoveEmployerViewedAsync(db, applicationId, cancellationToken);
                await RemoveInterviewAsync(db, applicationId, cancellationToken);
                await RemoveMessagesAsync(db, applicationId, cancellationToken);
                await RemoveOutcomeAsync(db, applicationId, cancellationToken);
                break;

            case DemoApplicationScenario.Seen:
                app.Status = ApplicationStatus.Pending;
                app.RespondedAt = null;
                await EnsureEmployerViewedAsync(db, applicationId, branchManagerId, createdAt.AddHours(6), cancellationToken);
                await RemoveInterviewAsync(db, applicationId, cancellationToken);
                await RemoveMessagesAsync(db, applicationId, cancellationToken);
                await RemoveOutcomeAsync(db, applicationId, cancellationToken);
                break;

            case DemoApplicationScenario.PickSlots:
            case DemoApplicationScenario.Scheduled:
            case DemoApplicationScenario.UnreadMessages:
                await EnsureAcceptedAsync(
                    db,
                    app,
                    branchManagerId,
                    candidate.Id,
                    createdAt.AddDays(1),
                    cancellationToken);
                await EnsureEmployerViewedAsync(
                    db,
                    applicationId,
                    branchManagerId,
                    createdAt.AddHours(8),
                    cancellationToken);
                await RemoveOutcomeAsync(db, applicationId, cancellationToken);
                break;

            case DemoApplicationScenario.Done:
                if (app.Status is ApplicationStatus.Pending)
                {
                    await EnsureAcceptedAsync(
                        db,
                        app,
                        branchManagerId,
                        candidate.Id,
                        createdAt.AddDays(-3),
                        cancellationToken);
                }

                app.Status = ApplicationStatus.Rejected;
                app.RespondedAt = createdAt.AddDays(2);
                await EnsureStatusHistoryAsync(
                    db,
                    applicationId,
                    ApplicationStatus.Accepted,
                    ApplicationStatus.Rejected,
                    branchManagerId,
                    app.RespondedAt.Value,
                    cancellationToken);
                await RemoveMessagesAsync(db, applicationId, cancellationToken);
                break;
        }

        switch (scenario)
        {
            case DemoApplicationScenario.PickSlots:
                await EnsureInterviewAsync(
                    db,
                    applicationId,
                    branchManagerId,
                    InterviewStatus.Proposing,
                    proposeSlots: true,
                    chooseFirstSlot: false,
                    cancellationToken);
                await RemoveMessagesAsync(db, applicationId, cancellationToken);
                break;

            case DemoApplicationScenario.Scheduled:
                await EnsureInterviewAsync(
                    db,
                    applicationId,
                    branchManagerId,
                    InterviewStatus.Scheduled,
                    proposeSlots: true,
                    chooseFirstSlot: true,
                    cancellationToken);
                await EnsureDemoConversationAsync(
                    db,
                    applicationId,
                    branchManagerId,
                    candidate.Id,
                    "Top, dinsdag 27 oktober past goed voor ons team.",
                    "Tot dinsdag!",
                    cancellationToken);
                break;

            case DemoApplicationScenario.UnreadMessages:
                await EnsureInterviewAsync(
                    db,
                    applicationId,
                    branchManagerId,
                    InterviewStatus.Proposing,
                    proposeSlots: false,
                    chooseFirstSlot: false,
                    cancellationToken);
                await ReplaceMessagesAsync(
                    db,
                    applicationId,
                    branchManagerId,
                    "Hoi! Leuk dat je solliciteert — heb je donderdag tijd voor een korte kennismaking?",
                    readAtUtc: null,
                    cancellationToken);
                break;

            case DemoApplicationScenario.Done:
                await EnsureInterviewAsync(
                    db,
                    applicationId,
                    branchManagerId,
                    InterviewStatus.Done,
                    proposeSlots: false,
                    chooseFirstSlot: false,
                    cancellationToken);
                await EnsureOutcomeAsync(db, applicationId, branchManagerId, cancellationToken);
                break;
        }
    }

    private static async Task EnsureAcceptedAsync(
        JobsyDbContext db,
        Application app,
        Guid branchManagerId,
        Guid candidateId,
        DateTime acceptedAtUtc,
        CancellationToken cancellationToken)
    {
        if (app.Status != ApplicationStatus.Accepted)
        {
            var from = app.Status;
            app.Status = ApplicationStatus.Accepted;
            app.RespondedAt = acceptedAtUtc;
            await EnsureStatusHistoryAsync(
                db,
                app.Id,
                from,
                ApplicationStatus.Accepted,
                branchManagerId,
                acceptedAtUtc,
                cancellationToken);
        }
        else
        {
            app.RespondedAt ??= acceptedAtUtc;
        }

        _ = candidateId;
    }

    private static async Task EnsureEmployerViewedAsync(
        JobsyDbContext db,
        Guid applicationId,
        Guid branchManagerId,
        DateTime viewedAtUtc,
        CancellationToken cancellationToken)
    {
        var exists = await db.ApplicationStatusHistories.AnyAsync(
            h => h.ApplicationId == applicationId
                 && h.Kind == ApplicationStatusEventKind.EmployerViewed,
            cancellationToken);
        if (exists)
        {
            return;
        }

        db.ApplicationStatusHistories.Add(
            ApplicationStatusTransitions.NewEmployerViewed(applicationId, branchManagerId, viewedAtUtc));
    }

    private static async Task RemoveEmployerViewedAsync(
        JobsyDbContext db,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var rows = await db.ApplicationStatusHistories
            .Where(h => h.ApplicationId == applicationId
                        && h.Kind == ApplicationStatusEventKind.EmployerViewed)
            .ToListAsync(cancellationToken);
        db.ApplicationStatusHistories.RemoveRange(rows);
    }

    private static async Task EnsureStatusHistoryAsync(
        JobsyDbContext db,
        Guid applicationId,
        ApplicationStatus from,
        ApplicationStatus to,
        Guid actorUserId,
        DateTime atUtc,
        CancellationToken cancellationToken)
    {
        var exists = await db.ApplicationStatusHistories.AnyAsync(
            h => h.ApplicationId == applicationId
                 && h.Kind == ApplicationStatusEventKind.StatusChanged
                 && h.FromStatus == from
                 && h.ToStatus == to,
            cancellationToken);
        if (exists)
        {
            return;
        }

        db.ApplicationStatusHistories.Add(
            ApplicationStatusTransitions.NewStatusChangedRow(
                applicationId,
                from,
                to,
                ApplicationStatusActorKind.Employer,
                actorUserId,
                atUtc));
    }

    private static async Task RemoveInterviewAsync(
        JobsyDbContext db,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var interview = await db.Interviews.FirstOrDefaultAsync(i => i.ApplicationId == applicationId, cancellationToken);
        if (interview is null)
        {
            return;
        }

        interview.ChosenSlotId = null;

        var slots = await db.InterviewSlots
            .Where(s => s.InterviewId == interview.Id)
            .ToListAsync(cancellationToken);
        db.InterviewSlots.RemoveRange(slots);

        var changes = await db.InterviewChanges
            .Where(c => c.InterviewId == interview.Id)
            .ToListAsync(cancellationToken);
        db.InterviewChanges.RemoveRange(changes);

        db.Interviews.Remove(interview);
    }

    private static async Task EnsureInterviewAsync(
        JobsyDbContext db,
        Guid applicationId,
        Guid branchManagerId,
        InterviewStatus status,
        bool proposeSlots,
        bool chooseFirstSlot,
        CancellationToken cancellationToken)
    {
        _ = branchManagerId;
        var interview = await db.Interviews.FirstOrDefaultAsync(i => i.ApplicationId == applicationId, cancellationToken);
        if (interview is null)
        {
            interview = new Interview
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                CalendarUid = $"test-sollic-demo-{applicationId:N}@lobsy.nl",
                CreatedAtUtc = DemoAnchorUtc,
                UpdatedAtUtc = DemoAnchorUtc,
                Format = InterviewFormat.OnSite,
                LocationOrLink = "Teststraat 1, Den Haag",
                WithWhom = "Test filiaalmanager",
                Note = "Mockup-gesprek voor acceptatie."
            };
            db.Interviews.Add(interview);
        }

        interview.Status = status;
        interview.Format = InterviewFormat.OnSite;
        interview.LocationOrLink = "Teststraat 1, Den Haag";
        interview.UpdatedAtUtc = DateTime.UtcNow;
        interview.ChosenSlotId = null;

        var existingSlots = await db.InterviewSlots
            .Where(s => s.InterviewId == interview.Id)
            .ToListAsync(cancellationToken);
        db.InterviewSlots.RemoveRange(existingSlots);

        if (!proposeSlots)
        {
            return;
        }

        var slotStarts = new[]
        {
            DemoAnchorUtc.AddDays(5).AddHours(9),
            DemoAnchorUtc.AddDays(6).AddHours(14),
            DemoAnchorUtc.AddDays(8).AddHours(10)
        };

        InterviewSlot? chosen = null;
        foreach (var start in slotStarts)
        {
            var slot = new InterviewSlot
            {
                Id = Guid.NewGuid(),
                InterviewId = interview.Id,
                StartUtc = start,
                EndUtc = start.AddMinutes(30),
                ProposedBy = InterviewSlotProposer.Employer
            };
            db.InterviewSlots.Add(slot);
            chosen ??= slot;
        }

        if (chooseFirstSlot && chosen is not null)
        {
            // Break Interview <-> InterviewSlot cycle (ChosenSlotId FK vs InterviewId FK) for EF SaveChanges.
            await db.SaveChangesAsync(cancellationToken);
            chosen.IsSelected = true;
            interview.ChosenSlotId = chosen.Id;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task RemoveMessagesAsync(
        JobsyDbContext db,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var messages = await db.ApplicationMessages
            .Where(m => m.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);
        db.ApplicationMessages.RemoveRange(messages);
    }

    private static async Task ReplaceMessagesAsync(
        JobsyDbContext db,
        Guid applicationId,
        Guid senderUserId,
        string body,
        DateTime? readAtUtc,
        CancellationToken cancellationToken)
    {
        await RemoveMessagesAsync(db, applicationId, cancellationToken);
        db.ApplicationMessages.Add(new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SenderUserId = senderUserId,
            Body = body,
            ReadAtUtc = readAtUtc,
            BlockReason = ApplicationMessageBlockReason.None,
            CreatedAtUtc = DemoAnchorUtc.AddDays(-1)
        });
    }

    private static async Task EnsureDemoConversationAsync(
        JobsyDbContext db,
        Guid applicationId,
        Guid employerUserId,
        Guid candidateUserId,
        string employerBody,
        string candidateBody,
        CancellationToken cancellationToken)
    {
        await RemoveMessagesAsync(db, applicationId, cancellationToken);
        var baseAt = DemoAnchorUtc.AddDays(-2);
        db.ApplicationMessages.Add(new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SenderUserId = employerUserId,
            Body = employerBody,
            ReadAtUtc = baseAt.AddHours(1),
            BlockReason = ApplicationMessageBlockReason.None,
            CreatedAtUtc = baseAt
        });
        db.ApplicationMessages.Add(new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SenderUserId = candidateUserId,
            Body = candidateBody,
            ReadAtUtc = baseAt.AddHours(3),
            BlockReason = ApplicationMessageBlockReason.None,
            CreatedAtUtc = baseAt.AddHours(2)
        });
    }

    private static async Task RemoveOutcomeAsync(
        JobsyDbContext db,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var outcome = await db.ApplicationOutcomes.FirstOrDefaultAsync(o => o.ApplicationId == applicationId, cancellationToken);
        if (outcome is not null)
        {
            db.ApplicationOutcomes.Remove(outcome);
        }
    }

    private static async Task EnsureOutcomeAsync(
        JobsyDbContext db,
        Guid applicationId,
        Guid branchManagerId,
        CancellationToken cancellationToken)
    {
        var existing = await db.ApplicationOutcomes.FirstOrDefaultAsync(o => o.ApplicationId == applicationId, cancellationToken);
        if (existing is not null)
        {
            existing.Outcome = AfterMatchOutcomeKind.NotNow;
            existing.Reason =
                "Bedankt voor je tijd. We kozen nu voor iemand met iets meer ervaring in de functie.";
            existing.ToneCheckOk = true;
            existing.RecordedByUserId = branchManagerId;
            return;
        }

        db.ApplicationOutcomes.Add(new ApplicationOutcome
        {
            ApplicationId = applicationId,
            Outcome = AfterMatchOutcomeKind.NotNow,
            Reason = "Bedankt voor je tijd. We kozen nu voor iemand met iets meer ervaring in de functie.",
            ToneCheckOk = true,
            CreatedAtUtc = DemoAnchorUtc.AddDays(3),
            RecordedByUserId = branchManagerId
        });
    }

    private static async Task EnsureExternalAppliedAsync(
        JobsyDbContext db,
        User candidate,
        CancellationToken cancellationToken)
    {
        var row = await db.CandidateExternalVacancies
            .FirstOrDefaultAsync(v => v.Id == TestAccountsIds.SollicDemoExternalVacancy, cancellationToken);
        if (row is null)
        {
            row = new CandidateExternalVacancy
            {
                Id = TestAccountsIds.SollicDemoExternalVacancy,
                CandidateUserId = candidate.Id,
                SourceUrl = "https://example.com/vacatures/test-acceptatie-buiten-lobsy",
                SourceHost = "example.com",
                Title = "Allround medewerker tuincentrum (extern)",
                CompanyName = "Tuincentrum Voorbeeld BV",
                Place = "Wateringen",
                HoursText = "32–40 uur",
                PayText = "€ 14,50 per uur",
                StartText = "Per direct",
                TrainingText = "Inwerken op locatie",
                SavedAtUtc = DemoAnchorUtc.AddDays(-4),
                Status = CandidateExternalVacancyStatus.Applied
            };
            db.CandidateExternalVacancies.Add(row);
            return;
        }

        row.CandidateUserId = candidate.Id;
        row.Status = CandidateExternalVacancyStatus.Applied;
        row.Title = "Allround medewerker tuincentrum (extern)";
        row.CompanyName = "Tuincentrum Voorbeeld BV";
    }

    private static async Task EnsureInitiativesAsync(
        JobsyDbContext db,
        User candidate,
        CompanySeed[] companies,
        CancellationToken cancellationToken)
    {
        var groen = companies.First(c => c.Id == TestAccountsIds.SollicDemoCompanyGroen);
        var retail = companies.First(c => c.Id == TestAccountsIds.SollicDemoCompanyRetail);

        await UpsertInitiativeAsync(
            db,
            TestAccountsIds.SollicDemoInitiativeViewed,
            candidate,
            groen.Id,
            WorkInitiativeStatus.Viewed,
            "Ik wil graag in het groen werken en leer graag het vak.",
            DemoAnchorUtc.AddDays(-6),
            employerViewedAtUtc: DemoAnchorUtc.AddDays(-5),
            respondedAtUtc: null,
            cancellationToken);

        await UpsertInitiativeAsync(
            db,
            TestAccountsIds.SollicDemoInitiativeAccepted,
            candidate,
            retail.Id,
            WorkInitiativeStatus.Accepted,
            "Ik wil graag in de winkel werken en klanten helpen.",
            DemoAnchorUtc.AddDays(-9),
            employerViewedAtUtc: DemoAnchorUtc.AddDays(-8),
            respondedAtUtc: DemoAnchorUtc.AddDays(-7),
            cancellationToken);
    }

    private static async Task UpsertInitiativeAsync(
        JobsyDbContext db,
        Guid id,
        User candidate,
        Guid rootCompanyId,
        WorkInitiativeStatus status,
        string motivation,
        DateTime createdAtUtc,
        DateTime? employerViewedAtUtc,
        DateTime? respondedAtUtc,
        CancellationToken cancellationToken)
    {
        var row = await db.WorkInitiatives.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (row is null)
        {
            row = new WorkInitiative
            {
                Id = id,
                CandidateUserId = candidate.Id,
                RootCompanyId = rootCompanyId,
                Status = status,
                MatchLabel = InitiativeMatchLabel.Good,
                Motivation = motivation,
                SnapshotRoleTitle = "Allround medewerker",
                SnapshotRegionLabel = "Haaglanden",
                SnapshotWorkDnaSummary = "Nauwkeurig, klantgericht, teamspeler",
                SnapshotAvailabilitySummary = "Ma–za",
                SnapshotHoursPerWeek = 24,
                CandidateDisplayName = candidate.FullName,
                CandidateEmail = candidate.Email,
                CountsTowardWeeklyLimit = true,
                CreatedAtUtc = createdAtUtc,
                EmployerViewedAtUtc = employerViewedAtUtc,
                RespondedAtUtc = respondedAtUtc,
                ResponseDeadlineUtc = InitiativeBusinessDayRules.AddBusinessDaysUtc(createdAtUtc, 5)
            };
            db.WorkInitiatives.Add(row);
            return;
        }

        row.CandidateUserId = candidate.Id;
        row.RootCompanyId = rootCompanyId;
        row.Status = status;
        row.Motivation = motivation;
        row.CandidateDisplayName = candidate.FullName;
        row.CandidateEmail = candidate.Email;
        row.EmployerViewedAtUtc = employerViewedAtUtc;
        row.RespondedAtUtc = respondedAtUtc;
        row.ResponseDeadlineUtc = InitiativeBusinessDayRules.AddBusinessDaysUtc(createdAtUtc, 5);
    }

    private static async Task<bool> EntityExistsAsync<TEntity>(
        DbSet<TEntity> set,
        Guid id,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        foreach (var entity in set.Local)
        {
            var idProp = entity.GetType().GetProperty("Id");
            if (idProp?.GetValue(entity) is Guid trackedId && trackedId == id)
            {
                return true;
            }
        }

        return await set.AsNoTracking().AnyAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

}
