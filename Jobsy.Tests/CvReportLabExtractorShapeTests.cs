using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public sealed class CvReportLabExtractorShapeTests
{
    [Fact]
    public void PdfPig_extractor_preserves_year_range_before_role()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Cv", "ReportLabDeLindeCv.pdf");
        var text = new CvTextExtractor().Extract(File.ReadAllBytes(path), "application/pdf", "cv.pdf");
        Assert.Contains("2019", text, StringComparison.Ordinal);
        Assert.Contains("2024", text, StringComparison.Ordinal);
        Assert.Contains("2019 - 2024", text.Replace("  ", " ", StringComparison.Ordinal), StringComparison.Ordinal);
        var start = text.IndexOf("Werkervaring", StringComparison.OrdinalIgnoreCase);
        var end = text.IndexOf("Opleiding", start, StringComparison.OrdinalIgnoreCase);
        var work = text[start..end];
        Assert.Contains("2024 Verpleegkundige", work, StringComparison.OrdinalIgnoreCase);
    }
}