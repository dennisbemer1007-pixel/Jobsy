namespace Jobsy.Core.Rules;

/// <summary>Short competence blurbs for referents (words only, no scores).</summary>
public static class ReferenceVerificationCompetenceCopy
{
    public static string Description(string code, string language)
    {
        var en = language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        return code switch
        {
            CompetencyTestCatalog.Samenwerken => en
                ? "Works well with others and helps colleagues when needed."
                : "Werkt prettig samen en helpt collega's waar dat kan.",
            CompetencyTestCatalog.Resultaatgerichtheid => en
                ? "Finishes tasks and keeps an eye on results."
                : "Maakt dingen af en houdt resultaat voor ogen.",
            CompetencyTestCatalog.Stressbestendigheid => en
                ? "Stays calm and friendly when it gets busy."
                : "Blijft rustig en vriendelijk als het druk is.",
            CompetencyTestCatalog.Innovatie => en
                ? "Is open to new ideas and ways of working."
                : "Staat open voor nieuwe ideeën en manieren van werken.",
            CompetencyTestCatalog.Extraversie => en
                ? "Gets energy from contact with people."
                : "Krijgt energie van contact met mensen.",
            _ => en
                ? "A strength seen in day-to-day work."
                : "Een sterk punt in het dagelijkse werk."
        };
    }
}
