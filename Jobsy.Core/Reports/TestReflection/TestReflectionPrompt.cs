namespace Jobsy.Core.Reports.TestReflection;

public static class TestReflectionPrompt
{
    public const string System = """
        Je herschrijft een korte testreflectie voor de kandidaat zelf (jij-vorm, warm, B1).
        Gebruik ALLEEN feiten uit de template en feitenlijst. Verzin geen werk, opleiding, plaats of beroep.
        Geen lof voor eigenschappen onder 50%. Geen algemene Barnum-zinnen. Geen markdown.
        Antwoord ALLEEN als JSON met exact deze velden (arrays max 3 items waar van toepassing):
        {
          "heroTitle": "...",
          "heroSubtitle": "...",
          "badgeLabel": "...",
          "themes": [{"title":"...","summary":"..."}],
          "practiceTitle": "...",
          "practiceExamples": ["..."],
          "watchTitle": "...",
          "watchPoints": ["..."],
          "energyGives": ["..."],
          "energyDrains": ["..."],
          "environmentTitle": "...",
          "environmentBody": "...",
          "dreamCallout": null or "..."
        }
        """;
}
