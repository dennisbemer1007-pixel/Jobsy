namespace Jobsy.Core.Careers;

/// <summary>Short curated duty phrases (2–5 words) for onboarding work-history chips.</summary>
public static class OccupationDutySuggestions
{
    private sealed record Family(string[] Keywords, string[] Nl, string[] En);

    private static readonly Family[] Families =
    [
        new(
            ["magazijn", "warehouse", "orderpicker", "logistiek medew"],
            ["Orders verzamelen", "Goederen ontvangen", "Voorraad bijhouden", "Heftruck rijden", "Picken en packen"],
            ["Pick orders", "Receive goods", "Track stock", "Drive forklift", "Pick and pack"]),
        new(
            ["verkoop", "winkel", "retail", "verkoper", "sales"],
            ["Klanten adviseren", "Kassa bedienen", "Winkel netjes houden", "Voorraad aanvullen", "Klanten helpen"],
            ["Advise customers", "Run the register", "Keep store tidy", "Restock shelves", "Help customers"]),
        new(
            ["zorg", "verpleeg", "helpende", "nursing", "care"],
            ["Patiënten ondersteunen", "Medicatie klaarzetten", "Hygiëne bewaken", "Dagrapportage maken", "Samenwerken in team"],
            ["Support patients", "Prepare medication", "Maintain hygiene", "Write daily notes", "Team collaboration"]),
        new(
            ["horeca", "kok", "bediening", "chef", "kitchen"],
            ["Gasten bedienen", "Mise en place", "Keuken schoonhouden", "Bestellingen opnemen", "Drankjes tappen"],
            ["Serve guests", "Prep ingredients", "Keep kitchen clean", "Take orders", "Pour drinks"]),
        new(
            ["schoonma", "clean", "facility"],
            ["Ruimtes schoonmaken", "Materialen bijvullen", "Veilig werken", "Klachten melden", "Planning volgen"],
            ["Clean rooms", "Restock supplies", "Work safely", "Report issues", "Follow schedule"]),
        new(
            ["kantoor", "administrat", "office", "secretaresse"],
            ["Post verwerken", "Agenda bijhouden", "Documenten ordenen", "Telefoon beantwoorden", "Gegevens invoeren"],
            ["Handle mail", "Manage calendar", "Organize files", "Answer phone", "Enter data"]),
        new(
            ["monteur", "techniek", "mechanic", "engineer", "technician"],
            ["Storingen oplossen", "Onderhoud uitvoeren", "Veilig werken", "Werkbon invullen", "Tools onderhouden"],
            ["Fix breakdowns", "Do maintenance", "Work safely", "Fill work orders", "Maintain tools"]),
        new(
            ["chauffeur", "bezorger", "driver", "courier", "transport"],
            ["Ritten plannen", "Veilig rijden", "Goederen laden", "Route volgen", "Klanten informeren"],
            ["Plan routes", "Drive safely", "Load goods", "Follow route", "Update customers"]),
    ];

    private static readonly string[] GenericNl =
    [
        "Klanten helpen",
        "Samenwerken in een team",
        "Planning maken",
        "Kwaliteit bewaken",
        "Veilig werken",
        "Problemen oplossen",
    ];

    private static readonly string[] GenericEn =
    [
        "Help customers",
        "Work in a team",
        "Plan your work",
        "Watch quality",
        "Work safely",
        "Solve problems",
    ];

    public static IReadOnlyList<string> ForRole(string? role, string language = "nl", int max = 6)
    {
        var limit = Math.Clamp(max, 1, 6);
        if (string.IsNullOrWhiteSpace(role))
        {
            return [];
        }

        var folded = role.Trim().ToLowerInvariant();
        foreach (var family in Families)
        {
            if (family.Keywords.Any(k => folded.Contains(k, StringComparison.Ordinal)))
            {
                return Take(family, language, limit);
            }
        }

        var occupation = OccupationCatalog.Shared.Resolve(role.Trim())
            ?? ResolveFromSearch(role.Trim());
        if (occupation is not null)
        {
            var title = occupation.Nl.ToLowerInvariant();
            foreach (var family in Families)
            {
                if (family.Keywords.Any(k => title.Contains(k, StringComparison.Ordinal)))
                {
                    return Take(family, language, limit);
                }
            }
        }

        return TakeGeneric(language, limit);
    }

    private static Occupation? ResolveFromSearch(string role)
    {
        var hits = OccupationCatalog.Shared.Search(role, 1);
        if (hits.Count == 0)
        {
            return null;
        }

        return OccupationCatalog.Shared.Get(hits[0].EscoId);
    }

    private static IReadOnlyList<string> Take(Family family, string language, int max)
    {
        var list = IsEnglish(language) ? family.En : family.Nl;
        return list.Take(max).ToList();
    }

    private static IReadOnlyList<string> TakeGeneric(string language, int max)
        => (IsEnglish(language) ? GenericEn : GenericNl).Take(max).ToList();

    private static bool IsEnglish(string language)
        => language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
}
