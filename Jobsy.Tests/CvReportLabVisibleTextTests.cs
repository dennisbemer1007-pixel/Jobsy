using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public sealed class CvReportLabVisibleTextTests
{
    [Fact]
    public void Glued_year_role_token_parses_range_end()
    {
        const string cv = """
            Werkervaring
            2019 - 2024Verpleegkundige, Huisartsenpraktijk De Linde
            2022 Vrijwilliger, Dierenasiel Scheveningen
            Opleiding
            MBO Verpleegkunde niveau 4
            """;
        var filtered = CvExtractionGrounding.Filter(new CvExtractedProfile(), cv);
        var deLinde = filtered.Employers!.Single(e => e.EmployerName.Contains("De Linde", StringComparison.Ordinal));
        Assert.Equal("2024-12", deLinde.EndMonth);
    }

    [Fact]
    public void Visible_text_harvests_two_employers_without_ai()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Cv", "ReportLabDeLindeCv.pdf");
        var text = new CvTextExtractor().Extract(File.ReadAllBytes(path), "application/pdf", "cv.pdf");
        Assert.Contains("Werkervaring", text, StringComparison.OrdinalIgnoreCase);
        var filtered = CvExtractionGrounding.Filter(new CvExtractedProfile(), text);
        var employers = filtered.Employers ?? [];

        Assert.Equal(2, employers.Count);
        var deLinde = employers.Single(e => e.EmployerName.Contains("De Linde", StringComparison.Ordinal));
        Assert.NotNull(deLinde.StartMonth);
        Assert.Equal("2024-12", deLinde.EndMonth);
        Assert.Equal("2019-01", deLinde.StartMonth);
        Assert.Equal("2022-01", employers.Single(e => e.EmployerName.Contains("Dierenasiel", StringComparison.Ordinal)).StartMonth);
    }
}