namespace Jobsy.Web.Localization;

/// <summary>
/// Copy for the status / error pages (404, 500 and the shared ErrorLayout chrome).
/// nl and en are final; pl, ro and ar are B1 drafts — see <c>docs/i18n/errors-review.md</c>.
/// Tone: B1, "je", calm, never blaming and without technical words.
/// </summary>
public static class UiStringsStatus
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

        // —— 404 ——
        Add("Status.NotFound.Chip",
            "404",
            "404",
            "404",
            "404",
            "٤٠٤");
        Add("Status.NotFound.Title",
            "Deze pagina bestaat niet",
            "This page does not exist",
            "Ta strona nie istnieje",
            "Această pagină nu există",
            "هذه الصفحة غير موجودة");
        Add("Status.NotFound.Lead",
            "Misschien is de link oud, of zit er een typfout in. Geen zorgen, we helpen je verder.",
            "The link may be old, or there is a typo in it. No worries, we will help you on your way.",
            "Link może być stary albo jest w nim literówka. Spokojnie, pomożemy ci dalej.",
            "Poate linkul este vechi sau are o greșeală de scriere. Nicio grijă, te ajutăm mai departe.",
            "ربما يكون الرابط قديماً أو فيه خطأ مطبعي. لا تقلق، سنساعدك للمتابعة.");

        // —— 500 ——
        Add("Status.Error.Eyebrow",
            "Foutje bij ons",
            "A slip on our side",
            "Potknięcie po naszej stronie",
            "O mică eroare la noi",
            "خطأ من جانبنا");
        Add("Status.Error.Title",
            "Er ging iets mis",
            "Something went wrong",
            "Coś poszło nie tak",
            "Ceva nu a mers bine",
            "حدث خطأ ما");
        Add("Status.Error.Lead",
            "Het ligt niet aan jou. Probeer het zo nog eens. Lukt het niet? Stuur ons de code hieronder.",
            "It is not your fault. Try again in a moment. Still stuck? Send us the code below.",
            "To nie twoja wina. Spróbuj za chwilę jeszcze raz. Nadal nie działa? Wyślij nam kod poniżej.",
            "Nu este vina ta. Mai încearcă o dată peste puțin timp. Tot nu merge? Trimite-ne codul de mai jos.",
            "الأمر ليس بسببك. جرّب مرة أخرى بعد قليل. ما زال لا يعمل؟ أرسل لنا الرمز أدناه.");

        // —— Generic status (codes without their own page yet) ——
        Add("Status.Generic.Title",
            "Deze pagina kan nu niet geopend worden",
            "This page cannot be opened right now",
            "Tej strony nie można teraz otworzyć",
            "Această pagină nu poate fi deschisă acum",
            "لا يمكن فتح هذه الصفحة الآن");
        Add("Status.Generic.Lead",
            "Probeer het zo nog eens, of ga terug naar het begin.",
            "Try again in a moment, or go back to the start.",
            "Spróbuj za chwilę jeszcze raz albo wróć na początek.",
            "Mai încearcă peste puțin timp sau întoarce-te la început.",
            "جرّب مرة أخرى بعد قليل أو عُد إلى البداية.");

        // —— Shared actions and small print ——
        Add("Status.Common.Banenkaart",
            "Banenkaart",
            "Job map",
            "Mapa pracy",
            "Harta joburilor",
            "خريطة الوظائف");
        Add("Status.Common.Passport",
            "Mijn Paspoort",
            "My Passport",
            "Mój Paszport",
            "Pașaportul meu",
            "جوازي");
        Add("Status.Common.FreeTest",
            "Gratis test",
            "Free test",
            "Darmowy test",
            "Test gratuit",
            "اختبار مجاني");
        Add("Status.Common.Help",
            "Hulp",
            "Help",
            "Pomoc",
            "Ajutor",
            "مساعدة");
        Add("Status.Common.TryAgain",
            "Probeer opnieuw",
            "Try again",
            "Spróbuj ponownie",
            "Încearcă din nou",
            "حاول مرة أخرى");
        Add("Status.Common.MailSupport",
            "Mail support",
            "E-mail support",
            "Napisz do wsparcia",
            "Scrie-ne la suport",
            "راسل الدعم");
        Add("Status.Common.CodeLabel",
            "Foutcode",
            "Error code",
            "Kod błędu",
            "Cod de eroare",
            "رمز الخطأ");
        Add("Status.Common.Copy",
            "Kopieer",
            "Copy",
            "Kopiuj",
            "Copiază",
            "انسخ");
        Add("Status.Common.Copied",
            "Gekopieerd",
            "Copied",
            "Skopiowano",
            "Copiat",
            "تم النسخ");
        Add("Status.Common.SupportLine",
            "Klopt er iets niet? Mail {0}.",
            "Does something look wrong? E-mail {0}.",
            "Coś się nie zgadza? Napisz na {0}.",
            "Ceva nu este în regulă? Scrie la {0}.",
            "هل هناك شيء غير صحيح؟ راسل {0}.");
        Add("Status.Common.MailSubject",
            "Foutcode {0}",
            "Error code {0}",
            "Kod błędu {0}",
            "Cod de eroare {0}",
            "رمز الخطأ {0}");

        // —— ErrorLayout chrome ——
        Add("Status.Nav.SkipToContent",
            "Naar de inhoud",
            "Skip to content",
            "Przejdź do treści",
            "Sari la conținut",
            "انتقل إلى المحتوى");
        Add("Status.Nav.Home",
            "Naar de startpagina",
            "To the home page",
            "Na stronę główną",
            "Spre pagina principală",
            "إلى الصفحة الرئيسية");
        Add("Status.Nav.ChooseLanguage",
            "Kies je taal",
            "Choose your language",
            "Wybierz język",
            "Alege limba",
            "اختر لغتك");
        Add("Status.Nav.Login",
            "Inloggen",
            "Sign in",
            "Zaloguj się",
            "Autentificare",
            "تسجيل الدخول");
        Add("Status.Nav.MyStart",
            "Naar mijn start",
            "To my start page",
            "Do mojej strony startowej",
            "Spre pagina mea de start",
            "إلى صفحتي");
        Add("Status.Nav.Privacy",
            "Privacy",
            "Privacy",
            "Prywatność",
            "Confidențialitate",
            "الخصوصية");
        Add("Status.Nav.Terms",
            "Voorwaarden",
            "Terms",
            "Warunki",
            "Termeni",
            "الشروط");
        Add("Status.Nav.Help",
            "Hulp",
            "Help",
            "Pomoc",
            "Ajutor",
            "مساعدة");

        // —— 403 Geen toegang (errors 02) ——
        Add("Status.Forbidden.Chip",
            "🔒 Geen toegang",
            "🔒 Access denied",
            "🔒 Brak dostępu",
            "🔒 Acces interzis",
            "🔒 لا وصول");
        Add("Status.Forbidden.Title",
            "Deze pagina is niet voor jouw account",
            "This page is not for your account",
            "Ta strona nie jest dla twojego konta",
            "Această pagină nu este pentru contul tău",
            "هذه الصفحة ليست لحسابك");
        Add("Status.Forbidden.Lead",
            "Je bent ingelogd, maar dit deel van Lobsy hoort bij een andere rol.",
            "You are signed in, but this part of Lobsy belongs to a different role.",
            "Jesteś zalogowany, ale ta część Lobsy należy do innej roli.",
            "Ești autentificat, dar această parte din Lobsy aparține unui alt rol.",
            "أنت مسجّل الدخول، لكن هذا الجزء من لوبسي يخص دوراً آخر.");
        Add("Status.Forbidden.AccountIntro",
            "Je bent ingelogd als {0}",
            "You are signed in as {0}",
            "Jesteś zalogowany jako {0}",
            "Ești autentificat ca {0}",
            "أنت مسجّل الدخول باسم {0}");
        Add("Status.Forbidden.SwitchAccount",
            "Inloggen met een ander account",
            "Sign in with a different account",
            "Zaloguj się na inne konto",
            "Autentifică-te cu alt cont",
            "تسجيل الدخول بحساب آخر");
        Add("Status.Forbidden.ToHomepage",
            "Naar de voorpagina",
            "To the home page",
            "Na stronę główną",
            "Spre pagina principală",
            "إلى الصفحة الرئيسية");
        Add("Status.Forbidden.EmployersOffTitle",
            "Voor werkgevers: binnenkort",
            "For employers: coming soon",
            "Dla pracodawców: wkrótce",
            "Pentru angajatori: în curând",
            "لأصحاب العمل: قريباً");
        Add("Status.Forbidden.SchoolsOffTitle",
            "Het scholenportaal is nog niet open",
            "The school portal is not open yet",
            "Portal szkolny nie jest jeszcze otwarty",
            "Portalul școlii nu este încă deschis",
            "بوابة المدرسة ليست مفتوحة بعد");
        Add("Status.Forbidden.AmbassadorsOffTitle",
            "Sales en ambassadeurs staat uit",
            "Sales and ambassadors is switched off",
            "Sprzedaż i ambasadorzy są wyłączeni",
            "Vânzările și ambasadorii sunt oprite",
            "المبيعات والسفراء متوقفان");
        Add("Status.Forbidden.AmbassadorsOffLead",
            "Deze functie staat uit. Zet hem aan bij Platforminstellingen. Tot die tijd is deze pagina niet beschikbaar.",
            "This feature is off. Turn it on under Platform settings. Until then this page is not available.",
            "Ta funkcja jest wyłączona. Włącz ją w ustawieniach platformy.",
            "Funcția este oprită. Pornește-o la setările platformei.",
            "هذه الميزة متوقفة. فعّلها من إعدادات المنصة.");
        Add("Status.Forbidden.SchoolsOffLeadGuest",
            "Het scholenportaal is nog niet open. Je hebt geen account nodig. Vragen? Mail support.",
            "The school portal is not open yet. You do not need an account. Questions? Email support.",
            "Portal szkolny nie jest jeszcze otwarty. Konto nie jest potrzebne. Pytania? Napisz do wsparcia.",
            "Portalul școlii nu este încă deschis. Nu ai nevoie de un cont. Întrebări? Scrie la suport.",
            "بوابة المدارس ليست مفتوحة بعد. لا تحتاج إلى حساب. أسئلة؟ راسل الدعم.");
        Add("Status.Forbidden.SchoolsOffLead",
            "Lobsy voor scholen staat nu uit. Je account blijft bewaard. Log uit, of mail support als je een vraag hebt.",
            "Lobsy for schools is switched off right now. Your account is kept. Sign out, or email support if you have a question.",
            "Lobsy dla szkół jest teraz wyłączone. Twoje konto zostaje. Wyloguj się albo napisz do wsparcia, jeśli masz pytanie.",
            "Lobsy pentru școli este oprit acum. Contul tău rămâne. Deconectează-te sau scrie la suport dacă ai o întrebare.",
            "لوبسي للمدارس متوقف الآن. حسابك يبقى محفوظاً. سجّل الخروج، أو راسل الدعم إذا كان لديك سؤال.");
        Add("Login.SchoolsPaused",
            "Het scholenportaal is nog niet open. Je account blijft bewaard. Vragen? Mail support@lobsy.nl.",
            "The school portal is not open yet. Your account is kept. Questions? Email support@lobsy.nl.",
            "Portal szkolny nie jest jeszcze otwarty. Twoje konto zostaje. Pytania? Napisz na support@lobsy.nl.",
            "Portalul școlii nu este încă deschis. Contul tău rămâne. Întrebări? Scrie la support@lobsy.nl.",
            "بوابة المدرسة ليست مفتوحة بعد. حسابك يبقى محفوظاً. أسئلة؟ راسل support@lobsy.nl.");
        Add("Status.Forbidden.EmployersOffLead",
            "Lobsy is nu eerst voor kandidaten. De omgeving voor werkgevers komt terug in een volgende fase.",
            "Lobsy is for candidates first right now. The employer area returns in a later phase.",
            "Lobsy jest teraz najpierw dla kandydatów. Strefa pracodawców wróci w kolejnej fazie.",
            "Lobsy este acum mai întâi pentru candidați. Zona angajatorilor revine într-o fază următoare.",
            "لوبسي الآن أولاً للمرشحين. ستعود بيئة أصحاب العمل في مرحلة لاحقة.");
        Add("Status.Forbidden.Role.Candidate",
            "Kandidaat",
            "Candidate",
            "Kandydat",
            "Candidat",
            "مرشح");
        Add("Status.Forbidden.Role.BranchManager",
            "Vestigingsmanager",
            "Branch manager",
            "Kierownik placówki",
            "Manager de filială",
            "مدير الفرع");
        Add("Status.Forbidden.Role.RegionalManager",
            "Regiomanager",
            "Regional manager",
            "Kierownik regionu",
            "Manager regional",
            "مدير المنطقة");
        Add("Status.Forbidden.Role.EnterpriseManager",
            "Bedrijfsmanager",
            "Enterprise manager",
            "Kierownik przedsiębiorstwa",
            "Manager de companie",
            "مدير المؤسسة");
        Add("Status.Forbidden.Role.Intermediary",
            "Intermediair",
            "Intermediary",
            "Pośrednik",
            "Intermediar",
            "وسيط");
        Add("Status.Forbidden.Role.Admin",
            "Beheerder",
            "Admin",
            "Administrator",
            "Administrator",
            "مشرف");
        Add("Status.Forbidden.Role.SalesManager",
            "Salesmanager",
            "Sales manager",
            "Kierownik sprzedaży",
            "Manager de vânzări",
            "مدير المبيعات");
        Add("Status.Forbidden.Role.Ambassadeur",
            "Ambassadeur",
            "Ambassador",
            "Ambasador",
            "Ambasador",
            "سفير");
        Add("Status.Forbidden.Role.SchoolAdmin",
            "Schoolbeheerder",
            "School admin",
            "Administrator szkoły",
            "Administrator școlar",
            "مشرف المدرسة");
        Add("Status.Forbidden.Role.Teacher",
            "Docent",
            "Teacher",
            "Nauczyciel",
            "Profesor",
            "معلم");
        Add("Status.Forbidden.Role.Unknown",
            "Onbekende rol",
            "Unknown role",
            "Nieznana rola",
            "Rol necunoscut",
            "دور غير معروف");

        // —— 410 Vacature gesloten (errors 03) ——
        Add("Status.Gone.Eyebrow",
            "Vacature gesloten",
            "Vacancy closed",
            "Oferta zamknięta",
            "Loc de muncă închis",
            "الوظيفة مغلقة");
        Add("Status.Gone.Title",
            "Deze vacature is gesloten",
            "This vacancy is closed",
            "Ta oferta pracy jest zamknięta",
            "Acest loc de muncă este închis",
            "هذه الوظيفة مغلقة");
        Add("Status.Gone.Lead",
            "“{0}” in {1} staat niet meer open. Misschien past een van deze banen bij je.",
            "“{0}” in {1} is no longer open. Maybe one of these jobs is a good fit for you.",
            "„{0}” w {1} nie jest już otwarta. Może jedna z tych ofert będzie dla ciebie dobra.",
            "„{0}” din {1} nu mai este deschis. Poate unul dintre aceste locuri de muncă îți convine.",
            "لم تعد وظيفة ”{0}“ في {1} متاحة. ربما تناسبك واحدة من هذه الوظائف.");
        Add("Status.Gone.LeadNoCity",
            "“{0}” staat niet meer open. Misschien past een van deze banen bij je.",
            "“{0}” is no longer open. Maybe one of these jobs is a good fit for you.",
            "„{0}” nie jest już otwarta. Może jedna z tych ofert będzie dla ciebie dobra.",
            "„{0}” nu mai este deschis. Poate unul dintre aceste locuri de muncă îți convine.",
            "لم تعد وظيفة ”{0}“ متاحة. ربما تناسبك واحدة من هذه الوظائف.");
        Add("Status.Gone.NoneFound",
            "We vonden nu geen vergelijkbare banen in de buurt.",
            "We could not find similar jobs nearby right now.",
            "Nie znaleźliśmy teraz podobnych ofert w pobliżu.",
            "Nu am găsit acum locuri de muncă similare în apropiere.",
            "لم نجد حالياً وظائف مشابهة قريبة.");
        Add("Status.Gone.ViewMap",
            "Bekijk de banenkaart",
            "View the job map",
            "Zobacz mapę pracy",
            "Vezi harta joburilor",
            "عرض خريطة الوظائف");
        Add("Status.Gone.TravelMinutes",
            "{0} min",
            "{0} min",
            "{0} min",
            "{0} min",
            "{0} دقيقة");
        Add("Status.Gone.DistanceKm",
            "{0} km",
            "{0} km",
            "{0} km",
            "{0} km",
            "{0} كم");

        // —— 429 Even rustig aan (errors 04) ——
        Add("Status.TooMany.Eyebrow",
            "Even rustig aan",
            "Easy does it",
            "Spokojnie",
            "Mai ușor",
            "بهدوء قليلاً");
        Add("Status.TooMany.Title",
            "Even rustig aan",
            "Easy does it",
            "Spokojnie",
            "Mai ușor",
            "بهدوء قليلاً");
        Add("Status.TooMany.Lead",
            "Je deed veel verzoeken achter elkaar. Wacht {0} seconden en probeer het dan opnieuw.",
            "You made a lot of requests in a row. Wait {0} seconds and then try again.",
            "Wysłałeś wiele zapytań po kolei. Odczekaj {0} sekund i spróbuj ponownie.",
            "Ai trimis multe cereri una după alta. Așteaptă {0} secunde și încearcă din nou.",
            "قمت بمحاولات كثيرة متتابعة. انتظر {0} ثانية ثم حاول مرة أخرى.");

        // —— 503 Onderhoud (errors 05) ——
        Add("Status.Maintenance.Eyebrow",
            "Onderhoud",
            "Maintenance",
            "Prace serwisowe",
            "Mentenanță",
            "صيانة");
        Add("Status.Maintenance.Title",
            "We zijn even aan het klussen",
            "We are doing a bit of maintenance",
            "Chwilowo majsterkujemy",
            "Lucrăm puțin la site",
            "نحن نُجري بعض الصيانة");
        Add("Status.Maintenance.Lead",
            "Lobsy is zo terug.",
            "Lobsy will be back shortly.",
            "Lobsy wkrótce wróci.",
            "Lobsy revine imediat.",
            "سيعود لوبسي قريباً.");
        Add("Status.Maintenance.BackAt",
            "We verwachten terug te zijn om {0}.",
            "We expect to be back at {0}.",
            "Spodziewamy się wrócić o {0}.",
            "Ne așteptăm să revenim la {0}.",
            "نتوقع العودة في {0}.");
        Add("Status.Maintenance.AdminLogin",
            "Beheerder? Inloggen",
            "Admin? Sign in",
            "Administrator? Zaloguj się",
            "Administrator? Autentifică-te",
            "مشرف؟ سجّل الدخول");
        Add("Status.Maintenance.Short",
            "We zijn even aan het klussen. Lobsy is zo terug.",
            "We are doing a bit of maintenance. Lobsy will be back shortly.",
            "Chwilowo majsterkujemy. Lobsy wkrótce wróci.",
            "Lucrăm puțin la site. Lobsy revine imediat.",
            "نحن نُجري بعض الصيانة. سيعود لوبسي قريباً.");

        // —— Reconnect toast (errors 04) ——
        Add("Status.Reconnect.Trying",
            "Verbinding herstellen…",
            "Reconnecting…",
            "Łączenie ponownie…",
            "Se reconectează…",
            "جارٍ إعادة الاتصال…");
        Add("Status.Reconnect.Failed",
            "De verbinding is weg.",
            "The connection is gone.",
            "Połączenie zostało przerwane.",
            "Conexiunea s-a pierdut.",
            "انقطع الاتصال.");
        Add("Status.Reconnect.Rejected",
            "Je sessie is verlopen.",
            "Your session has expired.",
            "Twoja sesja wygasła.",
            "Sesiunea ta a expirat.",
            "انتهت صلاحية جلستك.");
        Add("Status.Reconnect.Reload",
            "Opnieuw laden",
            "Reload",
            "Odśwież",
            "Reîncarcă",
            "إعادة التحميل");

        // —— Inline block error (errors 04) ——
        Add("Status.Inline.Title",
            "Dit stukje laadt nu niet.",
            "This bit is not loading right now.",
            "Ten fragment teraz się nie wczytuje.",
            "Această bucată nu se încarcă acum.",
            "هذا الجزء لا يتم تحميله الآن.");
        Add("Status.Inline.Retry",
            "Opnieuw",
            "Retry",
            "Ponów",
            "Reîncearcă",
            "أعد المحاولة");
        Add("Status.Inline.Code",
            "Foutcode {0}",
            "Error code {0}",
            "Kod błędu {0}",
            "Cod de eroare {0}",
            "رمز الخطأ {0}");

        // —— User-facing fallbacks (errors 04, UserFacingError) ——
        Add("Common.Error.TryAgain",
            "Dat lukte niet. Probeer het zo nog eens.",
            "That did not work. Try again in a moment.",
            "Nie udało się. Spróbuj za chwilę jeszcze raz.",
            "Nu a funcționat. Mai încearcă peste puțin timp.",
            "لم ينجح ذلك. جرّب مرة أخرى بعد قليل.");
        Add("Common.Error.Network",
            "Geen verbinding. Probeer het zo nog eens.",
            "No connection. Try again in a moment.",
            "Brak połączenia. Spróbuj za chwilę jeszcze raz.",
            "Fără conexiune. Mai încearcă peste puțin timp.",
            "لا يوجد اتصال. جرّب مرة أخرى بعد قليل.");
        Add("Common.Error.RateLimited",
            "Even rustig aan. Wacht een momentje en probeer het opnieuw.",
            "Easy does it. Wait a moment and try again.",
            "Spokojnie. Odczekaj chwilę i spróbuj ponownie.",
            "Mai ușor. Așteaptă un moment și încearcă din nou.",
            "بهدوء. انتظر لحظة ثم حاول مرة أخرى.");
        Add("Common.Error.NotFound",
            "Dit kunnen we niet vinden.",
            "We cannot find this.",
            "Nie możemy tego znaleźć.",
            "Nu găsim acest lucru.",
            "لا يمكننا العثور على هذا.");
        Add("Common.Error.Forbidden",
            "Dit mag met jouw account niet.",
            "Your account is not allowed to do this.",
            "Twoje konto nie może tego zrobić.",
            "Contul tău nu are voie să facă asta.",
            "حسابك غير مسموح له بهذا.");
        Add("Common.Error.Validation",
            "Controleer wat je hebt ingevuld en probeer het opnieuw.",
            "Check what you filled in and try again.",
            "Sprawdź, co wpisałeś, i spróbuj ponownie.",
            "Verifică ce ai completat și încearcă din nou.",
            "تحقق من البيانات التي أدخلتها ثم حاول مرة أخرى.");
        Add("Common.Error.Maintenance",
            "We zijn even aan het werk aan Lobsy. Probeer het zo nog eens.",
            "We are working on Lobsy for a moment. Try again shortly.",
            "Pracujemy chwilę nad Lobsy. Spróbuj niedługo ponownie.",
            "Lucrăm puțin la Lobsy. Mai încearcă în scurt timp.",
            "نعمل على لوبسي لبعض الوقت. جرّب بعد قليل.");

        // —— Embedded 404 card on /{kvk} (StatusPageContent, public-pages hotfix) ——
        Add("Status.NotFound.ToMap",
            "Naar de banenkaart",
            "To the job map",
            "Do mapy ofert",
            "Către harta joburilor",
            "إلى خريطة الوظائف");
        Add("Status.NotFound.HowLobsy",
            "Hoe werkt Lobsy?",
            "How does Lobsy work?",
            "Jak działa Lobsy?",
            "Cum funcționează Lobsy?",
            "كيف يعمل Lobsy؟");
        Add("Status.Generic.Home",
            "Naar de banenkaart",
            "To the job map",
            "Do mapy ofert",
            "Către harta joburilor",
            "إلى خريطة الوظائف");
    }
}
