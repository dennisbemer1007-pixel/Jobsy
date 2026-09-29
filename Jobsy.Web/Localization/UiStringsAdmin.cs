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
    }
}
