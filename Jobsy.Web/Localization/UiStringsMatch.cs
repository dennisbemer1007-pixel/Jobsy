namespace Jobsy.Web.Localization;

public static class UiStringsMatch
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

        Add("Match.Reject",
            "Laten schieten", "Pass",
            "Odrzuć", "Renunță", "تخطي");
        Add("Match.Interest",
            "Snel kennismaken", "Show interest",
            "Pokaż zainteresowanie", "Arată interes", "أبدِ اهتماماً");
        Add("Match.WhyYouFit",
            "Waarom jij past", "Why you fit",
            "Dlaczego pasujesz", "De ce te potrivești", "لماذا تناسبك");
        Add("Match.MoreInfo",
            "Meer info", "More info",
            "Więcej info", "Mai multe info", "مزيد من المعلومات");
        Add("Match.KeyFacts",
            "Kerngegevens", "Key facts",
            "Kluczowe dane", "Date esențiale", "البيانات الأساسية");
        Add("Match.Actions",
            "Match-acties", "Match actions",
            "Akcje dopasowania", "Acțiuni match", "إجراءات المطابقة");
        Add("Match.Percentage",
            "Matchpercentage", "Match percentage",
            "Procent dopasowania", "Procentaj potrivire", "نسبة التطابق");
        Add("Match.CompanyFallback",
            "Bedrijf", "Company",
            "Firma", "Companie", "الشركة");
        Add("Match.TopMatch",
            "Jouw top-match", "Your top match",
            "Twój top match", "Top match-ul tău", "أفضل تطابق لك");
        Add("Match.TopMatchAria",
            "Jouw top-match: {0}% — {1}, {2}. Open je matches",
            "Your top match: {0}% — {1}, {2}. Open your matches",
            "Twój top match: {0}% — {1}, {2}. Otwórz dopasowania",
            "Top match-ul tău: {0}% — {1}, {2}. Deschide potrivirile",
            "أفضل تطابق لك: {0}% — {1}، {2}. افتح مطابقاتك");
        Add("Match.DialogTitle",
            "Jouw top-matches", "Your top matches",
            "Twoje top dopasowania", "Top potrivirile tale", "أفضل مطابقاتك");
        Add("Match.DialogProgress",
            "{0} van {1}", "{0} of {1}",
            "{0} z {1}", "{0} din {1}", "{0} من {1}");
        Add("Match.UpNext",
            "Hierna", "Up next",
            "Następne", "Urmează", "التالي");
        Add("Match.DialogFootnote",
            "Laten schieten zet hem achteraan in deze ronde. Hij verdwijnt niet.",
            "Pass moves it to the end of this round. It does not disappear.",
            "Odrzucenie przenosi ofertę na koniec tej rundy. Nie znika.",
            "Renunțarea o mută la finalul rundei. Nu dispare.",
            "التخطي ينقلها إلى نهاية هذه الجولة. لا تختفي.");
        Add("Match.DeckDone",
            "Je hebt ze allemaal gezien", "You've seen them all",
            "Widziałeś je wszystkie", "Le-ai văzut pe toate", "لقد شاهدتها كلها");
        Add("Match.ToastLiked",
            "Interesse genoteerd", "Interest noted",
            "Zainteresowanie zapisane", "Interes notat", "تم تسجيل الاهتمام");
        Add("Match.ToastSkipped",
            "We laten hem later nog eens zien", "We'll show it again later",
            "Pokażemy ją później jeszcze raz", "O mai arătăm mai târziu", "سنعرضها لاحقًا مرة أخرى");
        Add("Match.DialogClose",
            "Sluiten", "Close",
            "Zamknij", "Închide", "إغلاق");
        Add("Match.StampNope",
            "Nee", "Nope",
            "Nie", "Nu", "لا");
        Add("Match.StampLike",
            "Ja", "Like",
            "Tak", "Da", "نعم");
    }
}
