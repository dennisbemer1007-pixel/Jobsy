namespace Jobsy.Core.Rules;

/// <summary>Match score weights (MVP defaults from functional matching specs).</summary>
public static class MatchScoreWeights
{
    public const int Travel = 40;
    public const int Hours = 30;
    public const int DayParts = 30;
    public const int GuldenMiddenwegThreshold = 50;
    public const int StrongMatchThreshold = 70;
}

public sealed class MatchScoreInput
{
    public int? EstimatedTravelMinutes { get; init; }
    public int? MaxTravelMinutes { get; init; }
    public HoursRange? CandidateHours { get; init; }
    public HoursRange? VacancyHours { get; init; }
    public SchedulePayload? CandidateSchedule { get; init; }
    public SchedulePayload? VacancySchedule { get; init; }
    public int? CandidateAgeYears { get; init; }
    public LegalTaskFlags? LegalFlags { get; init; }
}

public sealed class MatchScoreBreakdown
{
    public int TotalPercent { get; init; }
    public int TravelScore { get; init; }
    public int HoursScore { get; init; }
    public int DayPartsScore { get; init; }
    public int? TravelMinutesEstimated { get; init; }
    public bool? TravelWithinPreference { get; init; }
    public decimal? HoursOverlapHours { get; init; }
    public decimal? HoursCandidateMin { get; init; }
    public decimal? HoursCandidateMax { get; init; }
    public decimal? HoursVacancyMin { get; init; }
    public decimal? HoursVacancyMax { get; init; }
    public IReadOnlyList<string> DayPartsMatched { get; init; } = [];
    public IReadOnlyList<string> DayPartsMissing { get; init; } = [];
    public bool DayPartsNeutral { get; init; }
    public bool LegalEligible { get; init; } = true;
    public bool LegalAgeKnown { get; init; }
    public IReadOnlyList<string> LegalBlockReasons { get; init; } = [];
    public IReadOnlyList<string> Advice { get; init; } = [];
    public bool ViaSafetyNet { get; init; }

    public string ColorBand => TotalPercent >= MatchScoreWeights.StrongMatchThreshold
        ? "green"
        : TotalPercent >= MatchScoreWeights.GuldenMiddenwegThreshold
            ? "orange"
            : "red";
}

public static class MatchScoreCalculator
{
    public static MatchScoreBreakdown Calculate(MatchScoreInput input)
    {
        var advice = new List<string>();

        var (travelPoints, travelWithin) = ScoreTravel(
            input.EstimatedTravelMinutes,
            input.MaxTravelMinutes,
            advice);

        var (hoursPoints, overlapHours) = ScoreHours(
            input.CandidateHours,
            input.VacancyHours,
            advice);

        var (dayPartsPoints, matched, missing, neutral) = ScoreDayParts(
            input.CandidateSchedule,
            input.VacancySchedule,
            advice);

        var legal = YouthLaborRules.Evaluate(input.CandidateAgeYears, input.LegalFlags);
        if (!legal.IsEligible)
        {
            advice.Add("Deze taken zijn wettelijk niet toegestaan op jouw leeftijd.");
        }
        else if (!legal.AgeKnown)
        {
            advice.Add("Vul je geboortedatum in voor wettelijke taakcontrole en loonzichtbaarheid.");
        }

        if (input.CandidateHours is null && input.VacancyHours is not null)
        {
            advice.Add("Maak je urenvoorkeur compleet voor een betere matchscore.");
        }

        if (input.EstimatedTravelMinutes is null || input.MaxTravelMinutes is null)
        {
            advice.Add("Deel je locatie en max. reistijd voor een betere reistijdscore.");
        }

        var total = travelPoints + hoursPoints + dayPartsPoints;
        return new MatchScoreBreakdown
        {
            TotalPercent = total,
            TravelScore = travelPoints,
            HoursScore = hoursPoints,
            DayPartsScore = dayPartsPoints,
            TravelMinutesEstimated = input.EstimatedTravelMinutes,
            TravelWithinPreference = travelWithin,
            HoursOverlapHours = overlapHours,
            HoursCandidateMin = input.CandidateHours?.MinHoursPerWeek,
            HoursCandidateMax = input.CandidateHours?.MaxHoursPerWeek,
            HoursVacancyMin = input.VacancyHours?.MinHoursPerWeek,
            HoursVacancyMax = input.VacancyHours?.MaxHoursPerWeek,
            DayPartsMatched = matched,
            DayPartsMissing = missing,
            DayPartsNeutral = neutral,
            LegalEligible = legal.IsEligible,
            LegalAgeKnown = legal.AgeKnown,
            LegalBlockReasons = legal.BlockReasons,
            Advice = advice
        };
    }

