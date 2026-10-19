using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Stable NL/EN practice examples per assessment item id (Quick-Scan v2 banks + deep analysis).
/// Replaces domain-only rotation in <see cref="DeepAnalysisQuestionHelp"/>.
/// </summary>
public static class AssessmentItemExamples
{
    public const int MaxLength = 140;

    private static readonly Lazy<IReadOnlyDictionary<(AssessmentKind Kind, int Id, string Lang), string>> Catalog =
        new(BuildCatalog);

    public static string ExampleNl(AssessmentKind kind, int id, string domain, bool reverse)
        => ExampleFor(kind, id, domain, reverse, "nl");

    public static string ExampleEn(AssessmentKind kind, int id, string domain, bool reverse)
        => ExampleFor(kind, id, domain, reverse, "en");

    public static string ExampleFor(AssessmentKind kind, int id, string domain, bool reverse, string lang)
    {
        var normalized = DeepItemLocalizations.NormalizeLang(lang);
        if (Catalog.Value.TryGetValue((kind, id, normalized), out var hit))
        {
            return hit;
        }

        return ComposeFallback(domain, reverse, id, normalized);
    }

    public static string ExampleFor(DeepAnalysisQuestion question)
        => ExampleFor(
            FamilyToKind(question.Family),
            question.Id,
            question.Domain,
            question.Reverse,
            "nl");

    public static IEnumerable<(AssessmentKind Kind, int Id)> AllCatalogIds()
        => Catalog.Value.Keys
            .Where(k => k.Lang == "nl")
            .Select(k => (k.Kind, k.Id))
            .Distinct();

    private static IReadOnlyDictionary<(AssessmentKind Kind, int Id, string Lang), string> BuildCatalog()
    {
        var map = new Dictionary<(AssessmentKind, int, string), string>();
        var usedNl = new Dictionary<AssessmentKind, HashSet<string>>();

        void Add(AssessmentKind kind, int id, string domain, bool _, bool skipIfExists = false)
        {
            if (skipIfExists && map.ContainsKey((kind, id, "nl")))
            {
                return;
            }

            var stemsNl = StemsFor(domain, "nl");
            if (stemsNl.Length == 0)
            {
                stemsNl = NlFallbackStems;
            }

            var stemsEn = StemsFor(domain, "en");
            if (stemsEn.Length == 0)
            {
                stemsEn = EnFallbackStems;
            }

            if (!usedNl.TryGetValue(kind, out var seen))
            {
                seen = [];
                usedNl[kind] = seen;
            }

            string nl = "";
            string en = "";
            for (var attempt = 0; attempt < stemsNl.Length; attempt++)
            {
                var idx = ((id - 1) + attempt) % stemsNl.Length;
                nl = Trim(stemsNl[idx]);
                en = Trim(stemsEn[idx % stemsEn.Length]);
                if (seen.Add(nl))
                {
                    break;
                }
            }

            map[(kind, id, "nl")] = nl;
            map[(kind, id, "en")] = en;
        }

        foreach (var q in CompetencyTestCatalogV2.Questions)
        {
            Add(AssessmentKind.Competence, q.Id, q.Category, q.Reverse);
        }

        foreach (var q in CareerTestCatalogV2.Questions)
        {
            Add(AssessmentKind.Career, q.Id, q.Category, q.Reverse);
        }

        foreach (var q in CulturePersonalityCatalogV2.Questions)
        {
            Add(AssessmentKind.Culture, q.Id, q.Category, q.Reverse);
        }

        foreach (var q in SchwartzValuesCatalogV2.Questions)
        {
            Add(AssessmentKind.Values, q.Id, q.Category, q.Reverse);
        }

        foreach (var kind in new[] { AssessmentKind.Competence, AssessmentKind.Career, AssessmentKind.Culture, AssessmentKind.Values })
        {
            foreach (var q in DeepAnalysisCatalog.QuestionsFor(kind))
            {
                Add(kind, q.Id, q.Domain, q.Reverse, skipIfExists: true);
            }
        }

        return map;
    }

    private static string Trim(string text)
    {
        var t = text.Length <= MaxLength ? text : text[..MaxLength].TrimEnd();
        if (t.Length > 0 && !t.EndsWith('.') && !t.EndsWith('?'))
        {
            t += ".";
        }

        return t;
    }

    private static string ComposeFallback(string domain, bool _, int id, string lang)
    {
        var stems = StemsFor(domain, lang);
        if (stems.Length == 0)
        {
            stems = lang == "en" ? EnFallbackStems : NlFallbackStems;
        }

        var stem = stems[Math.Abs(id - 1) % stems.Length];
        return Trim(stem);
    }

