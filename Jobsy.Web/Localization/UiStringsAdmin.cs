namespace Jobsy.Web.Localization;

/// <summary>Admin shell / nav / shared UI strings (nl final; en + natural pl/ro/ar).</summary>
public static class UiStringsAdmin
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string? plText = null, string? roText = null, string? arText = null)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText ?? enText;
            ro[key] = roText ?? enText;
            ar[key] = arText ?? enText;
        }

        // Groups
        Add("AdminNav.Group.Overview", "Overzicht", "Overview", "Przegląd", "Prezentare", "نظرة عامة");
        Add("AdminNav.Group.Users", "Gebruikers & rollen", "Users & roles", "Użytkownicy i role", "Utilizatori și roluri", "المستخدمون والأدوار");
        Add("AdminNav.Group.Organisations", "Organisaties", "Organisations", "Organizacje", "Organizații", "المنظمات");
        Add("AdminNav.Group.Candidates", "Kandidaten & tests", "Candidates & tests", "Kandydaci i testy", "Candidați și teste", "المرشحون والاختبارات");
        Add("AdminNav.Group.Vacancies", "Vacatures & matching", "Vacancies & matching", "Oferty i matching", "Joburi și matching", "الوظائف والمطابقة");
        Add("AdminNav.Group.Finance", "Financiën", "Finance", "Finanse", "Finanțe", "المالية");
        Add("AdminNav.Group.Content", "Content & opleidingen", "Content & training", "Treść i szkolenia", "Conținut și training", "المحتوى والتدريب");
        Add("AdminNav.Group.Settings", "Platforminstellingen", "Platform settings", "Ustawienia platformy", "Setări platformă", "إعدادات المنصة");
        Add("AdminNav.Group.Security", "Beveiliging & audit", "Security & audit", "Bezpieczeństwo i audyt", "Securitate și audit", "الأمن والتدقيق");

        // Items (= h1 = crumb)
        Add("AdminNav.Dashboard", "Dashboard", "Dashboard", "Panel", "Tablou", "لوحة التحكم");
        Add("AdminNav.Todo", "Te doen", "To do", "Do zrobienia", "De făcut", "للقيام");
        Add("AdminNav.Feedback", "Feedback", "Feedback", "Opinie", "Feedback", "ملاحظات");
        Add("AdminNav.AllUsers", "Alle gebruikers", "All users", "Wszyscy użytkownicy", "Toți utilizatorii", "كل المستخدمين");
        Add("AdminNav.Roles", "Rollen & rechten", "Roles & permissions", "Role i uprawnienia", "Roluri și drepturi", "الأدوار والصلاحيات");
        Add("AdminNav.SalesAmbassadors", "Sales & ambassadeurs", "Sales & ambassadors", "Sprzedaż i ambasadorzy", "Sales și ambasadori", "المبيعات والسفراء");
        Add("AdminNav.Companies", "Bedrijven & vestigingen", "Companies & branches", "Firmy i oddziały", "Companii și filiale", "الشركات والفروع");
        Add("AdminNav.Regions", "Regio's & domeinen", "Regions & domains", "Regiony i domeny", "Regiuni și domenii", "المناطق والنطاقات");
        Add("AdminNav.Requests", "Aanvragen", "Requests", "Wnioski", "Cereri", "الطلبات");
        Add("AdminNav.Candidates", "Kandidaten", "Candidates", "Kandydaci", "Candidați", "المرشحون");
        Add("AdminNav.Vacancies", "Vacatures", "Vacancies", "Oferty", "Joburi", "الوظائف");
        Add("AdminNav.Ats", "ATS-import", "ATS import", "Import ATS", "Import ATS", "استيراد ATS");
        Add("AdminNav.Moderation", "Moderatie", "Moderation", "Moderacja", "Moderare", "الإشراف");
        Add("AdminNav.CategoriesWages", "Categorieën & salaris", "Categories & wages", "Kategorie i wynagrodzenia", "Categorii și salarii", "الفئات والرواتب");
        Add("AdminNav.Revenue", "Omzet & transacties", "Revenue & transactions", "Przychody i transakcje", "Venituri și tranzacții", "الإيرادات والمعاملات");
        Add("AdminNav.Pricing", "Prijzen & pakketten", "Prices & packages", "Ceny i pakiety", "Prețuri și pachete", "الأسعار والباقات");
        Add("AdminNav.Goodwill", "Goodwill & tokens", "Goodwill & tokens", "Goodwill i tokeny", "Goodwill și tokenuri", "حسن النية والرموز");
        Add("AdminNav.Payouts", "Uitbetalingen & btw", "Payouts & VAT", "Wypłaty i VAT", "Plăți și TVA", "المدفوعات وضريبة القيمة المضافة");
        Add("AdminNav.PagesFlyer", "Pagina's & flyer", "Pages & flyer", "Strony i ulotka", "Pagini și flyer", "الصفحات والنشرة");
        Add("AdminNav.Training", "Opleidingen", "Training", "Szkolenia", "Traininguri", "التدريب");
        Add("AdminNav.Masterdata", "Stamgegevens", "Master data", "Dane podstawowe", "Date de bază", "البيانات الأساسية");
        Add("AdminNav.Emails", "E-mails & meldingen", "E-mails & notifications", "E-maile i powiadomienia", "E-mailuri și notificări", "البريد والإشعارات");
        Add("AdminNav.Features", "Functies", "Features", "Funkcje", "Funcții", "الميزات");
        Add("AdminNav.General", "Algemeen", "General", "Ogólne", "General", "عام");
        Add("AdminNav.Integrations", "Integraties & API", "Integrations & API", "Integracje i API", "Integrări și API", "التكاملات وAPI");
        Add("AdminNav.AuditLog", "Auditlog", "Audit log", "Dziennik audytu", "Jurnal de audit", "سجل التدقيق");
        Add("AdminNav.DataAccess", "Gegevensinzage", "Data access", "Dostęp do danych", "Acces la date", "الاطلاع على البيانات");
        Add("AdminNav.MfaSessions", "2FA & sessies", "2FA & sessions", "2FA i sesje", "2FA și sesiuni", "2FA والجلسات");
        Add("AdminNav.Privacy", "Privacy & AVG", "Privacy & GDPR", "Prywatność i RODO", "Confidențialitate și GDPR", "الخصوصية وAVG");
        Add("AdminNav.SystemLogs", "Systeemlogs", "System logs", "Logi systemowe", "Jurnale de sistem", "سجلات النظام");

        // Shell
        Add("AdminShell.Beheer", "Beheer", "Admin", "Administracja", "Administrare", "الإدارة");
        Add("AdminShell.Breadcrumb", "Kruimelpad", "Breadcrumb", "Ścieżka", "Breadcrumb", "مسار التنقل");
        Add("AdminShell.ToLobsy", "Naar Lobsy", "To Lobsy", "Do Lobsy", "Către Lobsy", "إلى Lobsy");
        Add("AdminShell.ToAdmin", "Beheer", "Admin", "Administracja", "Administrare", "الإدارة");
        Add("AdminShell.OpenMenu", "Menu openen", "Open menu", "Otwórz menu", "Deschide meniul", "فتح القائمة");
        Add("AdminShell.CloseMenu", "Menu sluiten", "Close menu", "Zamknij menu", "Închide meniul", "إغلاق القائمة");
        Add("AdminShell.SearchPlaceholder", "Zoek gebruiker, bedrijf, vacature of factuur…", "Search user, company, vacancy or invoice…", "Szukaj użytkownika, firmy, oferty lub faktury…", "Caută utilizator, firmă, job sau factură…", "ابحث عن مستخدم أو شركة أو وظيفة أو فاتورة…");
        Add("AdminShell.SearchShortcut", "Ctrl K", "Ctrl K");
        Add("AdminShell.SearchOpen", "Zoeken", "Search", "Szukaj", "Căutare", "بحث");
        Add("AdminShell.SearchNoResults", "Geen resultaten", "No results", "Brak wyników", "Niciun rezultat", "لا نتائج");
        Add("AdminShell.SearchUsers", "Gebruikers", "Users", "Użytkownicy", "Utilizatori", "المستخدمون");
        Add("AdminShell.SearchOrgs", "Organisaties", "Organisations", "Organizacje", "Organizații", "المنظمات");
        Add("AdminShell.SearchVacancies", "Vacatures", "Vacancies", "Oferty", "Joburi", "الوظائف");
        Add("AdminShell.SearchInvoices", "Facturen", "Invoices", "Faktury", "Facturi", "الفواتير");
        Add("AdminShell.RoleAdmin", "Beheerder", "Administrator", "Administrator", "Administrator", "مسؤول");
        Add("AdminShell.MfaActive", "2FA actief", "2FA active", "2FA aktywne", "2FA activ", "2FA نشط");
        Add("AdminShell.EnvAcceptatieTitle", "Je werkt in Acceptatie", "You are in Acceptatie", "Pracujesz w Acceptatie", "Lucrezi în Acceptatie", "أنت في Acceptatie");
        Add("AdminShell.EnvProductieTitle", "Let op: dit is Productie", "Warning: this is Production", "Uwaga: to jest produkcja", "Atenție: aceasta este producția", "تحذير: هذه بيئة الإنتاج");
        Add("AdminShell.EnvLokaalTitle", "Je werkt lokaal", "You are on a local environment", "Pracujesz lokalnie", "Lucrezi local", "أنت في بيئة محلية");
        Add("AdminShell.CompanyLead", "Gegevens van Lobsy zelf, voor facturen en e-mails.", "Lobsy's own company details, for invoices and e-mails.", "Dane samej Lobsy, do faktur i e-maili.", "Datele Lobsy pentru facturi și e-mailuri.", "بيانات Lobsy نفسها للفواتير والبريد.");

        // Tabs
        Add("AdminTabs.Categories", "Categorieën", "Categories", "Kategorie", "Categorii", "الفئات");
        Add("AdminTabs.Wages", "Salaris & WML", "Wages & WML", "Wynagrodzenia i WML", "Salarii și WML", "الرواتب وWML");
        Add("AdminTabs.About", "Wie zijn wij", "About us", "O nas", "Despre noi", "من نحن");
        Add("AdminTabs.Flyer", "Werkgeversflyer", "Employer flyer", "Ulotka pracodawcy", "Flyer angajator", "نشرة أصحاب العمل");
        Add("AdminTabs.Masterdata", "Stamgegevens", "Master data", "Dane podstawowe", "Date de bază", "البيانات الأساسية");
        Add("AdminTabs.Exclusivity", "Exclusiviteit stages", "Exclusivity stages", "Etapy wyłączności", "Etape exclusivitate", "مراحل الحصرية");
        Add("AdminTabs.Integrations", "Integraties", "Integrations", "Integracje", "Integrări", "التكاملات");
        Add("AdminTabs.ApiKeys", "API-sleutels", "API keys", "Klucze API", "Chei API", "مفاتيح API");
        Add("AdminTabs.SalesManagers", "Salesmanagers", "Sales managers", "Menedżerowie sprzedaży", "Manageri de vânzări", "مديرو المبيعات");
        Add("AdminTabs.Ambassadors", "Ambassadeurs", "Ambassadors", "Ambasadorzy", "Ambasadori", "السفراء");

        // Shared UI primitives
        Add("AdminUi.Empty", "Nog niets hier.", "Nothing here yet.", "Na razie nic tu nie ma.", "Încă nu e nimic aici.", "لا يوجد شيء هنا بعد.");
        Add("AdminUi.Loading", "Laden…", "Loading…", "Ładowanie…", "Se încarcă…", "جارٍ التحميل…");
        Add("AdminUi.Selected", "{0} geselecteerd", "{0} selected", "{0} zaznaczonych", "{0} selectate", "{0} محدد");
        Add("AdminUi.ClearSelection", "Selectie wissen", "Clear selection", "Wyczyść wybór", "Șterge selecția", "مسح التحديد");
        Add("AdminUi.PrevPage", "Vorige", "Previous", "Poprzednia", "Anterior", "السابق");
        Add("AdminUi.NextPage", "Volgende", "Next", "Następna", "Următor", "التالي");
        Add("AdminUi.PageOf", "Pagina {0} van {1}", "Page {0} of {1}", "Strona {0} z {1}", "Pagina {0} din {1}", "صفحة {0} من {1}");
        Add("AdminUi.CloseDrawer", "Sluiten", "Close", "Zamknij", "Închide", "إغلاق");
        Add("AdminUi.FilterSearch", "Zoeken…", "Search…", "Szukaj…", "Căutare…", "بحث…");
        Add("AdminUi.All", "Alles", "All", "Wszystko", "Tot", "الكل");

        // Dashboard (02)
        Add("AdminDash.Lead", "alles wat vandaag aandacht vraagt op één plek.", "everything that needs attention today in one place.", "wszystko, co dziś wymaga uwagi, w jednym miejscu.", "tot ce cere atenție azi într-un singur loc.", "كل ما يحتاج انتباهًا اليوم في مكان واحد.");
        Add("AdminDash.Greeting.Morning", "Goedemorgen", "Good morning", "Dzień dobry", "Bună dimineața", "صباح الخير");
        Add("AdminDash.Greeting.Afternoon", "Goedemiddag", "Good afternoon", "Dzień dobry", "Bună ziua", "مساء الخير");
        Add("AdminDash.Greeting.Evening", "Goedenavond", "Good evening", "Dobry wieczór", "Bună seara", "مساء الخير");
        Add("AdminDash.Period", "Periode", "Period", "Okres", "Perioadă", "الفترة");
        Add("AdminDash.Period.Today", "Vandaag", "Today", "Dziś", "Azi", "اليوم");
        Add("AdminDash.Period.Week", "7 dagen", "7 days", "7 dni", "7 zile", "7 أيام");
        Add("AdminDash.Period.Month", "30 dagen", "30 days", "30 dni", "30 zile", "30 يومًا");
        Add("AdminDash.Period.Quarter", "Kwartaal", "Quarter", "Kwartał", "Trimestru", "ربع سنة");
        Add("AdminDash.Period.TodayShort", "vandaag", "today", "dziś", "azi", "اليوم");
        Add("AdminDash.Period.WeekShort", "7 d", "7 days", "7 dni", "7 zile", "٧ أيام");
        Add("AdminDash.Period.MonthShort", "30 d", "30 days", "30 dni", "30 zile", "٣٠ يومًا");
        Add("AdminDash.Period.QuarterShort", "kwartaal", "quarter", "kwartał", "trimestru", "ربع");
        Add("AdminDash.Kpi.Candidates", "Actieve kandidaten", "Active candidates", "Aktywni kandydaci", "Candidați activi", "مرشحون نشطون");
        Add("AdminDash.Kpi.Employers", "Actieve werkgevers", "Active employers", "Aktywni pracodawcy", "Angajatori activi", "أصحاب عمل نشطون");
        Add("AdminDash.Kpi.Vacancies", "Vacatures live", "Live vacancies", "Oferty live", "Joburi live", "وظائف مباشرة");
        Add("AdminDash.Kpi.Applications", "Sollicitaties", "Applications", "Aplikacje", "Aplicări", "طلبات");
        Add("AdminDash.Kpi.Revenue", "Omzet", "Revenue", "Przychód", "Venit", "الإيرادات");
        Add("AdminDash.Todo.ViewAll", "Alles bekijken →", "View all →", "Zobacz wszystko →", "Vezi tot →", "عرض الكل →");
        Add("AdminDash.Todo.Empty", "Niets te doen. Mooi zo.", "Nothing to do. Nice.", "Nic do zrobienia. Super.", "Nimic de făcut. Super.", "لا مهام. رائع.");
        Add("AdminDash.Todo.EmptyHint", "We laten het hier zien zodra er iets is.", "We'll show items here as soon as there are any.", "Pokażemy je tutaj, gdy coś się pojawi.", "Le arătăm aici când apar.", "نظهرها هنا فورًا عند وجود شيء.");
        Add("AdminDash.Todo.PageLead", "Alles wat nu aandacht vraagt, op één plek.", "Everything that needs attention now, in one place.", "Wszystko, co wymaga uwagi, w jednym miejscu.", "Tot ce cere atenție acum, într-un loc.", "كل ما يحتاج انتباهًا الآن في مكان واحد.");
        Add("AdminDash.SystemStatus", "Systeemstatus", "System status", "Status systemu", "Stare sistem", "حالة النظام");
        Add("AdminDash.SystemLogsLink", "Systeemlogs →", "System logs →", "Logi systemowe →", "Jurnale →", "سجلات النظام →");
        Add("AdminDash.PlatformMode", "Platform-modus", "Platform mode", "Tryb platformy", "Mod platformă", "وضع المنصة");
        Add("AdminDash.FeaturesLink", "Functies →", "Features →", "Funkcje →", "Funcții →", "الميزات →");
        Add("AdminDash.AllKpis", "Alle KPI's en drilldown", "All KPIs and drilldown", "Wszystkie KPI i drilldown", "Toate KPI și drilldown", "كل مؤشرات الأداء والتفصيل");
        Add("AdminDash.Mode.AiModeration", "AI-vacaturemoderatie", "AI vacancy moderation", "Moderacja AI ofert", "Moderare AI joburi", "إشراف AI على الوظائف");
        Add("AdminDash.Mode.Mfa", "Tweestapsverificatie", "Two-factor authentication", "Uwierzytelnianie dwuskładnikowe", "Autentificare în doi pași", "التحقق بخطوتين");
        Add("AdminDash.Mode.On", "Aan", "On", "Włączone", "Pornit", "تشغيل");
        Add("AdminDash.Mode.Off", "Uit", "Off", "Wyłączone", "Oprit", "إيقاف");
        Add("AdminDash.Mode.Required", "Verplicht", "Required", "Wymagane", "Obligatoriu", "إلزامي");
        Add("AdminDash.Moderation.Lead", "Vacatures die de AI-moderatie heeft tegengehouden. Pas de tekst aan of keur handmatig goed.", "Vacancies blocked by AI moderation. Edit the text or approve manually.", "Oferty zablokowane przez moderację AI. Edytuj tekst lub zatwierdź ręcznie.", "Joburi blocate de moderarea AI. Editează textul sau aprobă manual.", "وظائف أوقفها إشراف AI. عدّل النص أو وافق يدويًا.");

        Add("AdminTodo.KvkFailed.Title", "KvK-controle mislukt", "Chamber of Commerce check failed", "Kontrola KVK nieudana", "Verificare KvK eșuată", "فشل فحص غرفة التجارة");
        Add("AdminTodo.KvkFailed.Action", "Controleren", "Review", "Sprawdź", "Verifică", "مراجعة");
        Add("AdminTodo.Takeover.Title", "Overnameverzoek vestiging", "Branch takeover request", "Wniosek o przejęcie oddziału", "Cerere preluare filială", "طلب استحواذ على فرع");
        Add("AdminTodo.Takeover.Action", "Beoordelen", "Review", "Oceń", "Evaluează", "تقييم");
        Add("AdminTodo.Moderation.Title", "{0} vacatures gemarkeerd door moderatie", "{0} vacancies flagged by moderation", "{0} ofert oznaczonych przez moderację", "{0} joburi marcate de moderare", "{0} وظائف مميزة بالإشراف");
        Add("AdminTodo.Moderation.Action", "Bekijken", "View", "Zobacz", "Vezi", "عرض");
        Add("AdminTodo.SalesApp.Title", "Aanmelding salesmanager", "Sales manager application", "Wniosek salesmanagera", "Aplicare sales manager", "طلب مدير مبيعات");
        Add("AdminTodo.SalesApp.Action", "Beoordelen", "Assess", "Oceń", "Evaluează", "تقييم");
        Add("AdminTodo.OpenPayouts.Title", "Facturen salesmanagers open", "Open sales manager invoices", "Otwarte faktury salesmanagerów", "Facturi sales manager deschise", "فواتير مديري المبيعات المفتوحة");
        Add("AdminTodo.OpenPayouts.Action", "Bekijken", "View", "Zobacz", "Vezi", "عرض");
        Add("AdminTodo.Feedback.Title", "{0} nieuwe feedbackmeldingen", "{0} new feedback items", "{0} nowych zgłoszeń feedbacku", "{0} feedback-uri noi", "{0} ملاحظات جديدة");
        Add("AdminTodo.Feedback.Action", "Openen", "Open", "Otwórz", "Deschide", "فتح");
        Add("AdminTodo.Col.Title", "Titel", "Title", "Tytuł", "Titlu", "العنوان");
        Add("AdminTodo.Col.Area", "Onderdeel", "Area", "Obszar", "Zonă", "القسم");
        Add("AdminTodo.Col.Since", "Sinds", "Since", "Od", "De la", "منذ");
        Add("AdminTodo.Col.Severity", "Ernst", "Severity", "Ważność", "Severitate", "الخطورة");
        Add("AdminTodo.Severity.All", "Alle ernst", "All severities", "Wszystkie poziomy", "Toate nivelurile", "كل المستويات");
        Add("AdminTodo.Severity.Danger", "Kritiek", "Critical", "Krytyczne", "Critic", "حرج");
        Add("AdminTodo.Severity.Warn", "Let op", "Warning", "Ostrzeżenie", "Avertisment", "تحذير");
        Add("AdminTodo.Severity.Info", "Info", "Information", "Informacja", "Informație", "معلومة");

        // Users redesign (03)
        Add("AdminUsers.Lead",
            "Persoonsgegevens zijn standaard gemaskeerd (AVG). Inzien kan alleen via support-toegang.",
            "Personal data is masked by default (GDPR). Viewing requires support access.",
            "Dane osobowe są domyślnie zamaskowane (RODO). Podgląd wymaga dostępu support.",
            "Datele personale sunt mascate implicit (GDPR). Vizualizarea necesită acces support.",
            "البيانات الشخصية مقنّعة افتراضياً. الاطلاع يتطلب صلاحية دعم.");
        Add("AdminUsers.CandidatesLead",
            "Kandidaten op het platform. Gegevens zijn standaard gemaskeerd.",
            "Candidates on the platform. Data is masked by default.",
            "Kandydaci na platformie. Dane są domyślnie zamaskowane.",
            "Candidați pe platformă. Datele sunt mascate implicit.",
            "المرشحون على المنصة. البيانات مقنّعة افتراضياً.");
        Add("AdminUsers.RolesLead",
            "Overzicht van rollen, aantallen en of 2FA verplicht is. Geen nieuw rechtenmodel.",
            "Overview of roles, counts and whether 2FA is required. No new permissions model.",
            "Przegląd ról, liczb i obowiązku 2FA. Bez nowego modelu uprawnień.",
            "Prezentare roluri, numărări și dacă 2FA e obligatoriu. Fără model nou de drepturi.",
            "نظرة على الأدوار والأعداد وهل 2FA إلزامي. بلا نموذج صلاحيات جديد.");
        Add("AdminUsers.SalesLead",
            "Salesmanagers en ambassadeurs. Uitbetalingen staan bij Financiën.",
            "Sales managers and ambassadors. Payouts live under Finance.",
            "Menedżerowie sprzedaży i ambasadorzy. Wypłaty są w Finansach.",
            "Manageri de vânzări și ambasadori. Plățile sunt la Finanțe.",
            "مديرو المبيعات والسفراء. المدفوعات في المالية.");
        Add("AdminUsers.SalesPayoutsLink",
            "Uitbetalingen staan bij Financiën › Uitbetalingen & btw",
            "Payouts are under Finance › Payouts & VAT",
            "Wypłaty są w Finanse › Wypłaty i VAT",
            "Plățile sunt la Finanțe › Plăți și TVA",
            "المدفوعات في المالية › المدفوعات وضريبة القيمة المضافة");
        Add("AdminUsers.SearchPlaceholder", "Zoek op naam, e-mail of ID…", "Search by name, e-mail or ID…", "Szukaj po nazwisku, e-mailu lub ID…", "Caută după nume, e-mail sau ID…", "ابحث بالاسم أو البريد أو المعرّف…");
        Add("AdminUsers.FilterRole", "Rol", "Role", "Rola", "Rolul", "الدور");
        Add("AdminUsers.FilterMfa", "2FA", "2FA", "2FA", "2FA", "2FA");
        Add("AdminUsers.FilterStatus", "Status", "Status", "Status", "Status", "الحالة");
        Add("AdminUsers.FilterOrg", "Organisatie", "Organisation", "Organizacja", "Organizație", "المنظمة");
        Add("AdminUsers.Empty", "Geen gebruikers gevonden.", "No users found.", "Brak użytkowników.", "Niciun utilizator.", "لا مستخدمين.");
        Add("AdminUsers.ColUser", "Gebruiker", "User", "Użytkownik", "Utilizator", "المستخدم");
        Add("AdminUsers.ColRole", "Rol", "Role", "Rola", "Rolul", "الدور");
        Add("AdminUsers.ColOrg", "Organisatie", "Organisation", "Organizacja", "Organizație", "المنظمة");
        Add("AdminUsers.ColMfa", "2FA", "2FA", "2FA", "2FA", "2FA");
        Add("AdminUsers.ColLastActive", "Laatst actief", "Last active", "Ostatnio aktywny", "Ultima activitate", "آخر نشاط");
        Add("AdminUsers.ColStatus", "Status", "Status", "Status", "Status", "الحالة");
        Add("AdminUsers.TabAll", "Alle", "All", "Wszyscy", "Toți", "الكل");
        Add("AdminUsers.TabCandidates", "Kandidaten", "Candidates", "Kandydaci", "Candidați", "المرشحون");
        Add("AdminUsers.TabEmployers", "Werkgevers", "Employers", "Pracodawcy", "Angajatori", "أصحاب العمل");
        Add("AdminUsers.TabSales", "Sales & partners", "Sales and partners", "Sprzedaż i partnerzy", "Sales și parteneri", "المبيعات والشركاء");
        Add("AdminUsers.TabAdmins", "Beheerders", "Admins", "Administratorzy", "Administratori", "المسؤولون");
        Add("AdminUsers.TabOverview", "Overzicht", "Overview", "Przegląd", "Prezentare", "نظرة عامة");
        Add("AdminUsers.TabRoles", "Rollen", "Roles", "Role", "Roluri", "الأدوار");
        Add("AdminUsers.TabSecurity", "Beveiliging", "Security", "Bezpieczeństwo", "Securitate", "الأمن");
        Add("AdminUsers.MfaOn", "Aan", "On", "Włączone", "Pornit", "تشغيل");
        Add("AdminUsers.MfaOff", "Niet ingesteld", "Not set up", "Nie ustawione", "Nesetat", "غير مضبوط");
        Add("AdminUsers.MfaExternal", "Via Microsoft/Google", "Through Microsoft/Google", "Przez Microsoft/Google", "Prin Microsoft/Google", "عبر Microsoft/Google");
        Add("AdminUsers.MfaExternalShort", "Via IdP", "Through IdP", "Przez IdP", "Prin IdP", "عبر IdP");
        Add("AdminUsers.StatusActive", "Actief", "Active", "Aktywny", "Activ", "نشط");
        Add("AdminUsers.StatusBlocked", "Geblokkeerd", "Blocked", "Zablokowany", "Blocat", "محظور");
        Add("AdminUsers.ActiveNow", "Nu actief", "Active now", "Aktywny teraz", "Activ acum", "نشط الآن");
        Add("AdminUsers.BulkEndSessions", "Sessies beëindigen", "End sessions", "Zakończ sesje", "Încheie sesiuni", "إنهاء الجلسات");
        Add("AdminUsers.BulkBlock", "Blokkeren", "Block", "Zablokuj", "Blochează", "حظر");
        Add("AdminUsers.BulkLead", "Geef een reden (verplicht). De actie geldt voor elke geselecteerde gebruiker.", "Provide a required reason. The action runs for each selected user.", "Podaj obowiązkowy powód.", "Oferă un motiv obligatoriu.", "قدّم سبباً إلزامياً.");
        Add("AdminUsers.BulkResult", "{0} gelukt · {1} overgeslagen", "{0} succeeded · {1} skipped", "{0} OK · {1} pominięto", "{0} reușite · {1} omise", "{0} نجحت · {1} تم تخطيها");
        Add("AdminUsers.FieldEmail", "E-mail", "E-mail", "E-mail", "E-mail", "البريد");
        Add("AdminUsers.FieldPhone", "Telefoon", "Phone", "Telefon", "Telefon", "الهاتف");
        Add("AdminUsers.FieldCreated", "Aangemaakt", "Created", "Utworzono", "Creat", "تاريخ الإنشاء");
        Add("AdminUsers.FieldEarly", "Early adapter", "Early adopter", "Wczesny adapter", "Early adopter", "متبنٍ مبكر");
        Add("AdminUsers.OpenOrg", "Organisatie openen", "Open organisation", "Otwórz organizację", "Deschide organizația", "فتح المنظمة");
        Add("AdminUsers.NoMemberships", "Geen extra lidmaatschappen.", "No extra memberships.", "Brak dodatkowych członkostw.", "Fără apartenențe extra.", "لا عضويات إضافية.");
        Add("AdminUsers.RoleChangeHint", "Rol wijzigen kan voor werkgeversrollen via de organisatiepagina (PUT company-users).", "Role changes for employer roles go via the organisation page.", "Zmiana roli pracodawcy przez stronę organizacji.", "Schimbarea rolului angajator via pagina organizației.", "تغيير دور صاحب العمل عبر صفحة المنظمة.");
        Add("AdminUsers.MfaBoxTitle", "Tweestapsverificatie", "Two-factor authentication", "Uwierzytelnianie dwuskładnikowe", "Autentificare în doi pași", "التحقق بخطوتين");
        Add("AdminUsers.MfaDetailOn", "Aan · authenticator-app · ingesteld op {0}", "On · authenticator app · set up on {0}", "Włączone · aplikacja · od {0}", "Pornit · aplicație · din {0}", "تشغيل · تطبيق · منذ {0}");
        Add("AdminUsers.MfaDetailOff", "Niet ingesteld", "Not set up", "Nie ustawione", "Nesetat", "غير مضبوط");
        Add("AdminUsers.MfaDetailExternal", "Via Microsoft/Google (2FA bij je IdP)", "Via Microsoft/Google (2FA at your IdP)", "Przez Microsoft/Google (2FA w IdP)", "Via Microsoft/Google (2FA la IdP)", "عبر Microsoft/Google (2FA عند IdP)");
        Add("AdminUsers.MfaResetHelp", "Na een reset stelt de gebruiker bij de volgende login opnieuw 2FA in. Je geeft een reden op en bevestigt met je eigen 2FA-code. De actie komt in het auditlog en de gebruiker krijgt een e-mail.", "After a reset the user sets up 2FA again on next login. You provide a reason and confirm with your own 2FA code. The action is audited and the user gets an e-mail.", "Po resecie użytkownik ustawi 2FA ponownie przy logowaniu.", "După reset utilizatorul setează din nou 2FA la login.", "بعد إعادة التعيين يضبط المستخدم 2FA مجدداً عند الدخول.");
        Add("AdminUsers.MfaResetTitle", "2FA resetten voor {0}?", "Reset 2FA for {0}?", "Zresetować 2FA dla {0}?", "Resetezi 2FA pentru {0}?", "إعادة تعيين 2FA لـ {0}؟");
        Add("AdminUsers.MfaResetLead", "De authenticator wordt ontkoppeld en alle {0} sessies worden beëindigd. Bij de volgende login stelt de gebruiker 2FA opnieuw in.", "The authenticator is unlinked and all {0} sessions end. On next login the user sets up 2FA again.", "Authenticator zostanie odłączony i {0} sesji zakończonych.", "Authenticatorul e deconectat și {0} sesiuni se închid.", "يُفصل المصادّق وتُنهى {0} جلسات.");
        Add("AdminUsers.MfaResetReason", "Reden (verplicht)", "Reason (required)", "Powód (wymagany)", "Motiv (obligatoriu)", "السبب (إلزامي)");
        Add("AdminUsers.MfaResetYourCode", "Jouw 2FA-code", "Your 2FA code", "Twój kod 2FA", "Codul tău 2FA", "رمز 2FA الخاص بك");
        Add("AdminUsers.MfaResetExternalConfirm", "Bevestigd via je Microsoft/Google-sessie", "Confirmed via your Microsoft/Google session", "Potwierdzone sesją Microsoft/Google", "Confirmat via sesiunea Microsoft/Google", "مؤكّد عبر جلسة Microsoft/Google");
        Add("AdminUsers.MfaResetAuditNote", "Komt in het auditlog met jouw naam en reden. {0} krijgt hierover een e-mail.", "Goes into the audit log with your name and reason. {0} gets an e-mail.", "Trafi do dziennika audytu. {0} dostanie e-mail.", "Intră în jurnalul de audit. {0} primește e-mail.", "يُسجَّل في سجل التدقيق. {0} يتلقى بريداً.");
        Add("AdminUsers.SessionsTitle", "Actieve sessies", "Active sessions", "Aktywne sesje", "Sesiuni active", "الجلسات النشطة");
        Add("AdminUsers.EndAllSessions", "Alle sessies beëindigen", "End all sessions", "Zakończ wszystkie sesje", "Încheie toate sesiunile", "إنهاء كل الجلسات");
        Add("AdminUsers.EndSession", "Beëindigen", "End", "Zakończ", "Încheie", "إنهاء");
        Add("AdminUsers.NoSessions", "Geen actieve sessies.", "No active sessions.", "Brak aktywnych sesji.", "Nicio sesiune activă.", "لا جلسات نشطة.");
        Add("AdminUsers.SessionCurrent", "Nu actief", "Active now", "Aktywna teraz", "Activă acum", "نشطة الآن");
        Add("AdminUsers.PiiTitle", "Persoonsgegevens", "Personal data", "Dane osobowe", "Date personale", "البيانات الشخصية");
        Add("AdminUsers.PiiMaskedNote", "Gemaskeerd volgens AVG. Volledige gegevens nodig voor support? Vraag tijdelijke toegang aan (15 min, met reden). Dit wordt gelogd en de gebruiker krijgt bericht.", "Masked under GDPR. Need full data for support? Request temporary access (15 min, with reason). This is logged and the user is notified.", "Zamaskowane (RODO). Potrzebujesz pełnych danych? Poproś o tymczasowy dostęp (15 min).", "Mascate (GDPR). Ai nevoie de date complete? Cere acces temporar (15 min).", "مقنّعة وفق AVG. تحتاج بيانات كاملة؟ اطلب وصولاً مؤقتاً (15 د).");
        Add("AdminUsers.RequestSupport", "Support-toegang aanvragen", "Request support access", "Poproś o dostęp support", "Cere acces support", "طلب صلاحية دعم");
        Add("AdminUsers.GrantRemaining", "Nog {0} min · Nu intrekken", "{0} min left · Revoke now", "Jeszcze {0} min · Cofnij", "Încă {0} min · Revocă", "متبقي {0} د · إلغاء الآن");
        Add("AdminUsers.Block", "Account blokkeren", "Block account", "Zablokuj konto", "Blochează contul", "حظر الحساب");
        Add("AdminUsers.Unblock", "Deblokkeren", "Unblock", "Odblokuj", "Deblochează", "إلغاء الحظر");
        Add("AdminUsers.BlockedToast", "Account geblokkeerd.", "Account blocked.", "Konto zablokowane.", "Cont blocat.", "تم حظر الحساب.");
        Add("AdminUsers.UnblockedToast", "Account gedeblokkeerd.", "Account unblocked.", "Konto odblokowane.", "Cont deblocat.", "تم إلغاء الحظر.");
        Add("AdminUsers.RolesCount", "Aantal", "Count", "Liczba", "Număr", "العدد");
        Add("AdminUsers.RolesMfaRequired", "2FA verplicht", "2FA required", "2FA wymagane", "2FA obligatoriu", "2FA إلزامي");
        Add("AdminUsers.RolesDescription", "Beschrijving", "Description", "Opis", "Descriere", "الوصف");
        Add("AdminUsers.RolesWhat", "Wat mag deze rol?", "What can this role do?", "Co może ta rola?", "Ce poate acest rol?", "ماذا يستطيع هذا الدور؟");
        Add("AdminUsers.RolesMatrixLink", "Bekijk matrix", "View matrix", "Zobacz macierz", "Vezi matricea", "عرض المصفوفة");
        Add("AdminUsers.RolesMatrixTitle", "Rollenmatrix (samenvatting)", "Roles matrix (summary)", "Macierz ról (skrót)", "Matricea rolurilor (rezumat)", "مصفوفة الأدوار (ملخص)");
        Add("AdminUsers.RolesMatrixLead", "Samenvatting uit docs/security/roles-matrix.md. Geen nieuw rechtenmodel.", "Summary from docs/security/roles-matrix.md. No new permissions model.", "Skrót z docs/security/roles-matrix.md.", "Rezumat din docs/security/roles-matrix.md.", "ملخص من docs/security/roles-matrix.md.");
        Add("AdminUsers.RolesCapability", "Capaciteit", "Capability", "Zdolność", "Capacitate", "القدرة");
        Add("AdminUsers.Cap.OwnProfile", "Eigen profiel", "Own profile", "Własny profil", "Profil propriu", "الملف الشخصي");
        Add("AdminUsers.Cap.Vacancies", "Vacatures", "Vacancies", "Oferty", "Joburi", "الوظائف");
        Add("AdminUsers.Cap.Applications", "Sollicitaties", "Applications", "Aplikacje", "Aplicări", "الطلبات");
        Add("AdminUsers.Cap.AdminScreens", "Beheerschermen", "Admin screens", "Ekrany admina", "Ecrane admin", "شاشات الإدارة");
        Add("AdminUsers.Role.Candidate", "Kandidaat", "Candidate", "Kandydat", "Candidat", "مرشح");
        Add("AdminUsers.Role.BranchManager", "Vestigingsmanager", "Branch manager", "Kierownik oddziału", "Manager filială", "مدير فرع");
        Add("AdminUsers.Role.RegionalManager", "Regiomanager", "Regional manager", "Kierownik regionu", "Manager regional", "مدير إقليمي");
        Add("AdminUsers.Role.EnterpriseManager", "Bedrijfsmanager", "Enterprise manager", "Kierownik firmy", "Manager enterprise", "مدير الشركة");
        Add("AdminUsers.Role.Intermediary", "Intermediair", "Intermediary", "Pośrednik", "Intermediar", "وسيط");
        Add("AdminUsers.Role.Admin", "Beheerder", "Admin", "Administrator", "Administrator", "مسؤول");
        Add("AdminUsers.Role.SalesManager", "Salesmanager", "Sales manager", "Menedżer sprzedaży", "Manager vânzări", "مدير مبيعات");
        Add("AdminUsers.Role.Ambassadeur", "Ambassadeur", "Ambassador", "Ambasador", "Ambasador", "سفير");
        Add("AdminUsers.RoleDesc.Candidate", "Solliciteert en beheert eigen profiel en tests.", "Applies and manages own profile and tests.", "Aplikuje i zarządza własnym profilem.", "Aplică și gestionează profilul propriu.", "يتقدم ويدير ملفه واختباراته.");
        Add("AdminUsers.RoleDesc.BranchManager", "Beheert één vestiging: vacatures en sollicitaties.", "Manages one branch: vacancies and applications.", "Zarządza jednym oddziałem.", "Gestionează o filială.", "يدير فرعاً واحداً.");
        Add("AdminUsers.RoleDesc.RegionalManager", "Beheert meerdere vestigingen in een regio.", "Manages multiple branches in a region.", "Zarządza oddziałami w regionie.", "Gestionează filiale în regiune.", "يدير فروعاً في منطقة.");
        Add("AdminUsers.RoleDesc.EnterpriseManager", "Beheert de gehele organisatie.", "Manages the whole organisation.", "Zarządza całą organizacją.", "Gestionează întreaga organizație.", "يدير المنظمة بالكامل.");
        Add("AdminUsers.RoleDesc.Intermediary", "Werkt namens klantorganisaties.", "Works on behalf of client organisations.", "Działa w imieniu klientów.", "Lucrează pentru organizații client.", "يعمل نيابة عن عملاء.");
        Add("AdminUsers.RoleDesc.Admin", "Volledige platformtoegang.", "Full platform access.", "Pełny dostęp do platformy.", "Acces complet la platformă.", "وصول كامل للمنصة.");
        Add("AdminUsers.RoleDesc.SalesManager", "Werft werkgevers en beheert goodwill.", "Acquires employers and manages goodwill.", "Pozyskuje pracodawców.", "Atrage angajatori.", "يستقطب أصحاب العمل.");
        Add("AdminUsers.RoleDesc.Ambassadeur", "Werft kandidaten via trackingcodes en flyers.", "Acquires candidates via tracking codes and flyers.", "Pozyskuje kandydatów.", "Atrage candidați.", "يستقطب المرشحين.");
        Add("AdminUi.RangeOf", "{0}–{1} van {2}", "{0}–{1} of {2}", "{0}–{1} z {2}", "{0}–{1} din {2}", "{0}–{1} من {2}");
        Add("AdminUi.PageSize", "Per pagina", "Per page", "Na stronę", "Pe pagină", "لكل صفحة");
    }
}
