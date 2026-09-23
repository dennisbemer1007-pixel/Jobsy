namespace Jobsy.Core.Rules;

/// <summary>
/// Hierarchical occupation taxonomy (sector → family → title).
/// Similar roles must share a sector; soft-skill overlap alone is not enough.
/// </summary>
public static class OccupationTaxonomy
{
    public const double SameSectorMin = 0.75;

    public static IReadOnlyList<OccupationNode> All { get; } = BuildNodes();

    public static IReadOnlyDictionary<string, IReadOnlyList<EducationStepTemplate>> Tracks { get; } = BuildTracks();

    public static OccupationNode? Resolve(string? title)
    {
        var folded = CareerOccupationKeys.Fold(title ?? "");
        if (folded.Length < 3)
        {
            return null;
        }

        OccupationNode? best = null;
        var bestScore = 0;
        foreach (var node in All)
        {
            var score = Score(folded, node);
            if (score > bestScore)
            {
                bestScore = score;
                best = node;
            }
        }

        return bestScore > 0 ? best : null;
    }

    public static double Affinity(OccupationNode anchor, OccupationNode other)
    {
        if (anchor.Id == other.Id)
        {
            return 1;
        }

        if (!string.Equals(anchor.Sector, other.Sector, StringComparison.Ordinal))
        {
            return 0;
        }

        return anchor.Family == other.Family ? 0.95 : 0.82;
    }

    public static bool VacancySharesDomain(string? searchedTitle, string? vacancyTitle)
    {
        var anchor = Resolve(searchedTitle);
        if (anchor is null)
        {
            return true;
        }

        var vacancy = Resolve(vacancyTitle);
        return vacancy is not null && Affinity(anchor, vacancy) >= SameSectorMin;
    }

    public static IReadOnlyList<RoleFitSimilarRole> WithinDomain(
        string anchorTitle,
        IEnumerable<RoleFitSimilarRole>? roles)
    {
        var anchor = Resolve(anchorTitle);
        var list = (roles ?? []).ToList();
        if (anchor is null)
        {
            return list;
        }

        return list
            .Where(role =>
            {
                var node = Resolve(role.Title);
                return node is not null
                       && node.Id != anchor.Id
                       && Affinity(anchor, node) >= SameSectorMin;
            })
            .ToList();
    }

    public static bool TryGetTrack(string? title, out OccupationNode node, out IReadOnlyList<EducationStepTemplate> steps)
    {
        var resolved = Resolve(title);
        if (resolved is not null && Tracks.TryGetValue(resolved.TrackId, out var found))
        {
            node = resolved;
            steps = found;
            return true;
        }

        node = resolved ?? new OccupationNode("", "", "", "", "", false, "", []);
        steps = [];
        return false;
    }

    private static int Score(string foldedTitle, OccupationNode node)
    {
        var score = 0;
        foreach (var alias in node.Aliases)
        {
            var foldedAlias = CareerOccupationKeys.Fold(alias);
            if (foldedAlias.Length < 3)
            {
                continue;
            }

            if (foldedTitle == foldedAlias
                || CareerOccupationKeys.Hits(foldedTitle, foldedAlias))
            {
                score += 10 + foldedAlias.Length;
            }
        }

        return score;
    }