    private static string[] StemsFor(string domain, string lang)
        => lang == "en" ? EnStemsFor(domain) : NlStemsFor(domain);

    private static string[] NlStemsFor(string domain) => domain switch
    {
        DeepAnalysisCompetenceItems.Openheid or CompetencyTestCatalog.Innovatie =>
        [
            "Voorbeeld: een collega stelt een nieuwe werkwijze voor. Jij probeert het eerst klein.",
            "Voorbeeld: je app op het werk krijgt een update. Jij klikt rond om te snappen wat beter kan.",
            "Voorbeeld: er komt een nieuw product. Jij wilt weten hoe het werkt voor je het uitlegt.",
            "Voorbeeld: iemand deelt een tip van een andere winkel. Jij hoort graag wat er anders kan.",
            "Voorbeeld: de planning wijzigt. Jij kijkt of een andere volgorde ook kan.",
            "Voorbeeld: je ziet een slimmere route voor een taak. Jij test die kort uit.",
            "Voorbeeld: een klant vraagt iets ongewoons. Jij zoekt een werkbare oplossing.",
            "Voorbeeld: er is een nieuwe tool. Jij leert die stap voor stap."
        ],
        DeepAnalysisCompetenceItems.Consciëntieusheid or CompetencyTestCatalog.Resultaatgerichtheid =>
        [
            "Voorbeeld: je belooft vandaag een lijst af te maken. Ook druk, rond je die af.",
            "Voorbeeld: vóór je doorgeeft, check je nog even of aantallen kloppen.",
            "Voorbeeld: je ruimt je plek op zodat de volgende ploeg verder kan.",
            "Voorbeeld: een taak duurt langer. Jij plant een nieuw moment en houdt je eraan.",
            "Voorbeeld: je noteert afspraken meteen zodat niets wegvalt.",
            "Voorbeeld: je levert werk netjes op, ook als het bijna klaar is.",
            "Voorbeeld: je dubbelcheckt een order voordat je hem verstuurt.",
            "Voorbeeld: je maakt een korte checklist voor een drukke shift."
        ],
        DeepAnalysisCompetenceItems.Extraversie or CompetencyTestCatalog.Extraversie =>
        [
            "Voorbeeld: op een drukke balie praat je makkelijk met nieuwe collega’s.",
            "Voorbeeld: bij een overleg neem je graag het woord.",
            "Voorbeeld: na veel klantcontact voel jij je opgeladen of juist leeg.",
            "Voorbeeld: je stelt een nieuwe collega voor aan het team.",
            "Voorbeeld: in de pauze start jij een kort gesprek.",
            "Voorbeeld: je belt liever even dan lang mailen.",
            "Voorbeeld: bij een feestje op work praat je met mensen die je niet kent.",
            "Voorbeeld: je deelt snel wat je bezig houdt."
        ],
        DeepAnalysisCompetenceItems.Vriendelijkheid or CompetencyTestCatalog.Samenwerken =>
        [
            "Voorbeeld: jullie tillen samen zware dozen tot de klus klaar is.",
            "Voorbeeld: er is discussie. Jij zoekt een oplossing waar iedereen mee verder kan.",
            "Voorbeeld: een klant is boos. Jij blijft beleefd en luistert eerst.",
            "Voorbeeld: iemand maakt een fout. Jij helpt herstellen zonder te blame.",
            "Voorbeeld: je deelt je kennis als iemand vastloopt.",
            "Voorbeeld: je vraagt hoe het gaat voordat je iets vraagt.",
            "Voorbeeld: je bedankt iemand die je helpt.",
            "Voorbeeld: je past je tempo aan zodat jullie samen klaar zijn."
        ],
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit or CompetencyTestCatalog.Stressbestendigheid =>
        [
            "Voorbeeld: de planning valt om. Jij ademt even en kijkt wat wél kan.",
            "Voorbeeld: iemand is kortaf. Jij blijft kalm en reageert later rustig.",
            "Voorbeeld: piekdruk in de winkel. Jij houdt je hoofd erbij.",
            "Voorbeeld: er gaat iets mis met een levering. Jij schakelt zonder paniek.",
            "Voorbeeld: je krijgt drie vragen tegelijk. Jij prioriteert rustig.",
            "Voorbeeld: een machine hapert. Jij zoekt hulp en blijft vriendelijk.",
            "Voorbeeld: je shift duurt langer. Jij blijft netjes werken.",
            "Voorbeeld: onverwachte klacht. Jij blijft oplossingsgericht."
        ],
        CareerTestCatalog.Realistic =>
        [
            "Voorbeeld: je werkt liever met je handen, bijvoorbeeld monteren, rijden of iets zichtbaars maken.",
            "Voorbeeld: een kapotte machine: jij wilt meteen doen, niet alleen praten.",
            "Voorbeeld: je controleert of materiaal op orde is voordat je start.",
            "Voorbeeld: buiten werken of bewegen geeft je energie.",
            "Voorbeeld: je lost een praktisch probleem op de vloer op.",
            "Voorbeeld: je leest liever een handleiding dan een essay.",
            "Voorbeeld: je houdt van taken met een duidelijk resultaat.",
            "Voorbeeld: je test of iets stevig genoeg is gebouwd."
        ],
        CareerTestCatalog.Investigative =>
        [
            "Voorbeeld: je wilt snappen waarom een cijfer niet klopt.",
            "Voorbeeld: je zoekt rustig uit hoe iets technisch werkt.",
            "Voorbeeld: je vergelijkt twee opties met feiten.",
            "Voorbeeld: je stelt doorvragen tot het helder is.",
            "Voorbeeld: je leest een rapport om een besluit te onderbouwen.",
            "Voorbeeld: je experimenteert klein om te zien wat gebeurt.",
            "Voorbeeld: je maakt aantekeningen bij metingen.",
            "Voorbeeld: je checkt bronnen voordat je iets deelt."
        ],
        CareerTestCatalog.Artistic =>
        [
            "Voorbeeld: je bedenkt een frissere indeling of etalage.",
            "Voorbeeld: vaste routines zonder eigen inbreng voelen saai.",
            "Voorbeeld: je maakt iets mooiers van een saaie slide.",
            "Voorbeeld: je schrijft tekst die mensen raakt.",
            "Voorbeeld: je kiest kleuren of vormen die passen.",
            "Voorbeeld: je improviseert als het script ontbreekt.",
            "Voorbeeld: je deelt een creatief idee in een overleg.",
            "Voorbeeld: je maakt een moodboard voor een project."
        ],
        CareerTestCatalog.Social =>
        [
            "Voorbeeld: je helpt een nieuwe collega op weg.",
            "Voorbeeld: werk waarbij je mensen steunt geeft energie.",
            "Voorbeeld: je legt rustig uit aan een klant.",
            "Voorbeeld: je begeleidt iemand bij een taak.",
            "Voorbeeld: je merkt dat iemand twijfelt en biedt hulp.",
            "Voorbeeld: je luistert eerst voordat je advies geeft.",
            "Voorbeeld: je verbindt twee collega’s die elkaar kunnen helpen.",
            "Voorbeeld: je geeft feedback op een respectvolle manier."
        ],
        CareerTestCatalog.Enterprising =>
        [
            "Voorbeeld: je ziet een kans om meer te verkopen en pakt die op.",
            "Voorbeeld: je vindt het leuk anderen mee te nemen in een doel.",
            "Voorbeeld: je pitcht een idee kort aan je leidinggevende.",
            "Voorbeeld: je onderhandelt over een betere deal.",
            "Voorbeeld: je start een verbetering zonder lang wachten.",
            "Voorbeeld: je neemt het woord in een druk overleg.",
            "Voorbeeld: je zet een plan uiteen voor het team.",
            "Voorbeeld: je meet of een actie resultaat oplevert."
        ],
        CareerTestCatalog.Conventional =>
        [
            "Voorbeeld: je houdt van duidelijke lijsten en labels.",
            "Voorbeeld: administratie op orde geeft je rust.",
            "Voorbeeld: je werkt stap voor stap volgens afspraken.",
            "Voorbeeld: je archiveert documenten op vaste plekken.",
            "Voorbeeld: je vult spreadsheets netjes en op tijd in.",
            "Voorbeeld: je controleert totalen aan het eind van de dag.",
            "Voorbeeld: je volgt een protocol ook als het druk is.",
            "Voorbeeld: je plant taken in vaste blokken."
        ],
        SchwartzValuesCatalog.Autonomy =>
        [
            "Voorbeeld: je mag zelf de volgorde van taken kiezen.",
            "Voorbeeld: een nieuwe werkwijze wil je zelf uitproberen.",
            "Voorbeeld: je vraagt om ruimte om een taak anders te doen.",
            "Voorbeeld: je plant je dag zelf binnen kaders.",
            "Voorbeeld: je kiest zelf welk hulpmiddel je gebruikt.",
            "Voorbeeld: je geeft aan wanneer overleg nodig is.",
            "Voorbeeld: je stelt je eigen mini-doelen per shift.",
            "Voorbeeld: je past tempo aan zolang het resultaat klopt."
        ],
        SchwartzValuesCatalog.Connection =>
        [
            "Voorbeeld: een collega heeft het zwaar. Jij merkt het en helpt.",
            "Voorbeeld: een warme sfeer weegt zwaarder dan alleen targets.",
            "Voorbeeld: je checkt even in na een conflict.",
            "Voorbeeld: je viert een teamresultaat samen.",
            "Voorbeeld: je deelt iets persoonlijks op passend moment.",
            "Voorbeeld: je stelt teamafspraken boven solo winst.",
            "Voorbeeld: je maakt tijd voor een gesprek.",
            "Voorbeeld: je helpt een nieuweling zich thuis voelen."
        ],
        SchwartzValuesCatalog.Achievement =>
        [
            "Voorbeeld: je ziet een target en wilt die graag halen.",
            "Voorbeeld: na een drukke week wil je weten of het verschil maakte.",
            "Voorbeeld: je houdt je voortgang bij.",
            "Voorbeeld: je zoekt feedback om beter te worden.",
            "Voorbeeld: je streeft naar een persoonlijk record.",
            "Voorbeeld: je leert van een gemiste deadline.",
            "Voorbeeld: je deelt een resultaat waar je trots op bent.",
            "Voorbeeld: je zet extra in als het erop aankomt."
        ],
        SchwartzValuesCatalog.Stability =>
        [
            "Voorbeeld: vaste roosters geven je rust.",
            "Voorbeeld: je werkt liever volgens bewezen procedures.",
            "Voorbeeld: je wilt weten wat er morgen gebeurt.",
            "Voorbeeld: je houdt van duidelijke regels.",
            "Voorbeeld: je vermijdt onnodige verrassingen.",
            "Voorbeeld: je bouwt op routines die werken.",
            "Voorbeeld: je checkt planning vroeg op de dag.",
            "Voorbeeld: je waardeert langdurige afspraken."
        ],
        SchwartzValuesCatalog.Impact =>
        [
            "Voorbeeld: je denkt na over eerlijkheid voor collega’s en klanten.",
            "Voorbeeld: je kiest werk dat iets goeds doet voor mens of milieu.",
            "Voorbeeld: je meldt verspilling.",
            "Voorbeeld: je kiest leveranciers die passen bij waarden.",
            "Voorbeeld: je helpt een klant die kwetsbaar is.",
            "Voorbeeld: je stelt veiligheid boven snelheid.",
            "Voorbeeld: je deelt tips over duurzamer werken.",
            "Voorbeeld: je weigert een shortcut die anderen benadeelt."
        ],
        CulturePersonalityCatalog.Informal =>
        [
            "Voorbeeld: collega’s noemen elkaar bij de voornaam.",
            "Voorbeeld: je stuurt liever een kort bericht dan een formele mail.",
            "Voorbeeld: humor op de werkvloer voelt natuurlijk.",
            "Voorbeeld: je durft te zeggen dat iets gek is.",
            "Voorbeeld: informele afspraken werken voor jou.",
            "Voorbeeld: je kiest een relaxte toon in chat.",
            "Voorbeeld: je voelt je snel op je gemak in het team.",
            "Voorbeeld: je houdt van een luchtige pauze."
        ],
        CulturePersonalityCatalog.Collaboration =>
        [
            "Voorbeeld: je rondt een drukke shift samen af.",
            "Voorbeeld: een collega vraagt meedenken. Wat doe jij?",
            "Voorbeeld: je deelt taken eerlijk.",
            "Voorbeeld: je overlegt voordat je beslist.",
            "Voorbeeld: je vraagt input van het team.",
            "Voorbeeld: je helpt zonder dat het gevraagd wordt.",
            "Voorbeeld: je sluit aan bij een gezamenlijke klus.",
            "Voorbeeld: je geeft credit aan anderen."
        ],
        CulturePersonalityCatalog.Flexibility =>
        [
            "Voorbeeld: om half elf komt er een spoedklus bij.",
            "Voorbeeld: de planning verandert. Hoe reageer jij?",
            "Voorbeeld: je schakelt tussen taken zonder stress.",
            "Voorbeeld: je past je aan als de winkel drukker wordt.",
            "Voorbeeld: je blijft vriendelijk bij wisselende uren.",
            "Voorbeeld: je pakt een onverwachte taak op.",
            "Voorbeeld: je vindt een omweg als de normale route vastloopt.",
            "Voorbeeld: je blijft nuchter bij tegenslag."
        ],
        CulturePersonalityCatalog.Innovation =>
        [
            "Voorbeeld: iemand stelt een slimmere werkwijze voor.",
            "Voorbeeld: je ziet een omslachtig proces en bedenkt verbetering.",
            "Voorbeeld: je test een idee klein voordat je opschaalt.",
            "Voorbeeld: je deelt een hack die tijd bespaart.",
            "Voorbeeld: je leest over trends in je vak.",
            "Voorbeeld: je vraagt waarom iets altijd zo ging.",
            "Voorbeeld: je combineert bestaande tools op een nieuwe manier.",
            "Voorbeeld: je moedigt experimenteren aan."
        ],
        CulturePersonalityCatalog.PeopleFirst =>
        [
            "Voorbeeld: resultaat en mens botsen. Wat weegt zwaarder voor jou?",
            "Voorbeeld: je merkt stress bij een collega en past planning aan.",
            "Voorbeeld: je kiest voor veiligheid boven snelheid.",
            "Voorbeeld: je geeft rustig feedback.",
            "Voorbeeld: je beschermt iemand die wordt aangesproken.",
            "Voorbeeld: je vraagt hoe het echt gaat.",
            "Voorbeeld: je neemt tijd voor onboarding.",
            "Voorbeeld: je viert mensen, niet alleen cijfers."
        ],
        CulturePersonalityCatalogV2.Curiosity or "Curiosity" =>
        [
            "Voorbeeld: je stelt vragen tot je snapt waarom iets zo loopt.",
            "Voorbeeld: een collega doet het anders. Jij wilt de reden horen.",
            "Voorbeeld: je leest kort de uitleg bij een nieuwe werkwijze.",
            "Voorbeeld: je vraagt door als een stappenplan onduidelijk is.",
            "Voorbeeld: je zoekt een voorbeeld voordat je zelf start.",
            "Voorbeeld: je noteert wat je leert voor de volgende shift.",
            "Voorbeeld: je kijkt mee om te zien hoe een taak in elkaar zit.",
            "Voorbeeld: je deelt een link die het team helpt."
        ],
        CulturePersonalityCatalog.Openness =>
        [
            "Voorbeeld: er komt een nieuwe tool. Jij wilt weten hoe die werkt.",
            "Voorbeeld: je leert graag iets nieuws over je vak.",
            "Voorbeeld: je probeert een andere aanpak.",
            "Voorbeeld: je luistert naar feedback zonder meteen nee te zeggen.",
            "Voorbeeld: je leest artikelen buiten je comfortzone.",
            "Voorbeeld: je deelt wat je leert met collega’s.",
            "Voorbeeld: je vraagt uitleg bij jargon.",
            "Voorbeeld: je zoekt nieuwe inzichten in data."
        ],
        CulturePersonalityCatalog.Conscientiousness =>
        [
            "Voorbeeld: je checkt aantallen voordat je doorgeeft.",
            "Voorbeeld: je ruimt je plek op voor de volgende ploeg.",
            "Voorbeeld: je komt afspraken na.",
            "Voorbeeld: je werkt netjes ook zonder controle.",
            "Voorbeeld: je plant vooruit.",
            "Voorbeeld: je dubbelcheckt kwaliteit.",
            "Voorbeeld: je documenteert belangrijke stappen.",
            "Voorbeeld: je houdt deadlines in de gaten."
        ],
        CulturePersonalityCatalog.Extraversion =>
        [
            "Voorbeeld: op een drukke balie praat je makkelijk met nieuwe collega’s.",
            "Voorbeeld: na veel mensencontact voel jij je opgeladen of juist leeg.",
            "Voorbeeld: je start gesprekken in de pauze.",
            "Voorbeeld: je presenteert kort je idee.",
            "Voorbeeld: je vindt netwerken oké.",
            "Voorbeeld: je belt sneller dan mailen.",
            "Voorbeeld: je steekt handen uit voor introducties.",
            "Voorbeeld: je deelt nieuws in het teamkanaal."
        ],
        CulturePersonalityCatalog.Agreeableness =>
        [
            "Voorbeeld: er is discussie. Jij zoekt een oplossing voor iedereen.",
            "Voorbeeld: je geeft een concreet compliment.",
            "Voorbeeld: je vermijdt harde taal.",
            "Voorbeeld: je zoekt compromis.",
            "Voorbeeld: je erkent de inspanning van anderen.",
            "Voorbeeld: je biedt hulp aan zonder agenda.",
            "Voorbeeld: je de-escaleert spanning.",
            "Voorbeeld: je zegt sorry als het nodig is."
        ],
        CulturePersonalityCatalog.EmotionalStability =>
        [
            "Voorbeeld: de planning valt om. Jij ademt even.",
            "Voorbeeld: bij piekdruk houd jij je hoofd erbij.",
            "Voorbeeld: kritiek raakt je kort, daarna schakel je door.",
            "Voorbeeld: je blijft vriendelijk onder druk.",
            "Voorbeeld: je vraagt hulp voordat je vastloopt.",
            "Voorbeeld: je laat emoties niet de boel blokkeren.",
            "Voorbeeld: je herpakt je na een fout.",
            "Voorbeeld: je blijft realistisch bij tegenslag."
        ],
        _ => []
    };

