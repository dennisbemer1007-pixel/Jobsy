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
        var pdf = LoadReportLabFixture();
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

        var harvestedOnly = CvExtractionFactsGuard.Apply(
            new CvExtractedProfile(
                Roles: ["opensource"],
                Employers:
                [
                    new("Huisartsenpraktijk De Linde", "2019 - 2024 Verpleegkundige"),
                    new("De Linde", "Verpleegkundige"),
                    new("Dierenasiel Scheveningen", "Vrijwilliger")
                ]),
            text);
        var harvestedKeys = CvExtractionFieldKeys.ListProposalKeys(harvestedOnly);
        Assert.Contains(CvExtractionFieldKeys.Employers, harvestedKeys);
        Assert.Equal(2, harvestedOnly.Employers!.Count);

        var deLinde = harvestedOnly.Employers!.Single(e =>
            e.EmployerName.Contains("De Linde", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Verpleegkundige", deLinde.Role);
        Assert.Equal("Huisartsenpraktijk De Linde", deLinde.EmployerName);
        Assert.Equal("2024-12", deLinde.EndMonth);
        Assert.Equal("2019-01", deLinde.StartMonth);
        Assert.DoesNotContain("2019", deLinde.Role ?? string.Empty, StringComparison.Ordinal);

        var dieren = harvestedOnly.Employers!.Single(e =>
            e.EmployerName.Contains("Dierenasiel", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Vrijwilliger", dieren.Role);
        Assert.Equal("2022-01", dieren.StartMonth);
        Assert.DoesNotContain("2014", $"{dieren.StartMonth}{dieren.EndMonth}", StringComparison.Ordinal);
        Assert.DoesNotContain("2018", $"{dieren.StartMonth}{dieren.EndMonth}", StringComparison.Ordinal);
        Assert.False(deLinde.IsCurrent == true);
    }

    [Fact]
    public void ReportLab_fixture_education_certificates_deduped_and_grounded()
    {
        var pdf = LoadReportLabFixture();
        var extractor = new CvTextExtractor();
        var text = extractor.Extract(pdf, "application/pdf", "cv-test.pdf");
        var harvestedOnly = CvExtractionFactsGuard.Apply(new CvExtractedProfile(), text);

        var education = harvestedOnly.Educations!.Single(e => e.Contains("Verpleegkunde", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("MBO Verpleegkunde niveau 4", education);
        Assert.DoesNotContain("2014", education, StringComparison.Ordinal);
        Assert.DoesNotContain("2018", education, StringComparison.Ordinal);
        Assert.Equal("ROC Mondriaan", harvestedOnly.EducationSchool);
        Assert.Equal(1, harvestedOnly.Certificates!.Count(c =>
            c.Name.Contains("BHV", StringComparison.OrdinalIgnoreCase)));
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

    private static byte[] LoadReportLabFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Cv", "ReportLabDeLindeCv.pdf");
        Assert.True(File.Exists(path), path);
        return File.ReadAllBytes(path);
    }
}
