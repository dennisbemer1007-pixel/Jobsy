namespace Jobsy.Web.Localization;

/// <summary>Candidate jobs (banenkaart / lijst / vacature / sollicitaties / bewaard / Match) strings.</summary>
public static class UiStringsKandidaatBanen
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

        // Fit
        Add("Kb.Fit.Percent",
            "{0}% past bij jou", "{0}% fit for you",
            "{0}% pasuje do ciebie", "{0}% ți se potrivește", "{0}% تناسبك");
        Add("Kb.Fit.Strong",
            "Sterke match", "Strong fit",
            "Silne dopasowanie", "Potrivire puternică", "تطابق قوي");
        Add("Kb.Fit.Good",
            "Goede match", "Good fit",
            "Dobre dopasowanie", "Potrivire bună", "تطابق جيد");
        Add("Kb.Fit.Some",
            "Enige match", "Some fit",
            "Częściowe dopasowanie", "Potrivire parțială", "تطابق جزئي");
        Add("Kb.Fit.Gate",
            "Maak je paspoort af", "Finish your passport",
            "Uzupełnij paszport", "Finalizează pașaportul", "أكمل جوازك");
        Add("Kb.Fit.GateHint",
            "Doe de cultuur- of waardentest, dan zie je hoe goed banen bij je passen.",
            "Take the culture or values test to see how well jobs fit you.",
            "Zrób test kultury lub wartości, a zobaczysz dopasowanie ofert.",
            "Fă testul de cultură sau valori ca să vezi cât de bine ți se potrivesc joburile.",
            "أجرِ اختبار الثقافة أو القيم لترى مدى ملاءمة الوظائف لك.");
        Add("Kb.Fit.SparkleAria",
            "Fit-indicatie", "Fit indicator",
            "Wskaźnik dopasowania", "Indicator de potrivire", "مؤشر الملاءمة");

        // Ranking / dislikes
        Add("Kb.Rank.Lower",
            "Staat lager: {0}", "Ranks lower: {0}",
            "Niżej: {0}", "Clasat mai jos: {0}", "ترتيب أدنى: {0}");

        // Travel
        Add("Kb.Travel.Minutes",
            "{0} min {1}", "{0} mins {1}",
            "{0} minuty {1}", "{0} min. {1}", "{0} د {1}");
        Add("Kb.Travel.Approx",
            "ongeveer", "about",
            "około", "aproximativ", "حوالي");
        Add("Kb.Travel.ToBureau",
            "Reistijd tot de vestiging van het bureau · werklocatie in de regio {0}",
            "Travel time to the agency branch · workplace in the {0} area",
            "Czas dojazdu do oddziału biura · lokalizacja w regionie {0}",
            "Timp până la sediul agenției · locație în zona {0}",
            "وقت الوصول إلى فرع الوكالة · موقع العمل في منطقة {0}");
        Add("Kb.Travel.Aria",
            "Reistijd {0} minuten {1}", "Travel time {0} minutes {1}",
            "Czas dojazdu {0} minut {1}", "Timp de deplasare {0} minute {1}", "وقت الوصول {0} دقيقة {1}");

        // Badges
        Add("Kb.Badge.More",
            "+{0}", "+{0}",
            "+{0}", "+{0}", "+{0}");
        Add("Kb.Badge.MoreAria",
            "Nog {0} badges: {1}", "{0} more badges: {1}",
            "Jeszcze {0} odznaki: {1}", "Încă {0} badge-uri: {1}", "{0} شارات إضافية: {1}");

        // Status (candidate wording)
        Add("Kb.Status.Pending",
            "Verstuurd", "Sent",
            "Wysłano", "Trimis", "أُرسل");
        Add("Kb.Status.Accepted",
            "In behandeling", "In review",
            "W trakcie", "În analiză", "قيد المراجعة");
        Add("Kb.Status.EmployerContacting",
            "Uitgenodigd", "Invited",
            "Zaproszono", "Invitat", "مدعو");
        Add("Kb.Status.Hired",
            "Aangenomen", "Hired",
            "Zatrudniony", "Angajat", "تم التوظيف");
        Add("Kb.Status.Rejected",
            "Niet gekozen", "Not selected",
            "Nie wybrano", "Neselectat", "لم تُختر");
        Add("Kb.Status.FilledElsewhere",
            "Vergeven aan iemand anders", "Filled by someone else",
            "Obsada przez kogoś innego", "Ocupat de altcineva", "شُغلت من شخص آخر");
        Add("Kb.Status.Withdrawn",
            "Ingetrokken", "Withdrawn",
            "Wycofano", "Retras", "تم السحب");

        // Timeline (07)
        Add("Kb.Timeline.Sent",
            "Verstuurd", "Sent",
            "Wysłano", "Trimis", "أُرسل");
        Add("Kb.Timeline.Seen",
            "Gezien door werkgever", "Seen by employer",
            "Widziane przez pracodawcę", "Văzut de angajator", "شاهده صاحب العمل");
        Add("Kb.Timeline.Interview",
            "Gesprek", "Interview",
            "Rozmowa", "Interviu", "مقابلة");
        Add("Kb.Timeline.Outcome",
            "Uitslag", "Outcome",
            "Wynik", "Rezultat", "النتيجة");
        Add("Kb.Timeline.StatusChanged",
            "Status bijgewerkt", "Status updated",
            "Status zaktualizowany", "Status actualizat", "تم تحديث الحالة");

        // Saved (07)
        Add("Kb.Saved.State",
            "Bewaard", "Saved",
            "Zapisane", "Salvat", "محفوظ");
        Add("Kb.Saved.Applied",
            "Gesolliciteerd", "Applied",
            "Aplikowano", "Aplicat", "تم التقديم");
        Add("Kb.Saved.Closed",
            "Gesloten", "Closed",
            "Zamknięte", "Închis", "مغلق");

        // Moved from VacancyDiscovery / VacancyDetail hardcoded Dutch (02.7)
        Add("Kb.Legend.Hide",
            "Legenda verbergen", "Hide legend",
            "Ukryj legendę", "Ascunde legenda", "إخفاء المفتاح");
        Add("Kb.Legend.Show",
            "Legenda tonen", "Show legend",
            "Pokaż legendę", "Arată legenda", "إظهار المفتاح");
        Add("Kb.Legend.Title",
            "Legenda", "Legend",
            "Legenda mapy", "Legendă", "المفتاح");
        Add("Kb.Legend.CategoriesAria",
            "Legenda vacaturecategorieën", "Vacancy category legend",
            "Legenda kategorii ofert", "Legendă categorii joburi", "مفتاح فئات الوظائف");
        Add("Kb.MyVacancies.Hide",
            "Verberg mijn vacatures", "Hide my vacancies",
            "Ukryj moje oferty", "Ascunde joburile mele", "إخفاء وظائفي");
        Add("Kb.MyVacancies.Show",
            "Toon mijn vacatures", "Show my vacancies",
            "Pokaż moje oferty", "Arată joburile mele", "إظهار وظائفي");
        Add("Kb.Video.Loading",
            "Video laden", "Loading video",
            "Ładowanie wideo", "Se încarcă video", "جارٍ تحميل الفيديو");

        // Map JS transport verbs / fallbacks (banenkaart path)
        Add("Kb.Map.NoVacancies",
            "Geen vacatures", "No vacancies",
            "Brak ofert", "Nicio ofertă", "لا وظائف");
        Add("Kb.Map.VacancyFallback",
            "Vacature", "Vacancy",
            "Oferta", "Ofertă", "وظيفة");
    }
}
