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
        // 1x1 PNG
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
}
