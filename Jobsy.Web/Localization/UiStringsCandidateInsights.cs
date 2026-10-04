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
            "Kandidaatinzichten", "Candidate insights",
            "Wglądy w kandydatów", "Perspective candidați", "رؤى المرشحين");
        Add("Insights.Lead",
            "Wie zoekt werk rond je vestigingen. Anoniem en opgeteld: je ziet nooit individuele kandidaten.",
            "Who is looking for work around your branches. Anonymous and aggregated: you never see individuals.",
            "Kto szuka pracy wokół Twoich placówek. Anonimowo i łącznie: nigdy nie widzisz osób.",
            "Cine caută de lucru în jurul filialelor tale. Anonim și agregat: nu vezi niciodată persoane.",
            "من يبحث عن عمل حول فروعك. مجهول ومُجمَّع: لا ترى أفراداً أبداً.");
        Add("Insights.FreeBadge",
            "Gratis versie", "Free version",
            "Wersja darmowa", "Versiune gratuită", "النسخة المجانية");
        Add("Insights.Cta.Talentpool",
            "Naar talentpool", "To talent pool",
            "Do puli talentów", "Către piscina de talente", "إلى مجموعة المواهب");
        Add("Insights.Cta.Export",
            "Exporteren", "Export",
            "Eksportuj", "Exportă", "تصدير");
        Add("Insights.Export.Locked",
            "Export is onderdeel van volledige inzichten", "Export is part of full insights",
            "Eksport jest częścią pełnych wglądów", "Exportul face parte din perspectivele complete", "التصدير جزء من الرؤى الكاملة");
        Add("Insights.FullUntil",
            "Volledig t/m {0}", "Full until {0}",
            "Pełne do {0}", "Complet până la {0}", "كامل حتى {0}");
        Add("Insights.UnlockedLine",
            "Ontgrendeld t/m {0} · {1}", "Unlocked until {0} · {1}",
            "Odblokowane do {0} · {1}", "Deblocat până la {0} · {1}", "مفتوح حتى {0} · {1}");
        Add("Insights.PartialCoverage",
            "{0} van {1} vestigingen ontgrendeld", "{0} of {1} branches unlocked",
            "{0} z {1} placówek odblokowanych", "{0} din {1} filiale deblocate", "{0} من {1} فروع مفتوحة");
        Add("Insights.Scope.Organisation",
            "alle vestigingen", "all branches",
            "wszystkie placówki", "toate filialele", "جميع الفروع");
        Add("Insights.Kpi.Free",
            "Gratis", "Free",
            "Za darmo", "Gratuit", "مجاني");
        Add("Insights.Kpi.Premium",
            "Premium", "Premium",
            "Premium", "Complet", "مميز");
        Add("Insights.Kpi.MatchingQ",
            "Hoeveel zijn beschikbaar voor je {0} vacatures?", "How many are available for your {0} vacancies?",
            "Ilu jest dostępnych dla Twoich {0} ofert?", "Câți sunt disponibili pentru {0} posturi?", "كم متاح لوظائفك ({0})؟");
        Add("Insights.Kpi.Hours32Q",
            "Hoeveel willen 32+ uur werken?", "How many want 32+ hours?",
            "Ilu chce pracować 32+ godzin?", "Câți vor 32+ ore?", "كم يريدون العمل 32+ ساعة؟");
        Add("Insights.Kpi.HoursFmt",
            "{0} u", "{0} h",
            "{0} godz.", "{0} ore", "{0} س");
        Add("Insights.Locked.Chip",
            "Vergrendeld", "Locked",
            "Zablokowane", "Blocat", "مقفل");
        Add("Insights.Map.DensityChip",
            "Dichtheid", "Density",
            "Gęstość", "Densitate", "الكثافة");
        Add("Insights.Map.DensityLock",
            "Waar wonen ze precies?", "Where exactly do they live?",
            "Gdzie dokładnie mieszkają?", "Unde locuiesc exact?", "أين يعيشون بالضبط؟");
        Add("Insights.Map.DensitySub",
            "Gratis: straal en totaal · < 10 kandidaten blijft leeg",
            "Free: radius and total · < 10 candidates stays empty",
            "Gratis: promień i suma · < 10 kandydatów pozostaje puste",
            "Gratuit: rază și total · < 10 candidați rămâne gol",
            "مجاني: النطاق والمجموع · أقل من 10 مرشحين يبقى فارغاً");
        Add("Insights.Premium.Tag",
            "Volledige inzichten", "Full insights",
            "Pełne wglądy", "Perspective complete", "رؤى كاملة");
        Add("Insights.Premium.Title",
            "Weet precies wie er rond je vestigingen zoekt",
            "Know exactly who is looking around your branches",
            "Wiedz dokładnie, kto szuka wokół Twoich placówek",
            "Știi exact cine caută în jurul filialelor tale",
            "اعرف بدقة من يبحث حول فروعك");
        Add("Insights.Premium.Lead",
            "Ontgrendel alles hierboven met echte cijfers voor {0}. Altijd anoniem.",
            "Unlock everything above with real numbers for {0}. Always anonymous.",
            "Odblokuj wszystko powyżej prawdziwymi liczbami dla {0}. Zawsze anonimowo.",
            "Deblochează tot ce e mai sus cu cifre reale pentru {0}. Întotdeauna anonim.",
            "افتح كل ما أعلاه بأرقام حقيقية لـ {0}. دائماً مجهول.");
        Add("Insights.Premium.Check.Density",
            "Dichtheid per wijk en reistijd", "Density per neighbourhood and travel time",
            "Gęstość w dzielnicach i czas dojazdu", "Densitate pe cartier și timp de deplasare", "الكثافة لكل حي ووقت التنقل");
        Add("Insights.Premium.Check.Match",
            "Beschikbaar voor je vacatures", "Available for your vacancies",
            "Dostępne dla twoich ofert", "Disponibil pentru posturile tale", "متاح لوظائفك");
        Add("Insights.Premium.Check.Fields",
            "Werkvelden, prioriteiten, droombanen", "Work fields, priorities, dream jobs",
            "Obszary, priorytety, wymarzone prace", "Domenii, priorități, joburi de vis", "مجالات وأولويات ووظائف الأحلام");
        Add("Insights.Premium.Check.Trends",
            "Trends en export (CSV)", "Trends and export (CSV)",
            "Trendy i eksport (CSV)", "Tendințe și export (CSV)", "الاتجاهات والتصدير (CSV)");
        Add("Insights.Premium.Cta",
            "Ontgrendel volledige inzichten", "Unlock full insights",
            "Odblokuj pełne wglądy", "Deblochează perspectivele complete", "افتح الرؤى الكاملة");
        Add("Insights.Premium.Days",
            "dagen", "days",
            "dni", "zile", "أيام");
        Add("Insights.Premium.Fine",
            "Afrekenen met je tokensaldo · geen abonnement",
            "Pay from your token balance · no subscription",
            "Zapłata z salda tokenów · bez abonamentu",
            "Plată din soldul de tokenuri · fără abonament",
            "الدفع من رصيد الرموز · بدون اشتراك");
        Add("Insights.Premium.AskBm",
            "Vraag je bedrijfsmanager om de volledige inzichten te ontgrendelen.",
            "Ask your company manager to unlock full insights.",
            "Poproś menedżera firmy o odblokowanie pełnych wglądów.",
            "Cere managerului firmei să deblocheze perspectivele complete.",
            "اطلب من مدير الشركة فتح الرؤى الكاملة.");
        Add("Insights.Premium.AskBmButton",
            "Vraag aan bedrijfsmanager", "Ask company manager",
            "Poproś menedżera firmy", "Cere managerului firmei", "اطلب من مدير الشركة");
        Add("Insights.Premium.RequestedOn",
            "Aangevraagd op {0}", "Requested on {0}",
            "Złożono {0}", "Cerut pe {0}", "طُلب في {0}");
        Add("Insights.Premium.UnlockBranch",
            "Ontgrendel voor {0}", "Unlock for {0}",
            "Odblokuj dla {0}", "Deblochează pentru {0}", "افتح لـ {0}");
        Add("Insights.Premium.Renew",
            "Verlengen", "Renew",
            "Przedłuż", "Reînnoiește", "تمديد");
        Add("Insights.Premium.RenewWithPrice",
            "Verlengen · {0} tokens", "Renew · {0} tokens",
            "Przedłuż · {0} tokenów", "Reînnoiește · {0} tokenuri", "تمديد · {0} رموز");
        Add("Insights.Confirm.Title",
            "Kandidaatinzichten ontgrendelen", "Unlock candidate insights",
            "Odblokuj wglądy w kandydatów", "Deblochează perspectivele candidați", "فتح رؤى المرشحين");
        Add("Insights.Confirm.Ok",
            "Ontgrendelen", "Unlock",
            "Odblokuj", "Deblochează", "فتح");
        Add("Insights.Confirm.Body",
            "Kandidaatinzichten ontgrendelen voor {0}? Dit kost {1} tokens. Je saldo wordt {2}. Geldig t/m {3}.",
            "Unlock candidate insights for {0}? This costs {1} tokens. Your balance becomes {2}. Valid until {3}.",
            "Odblokować wglądy dla {0}? Koszt: {1} tokenów. Saldo będzie {2}. Ważne do {3}.",
            "Deblochezi perspectivele pentru {0}? Costă {1} tokenuri. Soldul devine {2}. Valabil până la {3}.",
            "فتح رؤى المرشحين لـ {0}؟ التكلفة {1} رمزاً. يصبح رصيدك {2}. صالح حتى {3}.");
        Add("Insights.Toast.Unlocked",
            "Ontgrendeld t/m {0}", "Unlocked until {0}",
            "Odblokowane do {0}", "Deblocat până la {0}", "مفتوح حتى {0}");
        Add("Insights.Toast.Requested",
            "Aanvraag verstuurd naar je bedrijfsmanager.", "Request sent to your company manager.",
            "Wniosek wysłano do menedżera firmy.", "Cererea a fost trimisă managerului firmei.", "أُرسل الطلب إلى مدير شركتك.");
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
            "Altijd anoniem · groepen onder 10 kandidaten tonen we niet",
            "Always anonymous · we don't show groups under 10 candidates",
            "Zawsze anonimowo · grup poniżej 10 kandydatów nie pokazujemy",
            "Întotdeauna anonim · nu afișăm grupuri sub 10 candidați",
            "دائماً مجهول · لا نعرض مجموعات أقل من 10 مرشحين");
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
            "Beschikbaar voor jouw vacatures", "Available for your vacancies",
            "Dostępni dla Twoich ofert", "Disponibili pentru posturile tale", "متاحون لوظائفك");

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
            "Vergrendeld", "Locked",
            "Zablokowane", "Blocat", "مقفل");
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
