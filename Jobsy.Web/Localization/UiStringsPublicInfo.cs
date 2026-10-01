namespace Jobsy.Web.Localization;

/// <summary>
/// Public information pages (public-pages 06–09): prefixes <c>HowLobsy.</c>, <c>About.</c>,
/// <c>PartnerPage.</c>, <c>CompanyPage.</c> and <c>Report.</c>. 06 adds the <c>Report.</c> keys for
/// the meldknop and the <c>/melden</c> form.
/// </summary>
public static class UiStringsPublicInfo
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

        // —— Entry points on the vacancy and the company page ——
        Add("Report.Vacancy.Link",
            "Meld deze vacature",
            "Report this vacancy",
            "Zgłoś tę ofertę",
            "Raportează acest job",
            "أبلغ عن هذه الوظيفة");
        Add("Report.Company.Link",
            "Meld dit bedrijf",
            "Report this company",
            "Zgłoś tę firmę",
            "Raportează această firmă",
            "أبلغ عن هذه الشركة");
        Add("Report.Company.Prompt",
            "Klopt er iets niet op deze pagina?",
            "Is something wrong on this page?",
            "Coś na tej stronie jest nie tak?",
            "Ceva nu este în regulă pe această pagină?",
            "هل هناك خطأ في هذه الصفحة؟");
        Add("Report.Company.PromptCta",
            "Meld het.",
            "Report it.",
            "Zgłoś to.",
            "Raportează.",
            "أبلغ عنه.");

        // —— /melden form ——
        Add("Report.Seo.Title",
            "Iets melden",
            "Report something",
            "Zgłoś coś",
            "Raportează ceva",
            "أبلغ عن شيء");
        Add("Report.Seo.Description",
            "Meld een vacature of een bedrijfspagina die niet klopt.",
            "Report a vacancy or a company page that is not right.",
            "Zgłoś ofertę lub stronę firmy, która jest nieprawidłowa.",
            "Raportează un job sau o pagină de firmă care nu este în regulă.",
            "أبلغ عن وظيفة أو صفحة شركة غير صحيحة.");
        Add("Report.Title",
            "Iets melden",
            "Report something",
            "Zgłoś coś",
            "Raportează ceva",
            "أبلغ عن شيء");
        Add("Report.Lead",
            "Vertel ons wat er niet klopt. We kijken ernaar.",
            "Tell us what is wrong. We will look into it.",
            "Napisz, co jest nie tak. Sprawdzimy to.",
            "Spune-ne ce nu este în regulă. Ne vom uita la asta.",
            "أخبرنا بما هو غير صحيح. سننظر في الأمر.");
        Add("Report.About",
            "Je meldt:",
            "You are reporting:",
            "Zgłaszasz:",
            "Raportezi:",
            "أنت تبلّغ عن:");
        Add("Report.About.Unknown",
            "een pagina op Lobsy",
            "a page on Lobsy",
            "stronę w Lobsy",
            "o pagină de pe Lobsy",
            "صفحة على Lobsy");
        Add("Report.ReasonLegend",
            "Wat is er aan de hand?",
            "What is going on?",
            "Co się dzieje?",
            "Ce se întâmplă?",
            "ما المشكلة؟");
        Add("Report.Reason.Fake",
            "Nep of niet echt",
            "Fake or not real",
            "Fałszywe lub nieprawdziwe",
            "Fals sau nereal",
            "مزيّف أو غير حقيقي");
        Add("Report.Reason.Discriminating",
            "Discriminerend",
            "Discriminating",
            "Dyskryminujące",
            "Discriminatoriu",
            "تمييزي");
        Add("Report.Reason.Illegal",
            "Mag niet volgens de wet",
            "Not allowed by law",
            "Niezgodne z prawem",
            "Nu este permis de lege",
            "غير مسموح قانوناً");
        Add("Report.Reason.WrongInfo",
            "Verkeerde informatie",
            "Wrong information",
            "Błędne informacje",
            "Informații greșite",
            "معلومات خاطئة");
        Add("Report.Reason.Unsafe",
            "Onveilig werk",
            "Unsafe work",
            "Niebezpieczna praca",
            "Muncă nesigură",
            "عمل غير آمن");
        Add("Report.Reason.Other",
            "Iets anders",
            "Something else",
            "Coś innego",
            "Altceva",
            "شيء آخر");
        Add("Report.Details",
            "Wil je er iets over vertellen?",
            "Do you want to tell us more?",
            "Chcesz powiedzieć więcej?",
            "Vrei să ne spui mai mult?",
            "هل تريد إخبارنا بالمزيد؟");
        Add("Report.Details.Hint",
            "Niet verplicht. Maximaal 1000 tekens.",
            "Not required. Up to 1000 characters.",
            "Nieobowiązkowe. Maksymalnie 1000 znaków.",
            "Nu este obligatoriu. Maximum 1000 de caractere.",
            "غير مطلوب. حتى 1000 حرف.");
        Add("Report.Email",
            "Je e-mailadres",
            "Your e-mail address",
            "Twój adres e-mail",
            "Adresa ta de e-mail",
            "بريدك الإلكتروني");
        Add("Report.Email.Hint",
            "Als je wilt dat we je laten weten wat we doen.",
            "If you want us to tell you what we do.",
            "Jeśli chcesz, żebyśmy poinformowali Cię, co zrobimy.",
            "Dacă vrei să îți spunem ce facem.",
            "إذا أردت أن نخبرك بما سنفعله.");
        Add("Report.Submit",
            "Melding versturen",
            "Send report",
            "Wyślij zgłoszenie",
            "Trimite raportarea",
            "إرسال التبليغ");
        Add("Report.FalseWarning",
            "Meld alleen iets als je denkt dat het echt niet klopt.",
            "Only report something if you really think it is wrong.",
            "Zgłaszaj tylko wtedy, gdy naprawdę uważasz, że coś jest nie tak.",
            "Raportează doar dacă crezi într-adevăr că ceva nu este în regulă.",
            "أبلغ فقط إذا كنت تعتقد فعلاً أن هناك خطأ.");
        Add("Report.Privacy",
            "We bewaren je e-mailadres alleen om je het besluit te laten weten. We bewaren je IP-adres niet.",
            "We keep your e-mail address only to tell you the outcome. We do not store your IP address.",
            "Twój e-mail przechowujemy tylko po to, aby poinformować Cię o decyzji. Nie przechowujemy adresu IP.",
            "Păstrăm e-mailul tău doar pentru a-ți comunica decizia. Nu stocăm adresa ta IP.",
            "نحفظ بريدك فقط لإبلاغك بالقرار. لا نحفظ عنوان IP الخاص بك.");
        Add("Report.Rules",
            "Lees hoe we met meldingen omgaan",
            "Read how we handle reports",
            "Przeczytaj, jak postępujemy ze zgłoszeniami",
            "Citește cum tratăm raportările",
            "اقرأ كيف نتعامل مع التبليغات");
        Add("Report.Success.Title",
            "Dank je. We kijken ernaar.",
            "Thank you. We are looking into it.",
            "Dziękujemy. Sprawdzamy to.",
            "Mulțumim. Ne uităm la asta.",
            "شكراً. نحن ننظر في الأمر.");
        Add("Report.Success.Email",
            "Je krijgt een bevestiging per e-mail.",
            "You will get a confirmation by e-mail.",
            "Otrzymasz potwierdzenie e-mailem.",
            "Vei primi o confirmare pe e-mail.",
            "ستصلك رسالة تأكيد بالبريد.");
        Add("Report.Success.Back",
            "Naar de banenkaart",
            "To the job map",
            "Do mapy ofert",
            "Către harta joburilor",
            "إلى خريطة الوظائف");
        Add("Report.Error.Reason",
            "Kies eerst wat er aan de hand is.",
            "Please choose what is going on first.",
            "Najpierw wybierz, co się dzieje.",
            "Alege mai întâi ce se întâmplă.",
            "اختر أولاً ما المشكلة.");
        Add("Report.Error.TooMany",
            "Je hebt al veel gemeld. Probeer het later opnieuw.",
            "You have reported a lot already. Please try again later.",
            "Wysłałeś już wiele zgłoszeń. Spróbuj później.",
            "Ai raportat deja de multe ori. Încearcă mai târziu.",
            "لقد أبلغت كثيراً بالفعل. حاول لاحقاً.");
        Add("Report.Error.Retry",
            "Het lukte niet. Probeer het nog een keer.",
            "That did not work. Please try again.",
            "Nie udało się. Spróbuj ponownie.",
            "Nu a funcționat. Încearcă din nou.",
            "لم ينجح الأمر. حاول مرة أخرى.");

        // —— Admin tab "Meldingen" ——
        Add("Report.Admin.Tab",
            "Meldingen",
            "Reports",
            "Zgłoszenia",
            "Raportări",
            "التبليغات");
        Add("Report.Admin.Lead",
            "Meldingen over vacatures en bedrijfspagina's. Neem een besluit met een reden.",
            "Reports about vacancies and company pages. Decide with a reason.",
            "Zgłoszenia dotyczące ofert i stron firm. Zdecyduj z podaniem powodu.",
            "Raportări despre joburi și pagini de firme. Decide cu un motiv.",
            "تبليغات عن وظائف وصفحات شركات. اتخذ قراراً مع ذكر السبب.");
        Add("Report.Admin.Kind.Vacancy",
            "Vacature",
            "Vacancy",
            "Oferta",
            "Job",
            "وظيفة");
        Add("Report.Admin.Kind.Company",
            "Bedrijfspagina",
            "Company page",
            "Strona firmy",
            "Pagină de firmă",
            "صفحة شركة");
        Add("Report.Admin.What",
            "Wat",
            "What",
            "Co",
            "Ce",
            "ماذا");
        Add("Report.Admin.Reason",
            "Reden",
            "Reason",
            "Powód",
            "Motiv",
            "السبب");
        Add("Report.Admin.When",
            "Wanneer",
            "When",
            "Kiedy",
            "Când",
            "متى");
        Add("Report.Admin.Count",
            "Meldingen",
            "Reports",
            "Zgłoszenia",
            "Raportări",
            "التبليغات");
        Add("Report.Admin.Status",
            "Status",
            "Status",
            "Status",
            "Stare",
            "الحالة");
        Add("Report.Admin.FilterOpen",
            "Open",
            "Open",
            "Otwarte",
            "Deschise",
            "مفتوحة");
        Add("Report.Admin.FilterClosed",
            "Afgehandeld",
            "Handled",
            "Zakończone",
            "Rezolvate",
            "تمت معالجتها");
        Add("Report.Admin.Empty",
            "Geen meldingen.",
            "No reports.",
            "Brak zgłoszeń.",
            "Nicio raportare.",
            "لا توجد تبليغات.");
        Add("Report.Admin.Reporter",
            "Melder",
            "Reporter",
            "Zgłaszający",
            "Raportor",
            "المُبلِّغ");
        Add("Report.Admin.Anonymous",
            "Anoniem",
            "Anonymous",
            "Anonimowo",
            "Anonim",
            "مجهول");
        Add("Report.Admin.OpenPublic",
            "Bekijk de pagina",
            "View the page",
            "Zobacz stronę",
            "Vezi pagina",
            "اعرض الصفحة");
        Add("Report.Admin.OpenAdmin",
            "Open in beheer",
            "Open in admin",
            "Otwórz w panelu",
            "Deschide în administrare",
            "افتح في الإدارة");
        Add("Report.Admin.DecisionLegend",
            "Neem een besluit",
            "Make a decision",
            "Podejmij decyzję",
            "Ia o decizie",
            "اتخذ قراراً");
        Add("Report.Admin.NoAction",
            "Geen actie",
            "No action",
            "Bez działania",
            "Fără acțiune",
            "بدون إجراء");
        Add("Report.Admin.Restrict",
            "Beperken",
            "Limit",
            "Ogranicz",
            "Limitează",
            "تقييد");
        Add("Report.Admin.Remove",
            "Verwijderen",
            "Remove",
            "Usuń",
            "Scoate",
            "إزالة");
        Add("Report.Admin.ReasonLabel",
            "Reden van het besluit",
            "Reason for the decision",
            "Powód decyzji",
            "Motivul deciziei",
            "سبب القرار");
        Add("Report.Admin.ReasonHint",
            "Schrijf kort wat er mis is en welke regel het breekt.",
            "Write briefly what is wrong and which rule it breaks.",
            "Napisz krótko, co jest nie tak i którą zasadę to łamie.",
            "Scrie scurt ce nu este în regulă și ce regulă încalcă.",
            "اكتب بإيجاز ما الخطأ وأي قاعدة يخالفها.");
        Add("Report.Admin.ReasonRequired",
            "Een reden is verplicht bij Beperken en Verwijderen.",
            "A reason is required for Limit and Remove.",
            "Powód jest wymagany przy Ogranicz i Usuń.",
            "Un motiv este obligatoriu pentru Limitează și Scoate.",
            "السبب مطلوب عند التقييد والإزالة.");
        Add("Report.Admin.Saved",
            "Besluit opgeslagen. De betrokkenen krijgen bericht.",
            "Decision saved. The people involved get a message.",
            "Decyzja zapisana. Zainteresowani otrzymają wiadomość.",
            "Decizie salvată. Persoanele implicate primesc un mesaj.",
            "تم حفظ القرار. سيتم إبلاغ المعنيين.");
        Add("Report.Admin.Status.Open",
            "Open",
            "Open",
            "Otwarte",
            "Deschis",
            "مفتوح");
        Add("Report.Admin.Status.NoAction",
            "Geen actie",
            "No action",
            "Bez działania",
            "Fără acțiune",
            "بدون إجراء");
        Add("Report.Admin.Status.Restricted",
            "Beperkt",
            "Limited",
            "Ograniczone",
            "Limitat",
            "مُقيَّد");
        Add("Report.Admin.Status.Removed",
            "Verwijderd",
            "Removed",
            "Usunięte",
            "Scos",
            "تمت الإزالة");

        MergeHowLobsy(Add);
        MergeAbout(Add);
        MergePartnerPage(Add);
        MergeCompanyPage(Add);
    }

    private delegate void AddKey(string key, string nl, string en, string pl, string ro, string ar);

    /// <summary>Static <c>/hoe-werkt-lobsy</c> (public-pages 08). <c>.Zw</c> siblings are the werkgevers-OFF copy.</summary>
    private static void MergeHowLobsy(AddKey Add)
    {
        Add("HowLobsy.Seo.Title",
            "Hoe werkt Lobsy?",
            "How does Lobsy work?",
            "Jak działa Lobsy?",
            "Cum funcționează Lobsy?",
            "كيف يعمل Lobsy؟");
        Add("HowLobsy.Seo.Description",
            "In vier stappen: kijk op de kaart, doe de gratis test, maak een account en solliciteer op jouw tempo.",
            "In four steps: look at the map, take the free test, create an account and apply at your own pace.",
            "W czterech krokach: zobacz mapę, zrób darmowy test, utwórz konto i aplikuj w swoim tempie.",
            "În patru pași: uită-te pe hartă, fă testul gratuit, creează un cont și aplică în ritmul tău.",
            "في أربع خطوات: انظر إلى الخريطة، اخض الاختبار المجاني، أنشئ حساباً، وتقدَّم بالسرعة التي تريحك.");
        Add("HowLobsy.Eyebrow",
            "Hoe werkt Lobsy?",
            "How does Lobsy work?",
            "Jak działa Lobsy?",
            "Cum funcționează Lobsy?",
            "كيف يعمل Lobsy؟");
        Add("HowLobsy.Title",
            "Zo werkt Lobsy. In 4 stappen.",
            "This is how Lobsy works. In 4 steps.",
            "Tak działa Lobsy. W 4 krokach.",
            "Așa funcționează Lobsy. În 4 pași.",
            "هكذا يعمل Lobsy. في 4 خطوات.");
        Add("HowLobsy.Lead",
            "Eerst kijk je wie je bent en wat je kunt. Daarna vind je werk dat past, dichtbij huis.",
            "First you look at who you are and what you can do. Then you find work that fits, close to home.",
            "Najpierw sprawdzasz, kim jesteś i co umiesz. Potem znajdujesz pracę, która pasuje, blisko domu.",
            "Mai întâi vezi cine ești și ce poți. Apoi găsești muncă potrivită, aproape de casă.",
            "أولاً تتعرّف على نفسك وعلى ما تستطيع. ثم تجد عملاً يناسبك قريباً من بيتك.");
        Add("HowLobsy.Lead.Zw",
            "Eerst kijk je wie je bent en wat je kunt. Daarna weet je welk werk bij je past.",
            "First you look at who you are and what you can do. Then you know which work fits you.",
            "Najpierw sprawdzasz, kim jesteś i co umiesz. Potem wiesz, jaka praca do Ciebie pasuje.",
            "Mai întâi vezi cine ești și ce poți. Apoi știi ce fel de muncă ți se potrivește.",
            "أولاً تتعرّف على نفسك وعلى ما تستطيع. ثم تعرف أي عمل يناسبك.");

        Add("HowLobsy.Tabs.Label",
            "Voor wie wil je het weten?",
            "Who do you want to read about?",
            "O kim chcesz przeczytać?",
            "Despre cine vrei să citești?",
            "عن مَن تريد أن تقرأ؟");
        Add("HowLobsy.Tab.You",
            "Voor jou",
            "For you",
            "Dla Ciebie",
            "Pentru tine",
            "لك");
        Add("HowLobsy.Tab.YouRole",
            "Voor jou ({0})",
            "For you ({0})",
            "Dla Ciebie ({0})",
            "Pentru tine ({0})",
            "لك ({0})");
        Add("HowLobsy.Tab.Employers",
            "Voor werkgevers",
            "For employers",
            "Dla pracodawców",
            "Pentru angajatori",
            "لأصحاب العمل");
        Add("HowLobsy.Tab.Schools",
            "Voor scholen",
            "For schools",
            "Dla szkół",
            "Pentru școli",
            "للمدارس");

        Add("HowLobsy.Steps.Label",
            "De vier stappen",
            "The four steps",
            "Cztery kroki",
            "Cei patru pași",
            "الخطوات الأربع");
        Add("HowLobsy.Step1.Title",
            "Kijk op de kaart",
            "Look at the map",
            "Zobacz mapę",
            "Uită-te pe hartă",
            "انظر إلى الخريطة");
        Add("HowLobsy.Step1.Title.Zw",
            "Maak je paspoort",
            "Build your passport",
            "Zbuduj swój paszport",
            "Construiește-ți pașaportul",
            "ابنِ جوازك");
        Add("HowLobsy.Step1.Body",
            "Je ziet vacatures bij jou in de buurt. Op fiets-, OV- of autotijd.",
            "You see vacancies near you. By bike, public transport or car time.",
            "Widzisz oferty blisko Ciebie. Według czasu na rowerze, komunikacją lub samochodem.",
            "Vezi joburi aproape de tine. După timpul cu bicicleta, transportul public sau mașina.",
            "ترى الوظائف القريبة منك، بحسب زمن الدراجة أو المواصلات أو السيارة.");
        Add("HowLobsy.Step1.Body.Zw",
            "Je eigen paspoort met je DNA, je tests en je bewijzen. Jij kiest wat je deelt.",
            "Your own passport with your DNA, your tests and your proofs. You choose what you share.",
            "Twój paszport z DNA, testami i dowodami. Ty wybierasz, co udostępniasz.",
            "Pașaportul tău cu ADN-ul, testele și dovezile tale. Tu alegi ce împarți.",
            "جوازك الخاص مع حمضك واختباراتك وإثباتاتك. أنت تختار ما تشاركه.");
        Add("HowLobsy.Step1.Cta",
            "Open de banenkaart",
            "Open the job map",
            "Otwórz mapę ofert",
            "Deschide harta joburilor",
            "افتح خريطة الوظائف");
        Add("HowLobsy.Step2.Title",
            "Doe de gratis test",
            "Take the free test",
            "Zrób darmowy test",
            "Fă testul gratuit",
            "اخض الاختبار المجاني");
        Add("HowLobsy.Step2.Body",
            "Korte vragen. Je ziet meteen wat bij je past. Geen account nodig.",
            "Short questions. You see right away what fits you. No account needed.",
            "Krótkie pytania. Od razu widzisz, co do Ciebie pasuje. Konto nie jest potrzebne.",
            "Întrebări scurte. Vezi imediat ce ți se potrivește. Fără cont.",
            "أسئلة قصيرة. ترى فوراً ما يناسبك. دون حساب.");
        Add("HowLobsy.Step2.Cta",
            "Doe de test",
            "Take the test",
            "Zrób test",
            "Fă testul",
            "اخض الاختبار");
        Add("HowLobsy.Step3.Title",
            "Maak een account",
            "Create an account",
            "Utwórz konto",
            "Creează un cont",
            "أنشئ حساباً");
        Add("HowLobsy.Step3.Body",
            "Pas als je wilt solliciteren. Je testuitslag gaat mee.",
            "Only when you want to apply. Your test result comes along.",
            "Dopiero gdy chcesz aplikować. Wynik testu idzie z Tobą.",
            "Doar când vrei să aplici. Rezultatul testului vine cu tine.",
            "فقط عندما تريد التقدّم. نتيجة اختبارك تنتقل معك.");
        Add("HowLobsy.Step3.Body.Zw",
            "Pas als je je paspoort wilt bewaren. Je testuitslag gaat mee.",
            "Only when you want to keep your passport. Your test result comes along.",
            "Dopiero gdy chcesz zachować paszport. Wynik testu idzie z Tobą.",
            "Doar când vrei să păstrezi pașaportul. Rezultatul testului vine cu tine.",
            "فقط عندما تريد الاحتفاظ بجوازك. نتيجة اختبارك تنتقل معك.");
        Add("HowLobsy.Step3.Cta",
            "Account maken",
            "Create an account",
            "Utwórz konto",
            "Creează cont",
            "أنشئ حساباً");
        Add("HowLobsy.Step4.Title",
            "Solliciteer op jouw tempo",
            "Apply at your own pace",
            "Aplikuj w swoim tempie",
            "Aplică în ritmul tău",
            "تقدَّم بالسرعة التي تريحك");
        Add("HowLobsy.Step4.Title.Zw",
            "Groei verder op jouw tempo",
            "Keep growing at your own pace",
            "Rozwijaj się w swoim tempie",
            "Crește în ritmul tău",
            "واصل النمو بالسرعة التي تريحك");
        Add("HowLobsy.Step4.Body",
            "Werkgevers zien je gegevens pas als jij dat goed vindt.",
            "Employers only see your details when you agree to it.",
            "Pracodawcy widzą Twoje dane tylko wtedy, gdy się zgodzisz.",
            "Angajatorii îți văd datele doar dacă ești de acord.",
            "لا يرى أصحاب العمل بياناتك إلا إذا وافقت.");
        Add("HowLobsy.Step4.Body.Zw",
            "Je ziet wat bij je past en welke stap je kunt zetten. Jij kiest wat je deelt.",
            "You see what fits you and which step you can take. You choose what you share.",
            "Widzisz, co do Ciebie pasuje i jaki krok możesz zrobić. Ty wybierasz, co udostępniasz.",
            "Vezi ce ți se potrivește și ce pas poți face. Tu alegi ce împarți.",
            "ترى ما يناسبك وأي خطوة يمكنك اتخاذها. أنت تختار ما تشاركه.");

        Add("HowLobsy.Promise.Title",
            "Dit beloven we je",
            "This is what we promise you",
            "To Ci obiecujemy",
            "Asta îți promitem",
            "هذا ما نَعِدك به");
        Add("HowLobsy.Promise.Free",
            "Gratis voor werkzoekenden. Altijd.",
            "Free for job seekers. Always.",
            "Darmowe dla szukających pracy. Zawsze.",
            "Gratuit pentru cei care caută muncă. Întotdeauna.",
            "مجاني للباحثين عن عمل. دائماً.");
        Add("HowLobsy.Promise.Answers",
            "Werkgevers zien je testantwoorden nooit.",
            "Employers never see your test answers.",
            "Pracodawcy nigdy nie widzą Twoich odpowiedzi z testu.",
            "Angajatorii nu îți văd niciodată răspunsurile din test.",
            "لا يرى أصحاب العمل إجابات اختبارك أبداً.");
        Add("HowLobsy.Promise.Answers.Zw",
            "Niemand anders ziet je testantwoorden.",
            "Nobody else sees your test answers.",
            "Nikt inny nie widzi Twoich odpowiedzi z testu.",
            "Nimeni altcineva nu îți vede răspunsurile din test.",
            "لا يرى أحد غيرك إجابات اختبارك.");
        Add("HowLobsy.Promise.Contact",
            "Je naam en telefoon deel je pas na jouw ja.",
            "You share your name and phone number only after your yes.",
            "Imię i telefon udostępniasz dopiero po swojej zgodzie.",
            "Numele și telefonul le împarți doar după acordul tău.",
            "تشارك اسمك ورقم هاتفك فقط بعد موافقتك.");
        Add("HowLobsy.Promise.Delete",
            "Je kunt je account en gegevens zelf verwijderen.",
            "You can delete your account and data yourself.",
            "Konto i dane możesz usunąć samodzielnie.",
            "Îți poți șterge singur contul și datele.",
            "يمكنك حذف حسابك وبياناتك بنفسك.");

        Add("HowLobsy.Faq.Title",
            "Vragen",
            "Questions",
            "Pytania",
            "Întrebări",
            "أسئلة");
        Add("HowLobsy.Faq.PriceQ",
            "Kost Lobsy geld?",
            "Does Lobsy cost money?",
            "Czy Lobsy kosztuje?",
            "Lobsy costă bani?",
            "هل Lobsy بمقابل؟");
        Add("HowLobsy.Faq.PriceA",
            "Nee. Voor werkzoekenden is Lobsy gratis. Alleen de uitgebreide testanalyse kost vanaf {0}, en die heb je niet nodig.",
            "No. For job seekers Lobsy is free. Only the extended test analysis costs from {0}, and you do not need it.",
            "Nie. Dla szukających pracy Lobsy jest darmowe. Tylko rozszerzona analiza testu kosztuje od {0}, a nie jest potrzebna.",
            "Nu. Pentru cei care caută muncă, Lobsy este gratuit. Doar analiza extinsă a testului costă de la {0} și nu îți este necesară.",
            "لا. Lobsy مجاني للباحثين عن عمل. فقط التحليل الموسّع للاختبار يبدأ من {0}، وأنت لا تحتاجه.");
        Add("HowLobsy.Faq.CvQ",
            "Heb ik een cv nodig?",
            "Do I need a CV?",
            "Czy potrzebuję CV?",
            "Am nevoie de CV?",
            "هل أحتاج إلى سيرة ذاتية؟");
        Add("HowLobsy.Faq.CvA",
            "Nee. Je kunt solliciteren met je profiel. Een cv toevoegen mag, maar hoeft niet.",
            "No. You can apply with your profile. Adding a CV is allowed, but not required.",
            "Nie. Możesz aplikować ze swoim profilem. CV możesz dodać, ale nie musisz.",
            "Nu. Poți aplica cu profilul tău. Poți adăuga un CV, dar nu este obligatoriu.",
            "لا. يمكنك التقدّم بملفك الشخصي. إضافة سيرة ذاتية مسموحة لكنها غير مطلوبة.");
        Add("HowLobsy.Faq.CvA.Zw",
            "Nee. Je paspoort is genoeg. Een cv toevoegen mag, maar hoeft niet.",
            "No. Your passport is enough. Adding a CV is allowed, but not required.",
            "Nie. Twój paszport wystarczy. CV możesz dodać, ale nie musisz.",
            "Nu. Pașaportul tău este suficient. Poți adăuga un CV, dar nu este obligatoriu.",
            "لا. جوازك يكفي. إضافة سيرة ذاتية مسموحة لكنها غير مطلوبة.");
        Add("HowLobsy.Faq.AgeQ",
            "Ik ben jonger dan 16. Kan ik meedoen?",
            "I am younger than 16. Can I join?",
            "Mam mniej niż 16 lat. Czy mogę?",
            "Am mai puțin de 16 ani. Pot participa?",
            "عمري أقل من 16. هل أستطيع المشاركة؟");

        Add("HowLobsy.You.SignedInTitle",
            "Je bent ingelogd",
            "You are signed in",
            "Jesteś zalogowany",
            "Ești conectat",
            "أنت مسجَّل الدخول");
        Add("HowLobsy.You.SignedInBody",
            "Ga verder waar je gebleven was.",
            "Continue where you left off.",
            "Kontynuuj tam, gdzie skończyłeś.",
            "Continuă de unde ai rămas.",
            "تابع من حيث توقفت.");
        Add("HowLobsy.You.StartCta",
            "Naar je start",
            "To your start",
            "Do strony startowej",
            "Spre pagina ta de start",
            "إلى صفحة بدايتك");

        Add("HowLobsy.Employers.Title",
            "Zo werkt Lobsy voor werkgevers.",
            "This is how Lobsy works for employers.",
            "Tak działa Lobsy dla pracodawców.",
            "Așa funcționează Lobsy pentru angajatori.",
            "هكذا يعمل Lobsy لأصحاب العمل.");
        Add("HowLobsy.Employers.Lead",
            "Je vacature staat op de kaart. Je ziet wie past, zonder stapels cv's.",
            "Your vacancy is on the map. You see who fits, without piles of CVs.",
            "Twoja oferta jest na mapie. Widzisz, kto pasuje, bez stosów CV.",
            "Jobul tău este pe hartă. Vezi cine se potrivește, fără stive de CV-uri.",
            "وظيفتك على الخريطة. ترى مَن يناسبك دون أكوام من السير الذاتية.");
        Add("HowLobsy.Employers.Step1Title",
            "Meld je bedrijf aan",
            "Register your company",
            "Zgłoś swoją firmę",
            "Înscrie firma ta",
            "سجّل شركتك");
        Add("HowLobsy.Employers.Step1Body",
            "Je vult je KvK-nummer in. Wij controleren je bedrijf.",
            "You enter your Chamber of Commerce number. We check your company.",
            "Podajesz numer rejestrowy firmy. My ją sprawdzamy.",
            "Introduci numărul de înregistrare al firmei. Noi o verificăm.",
            "تُدخل رقم تسجيل شركتك. ونحن نتحقق منها.");
        Add("HowLobsy.Employers.Step2Title",
            "Plaats je vacature",
            "Publish your vacancy",
            "Opublikuj ofertę",
            "Publică jobul",
            "انشر وظيفتك");
        Add("HowLobsy.Employers.Step2Body",
            "Je baan komt op de banenkaart, op reistijd rond jouw locatie.",
            "Your job appears on the job map, by travel time around your location.",
            "Twoja oferta pojawia się na mapie, według czasu dojazdu do Twojej lokalizacji.",
            "Jobul apare pe hartă, după timpul de călătorie până la locația ta.",
            "تظهر وظيفتك على الخريطة بحسب زمن التنقّل إلى موقعك.");
        Add("HowLobsy.Employers.Step3Title",
            "Praat met mensen die passen",
            "Talk to people who fit",
            "Rozmawiaj z pasującymi osobami",
            "Vorbește cu oameni care se potrivesc",
            "تحدّث مع مَن يناسبك");
        Add("HowLobsy.Employers.Step3Body",
            "Je ziet contactgegevens pas als de kandidaat ja zegt. Testantwoorden zie je nooit.",
            "You see contact details only after the candidate says yes. You never see test answers.",
            "Dane kontaktowe widzisz dopiero po zgodzie kandydata. Odpowiedzi z testu nigdy.",
            "Vezi datele de contact doar după ce candidatul acceptă. Răspunsurile din test niciodată.",
            "ترى بيانات الاتصال فقط بعد موافقة المرشّح. ولا ترى إجابات الاختبار أبداً.");
        Add("HowLobsy.Employers.Cta",
            "Lees meer voor werkgevers",
            "Read more for employers",
            "Więcej dla pracodawców",
            "Citește mai mult pentru angajatori",
            "اقرأ المزيد لأصحاب العمل");

        Add("HowLobsy.Schools.Title",
            "Zo werkt Lobsy voor scholen.",
            "This is how Lobsy works for schools.",
            "Tak działa Lobsy dla szkół.",
            "Așa funcționează Lobsy pentru școli.",
            "هكذا يعمل Lobsy للمدارس.");
        Add("HowLobsy.Schools.Lead",
            "Leerlingen ontdekken wat bij ze past. Jij ziet alleen groepsbeelden, geen antwoorden.",
            "Pupils discover what fits them. You only see group pictures, never answers.",
            "Uczniowie odkrywają, co do nich pasuje. Ty widzisz tylko obraz grupy, nie odpowiedzi.",
            "Elevii descoperă ce li se potrivește. Tu vezi doar imaginea grupei, nu răspunsurile.",
            "يكتشف التلاميذ ما يناسبهم. أنت ترى صورة المجموعة فقط، لا الإجابات.");
        Add("HowLobsy.Schools.Step1Title",
            "Vraag een klascode aan",
            "Request a class code",
            "Poproś o kod klasy",
            "Cere un cod de clasă",
            "اطلب رمز الصف");
        Add("HowLobsy.Schools.Step1Body",
            "Je krijgt een code voor je klas. Leerlingen hebben geen account nodig.",
            "You get a code for your class. Pupils do not need an account.",
            "Dostajesz kod dla klasy. Uczniowie nie potrzebują konta.",
            "Primești un cod pentru clasă. Elevii nu au nevoie de cont.",
            "تحصل على رمز لصفك. ولا يحتاج التلاميذ إلى حساب.");
        Add("HowLobsy.Schools.Step2Title",
            "Leerlingen doen de test",
            "Pupils take the test",
            "Uczniowie robią test",
            "Elevii fac testul",
            "يخوض التلاميذ الاختبار");
        Add("HowLobsy.Schools.Step2Body",
            "In één les. Iedere leerling ziet een eigen uitslag in gewone taal.",
            "In one lesson. Every pupil sees their own result in plain language.",
            "W jednej lekcji. Każdy uczeń widzi własny wynik prostym językiem.",
            "Într-o singură lecție. Fiecare elev vede propriul rezultat în limbaj simplu.",
            "في حصة واحدة. يرى كل تلميذ نتيجته بلغة بسيطة.");
        Add("HowLobsy.Schools.Step3Title",
            "Praat na in de klas",
            "Talk it through in class",
            "Omówcie to w klasie",
            "Discutați în clasă",
            "ناقشوا النتائج في الصف");
        Add("HowLobsy.Schools.Step3Body",
            "Je ziet een groepsbeeld voor het gesprek. Losse antwoorden blijven privé.",
            "You see a group picture for the conversation. Individual answers stay private.",
            "Widzisz obraz grupy do rozmowy. Pojedyncze odpowiedzi pozostają prywatne.",
            "Vezi imaginea grupei pentru discuție. Răspunsurile individuale rămân private.",
            "ترى صورة المجموعة للحوار. تبقى الإجابات الفردية خاصة.");
        Add("HowLobsy.Schools.Cta",
            "Lees meer voor scholen",
            "Read more for schools",
            "Więcej dla szkół",
            "Citește mai mult pentru școli",
            "اقرأ المزيد للمدارس");
    }

    /// <summary>Static <c>/wie-zijn-wij</c> (public-pages 08, D10: no admin-editable text).</summary>
    private static void MergeAbout(AddKey Add)
    {
        Add("About.Eyebrow",
            "Wie zijn wij",
            "About us",
            "O nas",
            "Despre noi",
            "من نحن");
        Add("About.Title",
            "Hoi! Wij zijn Lobsy.",
            "Hi! We are Lobsy.",
            "Cześć! Jesteśmy Lobsy.",
            "Salut! Noi suntem Lobsy.",
            "مرحباً! نحن Lobsy.");
        Add("About.Lead",
            "Een klein team uit het Westland. We helpen mensen werk vinden dat past, dichtbij huis.",
            "A small team from the Westland. We help people find work that fits, close to home.",
            "Mały zespół z Westland. Pomagamy ludziom znaleźć pasującą pracę blisko domu.",
            "O echipă mică din Westland. Ajutăm oamenii să găsească muncă potrivită, aproape de casă.",
            "فريق صغير من منطقة ويستلاند. نساعد الناس على إيجاد عمل يناسبهم قريباً من بيوتهم.");
        Add("About.Stories.Label",
            "Ons verhaal",
            "Our story",
            "Nasza historia",
            "Povestea noastră",
            "قصتنا");
        Add("About.Story.Lobster.Title",
            "Waarom een kreeft?",
            "Why a lobster?",
            "Dlaczego homar?",
            "De ce un homar?",
            "لماذا سرطان البحر؟");
        Add("About.Story.Lobster.Body",
            "Een kreeft groeit door zijn oude schild af te werpen. Zo zien wij werk zoeken ook: je groeit, stap voor stap.",
            "A lobster grows by shedding its old shell. That is how we see job hunting: you grow, step by step.",
            "Homar rośnie, zrzucając starą skorupę. Tak samo widzimy szukanie pracy: rośniesz krok po kroku.",
            "Homarul crește lepădând carapacea veche. Așa vedem și căutarea unui job: crești pas cu pas.",
            "ينمو سرطان البحر بخلع قشرته القديمة. هكذا نرى البحث عن عمل: تنمو خطوة بخطوة.");
        Add("About.Story.Westland.Title",
            "Begonnen in het Westland",
            "Started in the Westland",
            "Zaczęło się w Westland",
            "A început în Westland",
            "بدأنا في ويستلاند");
        Add("About.Story.Westland.Body",
            "Lobsy begon met één vraag: hoe vinden mensen dichtbij werk dat echt past? Van daaruit bouwen we verder.",
            "Lobsy started with one question: how do people nearby find work that really fits? From there we keep building.",
            "Lobsy zaczęło się od jednego pytania: jak ludzie w okolicy znajdują pracę, która naprawdę pasuje? Od tego budujemy dalej.",
            "Lobsy a început cu o întrebare: cum găsesc oamenii din apropiere muncă ce li se potrivește? De acolo construim mai departe.",
            "بدأ Lobsy بسؤال واحد: كيف يجد الناس القريبون عملاً يناسبهم فعلاً؟ ومن هناك نكمل البناء.");
        Add("About.Story.BothSides.Title",
            "Voor twee kanten",
            "For both sides",
            "Dla obu stron",
            "Pentru ambele părți",
            "للطرفين");
        Add("About.Story.BothSides.Body",
            "Voor werkzoekenden: eerlijk en duidelijk. Voor werkgevers: snel en zonder gedoe.",
            "For job seekers: honest and clear. For employers: fast and without hassle.",
            "Dla szukających pracy: szczerze i jasno. Dla pracodawców: szybko i bez kłopotów.",
            "Pentru cei care caută muncă: cinstit și clar. Pentru angajatori: rapid și fără bătăi de cap.",
            "للباحثين عن عمل: بصدق ووضوح. ولأصحاب العمل: بسرعة ودون تعقيد.");
        Add("About.Founder.Name",
            "Dennis, oprichter",
            "Dennis, founder",
            "Dennis, założyciel",
            "Dennis, fondator",
            "دينيس، المؤسس");
        Add("About.Founder.Text",
            "Ik bouw software die logisch voelt. Met Lobsy wil ik dat iedereen dichtbij werk kan vinden, ook zonder cv of perfect Nederlands.",
            "I build software that feels logical. With Lobsy I want everyone to find work nearby, also without a CV or perfect Dutch.",
            "Buduję oprogramowanie, które ma sens. Chcę, by dzięki Lobsy każdy znalazł pracę blisko domu, też bez CV i perfekcyjnego niderlandzkiego.",
            "Construiesc software care are logică. Cu Lobsy vreau ca oricine să găsească muncă aproape de casă, și fără CV sau olandeză perfectă.",
            "أبني برمجيات منطقية. أريد مع Lobsy أن يجد الجميع عملاً قريباً، حتى بدون سيرة ذاتية أو لغة هولندية مثالية.");
        Add("About.Founder.PhotoAlt",
            "Dennis, oprichter van Lobsy",
            "Dennis, founder of Lobsy",
            "Dennis, założyciel Lobsy",
            "Dennis, fondatorul Lobsy",
            "دينيس، مؤسس Lobsy");
        Add("About.Contact.Title",
            "Contact en bedrijfsgegevens",
            "Contact and company details",
            "Kontakt i dane firmy",
            "Contact și date de firmă",
            "الاتصال وبيانات الشركة");
        Add("About.Contact.Lead",
            "Vraag of idee? Mail ons, we reageren binnen 2 werkdagen.",
            "A question or an idea? Mail us, we answer within 2 working days.",
            "Pytanie albo pomysł? Napisz do nas, odpowiadamy w 2 dni robocze.",
            "O întrebare sau o idee? Scrie-ne, răspundem în 2 zile lucrătoare.",
            "سؤال أو فكرة؟ راسلنا، ونرد خلال يومي عمل.");
        Add("About.Contact.MailCta",
            "Mail ons",
            "Mail us",
            "Napisz do nas",
            "Scrie-ne",
            "راسلنا");
        Add("About.Contact.PrivacyNote",
            "Gaat je vraag over je privacy of je gegevens? Lees eerst het privacybeleid.",
            "Is your question about your privacy or your data? Read the privacy statement first.",
            "Pytanie dotyczy prywatności lub danych? Przeczytaj najpierw politykę prywatności.",
            "Întrebarea ta e despre confidențialitate sau datele tale? Citește mai întâi declarația de confidențialitate.",
            "هل سؤالك عن خصوصيتك أو بياناتك؟ اقرأ بيان الخصوصية أولاً.");
    }

    /// <summary>Static <c>/partner</c> (public-pages 09): B1 copy, prices excl. btw (D5), no jargon.</summary>
    private static void MergePartnerPage(AddKey Add)
    {
        Add("PartnerPage.Seo.Title",
            "Personeel vinden dichtbij",
            "Find staff nearby",
            "Znajdź pracowników blisko",
            "Găsește personal aproape",
            "اعثر على موظفين قريبين");
        Add("PartnerPage.Seo.Description",
            "Laat je vacature zien aan mensen in de buurt. Je betaalt alleen als je een vacature plaatst.",
            "Show your vacancy to people nearby. You only pay when you publish a vacancy.",
            "Pokaż swoją ofertę ludziom z okolicy. Płacisz tylko, gdy publikujesz ofertę.",
            "Arată jobul tău oamenilor din apropiere. Plătești doar când publici un job.",
            "اعرض وظيفتك على أشخاص قريبين. تدفع فقط عند نشر وظيفة.");
        Add("PartnerPage.Eyebrow",
            "Voor werkgevers",
            "For employers",
            "Dla pracodawców",
            "Pentru angajatori",
            "لأصحاب العمل");
        Add("PartnerPage.Title",
            "Vind personeel dichtbij.",
            "Find staff nearby.",
            "Znajdź pracowników blisko.",
            "Găsește personal aproape.",
            "اعثر على موظفين قريبين.");
        Add("PartnerPage.Lead",
            "Lobsy laat je vacature zien aan mensen in de buurt, op fiets-, OV- of autotijd. Je betaalt alleen als je een vacature plaatst.",
            "Lobsy shows your vacancy to people nearby, by bike, public transport or car time. You only pay when you publish a vacancy.",
            "Lobsy pokazuje Twoją ofertę ludziom z okolicy, według czasu na rowerze, komunikacją lub samochodem. Płacisz tylko, gdy publikujesz ofertę.",
            "Lobsy arată jobul tău oamenilor din apropiere, după timpul cu bicicleta, transportul public sau mașina. Plătești doar când publici un job.",
            "يعرض Lobsy وظيفتك على أشخاص قريبين، بحسب زمن الدراجة أو المواصلات أو السيارة. تدفع فقط عند نشر وظيفة.");
        Add("PartnerPage.RegisterCta",
            "Bedrijf registreren",
            "Register your company",
            "Zarejestruj firmę",
            "Înregistrează firma",
            "سجّل شركتك");
        Add("PartnerPage.RatesCta",
            "Bekijk de tarieven",
            "See the rates",
            "Zobacz cennik",
            "Vezi tarifele",
            "اطّلع على الأسعار");
        Add("PartnerPage.MascotAlt",
            "De Lobsy-kreeft zwaait naar je",
            "The Lobsy lobster waves at you",
            "Homar Lobsy macha do Ciebie",
            "Homarul Lobsy îți face semn",
            "سرطان Lobsy يلوّح لك");

        Add("PartnerPage.Usps.Label",
            "Waarom Lobsy",
            "Why Lobsy",
            "Dlaczego Lobsy",
            "De ce Lobsy",
            "لماذا Lobsy");
        Add("PartnerPage.Usp.Nearby.Title",
            "Kandidaten dichtbij",
            "Candidates nearby",
            "Kandydaci w okolicy",
            "Candidați din apropiere",
            "مرشّحون قريبون");
        Add("PartnerPage.Usp.Nearby.Body",
            "We laten je vacature zien aan mensen die er snel kunnen zijn.",
            "We show your vacancy to people who can get there quickly.",
            "Pokazujemy Twoją ofertę osobom, które mogą tam szybko dotrzeć.",
            "Arătăm jobul tău celor care pot ajunge repede acolo.",
            "نعرض وظيفتك على مَن يمكنه الوصول بسرعة.");
        Add("PartnerPage.Usp.PerVacancy.Title",
            "Betaal per vacature",
            "Pay per vacancy",
            "Płać za ofertę",
            "Plătești per job",
            "ادفع لكل وظيفة");
        Add("PartnerPage.Usp.PerVacancy.Body",
            "Geen abonnement. Je koopt tokens en gebruikt ze als je wilt.",
            "No subscription. You buy tokens and use them when you want.",
            "Bez abonamentu. Kupujesz tokeny i używasz ich, kiedy chcesz.",
            "Fără abonament. Cumperi tokenuri și le folosești când vrei.",
            "دون اشتراك. تشتري رموزاً وتستخدمها وقتما تريد.");
        Add("PartnerPage.Usp.FreeHighlight.Title",
            "Gratis start-highlight",
            "Free starting highlight",
            "Darmowe wyróżnienie na start",
            "Evidențiere gratuită la start",
            "تمييز مجاني في البداية");
        Add("PartnerPage.Usp.FreeHighlight.Body",
            "Met een salescode is je eerste highlight gratis (ter waarde van {0} tokens).",
            "With a sales code your first highlight is free (worth {0} tokens).",
            "Z kodem sprzedażowym pierwsze wyróżnienie jest darmowe (warte {0} tokenów).",
            "Cu un cod de vânzări, prima evidențiere este gratuită (în valoare de {0} tokenuri).",
            "مع رمز المبيعات يكون التمييز الأول مجانياً (بقيمة {0} رمزاً).");

        Add("PartnerPage.Rates.Title",
            "Tarieven",
            "Rates",
            "Cennik",
            "Tarife",
            "الأسعار");
        Add("PartnerPage.Rates.TokenValue",
            "1 token = {0} excl. btw",
            "1 token = {0} excl. VAT",
            "1 token = {0} bez VAT",
            "1 token = {0} fără TVA",
            "رمز واحد = {0} بدون ضريبة");
        Add("PartnerPage.Rates.InclVat",
            "{0} incl. btw",
            "{0} incl. VAT",
            "{0} z VAT",
            "{0} cu TVA",
            "{0} مع الضريبة");
        Add("PartnerPage.Rates.Kind",
            "Soort",
            "Kind",
            "Rodzaj",
            "Tip",
            "النوع");
        Add("PartnerPage.Rates.Tokens",
            "Tokens",
            "Tokens",
            "Tokeny",
            "Tokenuri",
            "الرموز");
        Add("PartnerPage.Rates.Price",
            "Prijs",
            "Price",
            "Cena",
            "Preț",
            "السعر");
        Add("PartnerPage.Rates.Free",
            "Gratis",
            "Free",
            "Gratis",
            "Gratuit",
            "مجاني");
        Add("PartnerPage.Rates.NoTokens",
            "—",
            "—",
            "—",
            "—",
            "—");
        Add("PartnerPage.Rates.Highlight",
            "Highlight ({0} dagen)",
            "Highlight ({0} days)",
            "Wyróżnienie ({0} dni)",
            "Evidențiere ({0} zile)",
            "تمييز ({0} أيام)");
        Add("PartnerPage.Rates.ExclVatNote",
            "Alle prijzen zijn exclusief btw.",
            "All prices are excluding VAT.",
            "Wszystkie ceny są bez VAT.",
            "Toate prețurile sunt fără TVA.",
            "جميع الأسعار بدون ضريبة القيمة المضافة.");
        Add("PartnerPage.Rates.PackagesNote",
            "Pakketten met korting vind je na het aanmelden.",
            "You find discounted packages after you register.",
            "Pakiety ze zniżką znajdziesz po rejestracji.",
            "Pachetele cu discount le găsești după înregistrare.",
            "تجد الحزم المخفّضة بعد التسجيل.");
        Add("PartnerPage.RatesUnavailable",
            "De tarieven laden nu niet. Probeer het later nog eens.",
            "The rates are not loading right now. Please try again later.",
            "Cennik nie ładuje się teraz. Spróbuj później.",
            "Tarifele nu se încarcă acum. Încearcă mai târziu.",
            "الأسعار لا تُحمَّل الآن. حاول لاحقاً.");

        Add("PartnerPage.Share.Title",
            "Deel met een collega",
            "Share with a colleague",
            "Podziel się z kolegą",
            "Trimite unui coleg",
            "شارِكها مع زميل");
        Add("PartnerPage.Share.Body",
            "Stuur deze pagina door. Je salescode gaat automatisch mee.",
            "Forward this page. Your sales code comes along automatically.",
            "Przekaż tę stronę. Twój kod sprzedażowy idzie automatycznie.",
            "Trimite mai departe această pagină. Codul tău de vânzări merge automat.",
            "أعد إرسال هذه الصفحة. ينتقل رمز المبيعات تلقائياً.");
        Add("PartnerPage.Share.WhatsApp",
            "WhatsApp",
            "WhatsApp",
            "WhatsApp",
            "WhatsApp",
            "واتساب");
        Add("PartnerPage.Share.Mail",
            "Mail",
            "Mail",
            "E-mail",
            "E-mail",
            "بريد");
        Add("PartnerPage.Share.Flyer",
            "Flyer (pdf)",
            "Flyer (pdf)",
            "Ulotka (pdf)",
            "Flyer (pdf)",
            "منشور (pdf)");
        Add("PartnerPage.Share.Subject",
            "Lobsy: personeel vinden dichtbij",
            "Lobsy: find staff nearby",
            "Lobsy: znajdź pracowników blisko",
            "Lobsy: găsește personal aproape",
            "Lobsy: اعثر على موظفين قريبين");
        Add("PartnerPage.Share.Body.Mail",
            "Hoi,\n\nMet Lobsy laat je je vacature zien aan mensen in de buurt. Je betaalt alleen als je een vacature plaatst.\n\n{0}\n",
            "Hi,\n\nWith Lobsy you show your vacancy to people nearby. You only pay when you publish a vacancy.\n\n{0}\n",
            "Cześć,\n\nZ Lobsy pokazujesz swoją ofertę ludziom z okolicy. Płacisz tylko, gdy publikujesz ofertę.\n\n{0}\n",
            "Salut,\n\nCu Lobsy arăți jobul tău oamenilor din apropiere. Plătești doar când publici un job.\n\n{0}\n",
            "مرحباً،\n\nمع Lobsy تعرض وظيفتك على أشخاص قريبين. تدفع فقط عند نشر وظيفة.\n\n{0}\n");
        Add("PartnerPage.Share.Text",
            "Met Lobsy laat je je vacature zien aan mensen in de buurt: {0}",
            "With Lobsy you show your vacancy to people nearby: {0}",
            "Z Lobsy pokazujesz swoją ofertę ludziom z okolicy: {0}",
            "Cu Lobsy arăți jobul tău oamenilor din apropiere: {0}",
            "مع Lobsy تعرض وظيفتك على أشخاص قريبين: {0}");
        Add("PartnerPage.SalesCode",
            "Salescode",
            "Sales code",
            "Kod sprzedażowy",
            "Cod de vânzări",
            "رمز المبيعات");
    }

    /// <summary><c>/{kvk}</c> in the public layout (public-pages 09, D4).</summary>
    private static void MergeCompanyPage(AddKey Add)
    {
        Add("CompanyPage.Breadcrumb.Map",
            "Banenkaart",
            "Job map",
            "Mapa ofert",
            "Harta joburilor",
            "خريطة الوظائف");
        Add("CompanyPage.Breadcrumb.Label",
            "Waar je bent",
            "Where you are",
            "Gdzie jesteś",
            "Unde te afli",
            "موضعك");
        Add("CompanyPage.KvkVerified",
            "KvK gecontroleerd",
            "Chamber of Commerce checked",
            "Sprawdzone w rejestrze",
            "Verificat la registrul comerțului",
            "تم التحقق من السجل التجاري");
        Add("CompanyPage.VacancyCount.One",
            "1 vacature",
            "1 vacancy",
            "1 oferta",
            "1 job",
            "وظيفة واحدة");
        Add("CompanyPage.VacancyCount.Many",
            "{0} vacatures",
            "{0} vacancies",
            "{0} ofert",
            "{0} joburi",
            "{0} وظائف");
        Add("CompanyPage.Branches.Label",
            "Vestigingen",
            "Locations",
            "Placówki",
            "Filiale",
            "الفروع");
        Add("CompanyPage.Branches.All",
            "Alle vestigingen ({0})",
            "All locations ({0})",
            "Wszystkie placówki ({0})",
            "Toate filialele ({0})",
            "كل الفروع ({0})");
        Add("CompanyPage.Vacancies.Label",
            "Vacatures van dit bedrijf",
            "Vacancies of this company",
            "Oferty tej firmy",
            "Joburile acestei firme",
            "وظائف هذه الشركة");
        Add("CompanyPage.Map.Label",
            "Kaart met de vacatures",
            "Map with the vacancies",
            "Mapa z ofertami",
            "Hartă cu joburile",
            "خريطة الوظائف");
    }
}
