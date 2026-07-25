using System.Text.RegularExpressions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Local Dutch content checks used when OpenAI is not configured, and as fallback if the LLM call fails.
/// </summary>
public static partial class DutchVacancyModerationHeuristics
{
    public static VacancyContentModerationResult Check(string title, string description)
    {
        var text = Normalize(title, description);

        if (AgePattern().IsMatch(text))
        {
            return VacancyContentModerationResult.Blocked(
                "In de vacature staat een leeftijdsgrens of leeftijdsvoorkeur. Dat kan discriminerend zijn.",
                "Laat leeftijden weg en beschrijf in plaats daarvan wat de functie vraagt, bijvoorbeeld ervaring of fysieke belastbaarheid.");
        }

        if (GenderPattern().IsMatch(text))
        {
            return VacancyContentModerationResult.Blocked(
                "De tekst lijkt een voorkeur voor een bepaald geslacht te bevatten.",
                "Formuleer genderneutraal: spreek over ‘de kandidaat’ of ‘je’, en noem alleen eisen die voor de functie nodig zijn.");
        }

        if (OriginPattern().IsMatch(text))
        {
            return VacancyContentModerationResult.Blocked(
                "Er lijkt een eis of voorkeur rond afkomst, nationaliteit of etniciteit in te staan.",
                "Vervang dat door functionele eisen, zoals ‘goed Nederlands in woord en geschrift’ of ‘woonachtig in de regio’, als die echt nodig zijn.");
        }

        if (HarshRequirementPattern().IsMatch(text))
        {
            return VacancyContentModerationResult.Blocked(
                "De vacature bevat mogelijk onnodig zware of uitsluitende eisen.",
                "Maak onderscheid tussen must-haves en nice-to-haves, en vraag alleen wat echt nodig is om de functie goed uit te voeren.");
        }

        return VacancyContentModerationResult.Allowed();
    }

    private static string Normalize(string title, string description)
    {
        var plain = HtmlSanitize.ToPlainPreview($"{title}\n{description}", maxLength: 20_000);
        return plain.ToLowerInvariant();
    }

    [GeneratedRegex(
        @"\b(maximaal|jonger dan|ouder dan|tussen de)\s+\d{1,2}\s*jaar\b|\bminimaal\s+\d{1,2}\s*jaar\b(?!\s+ervaring)|\b\d{1,2}\s*[-–]\s*\d{1,2}\s*jaar\b|\b(alleen|bij voorkeur)\s+(jonge|jongere|jongeren)\b|\bleeftijd\s*(tot|van|tussen)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex AgePattern();

    [GeneratedRegex(
        @"\b(alleen|bij voorkeur)\s+(mannen|vrouwen|jongens|meisjes)\b|\b(mannelijke|vrouwelijke)\s+(kandidaat|kandidaten|medewerker|medewerkers|collega)\b|\b(hij|zij)\s+moet\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex GenderPattern();

    [GeneratedRegex(
        @"\b(geen\s+buitenlanders|autochtoon|blanke?\s+nederlanders?|westers\s+uiterlijk|alleen\s+nederlanders)\b|\b(van\s+nederlandse\s+afkomst)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex OriginPattern();

    [GeneratedRegex(
        @"\b(perfect|foutloos|moedertaalniveau)\s+(nederlands|engels)\b|\bminimaal\s+\d{2,}\s+jaar\s+ervaring\b|\b(geen\s+starters|alleen\s+ervaren)\b|\baltijd\s+beschikbaar\b|\bgeen\s+beperkingen\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex HarshRequirementPattern();
}
