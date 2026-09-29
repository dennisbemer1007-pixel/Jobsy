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
    }
}
