namespace Jobsy.Web.Localization;

public static class UiStringsApplyBasics
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string dutch, string english, string polish, string romanian, string arabic)
        {
            nl[key] = dutch;
            en[key] = english;
            pl[key] = polish;
            ro[key] = romanian;
            ar[key] = arabic;
        }

        Add("Apply.Gap.Name", "Vul je voor- en achternaam in.", "Enter your first and last name.", "Podaj imię i nazwisko.", "Completează prenumele și numele.", "أدخل اسمك الأول واسم العائلة.");
        Add("Apply.Gap.BirthDate", "Vul je geboortedatum in.", "Enter your date of birth.", "Podaj datę urodzenia.", "Completează data nașterii.", "أدخل تاريخ ميلادك.");
        Add("Apply.Gap.Location", "Vul je woonplaats of adres in (Even kennismaken).", "Enter your city or address (Getting to know you).", "Podaj miasto lub adres (Poznajmy się).", "Completează orașul sau adresa (Să ne cunoaștem).", "أدخل مدينتك أو عنوانك (التعارف).");
        Add("Apply.Gap.Hours", "Geef aan hoeveel uur per week je wilt werken.", "Set how many hours per week you want to work.", "Podaj, ile godzin tygodniowo chcesz pracować.", "Indică câte ore pe săptămână vrei să lucrezi.", "حدد عدد ساعات العمل أسبوعياً.");
        Add("Apply.Gap.Availability", "Geef aan wanneer je kunt werken (dagen en dagdelen).", "Set when you can work (days and time slots).", "Podaj, kiedy możesz pracować (dni i pory).", "Indică când poți lucra (zile și intervale).", "حدد متى يمكنك العمل (أيام وفترات).");
        Add("Apply.Gap.AvailabilityStart", "Kies vanaf wanneer je kunt starten.", "Choose when you can start.", "Wybierz, od kiedy możesz zacząć.", "Alege de când poți începe.", "اختر متى يمكنك البدء.");
        Add("Apply.Gap.WorkStatus", "Kies je huidige werkstatus.", "Choose your current work status.", "Wybierz aktualny status zawodowy.", "Alege statusul tău profesional actual.", "اختر وضعك المهني الحالي.");
        Add("Apply.Gap.OpenForWork", "Zet 'Beschikbaar voor werk' aan in je paspoort.", "Turn on 'Available for work' in your passport.", "Włącz „Dostępny/a do pracy” w paszporcie.", "Activează „Disponibil pentru muncă” în pașaport.", "فعّل «متاح للعمل» في جوازك.");
        Add(
            "Apply.BasicsBlockedLead",
            "Vul eerst je basisprofiel aan (Even kennismaken). Daarna stuur je mee wat je nu hebt: je Lobsy-CV. Tests zijn niet verplicht.",
            "Complete your basic profile first (Getting to know you). Then you apply with your Lobsy CV. Tests are not required.",
            "Najpierw uzupełnij profil podstawowy. Potem aplikujesz z CV Lobsy. Testy nie są wymagane.",
            "Finalizează mai întâi profilul de bază. Apoi aplici cu CV Lobsy. Testele nu sunt obligatorii.",
            "أكمل ملفك الأساسي أولاً. ثم تتقدم بسيرة Lobsy. الاختبارات ليست إلزامية.");
    }
}
