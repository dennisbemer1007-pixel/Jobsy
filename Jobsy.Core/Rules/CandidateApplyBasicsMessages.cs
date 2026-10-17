namespace Jobsy.Core.Rules;

/// <summary>NL API messages for apply-basics validation (UI uses Culture keys).</summary>
public static class CandidateApplyBasicsMessages
{
    public static string NlMessage(ApplyBasicsGap gap)
        => gap.MessageKey switch
        {
            "Apply.Gap.Name" => "Vul je voor- en achternaam in.",
            "Apply.Gap.BirthDate" => "Vul je geboortedatum in.",
            "Apply.Gap.Location" => "Vul je woonplaats of adres in (Even kennismaken).",
            "Apply.Gap.Hours" => "Geef aan hoeveel uur per week je wilt werken.",
            "Apply.Gap.Availability" => "Geef aan wanneer je kunt werken (dagen en dagdelen).",
            "Apply.Gap.AvailabilityStart" => "Kies vanaf wanneer je kunt starten.",
            "Apply.Gap.WorkStatus" => "Kies je huidige werkstatus.",
            "Apply.Gap.OpenForWork" => "Zet 'Beschikbaar voor werk' aan in je paspoort.",
            "Apply.Gap.PassportConsent" => "Accepteer de voorwaarden om je paspoort te gebruiken.",
            _ => "Vul eerst je basisprofiel aan in je paspoort."
        };

    public static string NlSummary(IReadOnlyList<ApplyBasicsGap> gaps)
    {
        if (gaps.Count == 0)
        {
            return "";
        }

        return string.Join(" ", gaps.Select(g => NlMessage(g)));
    }
}
