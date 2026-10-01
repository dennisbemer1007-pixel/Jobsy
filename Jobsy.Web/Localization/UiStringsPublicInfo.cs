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
    }
}
