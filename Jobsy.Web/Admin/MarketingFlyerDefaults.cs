using Jobsy.Web.Services;

namespace Jobsy.Web.Admin;

/// <summary>
/// Client-side flyer defaults. They match <c>MarketingFlyerSettingsService</c> so Opslaan is what persists a reset.
/// </summary>
public static class MarketingFlyerDefaults
{
    public const string Headline = "Lobsy. Vacatures die écht gezien worden.";
    public const string Subheadline = "Nieuw platform — samen met het Westland groot maken.";
    public const string Intro =
        "Hyper-lokaal werven zonder logge vacaturesites. Dichtbij, snel en betaalbaar — voor elke ondernemer.";
    public const string BulletPoints = """
        Vacatures zijn vele malen zichtbaarder dan op bestaande logge vacaturesites
        Stages en vrijwilligersbanen plaatsen is altijd gratis
        Vacatures plaatsen via een overzichtelijk tokensysteem — betaalbaar, óók voor de kleine ondernemer
        Duidelijk dashboard: zie direct waar successen en uitdagingen liggen
        Behulpzame Lobsy-bot die je onderweg ondersteunt
        Een vacature is zo geplaatst: handmatig, via CSV of via de API
        Tokens verdienen? Dat kan
        Matchen en afwijzen gaat makkelijker dan ooit
        Eigen flyers met QR-code naar jullie bedrijvenpagina — al je vacatures op één plek
        """;
    public const string PromoFreeText = "T/m 18 november 2026 is het gratis om vacatures te plaatsen.";
    public const string PromoDiscountText = "Tot 31 december 2026 krijg je 50% korting.";
    public const string CtaTitle = "Groei mee met Lobsy";
    public const string CtaBody =
        "Scan de QR-code en start vandaag — of ga naar lobsy.nl. Jouw bedrijvenpagina met alle vacatures is zo live.";
    public const string QrCaption = "Start op Lobsy";
    public const string QrPath = "/register";
    public const string FooterNote = "Lobsy · hyper-lokaal matchen in Westland & omgeving";

    public static MarketingFlyerItem CreateForm() => new()
    {
        Headline = Headline,
        Subheadline = Subheadline,
        Intro = Intro,
        BulletPoints = BulletPoints.Trim(),
        PromoFreeText = PromoFreeText,
        PromoDiscountText = PromoDiscountText,
        CtaTitle = CtaTitle,
        CtaBody = CtaBody,
        QrCaption = QrCaption,
        QrPath = QrPath,
        FooterNote = FooterNote,
        UpdatedAtUtc = null
    };
}
