namespace Jobsy.Web.Localization;

public static class UiStringsPassportPartners
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

        Add("Passport.Shared.Title",
            "Dit deel ik met werkgevers en bureaus",
            "What I share with employers and agencies",
            "Tym dzielę się z pracodawcami i agencjami",
            "Ce partajez cu angajatorii și agențiile",
            "ما أشاركه مع أصحاب العمل والوكالات");
        Add("Passport.Shared.Sub",
            "Alleen feiten die jij kiest",
            "Only facts you choose",
            "Tylko fakty, które wybierasz",
            "Doar faptele pe care le alegi",
            "فقط الحقائق التي تختارها");
        Add("Passport.Shared.Help",
            "Dit staat op je paspoort. Wat je níet leuk vindt blijft privé.",
            "This goes on your passport. What you dislike stays private.",
            "To trafia na paszport. Tego, czego nie lubisz, nikt nie widzi.",
            "Asta apare pe pașaport. Ce nu-ți place rămâne privat.",
            "هذا يظهر على جوازك. ما لا يعجبك يبقى خاصاً.");
        Add("Passport.Shared.Indoor", "Binnen", "Indoors", "Wewnątrz", "În interior", "في الداخل");
        Add("Passport.Shared.Outdoor", "Buiten", "Outdoors", "Na zewnątrz", "În aer liber", "في الخارج");
        Add("Passport.Shared.Physical", "Lichamelijk werk", "Physical work", "Praca fizyczna", "Muncă fizică", "عمل بدني");
        Add("Passport.Shared.Pace", "Tempo", "Pace", "Tempo pracy", "Ritm", "الوتيرة");
        Add("Passport.Shared.ShareEmployers",
            "Toon mijn werkomgeving-voorkeuren",
            "Show my workplace preferences",
            "Pokaż moje preferencje środowiska pracy",
            "Arată preferințele mele de mediu",
            "أظهر تفضيلات بيئة العمل");
        Add("Passport.Shared.WorkRegion", "Werkgebied", "Work area", "Obszar pracy", "Zona de lucru", "منطقة العمل");
        Add("Passport.Shared.OwnCar", "Eigen auto", "Own car", "Własny samochód", "Mașină proprie", "سيارة خاصة");
        Add("Passport.Shared.Contract", "Contract", "Contract type", "Umowa", "Tip de contract", "العقد");
        Add("Passport.Shared.Yes", "Ja", "Yes", "Tak", "Da", "نعم");
        Add("Passport.Shared.No", "Nee", "No", "Nie", "Nu", "لا");
        Add("Passport.Shared.EmailVerified", "E-mail bevestigd", "Email confirmed", "E-mail potwierdzony", "E-mail confirmat", "تم تأكيد البريد");
        Add("Passport.Shared.EmailConfirm", "Bevestig je e-mail", "Confirm your email", "Potwierdź e-mail", "Confirmă e-mailul", "أكد بريدك");
        Add("Passport.Shared.PhoneVerified", "Telefoon bevestigd", "Phone confirmed", "Telefon potwierdzony", "Telefon confirmat", "تم تأكيد الهاتف");
        Add("Passport.Shared.PhoneConfirm", "Bevestig telefoon", "Confirm phone", "Potwierdź telefon", "Confirmă telefonul", "أكد الهاتف");

        Add("WorkPref.Indoor.prefer", "Liever binnen", "Prefer indoors", "Wolę wewnątrz", "Prefer interior", "أفضل الداخل");
        Add("WorkPref.Indoor.ok", "Binnen is oké", "Indoors is fine", "Wewnątrz jest ok", "Interiorul e ok", "الداخل مناسب");
        Add("WorkPref.Indoor.rather-not", "Liever niet binnen", "Rather not indoors", "Raczej nie wewnątrz", "Mai degrabă nu în interior", "أفضل ألا يكون في الداخل");
        Add("WorkPref.Outdoor.prefer", "Liever buiten", "Prefer outdoors", "Wolę na zewnątrz", "Prefer aer liber", "أفضل الخارج");
        Add("WorkPref.Outdoor.ok", "Buiten is oké", "Outdoors is fine", "Na zewnątrz jest ok", "Aerul liber e ok", "الخارج مناسب");
        Add("WorkPref.Outdoor.ok-not-frost", "Buiten, niet bij vorst", "Outdoors, not in frost", "Na zewnątrz, nie przy mrozie", "Afară, nu pe ger", "في الخارج، ليس في الصقيع");
        Add("WorkPref.Outdoor.rather-not", "Liever niet buiten", "Rather not outdoors", "Raczej nie na zewnątrz", "Mai degrabă nu afară", "أفضل ألا يكون في الخارج");
        Add("WorkPref.Physical.light", "Licht", "Light", "Lekka", "Ușoară", "خفيف");
        Add("WorkPref.Physical.standing", "Staand", "Standing", "Na stojąco", "În picioare", "وقوفاً");
        Add("WorkPref.Physical.lifting-15", "Tillen tot 15 kg", "Lifting up to 15 kg", "Dźwiganie do 15 kg", "Ridicare până la 15 kg", "رفع حتى 15 كغ");
        Add("WorkPref.Physical.lifting-25", "Tillen tot 25 kg", "Lifting up to 25 kg", "Dźwiganie do 25 kg", "Ridicare până la 25 kg", "رفع حتى 25 كغ");
        Add("WorkPref.Pace.norm-ok", "Normaal tempo is oké", "A normal pace is fine", "Normalne tempo jest ok", "Ritmul normal e ok", "الوتيرة العادية مناسبة");
        Add("WorkPref.Pace.calm", "Rustig tempo", "A calm pace", "Spokojne tempo", "Ritm calm", "وتيرة هادئة");
        Add("Contract.uitzend", "Uitzend", "Agency", "Agencja", "Agenție", "وكالة");
        Add("Contract.vast", "Vast", "Permanent", "Stały", "Nedeterminat", "دائم");
        Add("Contract.tijdelijk", "Tijdelijk", "Temporary", "Tymczasowy", "Temporar", "مؤقت");
        Add("Contract.seizoen", "Seizoen", "Seasonal", "Sezonowy", "Sezonier", "موسمي");
        Add("Contract.oproep", "Oproep", "On-call", "Na wezwanie", "La chemare", "عند الطلب");
        Add("Contract.geen-voorkeur", "Geen voorkeur", "No preference", "Bez preferencji", "Fără preferință", "بدون تفضيل");
    }
}
