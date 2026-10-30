namespace Jobsy.Core.Scholen;

/// <summary>Leerling shell strings (worlds, answer scale) for Core PDF and localized parent exports.</summary>
public static class PupilLeerlingScholenStrings
{
    public static bool TryGet(string key, string? language, out string value)
    {
        if (PupilVerhaalLocalized.IsEnglish(language))
        {
            if (En.TryGetValue(key, out value!))
            {
                return true;
            }
        }

        if (Nl.TryGetValue(key, out value!))
        {
            return true;
        }

        value = "";
        return false;
    }

    public static string Get(string key, string? language)
        => TryGet(key, language, out var v) ? v : key;

    private static readonly Dictionary<string, string> Nl = new(StringComparer.Ordinal)
    {
        ["Leerling.World.Reef.Title"] = "Het Koraalrif",
        ["Leerling.World.Reef.Sub"] = "Hoe ben jij?",
        ["Leerling.World.Cave.Title"] = "De schatgrot",
        ["Leerling.World.Cave.Sub"] = "Wat doe je graag?",
        ["Leerling.World.Lighthouse.Title"] = "De vuurtoren",
        ["Leerling.World.Lighthouse.Sub"] = "Wat vind je belangrijk?",
        ["Leerling.World.Lagoon.Title"] = "De lagune",
        ["Leerling.World.Lagoon.Sub"] = "Waar voel je je thuis?",
        ["Leerling.Answer.No"] = "Nee",
        ["Leerling.Answer.NotReally"] = "Niet echt",
        ["Leerling.Answer.Sometimes"] = "Soms",
        ["Leerling.Answer.Quite"] = "Best wel",
        ["Leerling.Answer.Yes"] = "Ja",
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        ["Leerling.World.Reef.Title"] = "The Coral Reef",
        ["Leerling.World.Reef.Sub"] = "What are you like?",
        ["Leerling.World.Cave.Title"] = "The treasure cave",
        ["Leerling.World.Cave.Sub"] = "What do you like to do?",
        ["Leerling.World.Lighthouse.Title"] = "The lighthouse",
        ["Leerling.World.Lighthouse.Sub"] = "What matters to you?",
        ["Leerling.World.Lagoon.Title"] = "The lagoon",
        ["Leerling.World.Lagoon.Sub"] = "Where do you feel at home?",
        ["Leerling.Answer.No"] = "No",
        ["Leerling.Answer.NotReally"] = "Not really",
        ["Leerling.Answer.Sometimes"] = "Sometimes",
        ["Leerling.Answer.Quite"] = "Quite",
        ["Leerling.Answer.Yes"] = "Yes",
    };
}