    private static IReadOnlyList<OccupationNode> BuildNodes() =>
    [
        Node("pilot", "Verkeersvlieger", "luchtvaart", "luchtvaart", "cockpit", false, "atpl",
            "piloot", "vlieger", "verkeersvlieger", "copiloot", "gezagvoerder", "first officer", "captain"),
        Node("cabin", "Cabinemedewerker", "luchtvaart", "luchtvaart", "cabine", true, "cabin",
            "cabinemedewerker", "purser", "stewardess", "cabin crew", "cabinepersoneel"),
        Node("avionics", "Vliegtuigmonteur", "luchtvaart", "luchtvaart", "techniek", true, "avionics",
            "vliegtuigmonteur", "avionica", "vliegtuigtechniek"),
        Node("atc", "Luchtverkeersleider", "luchtvaart", "luchtvaart", "verkeer", false, "atc",
            "luchtverkeersleider", "luchtverkeersleiding"),
        Node("ramp", "Grondafhandelaar", "luchtvaart", "luchtvaart", "grond", true, "ramp",
            "grondafhandelaar", "grondsteward", "platformmedewerker", "afhandelaar"),

        Node("nurse", "Verpleegkundige", "zorg", "zorg", "verpleging", false, "hbo-v",
            "verpleegkundige", "verpleegkunde", "wijkverpleegkundige"),
        Node("anp", "Verpleegkundig specialist", "zorg", "zorg", "specialist", false, "anp",
            "verpleegkundig specialist", "nurse practitioner"),
        Node("helpende", "Helpende zorg", "zorg", "zorg", "basis", true, "helpende",
            "helpende", "helpende zorg", "zorghulp"),
        Node("vig", "Verzorgende IG", "zorg", "zorg", "basis", true, "vig",
            "verzorgende", "verzorgende ig", "vig"),
        Node("activities", "Activiteitenbegeleider", "zorg", "zorg", "welzijn", true, "activities",
            "activiteitenbegeleider"),

        Node("teacher", "Docent basisonderwijs", "onderwijs", "onderwijs", "les", false, "pabo",
            "juf", "meester", "docent", "leraar", "leerkracht", "pabo"),
        Node("teacher-assistant", "Onderwijsassistent", "onderwijs", "onderwijs", "ondersteuning", true, "onderwijsassistent",
            "onderwijsassistent", "klassenassistent"),
        Node("pedagogue", "Pedagogisch medewerker", "onderwijs", "onderwijs", "ondersteuning", true, "pedagogisch",
            "pedagogisch medewerker", "pedagogisch", "kinderopvang"),
        Node("practice-teacher", "Praktijkopleider", "onderwijs", "onderwijs", "ondersteuning", true, "praktijkopleider",
            "praktijkopleider"),

        Node("lab", "Laboratoriumassistent", "lab", "het laboratorium", "lab", false, "lab",
            "labassistent", "laboratoriumassistent", "laboratorium", "analist", "meetassistent", "lab medewerker"),

        Node("cafe", "Café-hulp", "horeca", "horeca", "bediening", true, "cafe",
            "cafehulp", "cafe hulp", "cafe-hulp", "barista", "barman", "barvrouw"),
        Node("hospitality", "Medewerker horeca", "horeca", "horeca", "bediening", true, "hospitality",
            "bediening", "horeca", "gastvrouw", "gastheer"),
        Node("cook", "Kok", "horeca", "horeca", "keuken", false, "cook",
            "kok", "chef", "keuken"),

        Node("warehouse", "Magazijnmedewerker", "logistiek", "logistiek", "magazijn", true, "warehouse",
            "magazijn", "orderpicker", "heftruck"),
        Node("driver", "Chauffeur", "logistiek", "logistiek", "transport", false, "driver",
            "chauffeur", "vrachtwagen", "bezorger"),

        Node("electrician", "Elektricien", "techniek", "techniek", "installatie", false, "electro",
            "elektricien", "elektro", "onderhoudsmonteur", "monteur"),
        Node("builder", "Medewerker bouw", "techniek", "techniek", "bouw", true, "bouw",
            "bouw", "afbouw", "loodgieter"),

        Node("retail", "Verkoopmedewerker", "retail", "retail", "winkel", true, "retail",
            "verkoop", "winkel", "kassa", "kassamedewerker"),
        Node("admin", "Administratief medewerker", "admin", "administratie", "kantoor", true, "admin",
            "administratief", "boekhouder", "administrateur", "planner", "receptionist", "hr medewerker"),
        Node("software", "Softwareontwikkelaar", "it", "IT", "software", false, "software",
            "software", "softwareontwikkelaar", "programmeur", "developer"),
        Node("it-ops", "ICT-beheerder", "it", "IT", "beheer", true, "it-ops",
            "ict", "it beheer", "systeembeheer"),
        Node("greenhouse", "Medewerker tuinbouw", "groen", "tuinbouw", "teelt", true, "greenhouse",
            "tuinbouw", "kas", "teelt", "bloemist"),
        Node("animals", "Dierenverzorger", "dieren", "dierverzorging", "dieren", false, "animals",
            "dierenverzorger", "dierenverzorging")
    ];

