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
        Summary: "Betaaldienst voor iDEAL en andere methoden.",
        UsedFor: "Tokenpakketten kopen door werkgevers/intermediairs (checkout). Op dit moment is de checkout in Lobsy nog een stub; de opgeslagen key wordt wel live getest tegen de Mollie API.",
        WhereToGetKey: "Mollie Dashboard → Developers → API keys. Gebruik eerst een test-key (begint met test_), later live_ voor echte betalingen.",
        Tip: "Base URL: https://api.mollie.com/v2/ (of leeg laten). Het API-key veld is na Opslaan bewust leeg; de gemaskeerde ‘Huidige API-key’ toont dat de key bewaard is.",
        DocsUrl: "https://my.mollie.com/dashboard/developers/api-keys",
        DocsUrlLabel: "Mollie API keys");

    private static readonly Doc Kvk = new(
        Summary: "Koppeling met het KvK Handelsregister (bedrijfs- en vestigingsgegevens).",
        UsedFor: "Opzoeken van KVK-nummers en vestigingen bij bedrijfsregistratie, admin ‘Bedrijven toevoegen’ en vestigingen toevoegen. Zonder live key gebruikt Lobsy een demo-stub met vaste testnummers.",
        WhereToGetKey: "KVK Developer Portal → API-abonnement aanvragen (KVK-nummer + tekenbevoegd). Daarna: Mijn API-keys.",
        Tip: "In de testfase kun je de stub laten staan — een live abonnement kost maandelijks + per bevraging. Stub-demo’s o.a.: 11223344, 55667788, 33445566.",
        DocsUrl: "https://developers.kvk.nl/nl/apply-for-apis",
        DocsUrlLabel: "KVK Developer Portal");

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
        Summary: "Uitgaande e-mail via SMTP (bijv. Gmail).",
        UsedFor: "Registratie-activatiemail, notificaties en overige platformmails.",
        WhereToGetKey: "Voor Gmail: Google-account → Beveiliging → 2-stapsverificatie aan → App-wachtwoorden. Plak dat als Client Secret. Client ID = je Gmail-adres. SMTP-host: smtp.gmail.com (poort 587).",
        Tip: "Base URL / SMTP-host is géén https:// — alleen de hostnaam. From-adres gelijk houden aan het Gmail-account. Opslaan vóór Test verbinding.",
        DocsUrl: "https://myaccount.google.com/apppasswords",
        DocsUrlLabel: "Gmail app-wachtwoorden");

    private static readonly Doc OpenAi = new(
        Summary: "OpenAI API voor tekstmodellen.",
        UsedFor: "Vacaturetekst-moderatie (ongepaste of risicovolle content markeren/blokkeren) én de interactieve coach in ‘Oefen je sollicitatiegesprek’ (met scripted fallback zonder key).",
        WhereToGetKey: "platform.openai.com → API keys → Create new secret key. Model bijv. gpt-4o-mini. Base URL leeg of https://api.openai.com/v1/",
        Tip: "Het veld toont na Opslaan geen key terug (alleen gemaskeerd). Test leest de opgeslagen key uit de database — eerst Opslaan of laat Test auto-opslaan.",
        DocsUrl: "https://platform.openai.com/api-keys",
        DocsUrlLabel: "OpenAI API keys");
}
