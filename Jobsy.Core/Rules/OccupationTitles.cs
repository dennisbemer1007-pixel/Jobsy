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
}
