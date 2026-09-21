using System.Text;

namespace Jobsy.Core.Rules;

public static class CultureFitPrompt
{
    public const string System = """
        Je bent de cultuurcoach van Lobsy. Je legt in warme Jip-en-Janneke-taal (Nederlands) uit of iemand past bij de teamdynamiek van een vacature.
        Verboden vaktermen: RIASEC, OCEAN, Holland-code, Holland code, Realistic, Investigative, Artistic, Social, Enterprising, Conventional, Big Five, DISC, extraversie, extraversion, neuroticisme, neuroticism, consciëntieusheid.
        Geen naam, e-mail, telefoon, adres of woonplaats. Geen bedrijfsnamen verzinnen.
        cultureFitPercent: 0-100, eerlijk. 75+ is hoge cultuurfit, 55-74 midden, daaronder laag.
        why: één korte zin waarom de persoon wel of niet past bij de dynamiek van het team.
        Antwoord ALLEEN als JSON-object:
        {
          "cultureFitPercent": 78,
          "why": "Sluit goed aan bij een informele, actieve werkomgeving waar proactief handelen gewaardeerd wordt."
        }
        """;

    public static string User(IReadOnlyList<string> pillarLabels, CompetencyScores scores, DiscScores? disc = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Cultuurpijlers van het team (gekozen door de werkgever):");
        foreach (var label in pillarLabels)
        {
            sb.Append("- ").AppendLine(label);
        }

        sb.AppendLine("Werkstijl van de kandidaat 0-100 (geen persoonsgegevens):");
        sb.Append("- Samenwerken: ").Append(scores.Samenwerken ?? 0).AppendLine("%");
        sb.Append("- Afmaken wat je belooft: ").Append(scores.Resultaatgerichtheid ?? 0).AppendLine("%");
        sb.Append("- Kalm blijven als het druk is: ").Append(scores.Stressbestendigheid ?? 0).AppendLine("%");
        sb.Append("- Nieuwe wegen zoeken: ").Append(scores.Innovatie ?? 0).AppendLine("%");
        if (scores.Extraversie is int extra)
        {
            sb.Append("- Energie van mensen om je heen: ").Append(extra).AppendLine("%");
        }

        if (disc is { IsComplete: true })
        {
            sb.AppendLine("Gedragsstijl in het team 0-100:");
            sb.Append("- Het voortouw nemen: ").Append(disc.Dominant ?? 0).AppendLine("%");
            sb.Append("- Mensen meenemen: ").Append(disc.Invloed ?? 0).AppendLine("%");
            sb.Append("- Rust en ritme: ").Append(disc.Stabiel ?? 0).AppendLine("%");
            sb.Append("- Nauwkeurig werken: ").Append(disc.Nauwkeurig ?? 0).AppendLine("%");
        }

        return sb.ToString();
    }
}
