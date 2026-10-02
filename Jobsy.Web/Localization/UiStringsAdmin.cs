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
        Add("AdminNav.Flyer", "Werkgeversflyer", "Employer flyer", "Ulotka dla pracodawców", "Flyer pentru angajatori", "نشرة أصحاب العمل");
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

        // Organisations redesign (04)
        Add("AdminOrgs.Lead",
            "Klanten en intermediairs met hun vestigingen. Lobsy's eigen gegevens staan onder Platforminstellingen.",
            "Customers and intermediaries with their branches. Lobsy's own details are under Platform settings.",
            "Klienci i pośrednicy z oddziałami. Dane Lobsy są w ustawieniach platformy.",
            "Clienți și intermediari cu filialele lor. Datele Lobsy sunt la setări platformă.",
            "العملاء والوسطاء مع فروعهم. بيانات Lobsy تحت إعدادات المنصة.");
        Add("AdminOrgs.RegionsLead",
            "Domeinen bepalen branding en kaartfocus (bijv. westland.lobsy.nl). Regio's zijn groepen vestigingen binnen één organisatie.",
            "Domains drive branding and map focus (e.g. westland.lobsy.nl). Regions group branches within one organisation.",
            "Domeny sterują brandingiem i mapą. Regiony grupują oddziały w organizacji.",
            "Domeniile definesc brandingul și harta. Regiunile grupează filialele într-o organizație.",
            "النطاقات تحدد العلامة التجارية وتركيز الخريطة. المناطق تجمع الفروع داخل منظمة.");
        Add("AdminOrgs.RequestsLead",
            "KvK-controles en overnameverzoeken op één plek.",
            "Chamber of Commerce checks and takeover requests in one place.",
            "Kontrole KVK i wnioski o przejęcie w jednym miejscu.",
            "Verificări KvK și cereri de preluare într-un singur loc.",
            "فحوصات غرفة التجارة وطلبات الاستحواذ في مكان واحد.");
        Add("AdminOrgs.AddOrganisation", "Organisatie toevoegen", "Add organisation", "Dodaj organizację", "Adaugă organizație", "إضافة منظمة");
        Add("AdminOrgs.SearchPlaceholder", "Zoek op naam of KvK", "Search by name or KvK", "Szukaj po nazwie lub KvK", "Caută după nume sau KvK", "ابحث بالاسم أو KvK");
        Add("AdminOrgs.ViewMode", "Weergave", "View", "Widok", "Vizualizare", "العرض");
        Add("AdminOrgs.ViewTree", "Boom", "Tree", "Drzewo", "Arbore", "شجرة");
        Add("AdminOrgs.ViewFlat", "Plat", "Flat", "Płaski", "Listă", "مسطح");
        Add("AdminOrgs.OpenRequestsNote", "{0} registraties wachten op KvK-controle of overname.", "{0} registrations await KvK check or takeover.", "{0} rejestracji czeka na kontrolę KvK lub przejęcie.", "{0} înregistrări așteaptă verificare KvK sau preluare.", "{0} تسجيلات بانتظار فحص KvK أو استحواذ.");
        Add("AdminOrgs.OpenRequestsLink", "Aanvragen bekijken", "View requests", "Zobacz wnioski", "Vezi cererile", "عرض الطلبات");
        Add("AdminOrgs.Empty", "Geen organisaties gevonden.", "No organisations found.", "Brak organizacji.", "Nicio organizație.", "لا منظمات.");
        Add("AdminOrgs.ColOrganisation", "Organisatie", "Organisation", "Organizacja", "Organizație", "المنظمة");
        Add("AdminOrgs.ColKvk", "KvK", "CoC", "nr KVK", "nr. KvK", "رقم KvK");
        Add("AdminOrgs.ColRegion", "Regio", "Region", "Region", "Regiune", "المنطقة");
        Add("AdminOrgs.ColUsers", "Gebr.", "Users", "Użytk.", "Util.", "مستخدمون");
        Add("AdminOrgs.ColVacancies", "Vac.", "Jobs", "Oferty", "Joburi", "وظائف");
        Add("AdminOrgs.ColTokens", "Tokens", "Tok.", "Tokeny", "Tokenuri", "رموز");
        Add("AdminOrgs.ColStatus", "Status", "Status", "Status", "Status", "الحالة");
        Add("AdminOrgs.ColType", "Type", "Kind", "Typ", "Tip", "النوع");
        Add("AdminOrgs.ColDomain", "Domein", "Domain", "Domena", "Domeniu", "النطاق");
        Add("AdminOrgs.ColEnterpriseManager", "Enterprisemanager", "Enterprise manager", "Kierownik firmy", "Manager enterprise", "مدير المؤسسة");
        Add("AdminOrgs.ColPackage", "Pakket", "Package", "Pakiet", "Pachet", "الباقة");
        Add("AdminOrgs.ColBranches", "# Vestigingen", "# Branches", "# Oddziały", "# Filiale", "# فروع");
        Add("AdminOrgs.ColDisplayName", "Weergavenaam", "Display name", "Nazwa wyświetlana", "Nume afișat", "اسم العرض");
        Add("AdminOrgs.ColAddress", "Adres", "Address", "Adres firmy", "Adresă", "العنوان");
        Add("AdminOrgs.ColRequester", "Aanvrager", "Requester", "Wnioskodawca", "Solicitant", "مقدّم الطلب");
        Add("AdminOrgs.ColTarget", "Doelvestiging", "Target branch", "Docelowy oddział", "Filiala țintă", "الفرع المستهدف");
        Add("AdminOrgs.ColSince", "Sinds", "Since", "Od", "De la", "منذ");
        Add("AdminOrgs.ColAttempts", "Pogingen", "Attempts", "Próby", "Încercări", "محاولات");
        Add("AdminOrgs.ColLastAttempt", "Laatste poging", "Last attempt", "Ostatnia próba", "Ultima încercare", "آخر محاولة");
        Add("AdminOrgs.TypeEmployer", "Werkgever", "Employer", "Pracodawca", "Angajator", "صاحب عمل");
        Add("AdminOrgs.TypeIntermediary", "Intermediair", "Intermediary", "Pośrednik", "Intermediar", "وسيط");
        Add("AdminOrgs.StatusActive", "Actief", "Active", "Aktywny", "Activ", "نشط");
        Add("AdminOrgs.StatusKvkFailed", "KvK mislukt", "KvK failed", "KvK nieudane", "KvK eșuat", "فشل KvK");
        Add("AdminOrgs.StatusInactive", "Inactief", "Inactive", "Nieaktywny", "Inactiv", "غير نشط");
        Add("AdminOrgs.StatusInactiveDays", "Inactief {0} d", "Inactive {0} d", "Nieaktywny {0} d", "Inactiv {0} z", "غير نشط {0} ي");
        Add("AdminOrgs.StatusTakeover", "Overname", "Takeover", "Przejęcie", "Preluare", "استحواذ");
        Add("AdminOrgs.StatusNotLive", "Niet live", "Not live", "Nieaktywny", "Nu e live", "غير مباشر");
        Add("AdminOrgs.StatusOff", "Uit", "Off", "Wył.", "Oprit", "إيقاف");
        Add("AdminOrgs.BranchCount", "{0} vest.", "{0} br.", "{0} oddz.", "{0} fil.", "{0} فرع");
        Add("AdminOrgs.ToggleBranches", "Vestigingen tonen of verbergen", "Show or hide branches", "Pokaż lub ukryj oddziały", "Arată sau ascunde filialele", "إظهار أو إخفاء الفروع");
        Add("AdminOrgs.DetailPanel", "Organisatiedetail", "Organisation detail", "Szczegóły organizacji", "Detaliu organizație", "تفاصيل المنظمة");
        Add("AdminOrgs.CustomerSince", "klant sinds", "customer since", "klient od", "client din", "عميل منذ");
        Add("AdminOrgs.KvkVerified", "Geverifieerd", "Verified", "Zweryfikowano", "Verificat", "موثّق");
        Add("AdminOrgs.KvkFailedPill", "Mislukt", "Failed", "Nieudane", "Eșuat", "فشل");
        Add("AdminOrgs.Goodwill", "goodwill", "goodwill part", "część goodwill", "parte goodwill", "حسن نية");
        Add("AdminOrgs.TakeoverNoteTitle", "Overname aangevraagd", "Takeover requested", "Wniosek o przejęcie", "Preluare solicitată", "طُلب استحواذ");
        Add("AdminOrgs.TakeoverNoteBody", "{0} wil deze vestiging overnemen ({1}).", "{0} wants to take over this branch ({1}).", "{0} chce przejąć ten oddział ({1}).", "{0} vrea să preia această filială ({1}).", "{0} يريد الاستحواذ على هذا الفرع ({1}).");
        Add("AdminOrgs.ReviewTakeover", "Overname beoordelen", "Review takeover", "Oceń przejęcie", "Evaluează preluarea", "تقييم الاستحواذ");
        Add("AdminOrgs.GrantTokens", "Tokens geven", "Grant tokens", "Przyznaj tokeny", "Acordă tokenuri", "منح رموز");
        Add("AdminOrgs.UsersLink", "{0} gebruikers", "{0} users", "{0} użytkowników", "{0} utilizatori", "{0} مستخدمون");
        Add("AdminOrgs.KvkNumber", "KVK-nummer", "Chamber of Commerce number", "Numer KVK", "Număr KvK", "رقم غرفة التجارة");
        Add("AdminOrgs.KvkDemoHint", "Zonder live KVK-key werken demo-nummers: 12345678, 11223344, 55667788, 33445566, 44556677, 66778899, 77889900, 88990011, 99001122", "Without a live KVK key, demo numbers work: 12345678, 11223344, 55667788, …", "Bez klucza KVK działają numery demo.", "Fără cheie KvK funcționează numere demo.", "بدون مفتاح KvK تعمل أرقام تجريبية.");
        Add("AdminOrgs.LookupEstablishments", "Zoek vestigingen", "Look up branches", "Szukaj oddziałów", "Caută filiale", "ابحث عن فروع");
        Add("AdminOrgs.AlreadyRegistered", "Al geregistreerd", "Already registered", "Już zarejestrowany", "Deja înregistrat", "مسجّل مسبقاً");
        Add("AdminOrgs.AddEstablishment", "Toevoegen", "Add", "Dodaj", "Adaugă", "إضافة");
        Add("AdminOrgs.NoEstablishments", "Geen vestigingen gevonden in KVK.", "No branches found in the Chamber of Commerce registry.", "Brak oddziałów w KVK.", "Nicio filială în KvK.", "لا فروع في السجل.");
        Add("AdminOrgs.AddedAs", "'{0}' toegevoegd als {1}.", "'{0}' added as {1}.", "'{0}' dodano jako {1}.", "'{0}' adăugat ca {1}.", "أُضيف '{0}' كـ {1}.");
        Add("AdminOrgs.TabDomains", "Domeinen", "Domains", "Domeny", "Domenii", "النطاقات");
        Add("AdminOrgs.TabRegions", "Regio's", "Regions", "Regiony", "Regiuni", "المناطق");
        Add("AdminOrgs.TabKvk", "KvK-controle", "KvK check", "Kontrola KvK", "Verificare KvK", "فحص KvK");
        Add("AdminOrgs.TabTakeovers", "Overnames", "Takeovers", "Przejęcia", "Preluări", "استحواذات");
        Add("AdminOrgs.DomainsTabTitle", "CNAME / regio-hosts", "CNAME / region hosts", "CNAME / hosty regionów", "CNAME / hosturi regiune", "CNAME / مضيفو المناطق");
        Add("AdminOrgs.CnameHelpTitle", "Hoe richt ik een nieuw CNAME-subdomein in?", "How do I set up a new CNAME subdomain?", "Jak skonfigurować nową subdomenę CNAME?", "Cum configurez un subdomeniu CNAME?", "كيف أضبط نطاق فرعي CNAME؟");
        Add("AdminOrgs.CnameHelpAria", "Hulp: CNAME-subdomein inrichten", "Help: set up CNAME subdomain", "Pomoc: subdomena CNAME", "Ajutor: subdomeniu CNAME", "مساعدة: نطاق فرعي CNAME");
        Add("AdminOrgs.DomainsHint", "Koppel subdomeinen zoals westland.lobsy.nl aan regionale branding, kaartfocus en campagne-KPI's.", "Map subdomains such as westland.lobsy.nl to regional branding, map focus and campaign KPIs.", "Powiąż subdomeny z brandingiem regionalnym.", "Leagă subdomenii de branding regional.", "اربط النطاقات الفرعية بالعلامة الإقليمية.");
        Add("AdminOrgs.DomainsEmpty", "Nog geen CNAME-hosts.", "No CNAME hosts yet.", "Brak hostów CNAME.", "Niciun host CNAME.", "لا مضيفين CNAME بعد.");
        Add("AdminOrgs.NewDomain", "Nieuwe regio-host", "New region host", "Nowy host regionu", "Host regiune nou", "مضيف منطقة جديد");
        Add("AdminOrgs.EditDomain", "Regio-host bewerken", "Edit region host", "Edytuj host regionu", "Editează host regiune", "تعديل مضيف المنطقة");
        Add("AdminOrgs.DomainUpdated", "Regio-host bijgewerkt.", "Region host updated.", "Host regionu zaktualizowany.", "Host regiune actualizat.", "تم تحديث مضيف المنطقة.");
        Add("AdminOrgs.DomainAdded", "Regio-host toegevoegd.", "Region host added.", "Dodano host regionu.", "Host regiune adăugat.", "أُضيف مضيف المنطقة.");
        Add("AdminOrgs.DomainDeleted", "‘{0}’ verwijderd.", "‘{0}’ deleted.", "Usunięto ‘{0}’.", "‘{0}’ șters.", "حُذف ‘{0}’.");
        Add("AdminOrgs.Refresh", "Vernieuwen", "Refresh", "Odśwież", "Reîmprospătează", "تحديث");
        Add("AdminOrgs.RegionsManagedNote", "Beheerd door de organisatie", "Managed by the organisation", "Zarządzane przez organizację", "Gestionate de organizație", "تُدار من المنظمة");
        Add("AdminOrgs.RegionsEmpty", "Nog geen regio's.", "No regions yet.", "Brak regionów.", "Nicio regiune.", "لا مناطق بعد.");
        Add("AdminOrgs.TakeoversEmpty", "Geen openstaande overnames.", "No pending takeovers.", "Brak oczekujących przejęć.", "Nicio preluare în așteptare.", "لا استحواذات معلّقة.");
        Add("AdminOrgs.KvkEmpty", "Geen openstaande KvK-controles.", "No pending KvK checks.", "Brak kontroli KvK.", "Nicio verificare KvK.", "لا فحوصات KvK معلّقة.");
        Add("AdminOrgs.RetryKvk", "Opnieuw controleren", "Retry check", "Sprawdź ponownie", "Reîncearcă", "إعادة الفحص");
        Add("AdminOrgs.RetryDone", "KvK-controle opnieuw gestart.", "KvK check restarted.", "Ponowiono kontrolę KvK.", "Verificarea KvK a fost relansată.", "أُعيد تشغيل فحص KvK.");
        Add("AdminOrgs.Approve", "Goedkeuren", "Approve", "Zatwierdź", "Aprobă", "موافقة");
        Add("AdminOrgs.Reject", "Afwijzen", "Reject", "Odrzuć", "Respinge", "رفض");
        Add("AdminOrgs.ApproveDone", "Overname goedgekeurd.", "Takeover approved.", "Przejęcie zatwierdzone.", "Preluare aprobată.", "تمت الموافقة على الاستحواذ.");
        Add("AdminOrgs.RejectDone", "Overname afgewezen.", "Takeover rejected.", "Przejęcie odrzucone.", "Preluare respinsă.", "رُفض الاستحواذ.");
        Add("AdminOrgs.RejectTitle", "Overname afwijzen", "Reject takeover", "Odrzuć przejęcie", "Respinge preluarea", "رفض الاستحواذ");
        Add("AdminOrgs.RejectLead", "Geef een korte reden. De aanvrager wordt geïnformeerd.", "Provide a short reason. The requester will be informed.", "Podaj krótki powód.", "Oferă un motiv scurt.", "قدّم سبباً موجزاً.");
        Add("AdminOrgs.RejectReason", "Reden", "Reason", "Powód", "Motiv", "السبب");

        // Platform settings (05)
        Add("AdminSettings.FeaturesLead", "Zet onderdelen van het platform aan of uit.", "Turn platform parts on or off.", "Włącz lub wyłącz części platformy.", "Pornește sau oprește părți ale platformei.", "شغّل أو أوقف أجزاء المنصة.");
        Add("AdminSettings.PricingLead", "Prijzen, pakketten en commissies op één plek.", "Prices, packages and commissions in one place.", "Ceny, pakiety i prowizje w jednym miejscu.", "Prețuri, pachete și comisioane într-un loc.", "الأسعار والباقات والعمولات في مكان واحد.");
        Add("AdminSettings.Group.PlatformMode", "Platform-modus", "Platform mode", "Tryb platformy", "Mod platformă", "وضع المنصة");
        Add("AdminSettings.Group.PlatformMode.Desc", "Grote schakelaars die bepalen wie het platform kan gebruiken.", "Major switches that decide who can use the platform.", "Główne przełączniki kto może używać platformy.", "Comutatoare majore care decid cine poate folosi platforma.", "مفاتيح أساسية تحدد من يستخدم المنصة.");
        Add("AdminSettings.Group.Vacancies", "Vacatures", "Vacancies", "Oferty", "Joburi", "الوظائف");
        Add("AdminSettings.Group.Vacancies.Desc", "Controle en prijsacties rond vacatures.", "Controls and price actions around vacancies.", "Kontrola i akcje cenowe wokół ofert.", "Controale și acțiuni de preț pentru joburi.", "التحكم وعروض الأسعار حول الوظائف.");
        Add("AdminSettings.Group.Scholen", "Scholen", "Schools", "Szkoły", "Școli", "المدارس");
        Add("AdminSettings.Group.Scholen.Desc", "Lobsy voor scholen: portalen, resultaten per code en bewaartermijn.", "Lobsy for schools: portals, per-code results and retention.", "Lobsy dla szkół: portale, wyniki per kod i retencja.", "Lobsy pentru școli: portaluri, rezultate pe cod și retenție.", "Lobsy للمدارس: البوابات والنتائج لكل رمز والاحتفاظ.");
        Add("AdminSettings.Group.Sales", "Sales", "Sales", "Sprzedaż", "Sales", "المبيعات");
        Add("AdminSettings.Group.Sales.Desc", "Partnerprogramma’s: ambassadeurs en gerelateerde schakelaars.", "Partner programmes: ambassadors and related switches.", "Programy partnerskie: ambasadorzy i powiązane przełączniki.", "Programe partener: ambasadori și comutatoare aferente.", "برامج الشركاء: السفراء والمفاتيح ذات الصلة.");
        Add("AdminSettings.Group.Security", "Beveiliging", "Security", "Bezpieczeństwo", "Securitate", "الأمن");
        Add("AdminSettings.Group.Security.Desc", "Inloggen, sessies en toegang tot persoonsgegevens.", "Sign-in, sessions and access to personal data.", "Logowanie, sesje i dostęp do danych osobowych.", "Autentificare, sesiuni și acces la date personale.", "تسجيل الدخول والجلسات والوصول إلى البيانات الشخصية.");
        Add("AdminSettings.Group.Demo", "Demo & test", "Demo and test", "Demo i test", "Demo și test", "تجريبي واختبار");
        Add("AdminSettings.Group.Demo.Desc", "Hulpmiddelen die nooit in Productie aan mogen staan.", "Tools that must never be on in Production.", "Narzędzia, które nigdy nie mogą być włączone na produkcji.", "Instrumente care nu trebuie pornite niciodată în producție.", "أدوات يجب ألا تُفعَّل أبداً في الإنتاج.");
        Add("AdminSettings.Group.General", "Algemeen", "General", "Ogólne", "General", "عام");
        Add("AdminSettings.Group.General.Desc", "Publieke URL en herinneringen voor werkgevers.", "Public URL and employer reminders.", "Publiczny URL i przypomnienia dla pracodawców.", "URL public și memento-uri pentru angajatori.", "الرابط العام وتذكيرات أصحاب العمل.");
        Add("AdminSettings.NoPlatformSwitches", "Nog geen platformschakelaars. Ze verschijnen hier zodra ze live staan.", "No platform switches yet. They appear here once live.", "Brak przełączników platformy. Pojawią się, gdy będą na żywo.", "Niciun comutator de platformă încă. Apar aici când sunt live.", "لا مفاتيح منصة بعد. تظهر هنا عند الإطلاق.");
        Add("AdminSettings.AiModeration.Title", "AI-vacaturemoderatie", "AI vacancy moderation", "Moderacja AI ofert", "Moderare AI joburi", "إشراف AI على الوظائف");
        Add("AdminSettings.AiModeration.Desc", "Nieuwe en gewijzigde vacatureteksten worden automatisch gecontroleerd op discriminatie en misleiding.", "New and changed vacancy texts are checked automatically for discrimination and misleading content.", "Nowe i zmienione teksty ofert są sprawdzane pod kątem dyskryminacji i wprowadzania w błąd.", "Textele joburilor noi și modificate sunt verificate automat pentru discriminare și inducere în eroare.", "تُفحص نصوص الوظائف الجديدة والمعدّلة تلقائياً للتمييز والتضليل.");
        Add("AdminSettings.AiModeration.ImpactOff", "Vacatures worden zonder controle gepubliceerd.", "Vacancies are published without checks.", "Oferty publikowane bez kontroli.", "Joburile se publică fără verificare.", "تُنشر الوظائف دون فحص.");
        Add("AdminSettings.FreePublish.Title", "Gratis publiceren", "Free publishing", "Bezpłatna publikacja", "Publicare gratuită", "نشر مجاني");
        Add("AdminSettings.FreePublish.Desc", "Werkgevers publiceren zonder tokens tot en met de gekozen datum. Uitlichten en PushBom blijven betaald.", "Employers publish without tokens until the chosen date. Highlights and PushBom stay paid.", "Pracodawcy publikują bez tokenów do wybranej daty. Wyróżnienia i PushBom pozostają płatne.", "Angajatorii publică fără tokenuri până la data aleasă. Evidențierile și PushBom rămân plătite.", "ينشر أصحاب العمل دون رموز حتى التاريخ المختار. التمييز وPushBom يبقيان مدفوعين.");
        Add("AdminSettings.CandidateInsights.Enabled.Title", "Kandidaatinzichten beschikbaar", "Candidate insights available", "Dostępne wglądy w kandydatów", "Perspective candidați disponibile", "رؤى المرشحين متاحة");
        Add("AdminSettings.CandidateInsights.Enabled.Desc", "Werkgevers kunnen kandidaatinzichten ontgrendelen met tokens. Prijs staat bij Financiën — spend costs (InsightsUnlock).", "Employers can unlock candidate insights with tokens. Price is under Finance — spend costs (InsightsUnlock).", "Pracodawcy mogą odblokować wglądy tokenami. Cena w Finanse — koszty (InsightsUnlock).", "Angajatorii pot debloca perspectivele cu tokenuri. Prețul e la Finanțe — costuri (InsightsUnlock).", "يمكن لأصحاب العمل فتح رؤى المرشحين بالرموز. السعر تحت المالية — تكاليف الإنفاق.");
        Add("AdminSettings.CandidateInsights.Enabled.ImpactOff", "Werkgevers zien geen kandidaatinzichten meer.", "Employers no longer see candidate insights.", "Pracodawcy nie widzą już wglądów.", "Angajatorii nu mai văd perspectivele.", "لم يعد أصحاب العمل يرون رؤى المرشحين.");
        Add("AdminSettings.CandidateInsights.UnlockDays.Title", "Looptijd ontgrendeling", "Unlock duration", "Czas odblokowania", "Durata deblocării", "مدة فتح القفل");
        Add("AdminSettings.CandidateInsights.UnlockDays.Desc", "Hoe lang een betaalde ontgrendeling geldig blijft. Geldt alleen voor nieuwe ontgrendelingen.", "How long a paid unlock remains valid. Applies only to new unlocks.", "Jak długo ważne jest płatne odblokowanie. Tylko dla nowych.", "Cât timp rămâne valabilă o deblocare plătită. Doar pentru deblocări noi.", "مدة صلاحية الفتح المدفوع. ينطبق فقط على الفتحات الجديدة.");
        Add("AdminSettings.CandidateInsights.PerBranch.Title", "Ontgrendelen per vestiging", "Unlock per branch", "Odblokuj per oddział", "Deblochează pe filială", "فتح لكل فرع");
        Add("AdminSettings.CandidateInsights.PerBranch.Desc", "Per vestiging: vestigingsmanagers betalen uit toegewezen saldo. Organisatie-breed: alleen de bedrijfsmanager ontgrendelt.", "Per branch: branch managers pay from allocated balance. Company-wide: only the enterprise manager unlocks.", "Per oddział: menedżerowie płacą z przydzielonego salda. Cała firma: tylko menedżer przedsiębiorstwa.", "Pe filială: managerii plătesc din soldul alocat. La nivel de firmă: doar managerul enterprise.", "لكل فرع: يدفع مديرو الفروع من الرصيد المخصص. على مستوى الشركة: المدير المؤسسي فقط.");
        Add("AdminSettings.Schools.Enabled.Title", "Scholen-portalen actief", "School portals enabled", "Portale szkół włączone", "Portaluri școli activate", "بوابات المدارس مفعّلة");
        Add("AdminSettings.Schools.Enabled.Desc", "School-, leraar- en leerlingportalen. Bewaartermijn draait altijd, ook als dit uit staat.", "School, teacher and pupil portals. Retention always runs, even when this is off.", "Portale szkoły, nauczyciela i ucznia. Retencja działa zawsze.", "Portaluri școală, profesor și elev. Retenția rulează mereu.", "بوابات المدرسة والمعلم والتلميذ. الاحتفاظ يعمل دائمًا.");
        Add("AdminSettings.Schools.Enabled.ImpactOff", "School- en leerlingportalen zijn niet bereikbaar.", "School and pupil portals are unreachable.", "Portale szkoły i ucznia niedostępne.", "Portalurile școală/elev sunt inaccesibile.", "بوابات المدرسة والتلميذ غير متاحة.");
        Add("AdminSettings.Schools.PerCode.Title", "Resultaten per code voor schoolbeheer", "Per-code results for school admins", "Wyniki per kod dla adminów szkoły", "Rezultate pe cod pentru admini școală", "نتائج لكل رمز لمسؤولي المدرسة");
        Add("AdminSettings.Schools.PerCode.Desc", "Als aan: schoolbeheerders zien korte uitkomsten per code. Uit: alleen groepsresultaten (k≥5).", "When on: school admins see short per-code outcomes. Off: group results only (k≥5).", "Wł: krótkie wyniki per kod. Wył: tylko grupy (k≥5).", "Pornit: rezultate scurte pe cod. Oprit: doar grupuri (k≥5).", "عند التشغيل: نتائج قصيرة لكل رمز. عند الإيقاف: نتائج جماعية فقط.");
        Add("AdminSettings.Schools.RetentionMonth.Title", "Bewaartermijn-maand", "Retention cutoff month", "Miesiąc retencji", "Luna retenției", "شهر قطع الاحتفاظ");
        Add("AdminSettings.Schools.RetentionMonth.Desc", "Maand (1–12) van de jaarlijkse cut-off. Standaard juli (7).", "Month (1–12) of the yearly cut-off. Default July (7).", "Miesiąc (1–12) cut-off. Domyślnie lipiec (7).", "Luna (1–12) cut-off. Implicit iulie (7).", "شهر (1–12) للقطع السنوي. الافتراضي يوليو (7).");
        Add("AdminSettings.Schools.RetentionDay.Title", "Bewaartermijn-dag", "Retention cutoff day", "Dzień retencji", "Ziua retenției", "يوم قطع الاحتفاظ");
        Add("AdminSettings.Schools.RetentionDay.Desc", "Dag van de maand voor de cut-off. Standaard 31.", "Day of month for the cut-off. Default 31.", "Dzień miesiąca cut-off. Domyślnie 31.", "Ziua din lună pentru cut-off. Implicit 31.", "يوم الشهر للقطع. الافتراضي 31.");
        Add("AdminSettings.Ambassadors.Enabled.Title", "Ambassadeursprogramma", "Ambassadors programme", "Program ambasadorów", "Programul de ambasadori", "برنامج السفراء");
        Add("AdminSettings.Ambassadors.Enabled.Desc", "Wanneer uit (standaard): geen Ambassadeur-pagina’s, links of nieuwe commissie. Bestaande data blijft bewaard.", "When off (default): no Ambassadeur pages, links or new commission. Existing data is kept.", "Wył (domyślnie): brak stron/linków/nowych prowizji Ambassadeur. Dane zachowane.", "Oprit (implicit): fără pagini/linkuri/comisioane Ambassadeur noi. Datele rămân.", "عند الإيقاف (الافتراضي): لا صفحات/روابط/عمولات Ambassadeur جديدة. تبقى البيانات.");
        Add("AdminSettings.Ambassadors.Enabled.ImpactOff", "Ambassadeur-portalen en nieuwe commissie stoppen; bestaande data blijft.", "Ambassadeur portals and new commission stop; existing data remains.", "Portale Ambassadeur i nowe prowizje stop; dane zostają.", "Portalurile Ambassadeur și comisioanele noi se opresc; datele rămân.", "تتوقف بوابات Ambassadeur والعمولات الجديدة؛ تبقى البيانات.");
        Add("AdminSettings.StopAction", "Actie stoppen", "Stop action", "Zatrzymaj akcję", "Oprește acțiunea", "إيقاف الإجراء");
        Add("AdminSettings.Mfa.Title", "Tweestapsverificatie", "Two-factor authentication", "Uwierzytelnianie dwuskładnikowe", "Autentificare în doi pași", "التحقق بخطوتين");
        Add("AdminSettings.Mfa.Desc", "Verplicht voor beheerders, vestigings-, regio- en enterprisemanagers en intermediairs bij inloggen met wachtwoord. Inloggen via Microsoft of Google gebruikt de 2FA van die dienst.", "Required for admins, branch, region and enterprise managers and intermediaries on password login. Microsoft or Google sign-in uses that service's 2FA.", "Wymagane dla adminów, menedżerów oddziałów/regionów/enterprise i pośredników przy logowaniu hasłem.", "Obligatoriu pentru admini, manageri de filială/regiune/enterprise și intermediari la login cu parolă.", "إلزامي للمشرفين ومديري الفروع والمناطق والمؤسسات والوسطاء عند تسجيل الدخول بكلمة مرور.");
        Add("AdminSettings.Mfa.Meta", "Vastgelegd in beleid (ADR 0005), niet uit te zetten.", "Fixed in policy (ADR 0005), cannot be turned off.", "Ustalony w polityce (ADR 0005), nie można wyłączyć.", "Stabilită în politică (ADR 0005), nu se poate dezactiva.", "مثبّت في السياسة (ADR 0005)، لا يمكن إيقافه.");
        Add("AdminSettings.SessionTimeout.Title", "Sessie-timeout bij inactiviteit", "Session timeout on inactivity", "Limit czasu sesji przy bezczynności", "Timeout sesiune la inactivitate", "مهلة الجلسة عند الخمول");
        Add("AdminSettings.SessionTimeout.Desc", "Gebruikers worden na deze periode zonder activiteit uitgelogd.", "Users are signed out after this period without activity.", "Użytkownicy są wylogowywani po tym okresie bez aktywności.", "Utilizatorii sunt deconectați după această perioadă fără activitate.", "يُسجَّل خروج المستخدمين بعد هذه الفترة دون نشاط.");
        Add("AdminSettings.SupportNotifyAdmins.Title", "Beheerders melden bij support-toegang", "Notify admins on support access", "Powiadom adminów przy dostępie support", "Notifică adminii la acces support", "إبلاغ المشرفين عند وصول الدعم");
        Add("AdminSettings.SupportNotifyAdmins.Desc", "Alle beheerders krijgen een e-mail zodra iemand tijdelijke toegang tot persoonsgegevens krijgt.", "All admins get an e-mail when someone gets temporary access to personal data.", "Wszyscy admini dostają e-mail, gdy ktoś otrzyma tymczasowy dostęp do danych osobowych.", "Toți adminii primesc e-mail când cineva primește acces temporar la date personale.", "يحصل كل المشرفين على بريد عند منح وصول مؤقت للبيانات الشخصية.");
        Add("AdminSettings.SupportNotifySubject.Title", "Gebruiker informeren bij support-toegang", "Inform user on support access", "Poinformuj użytkownika o dostępie support", "Informează utilizatorul la acces support", "إبلاغ المستخدم عند وصول الدعم");
        Add("AdminSettings.SupportNotifySubject.Desc", "De gebruiker ziet op zijn privacypagina dat support zijn gegevens heeft ingezien.", "The user sees on their privacy page that support viewed their data.", "Użytkownik widzi na stronie prywatności, że support wglądał w jego dane.", "Utilizatorul vede pe pagina de confidențialitate că support a consultat datele.", "يرى المستخدم في صفحة الخصوصية أن الدعم اطّلع على بياناته.");
        Add("AdminSettings.ActivationLinks.Title", "Activatielinks tonen bij registratie", "Show activation links on registration", "Pokaż linki aktywacji przy rejestracji", "Arată linkuri de activare la înregistrare", "إظهار روابط التفعيل عند التسجيل");
        Add("AdminSettings.ActivationLinks.Desc", "Voor demo's: toont de activatielink direct in plaats van via e-mail.", "For demos: shows the activation link directly instead of by e-mail.", "Do demo: pokazuje link aktywacyjny od razu zamiast e-mailem.", "Pentru demo: arată linkul de activare direct în loc de e-mail.", "للعروض: يُظهر رابط التفعيل مباشرة بدل البريد.");
        Add("AdminSettings.AuthenticatorStub.Title", "Authenticator bij sollicitatie (stub)", "Authenticator on application (stub)", "Authenticator przy aplikacji (stub)", "Authenticator la aplicare (stub)", "المصادقة عند التقديم (تجريبي)");
        Add("AdminSettings.AuthenticatorStub.Desc", "Laat kandidaten bij solliciteren de authenticator-stub gebruiken. Staat los van de 2FA bij inloggen.", "Lets candidates use the authenticator stub when applying. Separate from login 2FA.", "Pozwala kandydatom użyć stubu authenticatora przy aplikacji. Osobno od 2FA logowania.", "Permite candidaților stub-ul de authenticator la aplicare. Separat de 2FA la login.", "يتيح للمرشحين استخدام مصادقة تجريبية عند التقديم. منفصلة عن 2FA عند الدخول.");
        Add("AdminSettings.Badge.AcceptatieOnly", "Alleen in Acceptatie", "Acceptatie only", "Tylko Acceptatie", "Doar Acceptatie", "فقط في Acceptatie");
        Add("AdminSettings.PublicUrl.Title", "Publieke URL", "Public URL", "Publiczny URL", "URL public", "الرابط العام");
        Add("AdminSettings.PublicUrl.Desc", "Basis-URL van de publieke webapp (links in e-mails).", "Base URL of the public web app (links in e-mails).", "Bazowy URL publicznej aplikacji (linki w e-mailach).", "URL de bază al aplicației publice (linkuri în e-mail).", "عنوان أساس تطبيق الويب العام (روابط البريد).");
        Add("AdminSettings.InactiveDays.Title", "Werkgevers opnieuw benaderen", "Re-engage employers", "Ponowne podejście do pracodawców", "Reangajare angajatori", "إعادة التواصل مع أصحاب العمل");
        Add("AdminSettings.InactiveDays.Desc", "Na hoeveel dagen zonder activiteit krijgt een werkgever één herinneringsmail?", "After how many days without activity does an employer get one reminder e-mail?", "Po ilu dniach bez aktywności pracodawca dostaje jeden e-mail przypominający?", "După câte zile fără activitate primește un angajator un e-mail de reamintire?", "بعد كم يوماً دون نشاط يحصل صاحب العمل على تذكير واحد؟");
        Add("AdminSettings.Unit.Min", "min", "mins", "minut", "minute", "د");
        Add("AdminSettings.Unit.Days", "dagen", "days", "dni", "zile", "أيام");
        Add("AdminSettings.DirtyMeta", "Gewijzigd, nog niet opgeslagen · was: {0}", "Changed, not saved yet · was: {0}", "Zmieniono, jeszcze nie zapisano · było: {0}", "Modificat, încă nesalvat · era: {0}", "تم التغيير ولم يُحفظ بعد · كان: {0}");
        Add("AdminSettings.SaveSummary", "{0} wijziging(en): {1}", "{0} change(s): {1}", "{0} zmian(y): {1}", "{0} modificare(ări): {1}", "{0} تغيير(ات): {1}");
        Add("AdminSettings.LastSaved", "Laatst opgeslagen {0}", "Last saved {0}", "Ostatnio zapisano {0}", "Salvat ultima dată {0}", "آخر حفظ {0}");
        Add("AdminSettings.Saved", "Instellingen opgeslagen.", "Settings saved.", "Ustawienia zapisane.", "Setări salvate.", "تم حفظ الإعدادات.");
        Add("AdminSettings.LobsyCompany.Title", "Lobsy bedrijfsgegevens", "Lobsy company details", "Dane firmy Lobsy", "Datele firmei Lobsy", "بيانات شركة Lobsy");
        Add("AdminSettings.LobsyCompany.Hint", "Deze gegevens verschijnen onderaan factuur-PDF’s. De slogan staat ook in de header onder het Lobsy-logo. Het Knab BTW-IBAN wordt gebruikt voor de geautomatiseerde BTW-buffer.", "These details appear at the bottom of invoice PDFs. The slogan also shows in the header under the Lobsy logo. The Knab VAT IBAN is used for the automated VAT buffer.", "Te dane pojawiają się na dole PDF-ów faktur.", "Aceste date apar jos pe PDF-urile de factură.", "تظهر هذه البيانات أسفل ملفات فواتير PDF.");
        Add("AdminSettings.LobsyCompany.Name", "Bedrijfsnaam", "Company name", "Nazwa firmy", "Numele firmei", "اسم الشركة");
        Add("AdminSettings.LobsyCompany.Slogan", "Slogan", "Tagline", "Hasło reklamowe", "Motto", "الشعار");
        Add("AdminSettings.LobsyCompany.SloganHint", "Zichtbaar in de header onder het Lobsy-logo. Leeg laten herstelt de standaardslogan.", "Visible in the header under the Lobsy logo. Leave empty to restore the default slogan.", "Widoczny w nagłówku pod logo Lobsy.", "Vizibil în antet sub logo-ul Lobsy.", "ظاهر في الترويسة تحت شعار Lobsy.");
        Add("AdminSettings.LobsyCompany.Address", "Adres", "Address", "Adres firmy", "Adresă", "العنوان");
        Add("AdminSettings.LobsyCompany.PostalCode", "Postcode", "Postal code", "Kod pocztowy", "Cod poștal", "الرمز البريدي");
        Add("AdminSettings.LobsyCompany.City", "Plaats", "City", "Miasto", "Oraș", "المدينة");
        Add("AdminSettings.LobsyCompany.Country", "Land", "Country", "Kraj", "Țară", "البلد");
        Add("AdminSettings.LobsyCompany.Kvk", "KvK-nummer", "Chamber of Commerce number", "Numer KRS/KVK", "Număr KvK", "رقم الغرفة التجارية");
        Add("AdminSettings.LobsyCompany.Vat", "BTW-nummer", "VAT number", "Numer VAT", "Număr TVA", "رقم ضريبة القيمة المضافة");
        Add("AdminSettings.LobsyCompany.Iban", "Knab BTW-rekening (IBAN)", "Knab VAT account (IBAN)", "Konto VAT Knab (IBAN)", "Cont TVA Knab (IBAN)", "حساب ضريبة Knab (IBAN)");
        Add("AdminSettings.LobsyCompany.IbanHint", "GET toont alleen een gemaskeerd IBAN. Laat het veld ongewijzigd om de huidige rekening te behouden; vul een volledig IBAN in om te wijzigen.", "GET only shows a masked IBAN. Leave unchanged to keep the current account; enter a full IBAN to change it.", "GET pokazuje tylko zamaskowany IBAN.", "GET arată doar un IBAN mascat.", "يعرض GET رقم IBAN مقنّعاً فقط.");
        Add("AdminSettings.LobsyCompany.IbanVatHint", "Na elke succesvolle Mollie token-aankoop wordt het BTW-bedrag (21%) als overboeking-opdracht gelogd naar dit IBAN, met het unieke factuurnummer als omschrijving/kenmerk.", "After each successful Mollie token purchase, the VAT amount (21%) is logged as a transfer to this IBAN, with the unique invoice number as reference.", "Po każdym udanym zakupie tokenów Mollie kwota VAT (21%) jest logowana jako przelew na ten IBAN.", "După fiecare achiziție reușită de tokenuri Mollie, TVA (21%) este înregistrat ca transfer pe acest IBAN.", "بعد كل شراء رموز Mollie ناجح يُسجَّل مبلغ الضريبة (21٪) كتحويل إلى هذا IBAN.");
        Add("AdminSettings.LobsyCompany.Phone", "Telefoon", "Phone", "Telefon", "Telefon", "الهاتف");
        Add("AdminSettings.LobsyCompany.Email", "E-mail", "Email", "Adres e-mail", "Adresă de e-mail", "البريد");
        Add("AdminSettings.LobsyCompany.Saved", "Bedrijfsgegevens opgeslagen.", "Company details saved.", "Dane firmy zapisane.", "Datele firmei salvate.", "تم حفظ بيانات الشركة.");
        Add("AdminTabs.TokenPrices", "Tokenprijzen", "Token prices", "Ceny tokenów", "Prețuri token", "أسعار الرموز");
        Add("AdminTabs.PushBom", "PushBom", "PushBom pricing", "Ceny PushBom", "Prețuri PushBom", "تسعير PushBom");
        Add("AdminTabs.FlexTalent", "Flex & talent", "Flex and talent", "Flex i talent", "Flex și talent", "مرن ومواهب");
        Add("AdminTabs.EarlyAdapters", "Early adapters", "Early-adopter rules", "Reguły early adapter", "Reguli early adapter", "المبادرون الأوائل");
        Add("AdminTabs.SalesCommission", "Sales & commissie", "Sales & commission", "Sprzedaż i prowizja", "Sales și comision", "المبيعات والعمولة");

        // 06 · Financiën
        Add("AdminFinance.RevenueLead", "Betalingen via Mollie, facturen en tokenaankopen. Prijzen staan bij Prijzen & pakketten.", "Payments via Mollie, invoices and token purchases. Prices live under Prices & packages.", "Płatności Mollie, faktury i zakupy tokenów. Ceny są w Ceny i pakiety.", "Plăți via Mollie, facturi și achiziții de tokenuri. Prețurile sunt la Prețuri și pachete.", "مدفوعات Mollie والفواتير وشراء الرموز. الأسعار في الأسعار والباقات.");
        Add("AdminFinance.GoodwillLead", "Tokens toekennen en goodwillhistorie op één plek.", "Grant tokens and goodwill history in one place.", "Przyznawanie tokenów i historia goodwill w jednym miejscu.", "Acordă tokenuri și istoricul goodwill într-un singur loc.", "منح الرموز وسجل حسن النية في مكان واحد.");
        Add("AdminFinance.PayoutsLead", "Self-billing uitbetalingen, inkoop en btw-aangifte.", "Self-billing payouts, purchase costs and VAT filing.", "Wypłaty self-billing, koszty zakupu i deklaracja VAT.", "Plăți self-billing, costuri de achiziție și declarație TVA.", "مدفوعات الفوترة الذاتية وتكاليف الشراء وإقرار الضريبة.");
        Add("AdminFinance.Period", "Periode", "Period", "Okres", "Perioadă", "الفترة");
        Add("AdminFinance.ExportAccounting", "Export voor boekhouding", "Export for accounting", "Eksport do księgowości", "Export pentru contabilitate", "تصدير للمحاسبة");
        Add("AdminFinance.ExportCsv", "Export CSV", "Download CSV", "Pobierz CSV", "Descarcă CSV", "تصدير CSV");
        Add("AdminFinance.Refresh", "Vernieuwen", "Refresh", "Odśwież", "Reîmprospătează", "تحديث");
        Add("AdminFinance.Year", "Jaar", "Year", "Rok", "An", "السنة");
        Add("AdminFinance.Quarter", "Kwartaal", "Quarter", "Kwartał", "Trimestru", "الربع");
        Add("AdminFinance.WholeYear", "Heel jaar", "Whole year", "Cały rok", "Tot anul", "السنة كاملة");
        Add("AdminFinance.OrgFilter", "Organisatie", "Organisation", "Organizacja", "Organizație", "المنظمة");
        Add("AdminFinance.Rows", "regels", "rows", "wierszy", "rânduri", "صفوف");
        Add("AdminFinance.Tab.Transactions", "Transacties", "Transactions", "Transakcje", "Tranzacții", "المعاملات");
        Add("AdminFinance.Tab.Tokenlog", "Tokenlog", "Token log", "Dziennik tokenów", "Jurnal tokenuri", "سجل الرموز");
        Add("AdminFinance.Tab.Kpis", "KPI’s", "KPIs", "KPI", "KPI", "مؤشرات");
        Add("AdminFinance.Tab.Payouts", "Uitbetalingen", "Payouts", "Wypłaty", "Plăți", "المدفوعات");
        Add("AdminFinance.Tab.SmCosts", "Inkoop / salesmanagers", "Purchase / sales managers", "Zakup / sales managerowie", "Achiziții / sales manageri", "مشتريات / مديرو المبيعات");
        Add("AdminFinance.Tab.VatBuffer", "Btw-buffer", "VAT buffer", "Bufor VAT", "Buffer TVA", "احتياطي الضريبة");
        Add("AdminFinance.Tab.VatFiling", "Btw-aangifte & facturen", "VAT filing & invoices", "Deklaracja VAT i faktury", "Declarație TVA și facturi", "إقرار الضريبة والفواتير");
        Add("AdminFinance.Kpi.Revenue", "Omzet (periode, incl. btw)", "Revenue (period, incl. VAT)", "Przychód (okres, z VAT)", "Venit (perioadă, cu TVA)", "الإيراد (الفترة، شامل الضريبة)");
        Add("AdminFinance.Kpi.TokensSold", "Tokens verkocht", "Tokens sold", "Sprzedane tokeny", "Tokenuri vândute", "الرموز المباعة");
        Add("AdminFinance.Kpi.OpenMollie", "Open bij Mollie", "Open at Mollie", "Otwarte w Mollie", "Deschise la Mollie", "مفتوح لدى Mollie");
        Add("AdminFinance.Kpi.VatBuffer", "Btw-buffer", "VAT buffer", "Bufor VAT", "Buffer TVA", "احتياطي الضريبة");
        Add("AdminFinance.Kpi.OpenPayouts", "Open facturen salesmanagers", "Open sales-manager invoices", "Otwarte faktury sales managerów", "Facturi deschise sales manager", "فواتير مديري المبيعات المفتوحة");
        Add("AdminFinance.OldestDays", "oudste {0} dagen", "oldest {0} days", "najstarsze {0} dni", "cele mai vechi {0} zile", "أقدم {0} أيام");
        Add("AdminFinance.VatDueShort", "Q{0} aangifte {1}", "Q{0} filing {1}", "Q{0} deklaracja {1}", "Q{0} declarație {1}", "إقرار Q{0} {1}");
        Add("AdminFinance.OpenCount", "{0} open", "{0} outstanding", "{0} otwarte", "{0} deschise", "{0} مفتوح");
        Add("AdminFinance.OpenPayoutsCard", "Open facturen salesmanagers", "Open sales-manager invoices", "Otwarte faktury sales managerów", "Facturi deschise sales manager", "فواتير مديري المبيعات المفتوحة");
        Add("AdminFinance.ViewPayouts", "Bekijken", "View", "Zobacz", "Vezi", "عرض");
        Add("AdminFinance.Total", "Totaal", "Total", "Suma", "Total", "الإجمالي");
        Add("AdminFinance.VatCardTitle", "Btw Q{0} {1}", "VAT Q{0} {1}", "VAT Q{0} {1}", "TVA Q{0} {1}", "الضريبة Q{0} {1}");
        Add("AdminFinance.VatFiling", "Aangifte", "Filing", "Deklaracja", "Declarație", "الإقرار");
        Add("AdminFinance.VatOmzetEx", "Omzet excl. btw", "Revenue excl. VAT", "Przychód bez VAT", "Venit fără TVA", "إيراد بدون ضريبة");
        Add("AdminFinance.VatDue", "Af te dragen btw (21%)", "VAT due (21%)", "VAT do zapłaty (21%)", "TVA de plată (21%)", "الضريبة المستحقة (21٪)");
        Add("AdminFinance.VatBuffer", "Gereserveerd in buffer", "Reserved in buffer", "Zarezerwowane w buforze", "Rezervat în buffer", "محجوز في الاحتياطي");
        Add("AdminFinance.NoTransactions", "Geen transacties in deze periode.", "No transactions in this period.", "Brak transakcji w tym okresie.", "Nicio tranzacție în această perioadă.", "لا معاملات في هذه الفترة.");
        Add("AdminFinance.NoTokenlog", "Geen tokenlog-regels.", "No token-log rows.", "Brak wierszy dziennika tokenów.", "Niciun rând în jurnalul de tokenuri.", "لا صفوف في سجل الرموز.");
        Add("AdminFinance.NoOpenPayouts", "Geen openstaande facturen.", "No open invoices.", "Brak otwartych faktur.", "Nicio factură deschisă.", "لا فواتير مفتوحة.");
        Add("AdminFinance.NoGoodwill", "Geen goodwill-tokens in deze periode.", "No goodwill tokens in this period.", "Brak tokenów goodwill w tym okresie.", "Niciun token goodwill în această perioadă.", "لا رموز حسن نية في هذه الفترة.");
        Add("AdminFinance.NoCompanies", "Geen bedrijven gevonden.", "No companies found.", "Nie znaleziono firm.", "Nicio firmă găsită.", "لم يتم العثور على شركات.");
        Add("AdminFinance.NoSmCosts", "Geen salesmanager-uitbetalingen in deze periode.", "No sales-manager payouts in this period.", "Brak wypłat sales managerów w tym okresie.", "Nicio plată sales manager în această perioadă.", "لا مدفوعات مدير مبيعات في هذه الفترة.");
        Add("AdminFinance.NoVatTransfers", "Geen BTW-buffer overboekingen in deze periode.", "No VAT-buffer transfers in this period.", "Brak przelewów bufora VAT w tym okresie.", "Niciun transfer buffer TVA în această perioadă.", "لا تحويلات لاحتياطي الضريبة في هذه الفترة.");
        Add("AdminFinance.NoDeclarations", "Nog geen gegenereerde BTW-aangiftes.", "No VAT filings generated yet.", "Brak wygenerowanych deklaracji VAT.", "Nicio declarație TVA generată încă.", "لم يُنشأ أي إقرار ضريبة بعد.");
        Add("AdminFinance.Col.Date", "Datum", "Date", "Data", "Dată", "التاريخ");
        Add("AdminFinance.Col.Invoice", "Factuur", "Invoice", "Faktura", "Factură", "الفاتورة");
        Add("AdminFinance.Col.Org", "Organisatie", "Organisation", "Organizacja", "Organizație", "المنظمة");
        Add("AdminFinance.Col.Product", "Product", "Product line", "Produkt", "Produs", "المنتج");
        Add("AdminFinance.Col.InclVat", "Incl. btw", "Incl. VAT", "Z VAT", "Cu TVA", "شامل الضريبة");
        Add("AdminFinance.Col.Tokens", "Tokens", "Token amount", "Tokeny", "Tokenuri", "الرموز");
        Add("AdminFinance.Col.Value", "Waarde", "Value", "Wartość", "Valoare", "القيمة");
        Add("AdminFinance.Col.Reason", "Reden", "Reason", "Powód", "Motiv", "السبب");
        Add("AdminFinance.Col.IssuedBy", "Uitgegeven door", "Issued by", "Wystawione przez", "Emis de", "صادر عن");
        Add("AdminFinance.Col.Kind", "Soort", "Kind", "Rodzaj", "Tip", "النوع");
        Add("AdminFinance.Col.Payee", "Begunstigde", "Payee", "Odbiorca", "Beneficiar", "المستفيد");
        Add("AdminFinance.Col.Role", "Rol", "Role", "Rola", "Rolul", "الدور");
        Add("AdminFinance.Col.Amount", "Bedrag", "Amount", "Kwota", "Sumă", "المبلغ");
        Add("AdminFinance.Col.Status", "Status", "Payment status", "Status płatności", "Status plată", "الحالة");
        Add("AdminFinance.Col.Paid", "Betaald", "Paid", "Opłacono", "Plătit", "مدفوع");
        Add("AdminFinance.InvoicePdf", "Factuur (pdf)", "Invoice (pdf)", "Faktura (pdf)", "Factură (pdf)", "فاتورة (pdf)");
        Add("AdminFinance.GrantTitle", "Tokens toekennen", "Grant tokens", "Przyznaj tokeny", "Acordă tokenuri", "منح الرموز");
        Add("AdminFinance.GrantLead", "Zoek een organisatie en ken goodwill-tokens toe. Waarde € 0,00 — geen btw of omzet.", "Find an organisation and grant goodwill tokens. Value € 0.00 — no VAT or revenue.", "Znajdź organizację i przyznaj tokeny goodwill. Wartość 0,00 € — bez VAT i przychodu.", "Găsește o organizație și acordă tokenuri goodwill. Valoare 0,00 € — fără TVA sau venit.", "ابحث عن منظمة وامنح رموز حسن النية. القيمة 0,00 € — بلا ضريبة أو إيراد.");
        Add("AdminFinance.GrantAction", "Tokens toekennen", "Grant tokens", "Przyznaj tokeny", "Acordă tokenuri", "منح الرموز");
        Add("AdminFinance.GrantSuccess", "Tokens toegekend. Zie tokenlog voor de reden.", "Tokens granted. See the token log for the reason.", "Tokeny przyznane. Powód w dzienniku tokenów.", "Tokenuri acordate. Vezi jurnalul pentru motiv.", "تم منح الرموز. انظر سجل الرموز للسبب.");
        Add("AdminFinance.HistoryTitle", "Goodwillhistorie", "Goodwill history", "Historia goodwill", "Istoric goodwill", "سجل حسن النية");
        Add("AdminFinance.HistoryLead", "Administratieve waarde € 0,00 — geen BTW-verplichting of omzet. Token-saldo van de ondernemer loopt wel op.", "Administrative value € 0.00 — no VAT obligation or revenue. The employer token balance still increases.", "Wartość administracyjna 0,00 € — bez VAT i przychodu. Saldo tokenów przedsiębiorcy rośnie.", "Valoare administrativă 0,00 € — fără obligație TVA sau venit. Soldul de tokenuri crește totuși.", "قيمة إدارية 0,00 € — بلا التزام ضريبي أو إيراد. رصيد رموز صاحب العمل يرتفع.");
        Add("AdminFinance.SalesCrossLink", "Salesmanagers en ambassadeurs beheer je bij", "Manage sales managers and ambassadors under", "Sales managerów i ambasadorów zarządzasz w", "Sales managerii și ambasadorii se gestionează la", "تُدار مدراء المبيعات والسفراء في");
        Add("AdminFinance.MarkPaid", "Markeer als betaald", "Mark as paid", "Oznacz jako opłacone", "Marchează ca plătit", "علّم كمدفوع");
        Add("AdminFinance.BulkMarkPaid", "Markeer {0} als betaald", "Mark {0} as paid", "Oznacz {0} jako opłacone", "Marchează {0} ca plătite", "علّم {0} كمدفوعة");
        Add("AdminFinance.BulkMarkPaidTitle", "Bulk markeren als betaald", "Bulk mark as paid", "Masowe oznaczenie jako opłacone", "Marcare în masă ca plătite", "تعليم جماعي كمدفوع");
        Add("AdminFinance.BulkMarkPaidLead", "Je markeert {0} openstaande self-billing facturen als betaald. Dit gebruikt het bestaande mark-paid endpoint per factuur.", "You will mark {0} open self-billing invoices as paid. This calls the existing mark-paid endpoint per invoice.", "Oznaczysz {0} otwarte faktury self-billing jako opłacone.", "Vei marca {0} facturi self-billing deschise ca plătite.", "ستعلّم {0} فواتير مفتوحة كمدفوعة.");
        Add("AdminFinance.BulkMarkPaidOk", "{0} facturen gemarkeerd als betaald.", "{0} invoices marked as paid.", "{0} faktur oznaczonych jako opłacone.", "{0} facturi marcate ca plătite.", "عُلّمت {0} فواتير كمدفوعة.");
        Add("AdminFinance.BulkMarkPaidPartial", "{0} gelukt, {1} mislukt.", "{0} succeeded, {1} failed.", "{0} OK, {1} nieudane.", "{0} reușite, {1} eșuate.", "{0} نجحت، {1} فشلت.");
        Add("AdminFinance.PayoutCheckouts", "Payout-checkouts", "Payout checkouts", "Checkouty wypłat", "Checkout-uri de plată", "عمليات صرف الدفع");
        Add("AdminFinance.PayoutCheckoutsLead", "Self-service Mollie-payouts van salesmanagers/ambassadeurs (Pending → Paid → Completed).", "Self-service Mollie payouts from sales managers/ambassadors (Pending → Paid → Completed).", "Wypłaty Mollie self-service sales managerów/ambasadorów.", "Plăți Mollie self-service ale sales managerilor/ambasadorilor.", "مدفوعات Mollie ذاتية الخدمة لمديري المبيعات/السفراء.");
        Add("AdminFinance.SmCostsTitle", "Inkoop / kostenposten — salesmanager-uitbetalingen", "Purchase costs — sales-manager payouts", "Koszty zakupu — wypłaty sales managerów", "Costuri de achiziție — plăți sales manager", "تكاليف الشراء — مدفوعات مدير المبيعات");
        Add("AdminFinance.SmCostsLead", "Alleen betaalde self-billing facturen (PaidAt), gelijk aan Rubriek 5. Ex-BTW + BTW (standaard 21% inkoop-btw).", "Paid self-billing invoices only (PaidAt), same as box 5. Ex-VAT + VAT (default 21% purchase VAT).", "Tylko opłacone faktury self-billing (PaidAt), jak Rubryka 5.", "Doar facturi self-billing plătite (PaidAt), ca Rubrica 5.", "فواتير الفوترة الذاتية المدفوعة فقط (PaidAt)، مثل البند 5.");
        Add("AdminFinance.VatWizard", "BTW aangifte versturen / genereren", "Send / generate VAT filing", "Wyślij / wygeneruj deklarację VAT", "Trimite / generează declarația TVA", "إرسال / إنشاء إقرار الضريبة");
        Add("AdminFinance.VatWizardLead", "Selecteer een openstaand kwartaal. Na bevestiging worden omzet- en inkoopregels verwerkt en een PDF opgeslagen.", "Select an open quarter. After confirm, revenue and purchase rows are processed and a PDF is stored.", "Wybierz otwarty kwartał. Po potwierdzeniu przetwarzane są przychody i zakupy oraz zapisywany jest PDF.", "Selectează un trimestru deschis. După confirmare se procesează veniturile și achizițiile și se salvează un PDF.", "اختر ربعًا مفتوحًا. بعد التأكيد تُعالَج بنود الإيراد والمشتريات ويُحفظ PDF.");
        Add("AdminFinance.WhatDrivesTitle", "Wat bepaalt welke prijs?", "What drives which price?", "Co determinuje którą cenę?", "Ce determină care preț?", "ما الذي يحدد أي سعر؟");
        Add("AdminFinance.WhatDrivesLead", "Elke prijsentiteit stuurt iets anders aan. Waar twee entiteiten overlappen, zie de waarschuwingen hieronder — geen datamerge in deze release.", "Each pricing entity drives something different. Where two overlap, see the notes below — no data merge in this release.", "Każda jednostka cenowa steruje czym innym. Przy nakładaniu się — uwagi poniżej.", "Fiecare entitate de preț conduce altceva. Unde se suprapun — notele de mai jos.", "كل كيان تسعير يقود شيئًا مختلفًا. عند التداخل انظر التنبيهات أدناه.");
        Add("AdminFinance.WhatDrives.Entity", "Entiteit", "Entity", "Jednostka", "Entitate", "الكيان");
        Add("AdminFinance.WhatDrives.Drives", "Bepaalt", "Drives", "Determinuje", "Determină", "يحدد");
        Add("AdminFinance.WhatDrives.TokenPacks", "Tokenpakketten", "Token packs", "Pakiety tokenów", "Pachete de tokenuri", "باقات الرموز");
        Add("AdminFinance.WhatDrives.TokenPacksDesc", "Wat werkgevers betalen voor tokens (euro per pack op de Tokens-pagina).", "What employers pay for tokens (euro per pack on the Tokens page).", "Ile pracodawcy płacą za tokeny (euro za pakiet).", "Cât plătesc angajatorii pentru tokenuri (euro pe pachet).", "ما يدفعه أصحاب العمل مقابل الرموز (يورو لكل باقة).");
        Add("AdminFinance.WhatDrives.TokenSpendCost", "Kosten per actie (TokenSpendCost)", "Cost per action (TokenSpendCost)", "Koszt akcji (TokenSpendCost)", "Cost pe acțiune (TokenSpendCost)", "التكلفة لكل إجراء (TokenSpendCost)");
        Add("AdminFinance.WhatDrives.TokenSpendCostDesc", "Tokens per publicatie/uitlichten/PushBom/ContactUnlock als fallback wanneer geen specifiekere override geldt.", "Tokens per publish/highlight/PushBom/ContactUnlock as fallback when no more specific override applies.", "Tokeny na publikację/wyróżnienie/PushBom/ContactUnlock jako fallback.", "Tokenuri pe publicare/evidențiere/PushBom/ContactUnlock ca fallback.", "رموز لكل نشر/إبراز/PushBom/ContactUnlock كاحتياطي.");
        Add("AdminFinance.WhatDrives.VacancyType", "Token-kosten per vacaturetype (VacancyTypeTokenCost)", "Token cost per vacancy type (VacancyTypeTokenCost)", "Koszt tokenów per typ oferty (VacancyTypeTokenCost)", "Cost token pe tip job (VacancyTypeTokenCost)", "تكلفة الرمز حسب نوع الوظيفة (VacancyTypeTokenCost)");
        Add("AdminFinance.WhatDrives.VacancyTypeDesc", "Catalogustarieven per VacancyKind (Regulier/Stage/…); Regular sync’t naar TokenSpendCost.Publish.", "Catalog rates per VacancyKind (Regular/Internship/…); Regular syncs to TokenSpendCost.Publish.", "Stawki katalogowe per VacancyKind; Regular sync do TokenSpendCost.Publish.", "Tarife catalog pe VacancyKind; Regular se sincronizează cu TokenSpendCost.Publish.", "أسعار الكتالوج لكل VacancyKind؛ Regular يُزامَن إلى TokenSpendCost.Publish.");
        Add("AdminFinance.WhatDrives.VacancyCategory", "Vacaturecategorie (PublishCostTokens e.d.)", "Vacancy category (PublishCostTokens etc.)", "Kategoria oferty (PublishCostTokens itd.)", "Categorie job (PublishCostTokens etc.)", "فئة الوظيفة (PublishCostTokens وغيرها)");
        Add("AdminFinance.WhatDrives.VacancyCategoryDesc", "Wat VacancyProductService écht aftrekt bij publiceren/highlight — wint op de live spend-path.", "What VacancyProductService actually deducts on publish/highlight — wins on the live spend path.", "To, co VacancyProductService realnie potrąca przy publikacji — wygrywa na live spend.", "Ce deduce VacancyProductService la publicare — câștigă pe calea live de spend.", "ما يخصمه VacancyProductService فعليًا عند النشر — يفوز في مسار الصرف الحي.");
        Add("AdminFinance.WhatDrives.SalesPackage", "Salespakketten (SalesPackage)", "Sales packages (SalesPackage)", "Pakiety sprzedażowe (SalesPackage)", "Pachete sales (SalesPackage)", "باقات المبيعات (SalesPackage)");
        Add("AdminFinance.WhatDrives.SalesPackageDesc", "Campagne-/partnerpakketten voor sales (niet de werkgever-tokenpacks).", "Campaign/partner packages for sales (not employer token packs).", "Pakiety kampanii/partnerów dla sales (nie pakiety tokenów pracodawcy).", "Pachete campanie/partener pentru sales (nu pachetele de tokenuri angajator).", "باقات حملات/شركاء للمبيعات (ليست باقات رموز أصحاب العمل).");
        Add("AdminFinance.WhatDrives.PushBom", "PushBom-bereiktiers", "PushBom reach tiers", "Progi zasięgu PushBom", "Niveluri de acoperire PushBom", "طبقات نطاق PushBom");
        Add("AdminFinance.WhatDrives.PushBomDesc", "Tokenprijs naar aantal kandidaten in bereik; flat TokenSpendCost.PushBom is alleen fallback zonder matching tier.", "Token price by candidate count in reach; flat TokenSpendCost.PushBom is fallback only without a matching tier.", "Cena tokenów wg liczby kandydatów; flat TokenSpendCost.PushBom tylko jako fallback.", "Preț token după numărul de candidați; TokenSpendCost.PushBom flat doar ca fallback.", "سعر الرمز بعدد المرشحين في النطاق؛ TokenSpendCost.PushBom الثابت احتياطي فقط.");
        Add("AdminFinance.WhatDrives.SalesCommission", "SalesCommercialSettings / commissies", "SalesCommercialSettings / commissions", "SalesCommercialSettings / prowizje", "SalesCommercialSettings / comisioane", "SalesCommercialSettings / العمولات");
        Add("AdminFinance.WhatDrives.SalesCommissionDesc", "Basis €/token, highlight-toeslagen en commissiepercentages voor salesmanagers.", "Base €/token, highlight surcharges and commission percentages for sales managers.", "Bazowe €/token, dopłaty highlight i % prowizji sales managerów.", "Bază €/token, suplimente highlight și procente comision sales manager.", "أساس €/رمز ورسوم الإبراز ونسب عمولة مديري المبيعات.");
        Add("AdminFinance.Overlap.Publish", "Let op: Vacaturecategorie.PublishCostTokens, VacancyTypeTokenCost (Regular) en TokenSpendCost.Publish bepalen allebei publicatiekosten. Welke wint: VacancyProductService gebruikt categorieprijzen (ResolveCategoryPricingAsync); type/spend-cost worden gesynchroniseerd als fallback/catalogus.", "Note: VacancyCategory.PublishCostTokens, VacancyTypeTokenCost (Regular) and TokenSpendCost.Publish all touch publish cost. Winner: VacancyProductService uses category pricing (ResolveCategoryPricingAsync); type/spend-cost stay synced as fallback/catalog.", "Uwaga: kategoria, VacancyTypeTokenCost (Regular) i TokenSpendCost.Publish — wygrywa cena kategorii w VacancyProductService.", "Atenție: categoria, VacancyTypeTokenCost (Regular) și TokenSpendCost.Publish — câștigă prețul pe categorie în VacancyProductService.", "تنبيه: فئة الوظيفة وVacancyTypeTokenCost (Regular) وTokenSpendCost.Publish — يفوز سعر الفئة في VacancyProductService.");
        Add("AdminFinance.Overlap.Highlight", "Let op: categorie-HighlightCostTokens, SalesCommercial HighlightCarouselTokens en TokenSpendCost.Highlight overlappen. Welke wint: live publish/approve gebruikt categorie-HighlightCostTokens; GetHighlightCostTokensAsync leest HighlightCarouselTokens voor catalogus/partner.", "Note: category HighlightCostTokens, SalesCommercial HighlightCarouselTokens and TokenSpendCost.Highlight overlap. Winner: live publish/approve uses category HighlightCostTokens; GetHighlightCostTokensAsync reads HighlightCarouselTokens for catalog/partner.", "Uwaga: highlight kategorii, HighlightCarouselTokens i TokenSpendCost.Highlight — live używa kategorii.", "Atenție: highlight pe categorie, HighlightCarouselTokens și TokenSpendCost.Highlight — live folosește categoria.", "تنبيه: إبراز الفئة وHighlightCarouselTokens وTokenSpendCost.Highlight — المسار الحي يستخدم الفئة.");
        Add("AdminFinance.Overlap.PushBom", "Let op: PushBom-bereiktiers en TokenSpendCost.PushBom bepalen allebei PushBom-kosten. Welke wint: matching bereik-tier; flat TokenSpendCost.PushBom alleen zonder tier.", "Note: PushBom reach tiers and TokenSpendCost.PushBom both set PushBom cost. Winner: matching reach tier; flat TokenSpendCost.PushBom only without a tier.", "Uwaga: progi PushBom i TokenSpendCost.PushBom — wygrywa dopasowany próg.", "Atenție: nivelurile PushBom și TokenSpendCost.PushBom — câștigă nivelul potrivit.", "تنبيه: طبقات PushBom وTokenSpendCost.PushBom — تفوز الطبقة المطابقة.");

        // 07 · Beveiliging & audit
        Add("AdminShell.SearchCorrelations", "Correlatie", "Correlation", "Korelacja", "Corelație", "الارتباط");
        Add("AdminAudit.Lead", "Wie deed wat, wanneer en waarom. Het auditlog kan niet worden aangepast en wordt 7 jaar bewaard.", "Who did what, when and why. The audit log cannot be changed and is kept for 7 years.", "Kto, co, kiedy i dlaczego. Dziennik audytu jest niezmienny i przechowywany 7 lat.", "Cine a făcut ce, când și de ce. Jurnalul de audit nu poate fi modificat și este păstrat 7 ani.", "من فعل ماذا ومتى ولماذا. سجل التدقيق غير قابل للتعديل ويُحفظ 7 سنوات.");
        Add("AdminAudit.ExportCsv", "Exporteren (CSV)", "Export (CSV)", "Eksport (CSV)", "Export (CSV)", "تصدير (CSV)");
        Add("AdminAudit.SearchPlaceholder", "Zoek object of correlatie-id", "Search object or correlation id", "Szukaj obiektu lub id korelacji", "Caută obiect sau id corelație", "ابحث عن كائن أو معرّف ارتباط");
        Add("AdminAudit.Filter.ActionAll", "Alle acties", "All actions", "Wszystkie akcje", "Toate acțiunile", "كل الإجراءات");
        Add("AdminAudit.Filter.ResultAll", "Alle", "All", "Wszystkie", "Toate", "الكل");
        Add("AdminAudit.Period.7d", "7 dagen", "7 days", "7 dni", "7 zile", "7 أيام");
        Add("AdminAudit.Period.30d", "30 dagen", "30 days", "30 dni", "30 zile", "30 يومًا");
        Add("AdminAudit.Period.90d", "90 dagen", "90 days", "90 dni", "90 zile", "90 يومًا");
        Add("AdminAudit.Period.All", "Alles", "All time", "Całość", "Tot", "الكل");
        Add("AdminAudit.Empty", "Geen gebeurtenissen gevonden.", "No events found.", "Brak zdarzeń.", "Niciun eveniment.", "لا أحداث.");
        Add("AdminAudit.Col.Time", "Tijd", "Time", "Czas", "Oră", "الوقت");
        Add("AdminAudit.Col.Actor", "Door", "By", "Przez", "De", "بواسطة");
        Add("AdminAudit.Col.Action", "Actie", "Action", "Akcja", "Acțiune", "الإجراء");
        Add("AdminAudit.Col.Object", "Object · reden", "Object · reason", "Obiekt · powód", "Obiect · motiv", "الكائن · السبب");
        Add("AdminAudit.Col.Result", "Resultaat", "Result", "Wynik", "Rezultat", "النتيجة");
        Add("AdminAudit.Col.Correlation", "Correlatie", "Correlation ID", "Korelacja", "Corelație", "الارتباط");
        Add("AdminAudit.CopyCorrelation", "Kopieer correlatie-id", "Copy correlation id", "Kopiuj id korelacji", "Copiază id corelație", "نسخ معرّف الارتباط");
        Add("AdminAudit.Result.Success", "Gelukt", "Succeeded", "Udane", "Reușit", "نجح");
        Add("AdminAudit.Result.Denied", "Geblokkeerd", "Denied", "Odrzucone", "Refuzat", "مرفوض");
        Add("AdminAudit.Result.Failed", "Mislukt", "Failed", "Nieudane", "Eșuat", "فشل");
        Add("AdminAudit.Action.MfaReset", "2FA gereset", "2FA reset", "Reset 2FA", "Reset 2FA", "إعادة تعيين 2FA");
        Add("AdminAudit.Action.Support", "Support-toegang", "Support access", "Dostęp support", "Acces support", "وصول الدعم");
        Add("AdminAudit.Action.Setting", "Instelling gewijzigd", "Setting changed", "Zmiana ustawienia", "Setare modificată", "تغيير إعداد");
        Add("AdminAudit.Action.Export", "Export gemaakt", "Export created", "Eksport utworzony", "Export creat", "تم التصدير");
        Add("AdminAudit.Action.Block", "Gebruiker geblokkeerd", "User blocked", "Użytkownik zablokowany", "Utilizator blocat", "مستخدم محظور");
        Add("AdminAudit.Action.Tokens", "Tokens verleend", "Tokens granted", "Przyznano tokeny", "Tokenuri acordate", "رموز ممنوحة");
        Add("AdminAudit.Action.Deleted", "Account verwijderd", "Account deleted", "Konto usunięte", "Cont șters", "حساب محذوف");
        Add("AdminAudit.Action.LoginFailed", "Login mislukt", "Login failed", "Nieudane logowanie", "Autentificare eșuată", "فشل تسجيل الدخول");
        Add("AdminAudit.ActiveSupport", "Actieve support-toegang", "Active support access", "Aktywny dostęp support", "Acces support activ", "وصول دعم نشط");
        Add("AdminAudit.ActiveSupportEmpty", "Geen actieve support-toegang.", "No active support access.", "Brak aktywnego dostępu support.", "Niciun acces support activ.", "لا وصول دعم نشط.");
        Add("AdminAudit.MaskingOn", "Gegevens standaard gemaskeerd", "Data masked by default", "Dane domyślnie maskowane", "Date mascate implicit", "البيانات مقنّعة افتراضيًا");
        Add("AdminAudit.On", "Aan", "On", "Wł.", "Pornit", "تشغيل");
        Add("AdminAudit.RetentionJob", "Bewaartermijn-taak", "Retention job", "Zadanie retencji", "Job retenție", "مهمة الاحتفاظ");
        Add("AdminAudit.RetentionSchedule", "Nacht 03:00", "Night 03:00", "Noc 03:00", "Noapte 03:00", "ليلًا 03:00");
        Add("AdminAudit.LastRetention", "Laatste run", "Last run", "Ostatni przebieg", "Ultima rulare", "آخر تشغيل");
        Add("AdminAudit.Admins", "Beheerders", "Administrators", "Administratorzy", "Administratori", "المسؤولون");
        Add("AdminAudit.WithMfa", "Met 2FA", "With 2FA", "Z 2FA", "Cu 2FA", "مع 2FA");
        Add("AdminAudit.DrawerTitle", "Gebeurtenis", "Event", "Zdarzenie", "Eveniment", "حدث");
        Add("AdminAudit.Reason", "Reden", "Reason", "Powód", "Motiv", "السبب");
        Add("AdminAudit.Changes", "Wijzigingen", "Changes", "Zmiany", "Modificări", "التغييرات");
        Add("AdminDash.RecentAudit", "Recente beheeracties", "Recent admin actions", "Ostatnie działania admina", "Acțiuni admin recente", "إجراءات الإدارة الأخيرة");
        Add("AdminDash.AuditLink", "Auditlog →", "Audit log →", "Dziennik audytu →", "Jurnal audit →", "سجل التدقيق →");
        Add("AdminDash.RecentAuditEmpty", "Nog geen beheeracties gelogd.", "No admin actions logged yet.", "Brak działań admina.", "Nicio acțiune admin încă.", "لا إجراءات إدارة بعد.");
        Add("AdminMfa.Lead", "Overzicht van 2FA-inschrijving per rol en recente resets.", "Overview of 2FA enrollment per role and recent resets.", "Przegląd 2FA per rola i ostatnie resetowania.", "Prezentare 2FA pe rol și resetări recente.", "نظرة عامة على 2FA لكل دور وإعادات التعيين الأخيرة.");
        Add("AdminMfa.ByRole", "Per rol", "By role", "Według roli", "Pe rol", "حسب الدور");
        Add("AdminMfa.Col.Role", "Rol", "Role", "Rola", "Rol", "الدور");
        Add("AdminMfa.Col.Enrolled", "Ingeschreven", "Enrolled", "Zarejestrowani", "Înregistrați", "مسجّلون");
        Add("AdminMfa.Col.NotEnrolled", "Niet ingeschreven", "Not enrolled", "Niezarejestrowani", "Neînregistrați", "غير مسجّلين");
        Add("AdminMfa.Col.ViaIdp", "Via IdP", "Through IdP", "Przez IdP", "Prin IdP", "عبر IdP");
        Add("AdminMfa.Col.Total", "Totaal", "Total", "Razem", "Total", "الإجمالي");
        Add("AdminMfa.Without", "Privileged zonder 2FA", "Privileged without 2FA", "Uprzywilejowani bez 2FA", "Privilegiati fără 2FA", "مميزون بدون 2FA");
        Add("AdminMfa.WithoutEmpty", "Iedereen heeft 2FA.", "Everyone has 2FA.", "Wszyscy mają 2FA.", "Toți au 2FA.", "الجميع لديهم 2FA.");
        Add("AdminMfa.LastActive", "Laatst actief", "Last active", "Ostatnia aktywność", "Ultima activitate", "آخر نشاط");
        Add("AdminMfa.Recent", "Recente 2FA- en support-acties", "Recent 2FA and support actions", "Ostatnie akcje 2FA i support", "Acțiuni 2FA și support recente", "إجراءات 2FA والدعم الأخيرة");
        Add("AdminPrivacy.Lead", "Feiten over maskering, bewaartermijnen en uitgevoerde verwijderingen. Geen verzoekenwachtrij.", "Facts about masking, retention and completed deletions. No request queue.", "Fakty o maskowaniu, retencji i usunięciach. Bez kolejki wniosków.", "Fapte despre mascare, retenție și ștergeri. Fără coadă de cereri.", "حقائق عن التقنيع والاحتفاظ والحذف. بدون طابور طلبات.");
        Add("AdminPrivacy.MaskingDefault", "Persoonsgegevens standaard gemaskeerd", "Personal data masked by default", "Dane osobowe domyślnie maskowane", "Date personale mascate implicit", "البيانات الشخصية مقنّعة افتراضيًا");
        Add("AdminPrivacy.Retention", "Bewaartermijnen", "Retention periods", "Okresy przechowywania", "Perioade de retenție", "فترات الاحتفاظ");
        Add("AdminPrivacy.Col.Dataset", "Dataset", "Data set", "Zbiór", "Set de date", "مجموعة البيانات");
        Add("AdminPrivacy.Col.Days", "Dagen", "Days", "Dni", "Zile", "أيام");
        Add("AdminPrivacy.AdminAudit", "Admin-auditlog", "Admin audit log", "Dziennik audytu admina", "Jurnal audit admin", "سجل تدقيق الإدارة");
        Add("AdminPrivacy.AccessLog", "Gegevensinzage-log", "Data access log", "Dziennik dostępu", "Jurnal acces date", "سجل الاطلاع");
        Add("AdminPrivacy.PlatformLogs", "Systeemlogs", "System logs", "Logi systemowe", "Jurnale sistem", "سجلات النظام");
        Add("AdminPrivacy.LastRun", "Laatste bewaartermijn-run", "Last retention run", "Ostatni przebieg retencji", "Ultima rulare retenție", "آخر تشغيل احتفاظ");
        Add("AdminPrivacy.NoRunYet", "Nog geen run gelogd.", "No run logged yet.", "Brak przebiegu.", "Nicio rulare încă.", "لا تشغيل بعد.");
        Add("AdminPrivacy.RecentDeleted", "Recente accountverwijderingen", "Recent account deletions", "Ostatnie usunięcia kont", "Ștergeri recente de cont", "حذف حسابات حديث");
        Add("AdminPrivacy.NoDeleted", "Geen recente verwijderingen.", "No recent deletions.", "Brak usunięć.", "Nicio ștergere recentă.", "لا حذف حديث.");
        Add("AdminUsers.TabActivity", "Activiteit", "Activity", "Aktywność", "Activitate", "النشاط");
        Add("AdminUsers.ActivityEmpty", "Nog geen activiteit voor deze gebruiker.", "No activity for this user yet.", "Brak aktywności.", "Nicio activitate încă.", "لا نشاط بعد.");
        Add("AdminUsers.ActivityViewed", "Ingezien: {0}", "Viewed: {0}", "Podgląd: {0}", "Vizualizat: {0}", "اطّلع: {0}");
        Add("AdminSettings.FeaturesLeadAudit", "Zet onderdelen van het platform aan of uit. Elke wijziging wordt met reden gelogd in het auditlog.", "Turn platform parts on or off. Every change is logged with a reason in the audit log.", "Włącz lub wyłącz części platformy. Każda zmiana jest logowana z powodem.", "Pornește sau oprește părți ale platformei. Fiecare modificare este jurnalizată cu motiv.", "شغّل أو أوقف أجزاء المنصة. يُسجَّل كل تغيير مع سبب في سجل التدقيق.");
        Add("AdminSettings.Reason", "Reden", "Reason", "Powód", "Motiv", "السبب");
        Add("AdminSettings.ReasonPlaceholder", "Optioneel, verplicht bij riskante wijzigingen", "Optional; required for risky changes", "Opcjonalnie; wymagane przy ryzykownych zmianach", "Opțional; obligatoriu la schimbări riscante", "اختياري؛ مطلوب للتغييرات الخطرة");
        Add("AdminSettings.SaveAndLog", "Opslaan en loggen", "Save and log", "Zapisz i zaloguj", "Salvează și jurnalizează", "حفظ وتسجيل");
        Add("AdminSettings.ChangesPanel", "Wijzigingen", "Changes", "Zmiany", "Modificări", "التغييرات");
        Add("AdminSettings.ChangesEmpty", "Nog geen instellingswijzigingen.", "No setting changes yet.", "Brak zmian ustawień.", "Nicio modificare de setări.", "لا تغييرات إعدادات بعد.");
        Add("AdminSettings.LastChangedBy", "Laatst gewijzigd door {0} · {1}", "Last changed by {0} · {1}", "Ostatnio zmienione przez {0} · {1}", "Ultima modificare de {0} · {1}", "آخر تعديل بواسطة {0} · {1}");
        Add("AdminDataAccess.Actor", "Actor (user-id)", "Actor (user id)", "Aktor (id użytkownika)", "Actor (id utilizator)", "الفاعل (معرّف المستخدم)");
        Add("AdminDataAccess.Subject", "Subject (user-id)", "Subject (user id)", "Podmiot (id użytkownika)", "Subiect (id utilizator)", "الموضوع (معرّف المستخدم)");
        Add("AdminDataAccess.Resource", "Resource", "Resource name", "Zasób", "Nume resursă", "اسم المورد");
        Add("AdminDataAccess.Empty", "Geen inzage-logs gevonden.", "No access logs found.", "Brak logów dostępu.", "Niciun jurnal de acces.", "لا سجلات اطلاع.");
        Add("AdminLogs.Col.Date", "Datum", "Date", "Data", "Dată", "التاريخ");
        Add("AdminLogs.Col.Level", "Level", "Log level", "Poziom", "Nivel", "المستوى");
        Add("AdminLogs.Col.Category", "Categorie", "Category", "Kategoria", "Categorie jurnal", "الفئة");
        Add("AdminLogs.Col.Message", "Bericht", "Message", "Wiadomość", "Mesaj", "الرسالة");
        Add("AdminLogs.Empty", "Geen logs gevonden.", "No logs found.", "Brak logów.", "Niciun jurnal.", "لا سجلات.");
        Add("AdminLogs.CategoryPlaceholder", "Categorie", "Category", "Kategoria", "Filtrează categoria", "الفئة");

        // Onderhoudsmodus (errors 05). Admin UI stays nl + en.
        Add("Admin.Maintenance.Title", "Onderhoudsmodus", "Maintenance mode");
        Add("Admin.Maintenance.Desc", "Zet het platform in onderhoud voor een risicovolle deploy of migratie.", "Put the platform into maintenance for a risky deploy or migration.");
        Add("Admin.Maintenance.Impact", "Iedereen behalve admins ziet de onderhoudspagina.", "Everyone except admins sees the maintenance page.");
        Add("Admin.Maintenance.Banner", "Onderhoudsmodus staat AAN. Bezoekers zien de onderhoudspagina.", "Maintenance mode is ON. Visitors see the maintenance page.");
        Add("Admin.Maintenance.BannerLink", "Naar de schakelaar", "To the switch");
        Add("Admin.Maintenance.ExpectedEnd", "Verwacht klaar om", "Expected to be done at");
        Add("Admin.Maintenance.ExpectedEndHint", "Amsterdamse tijd. Leeg laten mag.", "Amsterdam time. May be left empty.");
        Add("Admin.Maintenance.Note", "Interne notitie", "Internal note");
        Add("Admin.Maintenance.NoteHint", "Alleen voor admins. Bezoekers zien dit nooit.", "Admins only. Visitors never see this.");
        Add("Admin.Maintenance.Propagation", "Binnen 15 seconden overal actief.", "Active everywhere within 15 seconds.");
        Add("Admin.Maintenance.ActiveSince", "Actief sinds {0} door {1}", "Active since {0} by {1}");
        Add("Admin.Maintenance.ConfirmTitle", "Onderhoudsmodus aanzetten", "Turn on maintenance mode");
        Add("Admin.Maintenance.ConfirmLead", "Bezoekers zien vanaf nu de onderhoudspagina. Jij blijft erin.", "Visitors will see the maintenance page from now on. You stay in.");
        Add("Admin.Maintenance.Saved", "Opgeslagen.", "Saved.");
        Add("Admin.Maintenance.UnknownAdmin", "een beheerder", "an admin");
    }
}
