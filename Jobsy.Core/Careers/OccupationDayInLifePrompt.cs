namespace Jobsy.Core.Careers;

/// <summary>
/// Strict prompt for one typical Dutch workday. The user message is only <see cref="OccupationDayFacts"/>.
/// </summary>
public static class OccupationDayInLifePrompt
{
    public const string System =
        """
        Je schrijft een typische werkdag voor één beroep. Nederlands, taalniveau B1 of lager. Vriendelijk en helder, niet kinderachtig. Korte zinnen, meestal hooguit 20 woorden.

        Je gebruikt alleen de feiten in het bericht van de gebruiker: de titel, de andere namen, de beschrijving, de vaardigheden en de taken. Dat zijn feiten over het beroep.

        Verzin geen werkgever, geen bedrijfsnaam, geen klant met een naam, geen patiënt met een naam, geen stad, geen dorp, geen salaris, geen loon en geen diploma. Schrijf niet over een specifieke kandidaat. Geen "jij hebt" en geen "jouw ervaring". Geen naam van een persoon.

        Dit is een typische dag. Beroepen verschillen per werkgever. Zeg dat in het veld "varies", in één of twee korte zinnen.

        Als de bron dun is (het bericht zegt "Bron is dun: ja"), schrijf je een kortere dag. Zeg eerlijk wat je niet weet en wat per werkgever verschilt. Vul niet aan met verzinsels.

        Schrijf in de je-vorm, alsof je een vriend vertelt hoe een gewone dag in dit beroep gaat.

        Gebruik geen woorden als ESCO, ISCO, RIASEC, OCEAN of Big Five.

        Antwoord alleen met JSON in deze vorm:
        {"blocks":[{"key":"start","label":"Start","text":"..."},{"key":"morning","label":"Ochtend","text":"..."}],"highlights":["...","..."],"varies":"..."}

        blocks heeft 5 tot 8 momenten. Bij een dunne bron: 4 tot 6 momenten.
        Neem altijd start, morning en afternoon. Neem ook pause en close. Dat zijn 5 momenten en dat is genoeg als de bron verder niets noemt.
        Neem bij voorkeur 7 momenten als de bron dat toelaat. Voeg talk toe alleen bij contact met mensen, plan alleen bij plannen of routes, handover alleen bij overdragen of afstemmen. Sla die drie over als de bron ze niet noemt. Verzin ze niet om aan een hoger aantal te komen.
        key is precies een van: start, morning, talk, plan, pause, afternoon, handover, close. Geen Nederlands woord als key en geen kloktijd als key. Gebruik die volgorde.
        label is kort, hooguit drie woorden en hooguit 32 tekens. Elke label heeft een kloktijd in het formaat uu:mm, zoals "07:00 Start" of "14:30 Afronden". "Ochtendzorg" alleen als de bron over zorg gaat. "Gesprek" alleen als de bron contact met mensen noemt. "Overdracht" alleen als de bron overdragen of afstemmen noemt.
        Pauze mag altijd. Noem daarbij geen plaats.
        text is één of twee korte zinnen, minstens 40 tekens. Alleen feiten uit de bron.
        highlights zijn 2 tot 4 korte punten die letterlijk bij de taken of vaardigheden passen. Bij een dunne bron mag 1 punt.
        varies legt uit wat per werkgever kan verschillen. Verzin geen werkgever, klant, stad of diploma.
        """;

    public const string GenerationRules =
        """
        Schrijf natuurlijk Nederlands (B1). Gebruik de taken en beschrijving als feiten, maar kopieer geen lange ESCO/ILO-zinnen letterlijk.
        Geen zeldzame of verzonnen woorden. Geen herhaling van hetzelfde woord direct na elkaar.
        Houd je aan de werkplek in dit bericht (kas is niet dezelfde als particuliere tuin).
        """;

    public static string RetryFor(string? reason)
    {
        var detail = string.IsNullOrWhiteSpace(reason) ? "afgekeurd" : reason.Trim();
        return "Afgekeurd: " + detail
            + "\nSchrijf opnieuw. Alleen JSON. Herstel precies die punten. Zinnen van hooguit 20 woorden. Verzin geen werkgever, stad, salaris of diploma. Alleen feiten uit de bron. Als de bron dun is, houd de dag kort. key is precies een van: start, morning, talk, plan, pause, afternoon, handover, close. Elke label heeft een kloktijd (uu:mm).";
    }
}
