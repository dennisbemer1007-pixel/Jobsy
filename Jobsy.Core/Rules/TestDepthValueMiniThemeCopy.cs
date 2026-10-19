namespace Jobsy.Core.Rules;

/// <summary>Candidate-facing mini-spiegel copy per competence domain (no scores).</summary>
public static class TestDepthValueMiniThemeCopy
{
    public sealed record ThemeCopy(string Meaning, string Example, string Energy);

    public static ThemeCopy? TryGet(string domainCode, string language)
    {
        var nl = string.Equals(language, "nl", StringComparison.OrdinalIgnoreCase)
                 || language.StartsWith("nl", StringComparison.OrdinalIgnoreCase);
        return domainCode.ToUpperInvariant() switch
        {
            var c when c == CompetencyTestCatalog.Stressbestendigheid.ToUpperInvariant() => nl
                ? new ThemeCopy(
                    "Bij drukte houd je het overzicht en blijf je rustig.",
                    "Op de werkvloer: jij bent degene die kalm blijft als de planning omgooit.",
                    "Vaste afspraken en een voorspelbaar ritme geven je rust.")
                : new ThemeCopy(
                    "Under pressure you keep overview and stay calm.",
                    "At work: you are the one who stays calm when the schedule changes.",
                    "Clear agreements and a steady rhythm give you energy."),
            var c when c == CompetencyTestCatalog.Samenwerken.ToUpperInvariant() => nl
                ? new ThemeCopy(
                    "Je let op collega's en werkt graag mee aan een goede sfeer.",
                    "Op de werkvloer: jij merkt snel wie hulp nodig heeft en springt bij.",
                    "Samen klaren met collega's waar je trots op bent.")
                : new ThemeCopy(
                    "You notice colleagues and like contributing to a good atmosphere.",
                    "At work: you spot who needs help and step in.",
                    "Clear wins with teammates that you can be proud of."),
            var c when c == CompetencyTestCatalog.Resultaatgerichtheid.ToUpperInvariant() => nl
                ? new ThemeCopy(
                    "Je wilt taken afmaken en ziet graag resultaat van je inzet.",
                    "Op de werkvloer: jij houdt de lijst bij en rondt af wat je start.",
                    "Een duidelijk doel per shift waar je na afloop op terugkijkt.")
                : new ThemeCopy(
                    "You want to finish tasks and see results from your effort.",
                    "At work: you track the list and finish what you start.",
                    "One clear goal per shift you can look back on."),
            var c when c == CompetencyTestCatalog.Innovatie.ToUpperInvariant() => nl
                ? new ThemeCopy(
                    "Je zoekt graag naar slimmere of nieuwere manieren van werken.",
                    "Op de werkvloer: jij stelt voor om iets anders te proberen als het stroef loopt.",
                    "Ruimte om een idee uit te proberen zonder meteen alles om te gooien.")
                : new ThemeCopy(
                    "You like finding smarter or newer ways of working.",
                    "At work: you suggest trying something different when things get stuck.",
                    "Room to try an idea without changing everything at once."),
            var c when c == CompetencyTestCatalog.Extraversie.ToUpperInvariant() => nl
                ? new ThemeCopy(
                    "Je haalt energie uit contact met mensen en gesprekken op de vloer.",
                    "Op de werkvloer: jij begroet collega's en start makkelijk een kort overleg.",
                    "Dagen met veel afstemming en korte lijntjes met het team.")
                : new ThemeCopy(
                    "You get energy from people and conversations on the floor.",
                    "At work: you greet colleagues and start check-ins easily.",
                    "Days with alignment and short lines with the team."),
            _ => null
        };
    }
}
