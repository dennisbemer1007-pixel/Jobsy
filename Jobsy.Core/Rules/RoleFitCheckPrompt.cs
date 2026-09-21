using System.Text;

namespace Jobsy.Core.Rules;

public static class RoleFitCheckPrompt
{
    public const string System = """
        Je bent de loopbaanadviseur van Lobsy. Je legt in warme, positieve Jip-en-Janneke-taal (Nederlands) uit of een functie bij dit kandidaatprofiel past.
        Kijk VERDER dan een exacte functietitel. Weeg holistisch: (1) opleidingsachtergrond (niveau én richting), (2) competenties & drijfveren uit Wie ben ik? / werkstijl / teamgedrag, (3) overdraagbare werkervaring (transferable skills).
        Bij een bredere match (geen exacte titelhit, wel goede fit) geef je in strengths of gaps een korte onderbouwing waarom deze rol toch past.
        Verboden vaktermen: RIASEC, OCEAN, Holland-code, Holland code, Realistic, Investigative, Artistic, Social, Enterprising, Conventional, Big Five, DISC, extraversie, extraversion, neuroticisme, neuroticism, consciëntieusheid.
        Geen naam, e-mail, telefoon, adres of woonplaats van de kandidaat. Geen bedrijfsnamen verzinnen.
        Beoordeel ALGEMENE functies op de Nederlandse arbeidsmarkt. Noem Den Haag en het Westland alleen als zoekadvies op de Lobsy-banenkaart.
        matchPercent: 0-100, eerlijk. 95+ alleen bij een kernfit, 85-94 sterk, 75-84 verbreding, daaronder een mogelijke switch met duidelijk gat.
        strengths: 2 tot 4 zinnen — waar de kandidaat al aan voldoet (opleiding, competenties/drijfveren, overdraagbare ervaring, reistijd/uren).
        gaps: 2 tot 4 zinnen — wat nog ontbreekt, inclusief harde papieren eisen. Geen valse hoop.
        actionSteps: 3 tot 5 concrete groeistappen (korte cursus/omscholing of BBL om een gat te dichten, meelopen, banenkaart-filter). Als er een gat is, noem subtiel: "Een korte cursus kan dit stukje aanvullen." Geen schreeuwende marketing.
        searchKeys: 2 tot 6 korte Nederlandse zoekwoorden voor de banenkaart.
        similarRoles: 2 tot 4 ALTERNATIEVE functietitels in dezelfde richting die beter bij het huidige profiel passen (opstap/assistent als de gevraagde functie te hoog gegrepen is). Geen bedrijfsnamen. why in Jip-en-Janneke met overdraagbare skills.
        Antwoord ALLEEN als JSON-object:
        {
          "matchPercent": 81,
          "strengths": ["..."],
          "gaps": ["..."],
          "actionSteps": ["..."],
          "searchKeys": ["zorg","verpleeg"],
          "similarRoles": [{"title":"Helpende zorg","why":"Zelfde richting, minder papieren eisen.","fitPercent":88}]
        }
        """;

    public static string User(
        string jobTitle,
        CompetencyScores competencies,
        RiasecScores career,
        bool fromDeepAnalysis,
        int? maxTravelMinutes,
        string? transport,
        IReadOnlyList<string>? licenses,
        IReadOnlyList<string>? roles,
        DiscScores? disc = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Functietitel om te toetsen (geen persoonsgegevens):");
        sb.AppendLine(jobTitle);
        sb.AppendLine(fromDeepAnalysis
            ? "Bron: uitgebreide 150-vragen analyse + quick-scans. Wees preciezer in het groeistappenplan."
            : "Bron: gratis quick-scans (2× 25 vragen). Geef een betrouwbare indicatie, geen overclaim.");
        sb.AppendLine("Werkstijl 0-100:");
        sb.Append("- Samenwerken: ").Append(competencies.Samenwerken ?? 0).AppendLine("%");
        sb.Append("- Afmaken wat je belooft: ").Append(competencies.Resultaatgerichtheid ?? 0).AppendLine("%");
        sb.Append("- Kalm blijven als het druk is: ").Append(competencies.Stressbestendigheid ?? 0).AppendLine("%");
        sb.Append("- Nieuwe wegen zoeken: ").Append(competencies.Innovatie ?? 0).AppendLine("%");
        if (disc is { IsComplete: true })
        {
            sb.AppendLine("Gedrag in het team 0-100:");
            sb.Append("- Het voortouw nemen: ").Append(disc.Dominant ?? 0).AppendLine("%");
            sb.Append("- Mensen meenemen: ").Append(disc.Invloed ?? 0).AppendLine("%");
            sb.Append("- Rust en ritme: ").Append(disc.Stabiel ?? 0).AppendLine("%");
            sb.Append("- Nauwkeurig werken: ").Append(disc.Nauwkeurig ?? 0).AppendLine("%");
        }
        sb.AppendLine("Richting 0-100:");
        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            sb.Append("- ")
                .Append(CareerCompassBuilder.TypeLabel(code))
                .Append(": ")
                .Append(career.Get(code))
                .AppendLine("%");
        }

        if (maxTravelMinutes is int minutes)
        {
            sb.Append("Reistijd-radius: ").Append(minutes).AppendLine(" minuten.");
        }

        if (!string.IsNullOrWhiteSpace(transport))
        {
            sb.Append("Vervoer: ").AppendLine(transport);
        }

        if (licenses is { Count: > 0 })
        {
            sb.Append("Rijbewijs: ").AppendLine(string.Join(", ", licenses));
        }

        if (roles is { Count: > 0 })
        {
            sb.Append("Voorkeursbranches: ").AppendLine(string.Join(", ", roles));
        }

        return sb.ToString();
    }
}
