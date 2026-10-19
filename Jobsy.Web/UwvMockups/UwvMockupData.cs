namespace Jobsy.Web.UwvMockups;

public sealed record UwvKansberoep(
    string TitleNl,
    string TitleEn,
    string ProspectNl,
    string ProspectEn,
    string RouteNl,
    string RouteEn);

public sealed record UwvSimilarJob(
    string TitleNl,
    string TitleEn,
    string WhyNl,
    string WhyEn);

public sealed record UwvOmscholenSector(
    string NameNl,
    string NameEn,
    string DetailNl,
    string DetailEn,
    bool HasBaangarantie);

public sealed record UwvLeerbaan(
    string TitleNl,
    string TitleEn,
    string CompanyNl,
    string CompanyEn);

public sealed record UwvPartnerLink(
    string TitleNl,
    string TitleEn,
    string Url,
    string Provider);

public sealed record UwvDriverChip(
    string LabelNl,
    string LabelEn,
    string ExplainNl,
    string ExplainEn);

public static class UwvMockupData
{
    public static IReadOnlyList<UwvKansberoep> Kansberoepen { get; } =
    [
        new(
            "Logistiek medewerker",
            "Logistics associate",
            "Veel kans op werk in de regio",
            "Strong hiring outlook in the region",
            "MBO logistiek · BBL mogelijk",
            "MBO logistics · apprenticeship available"),
        new(
            "Installatiemonteur",
            "Installation technician",
            "Stabiele vraag, ook via omscholing",
            "Steady demand, including retraining routes",
            "Technische opleiding · erkend leerbedrijf",
            "Technical training · recognised learning company"),
        new(
            "Zorgondersteuner",
            "Care support worker",
            "Groeiende sector, korte omscholingsroutes",
            "Growing sector, short retraining paths",
            "BBL zorg · baangarantie-route (voorbeeld)",
            "Care apprenticeship · job-guarantee route (sample)"),
    ];

    public static IReadOnlyList<UwvSimilarJob> SimilarJobs { get; } =
    [
        new(
            "Customer service medewerker",
            "Customer service agent",
            "Past bij je communicatie en plannen",
            "Fits your communication and planning strengths"),
        new(
            "Teamleider backoffice",
            "Back-office team lead",
            "Bouwt voort op je ervaring met processen",
            "Builds on your process experience"),
        new(
            "HR support medewerker",
            "HR support officer",
            "Combineert organiseren met mensgericht werk",
            "Combines organising with people-focused work"),
    ];

    public static IReadOnlyList<UwvOmscholenSector> OmscholenSectors { get; } =
    [
        new("IT", "IT", "Helpdesk / junior developer via omscholing", "Help desk / junior developer via retraining", true),
        new("Zorg", "Care", "Zorgondersteuner met BBL", "Care support with apprenticeship", true),
        new("Onderwijs", "Education", "Onderwijsassistent", "Teaching assistant", false),
        new("Bouw en techniek", "Construction and engineering", "Installatie / elektro", "Installation / electrical", true),
    ];

    public static IReadOnlyList<UwvLeerbaan> Leerbanen { get; } =
    [
        new(
            "BBL logistiek (voorbeeld)",
            "Logistics apprenticeship (sample)",
            "Leerbedrijf · gekoppeld aan kaartvacature",
            "Learning company · linked to map vacancy"),
        new(
            "BBL techniek (voorbeeld)",
            "Technical apprenticeship (sample)",
            "Erkend leerbedrijf regio Haaglanden",
            "Recognised learning company Haaglanden"),
    ];

    public static IReadOnlyList<UwvPartnerLink> UwvPartners { get; } =
    [
        new(
            "Online training Competenties",
            "Online training Competencies",
            "https://onlinetrainingen.uwv.nl/",
            "UWV"),
        new(
            "Webinar: Overstappen naar een ander beroep",
            "Webinar: Moving to another occupation",
            "https://www.uwv.nl/",
            "UWV"),
    ];

    public static IReadOnlyList<UwvDriverChip> Drivers { get; } =
    [
        new(
            "Zekerheid en rust",
            "Security and calm",
            "Je wilt vooruit kunnen plannen en duidelijke afspraken.",
            "You want to plan ahead and rely on clear agreements."),
        new(
            "Menselijk contact",
            "Human connection",
            "Je krijgt energie van samenwerken en iemand echt helpen.",
            "You gain energy from teamwork and helping someone directly."),
        new(
            "Groei en leren",
            "Growth and learning",
            "Nieuwe vaardigheden opdoen motiveert je meer dan routine.",
            "Picking up new skills motivates you more than routine."),
    ];

    public static IReadOnlyList<(string Nl, string En)> AdviseurSections { get; } =
    [
        ("Carrièreroute (voorbeeld)", "Career path (sample)"),
        ("Kansberoepen en omscholingsideeën", "Opportunity occupations and retraining ideas"),
        ("Drijfveren en motivatie", "Drivers and motivation"),
        ("Onderschreven competenties (paspoort)", "Endorsed competencies (passport)"),
        ("Banen op de kaart (selectie)", "Jobs on the map (selection)"),
    ];
}
