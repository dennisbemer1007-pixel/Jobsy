using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public sealed class CvPdfExtractionPipelineTests
{
    [Fact]
    public void Realistic_reportlab_pdf_yields_body_fields_not_producer_metadata()
    {
        var pdf = BuildReportLabStyleCvPdf();
        var extractor = new CvTextExtractor();
        var text = extractor.Extract(pdf, "application/pdf", "cv-test.pdf");

        Assert.DoesNotContain("ReportLab PDF Library", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("De Linde", text, StringComparison.Ordinal);
        Assert.Contains("Dierenasiel", text, StringComparison.Ordinal);
        Assert.Contains("MBO", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ROC Mondriaan", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rijbewijs", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BHV", text, StringComparison.OrdinalIgnoreCase);

        var extracted = new CvExtractedProfile(
            Employers: [new("ReportLab PDF Library"), new("De Linde")],
            Educations: ["MBO"],
            DrivingLicenses: ["B"],
            Certificates: [new("ReportLab PDF Library"), new("BHV", 2022)]);

        var filtered = CvExtractionGrounding.Filter(extracted, text);
        var keys = CvExtractionFieldKeys.ListProposalKeys(filtered);

        Assert.Contains(CvExtractionFieldKeys.Employers, keys);
        Assert.Contains(CvExtractionFieldKeys.Educations, keys);
        Assert.Contains(CvExtractionFieldKeys.DrivingLicenses, keys);
        Assert.Contains(CvExtractionFieldKeys.Certificates, keys);
        Assert.DoesNotContain(filtered.Certificates!, c => c.Name.Contains("ReportLab", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(filtered.Certificates!, c => c.Name.Equals("BHV", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Metadata_strings_never_become_suggestions()
    {
        const string cv = "Certificaat BHV 2022 ReportLab PDF Library in file tail";
        var extracted = new CvExtractedProfile(
            Certificates:
            [
                new("ReportLab PDF Library"),
                new("Adobe PDF Library"),
                new("BHV", 2022)
            ]);

        var filtered = CvExtractionGrounding.Filter(extracted, cv);

        Assert.Single(filtered.Certificates!);
        Assert.Equal("BHV", filtered.Certificates![0].Name);
        Assert.True(CvPdfMetadataGuard.IsPdfMetadataNoise("ReportLab PDF Library"));
    }

    private static byte[] BuildReportLabStyleCvPdf()
    {
        var pdf = """
            %PDF-1.4
            1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
            2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
            3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R/Contents 4 0 R>>endobj
            4 0 obj<</Length 220>>stream
            (Werkervaring De Linde, Dierenasiel)
            (Opleiding MBO ROC Mondriaan 2018)
            (Rijbewijs B)
            (Certificaat BHV 2022)
            endstream
            endobj
            5 0 obj<</Producer (ReportLab PDF Library)/Creator (ReportLab PDF Library)>>endobj
            trailer<</Root 1 0 R/Info 5 0 R>>
            %%EOF
            """;
        return Encoding.ASCII.GetBytes(pdf);
    }
}
