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
        Add("Legal.Processors.Basis",
            "Grondslag doorgifte",
            "Transfer basis",
            "Podstawa przekazania",
            "Baza transferului",
            "أساس النقل");
        Add("Legal.Processors.Planned",
            "vanaf de start van deze functie",
            "from the start of this feature",
            "od uruchomienia tej funkcji",
            "de la lansarea acestei funcții",
            "من بداية هذه الميزة");
        Add("Legal.Transfer.Eu",
            "Binnen de EU",
            "Inside the EU",
            "W obrębie UE",
            "În interiorul UE",
            "داخل الاتحاد الأوروبي");
        Add("Legal.Transfer.Dpf",
            "EU-VS Data Privacy Framework of standaardcontractbepalingen",
            "EU-US Data Privacy Framework or standard contractual clauses",
            "EU-US Data Privacy Framework albo standardowe klauzule umowne",
            "EU-US Data Privacy Framework sau clauze contractuale standard",
            "إطار خصوصية البيانات بين الاتحاد الأوروبي والولايات المتحدة أو الشروط التعاقدية النموذجية");
        Add("Legal.Transfer.Scc",
            "Standaardcontractbepalingen van de EU",
            "EU standard contractual clauses",
            "Standardowe klauzule umowne UE",
            "Clauzele contractuale standard ale UE",
            "الشروط التعاقدية النموذجية للاتحاد الأوروبي");
        Add("Legal.Transfer.Adequacy",
            "Adequaatheidsbesluit van de EU",
            "EU adequacy decision",
            "Decyzja UE o odpowiednim stopniu ochrony",
            "Decizia UE privind caracterul adecvat",
            "قرار الكفاية الصادر عن الاتحاد الأوروبي");
        Add("Legal.Processors.TransferNote",
            "Buiten de EU gebruiken we het EU-VS Data Privacy Framework of de standaardcontractbepalingen van de EU.",
            "Outside the EU we use the EU-US Data Privacy Framework or the EU standard contractual clauses.",
            "Poza UE korzystamy z ram EU-US Data Privacy Framework albo standardowych klauzul umownych UE.",
            "În afara UE folosim EU-US Data Privacy Framework sau clauzele contractuale standard ale UE.",
            "خارج الاتحاد الأوروبي نستخدم إطار خصوصية البيانات بين الاتحاد الأوروبي والولايات المتحدة أو الشروط التعاقدية النموذجية للاتحاد.");

        MergeProcessorRows(Add);

        // —— Cookie table (03.2 §6) ——
        Add("Legal.Cookies.Name",
            "Naam",
            "Name",
            "Nazwa",
            "Nume",
            "الاسم");
        Add("Legal.Cookies.Why",
            "Waarvoor",
            "What for",
            "Po co",
            "Pentru ce",
            "الغرض");
        Add("Legal.Cookies.HowLong",
            "Hoe lang",
            "How long",
            "Jak długo",
            "Cât timp",
            "المدة");
        Add("Legal.Cookies.Needed",
            "Nodig?",
            "Needed?",
            "Konieczne?",
            "Necesar?",
            "ضروري؟");

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
            "Nieuwe opzet in gewone taal: een samenvatting per onderdeel in 5 talen, de volledige lijst met partijen die voor ons werken, een apart stuk over cookies, en de bewaartermijnen en leeftijden rechtstreeks uit de code.",
            "New structure in plain language: a summary per section in 5 languages, the full list of parties working for us, a separate cookie section, and retention periods and ages straight from the code.",
            "Nowy układ prostym językiem: podsumowanie każdej części w 5 językach, pełna lista podmiotów pracujących dla nas, osobna część o plikach cookie oraz terminy i granice wieku wprost z kodu.",
            "Structură nouă în limbaj simplu: un rezumat pentru fiecare secțiune în 5 limbi, lista completă a părților care lucrează pentru noi, o secțiune separată despre cookie-uri și termenele și vârstele direct din cod.",
            "هيكل جديد بلغة بسيطة: ملخص لكل قسم بخمس لغات، والقائمة الكاملة للجهات التي تعمل لصالحنا، وقسم منفصل لملفات تعريف الارتباط، ومدد الاحتفاظ والأعمار مباشرة من الكود.");
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

    /// <summary>Purpose and data of every <c>LegalProcessors</c> row (03.3).</summary>
    private static void MergeProcessorRows(AddString Add)
    {
        Add("Legal.Processor.render.Purpose",
            "De app en de database hosten",
            "Hosting the app and the database",
            "Hosting aplikacji i bazy danych",
            "Găzduirea aplicației și a bazei de date",
            "استضافة التطبيق وقاعدة البيانات");
        Add("Legal.Processor.render.Data",
            "Alle gegevens van je account en het platform",
            "All account and platform data",
            "Wszystkie dane konta i platformy",
            "Toate datele contului și ale platformei",
            "جميع بيانات الحساب والمنصة");
        Add("Legal.Processor.cloudflare.Purpose",
            "Beveiliging, bescherming tegen aanvallen en snelle levering",
            "Security, protection against attacks and fast delivery",
            "Bezpieczeństwo, ochrona przed atakami i szybkie dostarczanie",
            "Securitate, protecție împotriva atacurilor și livrare rapidă",
            "الأمان والحماية من الهجمات وسرعة التحميل");
        Add("Legal.Processor.cloudflare.Data",
            "Je IP-adres en gegevens over je verzoek",
            "Your IP address and request data",
            "Twój adres IP i dane żądania",
            "Adresa ta IP și datele cererii",
            "عنوان IP وبيانات الطلب");
        Add("Legal.Processor.resend.Purpose",
            "E-mail versturen",
            "Sending e-mail",
            "Wysyłanie e-maili",
            "Trimiterea de e-mailuri",
            "إرسال البريد الإلكتروني");
        Add("Legal.Processor.resend.Data",
            "Je e-mailadres, je naam en de inhoud van de mail",
            "Your e-mail address, your name and the content of the mail",
            "Twój e-mail, imię i treść wiadomości",
            "Adresa de e-mail, numele și conținutul mesajului",
            "بريدك الإلكتروني واسمك ومحتوى الرسالة");
        Add("Legal.Processor.sentry.Purpose",
            "Foutmeldingen zodat we storingen kunnen oplossen",
            "Error reports so we can fix outages",
            "Raporty błędów, żebyśmy mogli usuwać awarie",
            "Rapoarte de erori ca să putem repara defecțiunile",
            "تقارير الأخطاء لإصلاح الأعطال");
        Add("Legal.Processor.sentry.Data",
            "Technische gegevens over de fout. Geen inhoud van formulieren",
            "Technical data about the error. No form content",
            "Dane techniczne o błędzie. Bez treści formularzy",
            "Date tehnice despre eroare. Fără conținut din formulare",
            "بيانات تقنية عن الخطأ. بدون محتوى النماذج");
        Add("Legal.Processor.mollie.Purpose",
            "Betalingen",
            "Payments",
            "Płatności",
            "Plăți",
            "المدفوعات");
        Add("Legal.Processor.mollie.Data",
            "Het bedrag, de status en de betaalmethode. Lobsy krijgt geen kaartgegevens",
            "The amount, the status and the payment method. Lobsy receives no card data",
            "Kwota, status i metoda płatności. Lobsy nie dostaje danych karty",
            "Suma, starea și metoda de plată. Lobsy nu primește date de card",
            "المبلغ والحالة وطريقة الدفع. لا يتلقى Lobsy بيانات البطاقة");
        Add("Legal.Processor.pingen.Purpose",
            "Verificatiebrieven per post versturen",
            "Sending verification letters by post",
            "Wysyłanie listów weryfikacyjnych pocztą",
            "Trimiterea scrisorilor de verificare prin poștă",
            "إرسال خطابات التحقق بالبريد");
        Add("Legal.Processor.pingen.Data",
            "De bedrijfsnaam, het adres en de code in de brief",
            "The company name, the address and the code in the letter",
            "Nazwa firmy, adres i kod w liście",
            "Numele firmei, adresa și codul din scrisoare",
            "اسم الشركة والعنوان والرمز في الخطاب");
        Add("Legal.Processor.pingen.Planned",
            "vanaf de start van brief-verificatie",
            "from the start of letter verification",
            "od uruchomienia weryfikacji listem",
            "de la începutul verificării prin scrisoare",
            "من بداية التحقق بالخطاب");
        Add("Legal.Processor.openai.Purpose",
            "AI-functies die jij zelf kiest",
            "AI features you choose yourself",
            "Funkcje AI, które sam wybierasz",
            "Funcții AI pe care le alegi tu",
            "ميزات الذكاء الاصطناعي التي تختارها");
        Add("Legal.Processor.openai.Data",
            "De tekst die je invult. Bij cv-uitlezen de hele tekst van je cv",
            "The text you enter. For cv reading the whole text of your cv",
            "Tekst, który wpisujesz. Przy czytaniu CV cała treść CV",
            "Textul pe care îl introduci. La citirea CV-ului, tot textul CV-ului",
            "النص الذي تكتبه. وعند قراءة السيرة الذاتية كامل نصها");
        Add("Legal.Processor.cursor.Purpose",
            "Feedback die je stuurt verwerken",
            "Handling the feedback you send",
            "Obsługa opinii, które wysyłasz",
            "Procesarea feedbackului pe care îl trimiți",
            "معالجة الملاحظات التي ترسلها");
        Add("Legal.Processor.cursor.Data",
            "Je feedbacktekst en, als je die meestuurt, een schermafbeelding",
            "Your feedback text and, if you send it along, a screenshot",
            "Treść opinii i, jeśli ją dołączysz, zrzut ekranu",
            "Textul feedbackului și, dacă îl trimiți, o captură de ecran",
            "نص ملاحظتك، ولقطة شاشة إن أرسلتها");
        Add("Legal.Processor.google-ms.Purpose",
            "Inloggen met je Google- of Microsoft-account",
            "Signing in with your Google or Microsoft account",
            "Logowanie kontem Google lub Microsoft",
            "Autentificare cu contul Google sau Microsoft",
            "تسجيل الدخول بحساب Google أو Microsoft");
        Add("Legal.Processor.google-ms.Data",
            "Je gecontroleerde e-mailadres en een inlogcode",
            "Your verified e-mail address and a login id",
            "Twój potwierdzony e-mail i identyfikator logowania",
            "Adresa de e-mail verificată și un cod de autentificare",
            "بريدك الموثّق ورمز تسجيل الدخول");
        Add("Legal.Processor.kvk.Purpose",
            "Bedrijven controleren",
            "Checking companies",
            "Weryfikacja firm",
            "Verificarea firmelor",
            "التحقق من الشركات");
        Add("Legal.Processor.kvk.Data",
            "Het KvK-nummer en het vestigingsnummer",
            "The KvK number and the establishment number",
            "Numer KvK i numer oddziału",
            "Numărul KvK și numărul unității",
            "رقم السجل التجاري ورقم الفرع");
        Add("Legal.Processor.routing.Purpose",
            "Reistijd en reiszones uitrekenen",
            "Calculating travel time and travel zones",
            "Obliczanie czasu podróży i strefy dojazdu",
            "Calcularea timpului de călătorie și a zonelor",
            "حساب وقت التنقل ونطاقات التنقل");
        Add("Legal.Processor.routing.Data",
            "Coördinaten van vertrek en bestemming, en je vervoermiddel",
            "Coordinates of start and destination, and your transport mode",
            "Współrzędne początku i celu oraz środek transportu",
            "Coordonatele de plecare și destinație și mijlocul de transport",
            "إحداثيات الانطلاق والوصول ووسيلة التنقل");
        Add("Legal.Processor.maps.Purpose",
            "Kaarten tonen, ook kaartjes in pdf’s",
            "Showing maps, also map images in pdfs",
            "Pokazywanie map, także obrazków map w pdf",
            "Afișarea hărților, inclusiv imagini de hartă în pdf-uri",
            "عرض الخرائط، وكذلك صور الخرائط في ملفات pdf");
        Add("Legal.Processor.maps.Data",
            "Het IP-adres van je browser en het stuk kaart dat je bekijkt",
            "Your browser's IP address and the part of the map you view",
            "Adres IP przeglądarki i fragment mapy, który oglądasz",
            "Adresa IP a browserului și zona de hartă pe care o vezi",
            "عنوان IP لمتصفحك والجزء الذي تشاهده من الخريطة");
        Add("Legal.Processor.push.Purpose",
            "Meldingen op je telefoon, alleen als je die aanzet",
            "Notifications on your phone, only if you turn them on",
            "Powiadomienia na telefonie, tylko gdy je włączysz",
            "Notificări pe telefon, doar dacă le activezi",
            "إشعارات على هاتفك، فقط إذا شغّلتها");
        Add("Legal.Processor.push.Data",
            "Een code van je apparaat en de tekst van de melding",
            "A code of your device and the text of the notification",
            "Kod twojego urządzenia i treść powiadomienia",
            "Un cod al dispozitivului și textul notificării",
            "رمز جهازك ونص الإشعار");
        Add("Legal.Processor.video.Purpose",
            "Video’s, pas nadat jij op play klikt",
            "Videos, only after you click play",
            "Filmy, dopiero po kliknięciu play",
            "Videoclipuri, doar după ce apeși play",
            "الفيديوهات، فقط بعد الضغط على التشغيل");
        Add("Legal.Processor.video.Data",
            "Je IP-adres en gegevens over je apparaat",
            "Your IP address and data about your device",
            "Twój adres IP i dane o urządzeniu",
            "Adresa ta IP și date despre dispozitiv",
            "عنوان IP وبيانات عن جهازك");
    }

    /// <summary>Privacy section titles and summaries (03 writes the Dutch bodies).</summary>
    private static void MergePrivacySections(AddString Add)
    {
        Add("Privacy.Sec.wie.Title",
            "Wie is verantwoordelijk?",
            "Who is responsible?",
            "Kto jest odpowiedzialny?",
            "Cine este responsabil?",
            "من المسؤول؟");
        Add("Privacy.Sec.wie.Summary",
            "Lobsy is verantwoordelijk voor je gegevens. Vragen? Mail ons. We hebben geen functionaris gegevensbescherming; dat hoeft voor ons niet.",
            "Lobsy is responsible for your data. Questions? Send us an e-mail. We have no data protection officer; we are not required to have one.",
            "Lobsy odpowiada za twoje dane. Masz pytania? Napisz do nas. Nie mamy inspektora ochrony danych; nie musimy go mieć.",
            "Lobsy răspunde pentru datele tale. Ai întrebări? Scrie-ne. Nu avem responsabil cu protecția datelor; nu suntem obligați.",
            "‏Lobsy مسؤول عن بياناتك. لديك سؤال؟ راسلنا. ليس لدينا مسؤول حماية بيانات؛ وهذا غير إلزامي لنا.");
        Add("Privacy.Sec.gegevens.Title",
            "Welke gegevens gebruiken we?",
            "Which data do we use?",
            "Jakich danych używamy?",
            "Ce date folosim?",
            "ما البيانات التي نستخدمها؟");
        Add("Privacy.Sec.gegevens.Summary",
            "Alleen wat nodig is: je account, wat je zoekt, waar je vandaan reist en wat je ons zelf stuurt.",
            "Only what is needed: your account, what you look for, where you travel from and what you send us yourself.",
            "Tylko to, co potrzebne: twoje konto, czego szukasz, skąd dojeżdżasz i co sam nam wyślesz.",
            "Doar ce este necesar: contul tău, ce cauți, de unde călătorești și ce ne trimiți tu.",
            "فقط ما هو ضروري: حسابك، وما تبحث عنه، ومن أين تنتقل، وما ترسله لنا بنفسك.");
        Add("Privacy.Sec.waarom.Title",
            "Waarom mogen we dat?",
            "Why are we allowed to?",
            "Dlaczego nam wolno?",
            "De ce avem dreptul?",
            "لماذا يُسمح لنا بذلك؟");
        Add("Privacy.Sec.waarom.Summary",
            "Omdat we Lobsy moeten kunnen leveren, omdat jij toestemming geeft, omdat we er een goed eigen belang bij hebben, of omdat de wet het vraagt.",
            "Because we must be able to provide Lobsy, because you give permission, because we have a good interest of our own, or because the law requires it.",
            "Bo musimy móc świadczyć Lobsy, bo dajesz zgodę, bo mamy własny uzasadniony interes albo bo wymaga tego prawo.",
            "Pentru că trebuie să putem oferi Lobsy, pentru că ne dai acordul, pentru că avem un interes legitim propriu sau pentru că legea o cere.",
            "لأننا يجب أن نقدّم خدمة Lobsy، أو لأنك توافق، أو لأن لدينا مصلحة مشروعة، أو لأن القانون يطلب ذلك.");
        Add("Privacy.Sec.delen.Title",
            "Met wie delen we gegevens?",
            "Who do we share data with?",
            "Z kim udostępniamy dane?",
            "Cu cine partajăm datele?",
            "مع من نشارك البيانات؟");
        Add("Privacy.Sec.delen.Summary",
            "We delen alleen wat nodig is. Werkgevers zien je naam en telefoon pas als jij dat goed vindt. We verkopen niets.",
            "We only share what is needed. Employers see your name and phone number only when you agree. We sell nothing.",
            "Udostępniamy tylko to, co konieczne. Pracodawca zobaczy twoje imię i telefon dopiero, gdy się zgodzisz. Niczego nie sprzedajemy.",
            "Partajăm doar ce este necesar. Angajatorii văd numele și telefonul tău doar când ești de acord. Nu vindem nimic.",
            "نشارك فقط ما هو ضروري. لا يرى صاحب العمل اسمك أو هاتفك إلا بموافقتك. لا نبيع أي شيء.");
        Add("Privacy.Sec.tests.Title",
            "Tests en talentpool",
            "Tests and talent pool",
            "Testy i baza talentów",
            "Teste și rezerva de talente",
            "الاختبارات ومجموعة المواهب");
        Add("Privacy.Sec.tests.Summary",
            "De tests zijn vrijwillig. Je ruwe antwoorden ziet niemand. De talentpool is vanaf 18 jaar en staat standaard uit.",
            "The tests are voluntary. Nobody sees your raw answers. The talent pool starts at 18 and is off by default.",
            "Testy są dobrowolne. Nikt nie widzi twoich surowych odpowiedzi. Baza talentów jest od 18 lat i domyślnie wyłączona.",
            "Testele sunt opționale. Nimeni nu vede răspunsurile tale brute. Rezerva de talente este de la 18 ani și e oprită implicit.",
            "الاختبارات اختيارية. لا أحد يرى إجاباتك الأصلية. مجموعة المواهب من عمر 18 سنة ومعطّلة افتراضياً.");
        Add("Privacy.Sec.bewaren.Title",
            "Hoe lang bewaren we gegevens?",
            "How long do we keep data?",
            "Jak długo przechowujemy dane?",
            "Cât timp păstrăm datele?",
            "كم مدة الاحتفاظ بالبيانات؟");
        Add("Privacy.Sec.bewaren.Summary",
            "We bewaren gegevens niet langer dan nodig. Je account blijft tot je hem verwijdert.",
            "We keep data no longer than needed. Your account stays until you delete it.",
            "Nie przechowujemy danych dłużej niż to potrzebne. Twoje konto zostaje, dopóki go nie usuniesz.",
            "Nu păstrăm datele mai mult decât este nevoie. Contul tău rămâne până îl ștergi.",
            "لا نحتفظ بالبيانات أكثر من اللازم. يبقى حسابك حتى تحذفه.");
        Add("Privacy.Sec.cookies.Title",
            "Cookies en opslag op je apparaat",
            "Cookies and storage on your device",
            "Pliki cookie i pamięć na twoim urządzeniu",
            "Cookie-uri și stocarea pe dispozitivul tău",
            "ملفات تعريف الارتباط والتخزين على جهازك");
        Add("Privacy.Sec.cookies.Summary",
            "Cookies die nodig zijn om in te loggen, zetten we altijd. Statistieken alleen na ‘Accepteer cookies’.",
            "Cookies needed to sign in are always set. Statistics only after ‘Accept cookies’.",
            "Pliki cookie potrzebne do logowania ustawiamy zawsze. Statystyki tylko po „Akceptuj pliki cookie”.",
            "Cookie-urile necesare autentificării le punem mereu. Statistici doar după „Accept cookie-urile”.",
            "نضع دائماً ملفات تعريف الارتباط اللازمة لتسجيل الدخول. الإحصاءات فقط بعد «أقبل ملفات تعريف الارتباط».");
        Add("Privacy.Sec.ai.Title",
            "AI, matching en reistijd",
            "AI, matching and travel time",
            "AI, dopasowanie i czas podróży",
            "AI, potrivire și timp de călătorie",
            "الذكاء الاصطناعي والمطابقة ووقت التنقل");
        Add("Privacy.Sec.ai.Summary",
            "AI helpt alleen waar jij dat kiest. Een computer beslist nooit alleen over jou; een werkgever kiest zelf.",
            "AI only helps where you choose it. A computer never decides about you on its own; the employer chooses.",
            "AI pomaga tylko tam, gdzie tego chcesz. Komputer nigdy nie decyduje sam o tobie; wybiera pracodawca.",
            "AI ajută doar unde alegi tu. Un computer nu decide niciodată singur despre tine; angajatorul alege.",
            "الذكاء الاصطناعي يساعد فقط حيث تختار. لا يقرر الحاسوب بشأنك وحده؛ صاحب العمل هو من يختار.");
        Add("Privacy.Sec.beveiliging.Title",
            "Hoe beveiligen we je gegevens?",
            "How do we protect your data?",
            "Jak chronimy twoje dane?",
            "Cum îți protejăm datele?",
            "كيف نحمي بياناتك؟");
        Add("Privacy.Sec.beveiliging.Summary",
            "Iedereen ziet alleen wat bij zijn rol hoort. Sleutels en codes staan versleuteld opgeslagen.",
            "Everyone only sees what belongs to their role. Keys and codes are stored encrypted.",
            "Każdy widzi tylko to, co należy do jego roli. Klucze i kody są zaszyfrowane.",
            "Fiecare vede doar ce ține de rolul său. Cheile și codurile sunt stocate criptat.",
            "كل شخص يرى ما يخص دوره فقط. المفاتيح والرموز مخزّنة مشفّرة.");
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
            "Jonger dan 16?",
            "Under 16?",
            "Masz mniej niż 16 lat?",
            "Ai sub 16 ani?",
            "أقل من 16 سنة؟");
        Add("Privacy.Sec.jonger.Summary",
            "Je mag Lobsy gebruiken vanaf 13 jaar. Ben je jonger dan 16? Dan vragen we eerst je ouder of voogd. De talentpool is vanaf 18 jaar.",
            "You may use Lobsy from age 13. Under 16? Then we ask your parent or guardian first. The talent pool starts at 18.",
            "Możesz korzystać z Lobsy od 13 lat. Masz mniej niż 16? Najpierw pytamy rodzica lub opiekuna. Baza talentów jest od 18 lat.",
            "Poți folosi Lobsy de la 13 ani. Ai sub 16? Atunci întrebăm mai întâi părintele sau tutorele. Rezerva de talente e de la 18 ani.",
            "يمكنك استخدام Lobsy من عمر 13 سنة. أقل من 16؟ نسأل والدك أو وليّك أولاً. مجموعة المواهب من عمر 18 سنة.");
        Add("Privacy.Sec.wijzigingen.Title",
            "Wijzigingen",
            "Changes",
            "Zmiany",
            "Modificări",
            "التغييرات");
        Add("Privacy.Sec.wijzigingen.Summary",
            "Verandert er iets belangrijks? Dan laten we het weten in het platform of per e-mail. Onderaan staat wat er is veranderd.",
            "Is something important changing? Then we let you know in the platform or by e-mail. At the bottom you see what changed.",
            "Zmienia się coś ważnego? Powiemy o tym w platformie lub e-mailem. Na dole widzisz, co się zmieniło.",
            "Se schimbă ceva important? Îți spunem în platformă sau prin e-mail. Jos vezi ce s-a schimbat.",
            "هل تغيّر شيء مهم؟ سنخبرك داخل المنصة أو بالبريد. في الأسفل ترى ما تغيّر.");
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
