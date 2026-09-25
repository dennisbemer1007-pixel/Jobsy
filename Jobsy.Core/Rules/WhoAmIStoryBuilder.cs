using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Local first-person "Wie ben ik?" narrative when OpenAI is unavailable.</summary>
public static class WhoAmIStoryBuilder
{
    public const int MaxStoryChars = 2500;

    public static string Build(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights? profile = null,
        SchwartzValuesScores? values = null)
    {
        var careerTop = TopLabels(
            CareerTestCatalog.RiasecCodes.Select(c => (CareerCompassBuilder.TypeLabel(c), career.Get(c))),
            2);
        var cultureTop = TopLabels(
            CulturePersonalityCatalog.CategoryCodes.Select(c => (CulturePersonalityCatalog.EverydayLabel(c), culture.Get(c))),
            2);
        var compTop = TopLabels(
            CompetencyTestCatalog.CategoryCodes.Select(c => (WhoAmIKeywords.EverydayCompetency(c), competency.Get(c))),
            2);
        var valuesTop = values is { IsComplete: true }
            ? TopLabels(
                SchwartzValuesCatalog.CategoryCodes.Select(c => (SchwartzValuesCatalog.EverydayLabel(c), values.Get(c))),
                2)
            : [];
        var keywords = WhoAmIKeywords.FromScores(competency, career, culture, values);
        profile ??= WhoAmIProfileHighlights.Empty;

        var sb = new StringBuilder();
        sb.Append("Ik ben iemand die tot zijn recht komt bij ");
        sb.Append(JoinDutch(careerTop));
        sb.Append(". Op de werkvloer voel ik me het best bij ");
        sb.Append(JoinDutch(cultureTop));
        sb.AppendLine(".");
        sb.AppendLine();
        if (valuesTop.Count > 0)
        {
            sb.Append("Wat mij drijft is ");
            sb.Append(JoinDutch(valuesTop));
            sb.AppendLine(" — dat zoek ik terug in cultuur en beloftes van een werkgever.");
            sb.AppendLine();
        }

        if (profile.Roles.Count > 0 || profile.Educations.Count > 0 || profile.Certificates.Count > 0)
        {
            sb.Append("In mijn pad zie je ");
            var bits = new List<string>();
            if (profile.Roles.Count > 0)
            {
                bits.Add("ervaring als " + JoinDutch(profile.Roles.Take(2).ToList()));
            }

            if (profile.Educations.Count > 0)
            {
                bits.Add("opleiding in " + JoinDutch(profile.Educations.Take(2).ToList()));
            }

            if (profile.Certificates.Count > 0)
            {
                bits.Add("cursussen zoals " + JoinDutch(profile.Certificates.Take(2).ToList()));
            }

            sb.Append(JoinDutch(bits));
            sb.AppendLine(".");
            sb.AppendLine();
        }

        sb.Append("Op de werkvloer is mijn kracht ");
        sb.Append(JoinDutch(compTop));
        sb.Append(". Ik zoek geen droge lijst van tests, maar werk waarin ik dat elke dag kan laten zien — dichtbij huis, in Den Haag of het Westland, bij een ploeg die op elkaar kan bouwen.");
        sb.AppendLine();
        sb.AppendLine();
        if (keywords.Count > 0)
        {
            sb.Append("Wat mij typeert: ");
            sb.Append(JoinDutch(keywords.Take(4).ToList()));
            sb.Append(". Ik vertel dit verhaal liever in gewone woorden, zodat een werkgever meteen voelt of we bij elkaar passen.");
        }
        else
        {
            sb.Append("Ik vertel dit verhaal liever in gewone woorden, zodat een werkgever meteen voelt of we bij elkaar passen.");
        }

        return Sanitize(sb.ToString()) ?? Fallback;
    }

    public static string? Sanitize(string? story)
    {
        if (string.IsNullOrWhiteSpace(story))
        {
            return null;
        }

        var trimmed = story.Trim();
        if (trimmed.Length > MaxStoryChars)
        {
            trimmed = trimmed[..MaxStoryChars].Trim();
        }

        if (CareerCompassBuilder.ContainsForbiddenJargon(trimmed))
        {
            return null;
        }

        if (trimmed.Contains('@', StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\+?\d[\d\s\-]{7,}\d"))
        {
            return null;
        }

        return trimmed;
    }

    private const string Fallback =
        "Ik ben klaar voor werk dichterbij dan je denkt. Ik zoek een ploeg waar ik mijn inzet, ritme en aandacht voor mensen kwijt kan — in gewone taal, zonder poespas.";

    private static List<string> TopLabels(IEnumerable<(string Label, int Percent)> items, int take)
        => items
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Label)
            .Where(l => !CareerCompassBuilder.ContainsForbiddenJargon(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();

    private static string JoinDutch(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return "betrouwbaar werk";
        }

        if (items.Count == 1)
        {
            return items[0];
        }

        return string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1];
    }
}
