namespace Jobsy.Core.Rules;

public static class VacancyEngagementReminderRules
{
    /// <summary>Days a vacancy must be Active before the engagement reminder is sent.</summary>
    public const int OpenDaysBeforeReminder = 14;

    /// <summary>Goodwill EndDate extension after the entrepreneur edits before the deadline.</summary>
    public const int GoodwillExtendDays = 7;

    public static bool IsEligibleForReminder(
        DateTime? publishedAtUtc,
        DateTime? reminderSentAtUtc,
        DateTime utcNow)
    {
        if (reminderSentAtUtc is not null || publishedAtUtc is null)
        {
            return false;
        }

        return publishedAtUtc.Value <= utcNow.AddDays(-OpenDaysBeforeReminder);
    }

    public static bool CanApplyGoodwillExtension(
        DateTime? reminderSentAtUtc,
        DateTime? goodwillExtendedAtUtc,
        DateOnly endDate,
        DateOnly today)
        => reminderSentAtUtc is not null
           && goodwillExtendedAtUtc is null
           && today <= endDate;

    public static string BuildHeuristicTip(
        int searchAppearances,
        int views,
        int shares,
        int saved,
        int applications)
    {
        if (applications == 0 && views < 5)
        {
            return "Je vacature wordt nog weinig gezien. Verrijk de titel met concrete taken of locatie, en overweeg Highlight of PushBom voor meer bereik.";
        }

        if (applications == 0 && views >= 5)
        {
            return "Er is interesse (bekeken), maar nog geen sollicitaties. Maak eisen realistischer, verduidelijk het uurloon/rooster, of verkort de reistijd-verwachting in de tekst.";
        }

        if (applications > 0 && applications < 3 && views > 20)
        {
            return "Veel bekeken, beperkt gesolliciteerd. Scherp de unieke voordelen aan (werktijden, team, doorgroeikansen) en check of harde eisen te streng zijn.";
        }

        if (shares == 0 && saved == 0)
        {
            return "Deel de vacature actief (social/WhatsApp) en vraag collega’s om te bewaren — dat vergroot herhaald bezoek.";
        }

        if (searchAppearances > 50 && views < 10)
        {
            return "Je komt vaak in zoekresultaten, maar weinig klikken. Maak titel en eerste zin concreter zodat kandidaten sneller doorklikken.";
        }

        return "Controleer of titel, taken en rooster nog kloppen. Kleine tekstuele updates verbeteren relevantie in matching en zoekfilters.";
    }
}
