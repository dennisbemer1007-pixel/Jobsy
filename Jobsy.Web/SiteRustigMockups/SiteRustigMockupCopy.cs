namespace Jobsy.Web.SiteRustigMockups;

/// <summary>NL/EN copy for static PO mockups (no production i18n).</summary>
public static class SiteRustigMockupCopy
{
    public static bool IsEn(string? lang) =>
        string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase);

    public static string LangLabel(string? lang) => IsEn(lang) ? "EN" : "NL";

    public static string PartnersTitle(string? lang) => IsEn(lang)
        ? "Find staff nearby."
        : "Vind personeel dichtbij.";

    public static string PartnersLead(string? lang) => IsEn(lang)
        ? "Lobsy shows your vacancy to people nearby, by bike, public transport or car time. You only pay when you publish a vacancy."
        : "Lobsy laat je vacature zien aan mensen in de buurt, op fiets-, OV- of autotijd. Je betaalt alleen als je een vacature plaatst.";

    public static string GratisDnaTitle(string? lang) => IsEn(lang)
        ? "Discover your work DNA for free"
        : "Ontdek gratis je werk-DNA";

    public static string GratisDnaLead(string? lang) => IsEn(lang)
        ? "Twenty short questions on cream, no account needed. You see how you work, what energises you and what matters to you."
        : "Twintig korte vragen op cream, zonder account. Je ziet hoe je werkt, wat je energie geeft en wat voor jou telt.";

    public static string RegisterTitle(string? lang) => IsEn(lang)
        ? "Register company"
        : "Bedrijf registreren";

    public static string RegisterLead(string? lang) => IsEn(lang)
        ? "Search your company in the KVK register to get started."
        : "Zoek je bedrijf in het KVK-register om te starten.";

    public static string TermsTitle(string? lang) => IsEn(lang)
        ? "Terms and conditions"
        : "Algemene voorwaarden";

    public static string TermsLead(string? lang) => IsEn(lang)
        ? "For employers using Lobsy. Available in Dutch and English."
        : "Voor werkgevers die Lobsy gebruiken. Beschikbaar in het Nederlands en Engels.";

    public static string A11yTitle(string? lang) => IsEn(lang)
        ? "Accessibility"
        : "Toegankelijkheid";

    public static string A11yLead(string? lang) => IsEn(lang)
        ? "We want Lobsy to work for everyone."
        : "We willen dat Lobsy voor iedereen werkt.";

    public static string StartTitle(string? lang, string name) => IsEn(lang)
        ? $"Hi {name}, nice to have you here"
        : $"Hoi {name}, fijn dat je er bent";

    public static string StartLead(string? lang) => IsEn(lang)
        ? "In about six minutes we build your starting profile together. Then you see on your Compass who you are and which work fits you."
        : "In ongeveer zes minuten maken we samen je startprofiel. Daarna zie je op je Kompas wie je bent en welk werk bij je past.";
}
