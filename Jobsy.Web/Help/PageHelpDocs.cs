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
        return p is "/" or "/banen";
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
            "Toegang tot Lobsy met Microsoft Entra, Google of een Lobsy-/demo-account.",
            "Kies een inlogmethode. Externe login stuurt je naar de provider en terug naar Lobsy. Lokale/demo-accounts gebruiken e-mail en wachtwoord.",
            "Sessie starten zodat je sollicitaties, vacatures, tokens of beheer kunt gebruiken."),

        ["/register"] = new(
            "Bedrijf registreren",
            "Nieuwe werkgever of intermediair aanmelden via KVK-gegevens.",
            "Vul contact- en KVK-/vestigingsgegevens in. Na indienen volgt activatie (e-mail) en eventueel controle/overname als de vestiging al bestaat.",
            "Een organisatie-account opzetten om vacatures te plaatsen en tokens te beheren."),

        ["/register/activate"] = new(
            "Account activeren",
            "Bevestigen van een registratie via activatielink.",
            "Open de link uit de e-mail (of demo-link). Daarna is het account actief of volgt een overnameproces.",
            "Registratie afronden zodat managers kunnen inloggen."),

        ["/access-denied"] = new(
            "Geen toegang",
            "Je hebt deze pagina geopend zonder de juiste rol of rechten.",
            "Ga terug naar Home of log in met een account dat wél toegang heeft.",
            "Voorkomen dat gevoelige beheer- of werkgeversfuncties per ongeluk openstaan."),

        ["/home"] = new(
            "Home / dashboard",
            "Startscherm na inloggen, afgestemd op jouw rol.",
            "Je ziet kerncijfers bovenaan en KPI’s gegroepeerd in categorieën (groei, engagement, marketing, systeem). Kies een periode, open een categorie en klik een tegel voor drilldown.",
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
            "Vul interesses, opleiding, rijbewijzen, voorkeuren, locatie, uren per week en beschikbaarheid/dagdelen in (of tijden in overleg). Geboortedatum is nodig voor leeftijdsloon en wettelijke taakchecks. Wijzigingen verbeteren matchscores en filters.",
            "Betere matches en sneller solliciteren met volledige gegevens."),

        ["/employer/vacancies"] = new(
            "Vacatures (werkgever)",
            "Beheer van vacatures van jouw organisatie of vestiging, inclusief concepten uit CSV-import of API.",
            "Bekijk status en herkomst (Handmatig, CSV of API). Concepten publiceer je hier — daar wordt het tokenverbruik verwerkt. Afhankelijk van rol kun je publiceren, pauzeren of nieuwe vacatures plaatsen.",
            "Openstaande banen beheren, importeren afronden en opvolgen."),

        ["/branch/vacancies"] = new(
            "Vacatures (vestiging)",
            "Beheer van vacatures van jouw vestiging, inclusief concepten.",
            "Bekijk status, publiceer of pauzeer. Bij publiceren kies je tokenproducten (basis, highlight, PushBom, verlengen).",
            "Lokale banen beheren en opvolgen."),

        ["/employer/tokens"] = new(
            "Tokens",
            "Token-saldo, aankoop en allocatie binnen de organisatie.",
            "Bekijk wallet/saldo, koop een pakket via Mollie, of wijs tokens toe aan vestigingen. Logs tonen mutaties.",
            "Vacaturepublicatie en andere token-acties bekostigen."),

        ["/branch/tokens"] = new(
            "Tokens (vestiging)",
            "Token-saldo en aankoop voor jouw vestiging.",
            "Zelfde tokenflow als bij werkgever, gericht op branch-niveau.",
            "Zorgen dat er saldo is om vacatures te plaatsen."),

        ["/employer/branches"] = new(
            "Vestigingen",
            "Vestigingen onder jouw organisatie beheren.",
            "Bekijk vestigingen, zoek via KVK-stub nieuwe vestigingen en registreer ze. Overnames lopen via een apart scherm.",
            "Organisatiestructuur opbouwen zodat managers per vestiging kunnen werken."),

        ["/regional/branches"] = new(
            "Mijn vestigingen (regio)",
            "Vestigingen binnen jouw regiobereik.",
            "Inzicht in gekoppelde vestigingen; beheer hangt af van rechten.",
            "Regionale sturing over meerdere locaties."),

        ["/employer/regions"] = new(
            "Regio’s",
            "Regio-indeling van de enterprise-organisatie.",
            "Bekijk of bewerk regio’s en koppelingen die regiomanagers gebruiken.",
            "Schaalbare structuur voor meerdere vestigingen."),

        ["/employer/users"] = new(
            "Gebruikers (organisatie)",
            "Managers en uitnodigingen binnen het bedrijf.",
            "Nodig gebruikers uit per e-mail, bekijk rollen en beheer toegang tot vestigingen/regio’s.",
            "Het juiste team toegang geven tot vacatures en tokens."),

        ["/employer/company"] = new(
            "Bedrijfsgegevens",
            "Beheer hier de kerninstellingen van je organisatie of vestiging: contactvoorkeur voor kandidaten, CSV Batch Import en de externe API-koppeling.",
            "Kies bovenaan de juiste organisatie/vestiging. Onder Overzicht zie je adres, KVK, tokens en actieve vacatures. Bij Contactvoorkeur geef je aan of kandidaten na sollicitatie mail, telefoon of WhatsApp mogen gebruiken (niet zichtbaar op de openbare vacaturepagina) en sla je de gegevens op. Schakel CSV Batch Import in om de tab CSV Import te tonen — vacatures komen binnen als concept. Bij API-koppeling zie je endpoint, X-API-Key-header en een link naar Swagger (request/response). Genereer of e-mail een API-key; de volledige sleutel is één keer zichtbaar. Publiceren (en tokens) doe je daarna onder Vacatures.",
            "Organisatie veilig bereikbaar maken voor kandidaten, batch-CSV en ATS/partners."),

        ["/employer/csv-import"] = new(
            "CSV Import",
            "Veilige batch-import van vacatures via een CSV-bestand voor jouw organisatie.",
            "Lees de How-to voor verplichte kolommen (titel, omschrijving, data’s, branches, salaristabel-id). Upload een .csv (komma of puntkomma) via slepen of bladeren. Afbeeldingen mag je als URL of Base64 in de kolom afbeelding zetten. Elke rij wordt strikt gevalideerd: geldige rijen worden concept-vacatures, ongeldige blijven staan met een foutmelding. Corrigeer mislukte rijen inline en klik Opnieuw aanbieden. Publiceer geslaagde concepten daarna via Vacatures (tokenverwerking).",
            "Snel en controleerbaar veel vacatures aanmaken zonder blind foute data in te lezen."),

        ["/employer/salary-tables"] = new(
            "Salaristabellen / CAO",
            "Loontabellen die aan vacatures of vestigingen gekoppeld kunnen worden.",
            "Maak of open tabellen, beheer schalen/bedragen en koppel waar nodig aan branches.",
            "Consistente beloning tonen en WML-/CAO-afspraken ondersteunen."),

        ["/employer/takeovers"] = new(
            "Overnameverzoeken",
            "Conflicten wanneer een vestiging al geregistreerd is.",
            "Bekijk openstaande overnames en keur goed of af. Goedkeuring kan org-structuur samenvoegen.",
            "Dubbele KVK-vestigingen netjes laten claimen door de juiste partij."),

        ["/employer/onboarding-checkout"] = new(
            "Onboarding-betaling",
            "Eerstejaars of onboarding-checkout voor werkgevers.",
            "Rond de stub-betaling af zodat onboarding verder kan.",
            "Account/organisatie activeren voor productiegebruik."),

        ["/branch/vacancies/new"] = new(
            "Vacature plaatsen",
            "Nieuwe vacature aanmaken voor een vestiging.",
            "Vul titel, eisen, loon, locatie, uren/week, roosters/dagdelen en de wettelijke taakvinkjes (i-knoppen) in. Die vinkjes sturen automatisch of 15–17-jarigen mogen solliciteren. Publiceren kan tokens kosten en kan door moderatie gaan.",
            "Banen zichtbaar maken op de banenkaart voor kandidaten."),

        ["/branch/applicants"] = new(
            "Sollicitanten",
            "Binnenkomende sollicitaties op jouw vacatures.",
            "Filter en open kandidaten, bekijk matchscore/status en vervolgstappen. Contactgegevens (PII) verschijnen pas na acceptatie of contactstatus.",
            "Selectie en opvolging van sollicitaties door managers."),

        ["/employer/organization"] = new(
            "Organisatie",
            "Desktop-hub voor zwaar organisatiebeheer.",
            "Open vestigingen, regio’s, salaristabellen, bedrijfsgegevens, CSV-import en overnames. Op mobiel zie je een desktop-melding.",
            "Structuur en masterdata van de organisatie beheren zonder de mobiele ops-nav te overbelasten."),

        ["/regional/tokens"] = new(
            "Tokencontrole (regio)",
            "Centrale controle op tokens en vacatures in de regio.",
            "Bekijk saldo’s/gebruik over vestigingen heen (vaak meer inzicht dan bewerken).",
            "Regionale sturing zonder per vestiging te hoeven inloggen."),

        ["/intermediary"] = new(
            "Bedrijvenoverzicht",
            "Prestaties per gekoppelde opdrachtgever voor intermediairs.",
            "Bekijk vacatures, openstaande sollicitaties, conversie, tokens, boosts en snelle acties per bedrijf.",
            "Stuur op gezondheid per opdrachtgever zonder vestigingen of regio’s."),

        ["/intermediary/team"] = new(
            "Team (intermediair)",
            "Collega’s binnen jouw intermediair-organisatie.",
            "Nodig teamleden uit en bekijk wie toegang heeft tot opdrachtgevers en vacatures.",
            "Samenwerken zonder accounts buiten je organisatie te delen."),

        ["/salesmanager"] = new(
            "Salesdashboard",
            "Overzicht van trackingcode, referrals en commissiesaldo.",
            "Bekijk je performance, open toolkit of referrals en rond onboarding af als dat nog openstaat.",
            "Snel zien waar je staat in acquisitie en uitbetaling."),

        ["/salesmanager/toolkit"] = new(
            "Sales-toolkit",
            "Materialen en links om ondernemers te werven met jouw trackingcode.",
            "Kopieer je partnerlink, deel materialen en volg hoe prospects instappen.",
            "Acquisitie versnellen met consistente Lobsy-boodschap."),

        ["/salesmanager/referrals"] = new(
            "Sales-aanbevelingen",
            "Nieuwe salesmanagers aandragen (tier-afhankelijk).",
            "Deel referral-opties en volg wie via jou is aangemeld.",
            "Netwerk laten meegroeien binnen de commissiestructuur."),

        ["/partner"] = new(
            "Partner / tracking",
            "Publieke landingspagina via sales-trackingcode.",
            "Prospects komen hier via een saleslink en starten registratie of oriëntatie.",
            "Salesmanagers koppelen acquisitie aan hun trackingcode."),

        ["/salesmanager/onboarding"] = new(
            "Sales onboarding",
            "Profiel en gegevens van de salesmanager afronden.",
            "Vul verplichte velden (o.a. bedrijfs-/factuurgegevens) in tot onboarding compleet is.",
            "Klaarzetten voor facturatie en uitbetalingen."),

        ["/salesmanager/invoices"] = new(
            "Facturen (sales)",
            "Self-billing / factuuroverzicht voor salesmanagers. Kies zelf het uitbetalingsbedrag; download facturen als PDF.",
            "Bekijk of download facturen gekoppeld aan uitbetalingen.",
            "Administratie van commissies of uitbetalingen."),

        ["/salesmanager/payout-checkout"] = new(
            "Uitbetaling",
            "Uitbetalingstraject (Mollie-stub) voor salesmanagers.",
            "Start de checkout-stub; daarna volgt self-billing/documentatie in het platform.",
            "Verdiensten laten uitbetalen volgens het salesproces."),

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

        ["/admin/companies"] = new(
            "Beheer · Bedrijven",
            "Alle werkgevers en intermediairs op het platform.",
            "Zoek/filter bedrijven, ken tokens toe, of voeg toe via KVK-stub.",
            "Platformbeheer van organisatiestructuur en wallets."),

        ["/admin/users"] = new(
            "Beheer · Gebruikers",
            "Accounts van kandidaten en managers.",
            "Zoek gebruikers, bekijk rollen en beheer toegang waar nodig.",
            "Support en beheer van inloggerechtigde personen."),

        ["/admin/vacancies"] = new(
            "Beheer · Vacatures",
            "Platformbreed vacatureoverzicht.",
            "Zoek en open vacatures over alle bedrijven heen.",
            "Moderatie, support en kwaliteitscontrole."),

        ["/admin/finance"] = new(
            "Beheer · Financieel",
            "Financieel overzicht van het platform.",
            "Bekijk relevante geld-/tokenstromen en rapportages.",
            "Inzicht voor exploitatie en controle."),

        ["/admin/token-finance"] = new(
            "Beheer · Tokenfinance",
            "Detail van tokenstromen en financiële mutaties.",
            "Analyseer aankopen, grants en correcties in samenhang met finance.",
            "Controle en reconciliatie van tokens versus betalingen."),

        ["/admin/tokens"] = new(
            "Beheer · Tokens",
            "Centrale tokenadministratie.",
            "Ken tokens toe aan bedrijven en bekijk saldi.",
            "Demo’s, credits of correcties uitvoeren."),

        ["/admin/sales"] = new(
            "Beheer · Sales commercieel",
            "Commerciële salesinstellingen en overzicht.",
            "Beheer sales-gerelateerde platforminstellingen en rapportages.",
            "Saleskanaal en commissiestructuur ondersteunen."),

        ["/admin/sales-managers"] = new(
            "Beheer · Salesmanagers",
            "Salesmanager-accounts en status.",
            "Beheer onboarding, koppelingen en overzicht van salesmanagers.",
            "Het saleskanaal operationeel houden."),

        ["/admin"] = new(
            "Beheer · Start",
            "Ingang tot het admin-domein.",
            "Ga via de navigatie naar vacatures, finance, bedrijven of settings.",
            "Platformbeheer starten."),

        ["/admin/moderation"] = new(
            "Beheer · Moderatie",
            "Content- en vacaturemoderatie.",
            "Bekijk gemarkeerde of te beoordelen vacatureteksten (o.a. via OpenAI-moderatie).",
            "Ongewenste of risicovolle content tegenhouden."),

        ["/admin/settings"] = new(
            "Beheer · Instellingen",
            "Systeeminstellingen, platformfeatures en inactiviteitsperiode.",
            "Zet features aan/uit (moderatie, authenticator, …), stel de inactieve periode in voor de eenmalige “We missen je”-mail (standaard 120 dagen), en beheer integraties.",
            "Gedrag van Lobsy afstemmen zonder code-deploys."),

        ["/admin/company"] = new(
            "Beheer · Bedrijfsgegevens",
            "NAW, KvK en BTW van Lobsy.",
            "Vul bedrijfsnaam, slogan, adres, KvK en BTW in. Deze gegevens staan onderaan self-billing factuur-PDF’s.",
            "Juridische platformgegevens op facturen houden."),

        ["/admin/about"] = new(
            "Beheer · Wie zijn wij",
            "Publieke ‘Wie zijn wij’-pagina bewerken.",
            "Pas titel, introregel en inhoud aan. Gebruik koppen voor secties. De pagina is zichtbaar via /wie-zijn-wij.",
            "Het verhaal achter Lobsy up-to-date houden zonder code-deploys."),

        ["/admin/marketing-flyer"] = new(
            "Beheer · Werkgeversflyer",
            "Professionele A4-flyer voor werkgevers bewerken en afdrukken.",
            "Pas koppen, USP’s, lanceringsteksten en QR-doel aan. Download de PDF om te printen of digitaal te delen.",
            "Werkgevers overtuigen met een logo-first flyer zonder designbureau."),

        ["/admin/mail-test"] = new(
            "Beheer · Mailtest",
            "Elk transactioneel mailtype als test versturen naar een adres naar keuze.",
            "Vul een e-mailadres in en verstuur één type of alle types. De HTML is dezelfde als productie; knoppen linken naar echte Lobsy-pagina’s. OTP’s en wachtwoorden in testmails zijn voorbeelden en activeren geen accountactie.",
            "Visueel en functioneel nalopen van alle uitgaande mails zonder echte gebruikers te mailen."),

        ["/admin/integrations"] = new(
            "Beheer · Integraties",
            "API-koppelingen (Mollie, KVK, Entra, Google, Mail, OpenAI).",
            "Vul credentials in, sla op en test de verbinding. Gebruik de i per tegel voor details.",
            "Externe diensten laten werken voor login, mail, betalen en moderatie."),

        ["/admin/api-keys"] = new(
            "Beheer · API Beheer",
            "Overzicht van alle bedrijfs-API-keys (actief/inactief).",
            "Bekijk prefix, laatste gebruik en deactiveer keys direct bij incidenten. Plaintext keys zijn nooit zichtbaar voor admins.",
            "Platformbrede controle over externe vacature-API-toegang."),

        ["/admin/masterdata"] = new(
            "Beheer · Masterdata",
            "Keuzelijsten zoals branches, rijbewijzen, opleidingen en minimum werkgevers.",
            "Voeg opties toe, wijzig of deactiveer ze. Ze verschijnen in profiel- en vacatureformulieren.",
            "Consistente keuzes in de hele app zonder hardcoding."),

        ["/admin/wages"] = new(
            "Beheer · Salaris / WML",
            "Wettelijk minimumloon en loonreferenties.",
            "Beheer WML-gegevens die vacatures en validatie ondersteunen.",
            "Compliant lonen tonen en controleren."),

        ["/admin/notifications"] = new(
            "Beheer · Notificaties",
            "Platformnotificaties en berichtenverkeer.",
            "Bekijk of beheer notificatie-instellingen/-logs binnen het admin-domein.",
            "Communicatie naar gebruikers volgen."),

        ["/admin/logging"] = new(
            "Beheer · Logging",
            "Platformlogboek voor audits en fouten.",
            "Filter en bekijk logregels (o.a. betalingen, overnames, systeemevents).",
            "Problemen analyseren en acties nalopen."),

        ["/admin/feedback"] = new(
            "Beheer · Feedback",
            "Binnengekomen bugs, errors en featurewensen met screenshot en metadata.",
            "Bekijk het datagrid, maak een functionele prompt en start een Cursor-taak. De PR-link verschijnt automatisch zodra de agent klaar is.",
            "Visuele/functionele feedback omzetten in een geautomatiseerde fix-PR."),

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

        ("/home/metrics/", new(
            "Metric detail",
            "Uitgesplitste cijfers achter een dashboard-KPI.",
            "Bekijk de onderliggende lijst of grafiek bij de gekozen metric-key.",
            "Dieper analyseren waarom een KPI zo staat.")),

        ("/partner/", Exact["/partner"]),

        ("/employer/salary-tables/", Exact["/employer/salary-tables"])
    ];
}
