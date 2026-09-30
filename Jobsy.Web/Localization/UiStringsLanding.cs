namespace Jobsy.Web.Localization;

/// <summary>Public shell strings (nav, footer, skip link). Prefixes: PublicNav.*, PublicFooter.*, Public.*.</summary>
public static class UiStringsLanding
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

        // —— Nav ——
        Add("PublicNav.HowItWorks", "Hoe het werkt", "How it works", "Jak to działa", "Cum funcționează", "كيف يعمل");
        Add("PublicNav.JobMap", "Banenkaart", "Job map", "Mapa ofert", "Harta joburilor", "خريطة الوظائف");
        Add("PublicNav.Employers", "Werkgevers", "Employers", "Pracodawcy", "Angajatori", "أصحاب العمل");
        Add("PublicNav.Schools", "Scholen", "Schools", "Szkoły", "Școli", "المدارس");
        Add("PublicNav.Partners", "Partners", "Partner orgs", "Partnerzy", "Parteneri", "الشركاء");
        Add("PublicNav.MyPassport", "Mijn Paspoort", "My Passport", "Mój Paszport", "Pașaportul meu", "جواز سفري");
        Add("PublicNav.Discovery", "Ontdekkingsreis", "Discovery journey", "Podróż odkrywcza", "Călătoria de descoperire", "رحلة الاكتشاف");
        Add("PublicNav.Login", "Inloggen", "Log in", "Zaloguj się", "Autentificare", "تسجيل الدخول");
        Add("PublicNav.TakeFreeTest", "Doe de gratis test", "Take the free test", "Zrób darmowy test", "Fă testul gratuit", "أجرِ الاختبار المجاني");
        Add("PublicNav.CreateAccount", "Account maken", "Create account", "Utwórz konto", "Creează cont", "إنشاء حساب");
        Add("PublicNav.Menu", "Menu", "Open menu", "Otwórz menu", "Meniu", "القائمة");
        Add("PublicNav.Close", "Sluiten", "Close", "Zamknij", "Închide", "إغلاق");
        Add("PublicNav.SkipToContent", "Naar de inhoud", "Skip to content", "Przejdź do treści", "Sari la conținut", "انتقل إلى المحتوى");
        Add("PublicNav.ChooseLanguage", "Kies je taal", "Choose your language", "Wybierz język", "Alege limba", "اختر لغتك");
        Add("PublicNav.QuickLinks", "Snelle links", "Quick links", "Szybkie linki", "Linkuri rapide", "روابط سريعة");
        Add("PublicNav.MainNav", "Hoofdnavigatie", "Main navigation", "Główna nawigacja", "Navigare principală", "التنقل الرئيسي");

        // —— Footer ——
        Add("PublicFooter.BrandLine",
            "Lobsy laat zien wie jij bent en welk werk bij je past.",
            "Lobsy shows who you are and which work fits you.",
            "Lobsy pokazuje, kim jesteś i jaka praca do Ciebie pasuje.",
            "Lobsy arată cine ești și ce muncă ți se potrivește.",
            "لوبسي يُظهر من أنت وأي عمل يناسبك.");
        Add("PublicFooter.BrandLine.Zw",
            "Lobsy laat zien wie jij bent en welke richting bij je past.",
            "Lobsy shows who you are and which direction fits you.",
            "Lobsy pokazuje, kim jesteś i jaki kierunek do Ciebie pasuje.",
            "Lobsy arată cine ești și ce direcție ți se potrivește.",
            "لوبسي يُظهر من أنت وأي اتجاه يناسبك.");
        Add("PublicFooter.Brand", "Lobsy", "Lobsy", "Lobsy", "Lobsy", "Lobsy");
        Add("PublicFooter.For", "Voor", "For", "Dla", "Pentru", "من أجل");
        Add("PublicFooter.Account", "Account", "Your account", "Konto", "Cont", "الحساب");
        Add("PublicFooter.Legal", "Juridisch", "Legal", "Prawne", "Juridic", "قانوني");
        Add("PublicFooter.Candidates", "Kandidaten", "Candidates", "Kandydaci", "Candidați", "المرشحون");
        Add("PublicFooter.You", "Jou", "You", "Ciebie", "Tine", "أنت");
        Add("PublicFooter.NewInNetherlands", "Nieuw in Nederland", "New in the Netherlands", "Nowy w Holandii", "Nou în Țările de Jos", "جديد في هولندا");
        Add("PublicFooter.CompanyRegister", "Werkgever registreren", "Register as employer", "Zarejestruj pracodawcę", "Înregistrează angajator", "تسجيل صاحب عمل");
        Add("PublicFooter.Privacy", "Privacy", "Privacy policy", "Prywatność", "Confidențialitate", "الخصوصية");
        Add("PublicFooter.Cookies", "Cookies", "Cookie settings", "Pliki cookie", "Cookie-uri", "ملفات تعريف الارتباط");
        Add("PublicFooter.Terms", "Algemene voorwaarden", "Terms and conditions", "Regulamin", "Termeni și condiții", "الشروط العامة");
        Add("PublicFooter.UsageTerms", "Gebruiksvoorwaarden", "Terms of use", "Warunki użytkowania", "Condiții de utilizare", "شروط الاستخدام");
        Add("PublicFooter.About", "Wie zijn wij", "About us", "O nas", "Despre noi", "من نحن");
        Add("PublicFooter.MadeWith", "gemaakt met", "made with", "stworzone z", "realizat cu", "صُنع بـ");
        Add("PublicFooter.Copyright",
            "© {0} Lobsy · {1} 🧡",
            "© {0} Lobsy · {1} with 🧡",
            "© {0} Lobsy · {1} z 🧡",
            "© {0} Lobsy · {1} cu 🧡",
            "© {0} Lobsy · {1} مع 🧡");

        // —— Generic public ——
        Add("Public.Sample", "Voorbeeld", "Sample", "Przykład", "Exemplu", "مثال");
    }
}
