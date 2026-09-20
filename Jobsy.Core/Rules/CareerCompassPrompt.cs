using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Structured OpenAI prompt for general Dutch-labour-market occupations.</summary>
public static class CareerCompassPrompt
{
    public const string System = """
        Je bent een loopbaanadviseur voor Lobsy. Schrijf in warme, glasheldere Jip-en-Janneke-taal (Nederlands).
        Geen psychologische of wetenschappelijke vaktermen (geen RIASEC, OCEAN, Holland-code, Realistic, Investigative, Artistic, Social, Enterprising, Conventional, Big Five).
        Beroepen zijn ALGEMEEN uit de Nederlandse arbeidsmarkt: functiegroepen die in heel Nederland voorkomen.
        Niet beperken tot vacatures die nu op Lobsy staan, niet verzinnen van bedrijfsnamen, geen woonplaats van de kandidaat vragen.
        Antwoord ALLEEN als JSON-object met exact deze velden:
        {
          "strengths": ["korte sterke kanten in gewone taal"],
          "superMatches": [{"title":"algemeen beroep","percent":97,"why":"één zin","keys":["zoekwoord","synoniem"]}],
          "strongChoices": [{"title":"...","percent":88,"why":"...","keys":["..."]}],
          "broadening": [{"title":"...","percent":78,"why":"...","keys":["..."]}],
          "practicalNotes": ["Wat betekent dit voor jou? korte alinea's over werkplek, cultuur, taken, en hoe je de banenkaart gebruikt"]
        }
        superMatches: percent 95-100, 3 tot 6 beroepen.
        strongChoices: percent 85-94, 3 tot 6 beroepen.
        broadening: percent 75-84, 3 tot 6 beroepen (doorgroei of omscholing).
        keys: 2-6 korte Nederlandse zoekwoorden waarmee we actuele vacatures herkennen (bijv. verkoop, winkel, kas, zorg).
        Geen e-mail, naam of andere persoonsgegevens.
        """;

    public static string User(
        IReadOnlyList<DeepAnalysisDomainScore> scores,
        IReadOnlyDictionary<int, int> answers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Resultaten van de uitgebreide beroepentest (150 unieke vragen). Geen naam of e-mail.");
        sb.AppendLine("Scores 0-100 per richting:");
        foreach (var score in scores.OrderByDescending(s => s.Percent).ThenBy(s => s.Domain, StringComparer.Ordinal))
        {
            sb.Append("- ")
                .Append(CareerCompassBuilder.TypeLabel(score.Domain))
                .Append(": ")
                .Append(score.Percent)
                .AppendLine("%");
        }

        sb.AppendLine();
        sb.AppendLine("Antwoorden (1=helemaal oneens, 5=helemaal eens):");
        foreach (var question in DeepAnalysisCatalog.CareerQuestions)
        {
            if (!answers.TryGetValue(question.Id, out var value))
            {
                continue;
            }

            sb.Append(question.Id)
                .Append(". ")
                .Append(question.PromptNl)
                .Append(" → ")
                .Append(value)
                .AppendLine();
        }

        return sb.ToString();
    }
}
