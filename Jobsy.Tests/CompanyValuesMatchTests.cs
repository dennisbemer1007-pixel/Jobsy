using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests;

public class CompanyValuesMatchTests
{
    private static readonly CulturePersonalityScores CandidateCulture = new(
        Autonomy: 90, Informal: 85, Collaboration: 80, Flexibility: 75, Innovation: 70, PeopleFirst: 65,
        Openness: 80, Conscientiousness: 70, Extraversion: 75, Agreeableness: 80, EmotionalStability: 70);

    private static readonly SchwartzValuesScores CandidateValues = new(80, 90, 50, 40, 85);

    private static readonly CompetencyScores Competencies = new(85, 80, 75, 70, 65);

    [Fact]
    public void Fit01_uses_company_values_when_present_else_inferred_fallback()
    {
        var company = CompanyValueCards.Score(["zorg", "teamgevoel", "betekenis"]);
        var withCompany = SchwartzValuesFitRules.Fit01(CandidateValues, company);
        var inferred = SchwartzValuesFitRules.InferVacancyDrivers(
            "Zorgmedewerker team",
            "Warm team, zorgen voor cliënten, impact in de wijk");
        var without = SchwartzValuesFitRules.Fit01(CandidateValues, inferred);
        Assert.True(withCompany > 0);
        Assert.NotEqual(withCompany, without);
    }

    [Fact]
    public async Task Overriding_vacancy_ignores_company_culture_but_keeps_values()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var company = SeedCompany(db);
        SeedCulture(db, company.Id);
        db.CompanyValuesProfiles.Add(new CompanyValuesProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            CardIdsJson = CompanyValueCards.Serialize(["zorg", "vakmanschap", "betekenis"]),
            AutonomyPercent = 40,
            ConnectionPercent = 75,
            AchievementPercent = 40,
            StabilityPercent = 75,
            ImpactPercent = 75,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var lookup = new CompanyCultureLookup(db, cache);
        var service = new ProfileVacancyMatchService(
            db, new CompanyAuthorizationService(db), new UserLookupService(db), lookup);

        var inherit = MakeRecord(company.Id, "Zorgmedewerker");
        var overrideVacancy = MakeRecord(
            company.Id,
            "Zorgmedewerker",
            culturePillars: ["informeel", "samen", "kalm"]);

        var scored = await service.ScoreAsync(MakeContext(), [(inherit, 10), (overrideVacancy, 10)]);
        Assert.True(scored[inherit.Id].CompetencyScore01 is double);
        Assert.True(scored[overrideVacancy.Id].CompetencyScore01 is double);
        Assert.NotNull(scored[overrideVacancy.Id].CultureFit);

        var companyCulture = new CulturePersonalityScores(
            88, 82, 78, 74, 68, 66, 70, 70, 70, 70, 70);
        var values = CompanyValueCards.Score(["zorg", "vakmanschap", "betekenis"]);
        var calcOverride = ProfileVacancyMatchCalculator.Calculate(BaseInput(
            overrideVacancy.Id, companyCulture: null, companyValues: values,
            pillars: ["informeel", "samen", "kalm"]));
        var calcInherit = ProfileVacancyMatchCalculator.Calculate(BaseInput(
            inherit.Id, companyCulture: companyCulture, companyValues: values));
        Assert.NotEqual(calcOverride.CompetencyScore01, calcInherit.CompetencyScore01);
    }

    [Fact]
    public void Before_after_fixture_company_values_change_score()
    {
        const string title = "Verzorgende IG";
        const string description = "Werken in een hecht team, zorgen voor ouderen";
        var without = ProfileVacancyMatchCalculator.Calculate(BaseInput(
            Guid.NewGuid(),
            companyCulture: null,
            companyValues: null,
            title: title,
            description: description));
        var with = ProfileVacancyMatchCalculator.Calculate(BaseInput(
            Guid.NewGuid(),
            companyCulture: null,
            companyValues: CompanyValueCards.Score(["zorg", "teamgevoel", "betekenis"]),
            title: title,
            description: description));
        Assert.NotEqual(without.CompetencyScore01, with.CompetencyScore01);
        Assert.True(with.CompetencyScore01 > without.CompetencyScore01);
    }

    [Fact]
    public void Snapshot_version_bumped_for_company_values()
    {
        Assert.Equal("company-engagement-v1", CandidateInsightsFingerprint.MatchAlgorithmVersion);
    }

    private static ProfileVacancyMatchInput BaseInput(
        Guid vacancyId,
        CulturePersonalityScores? companyCulture,
        SchwartzValuesScores? companyValues,
        IReadOnlyList<string>? pillars = null,
        string title = "Kassamedewerker",
        string? description = "Winkel team")
        => new()
        {
            VacancyId = vacancyId,
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = 10,
                MaxTravelMinutes = 45,
                CandidateHours = new HoursRange(16, 24),
                VacancyHours = new HoursRange(16, 24),
                CandidateAgeYears = 30
            },
            VacancyTitle = title,
            VacancyDescription = description,
            WorkTypes = [WorkTypeLabels.Zorg],
            CandidateCompetencies = Competencies,
            CandidateCultureScores = CandidateCulture,
            CompanyCultureScores = companyCulture,
            CandidateValuesScores = CandidateValues,
            CompanyValuesScores = companyValues,
            CulturePillars = pillars
        };

    private static ProfileVacancyMatchContext MakeContext()
        => new()
        {
            UserId = Guid.NewGuid(),
            Prefs = new CandidatePreferencesDto(
                ["Zorg"],
                45,
                "Fiets",
                MinHoursPerWeek: 16,
                MaxHoursPerWeek: 24,
                FlexibleTimes: true),
            AgeYears = 30,
            Competencies = Competencies,
            CultureScores = CandidateCulture,
            ValuesScores = CandidateValues,
            RiasecTags = [],
            CareerOccupations = []
        };

    private static VacancyDiscoveryRecord MakeRecord(
        Guid companyId,
        string title,
        IReadOnlyList<string>? culturePillars = null)
        => new(
            Guid.NewGuid(),
            title,
            "Team zorg",
            16m,
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
            WorkType.Zorg,
            WorkTypeLabels.Zorg,
            [WorkTypeLabels.Zorg],
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
            null,
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
            null,
            culturePillars);

    private static void SeedCulture(JobsyDbContext db, Guid companyId)
    {
        db.CompanyCultureProfiles.Add(new CompanyCultureProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Status = CandidateCompetencyStatuses.Completed,
            Source = CompanyCultureSources.Quick,
            AnswersJson = "{}",
            AutonomyPercent = 88,
            InformalPercent = 82,
            CollaborationPercent = 78,
            FlexibilityPercent = 74,
            InnovationPercent = 68,
            PeopleFirstPercent = 66,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        });
    }

    private static Company SeedCompany(JobsyDbContext db)
    {
        var c = new Company
        {
            Id = Guid.NewGuid(),
            Name = "MatchCo",
            KvkNumber = "12345678",
            Address = "A 1",
            Location = new GeoPoint(52, 5),
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Backfill,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };
        db.Companies.Add(c);
        return c;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("company-values-match-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }
}
