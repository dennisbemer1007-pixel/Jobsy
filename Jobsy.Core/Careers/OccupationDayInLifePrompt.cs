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

        Verzin geen werkgever, geen bedrijfsnaam, geen stad, geen dorp, geen salaris, geen loon en geen diploma. Schrijf niet over een specifieke kandidaat. Geen "jij hebt" en geen "jouw ervaring". Geen naam van een persoon.

        Dit is een typische dag. Beroepen verschillen per werkgever. Zeg dat in het veld "varies", in één of twee korte zinnen.

        Als de bron dun is (het bericht zegt "Bron is dun: ja"), schrijf je een kortere dag. Zeg eerlijk wat je niet weet en wat per werkgever verschilt. Vul niet aan met verzinsels.

        Schrijf in de je-vorm, alsof je een vriend vertelt hoe een gewone dag in dit beroep gaat.

        Gebruik geen woorden als ESCO, ISCO, RIASEC, OCEAN of Big Five.

        Antwoord alleen met JSON in deze vorm:
        {"morning":"...","midday":"...","afternoon":"...","closing":"...","highlights":["...","..."],"varies":"..."}

        morning is de ochtend. midday is het midden van de dag. afternoon is de middag. closing is het afronden van de dag. highlights zijn 2 tot 4 korte punten die in de bron staan. varies legt uit wat kan verschillen. Verzin in closing geen werkgever en geen stad.
        """;

    public const string Retry =
        "Schrijf opnieuw. Alleen JSON. Zinnen van hooguit 20 woorden. Verzin geen werkgever, stad, salaris of diploma. Alleen feiten uit de bron. Als de bron dun is, houd de dag kort.";
}