    private static string[] EnStemsFor(string domain) => domain switch
    {
        DeepAnalysisCompetenceItems.Openheid or CompetencyTestCatalog.Innovatie =>
        [
            "Example: a colleague suggests a new way to work. You try it on a small scale.",
            "Example: your work app updates. You click around to see what improved.",
            "Example: a new product arrives. You learn how it works before explaining it.",
            "Example: someone shares a tip from another store. You listen for useful ideas.",
            "Example: the plan changes. You check if another order still works.",
            "Example: you spot a smarter route for a task and test it briefly.",
            "Example: a customer asks something unusual. You look for a workable fix.",
            "Example: a new tool lands. You learn it step by step."
        ],
        DeepAnalysisCompetenceItems.Consciëntieusheid or CompetencyTestCatalog.Resultaatgerichtheid =>
        [
            "Example: you promised to finish a list today. Even when busy, you do.",
            "Example: before submitting, you double-check quantities.",
            "Example: you tidy your station so the next shift can continue.",
            "Example: a task runs long. You reschedule and keep the promise.",
            "Example: you jot down agreements so nothing slips.",
            "Example: you deliver neatly even near the end of a shift.",
            "Example: you verify an order before sending it.",
            "Example: you make a short checklist for a busy day."
        ],
        DeepAnalysisCompetenceItems.Extraversie or CompetencyTestCatalog.Extraversie =>
        [
            "Example: on a busy counter you chat easily with new colleagues.",
            "Example: in a team meeting you like to share your view.",
            "Example: after lots of customer contact you feel wired or drained.",
            "Example: you introduce a new colleague to the team.",
            "Example: in a break you start a quick conversation.",
            "Example: you prefer a short call over long email threads.",
            "Example: at a work social you talk to people you do not know.",
            "Example: you quickly share what is on your mind."
        ],
        DeepAnalysisCompetenceItems.Vriendelijkheid or CompetencyTestCatalog.Samenwerken =>
        [
            "Example: you lift heavy boxes together until the job is done.",
            "Example: there is tension. You look for a solution everyone can live with.",
            "Example: a customer is upset. You stay polite and listen first.",
            "Example: someone slips up. You help fix it without blame.",
            "Example: you share know-how when someone is stuck.",
            "Example: you ask how they are before you ask for help.",
            "Example: you thank people who help you.",
            "Example: you match pace so you finish together."
        ],
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit or CompetencyTestCatalog.Stressbestendigheid =>
        [
            "Example: the plan collapses. You breathe and see what still works.",
            "Example: someone is blunt. You stay calm and reply later.",
            "Example: peak rush in the store. You keep a clear head.",
            "Example: a delivery goes wrong. You switch gears without panic.",
            "Example: three questions at once. You prioritize calmly.",
            "Example: a machine hiccups. You ask for help and stay friendly.",
            "Example: your shift runs long. You keep quality up.",
            "Example: a surprise complaint. You stay solution-focused."
        ],
        CareerTestCatalog.Realistic =>
        [
            "Example: you prefer hands-on work, like fixing, driving, or making something visible.",
            "Example: broken kit: you want to act, not only talk.",
            "Example: you check materials before you start.",
            "Example: moving or outdoor work gives you energy.",
            "Example: you solve a practical floor problem.",
            "Example: you read a manual rather than a long essay.",
            "Example: you like tasks with a clear physical result.",
            "Example: you test whether something is built solidly."
        ],
        CareerTestCatalog.Investigative =>
        [
            "Example: you want to know why a number does not add up.",
            "Example: you calmly dig into how something technical works.",
            "Example: you compare two options with facts.",
            "Example: you keep asking until it is clear.",
            "Example: you read a report to back a decision.",
            "Example: you run a small experiment to see what happens.",
            "Example: you note measurements as you go.",
            "Example: you check sources before sharing."
        ],
        CareerTestCatalog.Artistic =>
        [
            "Example: you imagine a fresher layout or window display.",
            "Example: fixed routines with no creative room feel dull.",
            "Example: you make a boring slide look better.",
            "Example: you write copy that lands with people.",
            "Example: you pick colours or shapes that fit.",
            "Example: you improvise when the script is missing.",
            "Example: you share a creative idea in a meeting.",
            "Example: you build a mood board for a project."
        ],
        CareerTestCatalog.Social =>
        [
            "Example: you help a new colleague get started.",
            "Example: work where you support people energises you.",
            "Example: you explain calmly to a customer.",
            "Example: you coach someone through a task.",
            "Example: you notice doubt and offer help.",
            "Example: you listen before you advise.",
            "Example: you connect two colleagues who can help each other.",
            "Example: you give feedback respectfully."
        ],
        CareerTestCatalog.Enterprising =>
        [
            "Example: you spot a chance to sell more and take it.",
            "Example: you like bringing others along toward a goal.",
            "Example: you pitch an idea briefly to your manager.",
            "Example: you negotiate a better deal.",
            "Example: you start an improvement without waiting long.",
            "Example: you speak up in a heated meeting.",
            "Example: you lay out a plan for the team.",
            "Example: you check whether an action paid off."
        ],
        CareerTestCatalog.Conventional =>
        [
            "Example: you like clear lists and labels.",
            "Example: tidy admin gives you peace.",
            "Example: you work step by step by agreement.",
            "Example: you file documents in fixed places.",
            "Example: you fill spreadsheets neatly and on time.",
            "Example: you check totals at day end.",
            "Example: you follow protocol even when it is busy.",
            "Example: you plan tasks in fixed blocks."
        ],
        SchwartzValuesCatalog.Autonomy =>
        [
            "Example: you may choose the order of tasks yourself.",
            "Example: you want to try a new method on your own.",
            "Example: you ask for room to do a task differently.",
            "Example: you plan your day within clear boundaries.",
            "Example: you pick which tool you use.",
            "Example: you flag when a meeting is needed.",
            "Example: you set mini-goals per shift.",
            "Example: you adjust pace while the outcome stays correct."
        ],
        SchwartzValuesCatalog.Connection =>
        [
            "Example: a colleague struggles. You notice and help.",
            "Example: team warmth matters more than targets alone.",
            "Example: you check in after a conflict.",
            "Example: you celebrate a team win together.",
            "Example: you share something personal at the right moment.",
            "Example: you put team agreements above solo wins.",
            "Example: you make time for a real conversation.",
            "Example: you help a newcomer feel at home."
        ],
        SchwartzValuesCatalog.Achievement =>
        [
            "Example: you see a target and want to hit it.",
            "Example: after a busy week you want to know it mattered.",
            "Example: you track your progress.",
            "Example: you seek feedback to improve.",
            "Example: you aim for a personal best.",
            "Example: you learn from a missed deadline.",
            "Example: you share a result you are proud of.",
            "Example: you push harder when it counts."
        ],
        SchwartzValuesCatalog.Stability =>
        [
            "Example: fixed rosters give you calm.",
            "Example: you prefer proven procedures.",
            "Example: you want to know what happens tomorrow.",
            "Example: you like clear rules.",
            "Example: you avoid needless surprises.",
            "Example: you rely on routines that work.",
            "Example: you check the plan early in the day.",
            "Example: you value long-term agreements."
        ],
        SchwartzValuesCatalog.Impact =>
        [
            "Example: you think about fairness for colleagues and customers.",
            "Example: you choose work that helps people or the planet.",
            "Example: you flag waste.",
            "Example: you pick suppliers that match your values.",
            "Example: you help a vulnerable customer.",
            "Example: you put safety before speed.",
            "Example: you share tips for greener work.",
            "Example: you refuse a shortcut that hurts others."
        ],
        CulturePersonalityCatalog.Informal =>
        [
            "Example: colleagues use first names.",
            "Example: you send a short message instead of a formal mail.",
            "Example: light humour on the floor feels natural.",
            "Example: you say when something feels odd.",
            "Example: informal agreements work for you.",
            "Example: you keep chat tone relaxed.",
            "Example: you feel at ease quickly in a team.",
            "Example: you enjoy an easy-going break."
        ],
        CulturePersonalityCatalog.Collaboration =>
        [
            "Example: you finish a busy shift together.",
            "Example: a colleague asks you to think along. What do you do?",
            "Example: you share tasks fairly.",
            "Example: you align before you decide.",
            "Example: you ask the team for input.",
            "Example: you help without being asked.",
            "Example: you join a shared job.",
            "Example: you give credit to others."
        ],
        CulturePersonalityCatalog.Flexibility =>
        [
            "Example: a rush job lands mid-morning.",
            "Example: the plan shifts. How do you react?",
            "Example: you switch tasks without stress.",
            "Example: you adapt when the store gets busier.",
            "Example: you stay friendly with changing hours.",
            "Example: you pick up an unexpected task.",
            "Example: you find a detour when the usual route blocks.",
            "Example: you stay matter-of-fact when things go wrong."
        ],
        CulturePersonalityCatalog.Innovation =>
        [
            "Example: someone proposes a smarter workflow.",
            "Example: you spot a clunky process and suggest a fix.",
            "Example: you test an idea small before scaling.",
            "Example: you share a hack that saves time.",
            "Example: you read about trends in your trade.",
            "Example: you ask why it always worked that way.",
            "Example: you combine tools in a new way.",
            "Example: you encourage trying things out."
        ],
        CulturePersonalityCatalog.PeopleFirst =>
        [
            "Example: results and people clash. Which weighs more for you?",
            "Example: you notice stress and adjust the plan.",
            "Example: you choose safety over speed.",
            "Example: you give calm feedback.",
            "Example: you stand up for someone being pressed.",
            "Example: you ask how someone really is.",
            "Example: you take time for onboarding.",
            "Example: you celebrate people, not only numbers."
        ],
        CulturePersonalityCatalogV2.Curiosity or "Curiosity" =>
        [
            "Example: you ask questions until you see why something works that way.",
            "Example: a colleague does it differently. You want to hear why.",
            "Example: you skim the notes for a new workflow.",
            "Example: you ask again when a checklist step is unclear.",
            "Example: you look for a sample before you start yourself.",
            "Example: you jot down what you learn for the next shift.",
            "Example: you watch how a task is done before trying it.",
            "Example: you share a link that helps the team."
        ],
        CulturePersonalityCatalog.Openness =>
        [
            "Example: a new tool arrives. You want to know how it works.",
            "Example: you like learning something new about your job.",
            "Example: you try another approach.",
            "Example: you listen to feedback before saying no.",
            "Example: you read outside your comfort zone.",
            "Example: you share what you learn with colleagues.",
            "Example: you ask when jargon is unclear.",
            "Example: you hunt for new insights in data."
        ],
        CulturePersonalityCatalog.Conscientiousness =>
        [
            "Example: you check counts before submitting.",
            "Example: you tidy for the next shift.",
            "Example: you keep agreements.",
            "Example: you work neatly without supervision.",
            "Example: you plan ahead.",
            "Example: you double-check quality.",
            "Example: you note important steps.",
            "Example: you watch deadlines."
        ],
        CulturePersonalityCatalog.Extraversion =>
        [
            "Example: on a busy counter you chat easily with new colleagues.",
            "Example: after lots of contact you feel wired or drained.",
            "Example: you start chats in breaks.",
            "Example: you present your idea briefly.",
            "Example: networking is fine for you.",
            "Example: you call sooner than you email.",
            "Example: you introduce people to each other.",
            "Example: you post news in the team channel."
        ],
        CulturePersonalityCatalog.Agreeableness =>
        [
            "Example: there is debate. You seek a fix for everyone.",
            "Example: you give a specific compliment.",
            "Example: you avoid harsh wording.",
            "Example: you look for compromise.",
            "Example: you acknowledge others' effort.",
            "Example: you offer help without an agenda.",
            "Example: you de-escalate tension.",
            "Example: you apologise when it is due."
        ],
        CulturePersonalityCatalog.EmotionalStability =>
        [
            "Example: the plan drops. You take a breath.",
            "Example: in peak pressure, you keep a clear head.",
            "Example: criticism stings briefly, then you move on.",
            "Example: you stay friendly under pressure.",
            "Example: you ask for help before you stall.",
            "Example: you do not let emotions block the job.",
            "Example: you reset after a mistake.",
            "Example: you stay realistic when things go wrong."
        ],
        _ => []
    };

    private static readonly string[] NlFallbackStems =
    [
        "Voorbeeld: op het werk loopt iets anders dan gepland.",
        "Voorbeeld: iemand vraagt iets snels van je op de vloer."
    ];

    private static readonly string[] EnFallbackStems =
    [
        "Example: something at work runs differently than planned.",
        "Example: someone asks you for a quick favour on the floor."
    ];

    private static AssessmentKind FamilyToKind(string family)
        => family.ToLowerInvariant() switch
        {
            "riasec" or "career" => AssessmentKind.Career,
            "culture" or "disc" => AssessmentKind.Culture,
            "schwartz" or "values" => AssessmentKind.Values,
            _ => AssessmentKind.Competence
        };
}
