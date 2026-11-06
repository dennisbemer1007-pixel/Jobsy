using System.Globalization;

namespace Jobsy.Core.Sales;

/// <summary>
/// Dutch PDF / share copy for sales materials (D10). Mirrored into <c>UiStringsSales</c> as SalesPdf.* / Sales.Materials.* / Sales.Pitch.*.
/// </summary>
public static class SalesPdfCopy
{
    public const string FlyerTitle = "Nodig werkgevers uit voor hyper-lokaal werven";
    public const string FlyerLead =
        "Reistijd-matching — bereik kandidaten die écht in de buurt wonen of studeren, zonder abonnement.";
    public const string FlyerWhyTitle = "Waarom Lobsy?";
    public const string FlyerWhy1 = "• Match op fiets, OV of auto — geen landelijke spill";
    public const string FlyerWhy2 = "• Banenkaart + carrousel-highlight";
    public const string FlyerWhy3 = "• Tokens i.p.v. abonnementen — betaal alleen voor plaatsing";
    public const string FlyerCta = "Scan de QR of ga naar je persoonlijke link";
    public const string FlyerScan = "Scan & start";
    public const string FlyerBadge = "1 blad A4";
    public const string FlyerRatesTitle = "Indicatie per vacaturetype";
    public const string FlyerPacksTitle = "Populaire tokenpakketten";

    public const string CardsTitle = "Visitekaartjes Lobsy Partner";
    public const string CardsHelp = "Knip langs de snijtekens · 10 per A4";
    public const string CardsScan = "Scan om te starten";

    public const string PriceTitle = "Prijskaart Lobsy";
    public const string PriceLead = "Tokens · geen abonnement · excl. btw";
    public const string PricePacks = "Tokenpakketten";
    public const string PriceActions = "Wat kost wat?";
    public const string PriceSalesPackages = "Pakketten via je salesmanager";
    public const string PricePerToken = "€ {0} / token";
    public const string PriceFrom = "vanaf {0}";
    public const string PriceFooter = "Prijzen excl. btw · actuele tokenpakketten";

    public const string PresWelcome = "Welkom";
    public const string PresProblem = "Het probleem";
    public const string PresHow = "Zo werkt Lobsy";
    public const string PresCost = "Wat kost het";
    public const string PresGet = "Wat u krijgt";
    public const string PresStart = "Zo start u";
    public const string PresContact = "Contact";
    public const string PresProblemBody =
        "Lange vacatureteksten die kandidaten niet lezen. Landelijke sites met veel noise. Abonnementen terwijl u alleen plaatsingen nodig heeft.";
    public const string PresHowBody =
        "Lobsy toont vacatures op een banenkaart dichtbij huis. Kandidaten solliciteren met één tik — op reistijd, niet op postcode.";
    public const string PresCostBody =
        "U betaalt alleen per vacature, met tokens. Vanaf {0} per token excl. btw. Geen abonnement.";
    public const string PresGetBody =
        "Gratis start-highlight op de eerste vacature ({0} tokens). Eén vast aanspreekpunt. Geen abonnement.";
    public const string PresStartBody =
        "Meld u aan via de QR of link. Vul de code in bij registratie als u de link al kent.";
    public const string PresContactLead = "Vragen? Uw salesmanager helpt u graag.";
}

public static class SalesMaterialsCopy
{
    public const string EmailSubject = "Lobsy — hyper-lokaal werven zonder abonnement";

    public const string EmailBodyTemplate =
        "Hoi,\n\nVia Lobsy bereik je kandidaten dichtbij huis op reistijd, met tokens in plaats van een abonnement. " +
        "Start via mijn link: {link}\n\nBij de eerste vacature krijg je een gratis highlight. Vragen? Mail me terug.\n\nGroet";

    public const string WhatsAppTemplate =
        "Hoi! Lobsy helpt je hyper-lokaal werven op reistijd — tokens i.p.v. abonnement. Start hier: {link} (eerste vacature gratis uitgelicht).";

    public const string PitchCostTemplate =
        "U betaalt alleen per vacature, met tokens. Vanaf € {0} per token. Geen abonnement.";

    public static string FillLink(string template, string link)
        => template.Replace("{link}", link, StringComparison.Ordinal);

    public static string PitchCost(decimal minPerToken)
        => string.Format(
            CultureInfo.GetCultureInfo("nl-NL"),
            PitchCostTemplate,
            minPerToken.ToString("0.00", CultureInfo.GetCultureInfo("nl-NL")));
}

public static class SalesPitchCopy
{
    public const string Step1Title = "Het probleem";
    public const string Step1Body = "Lange vacatureteksten die niemand leest.";
    public const string Step2Title = "Wat Lobsy doet";
    public const string Step2Body = "Toont je vacature op de banenkaart, dichtbij huis.";
    public const string Step3Title = "Wat het kost";
    public const string Step4Title = "Zo start u";
    public const string Step4Body = "Aanmelden via mijn link; eerste vacature gratis highlight.";
    public const string Tip = "Tip: laat de banenkaart even zien op je telefoon.";
}
