namespace Jobsy.Web.Localization;

/// <summary>Central mascot encouragement copy (action prompts + rotating “Wist je dat…” tips). Edit here.</summary>
public static class UiStringsEncourage
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

        Add("Encourage.Dismiss", "Sluiten", "Close", "Zamknij", "Închide", "إغلاق");

        Add(
            "Encourage.Action.Tests.Message",
            "Doe de korte tests en zie hoe goed je past.",
            "Take the short tests and see how well you fit.",
            "Zrób krótkie testy i zobacz, jak dobrze pasujesz.",
            "Fă testele scurte și vezi cât de bine te potrivești.",
            "أجرِ الاختبارات القصيرة واكتشف مدى ملاءمتك.");
        Add(
            "Encourage.Action.Tests.Link",
            "Naar de tests",
            "Go to tests",
            "Do testów",
            "La teste",
            "إلى الاختبارات");

        Add(
            "Encourage.Action.Profile.Message",
            "Vergroot je kans op een gesprek: maak je profiel compleet.",
            "Improve your chance of an interview: complete your profile.",
            "Zwiększ szansę na rozmowę: uzupełnij profil.",
            "Mărește șansa la interviu: completează profilul.",
            "زِد فرصتك في المقابلة: أكمل ملفك.");
        Add(
            "Encourage.Action.Profile.Link",
            "Paspoort aanvullen",
            "Complete passport",
            "Uzupełnij paszport",
            "Completează pașaportul",
            "أكمل الجواز");

        Add(
            "Encourage.Tip.TestsMatch",
            "Wist je dat… je met de gratis tests precies ziet welke banen bij je passen?",
            "Did you know… the free tests show which jobs fit you?",
            "Czy wiesz, że… darmowe testy pokazują, które oferty do Ciebie pasują?",
            "Știai că… testele gratuite îți arată ce joburi ți se potrivesc?",
            "هل تعلم… أن الاختبارات المجانية تُظهر الوظائف المناسبة لك؟");
        Add(
            "Encourage.Tip.TestsMatch.Link",
            "Start met de tests",
            "Start the tests",
            "Zacznij testy",
            "Începe testele",
            "ابدأ الاختبارات");

        Add(
            "Encourage.Tip.PassportApply",
            "Wist je dat… je na de gratis tests een compleet paspoort meestuurt in plaats van alleen je cv?",
            "Did you know… after the free tests you send a full passport instead of only your CV?",
            "Czy wiesz, że… po darmowych testach wysyłasz pełny paszport zamiast samego CV?",
            "Știai că… după testele gratuite trimiți un pașaport complet, nu doar CV-ul?",
            "هل تعلم… أنك بعد الاختبارات المجانية ترسل جوازاً كاملاً بدلاً من السيرة فقط؟");
        Add(
            "Encourage.Tip.PassportApply.Link",
            "Profiel aanvullen",
            "Complete profile",
            "Uzupełnij profil",
            "Completează profilul",
            "أكمل الملف");

        Add(
            "Encourage.Tip.DayInLife",
            "Wist je dat… je bij elke vacature kunt bekijken hoe een dag in het leven eruitziet?",
            "Did you know… you can see what a day in the life looks like on every vacancy?",
            "Czy wiesz, że… przy każdej ofercie możesz zobaczyć, jak wygląda dzień w pracy?",
            "Știai că… la fiecare job poți vedea cum arată o zi din viață?",
            "هل تعلم… أنك تستطيع عند كل وظيفة رؤية شكل يوم في العمل؟");
        Add(
            "Encourage.Tip.DayInLife.Link",
            "Scroll naar boven",
            "Scroll up",
            "Przewiń w górę",
            "Derulează sus",
            "مرّر لأعلى");

        Add(
            "Encourage.Tip.TestsPause",
            "Wist je dat… de korte tests maar een paar minuten duren en je ze tussendoor kunt pauzeren?",
            "Did you know… the short tests only take a few minutes and you can pause in between?",
            "Czy wiesz, że… krótkie testy trwają kilka minut i możesz je wstrzymać?",
            "Știai că… testele scurte durează câteva minute și le poți pune pe pauză?",
            "هل تعلم… أن الاختبارات القصيرة تستغرق دقائق ويمكنك إيقافها مؤقتاً؟");
        Add(
            "Encourage.Tip.TestsPause.Link",
            "Naar de tests",
            "Go to tests",
            "Do testów",
            "La teste",
            "إلى الاختبارات");
    }
}
