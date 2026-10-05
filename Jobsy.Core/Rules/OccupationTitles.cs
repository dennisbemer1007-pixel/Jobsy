using Jobsy.Core.Localization;

namespace Jobsy.Core.Rules;

/// <summary>Dutch catalogue titles with a plain English name for non-Dutch coach text.</summary>
public static class OccupationTitles
{
    private static readonly Dictionary<string, string> English = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Medewerker tuinbouw / kas"] = "Greenhouse worker",
        ["Magazijnmedewerker / orderpicker"] = "Warehouse worker",
        ["Productiemedewerker"] = "Production worker",
        ["Onderhoudsmonteur"] = "Maintenance technician",
        ["Medewerker bouw / afbouw"] = "Construction worker",
        ["Medewerker schoonmaak"] = "Cleaner",
        ["Kwaliteitscontroleur"] = "Quality checker",
        ["Teelttechnisch medewerker"] = "Crop technician",
        ["Lab- of meetassistent"] = "Lab assistant",
        ["Winkelstylist / visuele presentatie"] = "Shop stylist",
        ["Bloemist / groenpresentatie"] = "Florist",
        ["Content- of seizoensmaker"] = "Content maker",
        ["Helpende zorg"] = "Care assistant",
        ["Helpende zorg en welzijn"] = "Care assistant",
        ["Activiteitenbegeleider"] = "Activity leader",
        ["Medewerker horeca / bediening"] = "Hospitality worker",
        ["Gastvrouw / gastheer"] = "Host",
        ["Begeleider nieuwe collega’s"] = "Buddy for new colleagues",
        ["Verkoopmedewerker winkel"] = "Shop assistant",
        ["Teamleider winkel of horeca"] = "Shop or hospitality lead",
        ["Medewerker verkoop binnendienst"] = "Inside sales worker",
        ["Administratief medewerker"] = "Office administrator",
        ["Planningsmedewerker"] = "Planning worker",
        ["Planner"] = "Planner",
        ["Teamleider logistiek"] = "Logistics lead",
        ["Voorman"] = "Team lead",
        ["Kassamedewerker"] = "Cashier",
        ["Orderadministrator"] = "Order administrator",
        ["Verpleegkundige / zorgmedewerker"] = "Nurse",
        ["Verpleegkundige"] = "Nurse",
        ["Docent / leraar"] = "Teacher",
        ["Docent"] = "Teacher",
        ["ICT-beheerder"] = "IT support",
        ["Softwareontwikkelaar"] = "Software developer",
        ["Chauffeur"] = "Driver",
        ["Heftruckchauffeur"] = "Forklift driver",
        ["Elektricien"] = "Electrician",
        ["Elektrotechnicus"] = "Electrical technician",
        ["Kok"] = "Cook",
        ["Receptionist"] = "Receptionist",
        ["HR-medewerker"] = "HR worker",
        ["Marketingmedewerker"] = "Marketing worker",
        ["Boekhouder / administrateur"] = "Bookkeeper",
        ["Dierenverzorger"] = "Animal caretaker",
        ["Hovenier"] = "Gardener",
        ["Kasmedewerker"] = "Greenhouse worker",
        ["Monteur"] = "Technician"
    };

    private static readonly Dictionary<string, string> Polish = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Medewerker tuinbouw / kas"] = "Pracownik szklarni",
        ["Magazijnmedewerker / orderpicker"] = "Pracownik magazynu",
        ["Productiemedewerker"] = "Pracownik produkcji",
        ["Onderhoudsmonteur"] = "Monter utrzymania",
        ["Medewerker bouw / afbouw"] = "Pracownik budowy",
        ["Medewerker schoonmaak"] = "Pracownik sprzątania",
        ["Kwaliteitscontroleur"] = "Kontroler jakości",
        ["Teelttechnisch medewerker"] = "Pracownik upraw",
        ["Lab- of meetassistent"] = "Asystent laboratorium",
        ["Winkelstylist / visuele presentatie"] = "Stylista sklepu",
        ["Bloemist / groenpresentatie"] = "Kwiaciarz",
        ["Content- of seizoensmaker"] = "Twórca treści",
        ["Helpende zorg"] = "Opiekun",
        ["Helpende zorg en welzijn"] = "Opiekun",
        ["Activiteitenbegeleider"] = "Prowadzący zajęcia",
        ["Medewerker horeca / bediening"] = "Pracownik gastronomii",
        ["Gastvrouw / gastheer"] = "Host",
        ["Begeleider nieuwe collega’s"] = "Opiekun nowych kolegów",
        ["Verkoopmedewerker winkel"] = "Sprzedawca",
        ["Teamleider winkel of horeca"] = "Kierownik sklepu lub gastronomii",
        ["Medewerker verkoop binnendienst"] = "Pracownik sprzedaży",
        ["Administratief medewerker"] = "Pracownik biura",
        ["Planningsmedewerker"] = "Planista",
        ["Planner"] = "Planista",
        ["Teamleider logistiek"] = "Kierownik logistyki",
        ["Voorman"] = "Brygadzista",
        ["Kassamedewerker"] = "Kasjer",
        ["Orderadministrator"] = "Pracownik zamówień",
        ["Verpleegkundige / zorgmedewerker"] = "Pielęgniarz",
        ["Verpleegkundige"] = "Pielęgniarz",
        ["Docent / leraar"] = "Nauczyciel",
        ["Docent"] = "Nauczyciel",
        ["ICT-beheerder"] = "Informatyk",
        ["Softwareontwikkelaar"] = "Programista",
        ["Chauffeur"] = "Kierowca",
        ["Heftruckchauffeur"] = "Operator wózka widłowego",
        ["Elektricien"] = "Elektryk",
        ["Elektrotechnicus"] = "Elektrotechnik",
        ["Kok"] = "Kucharz",
        ["Receptionist"] = "Recepcjonista",
        ["HR-medewerker"] = "Pracownik kadr",
        ["Marketingmedewerker"] = "Pracownik marketingu",
        ["Boekhouder / administrateur"] = "Księgowy",
        ["Dierenverzorger"] = "Opiekun zwierząt",
        ["Hovenier"] = "Ogrodnik",
        ["Kasmedewerker"] = "Pracownik szklarni",
        ["Monteur"] = "Monter"
    };

    private static readonly Dictionary<string, string> Romanian = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Medewerker tuinbouw / kas"] = "Lucrător în seră",
        ["Magazijnmedewerker / orderpicker"] = "Lucrător în depozit",
        ["Productiemedewerker"] = "Lucrător în producție",
        ["Onderhoudsmonteur"] = "Tehnician de întreținere",
        ["Medewerker bouw / afbouw"] = "Lucrător în construcții",
        ["Medewerker schoonmaak"] = "Lucrător la curățenie",
        ["Kwaliteitscontroleur"] = "Controlor de calitate",
        ["Teelttechnisch medewerker"] = "Lucrător la culturi",
        ["Lab- of meetassistent"] = "Asistent de laborator",
        ["Winkelstylist / visuele presentatie"] = "Stilist de magazin",
        ["Bloemist / groenpresentatie"] = "Florar",
        ["Content- of seizoensmaker"] = "Creator de conținut",
        ["Helpende zorg"] = "Îngrijitor",
        ["Helpende zorg en welzijn"] = "Îngrijitor",
        ["Activiteitenbegeleider"] = "Animator",
        ["Medewerker horeca / bediening"] = "Lucrător în ospitalitate",
        ["Gastvrouw / gastheer"] = "Gazdă",
        ["Begeleider nieuwe collega’s"] = "Coleg pentru noii veniți",
        ["Verkoopmedewerker winkel"] = "Vânzător",
        ["Teamleider winkel of horeca"] = "Șef de magazin sau ospitalitate",
        ["Medewerker verkoop binnendienst"] = "Lucrător vânzări",
        ["Administratief medewerker"] = "Lucrător de birou",
        ["Planningsmedewerker"] = "Planificator",
        ["Planner"] = "Planificator",
        ["Teamleider logistiek"] = "Șef logistică",
        ["Voorman"] = "Șef de echipă",
        ["Kassamedewerker"] = "Casier",
        ["Orderadministrator"] = "Lucrător comenzi",
        ["Verpleegkundige / zorgmedewerker"] = "Asistent medical",
        ["Verpleegkundige"] = "Asistent medical",
        ["Docent / leraar"] = "Profesor",
        ["Docent"] = "Profesor",
        ["ICT-beheerder"] = "Suport IT",
        ["Softwareontwikkelaar"] = "Programator",
        ["Chauffeur"] = "Șofer",
        ["Heftruckchauffeur"] = "Operator stivuitor",
        ["Elektricien"] = "Electrician",
        ["Elektrotechnicus"] = "Tehnician electrician",
        ["Kok"] = "Bucătar",
        ["Receptionist"] = "Recepționer",
        ["HR-medewerker"] = "Lucrător resurse umane",
        ["Marketingmedewerker"] = "Lucrător marketing",
        ["Boekhouder / administrateur"] = "Contabil",
        ["Dierenverzorger"] = "Îngrijitor de animale",
        ["Hovenier"] = "Grădinar",
        ["Kasmedewerker"] = "Lucrător în seră",
        ["Monteur"] = "Tehnician"
    };

    private static readonly Dictionary<string, string> Arabic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Medewerker tuinbouw / kas"] = "عامل دفيئة",
        ["Magazijnmedewerker / orderpicker"] = "عامل مستودع",
        ["Productiemedewerker"] = "عامل إنتاج",
        ["Onderhoudsmonteur"] = "فنّي صيانة",
        ["Medewerker bouw / afbouw"] = "عامل بناء",
        ["Medewerker schoonmaak"] = "عامل نظافة",
        ["Kwaliteitscontroleur"] = "مراقب جودة",
        ["Teelttechnisch medewerker"] = "عامل زراعة",
        ["Lab- of meetassistent"] = "مساعد مختبر",
        ["Winkelstylist / visuele presentatie"] = "منسّق متجر",
        ["Bloemist / groenpresentatie"] = "بائع زهور",
        ["Content- of seizoensmaker"] = "صانع محتوى",
        ["Helpende zorg"] = "مساعد رعاية",
        ["Helpende zorg en welzijn"] = "مساعد رعاية",
        ["Activiteitenbegeleider"] = "منظّم أنشطة",
        ["Medewerker horeca / bediening"] = "عامل ضيافة",
        ["Gastvrouw / gastheer"] = "مضيف",
        ["Begeleider nieuwe collega’s"] = "زميل للجدد",
        ["Verkoopmedewerker winkel"] = "بائع",
        ["Teamleider winkel of horeca"] = "قائد متجر أو ضيافة",
        ["Medewerker verkoop binnendienst"] = "موظف مبيعات",
        ["Administratief medewerker"] = "موظف مكتب",
        ["Planningsmedewerker"] = "مخطّط",
        ["Planner"] = "مخطّط",
        ["Teamleider logistiek"] = "قائد لوجستيات",
        ["Voorman"] = "رئيس فريق",
        ["Kassamedewerker"] = "أمين صندوق",
        ["Orderadministrator"] = "موظف طلبات",
        ["Verpleegkundige / zorgmedewerker"] = "ممرض",
        ["Verpleegkundige"] = "ممرض",
        ["Docent / leraar"] = "معلّم",
        ["Docent"] = "معلّم",
        ["ICT-beheerder"] = "دعم تقني",
        ["Softwareontwikkelaar"] = "مطوّر برامج",
        ["Chauffeur"] = "سائق",
        ["Heftruckchauffeur"] = "سائق رافعة شوكية",
        ["Elektricien"] = "كهربائي",
        ["Elektrotechnicus"] = "فنّي كهرباء",
        ["Kok"] = "طبّاخ",
        ["Receptionist"] = "موظف استقبال",
        ["HR-medewerker"] = "موظف موارد بشرية",
        ["Marketingmedewerker"] = "موظف تسويق",
        ["Boekhouder / administrateur"] = "محاسب",
        ["Dierenverzorger"] = "معتني بالحيوانات",
        ["Hovenier"] = "بستاني",
        ["Kasmedewerker"] = "عامل دفيئة",
        ["Monteur"] = "فنّي"
    };

    /// <summary>Dutch title, with a gloss in the chat language. Polish, Romanian and Arabic are not English.</summary>
    public static string ForChat(string title, string? lang)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "";
        }

        var code = JobsyLanguages.Normalize(lang);
        if (code is "" or "nl")
        {
            return title.Trim();
        }

        var map = code switch
        {
            "pl" => Polish,
            "ro" => Romanian,
            "ar" => Arabic,
            _ => English
        };
        var trimmed = title.Trim();
        return map.TryGetValue(trimmed, out var gloss) && !string.Equals(gloss, trimmed, StringComparison.OrdinalIgnoreCase)
            ? $"{trimmed} ({gloss})"
            : trimmed;
    }

    /// <summary>
    /// Translates the Dutch "vanaf" year marker and adds the English job name in brackets.
    /// The Dutch title itself stays, so the fact is still recognisable.
    /// </summary>
    public static string LocalizeWorkLine(string? line, string? language)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return "";
        }

        var lang = JobsyLanguages.Normalize(language);
        var since = lang switch
        {
            "en" => "since",
            "pl" => "od",
            "ro" => "din",
            "ar" => "منذ",
            _ => "vanaf"
        };
        var text = line.Trim();
        string tail = "";
        var marker = text.IndexOf(" vanaf ", StringComparison.Ordinal);
        if (marker >= 0)
        {
            tail = " " + since + text[(marker + " vanaf".Length)..];
            text = text[..marker];
        }

        var paren = text.IndexOf(" (", StringComparison.Ordinal);
        var title = paren >= 0 ? text[..paren] : text;
        var rest = paren >= 0 ? text[paren..] : "";
        return ForChat(title, lang) + rest + tail;
    }
}
