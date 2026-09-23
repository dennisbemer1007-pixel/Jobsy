using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>Maps vacancy + candidate preference data into match-score inputs.</summary>
public static class MatchingProfileMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static HoursRange? TryVacancyHours(Vacancy vacancy)
    {
        if (vacancy.MinHoursPerWeek is null || vacancy.MaxHoursPerWeek is null)
        {
            return null;
        }

        var range = new HoursRange(vacancy.MinHoursPerWeek.Value, vacancy.MaxHoursPerWeek.Value);
        return range.Validate() is null ? range : null;
    }

    public static HoursRange? TryCandidateHours(CandidatePreferencesDto prefs)
    {
        if (prefs.MinHoursPerWeek is null || prefs.MaxHoursPerWeek is null)
        {
            return null;
        }

        var range = new HoursRange(prefs.MinHoursPerWeek.Value, prefs.MaxHoursPerWeek.Value);
        return range.Validate() is null ? range : null;
    }

    public static SchedulePayload? TryVacancySchedule(Vacancy vacancy)
    {
        if (vacancy.FlexibleTimes)
        {
            FlexibleScheduleSource? source = null;
            if (Enum.TryParse<FlexibleScheduleSource>(vacancy.FlexibleScheduleSource, true, out var parsed))
            {
                source = parsed;
            }

            return SchedulePayload.Flexible(source ?? FlexibleScheduleSource.Manual);
        }

        if (string.IsNullOrWhiteSpace(vacancy.ScheduleJson))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SchedulePayload>(vacancy.ScheduleJson, JsonOptions);
            return payload?.Normalize();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static SchedulePayload? TryCandidateSchedule(CandidatePreferencesDto prefs)
    {
        if (prefs.FlexibleTimes == true)
        {
            return SchedulePayload.Flexible(FlexibleScheduleSource.Manual);
        }

        if (prefs.Availability is null || prefs.Availability.Count == 0)
        {
            return null;
        }

        var payload = new SchedulePayload { FlexibleTimes = false };
        foreach (var (day, parts) in prefs.Availability)
        {
            if (parts is { Length: > 0 })
            {
                payload.Slots[day] = parts.ToList();
            }
        }

        return payload.Normalize();
    }

    public static LegalTaskFlags? TryLegalFlags(Vacancy vacancy)
    {
        // Incomplete legal answers → treat as unset (browse OK; publish should block separately).
        if (vacancy.LegalWorksAfter19 is null
            || vacancy.LegalNightShift23To06 is null
            || vacancy.LegalAdultSupervisorPresent is null
            || vacancy.LegalHandlesMoneyOrClosing is null
            || vacancy.LegalHeavyOrHazardousWork is null)
        {
            return null;
        }

        return new LegalTaskFlags
        {
            WorksAfter19 = vacancy.LegalWorksAfter19.Value,
            NightShift23To06 = vacancy.LegalNightShift23To06.Value,
            AdultSupervisorPresent = vacancy.LegalAdultSupervisorPresent.Value,
            HandlesMoneyOrClosing = vacancy.LegalHandlesMoneyOrClosing.Value,
            HeavyOrHazardousWork = vacancy.LegalHeavyOrHazardousWork.Value
        };
    }

    public static MatchScoreInput BuildInput(
        Vacancy vacancy,
        CandidatePreferencesDto prefs,
        int? estimatedTravelMinutes,
        int? candidateAgeYears)
        => new()
        {
            EstimatedTravelMinutes = estimatedTravelMinutes,
            MaxTravelMinutes = prefs.MaxTravelMinutes,
            CandidateHours = TryCandidateHours(prefs),
            VacancyHours = TryVacancyHours(vacancy),
            CandidateSchedule = TryCandidateSchedule(prefs),
            VacancySchedule = TryVacancySchedule(vacancy),
            CandidateAgeYears = candidateAgeYears,
            LegalFlags = TryLegalFlags(vacancy)
        };

    public static MatchScoreInput BuildInput(
        VacancyDiscoveryRecord vacancy,
        CandidatePreferencesDto prefs,
        int? estimatedTravelMinutes,
        int? candidateAgeYears)
        => new()
        {
            EstimatedTravelMinutes = estimatedTravelMinutes,
            MaxTravelMinutes = prefs.MaxTravelMinutes,
            CandidateHours = TryCandidateHours(prefs),
            VacancyHours = TryHours(vacancy.MinHoursPerWeek, vacancy.MaxHoursPerWeek),
            CandidateSchedule = TryCandidateSchedule(prefs),
            VacancySchedule = TrySchedule(vacancy.FlexibleTimes, vacancy.ScheduleJson),
            CandidateAgeYears = candidateAgeYears,
            LegalFlags = TryLegalFlags(
                vacancy.LegalWorksAfter19,
                vacancy.LegalNightShift23To06,
                vacancy.LegalAdultSupervisorPresent,
                vacancy.LegalHandlesMoneyOrClosing,
                vacancy.LegalHeavyOrHazardousWork)
        };

    public static HoursRange? TryHours(decimal? min, decimal? max)
    {
        if (min is null || max is null)
        {
            return null;
        }

        var range = new HoursRange(min.Value, max.Value);
        return range.Validate() is null ? range : null;
    }

    public static SchedulePayload? TrySchedule(bool flexibleTimes, string? scheduleJson)
    {
        if (flexibleTimes)
        {
            return SchedulePayload.Flexible(FlexibleScheduleSource.Manual);
        }

        if (string.IsNullOrWhiteSpace(scheduleJson))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SchedulePayload>(scheduleJson, JsonOptions);
            return payload?.Normalize();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static CandidatePreferencesDto DeserializePrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }

    public static LegalTaskFlags? TryLegalFlags(
        bool? worksAfter19,
        bool? nightShift,
        bool? supervisorPresent,
        bool? handlesMoney,
        bool? heavyWork)
    {
        if (worksAfter19 is null
            || nightShift is null
            || supervisorPresent is null
            || handlesMoney is null
            || heavyWork is null)
        {
            return null;
        }

        return new LegalTaskFlags
        {
            WorksAfter19 = worksAfter19.Value,
            NightShift23To06 = nightShift.Value,
            AdultSupervisorPresent = supervisorPresent.Value,
            HandlesMoneyOrClosing = handlesMoney.Value,
            HeavyOrHazardousWork = heavyWork.Value
        };
    }
}
