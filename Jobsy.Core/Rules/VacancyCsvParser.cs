using System.Globalization;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>RFC4180-ish CSV parser for vacancy imports (quoted multiline + ,/; delimiter).</summary>
public static class VacancyCsvParser
{
    public const int MaxRows = 500;

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

        var text = csvText.TrimStart('\uFEFF');
        var delimiter = DetectDelimiter(text);
        if (delimiter is null)
        {
            return new ParseResult(
                false,
                "Kon geen CSV-scheidingsteken vinden. Gebruik komma (,) of puntkomma (;).",
                [],
                []);
        }

        var records = ParseRecords(text, delimiter.Value, out var parseError);
        if (parseError is not null)
        {
            return new ParseResult(false, parseError, [], []);
        }

        if (records.Count == 0)
        {
            return new ParseResult(false, "CSV-headerrij ontbreekt.", [], []);
        }

        var headerCells = records[0].Cells;
        if (headerCells.Count == 0 || headerCells.All(string.IsNullOrWhiteSpace))
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
        for (var i = 1; i < records.Count; i++)
        {
            var record = records[i];
            if (record.Cells.Count == 0 || record.Cells.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                map[headers[c]] = c < record.Cells.Count ? record.Cells[c].Trim() : string.Empty;
            }

            rows.Add(new ParsedRow(record.StartLineNumber, map));
        }

        if (rows.Count == 0)
        {
            return new ParseResult(false, "CSV bevat geen datarijen.", headers, []);
        }

        if (rows.Count > MaxRows)
        {
            return new ParseResult(
                false,
                $"Maximaal {MaxRows} rijen per import (bestand heeft {rows.Count}).",
                headers,
                []);
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

    private sealed record CsvRecord(int StartLineNumber, List<string> Cells);

    private static char? DetectDelimiter(string text)
    {
        // Inspect the first logical header line outside quotes.
        var commas = 0;
        var semis = 0;
        var inQuotes = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
            {
                continue;
            }

            if (ch is '\n' or '\r')
            {
                break;
            }

            if (ch == ',')
            {
                commas++;
            }
            else if (ch == ';')
            {
                semis++;
            }
        }

        if (commas == 0 && semis == 0)
        {
            return ','; // single-column edge case; header validation will fail clearly
        }

        return semis > commas ? ';' : ',';
    }

    private static List<CsvRecord> ParseRecords(string text, char delimiter, out string? error)
    {
        error = null;
        var records = new List<CsvRecord>();
        var cells = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        var lineNumber = 1;
        var recordStartLine = 1;
        var i = 0;

        void EndCell()
        {
            cells.Add(sb.ToString());
            sb.Clear();
        }

        void EndRecord()
        {
            EndCell();
            records.Add(new CsvRecord(recordStartLine, cells));
            cells = new List<string>();
            recordStartLine = lineNumber;
        }

        while (i < text.Length)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        sb.Append('"');
                        i += 2;
                        continue;
                    }

                    inQuotes = false;
                    i++;
                    continue;
                }

                if (ch == '\n')
                {
                    lineNumber++;
                }

                sb.Append(ch);
                i++;
                continue;
            }

            if (ch == '"')
            {
                inQuotes = true;
                i++;
                continue;
            }

            if (ch == delimiter)
            {
                EndCell();
                i++;
                continue;
            }

            if (ch == '\r')
            {
                i++;
                continue;
            }

            if (ch == '\n')
            {
                lineNumber++;
                EndRecord();
                i++;
                continue;
            }

            sb.Append(ch);
            i++;
        }

        if (inQuotes)
        {
            error = $"CSV heeft een niet-afgesloten aanhalingsteken (rond regel {recordStartLine}).";
            return [];
        }

        // Trailing content / final record (even if file ends without newline).
        if (sb.Length > 0 || cells.Count > 0)
        {
            EndRecord();
        }

        return records;
    }
}
