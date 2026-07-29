using System.Globalization;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Lightweight RFC4180-ish CSV parser for vacancy imports (shared by Web + tests).</summary>
public static class VacancyCsvParser
{
    public sealed record ParsedRow(int RowNumber, IReadOnlyDictionary<string, string> Fields);

    public sealed record ParseResult(
        bool Succeeded,
        string? ErrorMessage,
        IReadOnlyList<string> Headers,
        IReadOnlyList<ParsedRow> Rows);

    public static ParseResult Parse(string? csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return new ParseResult(false, "CSV-bestand is leeg.", [], []);
        }

        var lines = SplitLines(csvText);
        if (lines.Count == 0)
        {
            return new ParseResult(false, "CSV-bestand is leeg.", [], []);
        }

        var headerCells = ParseLine(lines[0]);
        if (headerCells.Count == 0)
        {
            return new ParseResult(false, "CSV-headerrij ontbreekt.", [], []);
        }

        var headers = new List<string>(headerCells.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerCells)
        {
            var canonical = VacancyCsvSchema.CanonicalHeader(cell);
            if (canonical is null)
            {
                return new ParseResult(
                    false,
                    $"Onbekende kolom '{cell.Trim()}'. Gebruik de kolommen uit de How-to.",
                    [],
                    []);
            }

            if (!seen.Add(canonical))
            {
                return new ParseResult(false, $"Dubbele kolom '{canonical}'.", [], []);
            }

            headers.Add(canonical);
        }

        foreach (var required in VacancyCsvSchema.RequiredHeaders)
        {
            if (!seen.Contains(required))
            {
                return new ParseResult(
                    false,
                    $"Verplichte kolom ontbreekt: {required}.",
                    headers,
                    []);
            }
        }

        var rows = new List<ParsedRow>();
        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = ParseLine(line);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                map[headers[c]] = c < cells.Count ? cells[c].Trim() : string.Empty;
            }

            rows.Add(new ParsedRow(i + 1, map));
        }

        if (rows.Count == 0)
        {
            return new ParseResult(false, "CSV bevat geen datarijen.", headers, []);
        }

        return new ParseResult(true, null, headers, rows);
    }

    public static DateOnly? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        string[] formats =
        [
            "yyyy-MM-dd",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy"
        ];

        if (DateOnly.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return DateOnly.TryParse(trimmed, CultureInfo.GetCultureInfo("nl-NL"), DateTimeStyles.None, out date)
            || DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? date
            : null;
    }

    public static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().Replace(',', '.');
        return decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var n)
            ? n
            : null;
    }

    public static Guid? TryParseGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value.Trim(), out var id) ? id : null;
    }

    public static string[] SplitMultiValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(['|', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<string> SplitLines(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n', StringSplitOptions.None).ToList();
        if (lines.Count > 0 && lines[0].StartsWith('\uFEFF'))
        {
            lines[0] = lines[0][1..];
        }

        return lines;
    }

    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            else if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == ',')
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
