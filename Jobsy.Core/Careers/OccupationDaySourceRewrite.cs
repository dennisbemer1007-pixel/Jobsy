using System.Text.RegularExpressions;

namespace Jobsy.Core.Careers;

/// <summary>
/// Turns stiff ESCO/ILO Dutch into plain B1 without adding facts. English ILO lines are used when the Dutch line is wrong.
/// </summary>
public static partial class OccupationDaySourceRewrite
{
    public enum WorkplaceKind
    {
        General,
        Greenhouse,
        Kitchen,
        RoadTransport,
    }

    public static WorkplaceKind DetectWorkplace(string titleNl, string description, IReadOnlyList<string> altNames)
    {
        var blob = string.Join(
            ' ',
            new[] { titleNl, description }.Concat(altNames ?? [])).ToLowerInvariant();
        if (blob.Contains("glastuin", StringComparison.Ordinal)
            || blob.Contains("kwekerij", StringComparison.Ordinal)
            || blob.Contains("kas", StringComparison.Ordinal)
            || blob.Contains("tuinbouw", StringComparison.Ordinal)
            || blob.Contains("gewas", StringComparison.Ordinal))
        {
            return WorkplaceKind.Greenhouse;
        }

        if (blob.Contains("kok", StringComparison.Ordinal)
            || blob.Contains("keuken", StringComparison.Ordinal)
            || blob.Contains("chef", StringComparison.Ordinal)
            || blob.Contains("maaltijd", StringComparison.Ordinal))
        {
            return WorkplaceKind.Kitchen;
        }

        if (blob.Contains("vrachtwagen", StringComparison.Ordinal)
            || blob.Contains("truck", StringComparison.Ordinal)
            || blob.Contains("chauffeur", StringComparison.Ordinal)
            || blob.Contains("transport", StringComparison.Ordinal))
        {
            return WorkplaceKind.RoadTransport;
        }

        return WorkplaceKind.General;
    }

    public static string RewriteDescription(string? description)
    {
        var text = (description ?? "").Trim();
        if (text.Length == 0)
        {
            return text;
        }

        text = DedupeAdjacentWords(text);
        return text;
    }

    /// <summary>Plain B1 task line. <paramref name="english"/> is the ILO source when Dutch is a bad translation.</summary>
    public static string RewriteTask(string? dutch, string? english, WorkplaceKind workplace)
    {
        var nl = DedupeAdjacentWords((dutch ?? "").Trim());
        var en = (english ?? "").Trim();
        if (nl.Length == 0 && en.Length > 0)
        {
            nl = FromEnglishTask(en, workplace);
        }
        else if (nl.Length > 0 && en.Length > 0 && LooksLikeBadDutchTask(nl, en, workplace))
        {
            nl = FromEnglishTask(en, workplace);
        }
        else if (nl.Length > 0)
        {
            nl = SoftenDutchTask(nl, workplace);
        }

        return nl;
    }

