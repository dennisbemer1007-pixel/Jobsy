using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Location + hours teaser for onboarding moment A (no personality scores).
/// </summary>
public static class OnboardingEarlyNearbySignal
{
    public sealed record Card(
        Guid VacancyId,
        string Title,
        double DistanceKm,
        int TravelMinutes,
        string HoursLabel,
        string ScheduleHint,
        string WhyLine,
        int LogisticsScore);

    public sealed record Result(
        int Count,
        int? WiderCount,
        int MaxTravelMinutesUsed,
        IReadOnlyList<Card> Examples,
        bool MissingHomeLocation);

    public static Result Compute(
        double? homeLat,
        double? homeLng,
        CandidatePreferencesDto prefs,
        IEnumerable<VacancyDiscoveryRecord> vacancies,
        int? candidateAgeYears,
        int exampleLimit = 3)
    {
        if (homeLat is not double lat || homeLng is not double lng)
        {
            return new Result(0, null, prefs.MaxTravelMinutes ?? 30, [], true);
        }

        var maxTravel = prefs.MaxTravelMinutes is > 0 and <= 180 ? prefs.MaxTravelMinutes.Value : 30;
        var transport = TravelReach.Fastest(TransportLabels.ParseMany(prefs.PreferredTransport));
        var educations = prefs.Educations ?? [];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var scored = new List<Card>();
        foreach (var vacancy in vacancies)
        {
            if (!PublicVisibility.IsVacancyPublic(vacancy, today))
            {
                continue;
            }

            var estimate = TravelReach.Estimate(lat, lng, vacancy.Latitude, vacancy.Longitude, transport);
            if (!MatchVacancyRelevance.IsRelevant(
                    educations,
                    maxTravel,
                    prefs.PreferredTransport,
                    vacancy.RequiredEducation,
                    estimate.TravelMinutes,
                    vacancy.RequiredTransportLabels))
            {
                continue;
            }

            var breakdown = MatchScoreCalculator.Calculate(
                MatchingProfileMapper.BuildInput(vacancy, prefs, estimate.TravelMinutes, candidateAgeYears));

            if (breakdown.LegalEligible == false)
            {
                continue;
            }

            if (breakdown.TravelWithinPreference != true)
            {
                continue;
            }

            if (breakdown.HoursOverlapHours is <= 0 && breakdown.HoursVacancyMin is not null)
            {
                continue;
            }

            var logistics = breakdown.TravelScore + breakdown.HoursScore
                            + (breakdown.DayPartsNeutral ? breakdown.DayPartsScore / 2 : breakdown.DayPartsScore);

            scored.Add(new Card(
                vacancy.Id,
                vacancy.Title,
                estimate.DistanceKm,
                estimate.TravelMinutes,
                ProfileVacancyMatchCalculator.FormatHoursRange(
                    vacancy.MinHoursPerWeek,
                    vacancy.MaxHoursPerWeek),
                ScheduleHint(vacancy),
                WhyLine(breakdown, vacancy.CompanyAddress),
                logistics));
        }

        scored.Sort((a, b) =>
        {
            var byScore = b.LogisticsScore.CompareTo(a.LogisticsScore);
            return byScore != 0 ? byScore : a.TravelMinutes.CompareTo(b.TravelMinutes);
        });

        var count = scored.Count;
        int? wider = null;
        if (count == 0 && maxTravel < 60)
        {
            var widerMax = Math.Min(maxTravel + 15, 60);
            wider = CountWithinTravel(lat, lng, prefs, vacancies, candidateAgeYears, widerMax, transport, educations);
        }

        var examples = scored.Take(Math.Max(0, exampleLimit)).ToList();
        return new Result(count, wider, maxTravel, examples, false);
    }

    private static int CountWithinTravel(
        double lat,
        double lng,
        CandidatePreferencesDto prefs,
        IEnumerable<VacancyDiscoveryRecord> vacancies,
        int? candidateAgeYears,
        int maxTravel,
        TransportMode transport,
        IEnumerable<string> educations)
    {
        var n = 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var vacancy in vacancies)
        {
            if (!PublicVisibility.IsVacancyPublic(vacancy, today))
            {
                continue;
            }

            var estimate = TravelReach.Estimate(lat, lng, vacancy.Latitude, vacancy.Longitude, transport);
            if (!MatchVacancyRelevance.IsRelevant(
                    educations,
                    maxTravel,
                    prefs.PreferredTransport,
                    vacancy.RequiredEducation,
                    estimate.TravelMinutes,
                    vacancy.RequiredTransportLabels))
            {
                continue;
            }

            var breakdown = MatchScoreCalculator.Calculate(
                MatchingProfileMapper.BuildInput(vacancy, prefs, estimate.TravelMinutes, candidateAgeYears));
            if (breakdown.LegalEligible && breakdown.TravelWithinPreference == true)
            {
                n++;
            }
        }

        return n;
    }

    private static string ScheduleHint(VacancyDiscoveryRecord vacancy)
    {
        if (vacancy.FlexibleTimes)
        {
            return "flexibel";
        }

        var schedule = MatchingProfileMapper.TrySchedule(vacancy.FlexibleTimes, vacancy.ScheduleJson);
        if (schedule is null || schedule.FlexibleTimes)
        {
            return "doordeweeks";
        }

        var parts = schedule.Slots.Values.SelectMany(v => v).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
        if (parts.Count == 0)
        {
            return "doordeweeks";
        }

        return string.Join(" & ", parts.Select(PrettyPart));
    }

    private static string PrettyPart(string code) => code.ToLowerInvariant() switch
    {
        "ochtend" => "ochtend",
        "middag" => "middag",
        "avond" => "avond",
        "nacht" => "nacht",
        _ => code
    };

    private static string WhyLine(MatchScoreBreakdown breakdown, string companyAddress)
    {
        if (breakdown.HoursOverlapHours is > 0 && breakdown.TravelWithinPreference == true)
        {
            return "Past bij je reistijd en uren";
        }

        if (breakdown.TravelWithinPreference == true)
        {
            return "Binnen je max. reistijd";
        }

        var city = ExtractCity(companyAddress);
        return string.IsNullOrWhiteSpace(city)
            ? "Dichtbij op basis van je locatie"
            : $"In de buurt van {city}";
    }

    private static string? ExtractCity(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : null;
    }
}
