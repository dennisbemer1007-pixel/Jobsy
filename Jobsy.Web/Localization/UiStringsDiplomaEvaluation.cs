namespace Jobsy.Web.Localization;

/// <summary>
/// Foreign diploma evaluation (diplomawaardering). Dutch is source.
/// pl/ro/ar (and the English wording) are machine-translated and need native review.
/// See docs/i18n/diploma-evaluation-review.md.
/// </summary>
public static class UiStringsDiplomaEvaluation
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

        Add("DiplomaEval.Title",
            "Diploma in het buitenland",
            "Diploma from abroad",
            "Dyplom z zagranicy",
            "Diplomă din străinătate",
            "شهادة من الخارج");
        Add("DiplomaEval.LeadEmpty",
            "Heb je nog geen officiële Nederlandse waardering? Die vraag je zelf aan. Lobsy schat je niveau niet in.",
            "No official Dutch evaluation yet? You request it yourself. Lobsy does not estimate your level.",
            "Nie masz jeszcze oficjalnej holenderskiej oceny? Wnioskujesz o nią sam. Lobsy nie szacuje twojego poziomu.",
            "Nu ai încă o evaluare oficială olandeză? O ceri tu. Lobsy nu estimează nivelul tău.",
            "ليس لديك بعد تقييم هولندي رسمي؟ تطلبه بنفسك. لوبسي لا يقدّر مستواك.");
        Add("DiplomaEval.LinkNuffic",
            "Nuffic IDW — voor hbo, wo, havo en vwo",
            "Nuffic IDW — for hbo, wo, havo and vwo",
            "Nuffic IDW — dla hbo, wo, havo i vwo",
            "Nuffic IDW — pentru hbo, wo, havo și vwo",
            "Nuffic IDW — لمستويات hbo وwo وhavo وvwo");
        Add("DiplomaEval.LinkSbb",
            "SBB — voor mbo en beroepsonderwijs",
            "SBB — for mbo and vocational education",
            "SBB — dla mbo i kształcenia zawodowego",
            "SBB — pentru mbo și învățământ profesional",
            "SBB — لتعليم mbo والمهني");
        Add("DiplomaEval.Statushouders",
            "Statushouders kunnen de waardering vaak gratis aanvragen via hun gemeente of het UWV.",
            "People with refugee status can often request the evaluation for free via their municipality or the UWV.",
            "Osoby ze statusem uchodźcy często mogą wnioskować o ocenę bezpłatnie przez gminę lub UWV.",
            "Persoanele cu statut de refugiat pot cere adesea evaluarea gratuit prin primărie sau UWV.",
            "يمكن لحاملي صفة اللجوء غالباً طلب التقييم مجاناً عبر البلدية أو UWV.");
        Add("DiplomaEval.NewWindow",
            "opent in een nieuw venster",
            "opens in a new window",
            "otwiera się w nowym oknie",
            "se deschide într-o fereastră nouă",
            "يفتح في نافذة جديدة");
        Add("DiplomaEval.Add",
            "Waardering toevoegen",
            "Add evaluation",
            "Dodaj ocenę",
            "Adaugă evaluarea",
            "أضف التقييم");
        Add("DiplomaEval.Edit",
            "Bewerken",
            "Edit evaluation",
            "Edytuj ocenę",
            "Editează evaluarea",
            "عدّل التقييم");
        Add("DiplomaEval.Remove",
            "Verwijderen",
            "Remove evaluation",
            "Usuń ocenę",
            "Șterge evaluarea",
            "احذف التقييم");
        Add("DiplomaEval.Removed",
            "Waardering verwijderd.",
            "Evaluation removed.",
            "Ocena usunięta.",
            "Evaluare ștearsă.",
            "تم حذف التقييم.");
        Add("DiplomaEval.Save",
            "Opslaan",
            "Save evaluation",
            "Zapisz ocenę",
            "Salvează evaluarea",
            "احفظ التقييم");
        Add("DiplomaEval.Saved",
            "Waardering opgeslagen.",
            "Evaluation saved.",
            "Ocena zapisana.",
            "Evaluare salvată.",
            "تم حفظ التقييم.");
        Add("DiplomaEval.Cancel",
            "Annuleren",
            "Cancel",
            "Anuluj",
            "Anulează",
            "إلغاء");
        Add("DiplomaEval.DiplomaTitle",
            "Naam van het diploma",
            "Name of the diploma",
            "Nazwa dyplomu",
            "Numele diplomei",
            "اسم الشهادة");
        Add("DiplomaEval.Body",
            "Instantie die de waardering afgaf",
            "Organisation that issued the evaluation",
            "Instytucja, która wydała ocenę",
            "Instituția care a emis evaluarea",
            "الجهة التي أصدرت التقييم");
        Add("DiplomaEval.Body.Nuffic",
            "Nuffic / IDW",
            "Nuffic / IDW (credential evaluation)",
            "Nuffic / IDW (ocena dyplomu)",
            "Nuffic / IDW (evaluarea diplomei)",
            "Nuffic / IDW (تقييم الشهادة)");
        Add("DiplomaEval.Body.Sbb",
            "SBB",
            "SBB (vocational)",
            "SBB (zawodowe)",
            "SBB (profesional)",
            "SBB (مهني)");
        Add("DiplomaEval.Body.Other",
            "Andere instantie",
            "Another organisation",
            "Inna instytucja",
            "Altă instituție",
            "جهة أخرى");
        Add("DiplomaEval.BodyOther",
            "Naam van de instantie",
            "Name of the organisation",
            "Nazwa instytucji",
            "Numele instituției",
            "اسم الجهة");
        Add("DiplomaEval.LevelText",
            "Nederlands niveau, letterlijk zoals op de waardering",
            "Dutch level, exactly as written on the evaluation",
            "Poziom niderlandzki, dokładnie jak na ocenie",
            "Nivel olandez, exact cum scrie pe evaluare",
            "المستوى الهولندي، كما هو مكتوب على التقييم");
        Add("DiplomaEval.LevelTextHint",
            "Typ precies wat er op het document staat. Lobsy vult of berekent dit niet.",
            "Type exactly what the document says. Lobsy does not fill this in or calculate it.",
            "Wpisz dokładnie to, co jest na dokumencie. Lobsy tego nie uzupełnia ani nie oblicza.",
            "Scrie exact ce scrie pe document. Lobsy nu completează și nu calculează asta.",
            "اكتب تماماً ما هو على المستند. لوبسي لا يملأ هذا ولا يحسبه.");
        Add("DiplomaEval.LevelCode",
            "Optioneel: kies het niveau uit de lijst",
            "Optional: pick the level from the list",
            "Opcjonalnie: wybierz poziom z listy",
            "Opțional: alege nivelul din listă",
            "اختياري: اختر المستوى من القائمة");
        Add("DiplomaEval.LevelCode.None",
            "Niet kiezen",
            "Do not pick",
            "Nie wybieraj",
            "Nu alege",
            "بدون اختيار");
        Add("DiplomaEval.Level.mbo-1", "mbo 1", "mbo level 1", "poziom mbo 1", "nivel mbo 1", "مستوى mbo 1");
        Add("DiplomaEval.Level.mbo-2", "mbo 2", "mbo level 2", "poziom mbo 2", "nivel mbo 2", "مستوى mbo 2");
        Add("DiplomaEval.Level.mbo-3", "mbo 3", "mbo level 3", "poziom mbo 3", "nivel mbo 3", "مستوى mbo 3");
        Add("DiplomaEval.Level.mbo-4", "mbo 4", "mbo level 4", "poziom mbo 4", "nivel mbo 4", "مستوى mbo 4");
        Add("DiplomaEval.Level.havo", "havo", "havo (senior general)", "havo (ogólnokształcące)", "havo (general)", "havo (عام)");
        Add("DiplomaEval.Level.vwo", "vwo", "vwo (pre-university)", "vwo (przeduniwersyteckie)", "vwo (preuniversitar)", "vwo (ما قبل الجامعة)");
        Add("DiplomaEval.Level.hbo-bachelor", "hbo-bachelor", "hbo bachelor", "licencjat hbo", "licență hbo", "بكالوريوس hbo");
        Add("DiplomaEval.Level.hbo-master", "hbo-master", "hbo master", "magister hbo", "master hbo", "ماجستير hbo");
        Add("DiplomaEval.Level.wo-bachelor", "wo-bachelor", "wo bachelor", "licencjat wo", "licență wo", "بكالوريوس wo");
        Add("DiplomaEval.Level.wo-master", "wo-master", "wo master", "magister wo", "master wo", "ماجستير wo");
        Add("DiplomaEval.Date",
            "Datum van de waardering",
            "Date of the evaluation",
            "Data oceny",
            "Data evaluării",
            "تاريخ التقييم");
        Add("DiplomaEval.Reference",
            "Kenmerk of referentienummer",
            "Reference number",
            "Numer referencyjny",
            "Număr de referință",
            "الرقم المرجعي");
        Add("DiplomaEval.Document",
            "Document (PDF of afbeelding, optioneel)",
            "Document (PDF or image, optional)",
            "Dokument (PDF lub obraz, opcjonalnie)",
            "Document (PDF sau imagine, opțional)",
            "مستند (PDF أو صورة، اختياري)");
        Add("DiplomaEval.DocumentHint",
            "Alleen jij kunt dit bestand openen. Partners en werkgevers zien het bestand niet, wel de gegevens die je hier invult.",
            "Only you can open this file. Partners and employers do not see the file, only the details you enter here.",
            "Tylko ty możesz otworzyć ten plik. Partnerzy i pracodawcy nie widzą pliku, tylko dane, które tu wpisujesz.",
            "Doar tu poți deschide fișierul. Partenerii și angajatorii nu văd fișierul, ci doar datele pe care le completezi aici.",
            "أنت فقط تستطيع فتح هذا الملف. الشركاء وأصحاب العمل لا يرون الملف، بل البيانات التي تدخلها هنا.");
        Add("DiplomaEval.ChooseFile",
            "Bestand kiezen",
            "Choose file",
            "Wybierz plik",
            "Alege fișierul",
            "اختر ملفاً");
        Add("DiplomaEval.Download",
            "Document downloaden",
            "Download document",
            "Pobierz dokument",
            "Descarcă documentul",
            "نزّل المستند");
        Add("DiplomaEval.RemoveDocument",
            "Document verwijderen",
            "Remove document",
            "Usuń dokument",
            "Șterge documentul",
            "احذف المستند");
        Add("DiplomaEval.Attribution.Official",
            "volgens waardering van Nuffic/SBB",
            "according to an evaluation by Nuffic/SBB",
            "według oceny Nuffic/SBB",
            "conform evaluării Nuffic/SBB",
            "وفقاً لتقييم Nuffic/SBB");
        Add("DiplomaEval.Attribution.Other",
            "volgens waardering van {0}",
            "according to an evaluation by {0}",
            "według oceny {0}",
            "conform evaluării {0}",
            "وفقاً لتقييم {0}");
        Add("DiplomaEval.YourChoice",
            "Jouw keuze: {0}",
            "Your pick: {0}",
            "Twój wybór: {0}",
            "Alegerea ta: {0}",
            "اختيارك: {0}");
        Add("DiplomaEval.FactMeta",
            "{0} · kenmerk {1}",
            "{0} · reference {1}",
            "{0} · numer {1}",
            "{0} · referință {1}",
            "{0} · مرجع {1}");

        Add("DiplomaEval.Error.body_required",
            "Kies de instantie die de waardering afgaf.",
            "Choose the organisation that issued the evaluation.",
            "Wybierz instytucję, która wydała ocenę.",
            "Alege instituția care a emis evaluarea.",
            "اختر الجهة التي أصدرت التقييم.");
        Add("DiplomaEval.Error.body_invalid",
            "Kies de instantie die de waardering afgaf.",
            "Choose the organisation that issued the evaluation.",
            "Wybierz instytucję, która wydała ocenę.",
            "Alege instituția care a emis evaluarea.",
            "اختر الجهة التي أصدرت التقييم.");
        Add("DiplomaEval.Error.other_name_required",
            "Vul de naam van de instantie in.",
            "Enter the name of the organisation.",
            "Podaj nazwę instytucji.",
            "Completează numele instituției.",
            "أدخل اسم الجهة.");
        Add("DiplomaEval.Error.other_name_too_long",
            "De naam van de instantie is te lang.",
            "The organisation name is too long.",
            "Nazwa instytucji jest za długa.",
            "Numele instituției este prea lung.",
            "اسم الجهة طويل جداً.");
        Add("DiplomaEval.Error.title_too_long",
            "De naam van het diploma is te lang.",
            "The diploma name is too long.",
            "Nazwa dyplomu jest za długa.",
            "Numele diplomei este prea lung.",
            "اسم الشهادة طويل جداً.");
        Add("DiplomaEval.Error.level_required",
            "Vul het Nederlandse niveau in, letterlijk zoals op de waardering.",
            "Enter the Dutch level, exactly as written on the evaluation.",
            "Wpisz poziom niderlandzki, dokładnie jak na ocenie.",
            "Completează nivelul olandez, exact cum scrie pe evaluare.",
            "أدخل المستوى الهولندي كما هو مكتوب على التقييم.");
        Add("DiplomaEval.Error.level_too_long",
            "Het niveau is te lang.",
            "The level text is too long.",
            "Tekst poziomu jest za długi.",
            "Textul nivelului este prea lung.",
            "نص المستوى طويل جداً.");
        Add("DiplomaEval.Error.level_code_invalid",
            "Kies een niveau uit de lijst, of laat de lijst leeg.",
            "Pick a level from the list, or leave the list empty.",
            "Wybierz poziom z listy albo zostaw listę pustą.",
            "Alege un nivel din listă sau lasă lista goală.",
            "اختر مستوى من القائمة أو اترك القائمة فارغة.");
        Add("DiplomaEval.Error.date_required",
            "Vul de datum van de waardering in.",
            "Enter the date of the evaluation.",
            "Podaj datę oceny.",
            "Completează data evaluării.",
            "أدخل تاريخ التقييم.");
        Add("DiplomaEval.Error.date_future",
            "De datum van de waardering kan niet in de toekomst liggen.",
            "The evaluation date cannot be in the future.",
            "Data oceny nie może być w przyszłości.",
            "Data evaluării nu poate fi în viitor.",
            "لا يمكن أن يكون تاريخ التقييم في المستقبل.");
        Add("DiplomaEval.Error.date_too_old",
            "De datum van de waardering is te oud.",
            "The evaluation date is too old.",
            "Data oceny jest za wczesna.",
            "Data evaluării este prea veche.",
            "تاريخ التقييم قديم جداً.");
        Add("DiplomaEval.Error.reference_required",
            "Vul het kenmerk of referentienummer in.",
            "Enter the reference number.",
            "Podaj numer referencyjny.",
            "Completează numărul de referință.",
            "أدخل الرقم المرجعي.");
        Add("DiplomaEval.Error.reference_too_long",
            "Het kenmerk is te lang.",
            "The reference number is too long.",
            "Numer referencyjny jest za długi.",
            "Numărul de referință este prea lung.",
            "الرقم المرجعي طويل جداً.");
        Add("DiplomaEval.Error.too_many",
            "Je kunt maximaal 8 waarderingen bewaren.",
            "You can keep at most 8 evaluations.",
            "Możesz zachować najwyżej 8 ocen.",
            "Poți păstra cel mult 8 evaluări.",
            "يمكنك الاحتفاظ بـ 8 تقييمات كحد أقصى.");
        Add("DiplomaEval.Error.file_empty",
            "Het bestand is leeg.",
            "The file is empty.",
            "Plik jest pusty.",
            "Fișierul este gol.",
            "الملف فارغ.");
        Add("DiplomaEval.Error.file_too_large",
            "Het bestand mag maximaal 5 MB zijn.",
            "The file may be at most 5 MB.",
            "Plik może mieć najwyżej 5 MB.",
            "Fișierul poate avea cel mult 5 MB.",
            "يجب ألا يتجاوز الملف 5 ميغابايت.");
        Add("DiplomaEval.Error.file_type",
            "Upload een PDF of een afbeelding (PNG, JPEG, WEBP of GIF).",
            "Upload a PDF or an image (PNG, JPEG, WEBP or GIF).",
            "Prześlij PDF lub obraz (PNG, JPEG, WEBP lub GIF).",
            "Încarcă un PDF sau o imagine (PNG, JPEG, WEBP sau GIF).",
            "ارفع PDF أو صورة (PNG أو JPEG أو WEBP أو GIF).");
        Add("DiplomaEval.Error.not_found",
            "Deze waardering is niet gevonden.",
            "This evaluation was not found.",
            "Nie znaleziono tej oceny.",
            "Această evaluare nu a fost găsită.",
            "لم يُعثر على هذا التقييم.");
        Add("DiplomaEval.Error.invalid",
            "De waardering is niet geldig.",
            "The evaluation is not valid.",
            "Ocena jest nieprawidłowa.",
            "Evaluarea nu este validă.",
            "التقييم غير صالح.");
    }
}
