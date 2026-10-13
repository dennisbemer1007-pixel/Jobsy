using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

/// <summary>
/// Doc scenarios S1–S5 from docs/psychometrie/TESTARCHITECTUUR_VOORSTEL.md §4.3.
/// </summary>
public class MatchCalibrationV1ScenarioTests
{
    [Fact]
    public void S1_sparse_profile_legacy_inflates_v1_moderates()
    {
        var input = S1Input();
        var legacy = ProfileVacancyMatchCalculator.CalculateLegacy(input);
        var v1 = ProfileVacancyMatchCalculator.Calculate(input, useMatchCalibrationV1: true);

        Assert.True(legacy.TotalPercent >= 74, $"legacy={legacy.TotalPercent}");
        Assert.InRange(v1.TotalPercent, 18, 45);
        Assert.InRange(v1.ConfidencePercent!.Value, 8, 30);
        Assert.True(v1.TotalPercent < legacy.TotalPercent - 25);
    }

    [Fact]
    public void S2_wrong_sector_tests_high_v1_below_legacy()
    {
        var input = S2Input();
        var legacy = ProfileVacancyMatchCalculator.CalculateLegacy(input);
        var v1 = ProfileVacancyMatchCalculator.Calculate(input, useMatchCalibrationV1: true);

        Assert.InRange(legacy.TotalPercent, 68, 85);
        Assert.InRange(v1.TotalPercent, 45, 72);
        Assert.InRange(v1.ConfidencePercent!.Value, 45, 85);
        Assert.True(v1.TotalPercent <= legacy.TotalPercent + 5);
    }

    [Fact]
    public void S3_strong_retail_fit_stays_high()
    {
        var input = S3Input();
        var legacy = ProfileVacancyMatchCalculator.CalculateLegacy(input);
        var v1 = ProfileVacancyMatchCalculator.Calculate(input, useMatchCalibrationV1: true);

        Assert.InRange(legacy.TotalPercent, 74, 95);
        Assert.InRange(v1.TotalPercent, 70, 92);
        Assert.True(v1.ConfidencePercent!.Value >= 85);
    }

    [Fact]
    public void S4_travel_and_hours_mismatch_stays_low()
    {
        var input = S4Input();
        var legacy = ProfileVacancyMatchCalculator.CalculateLegacy(input);
        var v1 = ProfileVacancyMatchCalculator.Calculate(input, useMatchCalibrationV1: true);

        Assert.True(legacy.TotalPercent < 55);
        Assert.InRange(v1.TotalPercent, 8, 40);
        Assert.True(v1.DealBreaker);
    }

    [Fact]
    public void S5_broad_admin_stays_above_threshold_but_below_legacy()
    {
        var input = S5Input();
        var legacy = ProfileVacancyMatchCalculator.CalculateLegacy(input);
        var v1 = ProfileVacancyMatchCalculator.Calculate(input, useMatchCalibrationV1: true);

        Assert.True(legacy.TotalPercent >= 80);
        Assert.InRange(v1.TotalPercent, 72, 88);
        Assert.True(v1.TotalPercent >= MatchCalibrationBands.DisplayThreshold);
        Assert.True(v1.ConfidencePercent!.Value >= 80);
    }

    [Theory]
    [InlineData(0.22, 0.85, 55)]
    [InlineData(0.92, 0.88, 85)]
    public void Shrinkage_formula_matches_doc(double confidence, double fitRaw, int expectedApprox)
    {
        var display = MatchCalibrationV1Calculator.ShrinkDisplay(fitRaw, confidence);
        Assert.InRange(display, expectedApprox - 8, expectedApprox + 8);
    }

    [Fact]
    public void Symmetric_competency_penalizes_overfit()
    {
        var candidate = new CompetencyScores(90, 90, 90, 90);
        var vacancy = new CompetencyScores(60, 60, 60, 60);
        var asymmetric = VacancyCompetencyProfile.Fit01(candidate, vacancy);
        var symmetric = VacancyCompetencyProfile.Fit01Symmetric(candidate, vacancy);
        Assert.True(symmetric < asymmetric);
    }

    private static ProfileVacancyMatchInput S1Input() => new()
    {
        VacancyId = Guid.NewGuid(),
        VacancyTitle = "Magazijnmedewerker",
        WorkTypes = ["Logistiek"],
        CandidateRoles = ["Logistiek medewerker"],
        Core = new MatchScoreInput
        {
            VacancyHours = new HoursRange(32, 40),
            VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
            CandidateAgeYears = 22
        }
    };

