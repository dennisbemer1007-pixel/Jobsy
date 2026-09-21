using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>OpenAI prompt for a first-person "Wie ben ik?" story. No name, e-mail, or test jargon.</summary>
public static class WhoAmIPrompt
{
    public const string System = """
        Je bent de loopbaanverteller van Lobsy. Je schrijft één vloeiend, inspirerend persoonlijk verhaal in de ik-vorm (Nederlands, Jip-en-Janneke).
        Verboden vaktermen: RIASEC, OCEAN, Holland-code, Holland code, Realistic, Investigative, Artistic, Social, Enterprising, Conventional, Big Five, extraversie, extraversion, neuroticisme, neuroticism, consciëntieusheid, DISC.
        Geen naam, e-mail, telefoon, adres of woonplaats van de kandidaat. Geen bedrijfsnamen.
        Vertel wie ik ben, wat mij drijft, hoe ik in een team communiceer (voortouw / mensen meenemen / rust en ritme / nauwkeurig werken) en welke talenten uit de competenties naar voren komen.
        Geen opsomming met bullets. 2 tot 4 alinea's, warm en concreet, gericht op werk in Den Haag / het Westland.
        Antwoord ALLEEN als JSON-object: { "story": "lopende tekst in ik-vorm", "keywords": ["kort kernwoord","..."] }
        keywords: 4 tot 8 korte Nederlandse kernwoorden of sterke punten, zonder vaktermen.
        """;

    public static string User(
        CompetencyScores competency,
        RiasecScores career,
        DiscScores disc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Scores 0-100. Geen naam of e-mail. Schrijf het verhaal alsof ik het zelf vertel.");
        sb.AppendLine("Competenties:");
        foreach (var code in CompetencyTestCatalog.CategoryCodes)
        {
            sb.Append("- ").Append(WhoAmIKeywords.EverydayCompetency(code)).Append(": ").Append(competency.Get(code)).AppendLine("%");
        }

        sb.AppendLine("Gedrag in het team:");
        foreach (var code in DiscTestCatalog.CategoryCodes)
        {
            sb.Append("- ").Append(DiscTestCatalog.EverydayLabel(code)).Append(": ").Append(disc.Get(code)).AppendLine("%");
        }

        sb.AppendLine("Wat mij trekt in werk:");
        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            sb.Append("- ").Append(CareerCompassBuilder.TypeLabel(code)).Append(": ").Append(career.Get(code)).AppendLine("%");
        }

        return sb.ToString();
    }
}
