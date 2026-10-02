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

        // Why line fragments (04)
        Add("Kb.Why.culture",
            "Je past bij de cultuur", "You fit the culture",
            "Pasujesz do kultury", "Te potrivești culturii", "تناسب الثقافة");
        Add("Kb.Why.values",
            "Je waarden sluiten aan", "Your values align",
            "Twoje wartości pasują", "Valorile tale se potrivesc", "قيمك متوافقة");
        Add("Kb.Why.competency",
            "Je competenties passen", "Your competencies fit",
            "Twoje kompetencje pasują", "Competențele tale se potrivesc", "كفاءاتك مناسبة");
        Add("Kb.Why.interest",
            "Dit past bij je interesses", "This matches your interests",
            "To pasuje do Twoich zainteresowań", "Se potrivește intereselor tale", "هذا يلائم اهتماماتك");
        Add("Kb.Why.travel",
            "Goede reistijd", "Good travel time",
            "Dobry czas dojazdu", "Timp bun de deplasare", "وقت وصول جيد");
        Add("Kb.Why.hours",
            "Je uren passen", "Your hours fit",
            "Twoje godziny pasują", "Orele tale se potrivesc", "ساعاتك مناسبة");

        // Dislike reasons (04; Dep D — keys ready when paspoort 06 lands)
        Add("Kb.Dislike.night-shifts",
            "nachtdienst", "night shifts",
            "nocne zmiany", "ture de noapte", "نوبات ليلية");
        Add("Kb.Dislike.weekend-work",
            "weekendwerk", "weekend work",
            "praca w weekend", "muncă în weekend", "عمل في عطلة نهاية الأسبوع");
        Add("Kb.Dislike.customer-facing",
            "klantcontact", "customer-facing work",
            "kontakt z klientem", "lucru cu clienții", "التعامل مع العملاء");

        // DNA bars (04 detail)
        Add("Kb.Dna.Culture",
            "Cultuur", "Culture",
            "Kultura", "Cultură", "الثقافة");
        Add("Kb.Dna.Values",
            "Waarden", "Values",
            "Wartości", "Valori", "القيم");
        Add("Kb.Dna.Competencies",
            "Competenties", "Competencies",
            "Kompetencje", "Competențe", "الكفاءات");
        Add("Kb.Dna.Interests",
            "Interesses", "Interests",
            "Zainteresowania", "Interese", "الاهتمامات");
        Add("Kb.Dna.NotDone",
            "Nog niet gedaan", "Not done yet",
            "Jeszcze nie zrobione", "Încă nefăcut", "لم يُنجز بعد");
        Add("Kb.Dna.NewForYou",
            "Nieuw voor jou", "New for you",
            "Nowe dla Ciebie", "Nou pentru tine", "جديد لك");
        Add("Kb.Fit.GateMatch",
            "Doe de cultuur- of waardentest om Match te gebruiken.",
            "Take the culture or values test to use Match.",
            "Zrób test kultury lub wartości, by korzystać z Match.",
            "Fă testul de cultură sau valori ca să folosești Match.",
            "أجرِ اختبار الثقافة أو القيم لاستخدام المطابقة.");

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
        // KB-FALLBACK(A): simpler label when intermediair D5 region label is unavailable
        Add("Kb.Travel.ToBureauSimple",
            "Reistijd tot het bureau",
            "Travel time to the agency",
            "Czas dojazdu do biura",
            "Timp până la agenție",
            "وقت الوصول إلى الوكالة");
        Add("Kb.Travel.Unknown",
            "Reistijd onbekend",
            "Travel time unknown",
            "Czas dojazdu nieznany",
            "Timp de deplasare necunoscut",
            "وقت الوصول غير معروف");
        Add("Kb.Travel.Aria",
            "Reistijd {0} minuten {1}", "Travel time {0} minutes {1}",
            "Czas dojazdu {0} minut {1}", "Timp de deplasare {0} minute {1}", "وقت الوصول {0} دقيقة {1}");

        // Uitzendbureau hidden mode (05 / Dep A fallback)
        Add("Kb.Via.Bureau",
            "via uitzendbureau {0}",
            "via agency {0}",
            "przez agencję {0}",
            "prin agenția {0}",
            "عبر وكالة {0}");
        Add("Kb.Hidden.Info",
            "Bij welk bedrijf je gaat werken, hoor je van {0} na je sollicitatie. De kaart toont het kantoor van {0}.",
            "Which company you will work for is told by {0} after you apply. The map shows the {0} office.",
            "Dla jakiej firmy będziesz pracować, dowiesz się od {0} po aplikacji. Mapa pokazuje biuro {0}.",
            "La ce firmă vei lucra afli de la {0} după ce aplici. Harta arată biroul {0}.",
            "أي شركة ستعمل لديها تخبرك بها {0} بعد التقديم. الخريطة تعرض مكتب {0}.");
        Add("Kb.Map.ShowsBureau",
            "Kaart toont de vestiging van het bureau",
            "Map shows the agency branch",
            "Mapa pokazuje oddział biura",
            "Harta arată sediul agenției",
            "الخريطة تعرض فرع الوكالة");
        Add("Kb.Map.BureauPin",
            "Kantoor {0}",
            "{0} office",
            "Biuro {0}",
            "Biroul {0}",
            "مكتب {0}");
        Add("Kb.Map.TravelNoteHidden",
            "{0} min {1} naar het kantoor. De echte werkplek kan anders zijn.",
            "{0} min {1} to the office. The real workplace may differ.",
            "{0} min {1} do biura. Prawdziwe miejsce pracy może być inne.",
            "{0} min {1} până la birou. Locul real de muncă poate diferi.",
            "{0} د {1} إلى المكتب. قد يختلف مكان العمل الحقيقي.");

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
        Add("Kb.Timeline.WithdrawnOn",
            "Ingetrokken op {0}", "Withdrawn on {0}",
            "Wycofano {0}", "Retras pe {0}", "تم السحب في {0}");
        Add("Kb.Timeline.LegacySub",
            "Eerdere stappen zijn niet bewaard", "Earlier steps were not saved",
            "Wcześniejsze kroki nie zostały zapisane", "Pașii anteriori nu au fost salvați", "الخطوات السابقة غير محفوظة");
        Add("Kb.Timeline.TapForSteps",
            "Gezien op {0} · tik voor alle stappen", "Seen on {0} · tap for all steps",
            "Widziane {0} · dotknij, by zobaczyć kroki", "Văzut pe {0} · atinge pentru pași", "شوهد في {0} · اضغط لكل الخطوات");
        Add("Kb.Timeline.FinishedOn",
            "Afgerond op {0}", "Finished on {0}",
            "Zakończono {0}", "Finalizat pe {0}", "اكتمل في {0}");
        Add("Kb.Timeline.AfterInterview",
            "Na het gesprek", "After the interview",
            "Po rozmowie", "După interviu", "بعد المقابلة");

        // Wat nu? (07)
        Add("Kb.Next.Title",
            "Wat nu?", "What's next?",
            "Co dalej?", "Ce urmează?", "ماذا الآن؟");
        Add("Kb.Next.Pending",
            "Je hoort het hier zodra de werkgever reageert.",
            "You'll hear here as soon as the employer responds.",
            "Dowiesz się tutaj, gdy pracodawca odpowie.",
            "Afli aici de îndată ce angajatorul răspunde.",
            "ستعلم هنا فور رد صاحب العمل.");
        Add("Kb.Next.Accepted",
            "De werkgever heeft je gegevens. Houd je telefoon en mail in de gaten.",
            "The employer has your details. Keep an eye on your phone and email.",
            "Pracodawca ma Twoje dane. Sprawdzaj telefon i e-mail.",
            "Angajatorul are datele tale. Urmărește telefonul și e-mailul.",
            "صاحب العمل لديه بياناتك. راقب هاتفك وبريدك.");
        Add("Kb.Next.EmployerContacting",
            "Oefen je gesprek met Lobsy",
            "Practice your interview with Lobsy",
            "Poćwicz rozmowę z Lobsy",
            "Exersează interviul cu Lobsy",
            "تدرّب على مقابلتك مع Lobsy");
        Add("Kb.Next.Practice",
            "Oefenen", "Practice",
            "Ćwicz", "Exersează", "تدرّب");
        Add("Kb.Next.Hired",
            "Gefeliciteerd! Spreek je startdatum af met de werkgever.",
            "Congratulations! Agree a start date with the employer.",
            "Gratulacje! Uzgodnij datę startu z pracodawcą.",
            "Felicitări! Stabilește data de început cu angajatorul.",
            "تهانينا! اتفق على تاريخ البدء مع صاحب العمل.");
        Add("Kb.Next.Rejected",
            "Jammer. Dit zegt niets over wie jij bent. Er zijn banen die hierop lijken.",
            "Sorry. This says nothing about who you are. There are similar jobs.",
            "Szkoda. To nic nie mówi o Tobie. Są podobne oferty.",
            "Păcat. Asta nu spune nimic despre tine. Există joburi asemănătoare.",
            "للأسف. هذا لا يقول شيئًا عنك. هناك وظائف مشابهة.");
        Add("Kb.Next.FilledElsewhere",
            "Jammer. Dit zegt niets over wie jij bent. Er zijn banen die hierop lijken.",
            "Sorry. This says nothing about who you are. There are similar jobs.",
            "Szkoda. To nic nie mówi o Tobie. Są podobne oferty.",
            "Păcat. Asta nu spune nimic despre tine. Există joburi asemănătoare.",
            "للأسف. هذا لا يقول شيئًا عنك. هناك وظائف مشابهة.");
        Add("Kb.Next.ViewSimilar",
            "Bekijk ze", "View them",
            "Zobacz je", "Vezi-le", "اعرضها");

        // Steps legend (07)
        Add("Kb.Steps.Title",
            "Wat betekenen de stappen?", "What do the steps mean?",
            "Co oznaczają kroki?", "Ce înseamnă pașii?", "ماذا تعني الخطوات؟");
        Add("Kb.Steps.Sent",
            "Je sollicitatie is verstuurd naar de werkgever.",
            "Your application was sent to the employer.",
            "Twoja aplikacja została wysłana do pracodawcy.",
            "Aplicația ta a fost trimisă angajatorului.",
            "أُرسل طلبك إلى صاحب العمل.");
        Add("Kb.Steps.Seen",
            "De werkgever heeft je sollicitatie geopend.",
            "The employer opened your application.",
            "Pracodawca otworzył Twoją aplikację.",
            "Angajatorul a deschis aplicația ta.",
            "فتح صاحب العمل طلبك.");
        Add("Kb.Steps.Interview",
            "Je bent uitgenodigd voor een gesprek.",
            "You were invited for an interview.",
            "Zaproszono Cię na rozmowę.",
            "Ai fost invitat la un interviu.",
            "دُعيت إلى مقابلة.");
        Add("Kb.Steps.Outcome",
            "De uitslag: aangenomen, niet gekozen, of vergeven.",
            "The outcome: hired, not selected, or filled elsewhere.",
            "Wynik: zatrudnienie, nie wybrano lub obsadzono.",
            "Rezultatul: angajat, neselectat sau ocupat.",
            "النتيجة: توظيف أو لم تُختر أو شُغلت.");

        // Filters (07)
        Add("Kb.Apps.FilterAll",
            "Alles", "All",
            "Wszystkie", "Toate", "الكل");
        Add("Kb.Apps.FilterRunning",
            "Loopt nog", "In progress",
            "W toku", "În curs", "جارٍ");
        Add("Kb.Apps.FilterDone",
            "Afgerond", "Finished",
            "Zakończone", "Finalizate", "مكتمل");

        // Saved (07)
        Add("Kb.Saved.State",
            "Bewaard", "Saved",
            "Zapisane", "Salvat", "محفوظ");
        Add("Kb.Saved.Applied",
            "Gesolliciteerd", "Applied",
            "Aplikowano", "Ai aplicat", "تم التقديم");
        Add("Kb.Saved.Closed",
            "Gesloten", "Closed",
            "Zamknięte", "Închis", "مغلق");
        Add("Kb.Saved.Open",
            "Open", "Open",
            "Otwarte", "Deschis", "مفتوح");
        Add("Kb.Saved.ClosingSoon",
            "Sluit over {0} dagen", "Closes in {0} days",
            "Zamyka się za {0} dni", "Se închide în {0} zile", "يُغلق خلال {0} أيام");
        Add("Kb.Saved.Fulfilled",
            "Baan is al vergeven", "Job already filled",
            "Oferta już obsadzona", "Jobul e deja ocupat", "الوظيفة مشغولة");
        Add("Kb.Saved.Title",
            "Bewaard", "Saved",
            "Zapisane", "Salvate", "محفوظ");
        Add("Kb.Saved.Lead",
            "Banen die je wilt onthouden. Ook de banen die je in Match bewaarde.",
            "Jobs you want to remember. Including jobs you saved in Match.",
            "Oferty, które chcesz zapamiętać. Także z Match.",
            "Joburi pe care vrei să le ții minte. Inclusiv din Match.",
            "وظائف تريد تذكّرها. بما فيها من Match.");
        Add("Kb.Saved.Count",
            "{0} banen", "{0} jobs",
            "{0} ofert", "{0} joburi", "{0} وظائف");
        Add("Kb.Saved.FilterAll",
            "Alles", "All",
            "Wszystkie", "Toate", "الكل");
        Add("Kb.Saved.FilterOpen",
            "Nog open", "Still open",
            "Jeszcze otwarte", "Încă deschise", "ما زالت مفتوحة");
        Add("Kb.Saved.FilterClosed",
            "Gesloten", "Closed",
            "Zamknięte", "Închise", "مغلقة");
        Add("Kb.Saved.SortBestFit",
            "Past het best", "Best fit",
            "Najlepsze dopasowanie", "Cea mai bună potrivire", "الأفضل ملاءمة");
        Add("Kb.Saved.SortNewest",
            "Laatst bewaard", "Recently saved",
            "Ostatnio zapisane", "Salvate recent", "آخر المحفوظات");
        Add("Kb.Saved.SortClosing",
            "Sluit het eerst", "Closing soonest",
            "Najszybciej zamykane", "Se închid cele mai curând", "الأقرب للإغلاق");
        Add("Kb.Saved.Apply",
            "Solliciteer", "Apply",
            "Aplikuj", "Aplică", "قدّم");
        Add("Kb.Saved.View",
            "Bekijk", "View",
            "Zobacz", "Vezi", "عرض");
        Add("Kb.Saved.Similar",
            "Zoek banen die hierop lijken", "Find similar jobs",
            "Szukaj podobnych ofert", "Caută joburi asemănătoare", "ابحث عن وظائف مشابهة");
        Add("Kb.Saved.Remove",
            "Weg", "Remove",
            "Usuń", "Elimină", "إزالة");
        Add("Kb.Saved.Unsave",
            "Uit Bewaard halen", "Remove from Saved",
            "Usuń z zapisanych", "Scoate din Salvate", "إزالة من المحفوظات");
        Add("Kb.Saved.SavedOn",
            "Bewaard {0}", "Saved {0}",
            "Zapisano {0}", "Salvat {0}", "حُفظ في {0}");
        Add("Kb.Saved.UndoToast",
            "Verwijderd uit bewaard. Ongedaan maken?",
            "Removed from saved. Undo?",
            "Usunięto z zapisanych. Cofnąć?",
            "Eliminat din salvate. Anulezi?",
            "أُزيل من المحفوظات. تراجع؟");
        Add("Kb.Saved.Undo",
            "Ongedaan maken", "Undo",
            "Cofnij", "Anulează", "تراجع");

        // Match refresh (08)
        Add("Kb.Match.Skipped",
            "We laten hem later nog eens zien",
            "We'll show it again later",
            "Pokażemy ją później jeszcze raz",
            "O mai arătăm mai târziu",
            "سنعرضها لاحقًا مرة أخرى");
        Add("Kb.Match.NoWrongChoice",
            "je kunt niets fout doen",
            "you can't go wrong",
            "nic nie zepsujesz",
            "nu poți greși",
            "لا يمكنك أن تخطئ");
        Add("Kb.Match.ViewSaved",
            "Bekijk bewaarde banen",
            "View saved jobs",
            "Zobacz zapisane oferty",
            "Vezi joburile salvate",
            "اعرض الوظائف المحفوظة");
        Add("Kb.Match.BackToMap",
            "Terug naar de kaart",
            "Back to the map",
            "Wróć do mapy",
            "Înapoi la hartă",
            "العودة إلى الخريطة");
        Add("Kb.Match.KeyboardLead",
            "Toetsen:",
            "Keys:",
            "Klawisze:",
            "Taste:",
            "المفاتيح:");
        Add("Kb.Match.SwipeHint",
            "Veeg naar links of rechts. Laten schieten zet de baan achteraan. Hij verdwijnt niet.",
            "Swipe left or right. Pass moves the job to the end. It does not disappear.",
            "Przesuń w lewo lub w prawo. Odrzucenie przenosi ofertę na koniec. Nie znika.",
            "Glisează stânga sau dreapta. Renunțarea mută jobul la final. Nu dispare.",
            "اسحب يسارًا أو يمينًا. التخطي ينقل الوظيفة إلى النهاية. لا تختفي.");
        Add("Kb.Match.EmptyTitle",
            "Geen passende matches",
            "No matching jobs",
            "Brak pasujących ofert",
            "Nicio potrivire",
            "لا مطابقات");
        Add("Kb.Match.EmptyLead",
            "Er liggen nu geen vacatures die bij jouw opleiding en reistijd passen.",
            "There are no vacancies that match your education and travel time right now.",
            "Nie ma teraz ofert pasujących do Twojego wykształcenia i czasu dojazdu.",
            "Momentan nu există oferte potrivite cu educația și timpul de deplasare.",
            "لا توجد وظائف تناسب تعليمك ووقت وصولك الآن.");
        Add("Kb.Match.Reload",
            "Opnieuw laden",
            "Reload",
            "Załaduj ponownie",
            "Reîncarcă",
            "إعادة التحميل");
        Add("Kb.Match.HoursUnit",
            "uur",
            "hrs",
            "godz.",
            "ore",
            "ساعة");

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

        // Calm vacancy sheet / popup (kandidaat-polish 04)
        Add("Kb.Map.JobsAtPlace",
            "{count} banen", "{count} jobs",
            "{count} ofert", "{count} joburi", "{count} وظائف");
        Add("Kb.Map.JobAtPlace",
            "1 baan", "1 job",
            "1 oferta", "1 job", "وظيفة واحدة");
        Add("Kb.Map.JobsAtPlaceWithPlace",
            "{count} banen in {place}", "{count} jobs in {place}",
            "{count} ofert w {place}", "{count} joburi în {place}", "{count} وظائف في {place}");
        Add("Kb.Map.JobAtPlaceWithPlace",
            "1 baan in {place}", "1 job in {place}",
            "1 oferta w {place}", "1 job în {place}", "وظيفة واحدة في {place}");
        Add("Kb.Map.TravelFromHome",
            "{minutes} min {mode} van huis",
            "{minutes} min {mode} from home",
            "{minutes} min {mode} od domu",
            "{minutes} min {mode} de acasă",
            "{minutes} د {mode} من المنزل");
        Add("Kb.Map.WhyPrefix",
            "Waarom:", "Why:",
            "Dlaczego:", "De ce:", "لماذا:");
        Add("Kb.Map.ViewJob",
            "Bekijk deze baan", "View this job",
            "Zobacz tę ofertę", "Vezi acest job", "عرض هذه الوظيفة");
        Add("Kb.Map.Save",
            "Bewaar", "Save",
            "Zapisz", "Salvează", "احفظ");
        Add("Kb.Map.Saved",
            "Bewaard", "Saved",
            "Zapisano", "Salvat", "محفوظ");
        Add("Kb.Map.FitPercent",
            "{percent}% past bij jou", "{percent}% fit for you",
            "{percent}% pasuje do ciebie", "{percent}% ți se potrivește", "{percent}% تناسبك");
        Add("Kb.Map.HoursSingle",
            "{hours} uur", "{hours} hrs",
            "{hours} godz.", "{hours} ore", "{hours} ساعة");
        Add("Kb.Map.HoursRange",
            "{min}–{max} uur", "{min}–{max} hrs",
            "{min}–{max} godz.", "{min}–{max} ore", "{min}–{max} ساعة");

        // Banenkaart start / location prompt (03)
        Add("Kb.Start.Title",
            "Waar woon je?", "Where do you live?",
            "Gdzie mieszkasz?", "Unde locuiești?", "أين تسكن؟");
        Add("Kb.Start.Hint",
            "We tonen banen binnen 20 minuten fietsen vanaf jouw adres.",
            "We show jobs within a 20-minute bike ride from your address.",
            "Pokazujemy oferty w zasięgu 20 minut rowerem od Twojego adresu.",
            "Îți arătăm joburi la 20 de minute cu bicicleta de acasă.",
            "نعرض وظائف على بُعد 20 دقيقة بالدراجة من عنوانك.");
        Add("Kb.Start.UseMyLocation",
            "Gebruik mijn locatie", "Use my location",
            "Użyj mojej lokalizacji", "Folosește locația mea", "استخدم موقعي");
        Add("Kb.Start.Later",
            "Later", "Not now",
            "Później", "Mai târziu", "لاحقاً");
        Add("Kb.Start.AddressPlaceholder",
            "Straat en plaats", "Street and place",
            "Ulica i miejscowość", "Stradă și localitate", "الشارع والمدينة");

        // Filter bar chips (03)
        Add("Kb.Filter.TravelChip",
            "{0} min {1}", "{0} mins {1}",
            "{0} min. {1}", "{0} min. {1}", "{0} د {1}");
        Add("Kb.Filter.TravelChipBike",
            "{0} min fietsen", "{0} min by bike",
            "{0} min rowerem", "{0} min cu bicicleta", "{0} د بالدراجة");
        Add("Kb.Filter.WorkType",
            "Soort werk", "Type of work",
            "Rodzaj pracy", "Tip de muncă", "نوع العمل");
        Add("Kb.Filter.Hours",
            "Uren", "Hours",
            "Godziny", "Ore", "ساعات");
        Add("Kb.Filter.Wage",
            "Loon", "Wage",
            "Wynagrodzenie", "Salariu", "الأجر");
        Add("Kb.Filter.SearchPlaceholder",
            "Zoek op functie of bedrijf", "Search by role or company",
            "Szukaj stanowiska lub firmy", "Caută după rol sau firmă", "ابحث عن وظيفة أو شركة");
        Add("Kb.Filter.KeywordPlaceholder",
            "Wat voor werk zoek je?", "What kind of work are you looking for?",
            "Jakiej pracy szukasz?", "Ce fel de muncă cauți?", "ما نوع العمل الذي تبحث عنه؟");
        Add("Kb.Filter.More",
            "Meer filters", "More filters",
            "Więcej filtrów", "Mai multe filtre", "المزيد من الفلاتر");
        Add("Kb.Filter.FiltersAria",
            "Filters openen", "Open filters",
            "Otwórz filtry", "Deschide filtrele", "فتح عوامل التصفية");
        Add("Kb.Filter.TravelAria",
            "Reistijd en vervoer", "Travel time and transport",
            "Czas dojazdu i transport", "Timp de deplasare și transport", "وقت التنقل ووسيلة المواصلات");
        Add("Kb.Filter.Clear",
            "Wis filters", "Clear filters",
            "Wyczyść filtry", "Șterge filtrele", "مسح الفلاتر");
        Add("Kb.Filter.MinutesPreset",
            "{0} min", "{0} mins",
            "{0} minut", "{0} min.", "{0} د");
        Add("Kb.Filter.Exact",
            "Precies…", "Exact…",
            "Dokładnie…", "Exact…", "بالضبط…");
        Add("Kb.Filter.HoursAny",
            "Alle uren", "Any hours",
            "Wszystkie godziny", "Orice ore", "أي ساعات");
        Add("Kb.Filter.WageAny",
            "Elk loon", "Any wage",
            "Każda stawka", "Orice salariu", "أي أجر");

        // Filter sheet (03 / mockup B)
        Add("Kb.Filter.ClearAll",
            "Wis alles", "Clear all",
            "Wyczyść wszystko", "Șterge tot", "مسح الكل");
        Add("Kb.Filter.Show",
            "Toon banen", "Show jobs",
            "Pokaż oferty", "Arată joburi", "عرض الوظائف");
        Add("Kb.Filter.ShowCount",
            "Toon {0} banen", "Show {0} jobs",
            "Pokaż {0} ofert", "Arată {0} joburi", "عرض {0} وظائف");
        Add("Kb.Filter.Keyword",
            "Zoekwoord", "Keyword",
            "Słowo kluczowe", "Cuvânt cheie", "كلمة البحث");
        Add("Kb.Filter.SheetKeywordPlaceholder",
            "Bedrijf, baan of woord", "Company, job or word",
            "Firma, praca lub słowo", "Firmă, job sau cuvânt", "شركة أو وظيفة أو كلمة");
        Add("Kb.Filter.TravelHow",
            "Hoe ga je naar je werk?", "How do you get to work?",
            "Jak dojeżdżasz do pracy?", "Cum ajungi la muncă?", "كيف تذهب إلى العمل؟");
        Add("Kb.Filter.TravelValue",
            "max. {0} min", "up to {0} min",
            "maks. {0} min", "max. {0} min.", "حد أقصى {0} د");
        Add("Kb.Filter.Match",
            "Match", "Fit",
            "Dopasowanie", "Potrivire", "التطابق");
        Add("Kb.Filter.MatchHint",
            "Hoe goed past de baan bij jou?", "How well does the job fit you?",
            "Jak bardzo oferta do Ciebie pasuje?", "Cât de bine ți se potrivește jobul?", "ما مدى ملاءمة الوظيفة لك؟");
        Add("Kb.Filter.MatchAny",
            "Alle banen", "All jobs",
            "Wszystkie oferty", "Toate joburile", "كل الوظائف");
        Add("Kb.Filter.Match60",
            "Vanaf 60%", "From 60%",
            "Od 60%", "De la 60%", "من 60٪");
        Add("Kb.Filter.Match80",
            "Vanaf 80%", "From 80%",
            "Od 80%", "De la 80%", "من 80٪");
        Add("Kb.Filter.HoursValue",
            "{0} – {1} uur", "{0} – {1} hrs",
            "{0} – {1} godz.", "{0} – {1} ore", "{0} – {1} ساعة");
        Add("Kb.Filter.Distance",
            "Afstand", "Distance",
            "Odległość", "Distanță", "المسافة");
        Add("Kb.Filter.DistanceValue",
            "max. {0} km", "up to {0} km",
            "maks. {0} km", "max. {0} km.", "حد أقصى {0} كم");
        Add("Kb.Filter.AgeHint",
            "Sommige banen hebben een minimum leeftijd.",
            "Some jobs have a minimum age.",
            "Niektóre oferty mają minimalny wiek.",
            "Unele joburi au o vârstă minimă.",
            "بعض الوظائف لها حد أدنى للعمر.");
        Add("Kb.Filter.AgeAny",
            "Alle leeftijden", "All ages",
            "Wszystkie wieki", "Toate vârstele", "كل الأعمار");
        Add("Kb.Filter.WageHour",
            "Loon per uur", "Wage per hour",
            "Stawka godzinowa", "Salariu pe oră", "الأجر بالساعة");
        Add("Kb.Filter.WageMin",
            "Minimaal", "Minimum",
            "Minimum", "Minim", "الحد الأدنى");
        Add("Kb.Filter.WageMax",
            "Maximaal", "Maximum",
            "Maksimum", "Maxim", "الحد الأعلى");
        Add("Kb.Filter.WageNoMax",
            "Geen max.", "No max.",
            "Bez max.", "Fără max.", "بدون حد أعلى");
        Add("Kb.Filter.Sort",
            "Volgorde", "Order",
            "Kolejność", "Ordine", "الترتيب");
        Add("Kb.Filter.SortStart",
            "Startdatum", "Start date",
            "Data rozpoczęcia", "Data de început", "تاريخ البدء");

        // Side list / bottom sheet header (03; fit sub-line lands in 04)
        Add("Kb.List.Header",
            "{0} banen · {1} min {2}", "{0} jobs · {1} min {2}",
            "{0} ofert · {1} min {2}", "{0} joburi · {1} min {2}", "{0} وظائف · {1} د {2}");
        Add("Kb.List.HeaderWithin",
            "{0} banen binnen {1} min {2}", "{0} jobs within {1} min {2}",
            "{0} ofert w {1} min {2}", "{0} joburi în {1} min {2}", "{0} وظائف خلال {1} د {2}");
        Add("Kb.List.SubBestFirst",
            "Beste match bovenaan.", "Best match on top.",
            "Najlepsze dopasowanie na górze.", "Cea mai bună potrivire sus.", "أفضل تطابق في الأعلى.");
        Add("Kb.List.SubSwipe",
            "Beste match bovenaan · veeg omhoog voor meer",
            "Best match on top · swipe up for more",
            "Najlepsze dopasowanie na górze · przeciągnij w górę",
            "Cea mai bună potrivire sus · glisează în sus",
            "أفضل تطابق في الأعلى · اسحب لأعلى للمزيد");

        // Travel rings legend (03)
        Add("Kb.Legend.TravelTitle",
            "Reistijd met de {0}", "Travel time by {0}",
            "Czas dojazdu {0}", "Timp de deplasare cu {0}", "وقت الوصول ب{0}");
        Add("Kb.Legend.TravelReal",
            "Echte reistijd over de weg", "Real travel time on the road",
            "Rzeczywisty czas po drogach", "Timp real pe drum", "وقت وصول حقيقي عبر الطريق");
        Add("Kb.Legend.TravelRealHint",
            "Over echte wegen en fietspaden, geen cirkel.",
            "Along real roads and bike paths — not a circle.",
            "Po prawdziwych drogach i ścieżkach — nie okrąg.",
            "Pe drumuri și piste reale — nu un cerc.",
            "عبر طرق ومسارات حقيقية — وليس دائرة.");
        Add("Kb.Legend.TravelApprox",
            "Reistijd ongeveer (cirkel)", "Travel time approximate (circle)",
            "Czas orientacyjny (okrąg)", "Timp aproximativ (cerc)", "وقت تقريبي (دائرة)");
        Add("Kb.Legend.TravelApproxHint",
            "Ongeveer — echte OV-routes volgen we nog niet.",
            "Approximate — we do not follow real transit routes yet.",
            "Orientacyjnie — nie śledzimy jeszcze tras komunikacji.",
            "Aproximativ — încă nu urmărim rutele de transport.",
            "تقريبي — لا نتبع مسارات المواصلات بعد.");
        Add("Kb.Legend.RingMinutes",
            "{0} min", "{0} mins",
            "{0} minut", "{0} min.", "{0} د");

        // View toggle (06)
        Add("Kb.View.ToggleAria",
            "Weergave", "View",
            "Widok", "Vizualizare", "العرض");
        Add("Kb.View.Map",
            "Kaart", "Map",
            "Mapa", "Hartă", "خريطة");
        Add("Kb.View.List",
            "Lijst", "List",
            "Lista", "Listă", "قائمة");
        Add("Kb.View.OpenMap",
            "Kaart", "Map",
            "Mapa", "Hartă", "خريطة");

        // Full-width / mobile list (06)
        Add("Kb.List.FromAddress",
            "Vanaf {0}", "From {0}",
            "Od {0}", "De la {0}", "من {0}");
        Add("Kb.List.SortLabel",
            "Sorteer", "Sort",
            "Sortuj", "Sortează", "ترتيب");
        Add("Kb.List.SortBest",
            "Past het best", "Best fit",
            "Najlepsze dopasowanie", "Cea mai bună potrivire", "الأنسب لك");
        Add("Kb.List.SortNear",
            "Dichtbij", "Nearby",
            "Blisko", "Aproape", "بالقرب");
        Add("Kb.List.SortNew",
            "Nieuwste", "Newest",
            "Najnowsze", "Cele mai noi", "الأحدث");
        Add("Kb.List.Save",
            "Bewaar", "Save",
            "Zapisz", "Salvează", "احفظ");
        Add("Kb.List.View",
            "Bekijk", "View",
            "Zobacz", "Vezi", "عرض");
        Add("Kb.List.Hours",
            "{0}–{1} uur", "{0}–{1} hrs",
            "{0}–{1} godz.", "{0}–{1} ore", "{0}–{1} ساعة");
        Add("Kb.List.HoursSingle",
            "{0} uur", "{0} hrs",
            "{0} godz.", "{0} ore", "{0} ساعة");
        Add("Kb.List.DayFlexible",
            "Flexibel", "Flexible",
            "Elastycznie", "Flexibil", "مرن");
        Add("Kb.List.DayDay",
            "Dag", "Day",
            "Dzień", "Zi", "نهار");
        Add("Kb.List.DayEvening",
            "Avond", "Evening",
            "Wieczór", "Seară", "مساء");
        Add("Kb.List.DayNight",
            "Nacht", "Night",
            "Noc", "Noapte", "ليل");
        Add("Kb.List.DayAndEvening",
            "Dag en avond", "Day and evening",
            "Dzień i wieczór", "Zi și seară", "نهار ومساء");
        Add("Kb.List.HowWeSort.Title",
            "Zo sorteren we", "How we sort",
            "Jak sortujemy", "Cum sortăm", "كيف نرتّب");
        Add("Kb.List.HowWeSort.FitFirst",
            "Wat past bij jouw paspoort staat bovenaan.",
            "What fits your passport comes first.",
            "To, co pasuje do paszportu, jest na górze.",
            "Ce se potrivește pașaportului tău e sus.",
            "ما يناسب جوازك يأتي أولاً.");
        Add("Kb.List.HowWeSort.TravelNext",
            "Daarna kijken we naar reistijd.",
            "Then we look at travel time.",
            "Następnie bierzemy pod uwagę dojazd.",
            "Apoi ne uităm la timpul de drum.",
            "ثم ننظر إلى وقت الوصول.");
        Add("Kb.List.HowWeSort.DislikesLower",
            "Wat je liever niet doet staat lager — nooit verborgen.",
            "What you prefer not to do ranks lower — never hidden.",
            "Czego wolisz unikać, jest niżej — nigdy nie ukryte.",
            "Ce preferi să eviți e mai jos — niciodată ascuns.",
            "ما تفضّل تجنّبه يأتي أدنى — ولا يُخفى أبداً.");
        Add("Kb.List.HowWeSort.Passport",
            "Mijn Paspoort bekijken", "View my passport",
            "Zobacz mój paszport", "Vezi pașaportul", "عرض جوازي");
        Add("Kb.List.RailTravel.Title",
            "Jouw reistijd", "Your travel time",
            "Twój dojazd", "Timpul tău de drum", "وقت وصولك");
        Add("Kb.List.RailTravel.Body",
            "Binnen {0} min {1} van huis. Pas het aan met de knop '{2}'.",
            "Within {0} min {1} from home. Change it with the '{2}' chip.",
            "W {0} min {1} od domu. Zmień przyciskiem '{2}'.",
            "În {0} min {1} de acasă. Schimbă cu butonul '{2}'.",
            "خلال {0} د {1} من المنزل. عدّله بزر '{2}'.");
        Add("Kb.List.RailTravel.Approx",
            "Ongeveer — echte OV-routes volgen we nog niet.",
            "Approximate — we do not follow real transit routes yet.",
            "Orientacyjnie — nie śledzimy jeszcze tras komunikacji.",
            "Aproximativ — încă nu urmărim rutele de transport.",
            "تقريبي — لا نتبع مسارات المواصلات بعد.");

        // Vacancy detail (06)
        Add("Kb.Detail.TravelTitle",
            "Hoe kom je er?", "How do you get there?",
            "Jak dojedziesz?", "Cum ajungi?", "كيف تصل؟");
        Add("Kb.Detail.TravelFrom",
            "Vanaf {0}", "From {0}",
            "Od {0}", "De la {0}", "من {0}");
        Add("Kb.Detail.Apply",
            "Solliciteer", "Apply",
            "Aplikuj", "Aplică", "قدّم");
        Add("Kb.Detail.Applied",
            "Je hebt gesolliciteerd · Bekijk", "You applied · View",
            "Aplikowano · Zobacz", "Ai aplicat · Vezi", "قدّمت · عرض");
        Add("Kb.Detail.Save",
            "Bewaar", "Save",
            "Zapisz", "Salvează", "احفظ");
        Add("Kb.Detail.Share",
            "Delen", "Share",
            "Udostępnij", "Distribuie", "مشاركة");
        Add("Kb.Detail.PassportHint",
            "Je paspoort gaat mee. Een brief is niet nodig.",
            "Your passport goes with you. No cover letter needed.",
            "Twój paszport idzie z tobą. List nie jest potrzebny.",
            "Pașaportul tău te însoțește. Nu e nevoie de scrisoare.",
            "جوازك يرافقك. لا حاجة لرسالة.");
        Add("Kb.Detail.HoursFact",
            "Uren per week", "Hours per week",
            "Godziny tygodniowo", "Ore pe săptămână", "ساعات في الأسبوع");
        Add("Kb.Detail.WageFact",
            "Loon per uur", "Wage per hour",
            "Stawka godzinowa", "Salariu pe oră", "الأجر بالساعة");
        Add("Kb.Detail.WhenFact",
            "Wanneer", "When",
            "Kiedy", "Când", "متى");
        Add("Kb.Detail.StartFact",
            "Begin", "Start",
            "Start", "Început", "البداية");
        Add("Kb.Detail.StartFlexible",
            "In overleg", "By arrangement",
            "Do uzgodnienia", "De comun acord", "بالاتفاق");
        Add("Kb.Detail.Route",
            "Route", "Directions",
            "Trasa", "Rută", "المسار");
        Add("Kb.Detail.StreetView",
            "Street View", "Street-level view",
            "Widok ulicy", "Vedere stradală", "تجوّل افتراضي");
        Add("Kb.Detail.TransportAria",
            "Vervoerswijze", "Transport mode",
            "Środek transportu", "Mod de transport", "وسيلة النقل");
        Add("Kb.Detail.FactsAria",
            "Belangrijkste feiten", "Key facts",
            "Najważniejsze fakty", "Fapte cheie", "حقائق أساسية");
    }
}