    private static ProfileVacancyMatchInput S2Input() => new()
    {
        VacancyId = Guid.NewGuid(),
        VacancyTitle = "Verpleegkundige",
        VacancyDescription = "Zorg en begeleiding.",
        WorkTypes = ["Zorg"],
        CandidateRoles = ["Horeca medewerker"],
        CandidateCompetencies = new CompetencyScores(80, 80, 80, 80),
        CandidateCultureScores = new CulturePersonalityScores(
            60, 60, 60, 60, 60, 60, 60, 80, 60, 60, 70),
        CandidateRiasecScores = new RiasecScores(30, 30, 30, 70, 40, 40),
        CompetencyMatchEvidence = AssessmentMatchEvidence.FullQuickScan,
        CareerMatchEvidence = AssessmentMatchEvidence.FullQuickScan,
        Core = new MatchScoreInput
        {
            EstimatedTravelMinutes = 15,
            MaxTravelMinutes = 30,
            CandidateHours = new HoursRange(24, 32),
            VacancyHours = new HoursRange(24, 36),
            VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
            CandidateAgeYears = 28
        }
    };

    private static ProfileVacancyMatchInput S3Input() => new()
    {
        VacancyId = Guid.NewGuid(),
        VacancyTitle = "Verkoopmedewerker mode",
        WorkTypes = ["Winkel"],
        CandidateRoles = ["Winkel"],
        CandidateCompetencies = new CompetencyScores(85, 82, 78, 70),
        CandidateCultureScores = new CulturePersonalityScores(
            70, 65, 75, 60, 55, 70, 65, 88, 72, 70, 75),
        CandidateValuesScores = new SchwartzValuesScores(70, 65, 75, 60, 55),
        CandidateRiasecScores = new RiasecScores(25, 30, 35, 40, 85, 45),
        CareerDeepCompleted = true,
        CompetencyMatchEvidence = AssessmentMatchEvidence.FullQuickScan,
        CareerMatchEvidence = AssessmentMatchEvidence.DeepAnalysis,
        Core = new MatchScoreInput
        {
            EstimatedTravelMinutes = 12,
            MaxTravelMinutes = 25,
            CandidateHours = new HoursRange(24, 32),
            VacancyHours = new HoursRange(24, 32),
            VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
            CandidateAgeYears = 24
        }
    };

    private static ProfileVacancyMatchInput S4Input() => new()
    {
        VacancyId = Guid.NewGuid(),
        VacancyTitle = "Fulltime productiemedewerker",
        WorkTypes = ["Productie"],
        CandidateRoles = ["Productie"],
        Core = new MatchScoreInput
        {
            EstimatedTravelMinutes = 90,
            MaxTravelMinutes = 20,
            CandidateHours = new HoursRange(8, 12),
            VacancyHours = new HoursRange(32, 40),
            VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
            CandidateAgeYears = 30
        }
    };

    private static ProfileVacancyMatchInput S5Input() => MultidimensionalMatchingTests_BroadAdminInput();

    private static ProfileVacancyMatchInput MultidimensionalMatchingTests_BroadAdminInput()
        => new()
        {
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Business Controller",
            VacancyDescription = "Financiële planning, rapportages en cijfers bewaken.",
            WorkTypes = ["Administratie"],
            CandidateRoles = ["Administratief medewerker", "Boekhoudkundig assistent"],
            CandidateEducations = ["HBO"],
            RequiredEducation = "HBO",
            CandidateEmployerCount = 2,
            CandidateCompetencies = new CompetencyScores(70, 88, 75, 60),
            CandidateCultureScores = new CulturePersonalityScores(
                55, 45, 60, 50, 40, 50, 50, 90, 45, 55, 70),
            CandidateRiasecScores = new RiasecScores(20, 40, 20, 25, 50, 95),
            CareerDeepCompleted = true,
            CareerOccupations =
            [
                new CareerOccupationMatch(
                    "Financieel administratief medewerker",
                    88,
                    CareerCompassBuilder.BandStrong,
                    "Cijfers.",
                    ["financieel", "administratief", "boekhoud"])
            ],
            CompetencyMatchEvidence = AssessmentMatchEvidence.FullQuickScan,
            CareerMatchEvidence = AssessmentMatchEvidence.DeepAnalysis,
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = 12,
                MaxTravelMinutes = 30,
                CandidateHours = new HoursRange(32, 40),
                VacancyHours = new HoursRange(32, 40),
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateAgeYears = 28
            }
        };
}
