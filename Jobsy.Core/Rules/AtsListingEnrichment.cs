namespace Jobsy.Core.Rules;

/// <summary>Prompt + local heuristics for normalizing scraped vacancy HTML into readable fields.</summary>
public static class AtsListingEnrichment
{
    public const string SystemPrompt =
        """
        Je normaliseert Nederlandse vacatureteksten voor het Lobsy ATS-overzicht.
        Haal ALLEEN feiten uit de aangeleverde tekst. Verzin niets.
        Antwoord ALLEEN als JSON-object met exact deze velden:
        {
          "title": "korte functietitel of null",
          "description": "strakke NL omschrijving in 2-5 alinea's, zonder navigatie/footer/cookie-ruis",
          "salaryText": "salarisindicatie als '€ 3.200 - € 4.500 bruto/maand' of '€ 16,50 per uur' of null",
          "hoursText": "urenindicatie als '32-36 uur' of null",
          "minHoursPerWeek": null,
          "maxHoursPerWeek": null,
          "startDateText": "starten vanaf, bv. 'per direct' of '1 mei 2026' of null",
          "locationLabel": "standplaats/locatie of null",
          "requirementsText": "vereisten als bullets gescheiden door \\n, of null"
        }
        description: geen HTML, geen cookie-banners, geen menu's. Wel wat de functie inhoudt.
        requirementsText: opleiding, ervaring, certificaten, rijbewijs — elk punt op één regel met '- '.
        """;

    /// <summary>Local fallback when OpenAI is unavailable.</summary>
    public static AtsListingEnrichmentResult FromHeuristics(
        string? title,
        string? locationLabel,
        string? description,
        string? salaryText,
        string? hoursText,
        decimal? minHours,
        decimal? maxHours)
    {
        var cleaned = CleanDescription(description);
        var salary = FirstNonEmpty(salaryText, ExtractSalaryIndication(cleaned), ExtractSalaryIndication(description));
        var hours = FirstNonEmpty(hoursText, ExtractHoursText(cleaned), ExtractHoursText(description));
        var parsedHours = ParseHoursRange(hours);
        var minH = parsedHours?.Min ?? minHours;
        var maxH = parsedHours?.Max ?? maxHours;
        var start = ExtractStartDateText(cleaned) ?? ExtractStartDateText(description);
        var requirements = ExtractRequirements(cleaned) ?? ExtractRequirements(description);
        var location = FirstNonEmpty(locationLabel, ExtractLocationHint(cleaned));

        return new AtsListingEnrichmentResult(
            Title: NullIfBlank(title),
            Description: cleaned,
            SalaryText: NullIfBlank(salary),
            HoursText: NullIfBlank(hours),
            MinHoursPerWeek: minH,
            MaxHoursPerWeek: maxH,
            StartDateText: NullIfBlank(start),
            LocationLabel: NullIfBlank(location),
            RequirementsText: NullIfBlank(requirements),
            FromOpenAi: false);
    }

    public static string CleanDescription(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var t = text.Replace("\r\n", "\n").Replace('\r', '\n');
        // Drop obvious chrome / cookie noise lines.
        var lines = t.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Where(l => !IsNoiseLine(l))
            .ToList();

        t = string.Join("\n", lines);
        t = System.Text.RegularExpressions.Regex.Replace(t, @"[ \t]+", " ");
        t = System.Text.RegularExpressions.Regex.Replace(t, @"\n{3,}", "\n\n");
        return t.Trim();
    }

    private static bool IsNoiseLine(string line)
    {
        if (line.Length < 3)
        {
            return true;
        }

        var lower = line.ToLowerInvariant();
        return lower.Contains("cookie", StringComparison.Ordinal)
               || lower.Contains("privacyverklaring", StringComparison.Ordinal)
               || lower.Contains("toegankelijkheidsverklaring", StringComparison.Ordinal)
               || lower.StartsWith("delen op ", StringComparison.Ordinal)
               || lower is "menu" or "zoeken" or "home" or "footer"
               || lower.Contains("accepteer alle cookies", StringComparison.Ordinal);
    }

