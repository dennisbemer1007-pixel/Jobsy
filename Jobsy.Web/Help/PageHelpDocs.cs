namespace Jobsy.Web.Help;

/// <summary>Pagina-documentatie voor de globale info-knop (niet op de banenkaart).</summary>
public static class PageHelpDocs
{
    public sealed record Doc(
        string Title,
        string Purpose,
        string HowItWorks,
        string UsedFor);

    public static bool IsExcludedPath(string? path)
    {
        var p = Normalize(path);
        return p is "/" or "/banen" or "/banenkaart";
    }

    public static Doc? TryGet(string? path)
    {
        var p = Normalize(path);
        if (IsExcludedPath(p))
        {
            return null;
        }

        if (Exact.TryGetValue(p, out var exact))
        {
            return exact;
        }

        foreach (var (prefix, doc) in Prefixes)
        {
            if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return doc;
            }
        }

        return Fallback;
    }

    private static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var p = path.Trim();
        var q = p.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            p = p[..q];
        }

        if (p.Length > 1)
        {
            p = p.TrimEnd('/');
        }

        return string.IsNullOrEmpty(p) ? "/" : p.ToLowerInvariant();
    }

    private static readonly Doc Fallback = new(
        Title: "Deze pagina",
        Purpose: "Onderdeel van Lobsy — het platform voor banen zoeken en vacatures beheren.",
        HowItWorks: "Gebruik de navigatie onderaan of in het menu om tussen modules te wisselen. Op de meeste schermen kun je gegevens bekijken, filteren of bewerken binnen jouw rol.",
        UsedFor: "Afhankelijk van je rol (kandidaat, werkgever, intermediair, sales of admin) zie je andere modules en rechten.");

    private static readonly Dictionary<string, Doc> Exact = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/login"] = new(
            "Inloggen",
            "Hier log je in op Lobsy met Google, Microsoft of je e-mailadres en wachtwoord.",
            "Kies hoe je wilt inloggen. Bij Google of Microsoft ga je kort naar die dienst en kom je daarna terug. Met e-mail vul je je adres en wachtwoord in.",
            "Na het inloggen kun je solliciteren, vacatures bekijken en je profiel bijhouden."),

        ["/account-maken"] = new(
            "Account maken",
            "Gratis kandidaat-account met Google, Microsoft of een e-mailcode (zonder wachtwoord).",
            "Kies Google of Microsoft, of vul je e-mail in om een 6-cijferige code te ontvangen. Na bevestigen ben je ingelogd en gaan eventuele testantwoorden mee.",
            "Snel een account maken om je resultaat te bewaren en verder te gaan."),

        ["/account-maken/code"] = new(
            "Code invoeren",
            "Bevestig je e-mailadres met de 6-cijferige code uit je inbox.",
            "Vul de code in die we hebben gestuurd. Klopt die, dan ben je meteen ingelogd.",
            "Afronden van account maken of inloggen zonder wachtwoord."),

        ["/register"] = new(
            "Bedrijf registreren",
            "Nieuwe werkgever of intermediair aanmelden via KVK-gegevens.",
            "Vul contact- en KVK-/vestigingsgegevens in. Na indienen volgt activatie (e-mail) en eventueel controle/overname als de vestiging al bestaat.",
            "Een organisatie-account opzetten om vacatures te plaatsen en tokens te beheren."),

        ["/register/koppelen"] = new(
            "Account koppelen",
            "Microsoft- of Google-login koppelen aan het zojuist geactiveerde werkgeversaccount.",
            "Na de bevestigingscode word je doorgestuurd naar de IdP. Alleen als het IdP-e-mailadres overeenkomt, wordt de login gekoppeld.",
            "Inloggen zonder opnieuw een wachtwoord te kiezen."),
        ["/register/activate"] = new(
            "Account activeren",
            "Bevestigen van een registratie via activatielink.",
            "Open de link uit de e-mail (of demo-link). Daarna is het account actief of volgt een overnameproces.",
            "Registratie afronden zodat managers kunnen inloggen."),

        ["/register/verifieren"] = new(
            "Bedrijf verifiëren",
            "Kies zakelijk e-mailadres of brief met code om je bedrijf te verifiëren.",
            "Zonder website bij KVK is e-mail niet beschikbaar. Handmatige controle is de fallback.",
            "Zichtbaar worden voor kandidaten en publiceren/tokens vrijgeven."),

        ["/register/verifieren/brief"] = new(
            "Brief onderweg",
            "Code uit de verificatiebrief invullen.",
            "De brief gaat naar het KvK-adres. Code is 30 dagen geldig; opnieuw versturen na 7 dagen.",
            "Bedrijf verifiëren zonder zakelijk e-maildomein."),

        ["/register/toegang"] = new(
            "Toegang aanvragen",
            "Vraag toegang aan tot een bedrijf dat al op Lobsy staat.",
            "Bevestig je e-mail; de bedrijfsmanager beslist. Na 5 werkdagen kijkt Lobsy-support mee.",
            "Samenwerken in één bedrijfsaccount zonder tweede eigenaar te worden."),

        ["/register/bedrijf"] = new(
            "Over je bedrijf",
            "Optioneel: branches, Zo werken wij (cultuurschuifjes) en kernwaarden.",
            "Alles is optioneel (± 3 minuten). Overslaan brengt je naar verifiëren.",
            "Zelfde taal als Cultuurscan en Waardentest zodat Match beter werkt."),

        ["/admin/werkgeververificatie"] = new(
            "Werkgeververificatie",
            "Admin-wachtrij voor handmatige controles, gemarkeerde registraties, geblokkeerde brieven en geëscaleerde toegangsverzoeken.",
            "Goedkeuren, afwijzen met reden, of een brief sturen. Beslissingen worden geaudit.",
            "Twijfelgevallen afhandelen zodat echte bedrijven zichtbaar worden."),

        ["/access-denied"] = new(
            "Geen toegang",
            "Je hebt deze pagina geopend zonder de juiste rol of rechten.",
            "Ga terug naar Home of log in met een account dat wél toegang heeft.",
            "Voorkomen dat gevoelige beheer- of werkgeversfuncties per ongeluk openstaan."),

        ["/home"] = new(
            "Home / dashboard",
            "Startscherm na inloggen, afgestemd op jouw rol.",
            "Als kandidaat open je Mijn Lobsy Kompas: Mijn DNA, Profiel, Tests en Functiefit. Andere rollen zien kerncijfers en KPI-categorieën.",
            "Overzicht houden en snel naar vacatures, tokens, sollicitaties of beheer gaan."),

        ["/hoe-werkt-lobsy"] = new(
            "Hoe werkt Lobsy",
            "Uitleg over Lobsy afgestemd op jouw rol (vestiging, regio, bedrijf, intermediair of sales).",
            "Lees de stappen, volg de links naar de juiste modules en gebruik de knoppen onderaan om meteen aan de slag te gaan.",
            "Snel begrijpen wat jij in Lobsy doet en waar je de belangrijkste acties vindt."),

        ["/candidate/hoe-werkt-lobsy"] = new(
            "Hoe werkt Lobsy (kandidaat)",
            "Stapsgewijze uitleg voor kandidaten: banenkaart, profiel, bewaren, solliciteren en opvolging.",
            "Lees de stappen en ga daarna door naar de banenkaart of je profiel. Eerste keer afronden markeert de uitleg als gezien.",
            "Weten hoe je een baan vindt en solliciteert zonder te verdwalen."),

        ["/candidate/liked"] = new(
            "Bewaard",
            "Vacatures die je hebt geliket of bewaard.",
            "Bekijk de lijst, open details of verwijder items. Anonieme gebruikers zien een beperkte weergave; ingelogde kandidaten hun eigen bewaarde set.",
            "Interessante banen bijhouden zonder meteen te solliciteren."),

        ["/candidate/shared"] = new(
            "Gedeeld",
            "Vacatures die met jou zijn gedeeld.",
            "Open gedeelde items om de vacature te bekijken of verder te bewaren/solliciteren.",
            "Doorverwijzingen van anderen of eerdere shares terugvinden."),

        ["/candidate/applications"] = new(
            "Mijn sollicitaties",
            "Overzicht van je sollicitaties en statussen.",
            "Filter op tabbladen, open een sollicitatie of trek in waar dat mag.",
            "Voortgang volgen van openstaande en afgeronde sollicitaties."),

        ["/candidate/profile"] = new(
            "Mijn profiel",
            "Jouw kandidaatgegevens voor matching en solliciteren.",
            "Vul interesses, opleiding, rijbewijzen, voorkeuren, locatie, uren per week, beschikbaarheid/dagdelen in op het tabblad Mijn profiel. Competentietest en beroepentest staan op eigen tabbladen; uitkomsten landen in Mijn Beroepen-kompas. Na beide quick-scans kun je op Past dit bij mij? een functietitel toetsen. Geboortedatum is nodig voor leeftijdsloon en wettelijke taakchecks. Rechts zie je de Top 10 vacatures vanaf 60% match.",
            "Betere matches en sneller solliciteren met volledige gegevens."),

        ["/candidate/competencies"] = new(
            "Competentietest",
            "Vijfentwintig stellingen op basis van het Big Five-model, vertaald naar werkcompetenties.",
            "Beantwoord in je eigen tempo. Tussentijds opslaan mag; later kun je antwoorden wijzigen. Afronden herberekent je scores en de vacature-matches op je profiel. Optioneel: uitgebreide analyse (150 vragen, € 2,99) met PDF.",
            "Inzicht in samenwerken, resultaatgerichtheid, stressbestendigheid, innovatie en extraversie."),

        ["/candidate/culture"] = new(
            "Cultuurscan",
            "Achttien stellingen over hoe jij graag werkt: sfeer, zelfstandigheid, samenwerken en hoe jij in een team past.",
            "Rond af voor je cultuur- en persoonlijkheidsprofiel. Geen deep analysis — de Quick-Scan is genoeg voor matching.",
            "Cultuurvoorkeur mee laten wegen in Functie-Fit en cultuurfit."),

        ["/candidate/values"] = new(
            "Waarden & drijfveren",
            "Vijfentwintig stellingen gebaseerd op het Schwartz Value Model: autonomie, verbinding, prestatie, stabiliteit en impact.",
            "Rond de gratis scan af of ontgrendel de diepteanalyse (150/200 vragen, € 2,99). Pauzeren mag; je hervat later. Uitkomsten wegen mee in Mijn DNA en matching.",
            "Kernwaarden en drijfveren zichtbaar in je verhaal en vacature-fit."),

        ["/candidate/disc"] = new(
            "Cultuurscan",
            "Achttien stellingen over hoe jij graag werkt: sfeer, zelfstandigheid, samenwerken en hoe jij in een team past.",
            "Rond af voor je cultuur- en persoonlijkheidsprofiel. Geen deep analysis — de Quick-Scan is genoeg voor matching.",
            "Cultuurvoorkeur mee laten wegen in Functie-Fit en cultuurfit."),

        ["/werkgever/organisatie/profiel?tab=cultuur"] = new(
            "Bedrijfscultuur",
            "Twaalf stellingen over hoe jullie team écht werkt.",
            "Vul in zodat kandidaten beter matchen op sfeer, niet alleen op functietitel.",
            "Objectief cultuurprofiel voor matching."),

        ["/candidate/career"] = new(
            "Beroepentest",
            "Vijfentwintig stellingen over wat je wilt in werk: aanpakken, uitzoeken, maken, helpen, aanjagen of organiseren.",
            "Rond af voor Mijn Beroepen-kompas en een top 10 actieve vacatures in de regio. Optioneel: uitgebreide beroepentest (200 vragen, € 2,99) met een loopbaan-PDF in gewone taal (super-match, sterke keus, handige verbreding).",
            "Interesses koppelen aan actieve vacatures op de kaart."),

        ["/carriere"] = new(
            "Mijn carrière",
            "Kies je stip op de horizon en zie je voortgang via een uitklapbaar stappenplan.",
            "Selecteer een droombaan. De voortgangsbalk toont je totale match. Klap stappen open voor skills gap, competenties en een concrete actie (cursussen of vacatures).",
            "Loopbaandoel scherp houden en gericht doorgroeien."),

        ["/profiel"] = new(
            "Profiel",
            "Centrale hub: wie je bent, je test-baseline en accountvoorkeuren.",
            "Bekijk of bewerk persoonlijke gegevens, DNA/testscores en meldingen/privacy. Diepere bewerking via Gegevens bewerken of Mijn Lobsy Kompas.",
            "Je kandidaatbaseline rustig bijhouden."),

        ["/candidate/talent-contacts"] = new(
            "Contactverzoeken",
            "Berichten van werkgevers uit de anonieme talentpool.",
            "Reageer binnen 48 uur. Bij akkoord worden contactgegevens gedeeld. Als je al voorzien bent, kan de werkgever het token terugkrijgen.",
            "Contact leggen zonder dat je 06 of e-mail publiek staat."),

        ["/werkgever/talentpool"] = new(
            "Anonieme talentpool",
            "Zoek kandidaten op competenties, RIASEC, reistijd, beschikbaarheid en rijbewijs — zonder leeftijdsfilter.",
            "Profielen blijven anoniem tot je 1 token inzet. Reageert de kandidaat niet binnen 48 uur, dan kun je intrekken en het token terugkrijgen.",
            "Omgekeerd werven: gericht zoeken in de talentenpool."),

        ["/werkgever/talentpool?tab=contact"] = new(
            "Talentpool-contactverzoeken",
            "Openstaande ontgrendelingen en 48-uurs refund.",
            "Na 48 uur zonder reactie, of als de kandidaat al voorzien is, trek je in en wordt het token teruggestort. Na gedeeld contact geen refund.",
            "No-risk ContactUnlock bewaken."),

        ["/werkgever/vacatures"] = new(
            "Vacatures (werkgever)",
            "Beheer van vacatures van jouw organisatie of vestiging, inclusief concepten uit CSV-import of API.",
            "Bekijk status en herkomst (Handmatig, CSV of API). Concepten publiceer je hier — daar wordt het tokenverbruik verwerkt. Afhankelijk van rol kun je publiceren, pauzeren of nieuwe vacatures plaatsen.",
            "Openstaande banen beheren, importeren afronden en opvolgen."),

        ["/werkgever/tokens"] = new(
            "Tokens & facturen",
            "Saldo, verdeling over vestigingen, aankopen en facturen op één plek.",
            "Bekijk KPI's, koop pakketten (bedrijfsmanager), vraag tokens aan (vestigingsmanager) of bekijk verbruik (regiomanager).",
            "Vacaturepublicatie en andere token-acties bekostigen."),

        ["/werkgever/tokens/verbruik"] = new(
            "Verbruik per vestiging",
            "Toegewezen, verbruikt en resterend saldo per vestiging.",
            "Verdeel tokens vanuit de organisatiopot. Regiomanagers kijken alleen mee.",
            "Saldo over vestigingen verdelen."),

        ["/werkgever/tokens/mutaties"] = new(
            "Tokenmutaties",
            "Alle tokenbewegingen met filters en CSV-export.",
            "Filter op vestiging of type; exporteer voor administratie.",
            "Inzicht in aankopen, uitgaven en toewijzingen."),

        ["/werkgever/tokens/facturen"] = new(
            "Facturen",
            "Tokenaankoopfacturen en factuurgegevens.",
            "Download PDF's en stel de voorkeursbetaalmethode in.",
            "Administratie van prepaid tokenaankopen."),

        ["/werkgever/organisatie/vestigingen"] = new(
            "Vestigingen",
            "Vestigingen onder jouw organisatie beheren.",
            "Bekijk vestigingen, zoek via KVK nieuwe vestigingen en registreer ze. Overnames lopen via een apart scherm.",
            "Organisatiestructuur opbouwen zodat managers per vestiging kunnen werken."),

        ["/werkgever/organisatie/vestigingen?tab=regios"] = new(
            "Regio’s",
            "Regio-indeling van de enterprise-organisatie.",
            "Bekijk of bewerk regio’s en koppelingen die regiomanagers gebruiken.",
            "Schaalbare structuur voor meerdere vestigingen."),

        ["/werkgever/organisatie/team"] = new(
            "Gebruikers (organisatie)",
            "Managers en uitnodigingen binnen het bedrijf.",
            "Nodig gebruikers uit per e-mail, bekijk rollen en beheer toegang tot vestigingen/regio’s.",
            "Het juiste team toegang geven tot vacatures en tokens."),

        ["/werkgever/organisatie/profiel"] = new(
            "Bedrijfsgegevens",
            "Beheer hier de kerninstellingen van je organisatie of vestiging: contactvoorkeur voor kandidaten, CSV Batch Import en de externe API-koppeling.",
            "Kies bovenaan de juiste organisatie/vestiging. Onder Overzicht zie je adres, KVK, tokens en actieve vacatures. Bij Contactvoorkeur geef je aan of kandidaten na sollicitatie mail, telefoon of WhatsApp mogen gebruiken (niet zichtbaar op de openbare vacaturepagina) en sla je de gegevens op. Schakel CSV Batch Import in om de tab CSV Import te tonen — vacatures komen binnen als concept. Bij API-koppeling zie je endpoint, X-API-Key-header en een link naar Swagger (request/response). Genereer of e-mail een API-key; de volledige sleutel is één keer zichtbaar. Publiceren (en tokens) doe je daarna onder Vacatures.",
            "Organisatie veilig bereikbaar maken voor kandidaten, batch-CSV en ATS/partners."),

        ["/werkgever/koppelingen"] = new(
            "Koppelingen",
            "API-sleutels en CSV-import voor vacatures.",
            "Beheer API-keys en schakel CSV-import in.",
            "Koppelingen"),
        ["/werkgever/wervingsmateriaal"] = new(
            "Wervingsmateriaal",
            "Raamflyers en tracking voor werving op locatie.",
            "Download flyers per vestiging of als overzicht.",
            "Wervingsmateriaal"),
        ["/werkgever/koppelingen?tab=csv"] = new(
            "CSV Import",
            "Veilige batch-import van vacatures via een CSV-bestand voor jouw organisatie.",
            "Lees de How-to voor verplichte kolommen (titel, omschrijving, data’s, branches, salaristabel-id). Upload een .csv (komma of puntkomma) via slepen of bladeren. Afbeeldingen mag je als URL of Base64 in de kolom afbeelding zetten. Elke rij wordt strikt gevalideerd: geldige rijen worden concept-vacatures, ongeldige blijven staan met een foutmelding. Corrigeer mislukte rijen inline en klik Opnieuw aanbieden. Publiceer geslaagde concepten daarna via Vacatures (tokenverwerking).",
            "Snel en controleerbaar veel vacatures aanmaken zonder blind foute data in te lezen."),

        ["/werkgever/organisatie/salaristabellen"] = new(
            "Salaristabellen / CAO",
            "Loontabellen die aan vacatures of vestigingen gekoppeld kunnen worden.",
            "Maak of open tabellen, beheer schalen/bedragen en koppel waar nodig aan branches.",
            "Consistente beloning tonen en WML-/CAO-afspraken ondersteunen."),

        ["/werkgever/overnames"] = new(
            "Toegangsverzoeken",
            "Collega's die toegang vragen en legacy overnameverzoeken.",
            "Geef toegang (rol mag verlaagd), wijs af, of behandel oude overnames. Geëscaleerde verzoeken gaan naar admin.",
            "Nieuwe collega's laten aansluiten zonder tweede eigenaar te maken."),

        ["/werkgever/te-doen"] = new(
            "Te doen",
            "Openstaande acties en signalen in je bereik (regiomanager: Signalen).",
            "Bekijk publicatieaanvragen, openstaande sollicitaties, lage tokensaldo’s en overnames. Regiomanagers zien alles alleen-lezen.",
            "Snel zien wat aandacht vraagt zonder door menu’s te zoeken."),

        ["/werkgever"] = new(
            "Dashboard",
            "KPI’s, te doen / signalen, wervingstrechter en vestigingen.",
            "Kies een periode (7/30/90 dagen), bekijk wat openstaat en open vestigingen of te-doen items.",
            "In één oogopslag zien wat managers moeten doen."),

        ["/employer/onboarding-checkout"] = new(
            "Onboarding-betaling",
            "Eerstejaars of onboarding-checkout voor werkgevers.",
            "Rond de stub-betaling af zodat onboarding verder kan.",
            "Account/organisatie activeren voor productiegebruik."),

        ["/werkgever/vacatures/nieuw"] = new(
            "Vacature plaatsen",
            "Nieuwe vacature aanmaken voor een vestiging.",
            "Vul titel, eisen, loon, locatie, uren/week, roosters/dagdelen en de wettelijke taakvinkjes (i-knoppen) in. Die vinkjes sturen automatisch of 15–17-jarigen mogen solliciteren. Publiceren kan tokens kosten en kan door moderatie gaan.",
            "Banen zichtbaar maken op de banenkaart voor kandidaten."),

        ["/werkgever/sollicitaties"] = new(
            "Sollicitanten",
            "Binnenkomende sollicitaties op jouw vacatures.",
            "Filter en open kandidaten, bekijk matchscore/status en vervolgstappen. Naam en cv na acceptatie; e-mail en telefoon pas na aanname.",
            "Selectie en opvolging van sollicitaties door managers."),

        ["/werkgever/sollicitaties/{ApplicationId:guid}"] = new(
            "Sollicitatie",
            "Kandidaatdetails en privacyfasen voor één sollicitatie.",
            "Acties per fase: accepteren, uitnodigen, aannemen. Contactgegevens pas na aanname.",
            "Mobiele opvolging van een sollicitatie."),

        ["/werkgever/kandidaatinzichten"] = new(
            "Kandidaatinzichten",
            "Geaggregeerde trends over kandidaten in je bereik (gratis KPI’s + optioneel premium).",
            "Bekijk gratis overzicht. Ontgrendel trends en CSV-export met tokens (bedrijfsmanager; vestigingsmanager alleen bij per-vestiging scope). Regiomanager kijkt mee, zonder te kopen.",
            "Werving sturen met anonieme inzichten zonder PII."),

        ["/werkgever/partner"] = new(
            "Partnerprogramma",
            "Tracking, referrals en overzicht voor partnerbedrijven.",
            "Deel je link, bekijk referrals en commissie-status.",
            "Partnerschap en referrals beheren."),

        ["/werkgever/partner/uitbetalen"] = new(
            "Partner uitbetalen",
            "Selfbilling-uitbetaling is vervangen door automatische referraltokens.",
            "Ga terug naar het partneroverzicht voor je actuele saldo.",
            "Legacy checkout-stub na redirect."),



        ["/intermediary"] = new(
            "Bedrijvenoverzicht",
            "Prestaties per gekoppelde opdrachtgever voor intermediairs.",
            "Bekijk vacatures, openstaande sollicitaties, conversie, tokens en snelle acties per gekoppelde opdrachtgever. Geen vestigingspotten of boosts — één centrale tokenpot.",
            "Stuur op gezondheid per opdrachtgever zonder vestigingen of regio’s."),

        ["/intermediary/team"] = new(
            "Team (intermediair)",
            "Collega’s binnen jouw intermediair-organisatie.",
            "Nodig teamleden uit en bekijk wie toegang heeft tot opdrachtgevers en vacatures.",
            "Samenwerken zonder accounts buiten je organisatie te delen."),

        ["/sales"] = new(
            "Salesdashboard",
            "Overzicht van verdiensten, funnel en te-doen-lijst voor salesmanagers.",
            "Bekijk beschikbare commissie, maandgrafiek, funnel en beste werkgevers.",
            "Snel zien waar je staat in acquisitie en uitbetaling."),

        ["/sales/werkgevers"] = new(
            "Mijn werkgevers",
            "Privacyveilige lijst van werkgevers via jouw link of code.",
            "Filter op status of commissiejaar en open het detailpaneel voor je eigen commissie per aankoop.",
            "Werkgevers opvolgen zonder contact- of vacaturegegevens te zien."),

        ["/sales/link"] = new(
            "Mijn link & materiaal",
            "Persoonlijke link, QR, pitch en materialen met jouw salescode.",
            "Kopieer je link, download flyer/QR/presentatie en deel klaar-staande teksten.",
            "Acquisitie versnellen met consistente Lobsy-boodschap en echte tokenprijzen."),

        ["/sales/aanbevelen"] = new(
            "Salesmanager aanbevelen",
            "Beveel iemand aan; Lobsy beslist. De persoon krijgt een info-mail en kan bezwaar maken.",
            "Vul naam, e-mail en motivatie in, bevestig toestemming, en volg status in je lijst.",
            "Netwerk laten meegroeien binnen de commissiestructuur (één wervingslaag)."),

        ["/sales/aanbevelen/bezwaar"] = new(
            "Aanbeveling bezwaar",
            "Verwijder je gegevens na een salesmanager-aanbeveling.",
            "Open de eenmalige link uit de info-mail om je gegevens te wissen.",
            "AVG art. 14: geïnformeerd en recht op bezwaar."),

        ["/partner"] = new(
            "Partner / tracking",
            "Publieke landingspagina via sales-trackingcode.",
            "Prospects komen hier via een saleslink en starten registratie of oriëntatie.",
            "Salesmanagers koppelen acquisitie aan hun trackingcode."),

        ["/sales/start"] = new(
            "Sales onboarding",
            "Profiel en gegevens van de salesmanager afronden.",
            "Vul verplichte velden (o.a. bedrijfs-/factuurgegevens) in tot onboarding compleet is.",
            "Klaarzetten voor facturatie en uitbetalingen."),

        ["/sales/profiel"] = new(
            "Profiel & gegevens",
            "Bedrijfsgegevens, btw/KOR, uitbetaalrekening, self-billing en beveiliging.",
            "Houd factuurgegevens bij; wijzig IBAN veilig met 2FA.",
            "Correcte facturen en veilige uitbetalingen."),

        ["/sales/hulp"] = new(
            "Hulp & afspraken",
            "FAQ over commissie, self-billing, KOR en privacy.",
            "Download overeenkomst of self-billing-toestemming; mail support bij vragen.",
            "Duidelijkheid over hoe Lobsy Partner werkt."),

        ["/sales/wallet"] = new(
            "Wallet & uitbetalingen",
            "Saldo per status, mutaties, uitbetalingsaanvragen en self-billing facturen.",
            "Vraag een uitbetaling aan vanaf € 50 beschikbaar; download facturen en jaaroverzicht als PDF.",
            "Inzicht in commissie en uitbetalingen."),

        ["/sales/wallet/uitbetalen"] = new(
            "Uitbetaling aanvragen",
            "Aanvraag voor het volledige beschikbare bedrag met factuurvoorbeeld (self-billing).",
            "Controleer bedrag, btw/KOR en rekening; dien de aanvraag in voor de maandelijkse ronde.",
            "Commissie laten uitbetalen via Lobsy-goedkeuring."),

        ["/tokens/checkout-return"] = new(
            "Betaling afronden",
            "Terugkeer na Mollie-betaling voor een tokenpakket.",
            "Lobsy controleert de status bij Mollie en schrijft tokens bij. Blijft het hangen: opnieuw proberen of Tokens openen.",
            "Na betalen tokens automatisch bijschrijven."),

        ["/tokens/checkout-stub"] = new(
            "Token checkout (Development)",
            "Lokale stub-betaalpagina zonder Mollie API-key.",
            "Bevestig de aankoop in de stub; saldo wordt bijgeschreven alsof Mollie betaald heeft.",
            "Testen van token-aankoop zonder echte betaling."),

        ["/admin/organisaties"] = new(
            "Beheer · Bedrijven & vestigingen",
            "Klanten en intermediairs met hun vestigingen in een boomweergave.",
            "Filter, open detail (panel of drawer), geef tokens of beoordeel een overname.",
            "Platformbeheer van organisatiestructuur en wallets."),

        ["/admin/organisaties/aanvragen"] = new(
            "Beheer · Aanvragen",
            "KvK-controles en overnameverzoeken.",
            "Herstart een mislukte KvK-controle of keur een overname goed/af.",
            "Inbox voor organisatieregistraties die aandacht vragen."),

        ["/admin/gebruikers"] = new(
            "Beheer · Alle gebruikers",
            "Accounts van kandidaten en managers; persoonsgegevens standaard gemaskeerd.",
            "Filter op rol/2FA/status, open de detail drawer voor sessies en 2FA-reset, of vraag support-toegang.",
            "Support en beheer van inloggerechtigde personen."),

        ["/admin/gebruikers/rollen"] = new(
            "Beheer · Rollen & rechten",
            "Read-only overzicht van platformrollen, aantallen en 2FA-beleid.",
            "Bekijk welke rol wat mag via de samenvattende matrix.",
            "Geen nieuw rechtenmodel — alleen inzicht."),

        ["/admin/kandidaten"] = new(
            "Beheer · Kandidaten",
            "Kandidatenlijst (zelfde gemaskeerde gebruikerslijst, tab kandidaten).",
            "Zoek en open kandidaten; support-toegang voor volledige gegevens.",
            "Kandidaten & tests › Kandidaten."),

        ["/admin/vacatures"] = new(
            "Beheer · Vacatures",
            "Platformbreed vacatureoverzicht.",
            "Zoek en open vacatures over alle bedrijven heen.",
            "Moderatie, support en kwaliteitscontrole."),

        ["/admin/vacatures/ats"] = new(
            "Beheer · ATS Vacatures",
            "Gescrapete vacatures van directe lokale werkgevers.",
            "Review completeness, keur goed voor Match/banenkaart, of keur af.",
            "Whitelist-domeinen, blacklist uitzend/recruitment, TTL 30 dagen."),

        ["/admin/financien"] = new(
            "Beheer · Omzet & transacties",
            "Financieel overzicht van het platform.",
            "Bekijk relevante geld-/tokenstromen en rapportages.",
            "Inzicht voor exploitatie en controle."),

        ["/admin/financien/uitbetalingen"] = new(
            "Beheer · Uitbetalingen & btw",
            "Detail van tokenstromen en financiële mutaties.",
            "Analyseer aankopen, grants en correcties in samenhang met finance.",
            "Controle en reconciliatie van tokens versus betalingen."),

        ["/admin/financien/goodwill"] = new(
            "Beheer · Goodwill & tokens",
            "Centrale tokenadministratie.",
            "Ken tokens toe aan bedrijven en bekijk saldi.",
            "Demo’s, credits of correcties uitvoeren."),

        ["/admin/financien/prijzen"] = new(
            "Beheer · Sales commercieel",
            "Commerciële salesinstellingen en overzicht.",
            "Beheer sales-gerelateerde platforminstellingen en rapportages.",
            "Saleskanaal en commissiestructuur ondersteunen."),

        ["/admin/gebruikers/sales"] = new(
            "Beheer · Salesmanagers",
            "Salesmanager-accounts en status.",
            "Beheer onboarding, koppelingen en overzicht van salesmanagers.",
            "Het saleskanaal operationeel houden."),

        ["/admin"] = new(
            "Beheer · Dashboard",
            "Wat vandaag aandacht vraagt: KPI’s, te doen, systeemstatus.",
            "Bekijk headline-KPI’s, open acties en systeemgezondheid. Alle KPI’s blijven beschikbaar via drilldown.",
            "Platformbeheer starten."),

        ["/admin/te-doen"] = new(
            "Beheer · Te doen",
            "Open acties die admin-aandacht vragen.",
            "Filter op onderdeel en ernst. Elke rij heeft één actie.",
            "Openstaande admin-taken afhandelen."),

        ["/admin/vacatures/moderatie"] = new(
            "Beheer · Moderatie",
            "Vacatures die AI-moderatie heeft tegengehouden.",
            "Pas de tekst aan of keur handmatig goed. Zelfde acties als de vacaturelijst.",
            "Gemarkeerde vacatures beoordelen."),

        ["/admin/instellingen"] = new(
            "Beheer · Functies",
            "Platformfuncties groepsgewijs aan of uit.",
            "Zet moderatie, sessie-timeout, support-meldingen en demo-flags aan of uit. Wijzigingen gaan via één opslaan-balk. Prijzen staan onder Financiën › Prijzen.",
            "Gedrag van Lobsy afstemmen zonder code-deploys."),

        ["/admin/scholen"] = new(
            "Beheer · Scholen",
            "Scholen aanmaken, verwerkersovereenkomst en schoolbeheerders.",
            "Maak een school aan, registreer de verwerkersovereenkomst en nodig de eerste schoolbeheerder uit. Leerlingnamen worden niet opgeslagen.",
            "Scholen veilig onboarding geven zonder leerling-PII."),

        ["/admin/scholen/rapportage"] = new(
            "Beheer · Scholen-rapportage",
            "Anonieme totalen (k≥5) en bewaartermijn-runs.",
            "Bekijk aggregaten per schooljaar, exporteer CSV en bekijk of proefdraai de bewaartermijn. Geen koppeling naar codes.",
            "Platformrapportage zonder leerling-PII."),

        ["/admin/scholen/{schoolId}"] = new(
            "Beheer · School",
            "Schoolgegevens, overeenkomst, schoolbeheerders en totalen.",
            "Bewerk schoolgegevens, registreer de verwerkersovereenkomst, nodig schoolbeheerders uit of bekijk totalen (geen per-code data).",
            "Eén school beheren als Lobsy-admin."),

        ["/school"] = new(
            "School · Dashboard",
            "Klassen, codes, leraren en openstaande acties.",
            "Bekijk KPI’s, tests per klas, te-doen-lijst en interesses (vanaf 5 afgeronde tests). Lobsy kent geen leerlingnamen.",
            "Startpunt voor schoolbeheer."),

        ["/school/te-doen"] = new(
            "School · Te doen",
            "Openstaande acties voor de schoolbeheerder.",
            "Ouderbevestiging, leraren zonder klas, openstaande uitnodigingen en bewaartermijn.",
            "Niets missen vóór het testvenster."),

        ["/school/klassen"] = new(
            "School · Klassen & codes",
            "Klassen aanmaken en codes beheren.",
            "Maak een klas met N codes, print de codelijst (lege naamkolom) en beheer het testvenster.",
            "Leerlingcodes uitdelen zonder namen in Lobsy."),

        ["/school/klassen/{classId}"] = new(
            "School · Klasdetail",
            "Codes, ouders en testvenster van één klas.",
            "Print codelijst, bevestig ouders, open/sluit het testvenster, reset of verwijder codes.",
            "Eén klas veilig beheren."),

        ["/school/resultaten"] = new(
            "School · Resultaten",
            "Totalen per klas; per code alleen als de setting aan staat.",
            "Bekijk groepsresultaten vanaf 5 leerlingen. Per-code korte uitkomsten alleen als Lobsy-beheer dat toestaat.",
            "Inzicht zonder namen of losse antwoorden."),

        ["/school/leraren"] = new(
            "School · Leraren",
            "Leraren uitnodigen met verplichte 2FA.",
            "Nodig leraren uit, koppel eigen klassen en volg 2FA-status. Geen rolkeuze: altijd leraar.",
            "Team toegang geven tot alleen hun klassen."),

        ["/school/gegevens"] = new(
            "School · Schoolgegevens",
            "Alleen-lezen schoolgegevens.",
            "Bekijk naam, plaats, BRIN en e-maildomeinen. Wijzigen via Lobsy.",
            "Controleer of schoolgegevens kloppen."),

        ["/school/privacy"] = new(
            "School · Privacy & ouders",
            "Verwerkersovereenkomst, bewaartermijn en ouderbrief.",
            "Bekijk overeenkomststatus, bevestigingen per klas en download de ouderbrief-voorbeeldtekst.",
            "AVG-plichten van school nalopen."),

        ["/school/materiaal"] = new(
            "School · Lesbrief & materiaal",
            "Lesbrief en inlogstappen voor leerlingen.",
            "Open de lesbrief en leg uit hoe leerlingen inloggen met school, klas en code.",
            "Lesmateriaal klaarzetten."),

        ["/leraar"] = new(
            "Leraar · Dashboard",
            "Startpunt: door naar je eerste toegewezen klas of een lege staat.",
            "Als je klassen hebt, ga je naar het klasoverzicht. Anders vraag je je schoolbeheerder om je te koppelen.",
            "Startpunt voor leraren."),

        ["/leerling"] = new(
            "Leerling · Inloggen",
            "Log in met school, klas en code van je kaartje. Geen naam nodig.",
            "Kies je school en klas, typ de code en start je reis.",
            "Leerlingen starten de ontdekkingsreis zonder account."),

        ["/leerling/start"] = new(
            "Leerling · Zo werkt het",
            "Korte uitleg over de 4 werelden voordat je begint.",
            "Lees hoe de reis werkt en klik op Beginnen.",
            "Introductiescherm voor de leerlingwizard."),

        ["/leerling/reis"] = new(
            "Leerling · Reis",
            "Beantwoord vragen; Lobsy bewaart elk antwoord.",
            "Kies een antwoord met de knoppen of toetsen 1–5. Pauze mag altijd.",
            "Vragen beantwoorden in de leerlingwizard."),

        ["/leerling/eiland"] = new(
            "Leerling · Pauze-eiland",
            "Kies hobby’s en wat je niet leuk vindt (chips).",
            "Tik op knoppen; typ geen namen. Klaar, verder! mag ook leeg.",
            "Hobby-chips halverwege de reis."),

        ["/leerling/stop"] = new(
            "Leerling · Pauze",
            "Je sessie is beëindigd; antwoorden blijven bewaard.",
            "Log later opnieuw in met dezelfde code om verder te gaan.",
            "Sessie afsluiten op een gedeelde Chromebook."),

        ["/leerling/dit-ben-jij"] = new(
            "Leerling · Dit ben jij",
            "Jouw verhaal uit vaste sjablonen — positief en kort.",
            "Bekijk je tegels, hobby's en beroepen om eens te bekijken. Download de PDF of check je droombaan.",
            "Resultaat na 60 vragen, zonder AI of vacatures."),

        ["/leerling/droombaan"] = new(
            "Leerling · Droombaan-checker",
            "Kies een beroep uit de vaste lijst en zie wat je al hebt.",
            "Geen AI, geen links naar opleidingen of vacatures — alleen een schoolroute in gewone taal.",
            "Droombaan checken en bewaren als PDF."),

        ["/leerling/pdf"] = new(
            "Leerling · PDF",
            "Download je ontdekkingsreis als PDF.",
            "Een lege naamregel voor op papier. Lobsy bewaart geen namen.",
            "PDF voor jou en je leraar."),

        ["/admin/instellingen/algemeen"] = new(
            "Beheer · Bedrijfsgegevens",
            "NAW, KvK en BTW van Lobsy.",
            "Vul bedrijfsnaam, slogan, adres, KvK en BTW in. Deze gegevens staan onderaan self-billing factuur-PDF’s.",
            "Juridische platformgegevens op facturen houden."),

        ["/admin/content/paginas"] = new(
            "Beheer · Wie zijn wij",
            "Publieke ‘Wie zijn wij’-pagina bewerken.",
            "Pas titel, introregel en inhoud aan. Gebruik koppen voor secties. De pagina is zichtbaar via /wie-zijn-wij.",
            "Het verhaal achter Lobsy up-to-date houden zonder code-deploys."),

        ["/admin/content/paginas-flyer"] = new(
            "Beheer · Werkgeversflyer",
            "Professionele A4-flyer voor werkgevers bewerken en afdrukken.",
            "Pas koppen, USP’s, lanceringsteksten en QR-doel aan. Download de PDF om te printen of digitaal te delen.",
            "Werkgevers overtuigen met een logo-first flyer zonder designbureau."),

        ["/admin/content/emails"] = new(
            "Beheer · Mailtest",
            "Elk transactioneel mailtype als test versturen naar een adres naar keuze.",
            "Vul een e-mailadres in en verstuur één type of alle types. De HTML is dezelfde als productie; knoppen linken naar echte Lobsy-pagina’s. OTP’s en wachtwoorden in testmails zijn voorbeelden en activeren geen accountactie.",
            "Visueel en functioneel nalopen van alle uitgaande mails zonder echte gebruikers te mailen."),

        ["/admin/instellingen/integraties"] = new(
            "Beheer · Integraties",
            "API-koppelingen (Mollie, KVK, Entra, Google, Mail, OpenAI).",
            "Vul credentials in, sla op en test de verbinding. Gebruik de i per tegel voor details.",
            "Externe diensten laten werken voor login, mail, betalen en moderatie."),

        ["/admin/feedback"] = new(
            "Beheer · Feedback",
            "Ingezonden gebruikersfeedback.",
            "Bekijk, filter en beantwoord feedbackmeldingen van kandidaten en werkgevers.",
            "Productfeedback verzamelen en opvolgen."),

        ["/admin/beveiliging/systeemlogs"] = new(
            "Beheer · Systeemlogs",
            "Platformlogging voor incidenten en audits.",
            "Filter op type en datum; uitschrijvingen staan onder Unsubscribe.",
            "Storingen en AVG-relevante events naslaan."),

        ["/admin/beveiliging"] = new(
            "Beheer · Auditlog",
            "Onveranderbaar admin-auditlog (7 jaar).",
            "Filter op actie, periode en resultaat; exporteer CSV; bekijk detail in de drawer.",
            "Verantwoorden wie wat wanneer wijzigde."),

        ["/admin/beveiliging/2fa"] = new(
            "Beheer · 2FA & sessies",
            "2FA-inschrijving per rol en recente resets.",
            "Zie wie 2FA mist en open de gebruiker-drawer voor reset of sessies.",
            "Privileged accounts afdwingen tot 2FA."),

        ["/admin/beveiliging/privacy"] = new(
            "Beheer · Privacy & AVG",
            "Maskering, bewaartermijnen en uitgevoerde verwijderingen.",
            "Read-only feiten; geen verzoekenwachtrij.",
            "AVG-retentie en anonymisatie naslaan."),

        ["/admin/beveiliging/gegevensinzage"] = new(
            "Beheer · Gegevensinzage",
            "Accesslog van persoonsgegevensinzage door support.",
            "Bekijk wie wanneer gemaskeerde of onthulde persoonsgegevens heeft gezien.",
            "AVG-verantwoording van support-toegang."),

        ["/admin/content/stamgegevens"] = new(
            "Beheer · Stamgegevens",
            "Keuzelijsten voor kandidaatprofiel en vacatures.",
            "Beheer opties, exclusiviteit-stages en gerelateerde stamdata.",
            "Consistente keuzelijsten over het platform."),

        ["/admin/content/opleidingen"] = new(
            "Beheer · Opleidingen",
            "Opleidingsaanbod in Lobsy beheren.",
            "Beheer opleidingen die kandidaten en werkgevers zien.",
            "Opleidingscontent actueel houden."),

        ["/admin/vacatures/categorieen"] = new(
            "Beheer · Categorieën & salaris",
            "Vacaturecategorieën en WML-tarieven.",
            "Beheer categorieën (kleur, tokens) en platform WML-tarieven.",
            "Prijs- en categorielogica van vacatures afstemmen."),

        ["/admin/organisaties/regios"] = new(
            "Beheer · Regio's & domeinen",
            "Domeinen (CNAME) en regio's van organisaties.",
            "Beheer hostnames onder Domeinen; regio-lijst is read-only (beheerd door de organisatie).",
            "Regionale landingspagina’s en hosts beheren."),

        ["/privacy"] = new(
            "Privacyverklaring",
            "Uitleg welke gegevens Lobsy verwerkt, inclusief matching, AI en jeugdige-arbeidschecks.",
            "Lees de tekst; voor inzage/export ga je naar ‘Mijn gegevens’ als je bent ingelogd. Bij een nieuwe consentversie vragen we ondernemers opnieuw om akkoord.",
            "Transparantie en AVG-informatie."),

        ["/privacy/data"] = new(
            "Mijn gegevens",
            "Inzage in jouw persoonsgegevens in Lobsy.",
            "Bekijk welke data aan je account hangt en gebruik export/verwijderacties waar beschikbaar.",
            "AVG-rechten uitoefenen."),

        ["/gebruiksvoorwaarden"] = new(
            "Gebruiksvoorwaarden",
            "Voorwaarden en disclaimer voor kandidaten/bezoekers, inclusief AI en matchscores.",
            "Lees de afspraken over gebruik van het platform, chatbot en matching.",
            "Duidelijkheid over rechten en plichten als werkzoeker."),

        ["/algemene-voorwaarden"] = new(
            "Algemene voorwaarden",
            "Voorwaarden voor ondernemers/werkgevers, inclusief tokens, matching en taakvinkjes.",
            "Lees de AV over tokens, publicatie, jeugdige arbeid en aansprakelijkheid.",
            "Juridische basis voor zakelijk gebruik van Lobsy."),

        ["/wie-zijn-wij"] = new(
            "Wie zijn wij",
            "Het verhaal en contactkader achter Lobsy.",
            "Lees wie Lobsy is; inhoud kan door admins worden bijgewerkt.",
            "Context en vertrouwen in het platform.")
    };

    private static readonly (string Prefix, Doc Doc)[] Prefixes =
    [
        ("/vacancies/", new(
            "Vacaturedetail",
            "Details van één vacature op de banenkaart.",
            "Bekijk eisen, loon, uren/dagdelen en locatie. Solliciteer als kandidaat (met eventuele harde eisen en wettelijke checks), of bewaar/deel. Managers zien geen solliciteer-CTA.",
            "Een baan beoordelen en solliciteren of delen.")),

        ("/leraar/klas/", new(
            "Leraar · Klas",
            "Overzicht, codes, groepsresultaten en droombaan per eigen klas.",
            "Bekijk voortgang, open het testvenster, print de codelijst en bekijk groeps- of codedetails (geen namen, geen ruwe antwoorden).",
            "Les voorbereiden en nabespreken.")),

        ("/home/metrics/", new(
            "Metric detail",
            "Uitgesplitste cijfers achter een dashboard-KPI.",
            "Bekijk de onderliggende lijst of grafiek bij de gekozen metric-key.",
            "Dieper analyseren waarom een KPI zo staat.")),

        ("/partner/", Exact["/partner"]),

        ("/werkgever/sollicitaties/", Exact["/werkgever/sollicitaties"]),

        ("/werkgever/organisatie/salaristabellen/", Exact["/werkgever/organisatie/salaristabellen"])
    ];
}
