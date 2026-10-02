namespace Jobsy.Web.Localization;

public static class UiStringsFeatureFlags
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

        Add("Admin.EmployersEnabled",
            "Werkgevers actief",
            "Employers active",
            "Pracodawcy aktywni",
            "Angajatori activi",
            "أصحاب العمل نشطون");
        Add("Admin.EmployersEnabledHelp",
            "Uit = alleen zelfontdekking. Banenkaart, vacatures, sollicitaties en werkgeversportalen zijn dan verborgen en geblokkeerd. Er wordt niets verwijderd.",
            "Off = self-discovery only. Job map, vacancies, applications and employer portals are hidden and blocked. Nothing is deleted.",
            "Wył. = tylko samopoznanie. Mapa ofert, wakaty, aplikacje i portale pracodawców są ukryte i zablokowane. Nic nie jest usuwane.",
            "Oprit = doar auto-descoperire. Harta joburilor, posturile, candidaturile și portalele angajatorilor sunt ascunse și blocate. Nimic nu este șters.",
            "إيقاف = اكتشاف ذاتي فقط. خريطة الوظائف والشواغر والطلبات وبوابات أصحاب العمل مخفية وموقوفه. لا يُحذف شيء.");
        Add("Admin.CandidatePassportEnabled",
            "Mijn Paspoort (nieuw profiel)",
            "My Passport (new profile)",
            "Mój Paszport (nowy profil)",
            "Pașaportul meu (profil nou)",
            "جواز سفري (ملف جديد)");
        Add("Admin.CandidatePassportEnabledHelp",
            "Aan (standaard) = kandidaten zien Ontdekkingsreis, Mijn Paspoort, Zoeken, Sollicitaties, Carrière. Bewaard is een tab onder Sollicitaties. Uit = klassieke navigatie.",
            "On (default) = candidates see Discovery, My Passport, Search, Applications, Career. Saved is a tab under Applications. Off = classic navigation.",
            "Wł. (domyślnie) = kandydaci widzą Odkrywanie, Mój Paszport, Szukaj, Aplikacje, Kariera. Zapisane to zakładka. Wył. = klasyczna nawigacja.",
            "Pornit (implicit) = candidații văd Descoperire, Pașaportul meu, Căutare, Candidaturi, Carieră. Salvate e tab. Oprit = navigație clasică.",
            "تشغيل (افتراضي) = يرى المرشحون الاكتشاف وجواز سفري والبحث والطلبات والمسار. المحفوظات تبويب. إيقاف = التنقل الكلاسيكي.");
        Add("Admin.EmployersOffPill",
            "Werkgevers staan uit",
            "Employers are off",
            "Pracodawcy wyłączeni",
            "Angajatorii sunt opriți",
            "أصحاب العمل متوقفون");
    }
}
