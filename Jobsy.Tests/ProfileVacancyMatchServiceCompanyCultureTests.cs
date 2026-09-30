using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jobsy.Tests;

public class ProfileVacancyMatchServiceCompanyCultureTests
{
    private static readonly CulturePersonalityScores CandidateCulture = new(
        Autonomy: 90, Informal: 85, Collaboration: 80, Flexibility: 75, Innovation: 70, PeopleFirst: 65,
        Openness: 80, Conscientiousness: 70, Extraversion: 75, Agreeableness: 80, EmotionalStability: 70);

    private static readonly CulturePersonalityScores FittingCompany = new(
        Autonomy: 88, Informal: 82, Collaboration: 78, Flexibility: 74, Innovation: 68, PeopleFirst: 66,
        Openness: 70, Conscientiousness: 70, Extraversion: 70, Agreeableness: 70, EmotionalStability: 70);

    private static readonly CulturePersonalityScores MismatchCompany = new(
        Autonomy: 10, Informal: 15, Collaboration: 20, Flexibility: 25, Innovation: 30, PeopleFirst: 35,
        Openness: 40, Conscientiousness: 40, Extraversion: 40, Agreeableness: 40, EmotionalStability: 40);

    private static readonly CompetencyScores Competencies = new(85, 80, 75, 70, 65);

    [Fact]
    public async Task ScoreAsync_blends_company_culture_when_present_and_skips_when_absent()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var companyA = SeedCompany(db, "A");
        var companyB = SeedCompany(db, "B");
        SeedCompletedCompanyCulture(db, companyA.Id, FittingCompany);
        await db.SaveChangesAsync();

        var lookup = new CompanyCultureLookup(db, cache);
        var service = new ProfileVacancyMatchService(db, new CompanyAuthorizationService(db), new UserLookupService(db), lookup);
        var context = MakeContext();

        var vacancyA = MakeRecord(companyA.Id, "Kassamedewerker A");
        var vacancyB = MakeRecord(companyB.Id, "Kassamedewerker B");

        var scored = await service.ScoreAsync(context, [(vacancyA, 10), (vacancyB, 10)]);
        Assert.True(scored[vacancyA.Id].CompetencyScore01 is double);
        Assert.True(scored[vacancyB.Id].CompetencyScore01 is double);

        // Same vacancy inputs except company culture → A uses 0.55/0.45 blend, B personality only.
        var personalityOnly = CulturePersonalityFitRules.PersonalityFit01(
            CandidateCulture, vacancyB.Title, vacancyB.Description);
        var culture01 = CulturePersonalityFitRules.CultureFit01(CandidateCulture, FittingCompany);
        var blended = 0.55 * personalityOnly + 0.45 * culture01;
        var baseCompetency = VacancyCompetencyProfile.Fit01(
            Competencies, VacancyCompetencyProfile.Infer(vacancyA));
        var expectedA = 0.75 * baseCompetency + 0.25 * blended;
        var expectedB = 0.75 * baseCompetency + 0.25 * personalityOnly;

