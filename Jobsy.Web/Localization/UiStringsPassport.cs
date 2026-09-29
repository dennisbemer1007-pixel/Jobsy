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
    }
}
