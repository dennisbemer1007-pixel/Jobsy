using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.Competence;

/// <summary>
/// Fixed Dutch (B1) copy for the competence deep-analysis report: per-trait/per-level text
/// blocks, AI-fallback templates, and a small occupation map. All strings are hand-written,
/// positive but honest, and kept short (roughly 25 words or fewer).
/// </summary>
public static class CompetenceDeepReportTexts
{
    public sealed record TraitLevelText(
        string Meaning,
        string WorkQuote,
        string Pitfall,
        string Tip,
        string Strength,
        string ThriveAtWork,
        string FittingManager,
        string InTeam);

    private static readonly Dictionary<string, Dictionary<string, TraitLevelText>> ByTraitAndLevel =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [DeepAnalysisCompetenceItems.Consciëntieusheid] = new(StringComparer.OrdinalIgnoreCase)
            {
                [CompetenceDeepReportLevels.Laag] = new TraitLevelText(
                    Meaning: "Je werkt liever spontaan dan volgens een strak schema. Structuur voelt soms als een last, maar je past je makkelijk aan.",
                    WorkQuote: "Je pakt taken op zoals ze komen en improviseert als de planning verandert.",
                    Pitfall: "Je vergeet soms deadlines of laat klussen liggen als iets leukers voorbijkomt.",
                    Tip: "Gebruik een simpele takenlijst of reminder-app, zodat losse afspraken niet wegzakken.",
                    Strength: "Flexibel meebewegen als de planning ineens omgaat.",
                    ThriveAtWork: "Je doet het goed in werk met veel afwisseling en weinig vaste routine.",
                    FittingManager: "Een manager die duidelijke deadlines stelt maar de aanpak aan jou overlaat.",
                    InTeam: "Je brengt lucht en flexibiliteit in een team dat snel kan vastroesten."),
                [CompetenceDeepReportLevels.Gemiddeld] = new TraitLevelText(
                    Meaning: "Je bent redelijk betrouwbaar en organiseert je werk prima, zonder overdreven perfectionisme.",
                    WorkQuote: "Je maakt een planning, houdt je er meestal aan en schakelt als het moet.",
                    Pitfall: "Bij veel druk kan je overzicht verwateren en loop je achter de feiten aan.",
                    Tip: "Plan je belangrijkste taak elke dag als eerste, dan lukt de rest vaak vanzelf.",
                    Strength: "Je combineert structuur met ruimte om bij te sturen.",
                    ThriveAtWork: "Je functioneert goed in werk met een basisplanning en ruimte voor eigen aanpak.",
                    FittingManager: "Een manager die kaders schetst maar niet op je vingers kijkt.",
                    InTeam: "Je bent de stabiele factor die afspraken meestal wel nakomt."),
                [CompetenceDeepReportLevels.Hoog] = new TraitLevelText(
                    Meaning: "Je werkt gestructureerd, maakt dingen af en houdt je goed aan afspraken en deadlines.",
                    WorkQuote: "Je maakt een planning, checkt je werk en rondt taken netjes af voor de deadline.",
                    Pitfall: "Je kunt streng zijn voor jezelf en moeite hebben met loslaten als iets niet perfect is.",
                    Tip: "Zet af en toe een grens: 'goed genoeg' is soms ook echt goed genoeg.",
                    Strength: "Betrouwbaarheid: afspraken en deadlines zijn bij jou in veilige handen.",
                    ThriveAtWork: "Je floreert in werk met duidelijke procedures en aantoonbare kwaliteitseisen.",
                    FittingManager: "Een manager die je zelfstandig laat werken en op je resultaten vertrouwt.",
                    InTeam: "Je bent de stabiele kracht die zorgt dat losse eindjes worden afgerond.")
            },
            [DeepAnalysisCompetenceItems.Vriendelijkheid] = new(StringComparer.OrdinalIgnoreCase)
            {
                [CompetenceDeepReportLevels.Laag] = new TraitLevelText(
                    Meaning: "Je bent kritisch en zegt eerlijk wat je denkt, ook als dat niet iedereen bevalt.",
                    WorkQuote: "Je geeft je mening recht voor z'n raap, zonder eromheen te draaien.",
                    Pitfall: "Collega's kunnen je directheid als afstandelijk of confronterend ervaren.",
                    Tip: "Vraag af en toe naar de mening van de ander voordat je zelf oordeelt.",
                    Strength: "Je durft lastige boodschappen te brengen die anderen liever vermijden.",
                    ThriveAtWork: "Je doet het goed in werk waar kritisch denken en onderhandelen belangrijk zijn.",
                    FittingManager: "Een manager die resultaat waardeert boven een altijd zachte toon.",
                    InTeam: "Je houdt een team scherp door niet zomaar met alles akkoord te gaan."),
                [CompetenceDeepReportLevels.Gemiddeld] = new TraitLevelText(
                    Meaning: "Je werkt prettig samen, maar durft ook grenzen te stellen als dat nodig is.",
                    WorkQuote: "Je helpt collega's graag, maar zegt ook nee als je agenda vol staat.",
                    Pitfall: "Soms twijfel je tussen aardig blijven en eerlijk je grens aangeven.",
                    Tip: "Benoem je grens vroeg in het gesprek, dat voelt voor iedereen prettiger.",
                    Strength: "Je vindt een goede balans tussen meewerken en voor jezelf opkomen.",
                    ThriveAtWork: "Je functioneert goed in teams waar samenwerken en resultaat allebei tellen.",
                    FittingManager: "Een manager die overleg waardeert maar ook duidelijke keuzes maakt.",
                    InTeam: "Je bent de collega die meewerkt zonder zichzelf weg te cijferen."),
                [CompetenceDeepReportLevels.Hoog] = new TraitLevelText(
                    Meaning: "Je bent warm, behulpzaam en houdt rekening met anderen in wat je doet.",
                    WorkQuote: "Je springt bij als een collega het druk heeft, zonder dat iemand het vraagt.",
                    Pitfall: "Je kunt moeite hebben om nee te zeggen, ook als je zelf al vol zit.",
                    Tip: "Oefen met kort en vriendelijk grenzen stellen; dat hoeft geen ruzie te worden.",
                    Strength: "Je zorgt voor een prettige sfeer en helpt anderen zonder dat erom gevraagd wordt.",
                    ThriveAtWork: "Je floreert in werk met veel klant- of teamcontact en samenwerking.",
                    FittingManager: "Een manager die waardering toont en jouw hulp niet uitbuit.",
                    InTeam: "Je bent de lijm die het team samenhoudt en conflicten voorkomt.")
            },
            [DeepAnalysisCompetenceItems.EmotioneleStabiliteit] = new(StringComparer.OrdinalIgnoreCase)
            {
                [CompetenceDeepReportLevels.Laag] = new TraitLevelText(
                    Meaning: "Je voelt spanning en stress sneller dan gemiddeld, vooral onder tijdsdruk.",
                    WorkQuote: "Bij een hectische dag merk je dat onrust of piekeren toeneemt.",
                    Pitfall: "Bij veel druk tegelijk kan het even te veel worden en verlies je overzicht.",
                    Tip: "Bouw korte pauzes in en bespreek drukte op tijd met je leidinggevende.",
                    Strength: "Je voelt sfeer en spanning in een team sneller aan dan de meesten.",
                    ThriveAtWork: "Je doet het goed in werk met een voorspelbaar ritme en beperkte piekdruk.",
                    FittingManager: "Een manager die rust en duidelijkheid geeft in drukke periodes.",
                    InTeam: "Je signaleert vroeg als de druk in een team te hoog wordt."),
                [CompetenceDeepReportLevels.Gemiddeld] = new TraitLevelText(
                    Meaning: "Je blijft meestal rustig, met af en toe wat spanning bij drukke periodes.",
                    WorkQuote: "Bij een lastige klus blijf je meestal kalm, met soms een kort stressmoment.",
                    Pitfall: "Bij stapeling van deadlines kan het toch even spannen voor je.",
                    Tip: "Herken je eigen stresssignalen vroeg en neem dan bewust een korte pauze.",
                    Strength: "Je herstelt snel na een lastig moment en pakt daarna gewoon door.",
                    ThriveAtWork: "Je functioneert goed in werk met een normale hoeveelheid druk en afwisseling.",
                    FittingManager: "Een manager die af en toe checkt hoe het met je gaat.",
                    InTeam: "Je bent een stabiele factor, met af en toe een moment van twijfel."),
                [CompetenceDeepReportLevels.Hoog] = new TraitLevelText(
                    Meaning: "Je blijft kalm onder druk en laat je niet snel van je stuk brengen.",
                    WorkQuote: "Ook bij een hectische dag houd je het hoofd koel en denk je helder.",
                    Pitfall: "Je kunt spanning bij anderen soms onderschatten, omdat jij het zelf goed aankunt.",
                    Tip: "Vraag actief aan collega's hoe zij drukte ervaren, dat kan verschillen van jou.",
                    Strength: "Je bent het rustpunt in het team als het spannend wordt.",
                    ThriveAtWork: "Je floreert in werk met pieken, deadlines en onverwachte situaties.",
                    FittingManager: "Een manager die op je kan bouwen tijdens crisismomenten en drukte.",
                    InTeam: "Je geeft anderen rust en overzicht als de druk toeneemt.")
            },
            [DeepAnalysisCompetenceItems.Openheid] = new(StringComparer.OrdinalIgnoreCase)
            {
                [CompetenceDeepReportLevels.Laag] = new TraitLevelText(
                    Meaning: "Je houdt van vaste, vertrouwde manieren van werken die je goed kent.",
                    WorkQuote: "Je gebruikt liever de aanpak die al jaren werkt dan iets nieuws te proberen.",
                    Pitfall: "Verandering of een nieuw systeem kost jou wat meer aanpassingstijd.",
                    Tip: "Geef jezelf bij verandering iets meer tijd en vraag om een duidelijke uitleg.",
                    Strength: "Je bent voorspelbaar en consistent, ook als het ergens anders chaotisch is.",
                    ThriveAtWork: "Je doet het goed in werk met vaste routines en heldere procedures.",
                    FittingManager: "Een manager die verandering rustig uitlegt en tijd geeft om te wennen.",
                    InTeam: "Je zorgt voor rust en continuïteit als anderen steeds iets nieuws willen."),
                [CompetenceDeepReportLevels.Gemiddeld] = new TraitLevelText(
                    Meaning: "Je staat open voor nieuwe ideeën, maar hecht ook waarde aan wat al werkt.",
                    WorkQuote: "Je probeert een nieuwe aanpak als die duidelijk beter is, anders houd je vast aan wat werkt.",
                    Pitfall: "Bij te veel verandering tegelijk kun je toch behoefte aan houvast voelen.",
                    Tip: "Vraag om het 'waarom' bij een nieuwe werkwijze, dat maakt de omschakeling makkelijker.",
                    Strength: "Je combineert nieuwsgierigheid met gezond verstand over wat werkt.",
                    ThriveAtWork: "Je functioneert goed in werk met een mix van routine en nieuwe taken.",
                    FittingManager: "Een manager die nieuwe ideeën test voordat alles verandert.",
                    InTeam: "Je bent het klankbord dat nieuwe ideeën eerlijk aftast."),
                [CompetenceDeepReportLevels.Hoog] = new TraitLevelText(
                    Meaning: "Je bent nieuwsgierig, leert graag en probeert nieuwe dingen makkelijk uit.",
                    WorkQuote: "Je pakt een onbekend systeem of een nieuwe werkwijze met plezier op.",
                    Pitfall: "Je kunt sneller afhaken bij taken die lang hetzelfde blijven.",
                    Tip: "Zoek bewust ruimte voor verbetervoorstellen, dat houdt je werk interessant.",
                    Strength: "Je brengt frisse ideeën en nieuwe aanpakken in bij vastgelopen situaties.",
                    ThriveAtWork: "Je floreert in werk met afwisseling, verandering en ruimte om te leren.",
                    FittingManager: "Een manager die openstaat voor jouw ideeën en verbetervoorstellen.",
                    InTeam: "Je bent de aanjager van nieuwe ideeën en verbeteringen in het team.")
            },
            [DeepAnalysisCompetenceItems.Extraversie] = new(StringComparer.OrdinalIgnoreCase)
            {
                [CompetenceDeepReportLevels.Laag] = new TraitLevelText(
                    Meaning: "Je haalt energie uit rustig, geconcentreerd werk, meer dan uit veel sociaal contact.",
                    WorkQuote: "Je werkt het liefst even alleen door, zonder constant overleg of afleiding.",
                    Pitfall: "Een drukke, luide werkomgeving kan je sneller vermoeien dan collega's.",
                    Tip: "Plan bewust rustige momenten in tussen overleg- of klantcontact door.",
                    Strength: "Je kunt lang geconcentreerd doorwerken zonder afgeleid te raken.",
                    ThriveAtWork: "Je doet het goed in werk met veel zelfstandig, rustig uitvoerend werk.",
                    FittingManager: "Een manager die je ruimte geeft om rustig en zelfstandig te werken.",
                    InTeam: "Je brengt rust en focus in een team dat snel druk kan worden."),
                [CompetenceDeepReportLevels.Gemiddeld] = new TraitLevelText(
                    Meaning: "Je vindt een goede balans tussen sociaal contact en rustig, zelfstandig werken.",
                    WorkQuote: "Je praat mee in overleg, maar werkt ook prima een tijdje in je eigen tempo.",
                    Pitfall: "Bij een hele dag alleen óf een hele dag vol overleg raak je sneller moe.",
                    Tip: "Wissel bewust af tussen contactmomenten en rustige, zelfstandige taken.",
                    Strength: "Je schakelt makkelijk tussen samenwerken en zelfstandig doorwerken.",
                    ThriveAtWork: "Je functioneert goed in werk met een mix van klantcontact en eigen taken.",
                    FittingManager: "Een manager die zowel overleg als zelfstandig werk waardeert.",
                    InTeam: "Je bent de schakel tussen de stille en de drukke collega's."),
                [CompetenceDeepReportLevels.Hoog] = new TraitLevelText(
                    Meaning: "Je haalt energie uit contact met mensen en voelt je thuis in een druk team.",
                    WorkQuote: "Je zoekt zelf het gesprek op met klanten of collega's en neemt makkelijk het woord.",
                    Pitfall: "Bij lang alleen en stil werken kan je energie en motivatie wegzakken.",
                    Tip: "Plan bewust contactmomenten in, zeker op dagen met veel administratief werk.",
                    Strength: "Je brengt energie en verbinding in elk gesprek of overleg.",
                    ThriveAtWork: "Je floreert in werk met veel klant-, team- of publiekscontact.",
                    FittingManager: "Een manager die jouw energie inzet bij klantcontact en teamoverleg.",
                    InTeam: "Je bent de aanjager die de sfeer en het gesprek levendig houdt.")
            }
        };

    private static readonly Dictionary<string, (string Title, string Reason)[]> HighTraitOccupations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [DeepAnalysisCompetenceItems.Consciëntieusheid] =
            [
                ("Planner logistiek", "Je houdt overzicht, plant vooruit en rondt taken netjes af."),
                ("Kwaliteitscontroleur", "Je werkt nauwkeurig en signaleert afwijkingen voordat ze een probleem worden."),
                ("Officemanager", "Je zorgt dat afspraken, administratie en deadlines op orde blijven.")
            ],
            [DeepAnalysisCompetenceItems.Vriendelijkheid] =
            [
                ("Zorgmedewerker", "Je hebt oprechte aandacht voor mensen en helpt zonder dat het gevraagd wordt."),
                ("Klantenservicemedewerker", "Je blijft vriendelijk en behulpzaam, ook als een klant lastig doet."),
                ("Teamcoördinator zorg of horeca", "Je zorgt voor een prettige sfeer waarin collega's elkaar helpen.")
            ],
            [DeepAnalysisCompetenceItems.EmotioneleStabiliteit] =
            [
                ("Ploegleider logistiek", "Je houdt het hoofd koel als de druk en de deadlines oplopen."),
                ("Eventmedewerker", "Je blijft kalm bij onverwachte situaties en last-minute veranderingen."),
                ("Baliemedewerker spoed of receptie", "Je blijft rustig en overzichtelijk, ook op de drukste momenten.")
            ],
            [DeepAnalysisCompetenceItems.Openheid] =
            [
                ("Junior data-analist", "Je duikt graag in nieuwe systemen en zoekt zelf naar verbeteringen."),
                ("Marketingmedewerker", "Je bedenkt graag nieuwe invalshoeken en probeert onbekende aanpakken uit."),
                ("Verbetercoördinator", "Je stelt bestaande werkwijzen ter discussie en komt met frisse ideeën.")
            ],
            [DeepAnalysisCompetenceItems.Extraversie] =
            [
                ("Verkoopmedewerker winkel", "Je zoekt zelf het contact met klanten op en praat makkelijk met iedereen."),
                ("Accountmanager", "Je haalt energie uit netwerken en het opbouwen van nieuwe contacten."),
                ("Horecagastheer of -vrouw", "Je brengt energie en gezelligheid in een drukke, sociale werkomgeving.")
            ]
        };

    public static TraitLevelText? For(string domain, string level)
        => ByTraitAndLevel.TryGetValue(domain, out var byLevel) && byLevel.TryGetValue(level, out var text)
            ? text
            : null;

    public static string LabelNl(string domain) => domain switch
    {
        DeepAnalysisCompetenceItems.Consciëntieusheid => "Consciëntieusheid",
        DeepAnalysisCompetenceItems.Vriendelijkheid => "Vriendelijkheid",
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit => "Emotionele stabiliteit",
        DeepAnalysisCompetenceItems.Openheid => "Openheid",
        DeepAnalysisCompetenceItems.Extraversie => "Extraversie",
        _ => domain
    };

    /// <summary>Three occupation suggestions for a trait, used when the trait scores "Hoog".</summary>
    public static IReadOnlyList<(string Title, string Reason)> OccupationsFor(string domain)
        => HighTraitOccupations.TryGetValue(domain, out var list) ? list : [];

    /// <summary>Fallback (no-AI) three-sentence summary built from the candidate's own scores.</summary>
    public static string TemplateSummary(IReadOnlyList<CompetenceDeepTraitReport> traits)
    {
        if (traits.Count == 0)
        {
            return "We konden nog geen samenvatting maken. Vul de vragenlijst volledig in voor je persoonlijke profiel.";
        }

        var ranked = traits.OrderByDescending(t => t.Score).ToList();
        var top = ranked[0];
        var second = ranked.Count > 1 ? ranked[1] : null;
        var lowest = ranked[^1];

        var sentence1 = $"Je sterkste trek is {LabelNl(top.Domain)}, met een score van {top.Score} op 100.";
        var sentence2 = second is not null
            ? $"Daarnaast valt {LabelNl(second.Domain)} op, wat goed samen met je andere eigenschappen naar voren komt in je werk."
            : "Je profiel laat een duidelijk herkenbaar patroon zien in hoe je werkt.";
        var sentence3 = $"Op {LabelNl(lowest.Domain)} scoor je het laagst binnen je eigen profiel, wat vooral iets zegt over jouw voorkeuren, niet over je kwaliteiten.";

        return $"{sentence1} {sentence2} {sentence3}";
    }

    /// <summary>Fallback (no-AI) three-step action plan built from the candidate's own scores.</summary>
    public static IReadOnlyList<(string Title, string Body)> TemplateActionPlan(
        IReadOnlyList<CompetenceDeepTraitReport> traits)
    {
        if (traits.Count == 0)
        {
            return
            [
                ("Rond de vragenlijst af", "Vul alle 150 vragen in om een persoonlijk actieplan te krijgen.")
            ];
        }

        var ranked = traits.OrderByDescending(t => t.Score).ToList();
        var top = ranked[0];
        var lowest = ranked[^1];

        return
        [
            ("Benut je kracht",
                $"Zoek werk of taken waarin {LabelNl(top.Domain)} de hoofdrol speelt: {top.Strength}"),
            ("Oefen bewust",
                $"Werk aan {LabelNl(lowest.Domain)} met één kleine stap per week: {lowest.Tip}"),
            ("Praat erover",
                "Deel dit profiel met je leidinggevende of coach, zodat werk en team beter bij je passen.")
        ];
    }
}
