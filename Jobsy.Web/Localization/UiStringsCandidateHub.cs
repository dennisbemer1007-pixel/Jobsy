namespace Jobsy.Web.Localization;

public static class UiStringsCandidateHub
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
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

        Add("CandidateSettings.Title",
            "Instellingen", "Settings",
            "Ustawienia", "Setări", "الإعدادات");
        Add("CandidateSettings.Lead",
            "Alles wat je account, meldingen en voorkeuren regelt, op één plek.",
            "Everything for your account, notifications and preferences, in one place.",
            "Konto, powiadomienia i preferencje w jednym miejscu.",
            "Cont, notificări și preferințe, într-un singur loc.",
            "حسابك وإشعاراتك وتفضيلاتك في مكان واحد.");
        Add("CandidateSettings.NavSettings",
            "Instellingen", "Settings",
            "Ustawienia", "Setări", "الإعدادات");
        Add("CandidateSettings.NavPrivacy",
            "Privacy", "Privacy",
            "Prywatność", "Confidențialitate", "الخصوصية");
        Add("CandidateSettings.PickPane",
            "Kies links wat je wilt bekijken. Op desktop staat mail hier direct open.",
            "Pick a section on the left. On desktop, mail settings open here by default.",
            "Wybierz sekcję po lewej. Na desktopie ustawienia maila są tutaj.",
            "Alege o secțiune în stânga. Pe desktop, mailul se deschide aici.",
            "اختر قسمًا على اليسار. على سطح المكتب تفتح إعدادات البريد هنا.");
        Add("CandidateSettings.Mail.Title",
            "E-mail en meldingen", "E-mail and notifications",
            "E-mail i powiadomienia", "E-mail și notificări", "البريد والإشعارات");
        Add("CandidateSettings.Mail.Desc",
            "Herinneringen, tips en optionele updates per e-mail (en WhatsApp als je dat aanzet).",
            "Reminders, tips and optional e-mail updates (and WhatsApp if you enable it).",
            "Przypomnienia, tipy i opcjonalne e-maile (oraz WhatsApp, jeśli włączysz).",
            "Memento-uri, sfaturi și e-mail opțional (și WhatsApp dacă activezi).",
            "تذكيرات ونصائح وتحديثات اختيارية بالبريد (وواتساب إن فعّلته).");
        Add("CandidateSettings.Proactive.Title",
            "Lobsy zoekt voor je", "Lobsy searches for you",
            "Lobsy szuka dla Ciebie", "Lobsy caută pentru tine", "Lobsy يبحث لك");
        Add("CandidateSettings.Proactive.Desc",
            "Toestemming, frequentie en pauze voor proactieve vacaturetips.",
            "Consent, frequency and pause for proactive vacancy tips.",
            "Zgoda, częstotliwość i pauza dla proaktywnych tipów.",
            "Consimțământ, frecvență și pauză pentru sfaturi proactive.",
            "الموافقة وتكرار الإشعارات وإيقاف المطابقة الاستباقية.");
        Add("CandidateSettings.Profile.Title",
            "Mijn gegevens", "My details",
            "Moje dane", "Datele mele", "بياناتي");
        Add("CandidateSettings.Profile.Desc",
            "Profiel, apparaten en beschikbaarheid in je paspoort.",
            "Profile, devices and availability in your passport.",
            "Profil, urządzenia i dostępność w paszporcie.",
            "Profil, dispozitive și disponibilitate în pașaport.",
            "الملف والأجهزة والتوفر في جوازك.");
        Add("CandidateSettings.How.Title",
            "Hoe werkt Lobsy?", "How does Lobsy work?",
            "Jak działa Lobsy?", "Cum funcționează Lobsy?", "كيف يعمل Lobsy؟");
        Add("CandidateSettings.How.Desc",
            "Uitleg over privacy, matching en je ontdekkingsreis.",
            "How privacy, matching and your discovery journey work.",
            "Prywatność, dopasowanie i ontdekkingsreis.",
            "Confidențialitate, potrivire și călătoria ta.",
            "الخصوصية والمطابقة ورحلة الاكتشاف.");

        Add("Profile.Hub.ViewPassport",
            "Bekijk je paspoort", "View your passport",
            "Zobacz paszport", "Vezi pașaportul", "اعرض جوازك");
        Add("Profile.Hub.StatusComplete",
            "Compleet", "Complete",
            "Kompletny", "Complet", "مكتمل");

        Add("CandidatePrivacy.Title",
            "Privacy", "Privacy",
            "Prywatność", "Confidențialitate", "الخصوصية");
        Add("CandidatePrivacy.Lead",
            "Jouw data blijft van jou. Hier regel je inzage, export en toestemming.",
            "Your data stays yours. Manage access, export and consent here.",
            "Twoje dane należą do Ciebie. Eksport i zgody tutaj.",
            "Datele tale rămân ale tale. Export și consimțământ aici.",
            "بياناتك لك. تصدير وموافقة من هنا.");
        Add("CandidatePrivacy.Policy.Title",
            "Privacyverklaring", "Privacy statement",
            "Polityka prywatności", "Declarație de confidențialitate", "بيان الخصوصية");
        Add("CandidatePrivacy.Policy.Desc",
            "Wat we verzamelen, waarom, en hoe lang we bewaren.",
            "What we collect, why, and retention.",
            "Co zbieramy i jak długo przechowujemy.",
            "Ce colectăm și cât păstrăm.",
            "ما نجمعه ولماذا ومدة الاحتفاظ.");
        Add("CandidatePrivacy.Data.Title",
            "Mijn gegevens (AVG)", "My data (GDPR)",
            "Moje dane (RODO)", "Datele mele (GDPR)", "بياناتي (GDPR)");
        Add("CandidatePrivacy.Data.Desc",
            "Download een export, bekijk support-inzage, of vraag verwijdering aan.",
            "Download an export, see support access, or request deletion.",
            "Pobierz export lub usuń konto.",
            "Export sau ștergere cont.",
            "تصدير أو حذف الحساب.");
        Add("CandidatePrivacy.Consent.Title",
            "Toestemmingen in je profiel", "Consents in your profile",
            "Zgody w profilu", "Consimțăminte în profil", "الموافقات في ملفك");
        Add("CandidatePrivacy.Consent.Desc",
            "Talentpool, matching en commerciële voorkeuren. Aanpassen in Mijn gegevens.",
            "Talent pool, matching and marketing. Edit under My details.",
            "Talent pool i marketing. W danych paszportu.",
            "Talent pool și marketing. În datele pașaportului.",
            "مجموعة المواهب والتسويق. في بيانات الجواز.");
        Add("CandidatePrivacy.Mail.Title",
            "E-mailvoorkeuren", "E-mail preferences",
            "Preferencje e-mail", "Preferințe e-mail", "تفضيلات البريد");
        Add("CandidatePrivacy.Mail.Desc",
            "Zet herinneringen en optionele mails uit (essentiële mail blijft).",
            "Turn off reminders and optional mail (essential mail stays).",
            "Wyłącz przypomnienia i opcjonalne maile.",
            "Dezactivează memento-uri și mail opțional.",
            "إيقاف التذكيرات والبريد الاختياري.");
    }
}
