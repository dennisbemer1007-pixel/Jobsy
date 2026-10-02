namespace Jobsy.Web.Localization;

public static class UiStringsPassport
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

        Add("Nav.Passport",
            "Mijn Paspoort", "My Passport", "Mój Paszport", "Pașaportul meu", "جواز سفري");
        Add("Nav.Passport.Short",
            "Paspoort", "Passport", "Paszport", "Pașaport", "الجواز");
        Add("Nav.Discovery",
            "De ontdekkingsreis", "The discovery journey", "Podróż odkrywcza", "Călătoria de descoperire", "رحلة الاكتشاف");
        Add("Nav.Discovery.Short",
            "Reis", "Journey", "Podróż", "Călătorie", "الرحلة");
        // Passport-ON Banenkaart slot (legacy Nav.Search "Zoeken" kept for passport-OFF / other surfaces).
        Add("Nav.Banenkaart",
            "Banenkaart", "Job map", "Mapa ofert", "Hartă joburi", "خريطة الوظائف");
        Add("Nav.ApplicationsTab",
            "Sollicitaties", "Applications", "Aplikacje", "Candidaturi", "الطلبات");
        Add("Nav.SavedTab",
            "Bewaard", "Saved", "Zapisane", "Salvate", "المحفوظات");

        Add("Passport.Title",
            "Mijn Lobsy-paspoort", "My Lobsy passport", "Mój paszport Lobsy", "Pașaportul meu Lobsy", "جواز سفر لوبسي");
        Add("Passport.Banner",
            "LOBSY PASPOORT", "LOBSY PASSPORT", "PASZPORT LOBSY", "PAȘAPORT LOBSY", "جواز لوبسي");
        Add("Passport.MemberNo",
            "Lidnummer {0}", "Member {0}", "Numer {0}", "Membru {0}", "العضو {0}");
        Add("Passport.StartedStamp",
            "GESTART {0}", "STARTED {0}", "START {0}", "ÎNCEPUT {0}", "بدأ {0}");
        Add("Passport.Layers",
            "{0} van 5 lagen", "{0} of 5 layers", "{0} z 5 warstw", "{0} din 5 straturi", "{0} من 5 طبقات");
        Add("Passport.LayersAria",
            "Profiel {0} procent compleet", "Profile {0} percent complete", "Profil kompletny w {0} procent", "Profil complet în proporție de {0} la sută", "الملف مكتمل بنسبة {0} بالمئة");
        Add("Passport.Fill",
            "aanvullen", "complete", "uzupełnij", "completează", "أكمل");
        Add("Passport.MyStory",
            "Mijn verhaal", "My story", "Moja historia", "Povestea mea", "قصتي");
        Add("Passport.MyStoryEmpty",
            "Schrijf in een paar zinnen wie je bent.", "Write a few sentences about who you are.", "Napisz w kilku zdaniach kim jesteś.", "Scrie în câteva propoziții cine ești.", "اكتب بضع جمل عمّن أنت.");
        Add("Passport.Facts.LivesIn",
            "Ik woon in", "I live in", "Mieszkam w", "Locuiesc în", "أسكن في");
        Add("Passport.Facts.Travel",
            "Reizen", "Travel", "Dojazd", "Deplasare", "التنقل");
        Add("Passport.Facts.Hours",
            "Uren per week", "Hours per week", "Godziny tygodniowo", "Ore pe săptămână", "ساعات أسبوعياً");
        Add("Passport.Facts.Available",
            "Beschikbaar", "Available", "Dostępność", "Disponibil", "متاح");
        Add("Passport.Facts.LookingFor",
            "Wat ik zoek", "What I’m looking for", "Czego szukam", "Ce caut", "ما أبحث عنه");
        Add("Passport.Facts.License",
            "Rijbewijs", "Licence", "Prawo jazdy", "Permis", "رخصة القيادة");
        Add("Passport.LobsyCv",
            "Lobsy-CV", "Lobsy CV", "CV Lobsy", "CV Lobsy", "سيرة Lobsy");
        Add("Passport.Edit",
            "Aanpassen", "Edit", "Edytuj", "Editează", "تعديل");
        Add("Passport.Tagline",
            "Lobsy: ontdek wie je bent onder de schaal", "Lobsy: discover who you are under the shell", "Lobsy: odkryj kim jesteś pod skorupą", "Lobsy: descoperă cine ești sub cochilie", "لوبسي: اكتشف من أنت تحت الصدفة");

        Add("Passport.Overview.Title",
            "Dit ben jij", "This is you", "To jesteś ty", "Acesta ești tu", "هذا أنت");
        Add("Passport.Overview.Stage",
            "Jouw kreeft · fase {0} van 4", "Your lobster · stage {0} of 4", "Twój homar · faza {0} z 4", "Homarul tău · etapa {0} din 4", "كركندك · المرحلة {0} من 4");
        Add("Passport.Overview.Egg",
            "Jouw kreeft · nog in het ei", "Your lobster · still in the egg", "Twój homar · jeszcze w jajku", "Homarul tău · încă în ou", "كركندك · ما زال في البيضة");
        Add("Passport.Legend.Competence",
            "Competenties", "Competencies", "Kompetencje", "Competențe", "الكفاءات");
        Add("Passport.Legend.Career",
            "Beroepen", "Careers", "Zawody", "Meserii", "المهن");
        Add("Passport.Legend.Culture",
            "Cultuur", "Culture", "Kultura", "Cultură", "الثقافة");
        Add("Passport.Legend.Values",
            "Waarden", "Values", "Wartości", "Valori", "القيم");

        Add("Passport.Stat.Strongest",
            "Jouw sterkste klauw", "Your strongest claw", "Twoja najsilniejsza szczypca", "Clea ta cea mai puternică", "مخلبك الأقوى");
        Add("Passport.Stat.Work",
            "Werk dat bij je past", "Work that fits you", "Praca, która do ciebie pasuje", "Muncă care ți se potrivește", "عمل يناسبك");
        Add("Passport.Stat.Home",
            "Hier voel je je thuis", "Where you feel at home", "Tu czujesz się jak w domu", "Aici te simți acasă", "هنا تشعر بالانتماء");
        Add("Passport.Stat.Important",
            "Dit vind je belangrijk", "What matters to you", "To jest dla ciebie ważne", "Asta contează pentru tine", "هذا يهمك");
        Add("Passport.Stat.NotYet",
            "Nog niet ontdekt", "Not discovered yet", "Jeszcze nieodkryte", "Încă nedescoperit", "لم يُكتشف بعد");
        Add("Passport.Stat.DoTest",
            "Doe de test", "Take the test", "Zrób test", "Fă testul", "ابدأ الاختبار");

        Add("Passport.Tab.Dna",
            "Mijn DNA", "My DNA", "Moje DNA", "ADN-ul meu", "حمضي النووي");
        Add("Passport.Tab.Tests",
            "Mijn tests", "My tests", "Moje testy", "Testele mele", "اختباراتي");
        Add("Passport.Tab.Fit",
            "Past deze baan?", "Does this job fit?", "Czy ta praca pasuje?", "Se potrivește jobul?", "هل تناسب هذه الوظيفة؟");
        Add("Passport.Tab.Career",
            "Carrière", "Career", "Kariera", "Carieră", "المسار المهني");
        Add("Passport.Tab.Proof",
            "Bewijzen", "Proof", "Dowody", "Dovezi", "الإثباتات");
        Add("Passport.Tab.Data",
            "Mijn gegevens", "My details", "Moje dane", "Datele mele", "بياناتي");

        Add("Passport.Bubble.Dna",
            "Je hebt net een laag afgeworpen. Dit is wie eronder zat.",
            "You just shed a layer. This is who was underneath.",
            "Właśnie zrzuciłeś warstwę. Oto kto był pod spodem.",
            "Tocmai ai lepădat un strat. Iată cine era dedesubt.",
            "لقد خلعت طبقة للتو. هذا من كان تحتها.");
        Add("Passport.Bubble.DnaEmpty",
            "Hier werp je je oude schaal af. Begin met een test.",
            "Here you shed your old shell. Start with a test.",
            "Tu zrzucasz starą skorupę. Zacznij od testu.",
            "Aici lași cochilia veche. Începe cu un test.",
            "هنا تخلع صدفتك القديمة. ابدأ باختبار.");
        Add("Passport.Bubble.Tests",
            "Niet zoeken aan de oppervlakte. Duik dieper.",
            "Don’t skim the surface. Dive deeper.",
            "Nie szukaj po powierzchni. Zanurz się głębiej.",
            "Nu căuta la suprafață. Scufundă-te mai adânc.",
            "لا تبحث على السطح. اغص أعمق.");
        Add("Passport.Bubble.Fit",
            "Je antennes wijzen deze kant op. Twijfel je? Typ een baan, ik kijk mee.",
            "Your antennae point this way. Not sure? Type a job and I’ll look with you.",
            "Twoje czułki wskazują tę stronę. Wątpisz? Wpisz stanowisko, spojrzę razem z tobą.",
            "Antenele tale arată încoace. Ai îndoieli? Scrie un job, mă uit cu tine.",
            "هوائياتك تشير إلى هنا. متردد؟ اكتب وظيفة وأتابع معك.");
        Add("Passport.Bubble.Career",
            "Elke stap is een stukje nieuwe schaal dat aangroeit.",
            "Every step is a bit of new shell growing.",
            "Każdy krok to kawałek nowej skorupy, która narasta.",
            "Fiecare pas e un pic de cochilie nouă care crește.",
            "كل خطوة قطعة صدفة جديدة تنمو.");
        Add("Passport.Bubble.Proof",
            "Dit is je nieuwe, sterkere schaal. Alles wat je deed telt, ook mantelzorg.",
            "This is your new, stronger shell. Everything you did counts, including care work.",
            "To twoja nowa, mocniejsza skorupa. Wszystko się liczy, także opieka.",
            "Aceasta e cochilia ta nouă, mai puternică. Tot ce ai făcut contează, inclusiv îngrijirea.",
            "هذه صدفتك الجديدة الأقوى. كل ما فعلته يحسب، بما فيه الرعاية.");
        Add("Passport.Bubble.Data",
            "Jouw schaal, jouw regels. Jij bepaalt wie wat ziet.",
            "Your shell, your rules. You decide who sees what.",
            "Twoja skorupa, twoje zasady. Ty decydujesz, kto co widzi.",
            "Cochilia ta, regulile tale. Tu decizi cine ce vede.",
            "صدفتك، قواعدك. أنت تقرر من يرى ماذا.");

        Add("Passport.Dna.Eyebrow",
            "Onder je schaal", "Under your shell", "Pod skorupą", "Sub cochilie", "تحت صدفتك");
        Add("Passport.Dna.YouTitle",
            "Wat maakt jou jou", "What makes you you", "Co czyni cię tobą", "Ce te face pe tine", "ما يجعلك أنت");
        Add("Passport.Dna.Home",
            "Waar voel jij je thuis", "Where you feel at home", "Gdzie czujesz się jak w domu", "Unde te simți acasă", "أين تشعر بالانتماء");
        Add("Passport.Dna.ValuesProvisional",
            "Waarden op werk", "Values at work", "Wartości w pracy", "Valori la muncă", "قيم في العمل");

        Add("Passport.Shells.Title",
            "Mijn schalen", "My shells", "Moje skorupy", "Cochiliile mele", "صدفاتي");
        Add("Passport.Shells.Sub",
            "Hier werp je je oude schaal af · {0} van {1}",
            "Here you shed your old shell · {0} of {1}",
            "Tu zrzucasz starą skorupę · {0} z {1}",
            "Aici lași cochilia veche · {0} din {1}",
            "هنا تخلع صدفتك القديمة · {0} من {1}");
        Add("Passport.Shells.Tagline",
            "Groot worden doe je door je schaal af te werpen.",
            "You grow by shedding your shell.",
            "Rośniesz zrzucając skorupę.",
            "Crești lepădând cochilia.",
            "تنمو بخلع صدفتك.");
        Add("Passport.Shells.Next",
            "Nog {0} vragen tot je volgende schaal",
            "{0} questions until your next shell",
            "Jeszcze {0} pytań do następnej skorupy",
            "Încă {0} întrebări până la următoarea cochilie",
            "بقي {0} أسئلة حتى صدفتك التالية");
        Add("Passport.Shells.Item.FirstTest",
            "Eerste test", "First test", "Pierwszy test", "Primul test", "أول اختبار");
        Add("Passport.Shells.Item.FirstReport",
            "Eerste rapport", "First report", "Pierwszy raport", "Primul raport", "أول تقرير");
        Add("Passport.Shells.Item.Cv",
            "CV erbij", "CV added", "CV dodane", "CV adăugat", "أُضيفت السيرة");
        Add("Passport.Shells.Item.ThreeTests",
            "3 tests gedaan", "3 tests done", "3 testy zrobione", "3 teste făcute", "أُنجزت 3 اختبارات");
        Add("Passport.Shells.Item.DnaComplete",
            "DNA compleet", "DNA complete", "DNA kompletne", "ADN complet", "الحمض مكتمل");
        Add("Passport.Shells.Item.FirstJob",
            "Eerste baan", "First application", "Pierwsza aplikacja", "Prima candidatură", "أول طلب");

        Add("Passport.Transitional.OpenData",
            "Open je gegevens", "Open your details", "Otwórz swoje dane", "Deschide datele tale", "افتح بياناتك");
        Add("Passport.Career.Lead",
            "Je loopbaanplan staat op Mijn carrière.",
            "Your career plan lives on My career.",
            "Twój plan kariery jest w Moja kariera.",
            "Planul tău de carieră e la Cariera mea.",
            "خطة مسارك في مساري المهني.");
        Add("Passport.Career.Open",
            "Mijn loopbaanplan", "My career plan", "Mój plan kariery", "Planul meu de carieră", "خطة مساري");

        Add("Passport.Fit.Title",
            "Past deze baan bij mij?", "Does this job fit me?", "Czy ta praca do mnie pasuje?", "Mi se potrivește jobul?", "هل تناسبني هذه الوظيفة؟");
        Add("Passport.Fit.Subtitle",
            "Vind de omgeving waar jouw antennes tot rust komen.",
            "Find the place where your antennae can settle.",
            "Znajdź miejsce, w którym twoje czułki mogą odpocząć.",
            "Găsește locul unde antenele tale se liniștesc.",
            "اعثر على المكان الذي تهدأ فيه هوائياتك.");
        Add("Passport.Fit.Placeholder",
            "Bijv. verpleegkundige, juf, chauffeur…",
            "E.g. nurse, teacher, driver…",
            "Np. pielęgniarka, nauczycielka, kierowca…",
            "Ex. asistent medical, învățătoare, șofer…",
            "مثال: ممرض، معلمة، سائق…");
        Add("Passport.Fit.Check",
            "Check", "Check fit", "Sprawdź", "Verifică", "تحقق");
        Add("Passport.Fit.FullResult",
            "Hele uitslag ›", "Full result ›", "Pełny wynik ›", "Rezultat complet ›", "النتيجة الكاملة ›");
        Add("Passport.Fit.LastCheck",
            "Laatste check: {0}", "Last check: {0}", "Ostatni check: {0}", "Ultima verificare: {0}", "آخر فحص: {0}");
        Add("Passport.Fit.Similar",
            "Vergelijkbare functies ›", "Similar roles ›", "Podobne stanowiska ›", "Roluri similare ›", "وظائف مشابهة ›");
        Add("Passport.Fit.Result.Antennas",
            "Wat je antennes zeggen", "What your antennae say", "Co mówią czułki", "Ce spun antenele", "ماذا تقول هوائياتك");
        Add("Passport.Fit.Result.Claws",
            "Klauwen die je al hebt", "Claws you already have", "Szczypce, które już masz", "Cle pe care le ai deja", "مخالب لديك بالفعل");
        Add("Passport.Fit.Result.Growing",
            "Klauw die nog groeit", "Claw still growing", "Szczypce, które jeszcze rosną", "Clea care încă crește", "مخلب ما زال ينمو");
        Add("Passport.Fit.Result.Action",
            "Wat je kunt doen", "What you can do", "Co możesz zrobić", "Ce poți face", "ما يمكنك فعله");
        Add("Passport.Fit.Band.Good",
            "Past goed", "Fits well", "Pasuje dobrze", "Se potrivește bine", "يناسب جيداً");
        Add("Passport.Fit.Band.Fair",
            "Past redelijk", "Fits reasonably", "Pasuje w miarę", "Se potrivește rezonabil", "يناسب بشكل معقول");
        Add("Passport.Fit.Band.NotYet",
            "Past nog niet", "Doesn’t fit yet", "Jeszcze nie pasuje", "Încă nu se potrivește", "لا يناسب بعد");
        Add("Passport.Fit.CourseEyebrow",
            "Laat je klauw groeien", "Grow your claw", "Pozwól rosnąć szczypcom", "Lasă clea să crească", "دع مخلبك ينمو");
        Add("Passport.Fit.CultureTitle",
            "Cultuur: {0}", "Culture: {0}", "Kultura: {0}", "Cultură: {0}", "الثقافة: {0}");
        Add("Passport.Fit.CultureSub",
            "Hier komen jouw antennes tot rust",
            "This is where your antennae settle",
            "Tu twoje czułki odpoczywają",
            "Aici antenele tale se liniștesc",
            "هنا تهدأ هوائياتك");
        Add("Passport.Fit.DoCultureScan",
            "Doe de cultuurscan", "Take the culture scan", "Zrób skan kultury", "Fă scanarea de cultură", "أجرِ مسح الثقافة");
        Add("Passport.Fit.VacanciesTitle",
            "Vacatures die bij jou passen", "Vacancies that fit you", "Oferty dla ciebie", "Joburi care ți se potrivesc", "وظائف تناسبك");
        Add("Passport.Fit.VacanciesSub",
            "Niet de grootste steen, maar die bij jouw formaat past.",
            "Not the biggest stone — the one that fits your size.",
            "Nie największy kamień, lecz ten w twoim rozmiarze.",
            "Nu cea mai mare piatră, ci cea pe măsura ta.",
            "ليست أكبر صخرة، بل التي تناسب حجمك.");
        Add("Passport.Fit.Top10",
            "Top 10 ›", "See top 10 ›", "Zobacz top 10 ›", "Vezi top 10 ›", "أفضل 10 ›");
        Add("Passport.Fit.EmployersWant",
            "Werkgevers die je willen spreken",
            "Employers who want to talk",
            "Pracodawcy, którzy chcą rozmawiać",
            "Angajatori care vor să vorbească",
            "أصحاب عمل يريدون التحدث");
        Add("Passport.Fit.NewCount",
            "{0} nieuw", "{0} new", "{0} nowych", "{0} noi", "{0} جديد");
        Add("Passport.Fit.ViewContacts",
            "Bekijk", "View", "Zobacz", "Vezi", "عرض");

        Add("Passport.Career.DreamTitle",
            "Mijn droombaan", "My dream job", "Moja wymarzona praca", "Jobul meu de vis", "وظيفيّتي الحلم");
        Add("Passport.Career.OpenPlan",
            "Open mijn hele plan ›", "Open my full plan ›", "Otwórz cały plan ›", "Deschide tot planul ›", "افتح خطتي كاملة ›");
        Add("Passport.Career.ChangeDream",
            "Droombaan wijzigen", "Change dream job", "Zmień wymarzoną pracę", "Schimbă jobul de vis", "غيّر وظيفة الحلم");
        Add("Passport.Career.ShellNow",
            "Nu", "Now", "Teraz", "Acum", "الآن");
        Add("Passport.Career.ShellGrowing",
            "Groeit nu", "Growing now", "Rośnie teraz", "Crește acum", "ينمو الآن");
        Add("Passport.Career.ShellStep",
            "Stap {0}", "Step {0}", "Krok {0}", "Pasul {0}", "الخطوة {0}");
        Add("Passport.Career.GapsTitle",
            "Wat je nog mist", "What you still need", "Czego jeszcze brakuje", "Ce îți mai lipsește", "ما ما زال ينقصك");
        Add("Passport.Career.GapsSub",
            "Welke klauwen je al hebt, en welke je nog laat groeien.",
            "Which claws you already have, and which you still grow.",
            "Które szczypce już masz, a które jeszcze rosną.",
            "Ce cle ai deja și pe care le mai lași să crească.",
            "أي مخالب لديك، وأيها ما زالت تنمو.");
        Add("Passport.Career.AlreadyHave",
            "heb je al", "you already have", "już masz", "le ai deja", "لديك بالفعل");
        Add("Passport.Career.CourseTitle",
            "Opleiding die past", "Matching education", "Pasujące szkolenie", "Formare potrivită", "تدريب مناسب");
        Add("Passport.Career.CourseEmpty",
            "Nog geen gratis opleiding die past. Kijk op je hele plan.",
            "No matching free course yet. Check your full plan.",
            "Brak darmowego kursu. Zobacz cały plan.",
            "Niciun curs gratuit potrivit. Vezi tot planul.",
            "لا دورة مجانية مناسبة بعد. راجع خطتك الكاملة.");
        Add("Passport.Career.MatchTitle",
            "Match op deze stap", "Match on this step", "Dopasowanie na tym kroku", "Potrivire pe acest pas", "تطابق في هذه الخطوة");
        Add("Passport.Career.MatchEyebrow",
            "Groei eerst. Match daarna.",
            "Grow first. Match later.",
            "Najpierw rośnij. Potem match.",
            "Crește întâi. Potrivește după.",
            "انم أولاً. ثم طابق.");
        Add("Passport.Career.MatchStone",
            "Deze steen past al bij jouw formaat.",
            "This stone already fits your size.",
            "Ten kamień już pasuje do twojego rozmiaru.",
            "Această piatră e deja pe măsura ta.",
            "هذه الصخرة تناسب حجمك بالفعل.");
        Add("Passport.Career.MatchBand",
            "{0} bij {1}. Met de volgende stap worden je matches sterker.",
            "{0} for {1}. The next step makes your matches stronger.",
            "{0} do {1}. Kolejny krok wzmocni dopasowania.",
            "{0} cu {1}. Următorul pas îți întărește potrivirile.",
            "{0} مع {1}. الخطوة التالية تقوّي تطابقاتك.");
        Add("Passport.Career.VacanciesLink",
            "Vacatures voor deze stap ›", "Vacancies for this step ›", "Oferty na ten krok ›", "Joburi pentru acest pas ›", "وظائف لهذه الخطوة ›");
        Add("Passport.Career.EmptyTitle",
            "Kies je droombaan", "Choose your dream job", "Wybierz wymarzoną pracę", "Alege jobul de vis", "اختر وظيفة حلمك");
        Add("Passport.Career.EmptyLead",
            "Op Mijn carrière zet je je stip op de horizon. Dan groeit hier je pad.",
            "On My career you set your horizon. Your path will grow here.",
            "W Moja kariera ustawiasz horyzont. Tu urośnie twoja ścieżka.",
            "La Cariera mea îți setezi orizontul. Aici îți crește drumul.",
            "في مساري المهني تضع أفقك. هنا ينمو مسارك.");
        Add("Passport.Career.EmptyCta",
            "Naar Mijn carrière", "Go to My career", "Do Moja kariera", "La Cariera mea", "إلى مساري المهني");

        Add("Passport.Tests.Surface",
            "Oppervlakte", "Surface", "Powierzchnia", "Suprafață", "السطح");
        Add("Passport.Tests.Deep",
            "Diep", "Deep", "Głęboko", "Adânc", "عميق");
        Add("Passport.Tests.DepthAria",
            "Voortgang van quick-scan tot rapport", "Progress from quick scan to report", "Postęp od szybkiego skanu do raportu", "Progres de la scanare rapidă la raport", "التقدم من المسح السريع إلى التقرير");
        Add("Passport.Tests.Report",
            "Rapport", "Report", "Raport", "Raport", "تقرير");
        Add("Passport.Tests.Action.Start",
            "Start", "Start test", "Zacznij", "Începe", "ابدأ");
        Add("Passport.Tests.Action.Continue",
            "Ga verder", "Continue", "Kontynuuj", "Continuă", "تابع");
        Add("Passport.Tests.Action.Extended",
            "Uitgebreid", "Extended", "Rozszerzony", "Extins", "موسّع");
        Add("Passport.Tests.Action.Report",
            "Rapport", "Report", "Raport", "Raport", "تقرير");
        Add("Passport.Tests.Q.Competence",
            "Wat kun jij goed?", "What are you good at?", "W czym jesteś dobry?", "La ce ești bun?", "فيم أنت جيد؟");
        Add("Passport.Tests.Q.Career",
            "Welk werk past bij je?", "Which work fits you?", "Jaka praca do ciebie pasuje?", "Ce muncă ți se potrivește?", "أي عمل يناسبك؟");
        Add("Passport.Tests.Q.Culture",
            "Waar voel jij je thuis?", "Where do you feel at home?", "Gdzie czujesz się jak w domu?", "Unde te simți acasă?", "أين تشعر بالانتماء؟");
        Add("Passport.Tests.Q.Values",
            "Wat vind jij belangrijk?", "What matters to you?", "Co jest dla ciebie ważne?", "Ce contează pentru tine?", "ما الذي يهمك؟");
        Add("Passport.Tests.QuestionsLeft",
            "Nog {0} van {1} vragen", "{0} of {1} questions left", "Jeszcze {0} z {1} pytań", "Încă {0} din {1} întrebări", "بقي {0} من {1} أسئلة");
        Add("Passport.Tests.WhatCosts",
            "Wat kost uitgebreid?", "What does extended cost?", "Ile kosztuje rozszerzony?", "Cât costă cel extins?", "كم تكلفة الموسّع؟");
        Add("Passport.Tests.DownloadReports",
            "Download je rapporten", "Download your reports", "Pobierz raporty", "Descarcă rapoartele", "حمّل تقاريرك");

        Add("Passport.Course.GrowFurther",
            "Groei verder", "Grow further", "Rośnij dalej", "Crește mai departe", "نم أكثر");
        Add("Passport.Course.Free",
            "Gratis", "Free", "Bez opłat", "Gratuit", "مجاني");
        Add("Passport.Course.Partner",
            "Partnerlink", "Partner link", "Link partnerski", "Link partener", "رابط شريك");
        Add("Passport.Course.PartnerDisclosure",
            "Partnerlink: Lobsy kan een vergoeding krijgen.",
            "Partner link: Lobsy may receive a fee.",
            "Link partnerski: Lobsy może otrzymać wynagrodzenie.",
            "Link partener: Lobsy poate primi o remunerație.",
            "رابط شريك: قد تحصل لوبسي على عمولة.");
        Add("Passport.Course.FreeWhy",
            "Laat je klauw ‘{0}’ groeien",
            "Let your ‘{0}’ claw grow",
            "Pozwól rosnąć szczypcom ‘{0}’",
            "Lasă clea ‘{0}’ să crească",
            "دع مخلبك «{0}» ينمو");
        Add("Passport.Course.PartnerWhy",
            "Met certificaat voor je Bewijzen",
            "With a certificate for your Proof",
            "Z certyfikatem do Dowodów",
            "Cu certificat pentru Dovezi",
            "مع شهادة لإثباتاتك");
        Add("Passport.Course.Open",
            "Open cursus", "Open course", "Otwórz kurs", "Deschide cursul", "افتح الدورة");
        Add("Passport.Course.Type.Opleiding",
            "Opleiding", "Programme", "Kształcenie", "Formare", "تدريب");
        Add("Passport.Course.Type.Cursus",
            "Cursus", "Course", "Kurs", "Curs", "دورة");
        Add("Passport.Course.Type.Workshop",
            "Workshop", "Short workshop", "Warsztat", "Atelier", "ورشة");
        Add("Passport.Course.Delivery.Online",
            "online", "online only", "przez internet", "doar online", "عبر الإنترنت");
        Add("Passport.Course.Delivery.OnSite",
            "op locatie", "on site", "na miejscu", "la fața locului", "في الموقع");
        Add("Passport.Course.Delivery.Blended",
            "blended", "blended learning", "hybrydowo", "mixt", "مدمج");
        Add("Passport.Course.Duration.Hours",
            "{0} uur", "{0} hours", "{0} godz.", "{0} ore", "{0} ساعات");
        Add("Passport.Course.Duration.Days",
            "{0} dagen", "{0} days", "{0} dni", "{0} zile", "{0} أيام");
        Add("Passport.Course.Duration.Weeks",
            "{0} weken", "{0} weeks", "{0} tyg.", "{0} săptămâni", "{0} أسابيع");
        Add("Passport.Course.Duration.Months",
            "{0} maanden", "{0} months", "{0} mies.", "{0} luni", "{0} أشهر");
        Add("Passport.Course.Duration.Years",
            "{0} jaar", "{0} years", "{0} lat", "{0} ani", "{0} سنوات");

        Add("Admin.Training.ShowInPassport",
            "Toon in Mijn Paspoort", "Show in My Passport", "Pokaż w Paszporcie", "Arată în Pașaport", "أظهر في الجواز");
        Add("Admin.Training.IsFree",
            "Gratis", "Free", "Bez opłat", "Gratuit", "مجاني");
        Add("Admin.Training.IsPartner",
            "Partnerlink", "Partner link", "Link partnerski", "Link partener", "رابط شريك");
        Add("Admin.Training.AffiliateCode",
            "Affiliate-code", "Affiliate code", "Kod afiliacyjny", "Cod afiliat", "رمز الإحالة");
        Add("Admin.Training.Type",
            "Type", "Course type", "Typ", "Tip", "النوع");
        Add("Admin.Training.Duration",
            "Duur", "Duration", "Czas", "Durată", "المدة");
        Add("Admin.Training.Delivery",
            "Vorm", "Delivery", "Forma", "Livrare", "التقديم");
        Add("Admin.Training.Location",
            "Locatie", "Location", "Lokalizacja", "Locație", "الموقع");
        Add("Admin.Training.SaveOffer",
            "Opslaan opleiding", "Save course", "Zapisz kurs", "Salvează cursul", "احفظ الدورة");
        Add("Admin.Training.EditOffer",
            "Bewerk opleiding", "Edit course", "Edytuj kurs", "Editează cursul", "عدّل الدورة");

        // Profile consent (localized while extracting ProfileSections)
        Add("Profile.HoursMin",
            "Min.", "Min", "Od", "Min", "أدنى");
        Add("Profile.HoursMax",
            "Max.", "Max", "Do", "Max", "أقصى");
        Add("Profile.Consent.Title",
            "Privacy en toestemming", "Privacy and consent", "Prywatność i zgoda", "Confidențialitate și consimțământ", "الخصوصية والموافقة");
        Add("Profile.Consent.Lead",
            "Je kiest zelf of we je tests en AI-analyse gebruiken. De talentpool staat standaard uit.",
            "You choose whether we use your tests and AI analysis. The talent pool is off by default.",
            "Sam decydujesz, czy używamy Twoich testów i analizy AI. Pula talentów jest domyślnie wyłączona.",
            "Tu alegi dacă folosim testele și analiza AI. Pool-ul de talente e dezactivat implicit.",
            "أنت تختار إن كنا نستخدم اختباراتك وتحليل الذكاء الاصطناعي. مجموعة المواهب متوقفة افتراضياً.");
        Add("Profile.Consent.TestAiTitle",
            "Tests en AI-analyse", "Tests and AI analysis", "Testy i analiza AI", "Teste și analiză AI", "الاختبارات وتحليل الذكاء الاصطناعي");
        Add("Profile.Consent.TestAiNone",
            "Je hebt nog geen toestemming gegeven.",
            "You have not given consent yet.",
            "Nie wyraziłeś jeszcze zgody.",
            "Nu ai dat încă consimțământul.",
            "لم تمنح الموافقة بعد.");
        Add("Profile.Consent.TestAiSince",
            "Toestemming gegeven op {0}.",
            "Consent given on {0}.",
            "Zgoda udzielona {0}.",
            "Consimțământ acordat la {0}.",
            "تم منح الموافقة في {0}.");
        Add("Profile.Consent.Accept",
            "Toestemming geven", "Give consent", "Udziel zgody", "Acordă consimțământul", "امنح الموافقة");
        Add("Profile.Consent.Withdraw",
            "Toestemming intrekken", "Withdraw consent", "Cofnij zgodę", "Retrage consimțământul", "اسحب الموافقة");
        Add("Profile.Consent.WithdrawDelete",
            "Intrekken en testresultaten verwijderen",
            "Withdraw and delete test results",
            "Cofnij i usuń wyniki testów",
            "Retrage și șterge rezultatele testelor",
            "اسحب واحذف نتائج الاختبارات");
        Add("Profile.Consent.TalentTitle",
            "Anonieme talentpool", "Anonymous talent pool", "Anonimowa pula talentów", "Pool anonim de talente", "مجموعة مواهب مجهولة");
        Add("Profile.Consent.TalentOff",
            "Je profiel is niet zichtbaar in de talentpool.",
            "Your profile is not visible in the talent pool.",
            "Twój profil nie jest widoczny w puli talentów.",
            "Profilul tău nu e vizibil în pool-ul de talente.",
            "ملفك غير ظاهر في مجموعة المواهب.");
        Add("Profile.Consent.TalentOn",
            "Je profiel is anoniem zichtbaar in de talentpool.",
            "Your profile is anonymously visible in the talent pool.",
            "Twój profil jest anonimowo widoczny w puli talentów.",
            "Profilul tău e vizibil anonim în pool-ul de talente.",
            "ملفك ظاهر بشكل مجهول في مجموعة المواهب.");
        Add("Profile.Consent.TalentAccept",
            "Anoniem zichtbaar worden", "Become anonymously visible", "Uczyń widocznym anonimowo", "Devino vizibil anonim", "كن ظاهراً دون اسم");
        Add("Profile.Consent.TalentWithdraw",
            "Talentpooltoestemming intrekken",
            "Withdraw talent-pool consent",
            "Cofnij zgodę na pulę talentów",
            "Retrage consimțământul pentru pool",
            "اسحب موافقة مجموعة المواهب");
        Add("Profile.Consent.ParentalTitle",
            "Toestemming ouder of voogd",
            "Parent or guardian consent",
            "Zgoda rodzica lub opiekuna",
            "Consimțământ părinte sau tutore",
            "موافقة ولي الأمر");
        Add("Profile.Consent.ParentalLead",
            "Voor tests, AI en solliciteren hebben we eerst toestemming van je ouder of voogd nodig.",
            "For tests, AI and applying we first need consent from your parent or guardian.",
            "Do testów, AI i aplikacji potrzebujemy najpierw zgody rodzica lub opiekuna.",
            "Pentru teste, AI și aplicări avem nevoie mai întâi de consimțământul părintelui sau tutorelui.",
            "للاختبارات والذكاء الاصطناعي والتقديم نحتاج أولاً موافقة ولي أمرك.");
        Add("Profile.Consent.ParentalEmail",
            "E-mailadres van ouder of voogd",
            "Parent or guardian email",
            "E-mail rodzica lub opiekuna",
            "E-mail părinte sau tutore",
            "بريد ولي الأمر");
        Add("Profile.Consent.ParentalSend",
            "Bevestigingslink sturen", "Send confirmation link", "Wyślij link potwierdzający", "Trimite linkul de confirmare", "أرسل رابط التأكيد");
        Add("Profile.Consent.Toast.TestAccepted",
            "Toestemming voor tests en AI-analyse is opgeslagen.",
            "Consent for tests and AI analysis has been saved.",
            "Zgoda na testy i analizę AI została zapisana.",
            "Consimțământul pentru teste și analiza AI a fost salvat.",
            "تم حفظ موافقة الاختبارات وتحليل الذكاء الاصطناعي.");
        Add("Profile.Consent.Toast.TestWithdrawn",
            "Toestemming voor tests en AI-analyse is ingetrokken.",
            "Consent for tests and AI analysis has been withdrawn.",
            "Zgoda na testy i analizę AI została cofnięta.",
            "Consimțământul pentru teste și analiza AI a fost retras.",
            "تم سحب موافقة الاختبارات وتحليل الذكاء الاصطناعي.");
        Add("Profile.Consent.Toast.TestWithdrawnDeleted",
            "Toestemming ingetrokken en testresultaten verwijderd.",
            "Consent withdrawn and test results deleted.",
            "Zgoda cofnięta i wyniki testów usunięte.",
            "Consimțământ retras și rezultatele testelor șterse.",
            "سُحبت الموافقة وحُذفت نتائج الاختبارات.");
        Add("Profile.Consent.Toast.TalentAccepted",
            "Je profiel is nu anoniem zichtbaar in de talentpool.",
            "Your profile is now anonymously visible in the talent pool.",
            "Twój profil jest teraz anonimowo widoczny w puli talentów.",
            "Profilul tău e acum vizibil anonim în pool-ul de talente.",
            "ملفك ظاهر الآن بشكل مجهول في مجموعة المواهب.");
        Add("Profile.Consent.Toast.TalentWithdrawn",
            "Je profiel is direct uit de talentpool verwijderd.",
            "Your profile was removed from the talent pool immediately.",
            "Twój profil został od razu usunięty z puli talentów.",
            "Profilul tău a fost scos imediat din pool-ul de talente.",
            "أُزيل ملفك فوراً من مجموعة المواهب.");
        Add("Profile.Consent.Toast.ParentalSent",
            "De bevestigingslink is verstuurd.",
            "The confirmation link has been sent.",
            "Link potwierdzający został wysłany.",
            "Linkul de confirmare a fost trimis.",
            "تم إرسال رابط التأكيد.");

        // Bewijzen tab
        Add("Passport.Proof.ScaleTitle",
            "Je nieuwe schaal", "Your new shell", "Twoja nowa skorupa", "Cochilia ta nouă", "صدفتك الجديدة");
        Add("Passport.Proof.CountLabel",
            "{0} bewijzen · steeds steviger",
            "{0} proofs · getting stronger",
            "{0} dowodów · coraz mocniej",
            "{0} dovezi · tot mai solide",
            "{0} إثباتات · تزداد صلابة");
        Add("Passport.Proof.ScaleAria",
            "{0} van {1} bewijzen",
            "{0} of {1} proofs",
            "{0} z {1} dowodów",
            "{0} din {1} dovezi",
            "{0} من {1} إثباتات");
        Add("Passport.Proof.Soft",
            "Zacht", "Soft", "Miękka", "Moale", "طرية");
        Add("Passport.Proof.Hard",
            "Hard", "Firm", "Twarda", "Tare", "صلبة");
        Add("Passport.Proof.HintHard",
            "Je schaal is hard. Mooi zo.",
            "Your shell is hard. Well done.",
            "Twoja skorupa jest twarda. Super.",
            "Cochilia ta e tare. Bravo.",
            "صدفتك صلبة. أحسنت.");
        Add("Passport.Proof.HintOneReference",
            "Nog {0} recensie, dan is je schaal hard.",
            "{0} more review and your shell is hard.",
            "Jeszcze {0} recenzja, a skorupa będzie twarda.",
            "Încă {0} recenzie și cochilia e tare.",
            "مراجعة واحدة أخرى ({0}) وتصير صدفتك صلبة.");
        Add("Passport.Proof.HintOneCertificate",
            "Nog {0} certificaat, dan is je schaal hard.",
            "{0} more certificate and your shell is hard.",
            "Jeszcze {0} certyfikat, a skorupa będzie twarda.",
            "Încă {0} certificat și cochilia e tare.",
            "شهادة واحدة أخرى ({0}) وتصير صدفتك صلبة.");
        Add("Passport.Proof.HintOneEmployer",
            "Nog {0} werkgever, dan is je schaal hard.",
            "{0} more employer and your shell is hard.",
            "Jeszcze {0} pracodawca, a skorupa będzie twarda.",
            "Încă {0} angajator și cochilia e tare.",
            "صاحب عمل آخر ({0}) وتصير صدفتك صلبة.");
        Add("Passport.Proof.HintOneEducation",
            "Nog {0} opleiding, dan is je schaal hard.",
            "{0} more education and your shell is hard.",
            "Jeszcze {0} wykształcenie, a skorupa będzie twarda.",
            "Încă {0} studii și cochilia e tare.",
            "مؤهل آخر ({0}) وتصير صدفتك صلبة.");
        Add("Passport.Proof.HintOneCv",
            "Nog je eigen CV, dan is je schaal hard.",
            "Add your own CV and your shell is hard.",
            "Dodaj własne CV, a skorupa będzie twarda.",
            "Adaugă CV-ul tău și cochilia e tare.",
            "أضف سيرتك الذاتية وتصير صدفتك صلبة.");
        Add("Passport.Proof.HintTwo",
            "Nog {0} {1} en {2} {3}, dan is je schaal hard.",
            "{0} more {1} and {2} {3}, then your shell is hard.",
            "Jeszcze {0} {1} i {2} {3}, a skorupa będzie twarda.",
            "Încă {0} {1} și {2} {3}, și cochilia e tare.",
            "ما زال {0} {1} و{2} {3}، ثم تصير صدفتك صلبة.");
        Add("Passport.Proof.Noun.Reference",
            "recensie", "review", "recenzja", "recenzie", "مراجعة");
        Add("Passport.Proof.Noun.Certificate",
            "certificaat", "certificate", "certyfikat", "certificat", "شهادة");
        Add("Passport.Proof.Noun.Employer",
            "werkgever", "employer", "pracodawca", "angajator", "صاحب عمل");
        Add("Passport.Proof.Noun.Education",
            "opleiding", "education", "wykształcenie", "studii", "مؤهل");
        Add("Passport.Proof.Noun.Cv",
            "cv", "CV", "CV", "CV", "سيرة");
        Add("Passport.Proof.Experience",
            "Ervaring", "Experience", "Doświadczenie", "Experiență", "الخبرة");
        Add("Passport.Proof.ExperienceEmpty",
            "Nog geen werkgevers. Alles telt, ook mantelzorg.",
            "No employers yet. Everything counts, including care work.",
            "Brak pracodawców. Wszystko się liczy, także opieka.",
            "Niciun angajator încă. Totul contează, inclusiv îngrijirea.",
            "لا أصحاب عمل بعد. كل شيء يحسب، بما فيه الرعاية.");
        Add("Passport.Proof.AddEmployer",
            "+ Werkgever toevoegen", "+ Add employer", "+ Dodaj pracodawcę", "+ Adaugă angajator", "+ أضف صاحب عمل");
        Add("Passport.Proof.Education",
            "Opleiding & certificaten", "Education & certificates", "Wykształcenie i certyfikaty", "Studii și certificate", "التعليم والشهادات");
        Add("Passport.Proof.CertificatesEmpty",
            "Nog geen certificaten.", "No certificates yet.", "Brak certyfikatów.", "Niciun certificat încă.", "لا شهادات بعد.");
        Add("Passport.Proof.AddCertificate",
            "+ Certificaat toevoegen", "+ Add certificate", "+ Dodaj certyfikat", "+ Adaugă certificat", "+ أضف شهادة");
        Add("Passport.Proof.Reviews",
            "Recensies & CV", "Reviews & CV", "Recenzje i CV", "Recenzii și CV", "المراجعات والسيرة");
        Add("Passport.Proof.ReviewsEmpty",
            "Nog geen recensies.", "No reviews yet.", "Brak recenzji.", "Nicio recenzie încă.", "لا مراجعات بعد.");
        Add("Passport.Proof.AddReference",
            "+ Recensie toevoegen (max 3)",
            "+ Add review (max 3)",
            "+ Dodaj recenzję (maks. 3)",
            "+ Adaugă recenzie (max. 3)",
            "+ أضف مراجعة (حد أقصى 3)");
        Add("Passport.Proof.ReplaceCv",
            "↑ Vervangen", "↑ Replace", "↑ Zastąp", "↑ Înlocuiește", "↑ استبدال");
        Add("Passport.Proof.DownloadCv",
            "↓ Download", "↓ Download file", "↓ Pobierz", "↓ Descarcă", "↓ تنزيل");
        Add("Passport.Proof.LobsyCvNote",
            "Je Lobsy-CV (PDF) maken we automatisch van je bewijzen en je tests.",
            "We build your Lobsy CV (PDF) automatically from your proofs and tests.",
            "Twoje CV Lobsy (PDF) tworzymy automatycznie z dowodów i testów.",
            "CV-ul Lobsy (PDF) îl facem automat din dovezi și teste.",
            "ننشئ سيرة Lobsy (PDF) تلقائياً من إثباتاتك واختباراتك.");

        // Mijn gegevens tab
        Add("Passport.Data.OpenForWorkSub",
            "Werkgevers zien dat je zoekt",
            "Employers see that you are looking",
            "Pracodawcy widzą, że szukasz",
            "Angajatorii văd că cauți",
            "أصحاب العمل يرون أنك تبحث");
        Add("Passport.Data.TalentPool",
            "Anoniem in de talentpool",
            "Anonymous in the talent pool",
            "Anonimowo w puli talentów",
            "Anonim în pool-ul de talente",
            "مجهول في مجموعة المواهب");
        Add("Passport.Data.TalentPoolSub",
            "Staat standaard uit",
            "Off by default",
            "Domyślnie wyłączone",
            "Dezactivat implicit",
            "متوقف افتراضياً");
        Add("Passport.Data.ConsentOn",
            "Aan", "On", "Wł.", "Pornit", "تشغيل");
        Add("Passport.Data.ConsentOff",
            "Uit", "Off", "Wył.", "Oprit", "إيقاف");
        Add("Passport.Data.ConsentSince",
            "Toestemming sinds {0}",
            "Consent since {0}",
            "Zgoda od {0}",
            "Consimțământ din {0}",
            "موافقة منذ {0}");
        Add("Passport.Data.PersonalSub",
            "Naam, telefoon, WhatsApp, geboortedatum, adres, apparaten",
            "Name, phone, WhatsApp, date of birth, address, devices",
            "Imię, telefon, WhatsApp, data urodzenia, adres, urządzenia",
            "Nume, telefon, WhatsApp, data nașterii, adresă, dispozitive",
            "الاسم، الهاتف، واتساب، تاريخ الميلاد، العنوان، الأجهزة");
        Add("Passport.Data.PreferencesSub",
            "Reistijd, vervoer, interesses, rijbewijzen",
            "Travel time, transport, interests, licences",
            "Czas dojazdu, transport, zainteresowania, prawo jazdy",
            "Timp de deplasare, transport, interese, permis",
            "وقت التنقل، المواصلات، الاهتمامات، الرخص");
        Add("Passport.Data.AvailabilitySub",
            "Uren per week, snelkeuzes, dagdelen",
            "Hours per week, presets, day parts",
            "Godziny tygodniowo, szybkie wybory, pory dnia",
            "Ore pe săptămână, presetări, momente ale zilei",
            "ساعات أسبوعياً، اختيارات سريعة، فترات اليوم");
        Add("Passport.Data.Motivation",
            "Mijn motivatie", "My motivation", "Moja motywacja", "Motivația mea", "دافعي");
        Add("Passport.Data.MotivationSub",
            "Over jezelf, algemene motivatie",
            "About you, default motivation",
            "O sobie, ogólna motywacja",
            "Despre tine, motivație generală",
            "عن نفسك، الدافع العام");
        Add("Passport.Data.PrivacySub",
            "Tests en AI, talentpool, ouder/voogd",
            "Tests and AI, talent pool, parent/guardian",
            "Testy i AI, pula talentów, rodzic/opiekun",
            "Teste și AI, pool de talente, părinte/tutore",
            "اختبارات وذكاء اصطناعي، مجموعة مواهب، ولي الأمر");
        Add("Passport.Data.DeleteSub",
            "Je gegevens definitief wissen",
            "Permanently erase your data",
            "Trwale usuń swoje dane",
            "Șterge definitiv datele tale",
            "احذف بياناتك نهائياً");
    }
}
