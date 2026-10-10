namespace Jobsy.Core.Careers;

public static class OccupationDayQualityPrompt
{
    public const string System =
        """
        Je controleert een Nederlandse werkdagtekst voor één beroep. Taalniveau B1.
        Antwoord alleen met JSON: {"ok":true} of {"ok":false,"issues":["korte reden",...]}
        issues zijn korte codes in het Nederlands, maximaal 5.
        Zet ok op false bij:
        - een bloklabel zonder kloktijd (uu:mm)
        - herhaalde woorden of zinsdelen naast elkaar
        - verzonnen of rare woorden (bijv. keukenhulpverleners)
        - werk dat niet bij het beroep past (kas/kwekerij vs tuin schoonmaken)
        - tekst die letterlijk een ESCO/ILO-regel kopieert in plaats van natuurlijk Nederlands
        Verzin geen nieuwe feiten. Alleen wat in de bron staat mag in de dag.
        """;

    public static string UserMessage(string occupationTitle, string sourceSummary, string dayJson)
        => "Beroep: " + occupationTitle
           + "\nBron (samenvatting):\n"
           + sourceSummary
           + "\n\nDag-JSON:\n"
           + dayJson;
}
