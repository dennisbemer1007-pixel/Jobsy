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
    }
}
