namespace Jobsy.Web.Localization;

/// <summary>
/// Role-specific “Hoe werkt Lobsy” copy. Missing translations fall back to Dutch via <see cref="UiStrings"/>.
/// </summary>
internal static class UiStringsHowLobsyRoles
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        Merge(nl, Nl());
        Merge(en, En());
        // Keep catalog key parity across languages (LocalizationTests).
        // Temporary Dutch copy until dedicated translations land.
        Merge(pl, Nl());
        Merge(ro, Nl());
        Merge(ar, Nl());
    }

    private static void Merge(Dictionary<string, string> target, Dictionary<string, string> extra)
    {
        foreach (var (key, value) in extra)
        {
            target[key] = value;
        }
    }

    private static Dictionary<string, string> Nl() => new(StringComparer.OrdinalIgnoreCase)
    {
        // —— Branch manager ——
        ["HowLobsy.Branch.Title"] = "Hoe werkt Lobsy voor jouw vestiging?",
        ["HowLobsy.Branch.Lead"] = "Van dashboard tot sollicitant — zo werf je lokaal met tokens.",
        ["HowLobsy.Branch.Step1Title"] = "1. Start op je dashboard",
        ["HowLobsy.Branch.Step1Body"] = "Open {0} voor KPI’s van jouw vestiging: actieve vacatures, sollicitaties, clicks en meer. Kies een periode en klik een tegel voor detail.",
        ["HowLobsy.Branch.Step2Title"] = "2. Beheer en publiceer vacatures",
        ["HowLobsy.Branch.Step2Body"] = "Onder {0} maak je concepten of publiceer je banen. Bij publiceren kies je basis, highlight, PushBom of verlengen — dat kost tokens. Bij te weinig saldo kan publicatie op goedkeuring wachten.",
        ["HowLobsy.Branch.Step3Title"] = "3. Beoordeel sollicitanten",
        ["HowLobsy.Branch.Step3Body"] = "Via vacatures of {0} zie je kandidaten. Contactgegevens (PII) verschijnen pas nadat je iemand accepteert of contact opneemt — privacy by design.",
        ["HowLobsy.Branch.Step4Title"] = "4. Houd je tokens in de gaten",
        ["HowLobsy.Branch.Step4Body"] = "Op {0} zie je saldo en verbruik. Zonder tokens publiceer of boost je niet — vraag bij je organisatie om bijvulling als dat nodig is.",
        ["HowLobsy.Branch.Step5Title"] = "5. Bedrijfsgegevens en overnames",
        ["HowLobsy.Branch.Step5Body"] = "Controleer {0} (adres, contactvoorkeur, eventueel API/CSV). Onder {1} behandel je overnameverzoeken van vestigingen.",
        ["HowLobsy.Branch.Step6Title"] = "6. Check hoe kandidaten je zien",
        ["HowLobsy.Branch.Step6Body"] = "Open de {0} om te zien hoe jouw vacatures op de kaart staan voor werkzoekenden in de buurt.",
        ["HowLobsy.Branch.PrimaryCta"] = "Naar vacatures",
        ["HowLobsy.Branch.SecondaryCta"] = "Naar dashboard →",

        // —— Regional manager ——
        ["HowLobsy.Regional.Title"] = "Hoe werkt Lobsy voor jouw regio?",
        ["HowLobsy.Regional.Lead"] = "Overzicht houden over vestigingen, vacatures en werving in jouw regio.",
        ["HowLobsy.Regional.Step1Title"] = "1. Regio-dashboard",
        ["HowLobsy.Regional.Step1Body"] = "Op {0} zie je KPI’s over de vestigingen in jouw regio. Gebruik periodefilters en drilldowns om trends te volgen.",
        ["HowLobsy.Regional.Step2Title"] = "2. Vacatures inzien en opvolgen",
        ["HowLobsy.Regional.Step2Body"] = "Onder {0} bekijk je vacatures en sollicitaties binnen je scope. Zo zie je waar vestigingen actief werven.",
        ["HowLobsy.Regional.Step3Title"] = "3. Jouw vestigingen",
        ["HowLobsy.Regional.Step3Body"] = "Via {0} open je het overzicht van gekoppelde vestigingen en ga je gericht verder.",
        ["HowLobsy.Regional.Step4Title"] = "4. Tokens en bereik",
        ["HowLobsy.Regional.Step4Body"] = "Op {0} zie je tokenbewegingen die relevant zijn voor jouw regio. Publiceren en boosts lopen via tokens.",
        ["HowLobsy.Regional.Step5Title"] = "5. Banenkaart als spiegel",
        ["HowLobsy.Regional.Step5Body"] = "De {0} toont hoe kandidaten openstaande banen in de regio vinden — handig om dekking te checken.",
        ["HowLobsy.Regional.PrimaryCta"] = "Naar mijn vestigingen",
        ["HowLobsy.Regional.SecondaryCta"] = "Naar dashboard →",

        // —— Enterprise manager ——
        ["HowLobsy.Enterprise.Title"] = "Hoe werkt Lobsy voor jouw organisatie?",
        ["HowLobsy.Enterprise.Lead"] = "Organisatiebreed werven: vacatures, tokens, vestigingen, regio’s en gebruikers.",
        ["HowLobsy.Enterprise.Step1Title"] = "1. Bedrijfsdashboard",
        ["HowLobsy.Enterprise.Step1Body"] = "Start op {0} met kerncijfers over groei, engagement en activiteit. Klik tegels voor drilldown.",
        ["HowLobsy.Enterprise.Step2Title"] = "2. Vacatures en publicatie",
        ["HowLobsy.Enterprise.Step2Body"] = "Beheer alle vacatures onder {0}. Je kunt publiceren, pauzeren en waar nodig publicaties goedkeuren die op tokens wachten.",
        ["HowLobsy.Enterprise.Step3Title"] = "3. Tokenpot en uitgifte",
        ["HowLobsy.Enterprise.Step3Body"] = "Koop en verdeel tokens via {0}. Stel vestiging-opt-in in, volg logs en zorg dat teams kunnen publiceren zonder stilstand.",
        ["HowLobsy.Enterprise.Step4Title"] = "4. Vestigingen en regio’s",
        ["HowLobsy.Enterprise.Step4Body"] = "Beheer {0} en {1}. Zo blijft de organisatiestructuur kloppen voor rechten, tokens en rapportage.",
        ["HowLobsy.Enterprise.Step5Title"] = "5. Gebruikers uitnodigen",
        ["HowLobsy.Enterprise.Step5Body"] = "Nodig collega’s uit onder {0} en geef de juiste rol (vestiging, regio of enterprise).",
        ["HowLobsy.Enterprise.Step6Title"] = "6. Salaristabellen en bedrijfsgegevens",
        ["HowLobsy.Enterprise.Step6Body"] = "Houd {0} actueel voor vacaturelonen. Onder {1} regel je KvK-gegevens, contactvoorkeur en eventueel CSV-import of API-koppeling.",
        ["HowLobsy.Enterprise.PrimaryCta"] = "Naar vacatures",
        ["HowLobsy.Enterprise.SecondaryCta"] = "Naar dashboard →",

        // —— Intermediary ——
        ["HowLobsy.Intermediary.Title"] = "Hoe werkt Lobsy als intermediair?",
        ["HowLobsy.Intermediary.Lead"] = "Werven voor meerdere opdrachtgevers: dashboard, batch-publicatie en tokens.",
        ["HowLobsy.Intermediary.Step1Title"] = "1. Jouw KPI-dashboard",
        ["HowLobsy.Intermediary.Step1Body"] = "Op {0} zie je resultaten over je gekoppelde opdrachtgevers: vacatures, sollicitaties, clicks en engagement.",
        ["HowLobsy.Intermediary.Step2Title"] = "2. Opdrachtgevers beheren",
        ["HowLobsy.Intermediary.Step2Body"] = "Onder {0} zie je gekoppelde bedrijven, actieve vacatures en tokensaldo per opdrachtgever.",
        ["HowLobsy.Intermediary.Step3Title"] = "3. Vacatures per opdrachtgever",
        ["HowLobsy.Intermediary.Step3Body"] = "Publiceer en beheer banen via {0}. Kies het juiste bedrijf/locatie en de juiste token-opties bij publicatie.",
        ["HowLobsy.Intermediary.Step4Title"] = "4. Batch-tool voor meerdere locaties",
        ["HowLobsy.Intermediary.Step4Body"] = "Met de {0} zet je snel vacatures uit op meerdere locaties — ideaal voor volume-werving.",
        ["HowLobsy.Intermediary.Step5Title"] = "5. Tokens",
        ["HowLobsy.Intermediary.Step5Body"] = "Houd saldo’s bij via {0}. Zonder tokens geen publicatie of boost.",
        ["HowLobsy.Intermediary.Step6Title"] = "6. Check de banenkaart",
        ["HowLobsy.Intermediary.Step6Body"] = "Bekijk op de {0} hoe kandidaten jouw geplaatste vacatures vinden.",
        ["HowLobsy.Intermediary.PrimaryCta"] = "Naar batch-tool",
        ["HowLobsy.Intermediary.SecondaryCta"] = "Naar dashboard →",

        // —— Sales manager ——
        ["HowLobsy.Sales.Title"] = "Hoe werkt Lobsy als salesmanager?",
        ["HowLobsy.Sales.Lead"] = "Van onboarding tot commissie — zo werf je ondernemers met jouw trackingcode.",
        ["HowLobsy.Sales.Step1Title"] = "1. Rond je onboarding af",
        ["HowLobsy.Sales.Step1Body"] = "Vul onder {0} KvK, BTW en NAW in en onderteken de overeenkomst. Daarna ontvang je jouw unieke trackingcode.",
        ["HowLobsy.Sales.Step2Title"] = "2. Gebruik de sales-toolkit",
        ["HowLobsy.Sales.Step2Body"] = "In de {0} vind je je partnerlink, PDF-flyer en deelknoppen (mail/WhatsApp) met jouw code.",
        ["HowLobsy.Sales.Step3Title"] = "3. Deel de partnerpagina",
        ["HowLobsy.Sales.Step3Body"] = "Stuur ondernemers naar jouw persoonlijke {0}. Zij registreren met jouw code; jij bouwt pipeline en commissie op.",
        ["HowLobsy.Sales.PartnerLabel"] = "partnerpagina",
        ["HowLobsy.Sales.Step4Title"] = "4. Volg resultaat op je dashboard",
        ["HowLobsy.Sales.Step4Body"] = "Op {0} zie je trackingcode, gekoppelde ondernemers, saldo en openstaande commissies.",
        ["HowLobsy.Sales.Step5Title"] = "5. Facturen en uitbetaling",
        ["HowLobsy.Sales.Step5Body"] = "Vraag uitbetaling aan via {0} (self-billing). Na betaling download je de factuur als PDF.",
        ["HowLobsy.Sales.PrimaryCta"] = "Naar sales-toolkit",
        ["HowLobsy.Sales.SecondaryCta"] = "Naar dashboard →",
    };

    private static Dictionary<string, string> En() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["HowLobsy.Branch.Title"] = "How does Lobsy work for your branch?",
        ["HowLobsy.Branch.Lead"] = "From dashboard to applicants — hire locally with tokens.",
        ["HowLobsy.Branch.Step1Title"] = "1. Start on your dashboard",
        ["HowLobsy.Branch.Step1Body"] = "Open {0} for your branch KPIs: active vacancies, applications, clicks and more. Pick a period and tap a tile for detail.",
        ["HowLobsy.Branch.Step2Title"] = "2. Manage and publish vacancies",
        ["HowLobsy.Branch.Step2Body"] = "Under {0} you create drafts or publish jobs. When publishing you choose basic, highlight, PushBom or extend — that costs tokens. With a low balance, publishing may wait for approval.",
        ["HowLobsy.Branch.Step3Title"] = "3. Review applicants",
        ["HowLobsy.Branch.Step3Body"] = "Via vacancies or {0} you see candidates. Contact details (PII) appear only after you accept someone or start contacting them — privacy by design.",
        ["HowLobsy.Branch.Step4Title"] = "4. Watch your tokens",
        ["HowLobsy.Branch.Step4Body"] = "On {0} you see balance and spend. Without tokens you cannot publish or boost — ask your organisation to top up when needed.",
        ["HowLobsy.Branch.Step5Title"] = "5. Company details and takeovers",
        ["HowLobsy.Branch.Step5Body"] = "Check {0} (address, contact preference, optional API/CSV). Under {1} you handle branch takeover requests.",
        ["HowLobsy.Branch.Step6Title"] = "6. See how candidates see you",
        ["HowLobsy.Branch.Step6Body"] = "Open the {0} to see how your vacancies appear on the map for nearby job seekers.",
        ["HowLobsy.Branch.PrimaryCta"] = "Go to vacancies",
        ["HowLobsy.Branch.SecondaryCta"] = "Go to dashboard →",

        ["HowLobsy.Regional.Title"] = "How does Lobsy work for your region?",
        ["HowLobsy.Regional.Lead"] = "Keep an overview of branches, vacancies and hiring in your region.",
        ["HowLobsy.Regional.Step1Title"] = "1. Region dashboard",
        ["HowLobsy.Regional.Step1Body"] = "On {0} you see KPIs across branches in your region. Use period filters and drilldowns to follow trends.",
        ["HowLobsy.Regional.Step2Title"] = "2. Review vacancies",
        ["HowLobsy.Regional.Step2Body"] = "Under {0} you review vacancies and applications in your scope — where branches are actively hiring.",
        ["HowLobsy.Regional.Step3Title"] = "3. Your branches",
        ["HowLobsy.Regional.Step3Body"] = "Via {0} open the list of linked branches and drill into the right location.",
        ["HowLobsy.Regional.Step4Title"] = "4. Tokens and reach",
        ["HowLobsy.Regional.Step4Body"] = "On {0} you see token activity relevant to your region. Publishing and boosts run on tokens.",
        ["HowLobsy.Regional.Step5Title"] = "5. Job map as a mirror",
        ["HowLobsy.Regional.Step5Body"] = "The {0} shows how candidates find open jobs in the region — useful to check coverage.",
        ["HowLobsy.Regional.PrimaryCta"] = "Go to my branches",
        ["HowLobsy.Regional.SecondaryCta"] = "Go to dashboard →",

        ["HowLobsy.Enterprise.Title"] = "How does Lobsy work for your organisation?",
        ["HowLobsy.Enterprise.Lead"] = "Hire across the organisation: vacancies, tokens, branches, regions and users.",
        ["HowLobsy.Enterprise.Step1Title"] = "1. Company dashboard",
        ["HowLobsy.Enterprise.Step1Body"] = "Start on {0} with core figures for growth, engagement and activity. Tap tiles for drilldown.",
        ["HowLobsy.Enterprise.Step2Title"] = "2. Vacancies and publishing",
        ["HowLobsy.Enterprise.Step2Body"] = "Manage all vacancies under {0}. You can publish, pause and approve publishes that are waiting on tokens.",
        ["HowLobsy.Enterprise.Step3Title"] = "3. Token pool and allocation",
        ["HowLobsy.Enterprise.Step3Body"] = "Buy and allocate tokens via {0}. Set branch opt-in, follow logs and keep teams able to publish.",
        ["HowLobsy.Enterprise.Step4Title"] = "4. Branches and regions",
        ["HowLobsy.Enterprise.Step4Body"] = "Manage {0} and {1} so structure stays correct for permissions, tokens and reporting.",
        ["HowLobsy.Enterprise.Step5Title"] = "5. Invite users",
        ["HowLobsy.Enterprise.Step5Body"] = "Invite colleagues under {0} and assign the right role (branch, region or enterprise).",
        ["HowLobsy.Enterprise.Step6Title"] = "6. Salary tables and company details",
        ["HowLobsy.Enterprise.Step6Body"] = "Keep {0} up to date for vacancy wages. Under {1} manage company details, contact preference and optional CSV import or API.",
        ["HowLobsy.Enterprise.PrimaryCta"] = "Go to vacancies",
        ["HowLobsy.Enterprise.SecondaryCta"] = "Go to dashboard →",

        ["HowLobsy.Intermediary.Title"] = "How does Lobsy work for intermediaries?",
        ["HowLobsy.Intermediary.Lead"] = "Hire for multiple clients: dashboard, batch publishing and tokens.",
        ["HowLobsy.Intermediary.Step1Title"] = "1. Your KPI dashboard",
        ["HowLobsy.Intermediary.Step1Body"] = "On {0} you see results across linked clients: vacancies, applications, clicks and engagement.",
        ["HowLobsy.Intermediary.Step2Title"] = "2. Manage clients",
        ["HowLobsy.Intermediary.Step2Body"] = "Under {0} you see linked companies, active vacancies and token balance per client.",
        ["HowLobsy.Intermediary.Step3Title"] = "3. Vacancies per client",
        ["HowLobsy.Intermediary.Step3Body"] = "Publish and manage jobs via {0}. Pick the right company/location and token options when publishing.",
        ["HowLobsy.Intermediary.Step4Title"] = "4. Batch tool for multiple locations",
        ["HowLobsy.Intermediary.Step4Body"] = "With the {0} you roll out vacancies across locations quickly — ideal for volume hiring.",
        ["HowLobsy.Intermediary.Step5Title"] = "5. Tokens",
        ["HowLobsy.Intermediary.Step5Body"] = "Track balances via {0}. Without tokens there is no publish or boost.",
        ["HowLobsy.Intermediary.Step6Title"] = "6. Check the job map",
        ["HowLobsy.Intermediary.Step6Body"] = "On the {0} see how candidates find the vacancies you placed.",
        ["HowLobsy.Intermediary.PrimaryCta"] = "Go to batch tool",
        ["HowLobsy.Intermediary.SecondaryCta"] = "Go to dashboard →",

        ["HowLobsy.Sales.Title"] = "How does Lobsy work for sales managers?",
        ["HowLobsy.Sales.Lead"] = "From onboarding to commission — grow employers with your tracking code.",
        ["HowLobsy.Sales.Step1Title"] = "1. Finish onboarding",
        ["HowLobsy.Sales.Step1Body"] = "Under {0} enter Chamber of Commerce, VAT and address details and sign the agreement. Then you receive your unique tracking code.",
        ["HowLobsy.Sales.Step2Title"] = "2. Use the sales toolkit",
        ["HowLobsy.Sales.Step2Body"] = "In the {0} you find your partner link, PDF flyer and share buttons (mail/WhatsApp) with your code.",
        ["HowLobsy.Sales.Step3Title"] = "3. Share the partner page",
        ["HowLobsy.Sales.Step3Body"] = "Send employers to your personal {0}. They register with your code; you build pipeline and commission.",
        ["HowLobsy.Sales.PartnerLabel"] = "partner page",
        ["HowLobsy.Sales.Step4Title"] = "4. Track results on your dashboard",
        ["HowLobsy.Sales.Step4Body"] = "On {0} you see tracking code, linked entrepreneurs, balance and outstanding commissions.",
        ["HowLobsy.Sales.Step5Title"] = "5. Invoices and payout",
        ["HowLobsy.Sales.Step5Body"] = "Request payout via {0} (self-billing). After payment, download the invoice as PDF.",
        ["HowLobsy.Sales.PrimaryCta"] = "Go to sales toolkit",
        ["HowLobsy.Sales.SecondaryCta"] = "Go to dashboard →",
    };
}
