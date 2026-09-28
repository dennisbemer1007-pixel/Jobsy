namespace Jobsy.Web.Localization;

public static class UiStringsCandidateInsights
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

        Add("Nav.CandidateInsights",
            "Kandidaatinzichten", "Candidate insights",
            "Wglądy w kandydatów", "Perspective candidați", "رؤى المرشحين");
        Add("Insights.Tab.Talent",
            "Talentpool", "Talent pool",
            "Pula talentów", "Piscină de talente", "مجموعة المواهب");
        Add("Insights.Tab.Insights",
            "Kandidaatinzichten", "Candidate insights",
            "Wglądy w kandydatów", "Perspective candidați", "رؤى المرشحين");
        Add("Insights.Title",
            "Wat beweegt kandidaten in jouw regio", "What moves candidates in your region",
            "Co motywuje kandydatów w Twoim regionie", "Ce îi motivează pe candidați în regiunea ta", "ما الذي يحرك المرشحين في منطقتك");
        Add("Insights.Lead",
            "Anonieme inzichten over kandidaten rondom je vestiging.", "Anonymous insights about candidates around your branch.",
            "Anonimowe wglądy o kandydatach wokół Twojej placówki.", "Perspective anonime despre candidații din jurul filialei tale.", "رؤى مجهولة عن المرشحين حول فرعك.");
        Add("Insights.FreeBadge",
            "Gratis versie", "Free version",
            "Wersja darmowa", "Versiune gratuită", "النسخة المجانية");
        Add("Insights.Cta.Full",
            "Volledige inzichten", "Full insights",
            "Pełne wglądy", "Perspective complete", "رؤى كاملة");
        Add("Insights.Cta.Story",
            "Bekijk als story", "View as story",
            "Zobacz jako relację", "Vezi ca story", "عرض كقصة");
        Add("Insights.Cta.LockedWithTokens",
            "Volledige inzichten met tokens", "Full insights with tokens",
            "Pełne wglądy za tokeny", "Perspective complete cu tokenuri", "رؤى كاملة بالرموز");
        Add("Insights.Filter.Branch",
            "Vestiging", "Branch",
            "Placówka", "Filială", "الفرع");
        Add("Insights.Filter.AllBranches",
            "Alle vestigingen ({0})", "All branches ({0})",
            "Wszystkie placówki ({0})", "Toate filialele ({0})", "جميع الفروع ({0})");
        Add("Insights.Filter.Radius",
            "Straal", "Radius",
            "Promień", "Rază", "النطاق");
        Add("Insights.Filter.Period",
            "Periode", "Period",
            "Okres", "Perioadă", "الفترة");
        Add("Insights.Filter.Period.30",
            "Laatste 30 dagen", "Last 30 days",
            "Ostatnie 30 dni", "Ultimele 30 de zile", "آخر 30 يوماً");
        Add("Insights.Filter.Period.90",
            "Laatste 90 dagen", "Last 90 days",
            "Ostatnie 90 dni", "Ultimele 90 de zile", "آخر 90 يوماً");
        Add("Insights.Filter.Period.365",
            "Laatste jaar", "Last year",
            "Ostatni rok", "Ultimul an", "العام الماضي");
        Add("Insights.Filter.Km",
            "{0} km", "{0} km",
            "{0} km", "{0} km", "{0} كم");
        Add("Insights.Anonymity",
            "Anoniem, minimaal 10 kandidaten per groep. Kleinere groepen tonen we als 'te weinig data'.",
            "Anonymous, at least 10 candidates per group. Smaller groups show as 'too little data'.",
            "Anonimowo, minimum 10 kandydatów w grupie. Mniejsze grupy pokazujemy jako 'za mało danych'.",
            "Anonim, minim 10 candidați pe grup. Grupurile mai mici apar ca 'date insuficiente'.",
            "مجهول، 10 مرشحين على الأقل لكل مجموعة. المجموعات الأصغر تظهر كـ 'بيانات غير كافية'.");
        Add("Insights.Insufficient",
            "Te weinig data", "Too little data",
            "Za mało danych", "Date insuficiente", "بيانات غير كافية");
        Add("Insights.Loading",
            "Inzichten laden…", "Loading insights…",
            "Ładowanie wglądów…", "Se încarcă perspectivele…", "جارٍ تحميل الرؤى…");
        Add("Insights.Error",
            "Kon inzichten niet laden.", "Could not load insights.",
            "Nie udało się wczytać wglądów.", "Nu s-au putut încărca perspectivele.", "تعذر تحميل الرؤى.");
        Add("Insights.RegionalReadOnly",
            "Je bekijkt inzichten alleen-lezen voor je regio.", "You view insights read-only for your region.",
            "Przeglądasz wglądy tylko do odczytu dla swojego regionu.", "Vezi perspectivele doar în citire pentru regiunea ta.", "تعرض الرؤى للقراءة فقط لمنطقتك.");

        Add("Insights.Kpi.Candidates",
            "Kandidaten in straal", "Candidates in radius",
            "Kandydaci w promieniu", "Candidați în rază", "مرشحون ضمن النطاق");
        Add("Insights.Kpi.AvgHours",
            "Gem. uren per week", "Avg hours per week",
            "Śr. godzin tygodniowo", "Ore medii pe săptămână", "متوسط الساعات أسبوعياً");
        Add("Insights.Kpi.Hours32",
            "32+ uur beschikbaar", "Available 32+ hours",
            "Dostępni 32+ godzin", "Disponibili 32+ ore", "متاحون 32+ ساعة");
        Add("Insights.Kpi.Active30",
            "Actief laatste 30 dagen", "Active last 30 days",
            "Aktywni w ostatnich 30 dniach", "Activi în ultimele 30 de zile", "نشطون آخر 30 يوماً");
        Add("Insights.Kpi.Matching",
            "Match met jouw vacatures", "Match with your vacancies",
            "Dopasowanie do Twoich ofert", "Potrivire cu posturile tale", "تطابق مع وظائفك");

        Add("Insights.Section.DreamJobs",
            "Droombanen", "Dream jobs",
            "Wymarzone prace", "Joburi de vis", "وظائف الأحلام");
        Add("Insights.Section.Map",
            "Waar wonen kandidaten", "Where candidates live",
            "Gdzie mieszkają kandydaci", "Unde locuiesc candidații", "أين يعيش المرشحون");
        Add("Insights.Section.Competences",
            "Competenties & werk-DNA", "Competencies & work DNA",
            "Kompetencje i DNA pracy", "Competențe și ADN de muncă", "الكفاءات وبصمة العمل");
        Add("Insights.Section.Priorities",
            "Wat kandidaten belangrijk vinden", "What candidates find important",
            "Co jest ważne dla kandydatów", "Ce este important pentru candidați", "ما يهم المرشحين");
        Add("Insights.Section.Tip",
            "Maak je vacature aantrekkelijker", "Make your vacancy more attractive",
            "Uczyń ofertę atrakcyjniejszą", "Fă anunțul mai atractiv", "اجعل وظيفتك أكثر جاذبية");
        Add("Insights.Section.WorkFields",
            "Werkvelden", "Work fields",
            "Obszary pracy", "Domenii de muncă", "مجالات العمل");
        Add("Insights.Section.WorkKinds",
            "Soort werk", "Type of work",
            "Rodzaj pracy", "Tip de muncă", "نوع العمل");
        Add("Insights.Section.Trends",
            "Trends", "Trends",
            "Trendy", "Tendințe", "الاتجاهات");
        Add("Insights.Section.Vacancies",
            "Jouw vacatures", "Your vacancies",
            "Twoje oferty", "Posturile tale", "وظائفك");
        Add("Insights.Section.Dna",
            "Werk-DNA", "Work DNA",
            "DNA pracy", "ADN de muncă", "بصمة العمل");
        Add("Insights.Section.Personality",
            "Persoonlijkheid", "Personality",
            "Osobowość", "Personalitate", "الشخصية");
        Add("Insights.Section.Availability",
            "Beschikbaarheid", "Availability",
            "Dostępność", "Disponibilitate", "التوفر");

        Add("Insights.Priority.travel",
            "Reistijd", "Travel time",
            "Czas dojazdu", "Timp de deplasare", "وقت التنقل");
        Add("Insights.Priority.flexibility",
            "Flexibiliteit", "Flexibility",
            "Elastyczność", "Flexibilitate", "المرونة");
        Add("Insights.Priority.culture",
            "Sfeer & cultuur", "Atmosphere & culture",
            "Atmosfera i kultura", "Atmosferă și cultură", "الأجواء والثقافة");
        Add("Insights.Priority.stability",
            "Zekerheid en duidelijke afspraken", "Certainty and clear agreements",
            "Pewność i jasne ustalenia", "Siguranță și acorduri clare", "اليقين والاتفاقات الواضحة");

        Add("Insights.WorkKind.fulltime",
            "Fulltime", "Full-time",
            "Pełny etat", "Full-time", "دوام كامل");
        Add("Insights.WorkKind.parttime",
            "Parttime", "Part-time",
            "Część etatu", "Part-time", "دوام جزئي");
        Add("Insights.WorkKind.bijbaan",
            "Bijbaan", "Side job",
            "Praca dorywcza", "Job secundar", "عمل جانبي");
        Add("Insights.WorkKind.stage",
            "Stage", "Internship",
            "Staż", "Stagiu", "تدريب");
        Add("Insights.WorkKind.vrijwilliger",
            "Vrijwilliger", "Volunteer",
            "Wolontariusz", "Voluntar", "متطوع");

        Add("Insights.Tip.Travel",
            "Noem de reistijd en OV-verbinding in je vacature.", "Mention travel time and public transport in your vacancy.",
            "Wymień czas dojazdu i komunikację publiczną w ofercie.", "Menționează timpul de deplasare și transportul public în anunț.", "اذكر وقت التنقل والنقل العام في إعلانك.");
        Add("Insights.Tip.Flexibility",
            "Benoem flexibele uren of diensten waar dat kan.", "Mention flexible hours or shifts where possible.",
            "Wymień elastyczne godziny lub zmiany, gdzie to możliwe.", "Menționează ore flexibile sau ture unde este posibil.", "اذكر الساعات أو الورديات المرنة حيثما أمكن.");
        Add("Insights.Tip.Culture",
            "Schrijf iets over sfeer, team en werkwijze.", "Write something about atmosphere, team and way of working.",
            "Napisz coś o atmosferze, zespole i sposobie pracy.", "Scrie ceva despre atmosferă, echipă și stilul de lucru.", "اكتب شيئاً عن الأجواء والفريق وأسلوب العمل.");
        Add("Insights.Tip.Stability",
            "Maak afspraken en zekerheid concreet in de tekst.", "Make agreements and certainty concrete in the text.",
            "Uściślij ustalenia i pewność w tekście oferty.", "Fă acordurile și siguranța concrete în text.", "اجعل الاتفاقات واليقين ملموسين في النص.");
        Add("Insights.Tip.Generic",
            "Maak je vacature concreet over uren, sfeer en bereikbaarheid.", "Make your vacancy concrete about hours, atmosphere and accessibility.",
            "Uściślij ofertę pod kątem godzin, atmosfery i dojazdu.", "Fă anunțul concret despre ore, atmosferă și accesibilitate.", "اجعل إعلانك ملموساً حول الساعات والأجواء وإمكانية الوصول.");

        Add("Insights.Checklist.Title",
            "Zo scoort je vacature hierop", "How your vacancy scores on this",
            "Jak Twoja oferta wypada pod tym względem", "Cum se clasează anunțul tău aici", "كيف تقيّم وظيفتك في هذا");
        Add("Insights.Checklist.Salary",
            "Salaris genoemd", "Salary mentioned",
            "Wynagrodzenie wymienione", "Salariu menționat", "تم ذكر الراتب");
        Add("Insights.Checklist.Flex",
            "Flexibele uren", "Flexible hours",
            "Elastyczne godziny", "Ore flexibile", "ساعات مرنة");
        Add("Insights.Checklist.Culture",
            "Iets over sfeer en team", "Something about atmosphere and team",
            "Coś o atmosferze i zespole", "Ceva despre atmosferă și echipă", "شيء عن الأجواء والفريق");
        Add("Insights.Checklist.Yes",
            "Ja", "Yes",
            "Tak", "Da", "نعم");
        Add("Insights.Checklist.No",
            "Nee", "No",
            "Nie", "Nu", "لا");
        Add("Insights.ImproveVacancy",
            "Vacature verbeteren", "Improve vacancy",
            "Ulepsz ofertę", "Îmbunătățește anunțul", "تحسين الوظيفة");

        Add("Insights.Map.LegendDensity",
            "Minder → Meer kandidaten", "Fewer → More candidates",
            "Mniej → Więcej kandydatów", "Mai puțini → Mai mulți candidați", "أقل ← أكثر مرشحين");
        Add("Insights.Map.LegendRadius",
            "Straal vanaf vestiging", "Radius from branch",
            "Promień od placówki", "Rază de la filială", "النطاق من الفرع");
        Add("Insights.Map.LegendPrivacy",
            "Geen individuele locaties · gebieden met minder dan 10 kandidaten blijven leeg",
            "No individual locations · areas with fewer than 10 candidates stay empty",
            "Bez lokalizacji indywidualnych · obszary z mniej niż 10 kandydatami pozostają puste",
            "Fără locații individuale · zonele cu mai puțin de 10 candidați rămân goale",
            "بدون مواقع فردية · المناطق التي بها أقل من 10 مرشحين تبقى فارغة");

        Add("Insights.Trend.Placeholder",
            "Beschikbaar zodra er genoeg data is", "Available once there is enough data",
            "Dostępne, gdy będzie wystarczająco danych", "Disponibil când există suficiente date", "متاح عند توفر بيانات كافية");
        Add("Insights.Trend.InsufficientHistory",
            "Beschikbaar zodra er genoeg data is", "Available once there is enough data",
            "Dostępne, gdy będzie wystarczająco danych", "Disponibil când există suficiente date", "متاح عند توفر بيانات كافية");

        Add("Insights.Locked.BlurHint",
            "Volledige inzichten met tokens", "Full insights with tokens",
            "Pełne wglądy za tokeny", "Perspective complete cu tokenuri", "رؤى كاملة بالرموز");
        Add("Insights.MatchingCount",
            "{0} passende kandidaten", "{0} matching candidates",
            "{0} pasujących kandydatów", "{0} candidați potriviți", "{0} مرشحون متطابقون");
        Add("Insights.Candidates",
            "kandidaten", "candidates",
            "kandydaci", "candidați", "مرشحون");

        Add("Insights.Story.Title",
            "Kandidaatinzichten · {0}", "Candidate insights · {0}",
            "Wglądy w kandydatów · {0}", "Perspective candidați · {0}", "رؤى المرشحين · {0}");
        Add("Insights.Story.Progress",
            "{0} van 10 · {1} · {2}", "{0} of 10 · {1} · {2}",
            "{0} z 10 · {1} · {2}", "{0} din 10 · {1} · {2}", "{0} من 10 · {1} · {2}");
        Add("Insights.Story.Close",
            "Sluiten", "Close",
            "Zamknij", "Închide", "إغلاق");
        Add("Insights.Story.Prev",
            "Vorige", "Previous",
            "Poprzednia", "Anterior", "السابق");
        Add("Insights.Story.Next",
            "Volgende", "Next",
            "Następna", "Următor", "التالي");
        Add("Insights.Story.Card1",
            "Regio", "Region",
            "Region", "Regiune", "المنطقة");
        Add("Insights.Story.Card2",
            "Droombanen", "Dream jobs",
            "Wymarzone prace", "Joburi de vis", "وظائف الأحلام");
        Add("Insights.Story.Card3",
            "Beschikbaarheid", "Availability",
            "Dostępność", "Disponibilitate", "التوفر");
        Add("Insights.Story.Card4",
            "Wat ze belangrijk vinden", "What they find important",
            "Co jest dla nich ważne", "Ce este important pentru ei", "ما يجدونه مهماً");
        Add("Insights.Story.Card5",
            "Soort werk", "Type of work",
            "Rodzaj pracy", "Tip de muncă", "نوع العمل");
        Add("Insights.Story.Card6",
            "Jouw vacatures", "Your vacancies",
            "Twoje oferty", "Posturile tale", "وظائفك");
        Add("Insights.Story.Card7",
            "Werk-DNA", "Work DNA",
            "DNA pracy", "ADN de muncă", "بصمة العمل");
        Add("Insights.Story.Card8",
            "Werkvelden", "Work fields",
            "Obszary pracy", "Domenii de muncă", "مجالات العمل");
        Add("Insights.Story.Card9",
            "Trends", "Trends",
            "Trendy", "Tendințe", "الاتجاهات");
        Add("Insights.Story.Card10",
            "Volledige inzichten", "Full insights",
            "Pełne wglądy", "Perspective complete", "رؤى كاملة");
        Add("Insights.Story.Card10.Lead",
            "Ontgrendel droombanen 4–10, competenties, werk-DNA en meer met tokens.",
            "Unlock dream jobs 4–10, competencies, work DNA and more with tokens.",
            "Odblokuj wymarzone prace 4–10, kompetencje, DNA pracy i więcej za tokeny.",
            "Deblochează joburi de vis 4–10, competențe, ADN de muncă și mai mult cu tokenuri.",
            "افتح وظائف الأحلام 4–10 والكفاءات وبصمة العمل والمزيد بالرموز.");
    }
}
