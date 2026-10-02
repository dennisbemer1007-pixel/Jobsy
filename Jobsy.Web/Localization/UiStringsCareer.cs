namespace Jobsy.Web.Localization;

public static class UiStringsCareer
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

        Add("CareerErr.GenerationLimit",
            "Je kunt morgen weer een nieuw plan maken.",
            "You can create a new plan again tomorrow.",
            "Jutro znów możesz utworzyć nowy plan.",
            "Poți crea un plan nou mâine.",
            "يمكنك إنشاء خطة جديدة غداً.");
        Add("CareerErr.CompletePreviousFirst",
            "Maak eerst de stap ervoor af.",
            "Complete the previous step first.",
            "Najpierw ukończ poprzedni krok.",
            "Finalizează mai întâi pasul anterior.",
            "أكمل الخطوة السابقة أولاً.");
        Add("CareerErr.UndoLastFirst",
            "Je kunt alleen je laatste stap terugzetten.",
            "You can only undo your last completed step.",
            "Możesz cofnąć tylko ostatni ukończony krok.",
            "Poți anula doar ultimul pas finalizat.",
            "يمكنك التراجع عن آخر خطوة مكتملة فقط.");
        Add("CareerErr.DreamTextInvalid",
            "Schrijf alleen de naam van een beroep, bijvoorbeeld 'kok'.",
            "Enter a job title only, for example “chef”.",
            "Podaj tylko nazwę zawodu, na przykład „kucharz”.",
            "Introdu doar numele unui job, de exemplu „bucătar”.",
            "اكتب اسم مهنة فقط، مثل «طباخ».");
        Add("CareerErr.InProgress",
            "Lobsy maakt je plan al. Even geduld.",
            "Lobsy is already building your plan. Please wait.",
            "Lobsy już tworzy twój plan. Proszę czekać.",
            "Lobsy îți creează deja planul. Așteaptă puțin.",
            "لوبسي يبني خطتك بالفعل. انتظر قليلاً.");
        Add("CareerErr.UsePassportProof",
            "Voeg bewijs toe via je paspoort, niet via deze knop.",
            "Add proof via your passport, not this button.",
            "Dodaj dowód w paszporcie, nie tym przyciskiem.",
            "Adaugă dovada în pașaport, nu cu acest buton.",
            "أضف الإثبات عبر جوازك، وليس عبر هذا الزر.");
        Add("CareerErr.AiUnavailable",
            "We gebruiken een standaardplan. Probeer het later opnieuw voor een persoonlijk plan.",
            "We’re using a standard plan. Try again later for a personalised plan.",
            "Używamy planu standardowego. Spróbuj później po plan personalny.",
            "Folosim un plan standard. Încearcă mai târziu pentru un plan personal.",
            "نستخدم خطة افتراضية. جرّب لاحقاً للحصول على خطة شخصية.");
        Add("CareerErr.NoPlan",
            "Je hebt nog geen carrièreplan.",
            "You don’t have a career plan yet.",
            "Nie masz jeszcze planu kariery.",
            "Nu ai încă un plan de carieră.",
            "ليس لديك خطة مهنية بعد.");

        Add("CareerDream.Reason.Test",
            "Uit je Beroepen-test",
            "From your careers test",
            "Z testu zawodów",
            "Din testul de meserii",
            "من اختبار المهن");
        Add("CareerDream.Reason.Wish",
            "Past bij je wens",
            "Matches your preference",
            "Pasuje do twoich życzeń",
            "Se potrivește dorinței tale",
            "يتوافق مع رغبتك");

        Add("CareerFit.Good",
            "Past goed",
            "Good fit",
            "Dobrze pasuje",
            "Potrivire bună",
            "يتوافق جيداً");
        Add("CareerFit.Fair",
            "Past redelijk",
            "Fair fit",
            "Umiarkowanie pasuje",
            "Potrivire rezonabilă",
            "يتوافق بشكل معقول");
        Add("CareerFit.NotYet",
            "Past nog niet",
            "Not yet a fit",
            "Jeszcze nie pasuje",
            "Încă nu se potrivește",
            "لا يتوافق بعد");

        AddPage(Add);
        AddRail(Add);
        AddDream(Add);
        AddSay(Add);
    }

    private delegate void AddString(string key, string nl, string en, string pl, string ro, string ar);

    private static void AddPage(AddString Add)
    {
        Add("Career.Title",
            "Mijn carrière",
            "My career",
            "Moja kariera",
            "Cariera mea",
            "مسيرتي المهنية");
        Add("Career.Eyebrow",
            "Mijn carrière",
            "My career",
            "Moja kariera",
            "Cariera mea",
            "مسيرتي المهنية");
        Add("Career.EyebrowStep",
            "Mijn carrière · stap {0} van {1}",
            "My career · step {0} of {1}",
            "Moja kariera · krok {0} z {1}",
            "Cariera mea · pasul {0} din {1}",
            "مسيرتي المهنية · الخطوة {0} من {1}");
        Add("Career.Loading",
            "Je plan wordt opgehaald.",
            "Loading your plan.",
            "Ładujemy twój plan.",
            "Îți încărcăm planul.",
            "يتم تحميل خطتك.");
        Add("Career.Generating",
            "Ik maak je plan. Dat duurt even.",
            "I’m building your plan. This takes a moment.",
            "Tworzę twój plan. To chwilę potrwa.",
            "Îți creez planul. Durează un moment.",
            "أبني خطتك. سيستغرق ذلك لحظة.");

        Add("Career.Empty.Title",
            "Waar wil jij naartoe groeien?",
            "Where do you want to grow to?",
            "W jakim kierunku chcesz rosnąć?",
            "Încotro vrei să crești?",
            "إلى أين تريد أن تنمو؟");
        Add("Career.Empty.Lead",
            "Kies je droombaan. Lobsy maakt een plan met kleine stappen, van waar je nu bent tot daar. Je kunt altijd wisselen.",
            "Choose your dream job. Lobsy builds a plan with small steps, from where you are now to there. You can always switch.",
            "Wybierz wymarzoną pracę. Lobsy zrobi plan z małych kroków, od miejsca, w którym jesteś, aż tam. Zawsze możesz zmienić.",
            "Alege jobul visat. Lobsy face un plan cu pași mici, de unde ești acum până acolo. Poți schimba oricând.",
            "اختر وظيفة أحلامك. يصنع لوبسي خطة بخطوات صغيرة، من مكانك الآن إلى هناك. يمكنك التغيير في أي وقت.");
        Add("Career.Empty.SuggestTitle",
            "Past bij jou",
            "Fits you",
            "Pasuje do ciebie",
            "Ți se potrivește",
            "يناسبك");
        Add("Career.Empty.SuggestLead",
            "Uit je paspoort: wat je goed kunt en leuk vindt.",
            "From your passport: what you are good at and enjoy.",
            "Z twojego paszportu: w czym jesteś dobry i co lubisz.",
            "Din pașaportul tău: ce știi să faci și ce îți place.",
            "من جوازك: ما تجيده وما تحبه.");
        Add("Career.Empty.Or",
            "of",
            "or",
            "albo",
            "sau",
            "أو");
        Add("Career.Empty.DiscoveryHint",
            "Weet je het nog niet? Maak eerst de ontdekkingsreis af",
            "Not sure yet? Finish the discovery journey first",
            "Jeszcze nie wiesz? Najpierw skończ podróż odkrywczą",
            "Încă nu știi? Termină mai întâi călătoria de descoperire",
            "لا تعرف بعد؟ أكمل رحلة الاستكشاف أولاً");
        Add("Career.Empty.Cta",
            "Maak mijn groeiplan",
            "Build my growth plan",
            "Zrób mój plan rozwoju",
            "Creează planul meu de creștere",
            "أنشئ خطة نموي");

        Add("Career.Search.Label",
            "Zelf een beroep zoeken",
            "Search for a job yourself",
            "Sam poszukaj zawodu",
            "Caută singur o meserie",
            "ابحث عن مهنة بنفسك");
        Add("Career.Search.Placeholder",
            "Bijvoorbeeld kok, monteur of leraar",
            "For example chef, mechanic or teacher",
            "Na przykład kucharz, mechanik lub nauczyciel",
            "De exemplu bucătar, mecanic sau profesor",
            "مثلاً طباخ أو ميكانيكي أو مدرّس");
        Add("Career.Search.Hint",
            "We zoeken in een lijst met echte beroepen. Zo klopt je plan beter.",
            "We search a list of real jobs. That makes your plan more accurate.",
            "Szukamy na liście prawdziwych zawodów. Dzięki temu plan jest trafniejszy.",
            "Căutăm într-o listă de meserii reale. Așa planul e mai corect.",
            "نبحث في قائمة مهن حقيقية. بذلك تكون خطتك أدق.");
        Add("Career.Search.NotFound",
            "Ik vind mijn beroep niet",
            "I can’t find my job",
            "Nie znajduję swojego zawodu",
            "Nu îmi găsesc meseria",
            "لا أجد مهنتي");
        Add("Career.Search.FreeLabel",
            "Naam van het beroep",
            "Name of the job",
            "Nazwa zawodu",
            "Numele meseriei",
            "اسم المهنة");
        Add("Career.Search.FreeHint",
            "Schrijf alleen de naam van een beroep.",
            "Write only the name of a job.",
            "Napisz tylko nazwę zawodu.",
            "Scrie doar numele unei meserii.",
            "اكتب اسم مهنة فقط.");
        Add("Career.Search.Results",
            "Beroepen die passen bij wat je typt",
            "Jobs matching what you type",
            "Zawody pasujące do tego, co wpisujesz",
            "Meserii care se potrivesc cu ce scrii",
            "مهن تطابق ما تكتبه");
        Add("Career.Suggest.Fit",
            "Past goed bij je",
            "Fits you well",
            "Dobrze do ciebie pasuje",
            "Ți se potrivește bine",
            "يناسبك جيداً");

        Add("Career.Overview.Title",
            "Op weg naar {0}",
            "On your way to {0}",
            "W drodze do {0}",
            "În drum spre {0}",
            "في طريقك إلى {0}");
        Add("Career.Overview.TitleShort",
            "Naar {0}",
            "To {0}",
            "Do {0}",
            "Spre {0}",
            "إلى {0}");
        Add("Career.Overview.Lead",
            "Een kreeft groeit alleen als hij zijn oude schaal loslaat. Zo groei jij ook: steen voor steen.",
            "A lobster only grows when it lets go of its old shell. You grow the same way: stone by stone.",
            "Homar rośnie tylko wtedy, gdy zrzuci starą skorupę. Ty rośniesz tak samo: kamień po kamieniu.",
            "Un homar crește doar când își lasă carapacea veche. Așa crești și tu: piatră cu piatră.",
            "لا ينمو الكركند إلا إذا تخلّى عن قوقعته القديمة. وأنت تنمو كذلك: حجراً بعد حجر.");
        Add("Career.Overview.NowEyebrow",
            "Nu aan de beurt · groeit nu",
            "Up next · growing now",
            "Teraz twoja kolej · rośnie teraz",
            "Acum e rândul · crește acum",
            "الدور الآن · ينمو الآن");
        Add("Career.Overview.ViewStep",
            "Bekijk deze stap",
            "View this step",
            "Zobacz ten krok",
            "Vezi acest pas",
            "اعرض هذه الخطوة");
        Add("Career.Fact.Claws",
            "Nog {0} klauwen laten groeien",
            "{0} claws still to grow",
            "Jeszcze {0} szczypce do wyhodowania",
            "Mai ai {0} clești de crescut",
            "بقي {0} من المخالب لتنمو");
        Add("Career.Fact.Courses",
            "{0} opleidingen · {1} is gratis",
            "{0} courses · {1} is free",
            "{0} szkoleń · {1} jest bezpłatne",
            "{0} cursuri · {1} este gratuit",
            "{0} دورات · {1} مجانية");
        Add("Career.Fact.Band",
            "Deze steen past al {0} bij jou",
            "This stone already fits you {0}",
            "Ten kamień już {0} do ciebie pasuje",
            "Piatra aceasta ți se potrivește deja {0}",
            "هذا الحجر يناسبك {0} بالفعل");
        Add("Career.Have.Title",
            "Wat je al hebt",
            "What you already have",
            "Co już masz",
            "Ce ai deja",
            "ما لديك بالفعل");
        Add("Career.Have.TagPassport",
            "In je paspoort",
            "In your passport",
            "W twoim paszporcie",
            "În pașaportul tău",
            "في جوازك");
        Add("Career.Have.TagProfile",
            "In je profiel",
            "In your profile",
            "W twoim profilu",
            "În profilul tău",
            "في ملفك");
        Add("Career.CarriedOver",
            "Je hebt al {0} stappen gehaald. Die tellen mee.",
            "You already completed {0} steps. They still count.",
            "Ukończyłeś już {0} kroków. One się liczą.",
            "Ai finalizat deja {0} pași. Ei contează.",
            "أنجزت بالفعل {0} خطوات. وهي محسوبة.");
        Add("Career.Dismiss",
            "Sluiten",
            "Close",
            "Zamknij",
            "Închide",
            "إغلاق");
        Add("Career.Source.Ai",
            "Plan gemaakt met hulp van AI, op basis van je paspoort. Klopt iets niet? Zeg het ons.",
            "Plan made with help from AI, based on your passport. Something off? Tell us.",
            "Plan powstał z pomocą AI, na podstawie twojego paszportu. Coś się nie zgadza? Napisz nam.",
            "Planul a fost făcut cu ajutorul AI, pe baza pașaportului tău. Ceva nu e în regulă? Spune-ne.",
            "أُعدّت الخطة بمساعدة الذكاء الاصطناعي، بناءً على جوازك. هل هناك خطأ؟ أخبرنا.");
        Add("Career.Source.Local",
            "Plan gemaakt op basis van je paspoort.",
            "Plan made from your passport.",
            "Plan powstał na podstawie twojego paszportu.",
            "Planul a fost făcut pe baza pașaportului tău.",
            "أُعدّت الخطة بناءً على جوازك.");
        Add("Career.Language.Line",
            "Je plan is in het {0}. Maak je plan opnieuw in het {1}?",
            "Your plan is in {0}. Rebuild your plan in {1}?",
            "Twój plan jest w języku {0}. Zrobić plan ponownie w języku {1}?",
            "Planul tău este în {0}. Refacem planul în {1}?",
            "خطتك باللغة {0}. هل نعيد إنشاء خطتك باللغة {1}؟");
        Add("Career.Language.Action",
            "Plan opnieuw maken",
            "Rebuild plan",
            "Zrób plan ponownie",
            "Refă planul",
            "أعد إنشاء الخطة");
        Add("Career.Goal.Title",
            "Je bent er! Je bent klaar voor {0}.",
            "You made it! You are ready for {0}.",
            "Udało się! Jesteś gotowy na {0}.",
            "Ai reușit! Ești pregătit pentru {0}.",
            "وصلت! أنت جاهز لـ {0}.");
        Add("Career.Goal.Vacancies",
            "Bekijk vacatures",
            "View jobs",
            "Zobacz ogłoszenia",
            "Vezi joburile",
            "اعرض الوظائف");
        Add("Career.Goal.Passport",
            "Bekijk je paspoort",
            "View your passport",
            "Zobacz swój paszport",
            "Vezi pașaportul tău",
            "اعرض جوازك");
        Add("Career.Goal.NewDream",
            "Kies een nieuwe droombaan",
            "Choose a new dream job",
            "Wybierz nową wymarzoną pracę",
            "Alege un nou job visat",
            "اختر وظيفة أحلام جديدة");
        Add("Career.EditDream",
            "Droombaan wijzigen",
            "Change dream job",
            "Zmień wymarzoną pracę",
            "Schimbă jobul visat",
            "تغيير وظيفة الأحلام");

        Add("Career.Lang.nl",
            "Nederlands",
            "Dutch",
            "niderlandzkim",
            "olandeză",
            "الهولندية");
        Add("Career.Lang.en",
            "Engels",
            "English",
            "angielskim",
            "engleză",
            "الإنجليزية");
        Add("Career.Lang.pl",
            "Pools",
            "Polish",
            "polskim",
            "poloneză",
            "البولندية");
        Add("Career.Lang.ro",
            "Roemeens",
            "Romanian",
            "rumuńskim",
            "română",
            "الرومانية");
        Add("Career.Lang.ar",
            "Arabisch",
            "Arabic",
            "arabskim",
            "arabă",
            "العربية");
    }

    private static void AddRail(AddString Add)
    {
        Add("Career.Rail.Title",
            "Jouw groeireis",
            "Your growth journey",
            "Twoja podróż rozwoju",
            "Călătoria ta de creștere",
            "رحلة نموك");
        Add("Career.Rail.To",
            "Naar {0}",
            "To {0}",
            "Do {0}",
            "Spre {0}",
            "إلى {0}");
        Add("Career.Rail.NoDream",
            "Nog geen droombaan gekozen",
            "No dream job chosen yet",
            "Nie wybrano jeszcze wymarzonej pracy",
            "Încă nu ai ales jobul visat",
            "لم تختر وظيفة أحلامك بعد");
        Add("Career.Rail.Zone.Deep",
            "In de diepte · waar je nu bent",
            "In the deep · where you are now",
            "W głębinie · gdzie jesteś teraz",
            "În adânc · unde ești acum",
            "في العمق · حيث أنت الآن");
        Add("Career.Rail.Zone.Climb",
            "De klim",
            "The climb",
            "Wspinaczka",
            "Urcarea",
            "الصعود");
        Add("Career.Rail.Zone.Light",
            "Naar het licht",
            "Towards the light",
            "W stronę światła",
            "Spre lumină",
            "نحو النور");
        Add("Career.Rail.Now",
            "Nu",
            "Now",
            "Teraz",
            "Acum",
            "الآن");
        Add("Career.Rail.NowUnknown",
            "Waar je nu bent",
            "Where you are now",
            "Gdzie jesteś teraz",
            "Unde ești acum",
            "حيث أنت الآن");
        Add("Career.Rail.NewShell",
            "Nieuwe schaal",
            "New shell",
            "Nowa skorupa",
            "Carapace nouă",
            "قوقعة جديدة");
        Add("Career.Rail.GrowingNow",
            "Groeit nu",
            "Growing now",
            "Rośnie teraz",
            "Crește acum",
            "ينمو الآن");
        Add("Career.Rail.GrowingNowLevel",
            "Groeit nu · {0}",
            "Growing now · {0}",
            "Rośnie teraz · {0}",
            "Crește acum · {0}",
            "ينمو الآن · {0}");
        Add("Career.Rail.Goal",
            "Jouw droombaan",
            "Your dream job",
            "Twoja wymarzona praca",
            "Jobul tău visat",
            "وظيفة أحلامك");
        Add("Career.Rail.GoalLevel",
            "Jouw droombaan · {0}",
            "Your dream job · {0}",
            "Twoja wymarzona praca · {0}",
            "Jobul tău visat · {0}",
            "وظيفة أحلامك · {0}");
        Add("Career.Rail.Shells",
            "{0} van {1} nieuwe schalen",
            "{0} of {1} new shells",
            "{0} z {1} nowych skorup",
            "{0} din {1} carapace noi",
            "{0} من {1} قواقع جديدة");
        Add("Career.Rail.Saved",
            "Alles is bewaard. Wisselen mag altijd.",
            "Everything is saved. You can always switch.",
            "Wszystko jest zapisane. Zawsze możesz zmienić.",
            "Totul e salvat. Poți schimba oricând.",
            "كل شيء محفوظ. يمكنك التغيير دائماً.");
        Add("Career.Rail.EmptyNote",
            "Je plan is alleen voor jou.",
            "Your plan is only for you.",
            "Twój plan jest tylko dla ciebie.",
            "Planul tău e doar pentru tine.",
            "خطتك لك وحدك.");
        Add("Career.Stepper.Aria",
            "Je stappen: van Nu naar je droombaan",
            "Your steps: from Now to your dream job",
            "Twoje kroki: od Teraz do wymarzonej pracy",
            "Pașii tăi: de la Acum la jobul visat",
            "خطواتك: من الآن إلى وظيفة أحلامك");
        Add("Career.Stepper.Goal",
            "Doel",
            "Goal",
            "Cel",
            "Obiectiv",
            "الهدف");
    }

    private static void AddDream(AddString Add)
    {
        Add("CareerDream.DialogTitle",
            "Een andere droombaan kiezen?",
            "Choose a different dream job?",
            "Wybrać inną wymarzoną pracę?",
            "Alegi un alt job visat?",
            "أتختار وظيفة أحلام أخرى؟");
        Add("CareerDream.DialogLead",
            "Soms past een andere steen beter. Dat is helemaal goed.",
            "Sometimes another stone fits better. That is completely fine.",
            "Czasem inny kamień pasuje lepiej. To zupełnie w porządku.",
            "Uneori o altă piatră se potrivește mai bine. E perfect în regulă.",
            "أحياناً يناسبك حجر آخر أفضل. لا مشكلة في ذلك أبداً.");
        Add("CareerDream.KeepTitle",
            "Wat gebeurt er met wat je al deed?",
            "What happens to what you already did?",
            "Co stanie się z tym, co już zrobiłeś?",
            "Ce se întâmplă cu ce ai făcut deja?",
            "ماذا يحدث لما أنجزته بالفعل؟");
        Add("CareerDream.KeepLine1",
            "Wat je haalde, blijft in je paspoort: {0}.",
            "What you achieved stays in your passport: {0}.",
            "To, co zdobyłeś, zostaje w twoim paszporcie: {0}.",
            "Ce ai obținut rămâne în pașaportul tău: {0}.",
            "ما حققته يبقى في جوازك: {0}.");
        Add("CareerDream.KeepSummaryFallback",
            "alles wat je al deed",
            "everything you already did",
            "wszystko, co już zrobiłeś",
            "tot ce ai făcut deja",
            "كل ما أنجزته");
        Add("CareerDream.KeepSummaryCourses",
            "{0} cursussen",
            "{0} courses",
            "{0} kursów",
            "{0} cursuri",
            "{0} دورات");
        Add("CareerDream.KeepSummaryCoursesOne",
            "1 cursus",
            "1 course",
            "1 kurs",
            "1 curs",
            "دورة واحدة");
        Add("CareerDream.KeepSummaryExperience",
            "je werkervaring",
            "your work experience",
            "twoje doświadczenie zawodowe",
            "experiența ta de muncă",
            "خبرتك العملية");
        Add("CareerDream.KeepSummaryJoin",
            "{0} en {1}",
            "{0} and {1}",
            "{0} i {1}",
            "{0} și {1}",
            "{0} و{1}");
        Add("CareerDream.KeepLine2",
            "Je nieuwe plan telt dat mee. Je begint niet opnieuw.",
            "Your new plan counts that. You don’t start over.",
            "Nowy plan to uwzględnia. Nie zaczynasz od nowa.",
            "Noul plan ține cont de asta. Nu începi de la zero.",
            "خطتك الجديدة تحسب ذلك. لن تبدأ من جديد.");
        Add("CareerDream.KeepLine3",
            "Je oude plan bewaren we 30 dagen. Terugzetten kan.",
            "We keep your old plan for 30 days. You can restore it.",
            "Twój stary plan trzymamy 30 dni. Możesz go przywrócić.",
            "Păstrăm planul vechi 30 de zile. Îl poți restaura.",
            "نحفظ خطتك القديمة 30 يوماً. يمكنك استعادتها.");
        Add("CareerDream.Stay",
            "Blijf bij {0}",
            "Stay with {0}",
            "Zostań przy {0}",
            "Rămâi la {0}",
            "ابقَ مع {0}");
        Add("CareerDream.StayShort",
            "Blijf bij mijn plan",
            "Stay with my plan",
            "Zostań przy moim planie",
            "Rămâi la planul meu",
            "ابقَ مع خطتي");
        Add("CareerDream.Confirm",
            "Maak nieuw plan",
            "Build new plan",
            "Zrób nowy plan",
            "Creează planul nou",
            "أنشئ خطة جديدة");
        Add("CareerDream.Ready",
            "Je nieuwe plan staat klaar",
            "Your new plan is ready",
            "Twój nowy plan jest gotowy",
            "Noul tău plan e gata",
            "خطتك الجديدة جاهزة");
        Add("CareerDream.ArchiveTitle",
            "Eerdere plannen ({0})",
            "Earlier plans ({0})",
            "Wcześniejsze plany ({0})",
            "Planuri anterioare ({0})",
            "خطط سابقة ({0})");
        Add("CareerDream.ArchiveDays",
            "nog {0} dagen bewaard",
            "kept for {0} more days",
            "przechowywany jeszcze {0} dni",
            "păstrat încă {0} zile",
            "محفوظ {0} أيام أخرى");
        Add("CareerDream.ArchiveSteps",
            "{0} van {1} stappen",
            "{0} of {1} steps",
            "{0} z {1} kroków",
            "{0} din {1} pași",
            "{0} من {1} خطوات");
        Add("CareerDream.Restore",
            "Zet terug",
            "Restore",
            "Przywróć",
            "Restaurează",
            "استعادة");
        Add("CareerDream.RestoreTitle",
            "Terug naar {0}?",
            "Back to {0}?",
            "Wrócić do {0}?",
            "Revii la {0}?",
            "العودة إلى {0}؟");
        Add("CareerDream.RestoreLead",
            "Je huidige plan bewaren we ook 30 dagen.",
            "We keep your current plan for 30 days too.",
            "Twój obecny plan też zatrzymamy na 30 dni.",
            "Păstrăm și planul actual 30 de zile.",
            "سنحفظ خطتك الحالية 30 يوماً أيضاً.");
        Add("CareerDream.Cancel",
            "Annuleren",
            "Cancel",
            "Anuluj",
            "Anulează",
            "إلغاء");
    }

    private static void AddSay(AddString Add)
    {
        Add("Career.Say.Empty",
            "Met mijn antennes voel ik al een paar stenen die bij je passen. Welke wil jij?",
            "With my antennas I already feel a few stones that fit you. Which one do you want?",
            "Czułkami wyczuwam już kilka kamieni, które do ciebie pasują. Który wybierasz?",
            "Cu antenele simt deja câteva pietre care ți se potrivesc. Pe care o vrei?",
            "بقرون استشعاري أشعر بأحجار تناسبك. أيها تريد؟");
        Add("Career.Say.EmptyShort",
            "Welke steen past bij jou?",
            "Which stone fits you?",
            "Który kamień do ciebie pasuje?",
            "Care piatră ți se potrivește?",
            "أي حجر يناسبك؟");
        Add("Career.Say.Climb",
            "Ik zit nu op steen {0}. Mijn schaal wordt al krap. Dat is goed: dan groei ik.",
            "I’m on stone {0} now. My shell is getting tight. That is good: it means I grow.",
            "Jestem teraz na kamieniu {0}. Skorupa robi się ciasna. To dobrze: znaczy, że rosnę.",
            "Sunt acum pe piatra {0}. Carapacea îmi devine strâmtă. E bine: înseamnă că cresc.",
            "أنا الآن على الحجر {0}. قوقعتي تضيق. هذا جيد: يعني أنني أنمو.");
        Add("Career.Say.ClimbShort",
            "Steen {0}. Mijn schaal wordt krap: ik groei!",
            "Stone {0}. My shell is getting tight: I’m growing!",
            "Kamień {0}. Skorupa się zacieśnia: rosnę!",
            "Piatra {0}. Carapacea mi se strânge: cresc!",
            "الحجر {0}. قوقعتي تضيق: أنا أنمو!");
        Add("Career.Say.Goal",
            "Je staat op de gouden steen. Mooi geklommen!",
            "You are standing on the golden stone. Nicely climbed!",
            "Stoisz na złotym kamieniu. Dobra wspinaczka!",
            "Stai pe piatra de aur. Ai urcat frumos!",
            "أنت على الحجر الذهبي. صعود جميل!");
    }
}