        Assert.Equal(expectedA, scored[vacancyA.Id].CompetencyScore01!.Value, 5);
        Assert.Equal(expectedB, scored[vacancyB.Id].CompetencyScore01!.Value, 5);
        Assert.NotEqual(scored[vacancyA.Id].CompetencyScore01, scored[vacancyB.Id].CompetencyScore01);
    }

    [Fact]
    public async Task ScoreAsync_vestiging_without_own_profile_uses_organisation()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var org = SeedCompany(db, "Org");
        var vestiging = SeedCompany(db, "Vestiging", parentId: org.Id);
        SeedCompletedCompanyCulture(db, org.Id, FittingCompany);
        await db.SaveChangesAsync();

        var lookup = new CompanyCultureLookup(db, cache);
        var service = new ProfileVacancyMatchService(db, new CompanyAuthorizationService(db), new UserLookupService(db), lookup);
        var vacancy = MakeRecord(vestiging.Id, "Zorgmedewerker");
        var scored = await service.ScoreAsync(MakeContext(), [(vacancy, 8)]);

        var personalityOnly = CulturePersonalityFitRules.PersonalityFit01(
            CandidateCulture, vacancy.Title, vacancy.Description);
        var culture01 = CulturePersonalityFitRules.CultureFit01(CandidateCulture, FittingCompany);
        var blended = 0.55 * personalityOnly + 0.45 * culture01;
        var baseCompetency = VacancyCompetencyProfile.Fit01(
            Competencies, VacancyCompetencyProfile.Infer(vacancy));
        var expected = 0.75 * baseCompetency + 0.25 * blended;
        Assert.Equal(expected, scored[vacancy.Id].CompetencyScore01!.Value, 5);
    }

    [Fact]
    public async Task ScoreAsync_ignores_incomplete_company_profile()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var company = SeedCompany(db, "Draft");
        db.CompanyCultureProfiles.Add(new CompanyCultureProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Status = CandidateCompetencyStatuses.Draft,
            AnswersJson = "{}",
            AutonomyPercent = 90,
            InformalPercent = 90,
            CollaborationPercent = 90,
            FlexibilityPercent = 90,
            InnovationPercent = 90,
            PeopleFirstPercent = 90,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var lookup = new CompanyCultureLookup(db, cache);
        var service = new ProfileVacancyMatchService(db, new CompanyAuthorizationService(db), new UserLookupService(db), lookup);
        var vacancy = MakeRecord(company.Id, "Magazijn");
        var scored = await service.ScoreAsync(MakeContext(), [(vacancy, 10)]);

        var personalityOnly = CulturePersonalityFitRules.PersonalityFit01(
            CandidateCulture, vacancy.Title, vacancy.Description);
        var baseCompetency = VacancyCompetencyProfile.Fit01(
            Competencies, VacancyCompetencyProfile.Infer(vacancy));
        var expected = 0.75 * baseCompetency + 0.25 * personalityOnly;
        Assert.Equal(expected, scored[vacancy.Id].CompetencyScore01!.Value, 5);
    }

    [Fact]
    public async Task ScoreAsync_intermediary_vacancy_uses_end_client_not_bureau()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var endClient = SeedCompany(db, "EndClient");
        var bureau = SeedCompany(db, "Bureau");
        SeedCompletedCompanyCulture(db, endClient.Id, FittingCompany);
        SeedCompletedCompanyCulture(db, bureau.Id, MismatchCompany);
        await db.SaveChangesAsync();

        var lookup = new CompanyCultureLookup(db, cache);
        var service = new ProfileVacancyMatchService(db, new CompanyAuthorizationService(db), new UserLookupService(db), lookup);
        var vacancy = MakeRecord(endClient.Id, "Uitzendkracht", intermediaryCompanyId: bureau.Id);
        var scored = await service.ScoreAsync(MakeContext(), [(vacancy, 12)]);

        var personalityOnly = CulturePersonalityFitRules.PersonalityFit01(
            CandidateCulture, vacancy.Title, vacancy.Description);
        var fitCulture = CulturePersonalityFitRules.CultureFit01(CandidateCulture, FittingCompany);
        var mismatchCulture = CulturePersonalityFitRules.CultureFit01(CandidateCulture, MismatchCompany);
        var blendedFit = 0.55 * personalityOnly + 0.45 * fitCulture;
        var blendedMismatch = 0.55 * personalityOnly + 0.45 * mismatchCulture;
        var baseCompetency = VacancyCompetencyProfile.Fit01(
            Competencies, VacancyCompetencyProfile.Infer(vacancy));
        var expectedFit = 0.75 * baseCompetency + 0.25 * blendedFit;
        var expectedMismatch = 0.75 * baseCompetency + 0.25 * blendedMismatch;

        Assert.Equal(expectedFit, scored[vacancy.Id].CompetencyScore01!.Value, 5);
        Assert.True(
            Math.Abs(expectedMismatch - scored[vacancy.Id].CompetencyScore01!.Value) > 0.001);
    }

    [Fact]
    public async Task Lookup_batches_ids_caches_and_evicts_company_plus_children_on_save()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        await using var db = CreateLoggedDb(out var queries);
        var org = SeedCompany(db, "Org");
        var child = SeedCompany(db, "Child", parentId: org.Id);
        var other = SeedCompany(db, "Other");
        SeedCompletedCompanyCulture(db, org.Id, FittingCompany);
        await db.SaveChangesAsync();
        queries.Clear();

        var lookup = new CompanyCultureLookup(db, cache);
        var first = await lookup.GetForCompaniesAsync([child.Id, other.Id, org.Id]);
        Assert.True(first.ContainsKey(child.Id));
        Assert.True(first.ContainsKey(org.Id));
        Assert.False(first.ContainsKey(other.Id));
        // Two round-trips: companies (parents) + profiles. Never N+1 per id.
        Assert.True(queries.Count <= 2, $"Expected ≤2 queries, got {queries.Count}: {string.Join(" | ", queries)}");

        queries.Clear();
        var second = await lookup.GetForCompaniesAsync([child.Id, other.Id, org.Id]);
        Assert.Equal(first[child.Id], second[child.Id]);
        Assert.Empty(queries); // cache hit

        // Mutate org profile under the cache; still stale until eviction.
        var row = await db.CompanyCultureProfiles.FirstAsync(p => p.CompanyId == org.Id);
        row.AutonomyPercent = 11;
        row.InformalPercent = 12;
        row.CollaborationPercent = 13;
        row.FlexibilityPercent = 14;
        row.InnovationPercent = 15;
        row.PeopleFirstPercent = 16;
        await db.SaveChangesAsync();

        var stale = await lookup.GetForCompaniesAsync([child.Id]);
        Assert.Equal(FittingCompany.Autonomy, stale[child.Id].Autonomy);

        var cultureService = new CompanyCultureService(db, cache);
        await cultureService.SaveAsync(
            org.Id,
            BuildCultureAnswers(),
            complete: true);

        var refreshed = await lookup.GetForCompaniesAsync([child.Id, org.Id]);
        Assert.NotEqual(FittingCompany.Autonomy, refreshed[org.Id].Autonomy);
        Assert.Equal(refreshed[org.Id].Autonomy, refreshed[child.Id].Autonomy);
    }

    [Fact]
    public async Task ScoreAsync_returns_culture_fit_when_pillars_and_company_scan_present()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var company = SeedCompany(db, "Cultuur");
        SeedCompletedCompanyCulture(db, company.Id, FittingCompany);
        await db.SaveChangesAsync();

        var lookup = new CompanyCultureLookup(db, cache);
        var service = new ProfileVacancyMatchService(db, new CompanyAuthorizationService(db), new UserLookupService(db), lookup);
        var vacancy = MakeRecord(
            company.Id,
            "Vulploeg",
            culturePillars: ["informeel", "samen", "kalm"]);
        var scored = await service.ScoreAsync(MakeContext(), [(vacancy, 10)]);
        Assert.NotNull(scored[vacancy.Id].CultureFit);
    }

    [Fact]
    public void Match_fingerprint_includes_company_culture_algorithm_version()
    {
        Assert.Equal("company-culture-v1", CandidateInsightsFingerprint.MatchAlgorithmVersion);
        var withVersion = CandidateInsightsFingerprint.ForMatches(
            Competencies,
            new RiasecScores(20, 30, 25, 95, 40, 35),
            CandidateCulture,
            new SchwartzValuesScores(60, 70, 50, 55, 65),
            new CandidatePreferencesDto(["Winkel"], 30, "Fiets"),
            null);
        Assert.False(string.IsNullOrWhiteSpace(withVersion));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("company-culture-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static JobsyDbContext CreateLoggedDb(out List<string> queries)
    {
        var list = new List<string>();
        queries = list;
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("company-culture-log-" + Guid.NewGuid())
            .LogTo(msg =>
            {
                if (msg.Contains("Executing", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("Query", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(msg);
                }
            }, LogLevel.Information)
            .Options;
        return new JobsyDbContext(options);
    }

    private static Company SeedCompany(JobsyDbContext db, string name, Guid? parentId = null)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            KvkNumber = "9000" + Random.Shared.Next(1000, 9999),
            Address = "Straat 1",
            Location = new GeoPoint(52.07, 4.30),
            ParentCompanyId = parentId,
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };
        db.Companies.Add(company);
        return company;
    }

    private static void SeedCompletedCompanyCulture(
        JobsyDbContext db,
        Guid companyId,
        CulturePersonalityScores scores)
    {
        db.CompanyCultureProfiles.Add(new CompanyCultureProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}",
            AutonomyPercent = scores.Autonomy,
            InformalPercent = scores.Informal,
            CollaborationPercent = scores.Collaboration,
            FlexibilityPercent = scores.Flexibility,
            InnovationPercent = scores.Innovation,
            PeopleFirstPercent = scores.PeopleFirst,
            OpennessPercent = scores.Openness,
            ConscientiousnessPercent = scores.Conscientiousness,
            ExtraversionPercent = scores.Extraversion,
            AgreeablenessPercent = scores.Agreeableness,
            EmotionalStabilityPercent = scores.EmotionalStability,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        });
    }

    private static IReadOnlyDictionary<int, int> BuildCultureAnswers()
    {
        var map = new Dictionary<int, int>();
        for (var i = 1; i <= 12; i++)
        {
            map[i] = 2; // different from FittingCompany seed so eviction is observable
        }

        for (var i = 13; i <= 18; i++)
        {
            map[i] = 3;
        }

        return map;
    }

    private static ProfileVacancyMatchContext MakeContext()
        => new()
        {
            UserId = Guid.NewGuid(),
            Prefs = new CandidatePreferencesDto(
                ["Winkel"],
                30,
                "Fiets",
                MinHoursPerWeek: 16,
                MaxHoursPerWeek: 24,
                FlexibleTimes: true),
            AgeYears = 28,
            Competencies = Competencies,
            CultureScores = CandidateCulture,
            RiasecTags = [],
            CareerOccupations = []
        };

    private static VacancyDiscoveryRecord MakeRecord(
        Guid companyId,
        string title,
        Guid? intermediaryCompanyId = null,
        IReadOnlyList<string>? culturePillars = null)
        => new(
            Guid.NewGuid(),
            title,
            "Werk in de winkel met klanten.",
            14.50m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            VacancyStatus.Active,
            companyId,
            "Bedrijf",
            "Straat 1",
            null,
            null,
            null,
            52.07,
            4.30,
            TransportMode.Bike,
            [TransportLabels.Bike],
            WorkType.Winkel,
            WorkTypeLabels.Expand(WorkType.Winkel).FirstOrDefault(),
            WorkTypeLabels.Expand(WorkType.Winkel),
            false,
            null,
            0,
            null,
            [],
            null,
            null,
            null,
            null,
            VacancySource.Manual,
            16,
            24,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            intermediaryCompanyId,
            VacancyKind.Regular,
            null,
            null,
            true,
            null,
            [],
            VacancyCategoryDefaults.RegulierId,
            "Regulier",
            "#64748b",
            false,
            null,
            null,
            true,
            false,
            CulturePillars: culturePillars);
}
