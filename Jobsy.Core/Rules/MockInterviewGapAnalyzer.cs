using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

/// <summary>
/// Soft coaching gaps between a vacancy and a candidate profile for the practice interview bot.
/// Not hard apply gates — phrased as practice questions.
/// </summary>
public static class MockInterviewGapAnalyzer
{
    public static MockInterviewCandidateContext BuildCandidateContext(
        CandidatePreferencesDto prefs,
        Vacancy vacancy,
        MatchScoreBreakdown? match = null)
    {
        var employers = (prefs.Employers ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
            .Select(e =>
            {
                var role = string.IsNullOrWhiteSpace(e.Role) ? null : e.Role.Trim();
                return role is null ? e.EmployerName.Trim() : $"{e.EmployerName.Trim()} ({role})";
            })
            .Take(5)
            .ToList();

        string? hoursSummary = null;
        if (prefs.MinHoursPerWeek is not null && prefs.MaxHoursPerWeek is not null)
        {
            hoursSummary = $"{prefs.MinHoursPerWeek:0.#}–{prefs.MaxHoursPerWeek:0.#} u/w";
        }

        var gaps = BuildGaps(prefs, vacancy, match, employers.Count);
        return new MockInterviewCandidateContext(
            string.IsNullOrWhiteSpace(prefs.AboutMe) ? null : prefs.AboutMe.Trim(),
            prefs.Educations?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList() ?? [],
            prefs.DrivingLicenses?.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList() ?? [],
            employers,
            hoursSummary,
            gaps);
    }

    public static IReadOnlyList<MockInterviewGap> BuildGaps(
        CandidatePreferencesDto prefs,
        Vacancy vacancy,
        MatchScoreBreakdown? match,
        int? employerCount = null)
    {
        var gaps = new List<MockInterviewGap>();
        var count = employerCount ?? prefs.Employers?.Count ?? 0;

        if (!string.IsNullOrWhiteSpace(vacancy.RequiredDrivingLicense)
            && !DrivingLicenseLabels.CandidateMeetsRequirement(prefs.DrivingLicenses, vacancy.RequiredDrivingLicense))
        {
            var req = vacancy.RequiredDrivingLicense.Trim();
            gaps.Add(new MockInterviewGap(
                "license",
                $"Vacature vraagt rijbewijs {req}; dat staat niet (volledig) in je profiel.",
                $"De vacature vraagt rijbewijs {req}. Hoe zit dat bij jou — heb je het, ben je het aan het behalen, of hoe zou je dit oplossen?",
                $"The vacancy asks for driving license {req}. How does that work for you — do you have it, are you getting it, or how would you handle this?"));
        }

        if (!string.IsNullOrWhiteSpace(vacancy.RequiredEducation)
            && !EducationLevelLabels.CandidateMeetsRequirement(prefs.Educations, vacancy.RequiredEducation))
        {
            var req = vacancy.RequiredEducation.Trim();
            gaps.Add(new MockInterviewGap(
                "education",
                $"Vacature vraagt opleidingsniveau {req}; dat matcht niet met je profiel.",
                $"In de vacature staat opleidingsniveau {req}. Wat is jouw opleiding of leerweg, en waarom denk jij toch te passen?",
                $"The vacancy asks for education level {req}. What is your education or path, and why do you still think you fit?"));
        }

        if (vacancy.MinimumEmployers is > 0 && count < vacancy.MinimumEmployers.Value)
        {
            var need = vacancy.MinimumEmployers.Value;
            gaps.Add(new MockInterviewGap(
                "employers",
                $"Vacature vraagt minstens {need} eerdere werkgever(s); in je profiel staan er {count}.",
                $"De vacature vraagt ervaring bij minstens {need} werkgever(s). Welke ervaring (bijbaan, stage, schoolproject, vrijwilligerswerk) kun je hier laten zien?",
                $"The vacancy asks for experience with at least {need} employer(s). Which experience (side job, internship, school project, volunteering) can you show here?"));
        }

        var vacHours = MatchingProfileMapper.TryVacancyHours(vacancy);
        var candHours = MatchingProfileMapper.TryCandidateHours(prefs);
        if (vacHours is not null && candHours is not null)
        {
            var overlap = HoursRangeRules.OverlapHours(candHours.Value, vacHours.Value);
            if (overlap <= 0)
            {
                gaps.Add(new MockInterviewGap(
                    "hours",
                    $"Uren lijken niet te overlappen (jij {candHours.Value.MinHoursPerWeek:0.#}–{candHours.Value.MaxHoursPerWeek:0.#} u/w · vacature {vacHours.Value.MinHoursPerWeek:0.#}–{vacHours.Value.MaxHoursPerWeek:0.#} u/w).",
                    $"De vacature zoekt ongeveer {vacHours.Value.MinHoursPerWeek:0.#}–{vacHours.Value.MaxHoursPerWeek:0.#} uur per week, terwijl jij {candHours.Value.MinHoursPerWeek:0.#}–{candHours.Value.MaxHoursPerWeek:0.#} uur noemt. Hoe zou je dat combineren of aanpassen?",
                    $"The vacancy seeks about {vacHours.Value.MinHoursPerWeek:0.#}–{vacHours.Value.MaxHoursPerWeek:0.#} hours/week, while you list {candHours.Value.MinHoursPerWeek:0.#}–{candHours.Value.MaxHoursPerWeek:0.#}. How would you combine or adjust that?"));
            }
        }
        else if (vacHours is not null && candHours is null)
        {
            gaps.Add(new MockInterviewGap(
                "hours-unknown",
                $"Vacature noemt {vacHours.Value.MinHoursPerWeek:0.#}–{vacHours.Value.MaxHoursPerWeek:0.#} u/w; jouw urenvoorkeur ontbreekt in het profiel.",
                $"In de vacature staan ongeveer {vacHours.Value.MinHoursPerWeek:0.#}–{vacHours.Value.MaxHoursPerWeek:0.#} uur per week. Past dat bij jou, en hoe ziet jouw ideale week eruit?",
                $"The vacancy lists about {vacHours.Value.MinHoursPerWeek:0.#}–{vacHours.Value.MaxHoursPerWeek:0.#} hours/week. Does that fit you, and what does your ideal week look like?"));
        }

        if (match?.DayPartsMissing is { Count: > 0 } missing)
        {
            var sample = string.Join(", ", missing.Take(2).Select(PrettyDayPart));
            gaps.Add(new MockInterviewGap(
                "schedule",
                $"Beschikbaarheid mist dagdelen die de vacature vraagt ({sample}).",
                $"De vacature vraagt onder meer om {sample}. Past dat in jouw agenda, of hoe zou je dat oplossen?",
                $"The vacancy also asks for {sample}. Does that fit your schedule, or how would you handle it?"));
        }

        if (string.IsNullOrWhiteSpace(prefs.AboutMe)
            && LooksExperienceHeavy(vacancy))
        {
            gaps.Add(new MockInterviewGap(
                "about-me",
                "Je 'Over mij' is nog leeg, terwijl de vacature ervaring of zelfstandigheid noemt.",
                "Je profiel heeft nog geen kort 'Over mij'-verhaal. In één of twee zinnen: wat mag de werkgever weten over jou dat past bij deze vacature?",
                "Your profile has no short About me yet. In one or two sentences: what should the employer know about you that fits this vacancy?"));
        }

        return gaps.Take(4).ToList();
    }

    private static bool LooksExperienceHeavy(Vacancy vacancy)
    {
        var hay = $"{vacancy.Title} {vacancy.Description} {vacancy.RequiredEducation}".ToLowerInvariant();
        return hay.Contains("ervaring", StringComparison.Ordinal)
               || hay.Contains("ervaren", StringComparison.Ordinal)
               || hay.Contains("zelfstandig", StringComparison.Ordinal)
               || hay.Contains("verantwoordelijk", StringComparison.Ordinal)
               || vacancy.MinimumEmployers is > 0;
    }

    private static string PrettyDayPart(string code)
    {
        var parts = code.Split(':', 2);
        if (parts.Length != 2)
        {
            return code.Replace(':', ' ');
        }

        var day = parts[0].ToUpperInvariant() switch
        {
            "MA" => "maandag",
            "DI" => "dinsdag",
            "WO" => "woensdag",
            "DO" => "donderdag",
            "VR" => "vrijdag",
            "ZA" => "zaterdag",
            "ZO" => "zondag",
            _ => parts[0]
        };
        var part = parts[1].ToUpperInvariant() switch
        {
            "OCHTEND" or "MORNING" => "ochtend",
            "MIDDAG" or "AFTERNOON" => "middag",
            "AVOND" or "EVENING" => "avond",
            _ => parts[1].ToLowerInvariant()
        };
        return $"{day} {part}";
    }
}
