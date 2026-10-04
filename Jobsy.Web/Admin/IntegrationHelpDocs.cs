namespace Jobsy.Web.Admin;

/// <summary>Inline documentatie voor Admin → Integraties tegels.</summary>
public static class IntegrationHelpDocs
{
    public sealed record Doc(
        string Summary,
        string UsedFor,
        string WhereToGetKey,
        string? Tip = null,
        string? DocsUrl = null,
        string? DocsUrlLabel = null);

    public static Doc? TryGet(string key) => key?.Trim().ToLowerInvariant() switch
    {
        "mollie" => Mollie,
        "kvk" => Kvk,
        "microsoftentra" => MicrosoftEntra,
        "googleentra" or "google" => Google,
        "mail" => Mail,
        "openai" => OpenAi,
        _ => null
    };

    private static readonly Doc Mollie = new(
        Summary: "Betaaldienst voor iDEAL en creditcard (prepaid token-aankopen).",
        UsedFor: "Tokenpakketten kopen door werkgevers/intermediairs via iDEAL of creditcard. Met een opgeslagen API-key start checkout echte Mollie-betalingen; zonder key blijft Development op de lokale stub. Webhooks schrijven tokens direct bij na betaling.",
        WhereToGetKey: "Mollie Dashboard → Developers → API keys. Gebruik eerst een test-key (begint met test_), later live_ voor echte betalingen. Activeer iDEAL én creditcard in je Mollie-profiel.",
        Tip: "Base URL: https://api.mollie.com/v2/ (of leeg laten). Zet PublicApiBaseUrl / PublicWebBaseUrl goed zodat Mollie webhooks (/api/webhooks/mollie) en redirect-return werken — nodig voor instant creditcard/iDEAL top-ups.",
        DocsUrl: "https://my.mollie.com/dashboard/developers/api-keys",
        DocsUrlLabel: "Mollie API keys");

    private static readonly Doc Kvk = new(
        Summary: "Koppeling met het KvK Handelsregister (bedrijfs- en vestigingsgegevens).",
        UsedFor: "Opzoeken van KVK-nummers en vestigingen bij bedrijfsregistratie, admin ‘Bedrijven toevoegen’ en vestigingen toevoegen. Met API-key gaat Lobsy live naar KVK; zonder key blijft de demo-stub met vaste testnummers actief.",
        WhereToGetKey: "KVK Developer Portal → API-abonnement aanvragen (KVK-nummer + tekenbevoegd). Daarna: Mijn API-keys. Plak de key hier of zet Kvk__ApiKey / KVK_API_KEY op de API-service.",
        Tip: "Base URL leeg laten voor productie (https://api.kvk.nl/api/). Test-key: https://api.kvk.nl/test/api/. Niet de Zoeken-URL of developers.kvk.nl plakken. Na Opslaan: Test verbinding. Stub-demo’s (alleen zonder live key): 11223344, 55667788, 33445566.",
        DocsUrl: "https://developers.kvk.nl/nl/documentation/quickstart",
        DocsUrlLabel: "KVK API snelstart");

    private static readonly Doc MicrosoftEntra = new(
        Summary: "Microsoft-login via Entra ID (Azure AD) / OpenID Connect.",
        UsedFor: "De knop ‘Microsoft Entra’ op de loginpagina. Gebruikers loggen in met een Microsoft-account; Lobsy maakt (standaard) een kandidaat-sessie aan.",
        WhereToGetKey: "Azure Portal → Microsoft Entra ID → App-registraties → Nieuwe registratie. Neem Application (client) ID, maak een Client secret, en zet Tenant ID (of ‘common’). Redirect URI: https://JOUW-WEB-URL/signin-entra",
        Tip: "Redirect URI moet exact https://lobsy.nl/signin-entra zijn (of jouw web-URL). Credentials uit Integraties activeren de login-knop; vul ook Tenant ID in (of ‘common’). Env vars Authentication__Entra__… blijven optioneel als override.",
        DocsUrl: "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade",
        DocsUrlLabel: "Azure App-registraties");

    private static readonly Doc Google = new(
        Summary: "Google-login via OAuth 2.0.",
        UsedFor: "De knop ‘Google’ op de loginpagina voor kandidaten/gebruikers met een Google-account.",
        WhereToGetKey: "Google Cloud Console → eigen project → APIs & Services → OAuth consent screen, daarna Credentials → OAuth client ID (Web application). Redirect URI: https://JOUW-WEB-URL/signin-google",
        Tip: "Credentials uit Integraties activeren de login-knoppen automatisch. In Testing-modus moeten testusers op de consent screen staan.",
        DocsUrl: "https://console.cloud.google.com/apis/credentials",
        DocsUrlLabel: "Google Cloud Credentials");

    private static readonly Doc Mail = new(
        Summary: "Uitgaande e-mail. Mail__Provider kiest Resend of Lettermint.",
        UsedFor: "Registratie-activatiemail, sollicitatie-verificatiecodes, notificaties en overige platformmails.",
        WhereToGetKey: "Lettermint: lettermint.co → project-token als Lettermint__ApiKey (of LETTERMINT_API_KEY) en Mail__Provider=Lettermint. Resend: resend.com → API Keys, alleen als Mail__Provider=Resend. From: Mail__FromAddress op een geverifieerd domein.",
        Tip: "Ontbreekt de Lettermint-sleutel terwijl Mail__Provider=Lettermint staat, dan gaat er geen mail via Resend. De gezondheidscheck toont dan: Mail: niet ingesteld. De Lettermint-sleutel staat alleen in de omgeving, niet in dit formulier. Dit formulier blijft de Resend-sleutel. Op Acceptatie zet je Mail__AllowedRecipientPattern zodat alleen test-*@lobsy.nl (en extra adressen) mail krijgen.",
        DocsUrl: "https://lettermint.co/docs/api-reference/sending/send",
        DocsUrlLabel: "Lettermint Sending API");

    private static readonly Doc OpenAi = new(
        Summary: "AI-tekstmodellen. De actieve aanbieder komt uit Ai__Provider (OpenAI of Mistral).",
        UsedFor: "Vacaturetekst-moderatie en de coach in ‘Oefen je sollicitatiegesprek’. Zonder geldige sleutel voor de gekozen aanbieder blijft de AI uit.",
        WhereToGetKey: "Mistral: console.mistral.ai, sleutel als Mistral__ApiKey, en Ai__Provider=Mistral. OpenAI: platform.openai.com, alleen als Ai__Provider=OpenAI. Model en regio staan bovenaan deze pagina.",
        Tip: "Een ontbrekende Mistral-sleutel valt niet terug op OpenAI. Het model per onderdeel (verhaal, loopbaanrapport, kompas, coach) staat bovenaan en komt uit Mistral__Models__Story, Mistral__Models__CareerReport, Mistral__Models__Compass en Mistral__Models__Chat. Leeg betekent Mistral__Model. Het veld toont na Opslaan geen key terug (alleen gemaskeerd). Test leest de opgeslagen key uit de database — eerst Opslaan of laat Test auto-opslaan.",
        DocsUrl: "https://console.mistral.ai/",
        DocsUrlLabel: "Mistral console");
}
