using Jobsy.Web.Media;

namespace Jobsy.Web.UwvMockups;

public sealed record UwvSimilarJob(
    string TitleNl,
    string TitleEn,
    string WhyNl,
    string WhyEn,
    string RouteNl,
    string RouteEn);

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
    string CompanyEn,
    string MapHref);

public sealed record UwvMapVacancy(
    string TitleNl,
    string TitleEn,
    string TravelNl,
    string TravelEn,
    string CompanyNl,
    string CompanyEn,
    string LocationNl,
    string LocationEn,
    string ImageUrl,
    bool Kansberoep,
    bool Baangarantie);

public sealed record UwvKansCard(
    string TitleNl,
    string TitleEn,
    string ProspectNl,
    string ProspectEn,
    string RouteNl,
    string RouteEn,
    string SectorIcon,
    bool Baangarantie,
    int VacancyCount);

public sealed record UwvPartnerLink(
    string TitleNl,
    string TitleEn,
    string DescNl,
    string DescEn,
    string Url,
    string IconKind);

public sealed record UwvDriverChip(
    string LabelNl,
    string LabelEn,
    string ExplainNl,
    string ExplainEn);

public static class UwvMockupData
{
    public static IReadOnlyList<UwvKansCard> Kansberoepen { get; } =
    [
        new(
            "Logistiek medewerker",
            "Logistics associate",
            "Veel kans op werk in de regio",
            "Strong hiring outlook in the region",
            "MBO logistiek · BBL mogelijk",
            "MBO logistics · apprenticeship available",
            "logistiek",
            Baangarantie: true,
            VacancyCount: 12),
        new(
            "Installatiemonteur",
            "Installation technician",
            "Stabiele vraag, ook via omscholing",
            "Steady demand, including retraining routes",
            "Technische opleiding · erkend leerbedrijf",
            "Technical training · recognised learning company",
            "techniek",
            Baangarantie: false,
            VacancyCount: 6),
        new(
            "Zorgondersteuner",
            "Care support worker",
            "Groeiende sector, korte omscholingsroutes",
            "Growing sector, short retraining paths",
            "BBL zorg · baangarantie-route (voorbeeld)",
            "Care apprenticeship · job-guarantee route (sample)",
            "zorg",
            Baangarantie: true,
            VacancyCount: 8),
    ];

    public static IReadOnlyList<UwvMapVacancy> MapVacancies { get; } =
    [
        new(
            "Logistiek medewerker (voorbeeld)",
            "Logistics associate (sample)",
            "15 min fiets",
            "15 min bike",
            "Leerbedrijf West · erkend",
            "Learning company West · recognised",
            "Den Haag · Laak",
            "The Hague · Laak",
            CandidateIllustratedImages.SpotInclusiefZorgLogistiek,
            Kansberoep: true,
            Baangarantie: true),
        new(
            "Installatiemonteur (voorbeeld)",
            "Installation technician (sample)",
            "22 min fiets",
            "22 min bike",
            "Technisch leerbedrijf Midden",
            "Technical learning company Central",
            "Rijswijk",
            "Rijswijk",
            CandidateIllustratedImages.SpotInclusiefKantoor,
            Kansberoep: true,
            Baangarantie: false),
        new(
            "Zorgondersteuner BBL (voorbeeld)",
            "Care support apprenticeship (sample)",
            "18 min OV",
            "18 min transit",
            "Zorgorganisatie Midden",
            "Care organisation Central",
            "Zoetermeer",
            "Zoetermeer",
            CandidateIllustratedImages.SpotInclusiefZorgLogistiek,
            Kansberoep: true,
            Baangarantie: true),
    ];

    public static IReadOnlyList<UwvSimilarJob> SimilarJobs { get; } =
    [
        new(
            "Customer service medewerker",
            "Customer service agent",
            "Past bij je communicatie en plannen",
            "Fits your communication and planning strengths",
            "MBO dienstverlening · korte omscholing mogelijk",
            "Service vocational track · short retraining possible"),
        new(
            "Teamleider backoffice",
            "Back-office team lead",
            "Bouwt voort op je ervaring met processen",
            "Builds on your process experience",
            "Leidinggevende modules · combineerbaar met werk",
            "Supervisory modules · combinable with work"),
        new(
            "HR support medewerker",
            "HR support officer",
            "Combineert organiseren met mensgericht werk",
            "Combines organising with people-focused work",
            "HR-assistent opleiding · deeltijd mogelijk",
            "HR assistant training · part-time available"),
    ];

    public static IReadOnlyList<(UwvOmscholenSector Sector, string SectorIcon)> OmscholenSectors { get; } =
    [
        (new("IT", "IT", "Helpdesk / junior developer", "Help desk / junior developer", true), "it"),
        (new("Zorg", "Care", "Zorgondersteuner met BBL", "Care support with apprenticeship", true), "zorg"),
        (new("Onderwijs", "Education", "Onderwijsassistent", "Teaching assistant", false), "onderwijs"),
        (new("Bouw en techniek", "Construction and engineering", "Installatie / elektro", "Installation / electrical", true), "techniek"),
    ];

    public static IReadOnlyList<UwvLeerbaan> Leerbanen { get; } =
    [
        new(
            "BBL logistiek (voorbeeld)",
            "Logistics apprenticeship (sample)",
            "Leerbedrijf · gekoppeld aan kaartvacature",
            "Learning company · linked to map vacancy",
            "/banenkaart"),
        new(
            "BBL techniek (voorbeeld)",
            "Technical apprenticeship (sample)",
            "Erkend leerbedrijf regio Haaglanden",
            "Recognised learning company Haaglanden",
            "/banenkaart"),
    ];

    public static IReadOnlyList<UwvPartnerLink> UwvPartners { get; } =
    [
        new(
            "Online training Competenties",
            "Online training Competencies",
            "Korte modules over solliciteren en beroepskeuzes.",
            "Short modules on applying and career choices.",
            "https://onlinetrainingen.uwv.nl/",
            "compass"),
        new(
            "Webinar: Overstappen naar een ander beroep",
            "Webinar: Moving to another occupation",
            "Praktische tips en voorbeelden van omscholers.",
            "Practical tips and stories from people who retrained.",
            "https://www.uwv.nl/",
            "career"),
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
        ("Competenties (paspoort)", "Competencies (passport)"),
        ("Banen op de kaart (selectie)", "Jobs on the map (selection)"),
    ];
}
