using Jobsy.Core.Rules;
using Jobsy.Core.Security;

namespace Jobsy.Tests;

public class MatchingRulesTests
{
    [Fact]
    public void HoursRange_CategorizesByMidpoint()
    {
        Assert.Equal(HoursCategory.SideJob, new HoursRange(4, 8).Category);
        Assert.Equal(HoursCategory.PartTimeSmall, new HoursRange(12, 20).Category);
        Assert.Equal(HoursCategory.PartTimeLarge, new HoursRange(24, 30).Category);
        Assert.Equal(HoursCategory.FullTime, new HoursRange(32, 40).Category);
    }

    [Fact]
    public void HoursOverlap_ScoresVacancyCoverage()
    {
        var candidate = new HoursRange(8, 16);
        var vacancy = new HoursRange(12, 20);
        Assert.Equal(4m, HoursRangeRules.OverlapHours(candidate, vacancy));
        Assert.Equal(0.5m, HoursRangeRules.OverlapScore01(candidate, vacancy));
    }

    [Fact]
    public void YouthLabor_BlocksFifteenOnNightAndMoney()
    {
        var flags = new LegalTaskFlags
        {
            WorksAfter19 = false,
            NightShift23To06 = true,
            AdultSupervisorPresent = true,
            HandlesMoneyOrClosing = false,
            HeavyOrHazardousWork = false
        };
        var result = YouthLaborRules.Evaluate(15, flags);
        Assert.False(result.IsEligible);
        Assert.Contains("NightShift23To06", result.BlockReasons);
    }

    [Fact]
    public void YouthLabor_AdultAlwaysEligible()
    {
        var flags = new LegalTaskFlags
        {
            WorksAfter19 = true,
            NightShift23To06 = true,
            AdultSupervisorPresent = false,
            HandlesMoneyOrClosing = true,
            HeavyOrHazardousWork = true
        };
        Assert.True(YouthLaborRules.Evaluate(18, flags).IsEligible);
    }

    [Fact]
    public void MatchScore_FlexibleVacancy_NeutralDayParts()
    {
        var breakdown = MatchScoreCalculator.Calculate(new MatchScoreInput
        {
            EstimatedTravelMinutes = 15,
            MaxTravelMinutes = 30,
            CandidateHours = new HoursRange(12, 20),
            VacancyHours = new HoursRange(12, 20),
            VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
            CandidateSchedule = new SchedulePayload
            {
                Slots = new Dictionary<string, List<string>>
                {
                    ["Ma"] = ["Ochtend"]
                }
            }.Normalize(),
            CandidateAgeYears = 22
        });

        Assert.Equal(MatchScoreWeights.DayParts, breakdown.DayPartsScore);
        Assert.True(breakdown.DayPartsNeutral);
        Assert.True(breakdown.TotalPercent >= MatchScoreWeights.GuldenMiddenwegThreshold);
        Assert.False(GuldenMiddenwegRules.RequiresSafetyNetConfirmation(breakdown));
    }

    [Fact]
    public void MatchScore_LowOverlap_TriggersSafetyNet()
    {
        var breakdown = MatchScoreCalculator.Calculate(new MatchScoreInput
        {
            EstimatedTravelMinutes = 80,
            MaxTravelMinutes = 20,
            CandidateHours = new HoursRange(4, 8),
            VacancyHours = new HoursRange(32, 40),
            VacancySchedule = new SchedulePayload
            {
                Slots = new Dictionary<string, List<string>>
                {
                    ["Ma"] = ["Nacht"],
                    ["Di"] = ["Nacht"]
                }
            }.Normalize(),
            CandidateSchedule = new SchedulePayload
            {
                Slots = new Dictionary<string, List<string>>
                {
                    ["Za"] = ["Ochtend"]
                }
            }.Normalize(),
            CandidateAgeYears = 25
        });

        Assert.True(GuldenMiddenwegRules.RequiresSafetyNetConfirmation(breakdown));
        Assert.Equal("red", breakdown.ColorBand);
    }

    [Fact]
    public void DayPartMatrix_ValidatesCanonicalCodes()
    {
        Assert.True(DayPartMatrix.IsValidDayCode("ma"));
        Assert.Equal("Ma", DayPartMatrix.NormalizeDayCode("ma"));
        Assert.False(DayPartMatrix.IsValidDayPartCode("Lunch"));
        Assert.Null(SchedulePayload.Flexible(FlexibleScheduleSource.ApiEmpty).Validate());
        Assert.NotNull(new SchedulePayload().Validate());
    }

    [Fact]
    public void YouthWageFractions_AlignAdultAndFifteen()
    {
        Assert.Equal(1.00m, YouthWageFractions.FractionForAge(21));
        Assert.Equal(0.30m, YouthWageFractions.FractionForAge(15));
        var bands = VacancyWageResolver.BuildDefaultYouthBands(10m);
        Assert.Equal(3.00m, bands.First(b => b.AgeYears == 15).HourlyRate);
    }

    [Fact]
    public void VerificationCodes_PepperedHash_DiffersFromLegacy_AndMatchesBoth()
    {
        var code = "123456";
        var peppered = VerificationCodes.Hash(code);
        var legacy = VerificationCodes.HashLegacyUnsalted(code);
        Assert.NotEqual(peppered, legacy);
        Assert.True(VerificationCodes.MatchesHash(peppered, code));
        Assert.True(VerificationCodes.MatchesHash(legacy, code));
        Assert.False(VerificationCodes.MatchesHash(peppered, "000000"));
    }
}