    public static string? ExtractSalaryIndication(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Monthly ranges: € 3.200 - € 4.500 / 3200-4500 / 3.200 - 4.500
        var monthly = System.Text.RegularExpressions.Regex.Match(
            text,
            @"€?\s*(\d{1,2}(?:[.\s]\d{3})|\d{4,5})\s*(?:-|–|tot|t/m)\s*€?\s*(\d{1,2}(?:[.\s]\d{3})|\d{4,5})(?:\s*(?:bruto)?\s*(?:per\s*)?(?:maand|/mnd))?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (monthly.Success)
        {
            var a = NormalizeMoneyToken(monthly.Groups[1].Value);
            var b = NormalizeMoneyToken(monthly.Groups[2].Value);
            if (a is >= 1500 and <= 20000 && b is >= 1500 and <= 20000 && b >= a)
            {
                return $"€ {FormatThousands(a.Value)} - € {FormatThousands(b.Value)} bruto/maand";
            }
        }

        var hourly = System.Text.RegularExpressions.Regex.Match(
            text,
            @"€\s*(\d{1,3}(?:[.,]\d{1,2})?)\s*(?:per\s*)?(?:uur|/u)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (hourly.Success)
        {
            return hourly.Value.Trim();
        }

        // Bare "Salaris 4.520 - 6.422" style (Den Haag theme API).
        var bare = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(?:salaris|schaal)[:\s]+€?\s*(\d{1,2}[.\s]\d{3}|\d{4,5})\s*(?:-|–)\s*€?\s*(\d{1,2}[.\s]\d{3}|\d{4,5})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (bare.Success)
        {
            var a = NormalizeMoneyToken(bare.Groups[1].Value);
            var b = NormalizeMoneyToken(bare.Groups[2].Value);
            if (a is >= 1500 and <= 20000 && b is >= 1500 and <= 20000)
            {
                return $"€ {FormatThousands(a.Value)} - € {FormatThousands(b.Value)} bruto/maand";
            }
        }

        return null;
    }

    private static string FormatThousands(decimal value)
    {
        var n = (long)Math.Round(value, MidpointRounding.AwayFromZero);
        return n.ToString("#,##0", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));
    }

    private static decimal? NormalizeMoneyToken(string raw)
    {
        var cleaned = raw.Replace(" ", string.Empty).Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(
            cleaned,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v)
            ? v
            : null;
    }

    public static string? ExtractHoursText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var m = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(\d{1,2}(?:[.,]\d)?)\s*(?:-|–|t/m|tot)\s*(\d{1,2}(?:[.,]\d)?)\s*uur",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            return m.Value.Trim();
        }

        m = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(\d{1,2})\s*uur\s*(?:per\s*week)?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Value.Trim() : null;
    }

    public static (decimal? Min, decimal? Max)? ParseHoursRange(string? hoursText)
    {
        if (string.IsNullOrWhiteSpace(hoursText))
        {
            return null;
        }

        var m = System.Text.RegularExpressions.Regex.Match(
            hoursText,
            @"(\d{1,2}(?:[.,]\d)?)\s*(?:-|–|t/m|tot)\s*(\d{1,2}(?:[.,]\d)?)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            return (
                ParseDec(m.Groups[1].Value),
                ParseDec(m.Groups[2].Value));
        }

        m = System.Text.RegularExpressions.Regex.Match(hoursText, @"(\d{1,2}(?:[.,]\d)?)");
        if (m.Success)
        {
            var v = ParseDec(m.Groups[1].Value);
            return (v, v);
        }

        return null;
    }

    private static decimal? ParseDec(string raw)
    {
        var cleaned = raw.Replace(',', '.');
        return decimal.TryParse(
            cleaned,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v)
            ? v
            : null;
    }

    public static string? ExtractStartDateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(
                text,
                @"\b(per\s+direct|zo\s+spoedig\s+mogelijk|asap)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return "per direct";
        }

        var labeled = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(?:startdatum|starten\s+(?:vanaf|per|op)|indiensttreding|ingang)[:\s]+([^\n.]{3,60})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (labeled.Success)
        {
            var value = labeled.Groups[1].Value.Trim().TrimEnd(',', ';');
            if (value.Length is >= 3 and <= 80)
            {
                return value;
            }
        }

        var date = System.Text.RegularExpressions.Regex.Match(
            text,
            @"\b(\d{1,2}\s+(?:januari|februari|maart|april|mei|juni|juli|augustus|september|oktober|november|december)\s+20\d{2})\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return date.Success ? date.Groups[1].Value.Trim() : null;
    }

    public static string? ExtractRequirements(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var section = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(?:wat\s+(?:wij|we)\s+(?:van\s+jou\s+)?vragen|functie[- ]?eisen|eisen|vereisten|wat\s+breng\s+je\s+mee|profiel)[:\s]*\n+([\s\S]{20,2500}?)(?=\n\s*(?:wat\s+(?:wij|we)\s+bieden|arbeidsvoorwaarden|sollicit|over\s+(?:ons|de\s+organisatie)|salaris|uren)\b|\z)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!section.Success)
        {
            return null;
        }

        var body = section.Groups[1].Value.Trim();
        var bullets = body.Split('\n')
            .Select(l => l.Trim().TrimStart('-', '•', '*', '·').Trim())
            .Where(l => l.Length is >= 8 and <= 220)
            .Where(l => !IsNoiseLine(l))
            .Take(12)
            .Select(l => "- " + l)
            .ToList();

        return bullets.Count >= 2 ? string.Join('\n', bullets) : null;
    }

    private static string? ExtractLocationHint(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var m = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(?:standplaats|locatie|werkplek)[:\s]+([A-Za-zÀ-ÿ\-\s]{2,40})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            return null;
        }

        var loc = m.Groups[1].Value.Trim().TrimEnd(',', '.');
        return loc.Length is >= 2 and <= 40 ? loc : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record AtsListingEnrichmentResult(
    string? Title,
    string? Description,
    string? SalaryText,
    string? HoursText,
    decimal? MinHoursPerWeek,
    decimal? MaxHoursPerWeek,
    string? StartDateText,
    string? LocationLabel,
    string? RequirementsText,
    bool FromOpenAi);
