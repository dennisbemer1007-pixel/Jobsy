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
        Add("Match.ViewVacancy",
            "Bekijk de vacature", "View the vacancy",
            "Zobacz ofertę", "Vezi vacanța", "عرض الوظيفة");
        Add("Match.KeyFacts",
            "Kerngegevens", "Key facts",
            "Kluczowe dane", "Date esențiale", "البيانات الأساسية");
        Add("Match.Actions",
            "Match-acties", "Match actions",
            "Akcje dopasowania", "Acțiuni match", "إجراءات المطابقة");
        Add("Match.Percentage",
            "Matchpercentage", "Match percentage",
            "Procent dopasowania", "Procentaj potrivire", "نسبة التطابق");
        Add("Match.FitLabel",
            "Hoe goed je past", "How well you fit",
            "Jak pasujesz", "Cât de bine te potrivești", "مدى ملاءمتك");
        Add("Match.CompanyFallback",
            "Bedrijf", "Company",
            "Firma", "Companie", "الشركة");
        Add("Match.TopMatch",
            "Jouw top-match", "Your top match",
            "Twój top match", "Top match-ul tău", "أفضل تطابق لك");
        Add("Match.TopMatchAria",
            "Jouw top-match: {0} — {1}, {2}. Open je matches",
            "Your top match: {0} — {1}, {2}. Open your matches",
            "Twój top match: {0} — {1}, {2}. Otwórz dopasowania",
            "Top match-ul tău: {0} — {1}, {2}. Deschide potrivirile",
            "أفضل تطابق لك: {0} — {1}، {2}. افتح مطابقاتك");
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
        Add("Match.Gate.AuthTitle",
            "Ontgrendel Match", "Unlock Match",
            "Odblokuj Match", "Deblochează Match", "فعّل المطابقة");
        Add("Match.Gate.AuthLead",
            "Log in als kandidaat en rond je startprofiel af om relevante vacatures te swipen.",
            "Sign in as a candidate and finish your starter profile to swipe relevant vacancies.",
            "Zaloguj się jako kandydat i dokończ profil startowy, aby przesuwać oferty.",
            "Autentifică-te ca candidat și finalizează profilul de start pentru a da swipe.",
            "سجّل الدخول كمرشح وأكمل ملف البداية لتمرير الوظائف.");
        Add("Match.Gate.ProfileTitle",
            "Maak Match compleet", "Complete Match",
            "Dokończ Match", "Finalizează Match", "أكمل المطابقة");
        Add("Match.Gate.ProfileLead",
            "Rond je startprofiel af (ongeveer 6 minuten). Daarna tonen we vacatures die bij jou passen. Opleiding mag Geen zijn.",
            "Finish your starter profile (about 6 minutes). Then we show vacancies that fit you. Education may be None.",
            "Dokończ profil startowy (ok. 6 min). Potem pokażemy pasujące oferty. Wykształcenie może być Brak.",
            "Finalizează profilul de start (circa 6 min). Apoi arătăm joburi potrivite. Studiile pot fi Niciunul.",
            "أكمل ملف البداية (حوالي 6 دقائق). ثم نعرض وظائف تناسبك. يمكن أن تكون المؤهلات «لا شيء».");
        Add("Match.Gate.Progress",
            "{0} van {1} klaar", "{0} of {1} done",
            "{0} z {1} gotowe", "{0} din {1} gata", "{0} من {1} جاهزة");
        Add("Match.Gate.Check.BasicsTitle",
            "Basisgegevens", "Basic details",
            "Dane podstawowe", "Date de bază", "البيانات الأساسية");
        Add("Match.Gate.Check.BasicsLead",
            "Naam, beschikbaarheid en reistijd of vervoer",
            "Name, availability, and travel time or transport",
            "Imię, dostępność i czas dojazdu lub transport",
            "Nume, disponibilitate și timp de călătorie sau transport",
            "الاسم والتوفر ووقت السفر أو وسيلة النقل");
        Add("Match.Gate.Check.EducationTitle",
            "Opleidingsniveau", "Education level",
            "Poziom wykształcenia", "Nivel de studii", "المستوى التعليمي");
        Add("Match.Gate.Check.EducationLead",
            "Kies je niveau of Geen",
            "Choose your level or None",
            "Wybierz poziom lub Brak",
            "Alege nivelul sau Niciunul",
            "اختر مستواك أو لا شيء");
        Add("Match.Gate.Check.WizardTitle",
            "Startprofiel", "Starter profile",
            "Profil startowy", "Profil de start", "ملف البداية");
        Add("Match.Gate.Check.WizardLead",
            "Korte wizard met mini-tests voor je eerste indruk",
            "Short wizard with mini tests for your first impression",
            "Krótki kreator z mini testami na pierwsze wrażenie",
            "Scurt wizard cu mini-teste pentru prima impresie",
            "معالج قصير مع اختبارات مصغرة لانطباعك الأول");
        Add("Match.Gate.BroaderSearch",
            "Wil je breder zoeken of andere typen vacatures vinden? Ga dan naar de banenkaart.",
            "Want to search more broadly or find other vacancy types? Go to the job map.",
            "Chcesz szukać szerzej lub innych ofert? Idź do mapy pracy.",
            "Vrei să cauți mai larg sau alte joburi? Mergi la harta joburilor.",
            "تريد بحثًا أوسع أو أنواع وظائف أخرى؟ اذهب إلى خريطة الوظائف.");
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