    private static (int Points, bool? Within) ScoreTravel(
        int? estimatedMinutes,
        int? maxMinutes,
        List<string> advice)
    {
        if (estimatedMinutes is null || maxMinutes is null or <= 0)
        {
            // Neutral travel contribution when signals missing.
            return (MatchScoreWeights.Travel, null);
        }

        var within = estimatedMinutes.Value <= maxMinutes.Value;
        if (!within)
        {
            advice.Add($"Verhoog je max. reistijd (nu {maxMinutes} min; geschat {estimatedMinutes} min).");
            var ratio = Math.Clamp((double)maxMinutes.Value / estimatedMinutes.Value, 0, 1);
            return ((int)Math.Round(MatchScoreWeights.Travel * ratio), false);
        }

        var headroom = 1.0 - Math.Clamp((double)estimatedMinutes.Value / maxMinutes.Value, 0, 1);
        var points = (int)Math.Round(MatchScoreWeights.Travel * (0.7 + 0.3 * headroom));
        return (Math.Clamp(points, 0, MatchScoreWeights.Travel), true);
    }

    private static (int Points, decimal? Overlap) ScoreHours(
        HoursRange? candidate,
        HoursRange? vacancy,
        List<string> advice)
    {
        if (vacancy is null)
        {
            return (MatchScoreWeights.Hours, null);
        }

        if (candidate is null)
        {
            return ((int)Math.Round(MatchScoreWeights.Hours * 0.5m), null);
        }

        var overlap = HoursRangeRules.OverlapHours(candidate.Value, vacancy.Value);
        var score01 = HoursRangeRules.OverlapScore01(candidate.Value, vacancy.Value);
        if (score01 <= 0)
        {
            advice.Add(
                $"Pas je uren aan (jij: {candidate.Value.MinHoursPerWeek}–{candidate.Value.MaxHoursPerWeek} u/w · vacature: {vacancy.Value.MinHoursPerWeek}–{vacancy.Value.MaxHoursPerWeek} u/w).");
        }
        else if (candidate.Value.MaxHoursPerWeek < vacancy.Value.MinHoursPerWeek)
        {
            advice.Add($"Verhoog je max. uren naar minstens {vacancy.Value.MinHoursPerWeek}.");
        }

        return ((int)Math.Round(MatchScoreWeights.Hours * score01), overlap);
    }

    private static (int Points, IReadOnlyList<string> Matched, IReadOnlyList<string> Missing, bool Neutral)
        ScoreDayParts(
            SchedulePayload? candidate,
            SchedulePayload? vacancy,
            List<string> advice)
    {
        if (vacancy is null || vacancy.FlexibleTimes)
        {
            return (MatchScoreWeights.DayParts, [], [], true);
        }

        var required = Flatten(vacancy);
        if (required.Count == 0)
        {
            return (MatchScoreWeights.DayParts, [], [], true);
        }

        if (candidate is null)
        {
            advice.Add("Vul je beschikbaarheidsmatrix in voor een betere dagdelen-score.");
            return ((int)Math.Round(MatchScoreWeights.DayParts * 0.5m), [], required, false);
        }

        if (candidate.FlexibleTimes)
        {
            // Candidate flexible: partial credit — employer asked specific slots.
            return ((int)Math.Round(MatchScoreWeights.DayParts * 0.7m), [], required, false);
        }

        var available = new HashSet<string>(Flatten(candidate), StringComparer.OrdinalIgnoreCase);
        var matched = required.Where(available.Contains).ToList();
        var missing = required.Where(r => !available.Contains(r)).ToList();
        foreach (var miss in missing.Take(3))
        {
            advice.Add($"Zet {miss.Replace(':', ' ')} aan in je beschikbaarheid.");
        }

        var score01 = (decimal)matched.Count / required.Count;
        return ((int)Math.Round(MatchScoreWeights.DayParts * score01), matched, missing, false);
    }

    private static List<string> Flatten(SchedulePayload schedule)
    {
        var list = new List<string>();
        foreach (var day in DayPartMatrix.DayCodes)
        {
            if (!schedule.Slots.TryGetValue(day, out var parts) || parts is null)
            {
                continue;
            }

            foreach (var part in parts.Where(DayPartMatrix.IsValidDayPartCode))
            {
                list.Add($"{day}:{DayPartMatrix.NormalizeDayPartCode(part)}");
            }
        }

        return list;
    }
}

public static class GuldenMiddenwegRules
{
    public static bool RequiresSafetyNetConfirmation(MatchScoreBreakdown breakdown)
        => breakdown.LegalEligible && breakdown.TotalPercent < MatchScoreWeights.GuldenMiddenwegThreshold;

    public static bool CanProceedWithoutSafetyNet(MatchScoreBreakdown breakdown)
        => breakdown.LegalEligible && breakdown.TotalPercent >= MatchScoreWeights.GuldenMiddenwegThreshold;
}
