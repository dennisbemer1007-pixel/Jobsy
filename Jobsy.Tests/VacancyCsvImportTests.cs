using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class VacancyCsvParserTests
{
    [Fact]
    public void Parse_requires_headers_and_maps_aliases()
    {
        var csv = """
            title,description,start_date,end_date,work_types,salary_table_id
            Kassamedewerker,"Leuke baan, fulltime",2026-08-01,2026-12-31,Winkel,11111111-1111-1111-1111-111111111111
            """;

        var result = VacancyCsvParser.Parse(csv);
        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Single(result.Rows);
        Assert.Equal("Kassamedewerker", result.Rows[0].Fields[VacancyCsvSchema.Title]);
        Assert.Equal("Leuke baan, fulltime", result.Rows[0].Fields[VacancyCsvSchema.Description]);
        Assert.Equal("Winkel", result.Rows[0].Fields[VacancyCsvSchema.Branches]);
    }

    [Fact]
    public void Parse_fails_on_missing_required_column()
    {
        var csv = "titel,omschrijving,startdatum,einddatum,branches\nx,y,2026-01-01,2026-02-01,Winkel\n";
        var result = VacancyCsvParser.Parse(csv);
        Assert.False(result.Succeeded);
        Assert.Contains("salaristabel_id", result.ErrorMessage);
    }

    [Fact]
    public void Parse_skips_blank_data_rows()
    {
        var tableId = Guid.NewGuid();
        var csv = $"titel,omschrijving,startdatum,einddatum,branches,salaristabel_id\n\nA,B,01-08-2026,31-12-2026,Horeca,{tableId}\n\n";
        var result = VacancyCsvParser.Parse(csv);
        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Single(result.Rows);
        Assert.Equal(3, result.Rows[0].RowNumber);
    }

    [Fact]
    public void Parse_keeps_multiline_quoted_description()
    {
        var tableId = Guid.NewGuid();
        var csv =
            "titel,omschrijving,startdatum,einddatum,branches,salaristabel_id\n" +
            $"Hulp,\"Regel 1\nRegel 2 met komma, ok\",2026-08-01,2026-12-31,Winkel,{tableId}\n";

        var result = VacancyCsvParser.Parse(csv);
        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Single(result.Rows);
        Assert.Equal("Regel 1\nRegel 2 met komma, ok", result.Rows[0].Fields[VacancyCsvSchema.Description]);
    }

    [Fact]
    public void Parse_fails_on_unclosed_quote()
    {
        var tableId = Guid.NewGuid();
        var csv =
            "titel,omschrijving,startdatum,einddatum,branches,salaristabel_id\n" +
            $"Kapot,\"niet afgesloten,2026-08-01,2026-12-31,Winkel,{tableId}\n";

        var result = VacancyCsvParser.Parse(csv);
        Assert.False(result.Succeeded);
        Assert.Contains("aanhalingsteken", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_accepts_semicolon_delimiter()
    {
        var tableId = Guid.NewGuid();
        var csv =
            "titel;omschrijving;startdatum;einddatum;branches;salaristabel_id\n" +
            $"Kassière;Leuke baan;2026-08-01;2026-12-31;Winkel;{tableId}\n";

        var result = VacancyCsvParser.Parse(csv);
        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal("Kassière", result.Rows[0].Fields[VacancyCsvSchema.Title]);
    }

    [Theory]
    [InlineData("2026-08-01")]
    [InlineData("01-08-2026")]
    [InlineData("1-8-2026")]
    public void TryParseDate_accepts_common_formats(string raw)
    {
        var date = VacancyCsvParser.TryParseDate(raw);
        Assert.NotNull(date);
        Assert.Equal(2026, date!.Value.Year);
        Assert.Equal(8, date.Value.Month);
        Assert.Equal(1, date.Value.Day);
    }
}

public class HtmlSanitizeImageTests
{
    [Fact]
    public void NormalizeImageInput_accepts_https_url()
    {
        var url = HtmlSanitize.NormalizeImageInput("https://cdn.example.com/job.png", out var error);
        Assert.Null(error);
        Assert.Equal("https://cdn.example.com/job.png", url);
    }

    [Fact]
    public void NormalizeImageInput_accepts_png_base64()
    {
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var b64 = Convert.ToBase64String(png);
        var result = HtmlSanitize.NormalizeImageInput(b64, out var error);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.StartsWith("data:image/png;base64,", result);
    }

    [Fact]
    public void NormalizeImageInput_rejects_garbage()
    {
        var result = HtmlSanitize.NormalizeImageInput("not-an-image", out var error);
        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void NormalizeMediaUrl_rejects_base64_for_video()
    {
        var png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        Assert.Null(HtmlSanitize.NormalizeMediaUrl(png));
        Assert.Null(HtmlSanitize.NormalizeMediaUrl("data:image/png;base64," + png));
    }

    [Fact]
    public void NormalizeImageInput_prefers_sniffed_mime_over_declared()
    {
        var pngPayload = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        var mismatched = $"data:image/jpeg;base64,{pngPayload}";
        var result = HtmlSanitize.NormalizeImageInput(mismatched, out var error);
        Assert.Null(error);
        Assert.StartsWith("data:image/png;base64,", result);
    }
}

public class TransportModeParserTests
{
    [Fact]
    public void ParseMany_combines_flags()
    {
        Assert.True(TransportModeParser.TryParseMany("Fiets;OV", out var mode, out var error));
        Assert.Null(error);
        Assert.True(mode.HasFlag(Jobsy.Core.Enums.TransportMode.Bike));
        Assert.True(mode.HasFlag(Jobsy.Core.Enums.TransportMode.PublicTransport));
    }

    [Fact]
    public void TryParseMany_rejects_unknown()
    {
        Assert.False(TransportModeParser.TryParseMany("Fiets;Teleport", out _, out var error));
        Assert.Contains("Teleport", error);
    }
}
