namespace Jobsy.Core.Rules;

/// <summary>
/// Fixed Dutch sentence library for the wizard "Eerste indruk" result screen.
/// Keep this file reviewable — no AI generation.
/// </summary>
public static class OnboardingImpressionLibrary
{
    public const string ResultLabel = "Eerste indruk · op basis van 21 vragen";

    public static string StrengthSentence(string dimensionCode)
        => dimensionCode switch
        {
            CompetencyTestCatalog.Samenwerken
                => "Je werkt graag samen en houdt rekening met anderen.",
            CompetencyTestCatalog.Resultaatgerichtheid
                => "Je wilt dingen afmaken en houdt van duidelijk resultaat.",
            CompetencyTestCatalog.Stressbestendigheid
                => "Je blijft relatief rustig als het druk wordt.",
            CompetencyTestCatalog.Innovatie
                => "Je staat open voor nieuwe ideeën en manieren van werken.",
            CompetencyTestCatalog.Extraversie
                => "Je krijgt energie van contact met mensen.",
            _ => "Dit is een sterk punt in hoe jij werkt."
        };

    public static string RiasecSentence(string code)
        => code switch
        {
            CareerTestCatalog.Realistic
                => "Je past bij praktisch werk met je handen of machines.",
            CareerTestCatalog.Investigative
                => "Je past bij werk waarin je dingen uitzoekt en begrijpt.",
            CareerTestCatalog.Artistic
                => "Je past bij werk met creativiteit en eigen inbreng.",
            CareerTestCatalog.Social
                => "Je past bij werk waarin je anderen helpt of begeleidt.",
            CareerTestCatalog.Enterprising
                => "Je past bij werk met overtuigen, verkopen of leiden.",
            CareerTestCatalog.Conventional
                => "Je past bij werk met orde, cijfers of administratie.",
            _ => "Deze richting sluit aan bij jouw interesses."
        };

    public static string CultureSentence(string dimensionCode)
        => dimensionCode switch
        {
            CulturePersonalityCatalog.Autonomy
                => "Je werkt graag met ruimte om zelf keuzes te maken.",
            CulturePersonalityCatalog.Informal
                => "Je voelt je prettig in een informele werksfeer.",
            CulturePersonalityCatalog.Collaboration
                => "Je floreert in teams die echt samenwerken.",
            CulturePersonalityCatalog.Flexibility
                => "Je past je makkelijk aan als het werk verandert.",
            CulturePersonalityCatalog.PeopleFirst
                => "Je kiest voor plekken waar mensen voorop staan.",
            CulturePersonalityCatalog.Innovation
                => "Je zoekt een cultuur die vernieuwing waardeert.",
            _ => "Deze cultuur past bij hoe jij graag werkt."
        };

    public static string ValueSentence(string dimensionCode)
        => dimensionCode switch
        {
            SchwartzValuesCatalog.Autonomy
                => "Je vindt eigen regie en uitdaging belangrijk.",
            SchwartzValuesCatalog.Connection
                => "Je hecht aan warme banden met collega’s en klanten.",
            SchwartzValuesCatalog.Achievement
                => "Je wilt groeien en resultaat laten zien.",
            SchwartzValuesCatalog.Stability
                => "Je zoekt zekerheid en voorspelbaarheid in werk.",
            SchwartzValuesCatalog.Impact
                => "Je wilt bijdragen aan iets dat ertoe doet.",
            _ => "Deze waarde zegt veel over wat jij zoekt."
        };

    /// <summary>
    /// Plain why-line for a match card: strength + dream job + logistics.
    /// </summary>
    public static string MatchWhyLine(
        string? topStrengthLabel,
        string? dreamJob,
        int? travelMinutes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(topStrengthLabel))
        {
            parts.Add($"Past bij je sterkste punt {topStrengthLabel.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(dreamJob)
            && !string.Equals(dreamJob.Trim(), "Weet ik nog niet", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"sluit aan op je droombaan {dreamJob.Trim()}");
        }

        if (travelMinutes is > 0 and < 180)
        {
            parts.Add($"ligt {travelMinutes} min van huis");
        }

        if (parts.Count == 0)
        {
            return "Past bij jouw eerste indruk en beschikbaarheid.";
        }

        if (parts.Count == 1)
        {
            return parts[0] + ".";
        }

        if (parts.Count == 2)
        {
            return $"{parts[0]} en {parts[1]}.";
        }

        return $"{parts[0]}, {parts[1]} en {parts[2]}.";
    }
}
