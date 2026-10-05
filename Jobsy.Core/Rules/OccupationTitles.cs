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

    /// <summary>Dutch title, with the English name in brackets when the chat language is not Dutch.</summary>
    public static string ForChat(string title, string? lang)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(lang) || lang is "nl")
        {
            return title;
        }

        return English.TryGetValue(title.Trim(), out var english) && !string.Equals(english, title, StringComparison.OrdinalIgnoreCase)
            ? $"{title.Trim()} ({english})"
            : title.Trim();
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
