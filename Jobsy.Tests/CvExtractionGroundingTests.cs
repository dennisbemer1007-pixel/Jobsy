using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CvExtractionGroundingTests
{
    [Fact]
    public void Shell_employer_not_in_cv_text_is_removed()
    {
        var cv = """
            Werkervaring
            Bakkerij De Westhoek, medewerker, 2022-2024
            """;
        var extracted = new CvExtractedProfile(
            Employers: [new CandidateEmployerHistoryDto("Shell BV"), new CandidateEmployerHistoryDto("Bakkerij De Westhoek")],
            AboutMe: "Ik ben enthousiast");

        var filtered = CvExtractionGrounding.Filter(extracted, cv);

        Assert.Null(filtered.AboutMe);
        Assert.Single(filtered.Employers!);
        Assert.Equal("Bakkerij De Westhoek", filtered.Employers![0].EmployerName);
    }

    [Fact]
    public void Certificate_must_appear_in_source_text()
    {
        var cv = "Certificaat: BHV 2023";
        var extracted = new CvExtractedProfile(
            Certificates: [new CandidateCertificateDto("VCA"), new CandidateCertificateDto("BHV", 2023)]);

        var filtered = CvExtractionGrounding.Filter(extracted, cv);

        Assert.Single(filtered.Certificates!);
        Assert.Equal("BHV", filtered.Certificates![0].Name);
    }
}