    private static IReadOnlyDictionary<string, IReadOnlyList<EducationStepTemplate>> BuildTracks()
        => new Dictionary<string, IReadOnlyList<EducationStepTemplate>>(StringComparer.Ordinal)
        {
            ["atpl"] =
            [
                Step("Medische keuring klasse 1 en ogentest", 1,
                    "Verplichte keuring en ogentest voordat je met vliegen begint.",
                    "medische keuring", "klasse 1", "ogentest"),
                Step("Geïntegreerde ATPL-vliegopleiding", 24,
                    "Theorie, vlieguren en vliegbrevet (CPL/ATPL).",
                    "atpl", "vliegbrevet", "cpl"),
                Step("Type rating of airline-assessment", 4,
                    "Toestel-specifieke rating of selectie bij een maatschappij.",
                    "type rating")
            ],
            ["cabin"] =
            [
                Step("Medische keuring cabine", 1, "Basiekeuring voor cabinepersoneel.", "medische keuring"),
                Step("Cabine-opleiding", 3, "Veiligheid, service en noodprocedures.", "cabine")
            ],
            ["avionics"] =
            [
                Step("MBO Vliegtuigtechniek", 36, "Vakdiploma of BBL in vliegtuigonderhoud.", "vliegtuigtechniek", "avionica"),
                Step("VCA of Part-66 module", 6, "Veiligheidscertificaat of luchtvaartmodule.", "vca", "part 66", "part-66")
            ],
            ["atc"] =
            [
                Step("Selectie en medische keuring", 3, "Toelating en medische geschiktheid.", "medische keuring"),
                Step("Opleiding luchtverkeersleiding", 30, "Geleide opleiding tot luchtverkeersleider.", "luchtverkeersleiding")
            ],
            ["ramp"] =
            [
                Step("Inwerkopleiding afhandeling", 2, "Platform, veiligheid en apparatuur op de luchthaven.", "afhandeling")
            ],
            ["hbo-v"] =
            [
                Step("HBO Verpleegkunde", 48, "Diploma verpleegkunde op hbo-niveau.", "verpleegkunde", "hbo v", "mbo v"),
                Step("BIG-registratie", 2, "Inschrijving in het BIG-register na het diploma.", "big")
            ],
            ["anp"] =
            [
                Step("HBO Verpleegkunde", 48, "Eerst het diploma verpleegkunde.", "verpleegkunde"),
                Step("Master Advanced Nursing Practice", 24, "Vervolgmaster tot verpleegkundig specialist.", "advanced nursing", "verpleegkundig specialist")
            ],
            ["helpende"] =
            [
                Step("Helpende zorg en welzijn", 12, "MBO-2 of BBL, vaak naast het werk.", "helpende")
            ],
            ["vig"] =
            [
                Step("Verzorgende IG", 36, "MBO Verzorgende IG.", "verzorgende")
            ],
            ["activities"] =
            [
                Step("MBO Maatschappelijke zorg of agogisch werk", 24, "Korte route naar activiteitenbegeleiding.", "agogisch", "maatschappelijke zorg")
            ],
            ["pabo"] =
            [
                Step("Pabo", 48, "Lerarenopleiding basisonderwijs.", "pabo", "leraar basisonderwijs")
            ],
            ["onderwijsassistent"] =
            [
                Step("MBO Onderwijsassistent", 24, "Assisteren in de klas, zonder Pabo.", "onderwijsassistent")
            ],
            ["pedagogisch"] =
            [
                Step("MBO Pedagogisch werk", 36, "Kinderopvang of buitenschoolse opvang.", "pedagogisch")
            ],
            ["praktijkopleider"] =
            [
                Step("Praktijkopleiderscursus", 6, "Vak overdragen op de werkvloer.", "praktijkopleider")
            ],
            ["lab"] =
            [
                Step("MBO Laboratoriumtechniek", 36, "Analyseren en meten in een lab, niet in de cockpit of de horeca.", "laboratorium", "analist")
            ],
            ["cafe"] =
            [
                Step("Sociale hygiëne", 1, "Korte cursus voor wie alcohol schenkt.", "sociale hygiëne")
            ],
            ["hospitality"] =
            [
                Step("Inwerktraject bediening", 1, "Meelopen op de vloer is vaak genoeg.", "bediening")
            ],
            ["cook"] =
            [
                Step("MBO Kok of SVH-basiskok", 24, "Zelfstandig een keuken draaien.", "kok", "svh")
            ],
            ["warehouse"] =
            [
                Step("Heftruckcertificaat", 1, "Alleen nodig als de vacature heftruck vraagt.", "heftruck")
            ],
            ["driver"] =
            [
                Step("Rijbewijs C en code 95", 6, "Vrachtwagen plus nascholing.", "code 95", "rijbewijs c")
            ],
            ["electro"] =
            [
                Step("MBO Elektrotechniek", 36, "Vakdiploma of BBL.", "elektrotechniek", "elektricien"),
                Step("VCA", 1, "Veiligheidscertificaat op de bouw of in de installatie.", "vca")
            ],
            ["bouw"] =
            [
                Step("VCA", 1, "Veilig werken op de bouw.", "vca")
            ],
            ["retail"] =
            [
                Step("Inwerktraject winkel", 1, "Kassa en verkoop leer je op de vloer.", "verkoop")
            ],
            ["admin"] =
            [
                Step("MBO Administratie of een korte boekhoudcursus", 12, "Cijfers, planning of balie.", "administratie", "boekhoud")
            ],
            ["software"] =
            [
                Step("HBO Informatica of een omscholing tot developer", 24, "Programmeren in de praktijk.", "informatica", "software")
            ],
            ["it-ops"] =
            [
                Step("MBO ICT-beheer", 24, "Systemen, netwerk en gebruikers.", "ict")
            ],
            ["greenhouse"] =
            [
                Step("Inwerktraject teelt of kas", 1, "Seizoenswerk start vaak zonder extra diploma.", "tuinbouw")
            ],
            ["animals"] =
            [
                Step("MBO Dierverzorging", 36, "Vakdiploma dierverzorging.", "dierverzorging")
            ]
        };

    private static OccupationNode Node(
        string id,
        string title,
        string sector,
        string sectorLabel,
        string family,
        bool steppingStone,
        string trackId,
        params string[] aliases)
        => new(id, title, sector, sectorLabel, family, steppingStone, trackId, aliases);

    private static EducationStepTemplate Step(string title, int months, string detail, params string[] evidence)
        => new(title, months, detail, evidence);
}

public sealed record OccupationNode(
    string Id,
    string Title,
    string Sector,
    string SectorLabel,
    string Family,
    bool SteppingStone,
    string TrackId,
    string[] Aliases);

public sealed record EducationStepTemplate(
    string Title,
    int DurationMonths,
    string Detail,
    string[] EvidenceKeys);
