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
            "Aan = kandidaten zien ‘Mijn Paspoort’ in plaats van ‘Profiel’. Uit = alles zoals nu.",
            "On = candidates see ‘My Passport’ instead of ‘Profile’. Off = everything as today.",
            "Wł. = kandydaci widzą „Mój Paszport” zamiast „Profil”. Wył. = wszystko jak teraz.",
            "Pornit = candidații văd „Pașaportul meu” în loc de „Profil”. Oprit = tot ca acum.",
            "تشغيل = يرى المرشحون «جواز سفري» بدل «الملف». إيقاف = كل شيء كما هو الآن.");
        Add("Admin.EmployersOffPill",
            "Werkgevers staan uit",
            "Employers are off",
            "Pracodawcy wyłączeni",
            "Angajatorii sunt opriți",
            "أصحاب العمل متوقفون");
        Add("Access.EmployersOffTitle",
            "Even alleen voor kandidaten",
            "Candidates only for now",
            "Na razie tylko dla kandydatów",
            "Deocamdată doar pentru candidați",
            "للمرشحين فقط مؤقتاً");
        Add("Access.EmployersOffLead",
            "Lobsy staat even alleen open voor kandidaten. Je gegevens blijven bewaard. We laten het je weten als werkgevers weer welkom zijn.",
            "Lobsy is temporarily open for candidates only. Your data is kept. We’ll let you know when employers are welcome again.",
            "Lobsy jest tymczasowo otwarte tylko dla kandydatów. Twoje dane zostają. Dam y znać, gdy pracodawcy znów będą mile widziani.",
            "Lobsy este deschis temporar doar pentru candidați. Datele tale rămân. Te anunțăm când angajatorii sunt din nou bineveniți.",
            "لوبسي مفتوح مؤقتاً للمرشحين فقط. بياناتك محفوظة. سنُعلمك عندما يعود أصحاب العمل.");
        Add("Access.EmployersOffLogout",
            "Uitloggen",
            "Log out",
            "Wyloguj",
            "Deconectare",
            "تسجيل الخروج");
    }
}
