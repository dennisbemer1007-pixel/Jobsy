namespace Jobsy.Web.Localization;

public static class UiStringsWerkgever
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

        // Enterprise UI primitives
        Add("EntUi.Loading", "Laden…", "Loading…", "Ładowanie…", "Se încarcă…", "جارٍ التحميل…");
        Add("EntUi.Empty", "Niets gevonden.", "Nothing found.", "Nic nie znaleziono.", "Nimic găsit.", "لم يُعثر على شيء.");
        Add("EntUi.Selected", "{0} geselecteerd", "{0} selected", "{0} zaznaczono", "{0} selectate", "{0} محدد");
        Add("EntUi.ClearSelection", "Selectie wissen", "Clear selection", "Wyczyść zaznaczenie", "Șterge selecția", "مسح التحديد");
        Add("EntUi.FilterSearch", "Zoeken", "Search", "Szukaj", "Căutare", "بحث");
        Add("EntUi.PrevPage", "Vorige", "Previous", "Poprzednia", "Anterior", "السابق");
        Add("EntUi.NextPage", "Volgende", "Next", "Następna", "Următor", "التالي");
        Add("EntUi.PageOf", "Pagina {0} van {1}", "Page {0} of {1}", "Strona {0} z {1}", "Pagina {0} din {1}", "صفحة {0} من {1}");
        Add("EntUi.Pager", "Paginering", "Pagination", "Paginacja", "Paginare", "ترقيم الصفحات");
        Add("EntUi.CloseDrawer", "Sluiten", "Close", "Zamknij", "Închide", "إغلاق");
        Add("EntUi.Actions", "Acties", "Actions", "Akcje", "Acțiuni", "إجراءات");

        // Shell
        Add("WgShell.Werkgever", "Werkgever", "Employer", "Pracodawca", "Angajator", "صاحب العمل");
        Add("WgShell.ScopeChip", "Bereik", "Scope", "Zakres", "Domeniu", "النطاق");
        Add("WgShell.ReadOnly", "Alleen lezen", "Read only", "Tylko odczyt", "Doar citire", "للقراءة فقط");
        Add("WgShell.ReadOnlyHint", "Alleen lezen. Wijzigen doen de vestigings- en bedrijfsmanagers.", "Read only. Branch and company managers make changes.", "Tylko odczyt. Zmiany wprowadzają menedżerowie placówki i firmy.", "Doar citire. Modificările le fac managerii de filială și de firmă.", "للقراءة فقط. يقوم مديرو الفرع والشركة بالتعديل.");
        Add("WgShell.ReadOnlyAction", "Dit doet de vestigings- of bedrijfsmanager", "The branch or company manager does this", "To robi menedżer placówki lub firmy", "Aceasta o face managerul de filială sau de firmă", "يقوم بذلك مدير الفرع أو الشركة");
        Add("WgShell.RmFooter", "Je kijkt mee als regiomanager. Wijzigen doen de vestigings- en bedrijfsmanagers.", "You are viewing as regional manager. Branch and company managers make changes.", "Przeglądasz jako menedżer regionu. Zmiany wprowadzają menedżerowie placówki i firmy.", "Vizualizezi ca manager regional. Modificările le fac managerii de filială și de firmă.", "أنت تعرض كمدير منطقة. يقوم مديرو الفرع والشركة بالتعديل.");
        Add("WgShell.JobMap", "Banenkaart bekijken", "View job map", "Zobacz mapę ofert", "Vezi harta joburilor", "عرض خريطة الوظائف");
        Add("WgShell.Role.BM", "Bedrijfsmanager", "Company manager", "Menedżer firmy", "Manager de firmă", "مدير الشركة");
        Add("WgShell.Role.RM", "Regiomanager", "Regional manager", "Menedżer regionu", "Manager regional", "مدير المنطقة");
        Add("WgShell.Role.VM", "Vestigingsmanager", "Branch manager", "Menedżer placówki", "Manager de filială", "مدير الفرع");
        Add("WgShell.Role.IM", "Intermediair", "Intermediary", "Pośrednik", "Intermediar", "وسيط");
        Add("WgShell.AllBranches", "Alle vestigingen", "All branches", "Wszystkie placówki", "Toate filialele", "كل الفروع");
        Add("WgShell.ScopeDenied", "Je hebt geen toegang tot die vestiging.", "You do not have access to that branch.", "Nie masz dostępu do tej placówki.", "Nu ai acces la acea filială.", "ليس لديك وصول إلى ذلك الفرع.");
        Add("WgShell.Breadcrumb", "Kruimelpad", "Breadcrumb", "Ścieżka nawigacji", "Breadcrumb", "مسار التنقل");
        Add("WgShell.MainNav", "Werkgever navigatie", "Employer navigation", "Nawigacja pracodawcy", "Navigare angajator", "تنقل صاحب العمل");
        Add("WgShell.BottomNav", "Onderste navigatie", "Bottom navigation", "Dolna nawigacja", "Navigare inferioară", "التنقل السفلي");
        Add("WgShell.MoreSheet", "Meer", "More", "Więcej", "Mai mult", "المزيد");
        Add("WgShell.BranchCount", "{0} vestigingen", "{0} branches", "{0} placówek", "{0} filiale", "{0} فروع");
        Add("WgShell.TokensAskBm", "Vraag je bedrijfsmanager om tokens", "Ask your company manager for tokens", "Poproś menedżera firmy o tokeny", "Cere tokenuri managerului de firmă", "اطلب الرموز من مدير شركتك");

        // Nav groups
        Add("WgNav.Group.Overview", "Overzicht", "Overview", "Przegląd", "Prezentare", "نظرة عامة");
        Add("WgNav.Group.Recruitment", "Werving", "Recruitment", "Rekrutacja", "Recrutare", "التوظيف");
        Add("WgNav.Group.Organisation", "Organisatie", "Organisation", "Organizacja", "Organizație", "المنظمة");
        Add("WgNav.Group.MyBranch", "Mijn vestiging", "My branch", "Moja placówka", "Filiala mea", "فرعي");
        Add("WgNav.Group.MyRegion", "Mijn regio", "My region", "Mój region", "Regiunea mea", "منطقتي");
        Add("WgNav.Group.Tokens", "Tokens & facturen", "Tokens & invoices", "Tokeny i faktury", "Tokenuri și facturi", "الرموز والفواتير");
        Add("WgNav.Group.TokenUsage", "Tokenverbruik", "Token usage", "Zużycie tokenów", "Consum tokenuri", "استهلاك الرموز");
        Add("WgNav.Group.More", "Meer", "More", "Więcej", "Mai mult", "المزيد");

        // Nav items
        Add("WgNav.Dashboard", "Dashboard", "Dashboard", "Panel", "Tablou de bord", "لوحة التحكم");
        Add("WgNav.Todo", "Te doen", "To do", "Do zrobienia", "De făcut", "للقيام به");
        Add("WgNav.Signals", "Signalen", "Signals", "Sygnały", "Semnale", "إشارات");
        Add("WgNav.Vacancies", "Vacatures", "Vacancies", "Oferty", "Posturi", "الوظائف");
        Add("WgNav.Applications", "Sollicitaties", "Applications", "Aplikacje", "Aplicații", "الطلبات");
        Add("WgNav.Talentpool", "Talentpool", "Talent pool", "Pula talentów", "Baza de talente", "مجموعة المواهب");
        Add("WgNav.CandidateInsights", "Kandidaatinzichten", "Candidate insights", "Wnioski o kandydatach", "Perspective candidați", "رؤى المرشحين");
        Add("WgNav.BranchesRegions", "Vestigingen & regio's", "Branches & regions", "Placówki i regiony", "Filiale și regiuni", "الفروع والمناطق");
        Add("WgNav.TeamRights", "Team & rechten", "Team & rights", "Zespół i uprawnienia", "Echipă și drepturi", "الفريق والصلاحيات");
        Add("WgNav.CompanyProfile", "Bedrijfsprofiel", "Company profile", "Profil firmy", "Profil firmă", "ملف الشركة");
        Add("WgNav.BranchProfile", "Vestigingsprofiel", "Branch profile", "Profil placówki", "Profil filială", "ملف الفرع");
        Add("WgNav.SalaryTables", "Salaristabellen", "Salary tables", "Tabele wynagrodzeń", "Tabele salariale", "جداول الرواتب");
        Add("WgNav.BalanceBuy", "Saldo & kopen", "Balance & buy", "Saldo i zakup", "Sold și cumpărare", "الرصيد والشراء");
        Add("WgNav.BalanceRequest", "Saldo & aanvragen", "Balance & requests", "Saldo i wnioski", "Sold și cereri", "الرصيد والطلبات");
        Add("WgNav.TokenUsage", "Tokenverbruik", "Token usage", "Zużycie tokenów", "Consum tokenuri", "استهلاك الرموز");
        Add("WgNav.UsagePerBranch", "Verbruik per vestiging", "Usage per branch", "Zużycie per placówka", "Consum pe filială", "الاستهلاك لكل فرع");
        Add("WgNav.Mutations", "Mutaties", "Mutations", "Mutacje", "Mutări", "الحركات");
        Add("WgNav.Invoices", "Facturen", "Invoices", "Faktury", "Facturi", "الفواتير");
        Add("WgNav.Integrations", "Koppelingen", "Integrations", "Integracje", "Integrări", "التكاملات");
        Add("WgNav.Takeovers", "Overnames", "Takeovers", "Przejęcia", "Preluări", "الاستحواذات");
        Add("WgNav.RecruitmentMaterials", "Wervingsmateriaal", "Recruitment materials", "Materiały rekrutacyjne", "Materiale de recrutare", "مواد التوظيف");
        Add("WgNav.PartnerProgram", "Partnerprogramma", "Partner program", "Program partnerski", "Program partener", "برنامج الشركاء");
        Add("WgNav.Clients", "Klanten", "Clients", "Klienci", "Clienți", "العملاء");
        Add("WgNav.Team", "Team", "Team", "Zespół", "Echipă", "الفريق");
        Add("WgNav.MyApplications", "Mijn sollicitaties", "My applications", "Moje aplikacje", "Aplicațiile mele", "طلباتي");
        Add("WgNav.More", "Meer", "More", "Więcej", "Mai mult", "المزيد");

        // Tabs
        Add("WgTabs.Search", "Zoeken", "Search", "Szukaj", "Căutare", "بحث");
        Add("WgTabs.Contacts", "Contactverzoeken", "Contact requests", "Prośby o kontakt", "Cereri de contact", "طلبات التواصل");
        Add("WgTabs.Profile", "Profiel", "Profile", "Profil", "Profil", "الملف");
        Add("WgTabs.Culture", "Cultuur", "Culture", "Kultura", "Cultură", "الثقافة");
        Add("WgTabs.Regions", "Regio's", "Regions", "Regiony", "Regiuni", "المناطق");

        // Dashboard (02)
        Add("WgDash.Title.Rm", "Regio dashboard", "Region dashboard", "Panel regionu", "Tablou regional", "لوحة المنطقة");
        Add("WgDash.Lead.Bm", "{0} · {1} vestigingen in {2} regio's · laatste {3}", "{0} · {1} branches in {2} regions · last {3}", "{0} · {1} placówek w {2} regionach · ostatnie {3}", "{0} · {1} filiale în {2} regiuni · ultimele {3}", "{0} · {1} فروع في {2} مناطق · آخر {3}");
        Add("WgDash.Lead.Rm", "{0} · {1} vestigingen · laatste {2}", "{0} · {1} branches · last {2}", "{0} · {1} placówek · ostatnie {2}", "{0} · {1} filiale · ultimele {2}", "{0} · {1} فروع · آخر {2}");
        Add("WgDash.Lead.Vm", "{0} · laatste {1}", "{0} · last {1}", "{0} · ostatnie {1}", "{0} · ultimele {1}", "{0} · آخر {1}");
        Add("WgDash.Lead.Intermediary", "KPI’s over alle opdrachtgevers. Detail per bedrijf staat in Bedrijvenoverzicht.", "KPIs across all clients. Per-company detail is in Clients.", "KPI dla wszystkich klientów. Szczegóły per firma w Klienci.", "KPI pentru toți clienții. Detaliile pe firmă sunt la Clienți.", "مؤشرات لكل العملاء. التفاصيل لكل شركة في العملاء.");
        Add("WgDash.Export", "Exporteren", "Export", "Eksportuj", "Exportă", "تصدير");
        Add("WgDash.PostVacancy", "Vacature plaatsen", "Post vacancy", "Opublikuj ofertę", "Publică post", "نشر وظيفة");
        Add("WgDash.TodoTitle", "Te doen", "To do", "Do zrobienia", "De făcut", "للقيام به");
        Add("WgDash.SignalsTitle", "Signalen in je regio", "Signals in your region", "Sygnały w regionie", "Semnale în regiune", "إشارات في منطقتك");
        Add("WgDash.ForInfo", "Ter info", "For info", "Informacyjnie", "Informativ", "للعلم");
        Add("WgDash.ViewAll", "Alles bekijken", "View all", "Zobacz wszystko", "Vezi tot", "عرض الكل");
        Add("WgDash.TodoEmpty", "Niets te doen. Alles loopt.", "Nothing to do. Everything is on track.", "Nic do zrobienia. Wszystko działa.", "Nimic de făcut. Totul merge.", "لا شيء للقيام به. كل شيء على ما يرام.");
        Add("WgDash.Funnel", "Wervingstrechter", "Recruitment funnel", "Lejek rekrutacji", "Pâlnie recrutare", "قمع التوظيف");
        Add("WgDash.Funnel.Applications", "Sollicitaties", "Applications", "Aplikacje", "Aplicații", "الطلبات");
        Add("WgDash.Funnel.Accepted", "Geaccepteerd", "Accepted", "Zaakceptowane", "Acceptate", "مقبولة");
        Add("WgDash.Funnel.Invited", "Uitgenodigd", "Invited", "Zaproszeni", "Invitați", "مدعوون");
        Add("WgDash.Funnel.Hired", "Aangenomen", "Hired", "Zatrudnieni", "Angajați", "مُعيَّنون");
        Add("WgDash.Branches", "Vestigingen", "Branches", "Placówki", "Filiale", "الفروع");
        Add("WgDash.BranchesRegion", "Vestigingen in je regio", "Branches in your region", "Placówki w regionie", "Filiale în regiune", "فروع في منطقتك");
        Add("WgDash.BranchesBehind", "Vestigingen met achterstand", "Branches behind", "Placówki z opóźnieniem", "Filiale în întârziere", "فروع متأخرة");
        Add("WgDash.NoBehind", "Geen vestigingen met achterstand.", "No branches behind.", "Brak placówek z opóźnieniem.", "Nicio filială în întârziere.", "لا فروع متأخرة.");
        Add("WgDash.AllBranches", "Alle {0} vestigingen", "All {0} branches", "Wszystkie {0} placówki", "Toate cele {0} filiale", "كل الـ {0} فروع");
        Add("WgDash.AllBranchesShort", "Alle {0}", "All {0}", "Wszystkie {0}", "Toate {0}", "الكل {0}");
        Add("WgDash.Col.Branch", "Vestiging", "Branch", "Placówka", "Filială", "الفرع");
        Add("WgDash.Col.Live", "Live vac.", "Live jobs", "Live oferty", "Posturi live", "وظائف مباشرة");
        Add("WgDash.Col.New", "Nieuw", "New", "Nowe", "Noi", "جديد");
        Add("WgDash.Col.FirstResponse", "1e reactie", "1st response", "1. odpowiedź", "1. răspuns", "أول رد");
        Add("WgDash.Col.Status", "Status", "Status", "Status", "Status", "الحالة");
        Add("WgDash.Status.OpSchema", "Op schema", "On track", "Zgodnie z planem", "În grafic", "وفق الجدول");
        Add("WgDash.Status.Aandacht", "Aandacht", "Attention", "Uwaga", "Atenție", "انتباه");
        Add("WgDash.Status.Achterstand", "Achterstand", "Behind", "Opóźnienie", "Întârziere", "تأخر");
        Add("WgDash.Kpi.ActiveVacancies", "Actieve vacatures", "Active vacancies", "Aktywne oferty", "Posturi active", "وظائف نشطة");
        Add("WgDash.Kpi.NewApplications", "Nieuwe sollicitaties", "New applications", "Nowe aplikacje", "Aplicații noi", "طلبات جديدة");
        Add("WgDash.Kpi.FirstResponse", "Gem. eerste reactie", "Avg. first response", "Śr. pierwsza odpowiedź", "Răspuns mediu", "متوسط أول رد");
        Add("WgDash.Kpi.FirstResponseShort", "Eerste reactie", "First response", "Pierwsza odpowiedź", "Primul răspuns", "أول رد");
        Add("WgDash.Kpi.FirstResponseTip", "Te weinig reacties om te meten", "Too few responses to measure", "Za mało odpowiedzi do pomiaru", "Prea puține răspunsuri", "ردود قليلة جداً للقياس");
        Add("WgDash.Kpi.Hired", "Aangenomen", "Hired", "Zatrudnieni", "Angajați", "مُعيَّنون");
        Add("WgDash.Kpi.TokenBalance", "Tokensaldo", "Token balance", "Saldo tokenów", "Sold tokenuri", "رصيد الرموز");
        Add("WgDash.Kpi.TokenUsage", "Tokenverbruik regio", "Region token usage", "Zużycie tokenów regionu", "Consum tokenuri regiune", "استهلاك رموز المنطقة");
        Add("WgDash.Kpi.TokenUsageShort", "Tokenverbruik", "Token usage", "Zużycie tokenów", "Consum tokenuri", "استهلاك الرموز");
        Add("WgDash.Kpi.TokenUsageValue", "{0} van {1} toegewezen", "{0} of {1} allocated", "{0} z {1} przydzielonych", "{0} din {1} alocate", "{0} من {1} مخصصة");
        Add("WgDash.Kpi.Runway", "± {0} weken bij huidig verbruik", "± {0} weeks at current usage", "± {0} tyg. przy obecnym zużyciu", "± {0} săpt. la consum curent", "± {0} أسابيع بالاستهلاك الحالي");
        Add("WgDash.Kpi.RunwayShort", "± {0} weken", "± {0} weeks", "± {0} tyg.", "± {0} săpt.", "± {0} أسابيع");
        Add("WgDash.Days", "{0:0.#} d", "{0:0.#} d", "{0:0.#} d", "{0:0.#} z", "{0:0.#} ي");
        Add("WgDash.Faster", "{0} d sneller", "{0} d faster", "{0} d szybciej", "{0} z mai rapid", "{0} ي أسرع");
        Add("WgDash.Slower", "{0} d langzamer", "{0} d slower", "{0} d wolniej", "{0} z mai lent", "{0} ي أبطأ");
        Add("WgDash.Period.7d", "7 d", "7 d", "7 d", "7 z", "7 ي");
        Add("WgDash.Period.30d", "30 d", "30 d", "30 d", "30 z", "30 ي");
        Add("WgDash.Period.90d", "90 d", "90 d", "90 d", "90 z", "90 ي");
        Add("WgDash.Raamflyer", "Raamflyer", "Window flyer", "Ulotka witryny", "Flyer vitrină", "منشور الواجهة");
        Add("WgDash.RaamflyerHint", "Download wervingsmateriaal voor je vestiging.", "Download recruitment materials for your branch.", "Pobierz materiały rekrutacyjne dla placówki.", "Descarcă materiale de recrutare pentru filială.", "حمّل مواد التوظيف لفرعك.");

        // Te doen
        Add("WgTodo.Lead", "Wat aandacht vraagt in je huidige bereik.", "What needs attention in your current scope.", "Co wymaga uwagi w bieżącym zakresie.", "Ce necesită atenție în domeniul curent.", "ما يحتاج انتباهاً في نطاقك الحالي.");
        Add("WgTodo.Filters", "Filters", "Filters", "Filtry", "Filtre", "عوامل التصفية");
        Add("WgTodo.Filter.All", "Alles", "All", "Wszystko", "Tot", "الكل");
        Add("WgTodo.Severity.Danger", "Urgent", "Urgent", "Pilne", "Urgent", "عاجل");
        Add("WgTodo.Severity.Warning", "Aandacht", "Attention", "Uwaga", "Atenție", "انتباه");
        Add("WgTodo.Severity.Info", "Ter info", "For info", "Informacyjnie", "Informativ", "للعلم");
        Add("WgTodo.Kind.PublishRequests", "Publicatieaanvragen", "Publish requests", "Wnioski o publikację", "Cereri publicare", "طلبات النشر");
        Add("WgTodo.Kind.ApplicationsOverdue", "Sollicitaties > 48 uur", "Applications > 48h", "Aplikacje > 48 godz.", "Aplicații > 48 ore", "طلبات > 48 ساعة");
        Add("WgTodo.Kind.VacanciesExpiring", "Vacatures verlopen", "Vacancies expiring", "Oferty wygasają", "Posturi expiră", "وظائف تنتهي");
        Add("WgTodo.Kind.LowTokens", "Lage tokensaldo’s", "Low token balances", "Niskie salda tokenów", "Solduri tokenuri mici", "أرصدة رموز منخفضة");
        Add("WgTodo.Kind.NoManager", "Zonder vestigingsmanager", "Without branch manager", "Bez menedżera placówki", "Fără manager de filială", "بدون مدير فرع");
        Add("WgTodo.Kind.Takeovers", "Overnames", "Takeovers", "Przejęcia", "Preluări", "الاستحواذات");
        Add("WgTodo.Action.Beoordelen", "Beoordelen", "Review", "Oceń", "Evaluează", "مراجعة");
        Add("WgTodo.Action.Bekijken", "Bekijken", "View", "Zobacz", "Vezi", "عرض");
        Add("WgTodo.Action.Verlengen", "Verlengen", "Extend", "Przedłuż", "Prelungește", "تمديد");
        Add("WgTodo.Action.TokensVerdelen", "Tokens verdelen", "Allocate tokens", "Przydziel tokeny", "Alocă tokenuri", "توزيع الرموز");
        Add("WgTodo.Action.TokensAanvragen", "Tokens aanvragen", "Request tokens", "Poproś o tokeny", "Cere tokenuri", "طلب رموز");
        Add("WgTodo.Action.IemandUitnodigen", "Iemand uitnodigen", "Invite someone", "Zaproś kogoś", "Invită pe cineva", "دعوة شخص");
        Add("WgTodo.PublishRequests.Title", "{0} publicatieaanvragen wachten op jouw goedkeuring", "{0} publish requests await your approval", "{0} wnioski o publikację czekają na zatwierdzenie", "{0} cereri de publicare așteaptă aprobarea", "{0} طلبات نشر بانتظار موافقتك");
        Add("WgTodo.PublishRequests.Meta", "{0}", "{0}", "{0}", "{0}", "{0}");
        Add("WgTodo.PublishRequests.MetaRm", "De bedrijfsmanager keurt goed · {0}", "The company manager approves · {0}", "Menedżer firmy zatwierdza · {0}", "Managerul de firmă aprobă · {0}", "مدير الشركة يوافق · {0}");
        Add("WgTodo.PublishRequests.MetaVm", "Wacht op bedrijfsmanager · {0}", "Waiting for company manager · {0}", "Czeka na menedżera firmy · {0}", "Așteaptă managerul de firmă · {0}", "بانتظار مدير الشركة · {0}");
        Add("WgTodo.ApplicationsOverdue.Title", "{0} sollicitaties langer dan 48 uur zonder reactie", "{0} applications without a response for over 48 hours", "{0} aplikacji bez odpowiedzi ponad 48 godzin", "{0} aplicații fără răspuns de peste 48 de ore", "{0} طلبات بلا رد لأكثر من 48 ساعة");
        Add("WgTodo.ApplicationsOverdue.Meta", "{0}", "{0}", "{0}", "{0}", "{0}");
        Add("WgTodo.VacanciesExpiring.Title", "{0} vacatures verlopen binnen 7 dagen", "{0} vacancies expire within 7 days", "{0} ofert wygasa w ciągu 7 dni", "{0} posturi expiră în 7 zile", "{0} وظائف تنتهي خلال 7 أيام");
        Add("WgTodo.VacanciesExpiring.Meta", "{0}", "{0}", "{0}", "{0}", "{0}");
        Add("WgTodo.LowTokens.Title", "{0} heeft nog {1} tokens", "{0} still has {1} tokens", "{0} ma jeszcze {1} tokenów", "{0} mai are {1} tokenuri", "{0} لديه {1} رموز متبقية");
        Add("WgTodo.LowTokens.Meta", "Tokens verdelen naar vestigingen met tekort.", "Allocate tokens to branches that are short.", "Przydziel tokeny placówkom z niedoborem.", "Alocă tokenuri filialelor cu deficit.", "وزّع الرموز على الفروع الناقصة.");
        Add("WgTodo.LowTokens.MetaVm", "Vraag tokens aan bij je bedrijfsmanager.", "Ask your company manager for tokens.", "Poproś menedżera firmy o tokeny.", "Cere tokenuri managerului de firmă.", "اطلب الرموز من مدير شركتك.");
        Add("WgTodo.NoManager.Title", "{0} heeft nog geen vestigingsmanager", "{0} still has no branch manager", "{0} nie ma jeszcze menedżera placówki", "{0} nu are încă manager de filială", "{0} ليس لديه مدير فرع بعد");
        Add("WgTodo.NoManager.Meta", "Nodig iemand uit voor deze vestiging.", "Invite someone for this branch.", "Zaproś kogoś do tej placówki.", "Invită pe cineva pentru această filială.", "ادعُ شخصاً لهذا الفرع.");
        Add("WgTodo.Takeovers.Title", "Overnameverzoek voor vestiging {0}", "Takeover request for branch {0}", "Wniosek o przejęcie placówki {0}", "Cerere de preluare pentru filiala {0}", "طلب استحواذ على الفرع {0}");
        Add("WgTodo.Takeovers.Meta", "Aangevraagd op {0}", "Requested on {0}", "Złożono {0}", "Solicitat pe {0}", "طُلب في {0}");
    }
}
