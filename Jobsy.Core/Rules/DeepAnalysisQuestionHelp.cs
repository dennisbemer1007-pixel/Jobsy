namespace Jobsy.Core.Rules;

/// <summary>
/// Concrete praktijkvoorbeelden per diepte-analysevraag (Jip-en-Janneke, geen vaktermen).
/// </summary>
public static class DeepAnalysisQuestionHelp
{
    public static string ExampleFor(DeepAnalysisQuestion question)
    {
        var scenarios = ScenariosFor(question.Domain);
        if (scenarios.Length == 0)
        {
            scenarios = FallbackScenarios;
        }

        var pick = scenarios[Math.Abs(question.Id - 1) % scenarios.Length];
        var angle = question.Reverse
            ? "Dit gaat over situaties waarin je geneigd bent om vast te houden aan wat bekend is — of juist niet."
            : "Dit gaat over hoe jij meestal reageert als het erop aankomt.";
        return $"{pick} {angle}";
    }

    public static string DomainLabel(string domain) => domain switch
    {
        DeepAnalysisCompetenceItems.Openheid => "Nieuwe dingen proberen",
        DeepAnalysisCompetenceItems.Consciëntieusheid => "Afmaken & netjes werken",
        DeepAnalysisCompetenceItems.Extraversie => "Energie van mensen",
        DeepAnalysisCompetenceItems.Vriendelijkheid => "Samen & aardig",
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit => "Kalm blijven",
        // Free competentietest categories (Extraversie shares the Big Five label above)
        CompetencyTestCatalog.Samenwerken => "Samen & aardig",
        CompetencyTestCatalog.Resultaatgerichtheid => "Afmaken & netjes werken",
        CompetencyTestCatalog.Stressbestendigheid => "Kalm blijven",
        CompetencyTestCatalog.Innovatie => "Nieuwe dingen proberen",
        CareerTestCatalog.Realistic => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Realistic),
        CareerTestCatalog.Investigative => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Investigative),
        CareerTestCatalog.Artistic => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Artistic),
        CareerTestCatalog.Social => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Social),
        CareerTestCatalog.Enterprising => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Enterprising),
        CareerTestCatalog.Conventional => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Conventional),
        SchwartzValuesCatalog.Autonomy => "Eigen regie & uitdaging",
        SchwartzValuesCatalog.Connection => "Verbinding & zorg",
        SchwartzValuesCatalog.Achievement => "Prestatie & groei",
        SchwartzValuesCatalog.Stability => "Zekerheid & traditie",
        SchwartzValuesCatalog.Impact => "Impact & rechtvaardigheid",
        CulturePersonalityCatalog.Informal => "Informele sfeer",
        CulturePersonalityCatalog.Collaboration => "Samenwerken",
        CulturePersonalityCatalog.Flexibility => "Flexibel meebewegen",
        CulturePersonalityCatalog.Innovation => "Nieuwe dingen proberen",
        CulturePersonalityCatalog.PeopleFirst => "Mensen voorop",
        // Autonomy shares the string code with SchwartzValuesCatalog.Autonomy above.
        CulturePersonalityCatalog.Openness => "Openstaan voor nieuw",
        CulturePersonalityCatalog.Conscientiousness => "Netjes en betrouwbaar",
        CulturePersonalityCatalog.Extraversion => "Energie van mensen",
        CulturePersonalityCatalog.Agreeableness => "Prettig samen optrekken",
        CulturePersonalityCatalog.EmotionalStability => "Kalm onder druk",
        _ => string.IsNullOrWhiteSpace(domain) ? "Onderwerp" : domain
    };

    private static readonly string[] FallbackScenarios =
    [
        "Voorbeeld: je bent op het werk of thuis met een klus die even anders loopt dan gepland. Hoe reageer jij dan?",
        "Voorbeeld: in de winkel, kas of op kantoor vraagt iemand iets snels van je. Wat doe jij meestal?"
    ];

    private static string[] ScenariosFor(string domain) => domain switch
    {
        DeepAnalysisCompetenceItems.Openheid =>
        [
            "Voorbeeld: een collega in de kas of winkel stelt een nieuwe manier voor om orders te labelen. Jij probeert het eerst klein uit.",
            "Voorbeeld: je telefoon-app op het werk krijgt een update. Jij klikt rond om te zien wat er beter kan.",
            "Voorbeeld: er komt een nieuw product in het schap. Jij wilt weten hoe het werkt voordat je het aan klanten uitlegt."
        ],
        DeepAnalysisCompetenceItems.Consciëntieusheid =>
        [
            "Voorbeeld: je belooft vandaag de magazijnlijst af te maken. Ook als het druk is, rond je die nog netjes af.",
            "Voorbeeld: vóór je een bestelling doorgeeft, check je nog even of de aantallen kloppen.",
            "Voorbeeld: je ruimt je werkplek op zodat de volgende ploeg meteen verder kan."
        ],
        DeepAnalysisCompetenceItems.Extraversie =>
        [
            "Voorbeeld: op een drukke balie of in de kantine praat je makkelijk met nieuwe collega’s.",
            "Voorbeeld: bij een teamoverleg neem je graag het woord om iets te delen.",
            "Voorbeeld: na een lange dienst met veel klanten voel je je juist opgeladen of juist leeg."
        ],
        DeepAnalysisCompetenceItems.Vriendelijkheid =>
        [
            "Voorbeeld: een collega heeft een zware dag. Jij biedt aan om een taak over te nemen.",
            "Voorbeeld: er is discussie in het team. Jij zoekt een oplossing waar iedereen mee verder kan.",
            "Voorbeeld: een klant is ontevreden. Jij blijft beleefd en luistert eerst goed."
        ],
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit =>
        [
            "Voorbeeld: de planning valt ineens om. Jij ademt even en kijkt wat wél kan vandaag.",
            "Voorbeeld: iemand is kortaf tegen je. Jij blijft kalm en reageert later rustig.",
            "Voorbeeld: er is piekdruk in de winkel of kas. Jij houdt je hoofd erbij zonder paniek."
        ],
        CareerTestCatalog.Realistic =>
        [
            "Voorbeeld: je werkt liever met je handen — monteren, plukken, rijden of iets maken dat je kunt zien.",
            "Voorbeeld: een kapotte machine of een volle order: jij wilt meteen doen, niet alleen erover praten."
        ],
        CareerTestCatalog.Investigative =>
        [
            "Voorbeeld: je wilt snappen waarom een cijfer of meting niet klopt, en zoekt het rustig uit.",
            "Voorbeeld: je leest graag bij over hoe iets technisch of inhoudelijk écht werkt."
        ],
        CareerTestCatalog.Artistic =>
        [
            "Voorbeeld: je bedenkt een mooiere etalage, een frisse indeling of een creatieve aanpak voor een klus.",
            "Voorbeeld: vaste routines zonder ruimte voor eigen inbreng voelen voor jou saai."
        ],
        CareerTestCatalog.Social =>
        [
            "Voorbeeld: je helpt een nieuwe collega op weg of begeleidt een klant die de weg kwijt is.",
            "Voorbeeld: werk waarbij je mensen steunt of uitlegt, past bij hoe jij energie krijgt."
        ],
        CareerTestCatalog.Enterprising =>
        [
            "Voorbeeld: je ziet een kans om meer te verkopen of een plan sneller rond te krijgen en pakt dat op.",
            "Voorbeeld: je vindt het leuk om anderen mee te nemen in een doel of actie."
        ],
        CareerTestCatalog.Conventional =>
        [
            "Voorbeeld: je houdt van duidelijke lijsten, labels en systemen zodat niets zoekraakt.",
            "Voorbeeld: administratie of planning op orde geven je rust — chaos juist niet."
        ],
        SchwartzValuesCatalog.Autonomy =>
        [
            "Voorbeeld: je mag zelf de volgorde van je taken kiezen. Hoe belangrijk is die ruimte voor jou?",
            "Voorbeeld: er is een nieuwe werkwijze. Jij wilt die graag zelf uitproberen."
        ],
        SchwartzValuesCatalog.Connection =>
        [
            "Voorbeeld: een collega heeft het zwaar. Jij merkt het en biedt hulp aan.",
            "Voorbeeld: een warme sfeer in het team weegt voor jou zwaarder dan alleen targets."
        ],
        SchwartzValuesCatalog.Achievement =>
        [
            "Voorbeeld: je ziet een duidelijke target op het bord. Jij wilt die graag overtreffen.",
            "Voorbeeld: na een drukke week wil je weten of je resultaat echt verschil maakte."
        ],
        SchwartzValuesCatalog.Stability =>
        [
            "Voorbeeld: vaste roosters en duidelijke afspraken geven je rust.",
            "Voorbeeld: je werkt liever volgens bewezen procedures dan elke week iets nieuws."
        ],
        SchwartzValuesCatalog.Impact =>
        [
            "Voorbeeld: je denkt na over verspilling of eerlijke behandeling van collega’s en klanten.",
            "Voorbeeld: je kiest liever werk dat iets goeds doet voor mens of milieu."
        ],
        CulturePersonalityCatalog.Informal =>
        [
            "Voorbeeld: collega’s noemen elkaar bij de voornaam en maken een luchtige grap. Past dat bij jou?",
            "Voorbeeld: je stuurt liever een kort berichtje dan een formele mail."
        ],
        CulturePersonalityCatalog.Collaboration =>
        [
            "Voorbeeld: je rondt een drukke shift samen met het team af. Hoe voelt dat?",
            "Voorbeeld: een collega vraagt of je even wilt meedenken. Wat doe jij?"
        ],
        CulturePersonalityCatalog.Flexibility =>
        [
            "Voorbeeld: om half elf komt er een spoedklus bij. Jij schakelt zonder veel gedoe.",
            "Voorbeeld: de planning verandert. Hoe reageer jij meestal?"
        ],
        CulturePersonalityCatalog.Innovation =>
        [
            "Voorbeeld: iemand stelt een slimmere werkwijze voor. Jij wilt die graag uitproberen.",
            "Voorbeeld: je ziet een omslachtig proces en bedenkt een kleinere verbetering."
        ],
        CulturePersonalityCatalog.PeopleFirst =>
        [
            "Voorbeeld: een collega heeft een zware dag. Jij merkt het en biedt hulp.",
            "Voorbeeld: resultaat en mens botsen. Wat weegt voor jou het zwaarst?"
        ],
        CulturePersonalityCatalog.Openness =>
        [
            "Voorbeeld: er komt een nieuwe tool op de vloer. Jij wilt weten hoe die werkt.",
            "Voorbeeld: je leert graag iets nieuws over hoe het vak verandert."
        ],
        CulturePersonalityCatalog.Conscientiousness =>
        [
            "Voorbeeld: je checkt aantallen nog even voordat je een bestelling doorgeeft.",
            "Voorbeeld: je ruimt je werkplek op zodat de volgende ploeg meteen verder kan."
        ],
        CulturePersonalityCatalog.Extraversion =>
        [
            "Voorbeeld: op een drukke balie praat je makkelijk met nieuwe collega’s.",
            "Voorbeeld: na veel mensencontact voel jij je juist opgeladen of juist leeg."
        ],
        CulturePersonalityCatalog.Agreeableness =>
        [
            "Voorbeeld: er is discussie in het team. Jij zoekt een oplossing waar iedereen mee verder kan.",
            "Voorbeeld: je geeft een concreet compliment als iemand iets goed doet."
        ],
        CulturePersonalityCatalog.EmotionalStability =>
        [
            "Voorbeeld: de planning valt om. Jij ademt even en kijkt wat wél kan.",
            "Voorbeeld: piekdruk in de winkel of kas — jij houdt je hoofd erbij."
        ],
        _ => []
    };
}
