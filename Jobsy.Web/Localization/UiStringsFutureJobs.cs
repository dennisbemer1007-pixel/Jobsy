namespace Jobsy.Web.Localization;

/// <summary>Chrome for "Werk met toekomst dat bij jou past". The outlook sentences stay the Dutch templates.</summary>
public static class UiStringsFutureJobs
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string plText, string roText, string arText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText;
            ro[key] = roText;
            ar[key] = arText;
        }

        Add("FutureJobs.Eyebrow",
            "Voor jou",
            "For you",
            "Dla ciebie",
            "Pentru tine",
            "لك");
        Add("FutureJobs.Title",
            "Werk met toekomst dat bij jou past",
            "Work with a future that fits you",
            "Praca z przyszłością, która do ciebie pasuje",
            "Muncă cu viitor care ți se potrivește",
            "عمل له مستقبل ويناسبك");
        Add("FutureJobs.Lead",
            "Deze banen passen bij jouw test. Bovenaan staat wat het best bij jou past. Is dat gelijk, dan staat werk waar meer mensen voor nodig zijn hoger.",
            "These jobs fit your test. The best fit is at the top. When the fit is the same, work that needs more people comes first.",
            "Te zawody pasują do twojego testu. Najlepsze dopasowanie jest na górze. Przy tym samym dopasowaniu wyżej jest praca, do której potrzeba więcej ludzi.",
            "Aceste meserii se potrivesc cu testul tău. Cea mai bună potrivire este sus. La potrivire egală, munca unde e nevoie de mai mulți oameni stă mai sus.",
            "هذه الوظائف تناسب اختبارك. الأنسب في الأعلى. وإذا تساوت الملاءمة، يأتي أولاً العمل الذي يحتاج إلى مزيد من الناس.");
        Add("FutureJobs.Region",
            "Heel Nederland",
            "All of the Netherlands",
            "Cała Holandia",
            "Toată Olanda",
            "كل هولندا");
        Add("FutureJobs.RegionNote",
            "Voor Westland en Haaglanden zijn er nog geen cijfers per baan.",
            "There are no figures per job for Westland and Haaglanden yet.",
            "Dla Westland i Haaglanden nie ma jeszcze liczb dla każdego zawodu.",
            "Pentru Westland și Haaglanden nu există încă cifre pentru fiecare meserie.",
            "لا توجد بعد أرقام لكل وظيفة في ويستلاند وهاخلاندن.");
        Add("FutureJobs.How",
            "Hoe kiezen we dit?",
            "How do we choose this?",
            "Jak to wybieramy?",
            "Cum alegem asta?",
            "كيف نختار هذا؟");
        Add("FutureJobs.How1",
            "We kijken naar banen die passen bij jouw test. Alleen banen waarvoor we echte cijfers hebben.",
            "We look at jobs that fit your test. Only jobs for which we have real figures.",
            "Patrzymy na zawody, które pasują do twojego testu. Tylko te, dla których mamy prawdziwe liczby.",
            "Ne uităm la meserii care se potrivesc cu testul tău. Doar cele pentru care avem cifre reale.",
            "ننظر إلى الوظائف التي تناسب اختبارك. فقط الوظائف التي لدينا أرقام حقيقية لها.");
        Add("FutureJobs.How2",
            "We houden banen waar werkgevers tot 2030 hard of heel hard mensen voor zoeken. Dat verwacht ROA, een onderzoeksbureau van de Universiteit Maastricht.",
            "We keep jobs where employers will badly need people up to 2030. That is what ROA expects. ROA is a research institute of Maastricht University.",
            "Zostawiamy zawody, w których do 2030 pracodawcy bardzo potrzebują ludzi. Tak przewiduje ROA, instytut badawczy Uniwersytetu w Maastricht.",
            "Păstrăm meseriile unde angajatorii au mare nevoie de oameni până în 2030. Așa estimează ROA, un institut al Universității din Maastricht.",
            "نبقي الوظائف التي سيحتاج فيها أصحاب العمل بشدة إلى أشخاص حتى 2030. هذا ما يتوقعه ROA، وهو معهد بحث في جامعة ماستريخت.");
        Add("FutureJobs.How3",
            "De baan die het best bij jouw test past staat bovenaan. Is dat gelijk, dan staat de baan waar meer plekken vrijkomen hoger.",
            "The job that fits your test best is at the top. When that is equal, the job with more openings comes first.",
            "Zawód, który najlepiej pasuje do twojego testu, jest na górze. Przy remisie wyżej jest zawód, w którym zwalnia się więcej miejsc.",
            "Meseria care se potrivește cel mai bine cu testul tău este sus. La egalitate, meseria cu mai multe locuri libere stă mai sus.",
            "الوظيفة الأنسب لاختبارك في الأعلى. وإذا تساوت، تأتي أولاً الوظيفة التي يتحرر فيها عدد أكبر من الأماكن.");
        Add("FutureJobs.How4",
            "Wat AI aan het werk verandert, zie je als extra regel. Dat telt niet mee voor de volgorde. Dit komt van de ILO, de werkorganisatie van de Verenigde Naties.",
            "What AI changes in the work is an extra line. It does not change the order. This comes from the ILO, the labour organisation of the United Nations.",
            "To, co AI zmienia w pracy, widzisz jako dodatkową linijkę. Nie liczy się to do kolejności. Dane pochodzą od ILO, organizacji pracy ONZ.",
            "Ce schimbă AI la muncă apare ca un rând în plus. Nu contează la ordine. Vine de la ILO, organizația pentru muncă a Națiunilor Unite.",
            "ما يغيّره الذكاء الاصطناعي في العمل يظهر كسطر إضافي. وهو لا يؤثر في الترتيب. المصدر هو ILO، منظمة العمل في الأمم المتحدة.");
        Add("FutureJobs.Scale",
            "Zo lees je “hard nodig”",
            "How to read “badly needed”",
            "Jak czytać „bardzo potrzebne”",
            "Cum citești „foarte căutat”",
            "كيف تقرأ «حاجة كبيرة»");
        Add("FutureJobs.Need.4",
            "Heel hard nodig",
            "Very badly needed",
            "Bardzo pilnie potrzebne",
            "Foarte mare nevoie",
            "حاجة كبيرة جداً");
        Add("FutureJobs.Need.3",
            "Hard nodig",
            "Badly needed",
            "Pilnie potrzebne",
            "Mare nevoie",
            "حاجة كبيرة");
        Add("FutureJobs.Need.2",
            "Een beetje tekort",
            "A small shortage",
            "Nieduży brak ludzi",
            "Un pic de lipsă",
            "نقص بسيط");
        Add("FutureJobs.Need.1",
            "Bijna genoeg mensen",
            "Almost enough people",
            "Prawie dość ludzi",
            "Aproape destui oameni",
            "الناس يكفي تقريباً");
        Add("FutureJobs.Need.0",
            "Genoeg mensen",
            "Enough people",
            "Dość ludzi",
            "Destui oameni",
            "الناس يكفي");
        Add("FutureJobs.Bar",
            "{0}, {1} van 5",
            "{0}, {1} of 5",
            "{0}, {1} z 5",
            "{0}, {1} din 5",
            "{0}، {1} من 5");
        Add("FutureJobs.Openings",
            "{0} van elke 100 banen komen vrij tot 2030",
            "{0} out of every 100 jobs come open by 2030",
            "{0} na każde 100 miejsc zwolni się do 2030",
            "{0} din fiecare 100 locuri se eliberează până în 2030",
            "{0} من كل 100 وظيفة تصبح شاغرة حتى 2030");
        Add("FutureJobs.Day",
            "Een dag als {0}",
            "A day as {0}",
            "Dzień jako {0}",
            "O zi ca {0}",
            "يوم كـ {0}");
        Add("FutureJobs.Check",
            "Past dit bij mij?",
            "Does this fit me?",
            "Czy to do mnie pasuje?",
            "Mi se potrivește?",
            "هل يناسبني هذا؟");
        Add("FutureJobs.Empty",
            "Doe eerst de test over werk dat bij je past. Daarna laten we hier banen zien waar werkgevers de komende jaren veel mensen voor zoeken.",
            "Do the test about work that fits you first. Then we show jobs here that employers will need many people for in the coming years.",
            "Zrób najpierw test o pracy, która do ciebie pasuje. Potem pokażemy tu zawody, do których pracodawcy będą szukać wielu ludzi.",
            "Fă mai întâi testul despre munca care ți se potrivește. Apoi arătăm aici meserii pentru care angajatorii caută mulți oameni.",
            "أجرِ أولاً اختبار العمل الذي يناسبك. بعدها نعرض هنا وظائف سيبحث أصحاب العمل عن كثير من الناس لها في السنوات القادمة.");
        Add("FutureJobs.EmptyCta",
            "Doe de test",
            "Do the test",
            "Zrób test",
            "Fă testul",
            "أجرِ الاختبار");
        Add("FutureJobs.None",
            "We hebben nu geen banen met cijfers die ook bij jouw test passen.",
            "We do not have jobs with figures that also fit your test right now.",
            "Nie mamy teraz zawodów z liczbami, które pasują też do twojego testu.",
            "Nu avem acum meserii cu cifre care se potrivesc și cu testul tău.",
            "ليس لدينا الآن وظائف بأرقام تناسب اختبارك أيضاً.");
        Add("FutureJobs.Country",
            "Cijfers voor heel Nederland.",
            "Figures for the whole Netherlands.",
            "Liczby dla całej Holandii.",
            "Cifre pentru toată Olanda.",
            "أرقام لكل هولندا.");
        Add("FutureJobs.WhyOne",
            "Past bij jou: {0}.",
            "Fits you: {0}.",
            "Pasuje do ciebie: {0}.",
            "Ți se potrivește: {0}.",
            "يناسبك: {0}.");
        Add("FutureJobs.WhyTwo",
            "Past bij jou: {0} en {1}.",
            "Fits you: {0} and {1}.",
            "Pasuje do ciebie: {0} i {1}.",
            "Ți se potrivește: {0} și {1}.",
            "يناسبك: {0} و{1}.");
        Add("FutureJobs.Trait.R",
            "je werkt graag met je handen",
            "you like working with your hands",
            "lubisz pracować rękami",
            "îți place să lucrezi cu mâinile",
            "تحب العمل بيديك");
        Add("FutureJobs.Trait.I",
            "je zoekt graag uit hoe iets werkt",
            "you like finding out how something works",
            "lubisz sprawdzać, jak coś działa",
            "îți place să afli cum funcționează ceva",
            "تحب أن تعرف كيف يعمل شيء");
        Add("FutureJobs.Trait.A",
            "je maakt graag iets eigens",
            "you like making something of your own",
            "lubisz robić coś własnego",
            "îți place să faci ceva al tău",
            "تحب أن تصنع شيئاً من عندك");
        Add("FutureJobs.Trait.S",
            "je helpt graag andere mensen",
            "you like helping other people",
            "lubisz pomagać innym",
            "îți place să ajuți alți oameni",
            "تحب مساعدة الآخرين");
        Add("FutureJobs.Trait.E",
            "je zet graag dingen in beweging",
            "you like getting things moving",
            "lubisz wprawiać sprawy w ruch",
            "îți place să pui lucrurile în mișcare",
            "تحب أن تحرّك الأمور");
        Add("FutureJobs.Trait.C",
            "je houdt van duidelijke taken",
            "you like clear tasks",
            "lubisz jasne zadania",
            "îți plac sarcinile clare",
            "تحب المهام الواضحة");
        Add("AdminSettings.FutureJobs.Enabled.Title",
            "Werk met toekomst",
            "Work with a future",
            "Praca z przyszłością",
            "Muncă cu viitor",
            "عمل له مستقبل");
        Add("AdminSettings.FutureJobs.Enabled.Desc",
            "Aan = op Functiefit en Carrière staat een lijst van banen die bij de test passen en waar werkgevers tot 2030 veel mensen voor zoeken. Uit = dat blok blijft verborgen. Er worden geen cijfers verzonnen. Werkgevers blijven uit.",
            "On = Functiefit and Career show jobs that fit the test and that employers will need many people for up to 2030. Off = that block stays hidden. No figures are invented. Employers stay off.",
            "Wł. = na Functiefit i w Kariera widać zawody pasujące do testu, do których do 2030 potrzeba wielu ludzi. Wył. = blok zostaje ukryty. Nie wymyślamy liczb. Pracodawcy zostają wyłączeni.",
            "Pornit = pe Functiefit și Carieră apar meserii care se potrivesc testului și pentru care angajatorii caută mulți oameni până în 2030. Oprit = blocul rămâne ascuns. Nu inventăm cifre. Angajatorii rămân opriți.",
            "تشغيل = تظهر في تبويب الملاءمة وفي المسار وظائف تناسب الاختبار ويحتاج أصحاب العمل إلى كثير من الناس لها حتى 2030. إيقاف = يبقى الجزء مخفياً. لا نختلق أرقاماً. أصحاب العمل يبقون متوقفين.");
    }
}
