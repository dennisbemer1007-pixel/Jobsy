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
        Add("Kb.Filter.More",
            "Meer filters", "More filters",
            "Więcej filtrów", "Mai multe filtre", "المزيد من الفلاتر");
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

        // Side list / bottom sheet header (03; fit sub-line lands in 04)
        Add("Kb.List.Header",
            "{0} banen · {1} min {2}", "{0} jobs · {1} min {2}",
            "{0} ofert · {1} min {2}", "{0} joburi · {1} min {2}", "{0} وظائف · {1} د {2}");
        Add("Kb.List.HeaderWithin",
            "{0} banen binnen {1} min", "{0} jobs within {1} min",
            "{0} ofert w {1} min", "{0} joburi în {1} min", "{0} وظائف خلال {1} د");
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
    }
}
