using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Four-step gate for the "Wie ben ik?" report: profile, competence, careers, DISC Quick-Scan or deep.
/// </summary>
public static class WhoAmICompleteness
{
    public static bool IsProfileFilled(string? fullName, CandidatePreferencesDto? prefs)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return false;
        }

        if (prefs is null
            || prefs.MaxTravelMinutes is not > 0
            || string.IsNullOrWhiteSpace(prefs.PreferredTransport))
        {
            return false;
        }

        return HasBackground(prefs);
    }

    public static bool HasBackground(CandidatePreferencesDto prefs)
        => !string.IsNullOrWhiteSpace(prefs.AboutMe)
           || (prefs.Employers is { Count: > 0 })
           || (prefs.Educations is { Count: > 0 });

    public static bool IsUnlocked(
        bool profileFilled,
        bool competencyCompleted,
        bool careerCompleted,
        bool discCompleted)
        => profileFilled && competencyCompleted && careerCompleted && discCompleted;

    public static string Fingerprint(CompetencyScores competency, RiasecScores career, DiscScores disc)
        => string.Join('|',
            competency.Samenwerken,
            competency.Resultaatgerichtheid,
            competency.Stressbestendigheid,
            competency.Innovatie,
            competency.Extraversie,
            career.Realistic,
            career.Investigative,
            career.Artistic,
            career.Social,
            career.Enterprising,
            career.Conventional,
            disc.Dominant,
            disc.Invloed,
            disc.Stabiel,
            disc.Nauwkeurig);
}
