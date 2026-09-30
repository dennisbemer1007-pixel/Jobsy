namespace Jobsy.Web.Localization;

/// <summary>Public shell + landing strings. Prefixes: PublicNav.*, PublicFooter.*, Public.*, Landing.*.</summary>
public static class UiStringsLanding
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string? plText = null, string? roText = null, string? arText = null)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText ?? enText;
            ro[key] = roText ?? enText;
            ar[key] = arText ?? enText;
        }

        // —— Nav ——
        Add("PublicNav.HowItWorks", "Hoe het werkt", "How it works", "Jak to działa", "Cum funcționează", "كيف يعمل");
        Add("PublicNav.JobMap", "Banenkaart", "Job map", "Mapa ofert", "Harta joburilor", "خريطة الوظائف");
        Add("PublicNav.Employers", "Werkgevers", "Employers", "Pracodawcy", "Angajatori", "أصحاب العمل");
        Add("PublicNav.Schools", "Scholen", "Schools", "Szkoły", "Școli", "المدارس");
        Add("PublicNav.Partners", "Partners", "Partner orgs", "Partnerzy", "Parteneri", "الشركاء");
        Add("PublicNav.MyPassport", "Mijn Paspoort", "My Passport", "Mój Paszport", "Pașaportul meu", "جواز سفري");
        Add("PublicNav.Discovery", "Ontdekkingsreis", "Discovery journey", "Podróż odkrywcza", "Călătoria de descoperire", "رحلة الاكتشاف");
        Add("PublicNav.Login", "Inloggen", "Log in", "Zaloguj się", "Autentificare", "تسجيل الدخول");
        Add("PublicNav.TakeFreeTest", "Doe de gratis test", "Take the free test", "Zrób darmowy test", "Fă testul gratuit", "أجرِ الاختبار المجاني");
        Add("PublicNav.TestChip", "Gratis test", "Free test", "Darmowy test", "Test gratuit", "اختبار مجاني");
        Add("PublicNav.CreateAccount", "Account maken", "Create account", "Utwórz konto", "Creează cont", "إنشاء حساب");
        Add("PublicNav.Menu", "Menu", "Open menu", "Otwórz menu", "Meniu", "القائمة");
        Add("PublicNav.Close", "Sluiten", "Close", "Zamknij", "Închide", "إغلاق");
        Add("PublicNav.SkipToContent", "Naar de inhoud", "Skip to content", "Przejdź do treści", "Sari la conținut", "انتقل إلى المحتوى");
        Add("PublicNav.ChooseLanguage", "Kies je taal", "Choose your language", "Wybierz język", "Alege limba", "اختر لغتك");
        Add("PublicNav.QuickLinks", "Snelle links", "Quick links", "Szybkie linki", "Linkuri rapide", "روابط سريعة");
        Add("PublicNav.MainNav", "Hoofdnavigatie", "Main navigation", "Główna nawigacja", "Navigare principală", "التنقل الرئيسي");

        // —— Footer ——
        Add("PublicFooter.BrandLine",
            "Lobsy laat zien wie jij bent en welk werk bij je past.",
            "Lobsy shows who you are and which work fits you.",
            "Lobsy pokazuje, kim jesteś i jaka praca do Ciebie pasuje.",
            "Lobsy arată cine ești și ce muncă ți se potrivește.",
            "لوبسي يُظهر من أنت وأي عمل يناسبك.");
        Add("PublicFooter.BrandLine.Zw",
            "Lobsy laat zien wie jij bent en welke richting bij je past.",
            "Lobsy shows who you are and which direction fits you.",
            "Lobsy pokazuje, kim jesteś i jaki kierunek do Ciebie pasuje.",
            "Lobsy arată cine ești și ce direcție ți se potrivește.",
            "لوبسي يُظهر من أنت وأي اتجاه يناسبك.");
        Add("PublicFooter.Brand", "Lobsy", "Lobsy", "Lobsy", "Lobsy", "Lobsy");
        Add("PublicFooter.For", "Voor", "For", "Dla", "Pentru", "من أجل");
        Add("PublicFooter.Account", "Account", "Your account", "Konto", "Cont", "الحساب");
        Add("PublicFooter.Legal", "Juridisch", "Legal", "Prawne", "Juridic", "قانوني");
        Add("PublicFooter.Candidates", "Kandidaten", "Candidates", "Kandydaci", "Candidați", "المرشحون");
        Add("PublicFooter.You", "Jou", "You", "Ciebie", "Tine", "أنت");
        Add("PublicFooter.NewInNetherlands", "Nieuw in Nederland", "New in the Netherlands", "Nowy w Holandii", "Nou în Țările de Jos", "جديد في هولندا");
        Add("PublicFooter.CompanyRegister", "Werkgever registreren", "Register as employer", "Zarejestruj pracodawcę", "Înregistrează angajator", "تسجيل صاحب عمل");
        Add("PublicFooter.Privacy", "Privacy", "Privacy policy", "Prywatność", "Confidențialitate", "الخصوصية");
        Add("PublicFooter.Cookies", "Cookies", "Cookie settings", "Pliki cookie", "Cookie-uri", "ملفات تعريف الارتباط");
        Add("PublicFooter.Terms", "Algemene voorwaarden", "Terms and conditions", "Regulamin", "Termeni și condiții", "الشروط العامة");
        Add("PublicFooter.UsageTerms", "Gebruiksvoorwaarden", "Terms of use", "Warunki użytkowania", "Condiții de utilizare", "شروط الاستخدام");
        Add("PublicFooter.About", "Wie zijn wij", "About us", "O nas", "Despre noi", "من نحن");
        Add("PublicFooter.MadeWith", "gemaakt met", "made with", "stworzone z", "realizat cu", "صُنع بـ");
        Add("PublicFooter.Copyright",
            "© {0} Lobsy · {1} 🧡",
            "© {0} Lobsy · {1} with 🧡",
            "© {0} Lobsy · {1} z 🧡",
            "© {0} Lobsy · {1} cu 🧡",
            "© {0} Lobsy · {1} مع 🧡");

        // —— Generic public ——
        Add("Public.Sample", "Voorbeeld", "Sample", "Przykład", "Exemplu", "مثال");
        Add("Public.SampleData", "Voorbeelddata", "Sample data", "Dane przykładowe", "Date exemplu", "بيانات مثال");
        Add("Public.Soon", "Binnenkort", "Coming soon", "Wkrótce", "În curând", "قريبًا");

        // —— Landing SEO ——
        Add("Landing.Seo.Title",
            "Lobsy — ontdek wie jij bent en welk werk bij je past",
            "Lobsy — discover who you are and which work fits you",
            "Lobsy — odkryj kim jesteś i jaka praca do Ciebie pasuje",
            "Lobsy — descoperă cine ești și ce muncă ți se potrivește",
            "لوبسي — اكتشف من أنت وأي عمل يناسبك");
        Add("Landing.Seo.Description",
            "Gratis test van 20 vragen. Banenkaart op reistijd. Jij kiest wat een werkgever ziet.",
            "Free 20-question test. Job map by travel time. You choose what an employer sees.",
            "Darmowy test 20 pytań. Mapa ofert według czasu dojazdu. Ty decydujesz, co widzi pracodawca.",
            "Test gratuit cu 20 de întrebări. Hartă de joburi după timpul de drum. Tu alegi ce vede angajatorul.",
            "اختبار مجاني من 20 سؤالًا. خريطة وظائف حسب وقت الرحلة. أنت تختار ما يراه صاحب العمل.");

        // —— Hero ——
        Add("Landing.Hero.Eyebrow", "👋 Hoi! Gratis en zonder account", "👋 Hi! Free, no account needed", "👋 Cześć! Za darmo, bez konta", "👋 Salut! Gratuit, fără cont", "👋 مرحبًا! مجاني وبدون حساب");
        Add("Landing.Hero.Title", "Soms moet je uit je schild {0}.", "Sometimes you need to grow out of your shell {0}.", "Czasem musisz wyjść ze skorupy {0}.", "Uneori trebuie să ieși din cochilie {0}.", "أحيانًا تحتاج أن تخرج من صدفتك {0}.");
        Add("Landing.Hero.TitleAccent", "groeien", "grow", "rosnąć", "să crești", "تنمو");
        Add("Landing.Hero.Sub",
            "Lobsy laat zien wie jij bent, wat je kunt en welk werk bij je past. Eerst jij, dan de baan. Doe de korte test en zie: dit ben jij.",
            "Lobsy shows who you are, what you can do and which work fits you. You first, then the job. Take the short test and see: this is you.",
            "Lobsy pokazuje, kim jesteś, co potrafisz i jaka praca do Ciebie pasuje. Najpierw Ty, potem praca. Zrób krótki test i zobacz: to Ty.",
            "Lobsy arată cine ești, ce poți și ce muncă ți se potrivește. Mai întâi tu, apoi jobul. Fă testul scurt și vezi: acesta ești tu.",
            "لوبسي يُظهر من أنت وما تستطيع وأي عمل يناسبك. أنت أولًا، ثم الوظيفة. خُض الاختبار القصير وانظر: هذا أنت.");
        Add("Landing.Hero.CtaTest", "Doe de gratis test →", "Take the free test →", "Zrób darmowy test →", "Fă testul gratuit →", "أجرِ الاختبار المجاني ←");
        Add("Landing.Hero.CtaLogin", "Inloggen", "Log in", "Zaloguj się", "Autentificare", "تسجيل الدخول");
        Add("Landing.Hero.MetaTime", "⏱️ 20 vragen · 3 minuutjes", "⏱️ 20 questions · 3 minutes", "⏱️ 20 pytań · 3 minuty", "⏱️ 20 întrebări · 3 minute", "⏱️ 20 سؤالًا · 3 دقائق");
        Add("Landing.Hero.MetaPrivacy", "🔒 Werkgevers zien je antwoorden niet", "🔒 Employers never see your answers", "🔒 Pracodawcy nie widzą Twoich odpowiedzi", "🔒 Angajatorii nu îți văd răspunsurile", "🔒 أصحاب العمل لا يرون إجاباتك");
        Add("Landing.Hero.MetaMap", "🗺️ Of kijk eerst op de banenkaart", "🗺️ Or browse the job map first", "🗺️ Albo najpierw zobacz mapę ofert", "🗺️ Sau uită-te mai întâi pe hartă", "🗺️ أو اطّلع أولًا على خريطة الوظائف");
        Add("Landing.Hero.MetaMapCount", "🗺️ Nu ruim {0} vacatures op de banenkaart", "🗺️ About {0} jobs on the map right now", "🗺️ Około {0} ofert na mapie", "🗺️ Aproximativ {0} joburi pe hartă", "🗺️ نحو {0} وظيفة على الخريطة الآن");
        Add("Landing.Hero.ForYou", "Lobsy is er voor jou als je…", "Lobsy is for you when you…", "Lobsy jest dla Ciebie, gdy…", "Lobsy e pentru tine când…", "لوبسي لك عندما…");
        Add("Landing.Hero.ChipWork", "werk zoekt", "are looking for work", "szukasz pracy", "cauți de muncă", "تبحث عن عمل");
        Add("Landing.Hero.ChipNew", "net in Nederland bent", "just arrived in the Netherlands", "właśnie jesteś w Holandii", "ești nou în Țările de Jos", "وصلت حديثًا إلى هولندا");
        Add("Landing.Hero.ChipSchool", "op school zit", "are still in school", "jesteś w szkole", "ești la școală", "ما زلت في المدرسة");
        Add("Landing.Hero.ChipStuck", "even vastzit", "feel stuck", "utknąłeś", "te-ai blocat", "تشعر أنك عالق");
        Add("Landing.Hero.Bubble", "Hoi! Zin om te groeien? 👋", "Hi! Ready to grow? 👋", "Cześć! Chcesz rosnąć? 👋", "Salut! Gata să crești? 👋", "مرحبًا! مستعد لتنمو؟ 👋");
        Add("Landing.Hero.OldShell", "je oude schild", "your old shell", "Twoja stara skorupa", "cochilia ta veche", "صدفتك القديمة");
        Add("Landing.Hero.ThisIsYou", "🦞 Dit ben jij", "🦞 This is you", "🦞 To Ty", "🦞 Acesta ești tu", "🦞 هذا أنت");
        Add("Landing.Hero.JobBike", "Zorgmedewerker", "Care worker", "Opiekun", "Asistent medical", "عامل رعاية");
        Add("Landing.Hero.JobBikeMeta", "12 min fietsen · Naaldwijk", "12 min by bike · Naaldwijk", "12 min rowerem · Naaldwijk", "12 min cu bicicleta · Naaldwijk", "١٢ د بالدراجة · Naaldwijk");
        Add("Landing.Hero.JobBus", "Monteur", "Technician", "Monter", "Tehnician", "فني");
        Add("Landing.Hero.JobBusMeta", "24 min met de bus · Delft", "24 min by bus · Delft", "24 min autobusem · Delft", "24 min cu autobuzul · Delft", "٢٤ د بالحافلة · Delft");
        Add("Landing.Hero.BarTeam", "Samenwerken", "Teamwork", "Współpraca", "Colaborare", "العمل الجماعي");
        Add("Landing.Hero.BarPersist", "Doorzetten", "Persistence", "Wytrwałość", "Persistență", "المثابرة");
        Add("Landing.Hero.BarCalm", "Rust onder druk", "Calm under pressure", "Spokój pod presją", "Calm sub presiune", "الهدوء تحت الضغط");

        // —— Wat is Lobsy ——
        Add("Landing.WhatIs.Eyebrow", "Wat is Lobsy?", "What is Lobsy?", "Czym jest Lobsy?", "Ce este Lobsy?", "ما هو لوبسي؟");
        Add("Landing.WhatIs.Title", "Geen cv-site. Lobsy kijkt eerst naar jóu.", "Not a CV site. Lobsy looks at you first.", "To nie portal CV. Lobsy patrzy najpierw na Ciebie.", "Nu e un site de CV. Lobsy se uită mai întâi la tine.", "ليست موقع سيرة. لوبسي ينظر إليك أولًا.");
        Add("Landing.WhatIs.Lead",
            "Weet je niet goed wat je kunt of wilt? Telt je diploma hier niet? Dat is heel normaal. Lobsy helpt je ontdekken wie je bent, en wat bij je past.",
            "Not sure what you can or want? Does your diploma not count here? That is normal. Lobsy helps you discover who you are and what fits.",
            "Nie wiesz, co potrafisz lub chcesz? Dyplom tu nie liczy? To normalne. Lobsy pomaga odkryć, kim jesteś i co do Ciebie pasuje.",
            "Nu știi ce poți sau ce vrei? Diploma nu contează aici? E normal. Lobsy te ajută să descoperi cine ești și ce ți se potrivește.",
            "لا تعرف ماذا تستطيع أو تريد؟ شهادتك لا تُحسب هنا؟ هذا طبيعي. لوبسي يساعدك على اكتشاف من أنت وما يناسبك.");
        Add("Landing.WhatIs.Step", "Stap {0}", "Step {0}", "Krok {0}", "Pasul {0}", "الخطوة {0}");
        Add("Landing.WhatIs.S1Title", "Ontdek jezelf", "Discover yourself", "Odkryj siebie", "Descoperă-te", "اكتشف نفسك");
        Add("Landing.WhatIs.S1Body", "Korte, leuke vragen over hoe je werkt, wat je leuk vindt en wat je belangrijk vindt. Er is geen goed of fout.", "Short, fun questions about how you work, what you like and what matters to you. There is no right or wrong.", "Krótkie pytania o to, jak pracujesz, co lubisz i co jest ważne. Nie ma dobrych i złych odpowiedzi.", "Întrebări scurte despre cum lucrezi, ce-ți place și ce contează. Nu există corect sau greșit.", "أسئلة قصيرة ممتعة عن كيف تعمل وما تحب وما يهمك. لا صح ولا خطأ.");
        Add("Landing.WhatIs.S1Tag1", "Werk-DNA", "Work DNA", "DNA pracy", "ADN de muncă", "حمض العمل");
        Add("Landing.WhatIs.S1Tag2", "Gratis", "Free", "Za darmo", "Gratuit", "مجاني");
        Add("Landing.WhatIs.S2Title", "Zie wat bij je past", "See what fits you", "Zobacz, co do Ciebie pasuje", "Vezi ce ți se potrivește", "اعرف ما يناسبك");
        Add("Landing.WhatIs.S2Body", "Banen bij jou in de buurt op de kaart. Met je reistijd op de fiets, met de bus of met de auto.", "Jobs near you on the map. With travel time by bike, bus or car.", "Oferty w Twojej okolicy na mapie. Z czasem dojazdu rowerem, autobusem lub autem.", "Joburi aproape de tine pe hartă. Cu timpul de drum pe bicicletă, autobuz sau mașină.", "وظائف قربك على الخريطة. مع وقت الرحلة بالدراجة أو الحافلة أو السيارة.");
        Add("Landing.WhatIs.S2Tag1", "Banenkaart", "Job map", "Mapa ofert", "Hartă joburi", "خريطة الوظائف");
        Add("Landing.WhatIs.S2Tag2", "Reistijd", "Travel time", "Czas dojazdu", "Timp de drum", "وقت الرحلة");
        Add("Landing.WhatIs.S3Title", "Laat zien wat je kunt", "Show what you can do", "Pokaż, co potrafisz", "Arată ce poți", "أظهر ما تستطيع");
        Add("Landing.WhatIs.S3Body", "Je eigen paspoort met je DNA, je tests en je bewijzen. Jij kiest wat een werkgever ziet.", "Your own passport with your DNA, tests and proofs. You choose what an employer sees.", "Twój paszport z DNA, testami i dowodami. Ty decydujesz, co widzi pracodawca.", "Pașaportul tău cu ADN, teste și dovezi. Tu alegi ce vede angajatorul.", "جوازك مع الحمض والاختبارات والإثباتات. أنت تختار ما يراه صاحب العمل.");
        Add("Landing.WhatIs.S3Tag1", "Paspoort", "Passport", "Paszport", "Pașaport", "جواز");
        Add("Landing.WhatIs.S3Tag2", "Jij beslist", "You decide", "Ty decydujesz", "Tu decizi", "أنت تقرر");

        // —— Kreeft ——
        Add("Landing.Kreeft.Eyebrow", "De kreeft-visie", "The lobster vision", "Wizja homara", "Viziunea homarului", "رؤية الكركند");
        Add("Landing.Kreeft.Title", "Jij bent de kreeft.", "You are the lobster.", "Jesteś homarem.", "Tu ești homarul.", "أنت الكركند.");
        Add("Landing.Kreeft.Lead", "Een kreeft stopt nooit met groeien. Maar dat kan alleen als hij zijn oude schild durft los te laten. Net als jij.", "A lobster never stops growing. But only if it dares to leave its old shell. Just like you.", "Homar nigdy nie przestaje rosnąć. Ale tylko gdy odważy się zostawić starą skorupę. Tak jak Ty.", "Un homar nu se oprește din creștere. Doar dacă lasă cochilia veche. Ca tine.", "الكركند لا يتوقف عن النمو. لكن فقط إن ترك صدفته القديمة. مثلك.");
        Add("Landing.Kreeft.StoryTitle", "Soms moet je uit je schild groeien.", "Sometimes you need to grow out of your shell.", "Czasem musisz wyjść ze skorupy.", "Uneori trebuie să ieși din cochilie.", "أحيانًا تحتاج أن تخرج من صدفتك.");
        Add("Landing.Kreeft.Story1", "Wordt je schild te krap? Dan laat een kreeft het los. Even is hij zacht en kwetsbaar. Hij zoekt een veilige rots. En daarna is hij sterker dan ooit.", "Shell too tight? A lobster lets it go. For a moment it is soft and vulnerable. It finds a safe rock. Then it is stronger than ever.", "Skorupa za ciasna? Homar ją zostawia. Przez chwilę jest miękki i wrażliwy. Szuka bezpiecznej skały. Potem jest silniejszy niż kiedykolwiek.", "Cochilia prea strâmtă? Homarul o lasă. O clipă e moale și vulnerabil. Caută o stâncă sigură. Apoi e mai puternic ca niciodată.", "صدفتك ضيقة؟ الكركند يتركها. للحظة يكون لينًا وضعيفًا. يبحث عن صخرة آمنة. ثم يصبح أقوى من قبل.");
        Add("Landing.Kreeft.Story2", "Zo gaat het met werk ook. Een nieuw land, een nieuwe school, of gewoon het gevoel dat je vastzit. Lobsy is je veilige rots.", "Work is the same. A new country, a new school, or just feeling stuck. Lobsy is your safe rock.", "Z pracą jest tak samo. Nowy kraj, nowa szkoła albo poczucie, że utknąłeś. Lobsy to Twoja bezpieczna skała.", "La fel e și cu munca. O țară nouă, o școală nouă sau doar senzația că te-ai blocat. Lobsy e stânca ta sigură.", "العمل كذلك. بلد جديد أو مدرسة جديدة أو شعور بأنك عالق. لوبسي هو صخرتك الآمنة.");
        Add("Landing.Kreeft.Bubble", "Te krap? Tijd om te groeien! 🌱", "Too tight? Time to grow! 🌱", "Za ciasno? Czas rosnąć! 🌱", "Prea strâmt? E timpul să crești! 🌱", "ضيق؟ حان وقت النمو! 🌱");
        Add("Landing.Kreeft.InLobsy", "In Lobsy: {0}", "Inside Lobsy: {0}", "W Lobsy: {0}", "În Lobsy: {0}", "في لوبسي: {0}");
        Add("Landing.Kreeft.T1Title", "Diep duiken", "Dive deep", "Nurkuj głęboko", "Scufundă-te", "اغص عميقًا");
        Add("Landing.Kreeft.T1Body", "Je doet tests die verder gaan dan je cv. Zo zie je wat er echt in je zit.", "You take tests that go beyond your CV. That shows what is really in you.", "Robisz testy głębsze niż CV. Tak widzisz, co naprawdę w Tobie jest.", "Faci teste dincolo de CV. Așa vezi ce e cu adevărat în tine.", "تخوض اختبارات أعمق من سيرتك. هكذا ترى ما فيك حقًا.");
        Add("Landing.Kreeft.T1Link", "Mijn tests", "My tests", "Moje testy", "Testele mele", "اختباراتي");
        Add("Landing.Kreeft.T2Title", "Antennes", "Antennae", "Czułki", "Antene", "قرون الاستشعار");
        Add("Landing.Kreeft.T2Body", "Je voelt snel of een team of plek bij je past. Lobsy maakt dat gevoel zichtbaar.", "You quickly sense if a team or place fits. Lobsy makes that feeling visible.", "Szybko czujesz, czy zespół lub miejsce pasuje. Lobsy to pokazuje.", "Simți rapid dacă o echipă sau un loc ți se potrivește. Lobsy face asta vizibil.", "تشعر بسرعة إن كان الفريق أو المكان يناسبك. لوبسي يُظهر ذلك.");
        Add("Landing.Kreeft.T2Link", "Hier voel je je thuis", "Where you feel at home", "Tu czujesz się jak w domu", "Aici te simți acasă", "هنا تشعر أنك في بيتك");
        Add("Landing.Kreeft.T3Title", "De juiste rots", "The right rock", "Właściwa skała", "Stânca potrivită", "الصخرة المناسبة");
        Add("Landing.Kreeft.T3Body", "Een kreeft zoekt een plek waar hij veilig kan groeien. Jij ook.", "A lobster looks for a place where it can grow safely. So do you.", "Homar szuka miejsca, gdzie może bezpiecznie rosnąć. Ty też.", "Homarul caută un loc unde poate crește în siguranță. Și tu.", "الكركند يبحث عن مكان ينمو فيه بأمان. وأنت أيضًا.");
        Add("Landing.Kreeft.T3Link", "Banenkaart", "Job map", "Mapa ofert", "Hartă joburi", "خريطة الوظائف");
        Add("Landing.Kreeft.T4Title", "Je scharen", "Your claws", "Twoje szczypce", "Cleștii tăi", "مقابضك");
        Add("Landing.Kreeft.T4Body", "Je sterke punten. Met bewijzen erbij: diploma's, certificaten, ervaring.", "Your strengths. With proof: diplomas, certificates, experience.", "Twoje mocne strony. Z dowodami: dyplomy, certyfikaty, doświadczenie.", "Punctele tale forte. Cu dovezi: diplome, certificate, experiență.", "نقاط قوتك. مع إثباتات: شهادات وخبرات.");
        Add("Landing.Kreeft.T4Link", "Bewijzen", "Proofs", "Dowody", "Dovezi", "إثباتات");
        Add("Landing.Kreeft.T5Title", "Van zacht naar sterk", "From soft to strong", "Od miękkiego do silnego", "De la moale la puternic", "من الليونة إلى القوة");
        Add("Landing.Kreeft.T5Body", "Na elke groeistap word je sterker. Lobsy laat zien welke stap past.", "After every growth step you get stronger. Lobsy shows which step fits.", "Po każdym kroku jesteś silniejszy. Lobsy pokazuje, który krok pasuje.", "După fiecare pas de creștere ești mai puternic. Lobsy arată ce pas se potrivește.", "بعد كل خطوة نمو تصبح أقوى. لوبسي يُظهر الخطوة المناسبة.");
        Add("Landing.Kreeft.T5Link", "Carrière", "Career", "Kariera", "Carieră", "المسار المهني");
        Add("Landing.Kreeft.T6Title", "Je eigen weg", "Your own path", "Własna droga", "Drumul tău", "طريقك أنت");
        Add("Landing.Kreeft.T6Body", "Een kreeft loopt niet in een rij. Jij kiest je eigen richting.", "A lobster does not walk in a line. You choose your own direction.", "Homar nie idzie w szeregu. Ty wybierasz swój kierunek.", "Homarul nu merge în rând. Tu alegi direcția.", "الكركند لا يسير في صف. أنت تختار اتجاهك.");
        Add("Landing.Kreeft.T6Link", "Ontdekkingsreis", "Discovery journey", "Podróż odkrywcza", "Călătorie de descoperire", "رحلة الاكتشاف");

        MergeWhatYouGet(Add);
        MergeForWhom(Add);
        MergePrivacy(Add);
        MergeFaq(Add);
        MergeClosing(Add);
    }

    private static void MergeWhatYouGet(Action<string, string, string, string?, string?, string?> Add)
    {
        Add("Landing.Get.Eyebrow", "Wat je krijgt als je inlogt", "What you get when you sign in", "Co dostajesz po zalogowaniu", "Ce primești când te autentifici", "ما تحصل عليه عند تسجيل الدخول");
        Add("Landing.Get.Title", "Van “wie ben ik?” naar “hier wil ik werken”.", "From “who am I?” to “I want to work here”.", "Od „kim jestem?” do „tu chcę pracować”.", "De la „cine sunt?” la „aici vreau să lucrez”.", "من «من أنا؟» إلى «أريد العمل هنا».");
        Add("Landing.Get.Note", "Alle voorbeelden: Voorbeelddata", "All examples: sample data", "Wszystkie przykłady: dane przykładowe", "Toate exemplele: date exemplu", "كل الأمثلة: بيانات مثال");
        Add("Landing.Get.MapTitle", "🗺️ De banenkaart", "🗺️ The job map", "🗺️ Mapa ofert", "🗺️ Harta joburilor", "🗺️ خريطة الوظائف");
        Add("Landing.Get.MapLead", "Zie meteen welke banen je echt kunt halen.", "See right away which jobs you can actually reach.", "Od razu zobacz, które oferty realnie dojdziesz.", "Vezi imediat ce joburi poți ajunge cu adevărat.", "اعرف فورًا أي وظائف تستطيع الوصول إليها.");
        Add("Landing.Get.MapBike", "Fiets", "Bike", "Rower", "Bicicletă", "دراجة");
        Add("Landing.Get.MapOv", "OV", "Transit", "Komunikacja", "Transport", "مواصلات");
        Add("Landing.Get.MapCar", "Auto", "Car", "Samochód", "Mașină", "سيارة");
        Add("Landing.Get.MapWithin", "Binnen 30 minuten van huis: 38 banen", "Within 30 minutes from home: 38 jobs", "W 30 minut od domu: 38 ofert", "În 30 de minute de acasă: 38 joburi", "خلال ٣٠ دقيقة من المنزل: ٣٨ وظيفة");
        Add("Landing.Get.MapJob1", "Zorgmedewerker thuiszorg", "Home care worker", "Opiekun opieki domowej", "Asistent îngrijire la domiciliu", "عامل رعاية منزلية");
        Add("Landing.Get.MapJob1Meta", "Kasteelhof Zorg · Naaldwijk", "Kasteelhof Care · Naaldwijk", "Kasteelhof Opieka · Naaldwijk", "Kasteelhof Îngrijire · Naaldwijk", "Kasteelhof رعاية · Naaldwijk");
        Add("Landing.Get.MapJob2", "Monteur installatietechniek", "Installation technician", "Monter instalacji", "Tehnician instalații", "فني تركيبات");
        Add("Landing.Get.MapJob2Meta", "Van Dijk Installatie · Delft", "Van Dijk Installations · Delft", "Van Dijk Instalacje · Delft", "Van Dijk Instalații · Delft", "Van Dijk تركيب · Delft");
        Add("Landing.Get.MapJob3", "Teeltmedewerker kas", "Greenhouse grower", "Pracownik szklarni", "Lucrător în seră", "عامل دفيئة");
        Add("Landing.Get.MapJob3Meta", "Groene Kas · Westland", "Green House · Westland", "Zielona Szklarnia · Westland", "Sera Verde · Westland", "بيت أخضر · Westland");
        Add("Landing.Get.MapFit", "Past goed", "Good fit", "Dobrze pasuje", "Se potrivește bine", "يناسب جيدًا");
        Add("Landing.Get.MapFitOk", "Past redelijk", "Okay fit", "Dość pasuje", "Se potrivește ok", "يناسب إلى حد ما");
        Add("Landing.Get.MapTravel", "fietsen", "by bike", "rowerem", "cu bicicleta", "بالدراجة");
        Add("Landing.Get.MapRings", "Reistijd-cirkels", "Travel-time rings", "Pierścienie czasu", "Inele de timp", "حلقات وقت الرحلة");
        Add("Landing.Get.PassportTitle", "🪪 Mijn Paspoort", "🪪 My Passport", "🪪 Mój Paszport", "🪪 Pașaportul meu", "🪪 جوازي");
        Add("Landing.Get.PassportBody", "Alles over jou op één plek. Jij kiest wat je deelt.", "Everything about you in one place. You choose what you share.", "Wszystko o Tobie w jednym miejscu. Ty decydujesz, co udostępniasz.", "Totul despre tine într-un loc. Tu alegi ce împărtășești.", "كل شيء عنك في مكان واحد. أنت تختار ما تشاركه.");
        Add("Landing.Get.Passport.TabDna", "Mijn DNA", "My DNA", "Moje DNA", "ADN-ul meu", "حمضي");
        Add("Landing.Get.Passport.TabTests", "Mijn tests", "My tests", "Moje testy", "Testele mele", "اختباراتي");
        Add("Landing.Get.Passport.TabFit", "Past deze baan?", "Does this job fit?", "Czy ta praca pasuje?", "Se potrivește jobul?", "هل تناسب هذه الوظيفة؟");
        Add("Landing.Get.Passport.TabCareer", "Carrière", "Career", "Kariera", "Carieră", "المسار");
        Add("Landing.Get.Passport.TabProofs", "Bewijzen", "Proofs", "Dowody", "Dovezi", "إثباتات");
        Add("Landing.Get.Passport.BarSocial", "Sociaal", "Social", "Społeczny", "Social", "اجتماعي");
        Add("Landing.Get.Passport.BarPractical", "Praktisch", "Practical", "Praktyczny", "Practic", "عملي");
        Add("Landing.Get.Passport.BarCareful", "Zorgvuldig", "Careful", "Dokładny", "Atent", "دقيق");
        Add("Landing.Get.JourneyTitle", "🧭 Ontdekkingsreis", "🧭 Discovery journey", "🧭 Podróż odkrywcza", "🧭 Călătoria de descoperire", "🧭 رحلة الاكتشاف");
        Add("Landing.Get.JourneyBody", "Stap voor stap ontdekken wie je bent. Pauzeren mag altijd.", "Discover who you are step by step. Pausing is always ok.", "Krok po kroku odkrywaj, kim jesteś. Pauza zawsze wolna.", "Descoperă cine ești pas cu pas. Poți pauza oricând.", "اكتشف من أنت خطوة بخطوة. التوقف دائمًا مسموح.");
        Add("Landing.Get.Journey1", "Zo werk jij", "How you work", "Tak pracujesz", "Așa lucrezi", "هكذا تعمل");
        Add("Landing.Get.Journey2", "Dit vind je leuk", "What you like", "To lubisz", "Asta îți place", "هذا ما تحب");
        Add("Landing.Get.Journey3", "Hier voel je je thuis", "Where you feel at home", "Tu czujesz się jak w domu", "Aici te simți acasă", "هنا تشعر أنك في بيتك");
        Add("Landing.Get.Journey4", "Dit vind je belangrijk", "What matters to you", "To jest dla Ciebie ważne", "Ce contează pentru tine", "ما يهمك");
        Add("Landing.Get.MatchTitle", "💘 Match", "💘 Job match", "💘 Dopasowanie", "💘 Potrivire", "💘 تطابق");
        Add("Landing.Get.MatchBody", "Swipe door banen die bij je DNA passen.", "Swipe through jobs that fit your DNA.", "Przeglądaj oferty pasujące do Twojego DNA.", "Swipe printre joburi care se potrivesc ADN-ului tău.", "مرّر على وظائف تناسب حمضك.");
        Add("Landing.Get.MatchBadge", "⭐ Jouw top-match", "⭐ Your top match", "⭐ Twój top-match", "⭐ Top-match-ul tău", "⭐ أفضل تطابق لك");
        Add("Landing.Get.MatchRole", "Teamleider logistiek", "Logistics team lead", "Lider zespołu logistyki", "Lider echipă logistică", "قائد فريق لوجستي");
        Add("Landing.Get.MatchMeta", "Hoeve Transport · 18 min", "Hoeve Transport · 18 min bike", "Hoeve Transport · 18 min jazdy", "Hoeve Transport · 18 min drum", "Hoeve Transport · ١٨ د");
        Add("Landing.Get.ReportTitle", "📘 Jouw rapport", "📘 Your report", "📘 Twój raport", "📘 Raportul tău", "📘 تقريرك");
        Add("Landing.Get.ReportBody", "De diepteanalyse: meer vragen en een persoonlijk rapport om te bewaren.", "The deep analysis: more questions and a personal report to keep.", "Głęboka analiza: więcej pytań i osobisty raport do zachowania.", "Analiza profundă: mai multe întrebări și un raport personal de păstrat.", "التحليل العميق: مزيد من الأسئلة وتقرير شخصي تحفظه.");
        Add("Landing.Get.ReportName", "Dit ben jij · rapport", "This is you · report", "To Ty · raport", "Acesta ești tu · raport", "هذا أنت · تقرير");
        Add("Landing.Get.ReportPrice", "€ {0} eenmalig · na je gratis tests", "€ {0} once · after your free tests", "€ {0} jednorazowo · po darmowych testach", "€ {0} o singură dată · după testele gratuite", "€ {0} مرة واحدة · بعد اختباراتك المجانية");
    }

    private static void MergeForWhom(Action<string, string, string, string?, string?, string?> Add)
    {
        Add("Landing.For.Eyebrow", "Voor wie?", "Who is it for?", "Dla kogo?", "Pentru cine?", "لمن؟");
        Add("Landing.For.Title", "Lobsy is er voor iedereen die werk een plek geeft.", "Lobsy is for everyone who gives work a place.", "Lobsy jest dla każdego, kto daje pracy miejsce.", "Lobsy e pentru oricine dă un loc muncii.", "لوبسي لكل من يعطي العمل مكانًا.");
        Add("Landing.For.CandTitle", "Kandidaat", "Candidate", "Kandydat", "Candidat", "مرشح");
        Add("Landing.For.CandBody", "Voor jou als je werk zoekt, net in Nederland bent of vastzit.", "For you when you seek work, just arrived, or feel stuck.", "Dla Ciebie, gdy szukasz pracy, jesteś nowy w NL lub utknąłeś.", "Pentru tine când cauți de muncă, ești nou sau te-ai blocat.", "لك إن كنت تبحث عن عمل أو وصلت حديثًا أو عالق.");
        Add("Landing.For.Cand1", "Gratis tests en paspoort", "Free tests and passport", "Darmowe testy i paszport", "Teste și pașaport gratuite", "اختبارات وجواز مجانيان");
        Add("Landing.For.Cand2", "Banen op reistijd", "Jobs by travel time", "Oferty według czasu dojazdu", "Joburi după timpul de drum", "وظائف حسب وقت الرحلة");
        Add("Landing.For.Cand3", "Jij beslist wat je deelt", "You decide what you share", "Ty decydujesz, co udostępniasz", "Tu decizi ce împărtășești", "أنت تقرر ما تشاركه");
        Add("Landing.For.CandCta", "Doe de gratis test", "Take the free test", "Zrób darmowy test", "Fă testul gratuit", "أجرِ الاختبار المجاني");
        Add("Landing.For.EmpTitle", "Werkgever", "Employer", "Pracodawca", "Angajator", "صاحب عمل");
        Add("Landing.For.EmpBody", "Vind mensen die écht bij je team passen, niet alleen op papier.", "Find people who truly fit your team, not only on paper.", "Znajdź ludzi, którzy naprawdę pasują do zespołu, nie tylko na papierze.", "Găsește oameni care se potrivesc echipei, nu doar pe hârtie.", "اعثر على أشخاص يناسبون فريقك حقًا، لا على الورق فقط.");
        Add("Landing.For.Emp1", "Vacatures plaatsen", "Post vacancies", "Dodawaj oferty", "Publică joburi", "انشر وظائف");
        Add("Landing.For.Emp2", "Werken met tokens", "Work with tokens", "Praca z tokenami", "Lucru cu tokenuri", "العمل بالرموز");
        Add("Landing.For.Emp3", "Matches op DNA en reistijd", "Matches on DNA and travel time", "Dopasowania DNA i czas dojazdu", "Potriviri ADN și timp de drum", "تطابقات حسب الحمض ووقت الرحلة");
        Add("Landing.For.EmpCta", "Voor werkgevers", "For employers", "Dla pracodawców", "Pentru angajatori", "لأصحاب العمل");
        Add("Landing.For.SchTitle", "School", "Schools", "Szkoła", "Școală", "مدرسة");
        Add("Landing.For.SchBody", "Help leerlingen en studenten ontdekken wat bij ze past.", "Help pupils and students discover what fits them.", "Pomóż uczniom odkryć, co do nich pasuje.", "Ajută elevii să descopere ce li se potrivește.", "ساعد الطلاب على اكتشاف ما يناسبهم.");
        Add("Landing.For.Sch1", "Tests voor je klas", "Tests for your class", "Testy dla klasy", "Teste pentru clasă", "اختبارات لصفك");
        Add("Landing.For.Sch2", "Stages via je schooldomein", "Internships via your school domain", "Staże przez domenę szkoły", "Stagii prin domeniul școlii", "تدريب عبر نطاق مدرستك");
        Add("Landing.For.Sch3", "Inzicht per groep", "Insight per group", "Wgląd per grupa", "Insight pe grup", "رؤية لكل مجموعة");
        Add("Landing.For.SchCta", "Voor scholen", "For schools", "Dla szkół", "Pentru școli", "للمدارس");
        Add("Landing.For.ParTitle", "Partner", "Partners", "Partnerzy", "Partener", "شريك");
        Add("Landing.For.ParBody", "Breng Lobsy naar bedrijven en verdien mee als partner.", "Bring Lobsy to companies and earn as a partner.", "Przynieś Lobsy firmom i zarabiaj jako partner.", "Adu Lobsy la firme și câștigă ca partener.", "أحضِر لوبسي للشركات واكسب كشريك.");
        Add("Landing.For.Par1", "Eigen partnerlink", "Your own partner link", "Własny link partnerski", "Linkul tău de partener", "رابط شريك خاص بك");
        Add("Landing.For.Par2", "Toolkit en materiaal", "Toolkit and materials", "Narzędzia i materiały", "Kit și materiale", "أدوات ومواد");
        Add("Landing.For.Par3", "Inzicht in je resultaat", "Insight into your results", "Wgląd w wyniki", "Insight în rezultate", "رؤية لنتائجك");
        Add("Landing.For.ParCta", "Word partner", "Become a partner", "Zostań partnerem", "Devino partener", "كن شريكًا");
    }

    private static void MergePrivacy(Action<string, string, string, string?, string?, string?> Add)
    {
        Add("Landing.Privacy.Eyebrow", "Privacy en vertrouwen", "Privacy and trust", "Prywatność i zaufanie", "Confidențialitate și încredere", "الخصوصية والثقة");
        Add("Landing.Privacy.Title", "Jouw verhaal is van jou.", "Your story is yours.", "Twoja historia należy do Ciebie.", "Povestea ta e a ta.", "قصتك ملكك.");
        Add("Landing.Privacy.C1Title", "Eerst op jouw apparaat", "First on your device", "Najpierw na Twoim urządzeniu", "Mai întâi pe dispozitivul tău", "أولًا على جهازك");
        Add("Landing.Privacy.C1Body", "Je testantwoorden blijven op je eigen telefoon of computer tot je een account maakt.", "Your test answers stay on your phone or computer until you create an account.", "Odpowiedzi zostają na telefonie lub komputerze, aż założysz konto.", "Răspunsurile rămân pe telefon sau computer până creezi un cont.", "تبقى إجاباتك على هاتفك أو حاسوبك حتى تنشئ حسابًا.");
        Add("Landing.Privacy.C2Title", "Werkgevers zien niets zonder jou", "Employers see nothing without you", "Pracodawcy nic nie widzą bez Ciebie", "Angajatorii nu văd nimic fără tine", "أصحاب العمل لا يرون شيئًا دونك");
        Add("Landing.Privacy.C2Body", "Een werkgever ziet je antwoorden nooit. In je paspoort kies jij wat je deelt.", "An employer never sees your answers. In your passport you choose what you share.", "Pracodawca nigdy nie widzi odpowiedzi. W paszporcie Ty decydujesz, co udostępniasz.", "Angajatorul nu-ți vede niciodată răspunsurile. În pașaport tu alegi ce împărtășești.", "صاحب العمل لا يرى إجاباتك أبدًا. في جوازك تختار ما تشاركه.");
        Add("Landing.Privacy.C3Title", "Wissen kan altijd", "You can always erase", "Zawsze możesz usunąć", "Poți șterge oricând", "يمكنك المسح دائمًا");
        Add("Landing.Privacy.C4Title", "Veilig en eerlijk", "Safe and fair", "Bezpiecznie i uczciwie", "Sigur și corect", "آمن وعادل");
        Add("Landing.Privacy.C3Body", "Met één knop wis je je antwoorden. Je account verwijderen kan ook, zelf.", "One button clears your answers. You can also delete your account yourself.", "Jednym przyciskiem kasujesz odpowiedzi. Konto też usuwasz sam.", "Cu un buton ștergi răspunsurile. Poți șterge și contul singur.", "بزر واحد تمسح إجاباتك. ويمكنك حذف حسابك بنفسك.");
        Add("Landing.Privacy.C4Body", "Vanaf 16 jaar. Privacyregels in gewone taal. Geen verborgen kosten.", "From age 16. Privacy rules in plain language. No hidden costs.", "Od 16 lat. Zasady prywatności prostym językiem. Bez ukrytych kosztów.", "De la 16 ani. Reguli de confidențialitate în limbaj clar. Fără costuri ascunse.", "من سن ١٦. قواعد خصوصية بلغة بسيطة. بلا تكاليف مخفية.");
    }

    private static void MergeFaq(Action<string, string, string, string?, string?, string?> Add)
    {
        Add("Landing.Faq.Eyebrow", "Veelgestelde vragen", "Frequently asked questions", "Częste pytania", "Întrebări frecvente", "أسئلة شائعة");
        Add("Landing.Faq.Title", "Nog vragen?", "Still have questions?", "Jeszcze pytania?", "Mai ai întrebări?", "أسئلة أخرى؟");
        Add("Landing.Faq.Lead", "Kort en eerlijk antwoord.", "Short and honest answers.", "Krótko i szczerze.", "Răspunsuri scurte și sincere.", "إجابات قصيرة وصادقة.");
        Add("Landing.Faq.HelpTitle", "Hulp nodig?", "Need help?", "Potrzebujesz pomocy?", "Ai nevoie de ajutor?", "تحتاج مساعدة؟");
        Add("Landing.Faq.HelpBody", "Lees hoe Lobsy werkt.", "Read how Lobsy works.", "Przeczytaj, jak działa Lobsy.", "Citește cum funcționează Lobsy.", "اقرأ كيف يعمل لوبسي.");
        Add("Landing.Faq.HelpCta", "Hoe werkt Lobsy?", "How does Lobsy work?", "Jak działa Lobsy?", "Cum funcționează Lobsy?", "كيف يعمل لوبسي؟");
        Add("Landing.Faq.Q1", "Is Lobsy echt gratis?", "Is Lobsy really free?", "Czy Lobsy jest naprawdę darmowe?", "Lobsy e chiar gratuit?", "هل لوبسي مجاني حقًا؟");
        Add("Landing.Faq.A1", "Ja. De test, je account, je paspoort en de banenkaart zijn gratis. Alleen het uitgebreide rapport (de diepteanalyse) kost eenmalig {0}.", "Yes. The test, your account, passport and job map are free. Only the extended report (deep analysis) costs {0} once.", "Tak. Test, konto, paszport i mapa są darmowe. Tylko rozszerzony raport (głęboka analiza) kosztuje jednorazowo {0}.", "Da. Testul, contul, pașaportul și harta sunt gratuite. Doar raportul extins (analiza profundă) costă o dată {0}.", "نعم. الاختبار والحساب والجواز والخريطة مجانية. فقط التقرير الموسّع (التحليل العميق) يكلف مرة واحدة {0}.");
        Add("Landing.Faq.Q2", "Heb ik een account nodig voor de test?", "Do I need an account for the test?", "Czy potrzebuję konta do testu?", "Am nevoie de cont pentru test?", "هل أحتاج حسابًا للاختبار؟");
        Add("Landing.Faq.A2", "Nee. Je start meteen. Je antwoorden blijven 7 dagen op jouw apparaat. Maak later een account om ze te bewaren.", "No. You start right away. Answers stay on your device for 7 days. Create an account later to keep them.", "Nie. Zaczynasz od razu. Odpowiedzi zostają 7 dni na urządzeniu. Później załóż konto, by je zachować.", "Nu. Începi imediat. Răspunsurile stau 7 zile pe dispozitiv. Creează un cont mai târziu ca să le păstrezi.", "لا. تبدأ فورًا. تبقى الإجابات ٧ أيام على جهازك. أنشئ حسابًا لاحقًا لحفظها.");
        Add("Landing.Faq.Q3", "Wat gebeurt er met mijn antwoorden?", "What happens to my answers?", "Co się dzieje z moimi odpowiedziami?", "Ce se întâmplă cu răspunsurile mele?", "ماذا يحدث لإجاباتي؟");
        Add("Landing.Faq.A3", "Ze blijven 7 dagen op jouw apparaat. Bij account maken gaan ze mee. Je kunt ze altijd wissen met één knop.", "They stay on your device for 7 days. When you sign up they come with you. You can erase them anytime with one button.", "Zostają 7 dni na urządzeniu. Przy zakładaniu konta idą z Tobą. Zawsze możesz je usunąć jednym przyciskiem.", "Stau 7 zile pe dispozitiv. La crearea contului vin cu tine. Le poți șterge oricând cu un buton.", "تبقى ٧ أيام على جهازك. عند إنشاء الحساب تنتقل معك. يمكنك مسحها في أي وقت بزر واحد.");
        Add("Landing.Faq.Q4", "Ik spreek nog niet goed Nederlands. Kan ik Lobsy gebruiken?", "I do not speak Dutch well yet. Can I use Lobsy?", "Słabo mówię po niderlandzku. Mogę używać Lobsy?", "Nu vorbesc bine neerlandeză. Pot folosi Lobsy?", "لا أجيد الهولندية بعد. هل أستطيع استخدام لوبسي؟");
        Add("Landing.Faq.A4", "Ja. Lobsy is er in het Nederlands, Engels, Pools, Roemeens en Arabisch. Je wisselt van taal in het menu.", "Yes. Lobsy is available in Dutch, English, Polish, Romanian and Arabic. Switch language in the menu.", "Tak. Lobsy jest po niderlandzku, angielsku, polsku, rumuńsku i arabsku. Zmieniasz język w menu.", "Da. Lobsy e în neerlandeză, engleză, poloneză, română și arabă. Schimbi limba din meniu.", "نعم. لوبسي بالهولندية والإنجليزية والبولندية والرومانية والعربية. غيّر اللغة من القائمة.");
        Add("Landing.Faq.Q5", "Ik ben werkgever of school. Hoe begin ik?", "I am an employer or school. How do I start?", "Jestem pracodawcą lub szkołą. Jak zacząć?", "Sunt angajator sau școală. Cum încep?", "أنا صاحب عمل أو مدرسة. كيف أبدأ؟");
        Add("Landing.Faq.A5", "Werkgevers starten via de pagina voor werkgevers. Scholen lezen eerst Hoe werkt Lobsy? of mailen ons. Partners hebben een aparte partnerpagina.", "Employers start via the employers page. Schools first read How Lobsy works or email us. Partners have a separate partner page.", "Pracodawcy zaczynają od strony dla pracodawców. Szkoły najpierw czytają Jak działa Lobsy? lub piszą do nas. Partnerzy mają osobną stronę.", "Angajatorii încep de pe pagina pentru angajatori. Școlile citesc mai întâi Cum funcționează Lobsy? sau ne scriu. Partenerii au o pagină separată.", "يبدأ أصحاب العمل من صفحة أصحاب العمل. المدارس تقرأ أولًا كيف يعمل لوبسي أو تراسلنا. للشركاء صفحة منفصلة.");
        Add("Landing.Faq.Q6", "Waarom een kreeft?", "Why a lobster?", "Dlaczego homar?", "De ce un homar?", "لماذا الكركند؟");
        Add("Landing.Faq.A6", "Een kreeft groeit alleen als hij zijn oude schild loslaat. Dat is ook jouw verhaal bij werk of een nieuwe start. Lobsy is die veilige plek.", "A lobster grows only when it leaves its old shell. That is your story with work or a new start. Lobsy is that safe place.", "Homar rośnie tylko, gdy zostawia starą skorupę. To też Twoja historia przy pracy lub nowym starcie. Lobsy jest tym bezpiecznym miejscem.", "Homarul crește doar când lasă cochilia veche. Asta e și povestea ta la muncă sau un nou început. Lobsy e acel loc sigur.", "ينمو الكركند فقط عندما يترك صدفته القديمة. هذه قصتك مع العمل أو بداية جديدة. لوبسي هو ذلك المكان الآمن.");
        Add("Landing.Faq.Q7", "Hoe werkt Lobsy?", "How does Lobsy work?", "Jak działa Lobsy?", "Cum funcționează Lobsy?", "كيف يعمل لوبسي؟");
        Add("Landing.Faq.A7", "Je doet een korte gratis test, ziet wat bij je past op de banenkaart, en bouwt een paspoort dat jij deelt wanneer jij wilt.", "You take a short free test, see what fits on the job map, and build a passport you share when you want.", "Robisz krótki darmowy test, widzisz dopasowania na mapie i budujesz paszport, który udostępniasz, kiedy chcesz.", "Faci un test scurt gratuit, vezi ce se potrivește pe hartă și construiești un pașaport pe care îl împarți când vrei.", "تخوض اختبارًا مجانيًا قصيرًا، وترى ما يناسبك على الخريطة، وتبني جوازًا تشاركه متى شئت.");
    }

    private static void MergeClosing(Action<string, string, string, string?, string?, string?> Add)
    {
        Add("Landing.Close.Title", "Klaar om uit je schild te groeien?", "Ready to grow out of your shell?", "Gotowy wyjść ze skorupy?", "Gata să ieși din cochilie?", "مستعد للخروج من صدفتك؟");
        Add("Landing.Close.Lead", "20 vragen. 3 minuutjes. Daarna weet je meer over jezelf.", "20 questions. 3 minutes. Then you know more about yourself.", "20 pytań. 3 minuty. Potem wiesz więcej o sobie.", "20 de întrebări. 3 minute. Apoi știi mai multe despre tine.", "٢٠ سؤالًا. ٣ دقائق. بعدها تعرف المزيد عن نفسك.");
        Add("Landing.Close.CtaTest", "Doe de gratis test →", "Take the free test →", "Zrób darmowy test →", "Fă testul gratuit →", "أجرِ الاختبار المجاني ←");
        Add("Landing.Close.CtaLogin", "Inloggen", "Log in", "Zaloguj się", "Autentificare", "تسجيل الدخول");
    }
}