    private static bool LooksLikeBadDutchTask(string nl, string en, WorkplaceKind workplace)
    {
        var lower = nl.ToLowerInvariant();
        if (workplace == WorkplaceKind.Greenhouse
            && (lower.Contains("tuin schoonmaken", StringComparison.Ordinal)
                || lower.Contains("tuinen schoonmaken", StringComparison.Ordinal)
                || lower.StartsWith("het schoonmaken van tuinen", StringComparison.Ordinal)))
        {
            return true;
        }

        if (lower.Contains("keukenhulpverlener", StringComparison.Ordinal))
        {
            return true;
        }

        if (AdjacentDuplicateWord().IsMatch(nl))
        {
            return true;
        }

        if (nl.StartsWith("Het ", StringComparison.Ordinal) && nl.Length > 90)
        {
            return true;
        }

        if (en.Contains("garden", StringComparison.OrdinalIgnoreCase)
            && workplace == WorkplaceKind.Greenhouse
            && lower.Contains("tuin", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string FromEnglishTask(string en, WorkplaceKind workplace)
    {
        var lower = en.ToLowerInvariant();
        if (workplace == WorkplaceKind.Greenhouse && lower.Contains("cleaning garden", StringComparison.Ordinal))
        {
            return "Je ruimt afval op en houdt paden vrij in en rond de kas.";
        }

        if (lower.Contains("tending plants", StringComparison.Ordinal) || lower.Contains("hand watering", StringComparison.Ordinal))
        {
            return "Je verzorgt planten: water geven en onkruid wieden.";
        }

        if (lower.Contains("harvesting and packaging", StringComparison.Ordinal))
        {
            return "Je oogst gewassen en verpakt ze voor verkoop en transport.";
        }

        if (lower.Contains("minor repairs", StringComparison.Ordinal))
        {
            return "Je doet kleine reparaties aan hekken, apparatuur en gebouwen.";
        }

        if (lower.Contains("weighing, measuring and mixing", StringComparison.Ordinal))
        {
            return "Je weegt, meet en mengt ingrediënten volgens het recept.";
        }

        if (lower.Contains("regulating the temperature", StringComparison.Ordinal) || lower.Contains("ovens, grills", StringComparison.Ordinal))
        {
            return "Je regelt de temperatuur van ovens, grills en ander kookapparatuur.";
        }

        if (lower.Contains("operating large-volume cooking", StringComparison.Ordinal))
        {
            return "Je bedient groot kookapparatuur, zoals grills, friteuses of bakplaten.";
        }

        if (lower.Contains("planning, supervising and coordinating", StringComparison.Ordinal)
            && lower.Contains("kitchen", StringComparison.Ordinal))
        {
            return "Je plant het werk en stemt af met keukenhulp in de keuken.";
        }

        if (lower.Contains("driving and tending a heavy motor", StringComparison.Ordinal))
        {
            return "Je rijdt een vrachtwagen of ander zwaar voertuig om goederen te vervoeren.";
        }

        if (lower.Contains("determining the most appropriate routes", StringComparison.Ordinal))
        {
            return "Je kiest de beste route voor de rit.";
        }

        if (lower.Contains("loading or unloading", StringComparison.Ordinal))
        {
            return "Je helpt bij laden en lossen, soms met een hef- of kiepinstallatie.";
        }

        if (lower.Contains("minor maintenance to vehicles", StringComparison.Ordinal))
        {
            return "Je doet klein onderhoud aan het voertuig en regelt grotere reparaties.";
        }

        return SoftenDutchTask(en, workplace);
    }

    private static string SoftenDutchTask(string line, WorkplaceKind workplace)
    {
        var text = DedupeAdjacentWords(line.Trim());
        if (text.StartsWith("Het ", StringComparison.Ordinal))
        {
            text = text[4..].Trim();
            if (text.Length > 0)
            {
                text = char.ToUpperInvariant(text[0]) + text[1..];
            }

            if (!text.StartsWith("Je ", StringComparison.Ordinal))
            {
                text = "Je " + text.ToLowerInvariant();
            }
        }

        text = text
            .Replace("keukenhulpverleners", "keukenhulp", StringComparison.OrdinalIgnoreCase)
            .Replace("keukenhulpverlener", "keukenhulp", StringComparison.OrdinalIgnoreCase)
            .Replace("Regulering van de temperatuur", "Je regelt de temperatuur", StringComparison.Ordinal)
            .Replace("regulering van de temperatuur", "je regelt de temperatuur", StringComparison.Ordinal);

        if (workplace == WorkplaceKind.Greenhouse)
        {
            text = text
                .Replace("Het schoonmaken van tuinen", "Afval opruimen in en rond de kas", StringComparison.OrdinalIgnoreCase)
                .Replace("tuinen schoonmaken", "afval opruimen in de kas", StringComparison.OrdinalIgnoreCase);
        }

        return DedupeAdjacentWords(text);
    }

    public static string DedupeAdjacentWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? "";
        }

        var result = AdjacentDuplicateWord().Replace(text, "$1");
        result = AdjacentDuplicateComma().Replace(result, "$1");
        return result;
    }

    [GeneratedRegex(@"\b([\p{L}]{3,})\s*[,;]\s*\1\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AdjacentDuplicateComma();

    [GeneratedRegex(@"\b([\p{L}]{3,})\s+\1\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AdjacentDuplicateWord();
}
