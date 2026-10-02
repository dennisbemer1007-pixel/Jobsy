namespace Jobsy.Web.Localization;

/// <summary>
/// Legal document chrome and the "In het kort" summaries (public-pages 02–05).
/// The full legal text itself stays Dutch razor markup (D3); only chrome and summaries live here.
/// nl and en are final; pl / ro / ar are B1 drafts listed in <c>docs/i18n/public-pages-review.md</c>.
/// </summary>
public static class UiStringsLegal
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

        // —— Document chrome ——
        Add("Legal.OfficialNote",
            "De Nederlandse tekst is de officiële versie. De blokken ‘In het kort’ staan in jouw taal (nl, en, pl, ro, ar).",
            "The Dutch text is the official version. The ‘In short’ blocks are in your language (nl, en, pl, ro, ar).",
            "Tekst niderlandzki jest wersją oficjalną. Bloki „W skrócie” są w twoim języku (nl, en, pl, ro, ar).",
            "Textul în neerlandeză este versiunea oficială. Casetele „Pe scurt” sunt în limba ta (nl, en, pl, ro, ar).",
            "النص الهولندي هو النسخة الرسمية. فقرات «باختصار» بلغتك (nl, en, pl, ro, ar).");
        Add("Legal.InShort",
            "In het kort",
            "In short",
            "W skrócie",
            "Pe scurt",
            "باختصار");
        Add("Legal.DutchText",
            "Nederlandse tekst (officieel):",
            "Dutch text (official):",
            "Tekst niderlandzki (oficjalny):",
            "Text în neerlandeză (oficial):",
            "النص الهولندي (الرسمي):");
        Add("Legal.TocTitle",
            "Op deze pagina",
            "On this page",
            "Na tej stronie",
            "Pe această pagină",
            "في هذه الصفحة");
        Add("Legal.TocMobile",
            "Inhoud ({0} onderdelen)",
            "Contents ({0} sections)",
            "Spis treści ({0} części)",
            "Cuprins ({0} secțiuni)",
            "المحتوى ({0} أقسام)");
        Add("Legal.Print",
            "Afdrukken of opslaan als pdf",
            "Print or save as pdf",
            "Wydrukuj lub zapisz jako pdf",
            "Printează sau salvează ca pdf",
            "اطبع أو احفظ كملف pdf");
        Add("Legal.VersionLine",
            "Versie {0} · geldig vanaf {1}",
            "Version {0} · in effect from {1}",
            "Wersja {0} · obowiązuje od {1}",
            "Versiunea {0} · valabilă de la {1}",
            "الإصدار {0} · سارٍ من {1}");
        Add("Legal.SummaryLanguages",
            "Samenvatting in 5 talen",
            "Summary in 5 languages",
            "Podsumowanie w 5 językach",
            "Rezumat în 5 limbi",
            "ملخص بخمس لغات");
        Add("Legal.Changes",
            "Wat is er veranderd?",
            "What has changed?",
            "Co się zmieniło?",
            "Ce s-a schimbat?",
            "ما الذي تغيّر؟");
        Add("Legal.MyData",
            "Mijn gegevens",
            "My data",
            "Moje dane",
            "Datele mele",
            "بياناتي");

        // —— Document hero copy ——
        Add("Legal.Eyebrow.Privacy",
            "Privacy",
            "Privacy",
            "Prywatność",
            "Confidențialitate",
            "الخصوصية");
        Add("Legal.Eyebrow.Terms",
            "Voorwaarden",
            "Terms",
            "Warunki",
            "Condiții",
            "الشروط");
        Add("Privacy.Doc.Lead",
            "Welke gegevens we gebruiken, waarom, en wat jouw rechten zijn. In gewone taal.",
            "Which data we use, why, and what your rights are. In plain language.",
            "Jakich danych używamy, dlaczego i jakie masz prawa. Prostym językiem.",
            "Ce date folosim, de ce și care sunt drepturile tale. În limbaj simplu.",
            "ما البيانات التي نستخدمها، ولماذا، وما هي حقوقك. بلغة بسيطة.");
        Add("Terms.Employer.Lead",
            "De afspraken tussen jouw bedrijf en Lobsy.",
            "The agreements between your company and Lobsy.",
            "Ustalenia między twoją firmą a Lobsy.",
            "Acordurile dintre firma ta și Lobsy.",
            "الاتفاقات بين شركتك و Lobsy.");
        Add("Terms.Candidate.Lead",
            "De afspraken als je Lobsy gebruikt om werk te vinden.",
            "The agreements when you use Lobsy to find work.",
            "Ustalenia, gdy używasz Lobsy do szukania pracy.",
            "Acordurile când folosești Lobsy ca să găsești de lucru.",
            "الاتفاقات عند استخدام Lobsy للبحث عن عمل.");

        // —— Identity card (02.6) ——
        Add("Legal.IdentityCard.Name",
            "Naam",
            "Name",
            "Nazwa",
            "Nume",
            "الاسم");
        Add("Legal.IdentityCard.Address",
            "Adres",
            "Address",
            "Adres",
            "Adresă",
            "العنوان");
        Add("Legal.IdentityCard.Kvk",
            "KvK",
            "KvK (Dutch trade register)",
            "KvK (niderlandzki rejestr firm)",
            "KvK (registrul comerțului din Țările de Jos)",
            "رقم السجل التجاري (KvK)");
        Add("Legal.IdentityCard.Vat",
            "Btw-nummer",
            "VAT number",
            "Numer VAT",
            "Număr TVA",
            "رقم ضريبة القيمة المضافة");
        Add("Legal.IdentityCard.PrivacyQuestions",
            "Privacyvragen",
            "Privacy questions",
            "Pytania o prywatność",
            "Întrebări despre confidențialitate",
            "أسئلة الخصوصية");
        Add("Legal.IdentityCard.Contact",
            "Contact",
            "Contact",
            "Kontakt",
            "Contact",
            "جهة الاتصال");

        // —— Processor table (02.7) ——
        Add("Legal.Processors.Caption",
            "Partijen die voor ons werken (verwerkers):",
            "Parties that work for us (processors):",
            "Podmioty, które dla nas pracują (podmioty przetwarzające):",
            "Părțile care lucrează pentru noi (operatori împuterniciți):",
            "الجهات التي تعمل لصالحنا (المعالِجون):");
        Add("Legal.Processors.Party",
            "Partij",
            "Party",
            "Podmiot",
            "Parte",
            "الجهة");
        Add("Legal.Processors.Where",
            "Waar",
            "Where",
            "Gdzie",
            "Unde",
            "المكان");
        Add("Legal.Processors.Purpose",
            "Waarvoor",
            "What for",
            "Po co",
            "Pentru ce",
            "الغرض");
        Add("Legal.Processors.Data",
            "Welke gegevens",
            "Which data",
            "Jakie dane",
            "Ce date",
            "ما البيانات");
        Add("Legal.Processors.Planned",
            "vanaf de start van deze functie",
            "from the start of this feature",
            "od uruchomienia tej funkcji",
            "de la lansarea acestei funcții",
            "من بداية هذه الميزة");
        Add("Legal.Processors.TransferNote",
            "Buiten de EU gebruiken we het EU-VS Data Privacy Framework of de standaardcontractbepalingen van de EU.",
            "Outside the EU we use the EU-US Data Privacy Framework or the EU standard contractual clauses.",
            "Poza UE korzystamy z ram EU-US Data Privacy Framework albo standardowych klauzul umownych UE.",
            "În afara UE folosim EU-US Data Privacy Framework sau clauzele contractuale standard ale UE.",
            "خارج الاتحاد الأوروبي نستخدم إطار خصوصية البيانات بين الاتحاد الأوروبي والولايات المتحدة أو الشروط التعاقدية النموذجية للاتحاد.");

        // —— Retention table (02.7) ——
        Add("Legal.Retention.What",
            "Wat",
            "What",
            "Co",
            "Ce",
            "ما");
        Add("Legal.Retention.HowLong",
            "Hoe lang",
            "How long",
            "Jak długo",
            "Cât timp",
            "المدة");
        Add("Legal.Retention.Caption",
            "De termijnen komen rechtstreeks uit de code, zodat deze pagina en het systeem gelijk blijven.",
            "The periods come straight from the code, so this page and the system stay in sync.",
            "Terminy pochodzą wprost z kodu, więc ta strona i system są zgodne.",
            "Termenele vin direct din cod, așa că pagina și sistemul rămân identice.",
            "المدد تأتي مباشرة من الكود، لذلك تبقى الصفحة والنظام متطابقين.");
        Add("Legal.Retention.UnverifiedApplication",
            "Onbevestigde sollicitaties",
            "Unconfirmed applications",
            "Niepotwierdzone zgłoszenia",
            "Candidaturi neconfirmate",
            "طلبات غير مؤكدة");
        Add("Legal.Retention.UnconfirmedRegistration",
            "Registratie zonder bevestigde code",
            "Registration without a confirmed code",
            "Rejestracja bez potwierdzonego kodu",
            "Înregistrare fără cod confirmat",
            "تسجيل بدون رمز مؤكد");
        Add("Legal.Retention.CancelledRegistration",
            "Geannuleerde registraties",
            "Cancelled registrations",
            "Anulowane rejestracje",
            "Înregistrări anulate",
            "تسجيلات ملغاة");
        Add("Legal.Retention.ActionLinks",
            "Eenmalige links en actiecodes",
            "One-time links and action codes",
            "Jednorazowe linki i kody akcji",
            "Linkuri unice și coduri de acțiune",
            "روابط لمرة واحدة ورموز الإجراءات");
        Add("Legal.Retention.PlatformLogs",
            "Platformlogs en feedback-screenshots",
            "Platform logs and feedback screenshots",
            "Logi platformy i zrzuty ekranu z opinii",
            "Jurnalele platformei și capturile din feedback",
            "سجلات المنصة ولقطات الشاشة في الملاحظات");
        Add("Legal.Retention.Engagement",
            "Klik- en bezoekstatistieken en meldingen",
            "Click and visit statistics and notifications",
            "Statystyki kliknięć i wizyt oraz powiadomienia",
            "Statistici de clicuri și vizite și notificări",
            "إحصاءات النقر والزيارات والإشعارات");
        Add("Legal.Retention.AccessLog",
            "Log van wie jouw gegevens inzag",
            "Log of who viewed your data",
            "Rejestr osób, które zobaczyły twoje dane",
            "Jurnalul celor care ți-au văzut datele",
            "سجل من اطّلع على بياناتك");
        Add("Legal.Retention.SalesLinkClicks",
            "Dagtotalen van sales-linkklikken",
            "Daily totals of sales link clicks",
            "Dzienne sumy kliknięć linków sprzedażowych",
            "Totaluri zilnice ale clicurilor pe linkuri de vânzări",
            "الإجماليات اليومية لنقرات روابط المبيعات");
        Add("Legal.Retention.SalesManagerApplication",
            "Aanmeldingen als salesmanager",
            "Sales manager applications",
            "Zgłoszenia na opiekuna sprzedaży",
            "Candidaturi de manager de vânzări",
            "طلبات العمل كمدير مبيعات");
        Add("Legal.Retention.Invoices",
            "Facturen, commissies en uitbetalingen",
            "Invoices, commissions and payouts",
            "Faktury, prowizje i wypłaty",
            "Facturi, comisioane și plăți",
            "الفواتير والعمولات والمدفوعات");
        Add("Legal.Retention.AdminAudit",
            "Auditlog van beheeracties",
            "Audit log of admin actions",
            "Dziennik audytu działań administratora",
            "Jurnal de audit al acțiunilor de administrare",
            "سجل تدقيق إجراءات الإدارة");
        Add("Legal.Retention.Account",
            "Account, tests en uitslagen",
            "Account, tests and results",
            "Konto, testy i wyniki",
            "Cont, teste și rezultate",
            "الحساب والاختبارات والنتائج");

        // —— Change log (D16) ——
        Add("Legal.Change.Privacy.2026-10",
            "Nieuwe opzet: een samenvatting per onderdeel in 5 talen en één versiedatum uit de code.",
            "New structure: a summary per section in 5 languages and one version date from the code.",
            "Nowy układ: podsumowanie każdej części w 5 językach i jedna data wersji z kodu.",
            "Structură nouă: un rezumat pentru fiecare secțiune în 5 limbi și o singură dată de versiune din cod.",
            "هيكل جديد: ملخص لكل قسم بخمس لغات وتاريخ إصدار واحد من الكود.");
        Add("Legal.Change.Privacy.2026-09",
            "Tests, talentpool en AI-functies toegevoegd aan de verklaring.",
            "Tests, talent pool and AI features added to the statement.",
            "Do dokumentu dodano testy, bazę talentów i funkcje AI.",
            "Au fost adăugate testele, rezerva de talente și funcțiile AI.",
            "أُضيفت الاختبارات ومجموعة المواهب وميزات الذكاء الاصطناعي إلى البيان.");
        Add("Legal.Change.Terms.2026-10",
            "Nieuwe opzet: een samenvatting per onderdeel in 5 talen en één versiedatum uit de code.",
            "New structure: a summary per section in 5 languages and one version date from the code.",
            "Nowy układ: podsumowanie każdej części w 5 językach i jedna data wersji z kodu.",
            "Structură nouă: un rezumat pentru fiecare secțiune în 5 limbi și o singură dată de versiune din cod.",
            "هيكل جديد: ملخص لكل قسم بخمس لغات وتاريخ إصدار واحد من الكود.");
        Add("Legal.Change.Terms.2026-08",
            "Eerste versie met tokens, matching en jeugdige arbeid.",
            "First version covering tokens, matching and young workers.",
            "Pierwsza wersja z tokenami, dopasowaniem i pracą młodzieży.",
            "Prima versiune cu tokenuri, potrivire și munca minorilor.",
            "النسخة الأولى التي تشمل الرموز والمطابقة وعمل الشباب.");

        MergePrivacySections(Add);
        MergeTermsSections(Add);
    }

    private delegate void AddString(string key, string nl, string en, string pl, string ro, string ar);

    /// <summary>Privacy section titles and summaries (03 writes the Dutch bodies).</summary>
    private static void MergePrivacySections(AddString Add)
    {
        Add("Privacy.Sec.verantwoordelijk.Title",
            "Wie is verantwoordelijk?",
            "Who is responsible?",
            "Kto jest odpowiedzialny?",
            "Cine este responsabil?",
            "من المسؤول؟");
        Add("Privacy.Sec.verantwoordelijk.Summary",
            "Lobsy is verantwoordelijk voor je gegevens. Vragen? Mail ons.",
            "Lobsy is responsible for your data. Questions? Send us an e-mail.",
            "Lobsy odpowiada za twoje dane. Masz pytania? Napisz do nas.",
            "Lobsy răspunde pentru datele tale. Ai întrebări? Scrie-ne.",
            "‏Lobsy مسؤول عن بياناتك. لديك سؤال؟ راسلنا.");
        Add("Privacy.Sec.gegevens.Title",
            "Welke gegevens?",
            "Which data?",
            "Jakie dane?",
            "Ce date?",
            "ما البيانات؟");
        Add("Privacy.Sec.grondslag.Title",
            "Waarom mogen we dat?",
            "Why are we allowed to?",
            "Dlaczego nam wolno?",
            "De ce avem dreptul?",
            "لماذا يُسمح لنا بذلك؟");
        Add("Privacy.Sec.delen.Title",
            "Met wie delen we het?",
            "Who do we share it with?",
            "Z kim je udostępniamy?",
            "Cu cine le partajăm?",
            "مع من نشارك بياناتك؟");
        Add("Privacy.Sec.delen.Summary",
            "We delen alleen wat nodig is. Werkgevers zien je naam en telefoon pas als jij dat goed vindt. We verkopen niets.",
            "We only share what is needed. Employers see your name and phone number only when you agree. We sell nothing.",
            "Udostępniamy tylko to, co konieczne. Pracodawca zobaczy twoje imię i telefon dopiero, gdy się zgodzisz. Niczego nie sprzedajemy.",
            "Partajăm doar ce este necesar. Angajatorii văd numele și telefonul tău doar când ești de acord. Nu vindem nimic.",
            "نشارك فقط ما هو ضروري. لا يرى صاحب العمل اسمك أو هاتفك إلا بموافقتك. لا نبيع أي شيء.");
        Add("Privacy.Sec.locatie.Title",
            "Locatie, reistijd en matching",
            "Location, travel time and matching",
            "Lokalizacja, czas podróży i dopasowanie",
            "Locație, timp de călătorie și potrivire",
            "الموقع ووقت التنقل والمطابقة");
        Add("Privacy.Sec.jeugdige-arbeid.Title",
            "Jeugdige arbeid en taakvinkjes",
            "Young workers and task checks",
            "Praca młodzieży i znaczniki zadań",
            "Munca minorilor și bifele de sarcini",
            "عمل الشباب وبنود المهام");
        Add("Privacy.Sec.tests.Title",
            "Tests, diepte-analyse en talentpool",
            "Tests, in-depth analysis and talent pool",
            "Testy, analiza pogłębiona i baza talentów",
            "Teste, analiză detaliată și rezerva de talente",
            "الاختبارات والتحليل المتقدم ومجموعة المواهب");
        Add("Privacy.Sec.bewaren.Title",
            "Hoe lang bewaren we het?",
            "How long do we keep it?",
            "Jak długo to przechowujemy?",
            "Cât timp le păstrăm?",
            "كم مدة الاحتفاظ بها؟");
        Add("Privacy.Sec.bewaren.Summary",
            "We bewaren gegevens niet langer dan nodig. Je account blijft tot je hem verwijdert.",
            "We keep data no longer than needed. Your account stays until you delete it.",
            "Nie przechowujemy danych dłużej niż to potrzebne. Twoje konto zostaje, dopóki go nie usuniesz.",
            "Nu păstrăm datele mai mult decât este nevoie. Contul tău rămâne până îl ștergi.",
            "لا نحتفظ بالبيانات أكثر من اللازم. يبقى حسابك حتى تحذفه.");
        Add("Privacy.Sec.salesmanager.Title",
            "Aanmelden via een salesmanager",
            "Signing up through a sales manager",
            "Rejestracja przez opiekuna sprzedaży",
            "Înscriere prin un manager de vânzări",
            "التسجيل عبر مدير مبيعات");
        Add("Privacy.Sec.cookies.Title",
            "Cookies",
            "Cookies",
            "Pliki cookie",
            "Cookie-uri",
            "ملفات تعريف الارتباط");
        Add("Privacy.Sec.ai.Title",
            "AI en matching",
            "AI and matching",
            "AI i dopasowanie",
            "AI și potrivire",
            "الذكاء الاصطناعي والمطابقة");
        Add("Privacy.Sec.beveiliging.Title",
            "Beveiliging",
            "Security",
            "Bezpieczeństwo",
            "Securitate",
            "الأمان");
        Add("Privacy.Sec.rechten.Title",
            "Jouw rechten",
            "Your rights",
            "Twoje prawa",
            "Drepturile tale",
            "حقوقك");
        Add("Privacy.Sec.rechten.Summary",
            "Je kunt je gegevens bekijken, downloaden en verwijderen. Je kunt ook een klacht indienen bij de Autoriteit Persoonsgegevens.",
            "You can view, download and delete your data. You can also complain to the Dutch data protection authority.",
            "Możesz zobaczyć, pobrać i usunąć swoje dane. Możesz też złożyć skargę do niderlandzkiego organu ochrony danych.",
            "Poți vedea, descărca și șterge datele tale. Poți depune și o plângere la autoritatea olandeză de protecție a datelor.",
            "يمكنك رؤية بياناتك وتنزيلها وحذفها. يمكنك أيضًا تقديم شكوى إلى هيئة حماية البيانات الهولندية.");
        Add("Privacy.Sec.jonger.Title",
            "Jonger dan 16",
            "Under 16",
            "Młodsi niż 16 lat",
            "Sub 16 ani",
            "أقل من 16 سنة");
        Add("Privacy.Sec.wijzigingen.Title",
            "Wijzigingen",
            "Changes",
            "Zmiany",
            "Modificări",
            "التغييرات");
    }

    /// <summary>Terms section titles and summaries, shared by both terms documents (04 writes the bodies).</summary>
    private static void MergeTermsSections(AddString Add)
    {
        Add("Terms.Sec.wie-is-lobsy.Title",
            "Wie is Lobsy?",
            "Who is Lobsy?",
            "Kim jest Lobsy?",
            "Cine este Lobsy?",
            "من هو Lobsy؟");
        Add("Terms.Sec.wie-is-lobsy.Summary",
            "Dit zijn onze gegevens. Zo weet je met wie je afspraken maakt.",
            "These are our details, so you know who you are making agreements with.",
            "To nasze dane, żebyś wiedział, z kim się umawiasz.",
            "Acestea sunt datele noastre, ca să știi cu cine faci acordul.",
            "هذه بياناتنا، لتعرف مع من تتعامل.");
        Add("Terms.Sec.toepasselijkheid.Title",
            "Voor wie gelden ze?",
            "Who do they apply to?",
            "Dla kogo obowiązują?",
            "Pentru cine se aplică?",
            "على من تنطبق؟");
        Add("Terms.Sec.voor-wie.Title",
            "Voor wie gelden ze?",
            "Who do they apply to?",
            "Dla kogo obowiązują?",
            "Pentru cine se aplică?",
            "على من تنطبق؟");
        Add("Terms.Sec.voor-wie.Summary",
            "Voor iedereen die Lobsy gebruikt als werkzoekende, vanaf 13 jaar. Jonger dan 16? Dan vragen we eerst toestemming aan je ouder.",
            "For everyone who uses Lobsy to look for work, from age 13. Under 16? Then we ask your parent for permission first.",
            "Dla każdego, kto szuka pracy przez Lobsy, od 13 lat. Masz mniej niż 16 lat? Najpierw pytamy rodzica o zgodę.",
            "Pentru oricine caută de lucru pe Lobsy, de la 13 ani. Ai sub 16 ani? Atunci cerem mai întâi acordul părintelui.",
            "لكل من يستخدم Lobsy للبحث عن عمل، من عمر 13 سنة. أقل من 16؟ نطلب موافقة والدك أولاً.");
        Add("Terms.Sec.dienst.Title",
            "Wat doet Lobsy?",
            "What does Lobsy do?",
            "Co robi Lobsy?",
            "Ce face Lobsy?",
            "ماذا يفعل Lobsy؟");
        Add("Terms.Sec.account-kvk.Title",
            "Account en KvK",
            "Account and KvK",
            "Konto i KvK",
            "Cont și KvK",
            "الحساب والسجل التجاري");
        Add("Terms.Sec.account.Title",
            "Jouw account en gegevens",
            "Your account and data",
            "Twoje konto i dane",
            "Contul și datele tale",
            "حسابك وبياناتك");
        Add("Terms.Sec.tokens.Title",
            "Tokens",
            "Tokens",
            "Tokeny",
            "Tokenuri",
            "الرموز");
        Add("Terms.Sec.betalen-btw.Title",
            "Betalen en btw",
            "Payment and VAT",
            "Płatności i VAT",
            "Plată și TVA",
            "الدفع وضريبة القيمة المضافة");
        Add("Terms.Sec.betalen-btw.Summary",
            "Je betaalt vooraf met tokens via Mollie. Alle prijzen zijn exclusief btw, tenzij er iets anders staat.",
            "You pay up front with tokens through Mollie. All prices exclude VAT unless stated otherwise.",
            "Płacisz z góry tokenami przez Mollie. Wszystkie ceny są bez VAT, chyba że napisano inaczej.",
            "Plătești în avans cu tokenuri prin Mollie. Toate prețurile sunt fără TVA, dacă nu se spune altfel.",
            "تدفع مقدماً بالرموز عبر Mollie. جميع الأسعار بدون ضريبة القيمة المضافة إلا إذا ذُكر غير ذلك.");
        Add("Terms.Sec.bedenktijd.Title",
            "Betaalde extra’s en bedenktijd",
            "Paid extras and your right to withdraw",
            "Płatne dodatki i prawo odstąpienia",
            "Extra plătite și dreptul de retragere",
            "الإضافات المدفوعة وحق التراجع");
        Add("Terms.Sec.bedenktijd.Summary",
            "Lobsy is gratis. Koop je iets extra, zoals de uitgebreide analyse? Dan zie je vooraf de prijs en wat je krijgt.",
            "Lobsy is free. Buying something extra, like the in-depth analysis? Then you see the price and what you get up front.",
            "Lobsy jest darmowe. Kupujesz dodatek, na przykład analizę pogłębioną? Cenę i zakres widzisz z góry.",
            "Lobsy este gratuit. Cumperi un extra, cum ar fi analiza detaliată? Vezi dinainte prețul și ce primești.",
            "‏Lobsy مجاني. تشتري إضافة مثل التحليل المتقدم؟ ترى السعر وما تحصل عليه مسبقاً.");
        Add("Terms.Sec.vacatures.Title",
            "Vacatures en inhoud",
            "Vacancies and content",
            "Ogłoszenia i treści",
            "Anunțuri și conținut",
            "الوظائف والمحتوى");
        Add("Terms.Sec.matching.Title",
            "Matching en jeugdige arbeid",
            "Matching and young workers",
            "Dopasowanie i praca młodzieży",
            "Potrivirea și munca minorilor",
            "المطابقة وعمل الشباب");
        Add("Terms.Sec.matchscores.Title",
            "Matchpercentages",
            "Match percentages",
            "Procenty dopasowania",
            "Procentele de potrivire",
            "نسب المطابقة");
        Add("Terms.Sec.solliciteren.Title",
            "Solliciteren",
            "Applying for a job",
            "Składanie zgłoszeń",
            "Aplicarea la un job",
            "التقديم على وظيفة");
        Add("Terms.Sec.ai.Title",
            "Chatbot en AI",
            "Chatbot and AI",
            "Czatbot i AI",
            "Chatbot și AI",
            "روبوت الدردشة والذكاء الاصطناعي");
        Add("Terms.Sec.gebruik.Title",
            "Acceptabel gebruik",
            "Acceptable use",
            "Dozwolone korzystanie",
            "Utilizare acceptabilă",
            "الاستخدام المقبول");
        Add("Terms.Sec.melden.Title",
            "Iets melden",
            "Reporting something",
            "Zgłaszanie treści",
            "Raportarea unei probleme",
            "الإبلاغ عن شيء");
        Add("Terms.Sec.melden.Summary",
            "Zie je een vacature of bedrijf dat niet klopt? Meld het. We kijken ernaar en laten je weten wat we doen.",
            "See a vacancy or company that is not right? Report it. We look into it and tell you what we do.",
            "Widzisz ogłoszenie albo firmę, które budzą wątpliwości? Zgłoś to. Sprawdzimy i powiemy, co robimy.",
            "Vezi un anunț sau o firmă care nu e în regulă? Raportează. Verificăm și îți spunem ce facem.",
            "ترى وظيفة أو شركة غير سليمة؟ أبلغنا. سننظر في الأمر ونخبرك بما سنفعله.");
        Add("Terms.Sec.kandidaatgegevens.Title",
            "Kandidaatgegevens",
            "Candidate data",
            "Dane kandydatów",
            "Datele candidaților",
            "بيانات المرشحين");
        Add("Terms.Sec.beschikbaarheid.Title",
            "Beschikbaarheid en wijzigingen",
            "Availability and changes",
            "Dostępność i zmiany",
            "Disponibilitate și modificări",
            "التوافر والتغييرات");
        Add("Terms.Sec.aansprakelijkheid.Title",
            "Aansprakelijkheid",
            "Liability",
            "Odpowiedzialność",
            "Răspundere",
            "المسؤولية");
        Add("Terms.Sec.beeindiging.Title",
            "Misbruik en beëindiging",
            "Misuse and termination",
            "Nadużycia i zakończenie",
            "Abuz și încetare",
            "سوء الاستخدام والإنهاء");
        Add("Terms.Sec.wijzigingen.Title",
            "Wijzigingen en recht",
            "Changes and applicable law",
            "Zmiany i prawo",
            "Modificări și legea aplicabilă",
            "التغييرات والقانون");
        Add("Terms.Sec.recht.Title",
            "Recht en geschillen",
            "Law and disputes",
            "Prawo i spory",
            "Legea și litigiile",
            "القانون والنزاعات");
    }
}
