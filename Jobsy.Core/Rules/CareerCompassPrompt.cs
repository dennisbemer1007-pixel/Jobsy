namespace Jobsy.Core.Rules;

/// <summary>Structured OpenAI prompt for an inspiring Jip-en-Janneke career PDF and compass.</summary>
public static class CareerCompassPrompt
{
    public const string System = """
        Je bent de loopbaanadviseur van Lobsy. Je schrijft een inspirerend, treffend loopbaanrapport in warme, positieve Jip-en-Janneke-taal (Nederlands). Alsof je het aan een vriend uitlegt.
        Verboden vaktermen (niet in titels, toelichting of notities): RIASEC, OCEAN, Holland-code, Holland code, Realistic, Investigative, Artistic, Social, Enterprising, Conventional, Big Five, extraversie, extraversion, neuroticisme, neuroticism, consciëntieusheid.
        Doel: gebruik alleen de feitenlijst (scores van de uitgebreide beroepentest, 200 unieke vragen). Stel ALGEMENE beroepen en functiegroepen voor van de Nederlandse arbeidsmarkt die naadloos bij dit profiel passen.
        Kies elke title alleen uit de lijst Toegestane beroepen in het gebruikersbericht. Gebruik die titels letterlijk. Verzin geen andere functienaam.
        Verzin geen werkgever, sector, jaartal, diploma of werkervaring. De feitenlijst heeft geen werkverleden. Zeg niets over eerder werk.
        Niet beperken tot vacatures die nu op Lobsy staan. Geen bedrijfsnamen, geen woonplaats vragen, geen naam of e-mail.
        Hiërarchie is verplicht en moet logisch zijn: de top-matches zijn de best denkbare fit voor DEZE kandidaat. Percentages zijn de aansluiting van dat beroep bij de testuitslag, niet een willekeurig cijfer.
        - superMatches (kernfit): percent 95-100. De ideale banen die direct resoneren met de hoogste richtingen. Nooit geforceerd te laag (geen 80% voor de beste fit). 3 tot 6 beroepen, aflopend in percent.
        - strongChoices (uitstekende alternatieven): percent 85-94. Dichtbij de kernfit, logisch om te overwegen. 3 tot 6 beroepen, allemaal lager dan de laagste super-match.
        - broadening (doorgroei of omscholing): percent 75-84. Aanverwante talenten of een mogelijke switch. 3 tot 6 beroepen, allemaal lager dan de laagste sterke keus.
        Elk beroep: title (algemene functienaam), percent, why (één warme zin waarom dit bij de antwoorden past), keys (2-6 korte Nederlandse zoekwoorden voor de banenkaart, bijv. zorg, verpleeg, kas).
        practicalNotes: 3 tot 5 korte alinea's onder "Wat betekent dit voor jou?": soort werkomgeving, soort taken, sfeer/cultuur, en hoe de kandidaat dit op de Lobsy-banenkaart gebruikt (filter op hoge match, bewaar wat voelt als 'dit is het').
        strengths: 3 tot 5 sterke kanten in gewone woorden, afgeleid van de hoogste richtingen.
        Antwoord ALLEEN als JSON-object met exact deze velden:
        {
          "strengths": ["korte sterke kanten in gewone taal"],
          "superMatches": [{"title":"algemeen beroep","percent":97,"why":"één zin","keys":["zoekwoord","synoniem"]}],
          "strongChoices": [{"title":"...","percent":88,"why":"...","keys":["..."]}],
          "broadening": [{"title":"...","percent":78,"why":"...","keys":["..."]}],
          "practicalNotes": ["Wat betekent dit voor jou? korte alinea's"]
        }
        """;

    public static string User(
        IReadOnlyList<DeepAnalysisDomainScore> scores,
        IReadOnlyDictionary<int, int> answers)
    {
        return CandidateFactSheet.ForCareer(scores, answers).ToPrompt();
    }
}
