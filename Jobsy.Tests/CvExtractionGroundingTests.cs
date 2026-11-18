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

    [Fact]
    public void Employer_is_kept_when_name_is_grounded_even_if_role_is_not()
    {
        var cv = """
            Werkervaring bij Jumbo Supermarkt als kassamedewerker.
            """;
        var extracted = new CvExtractedProfile(
            Employers: [new CandidateEmployerHistoryDto("Jumbo Supermarkt", Role: "Shift manager")]);

        var filtered = CvExtractionGrounding.Filter(extracted, cv);

        Assert.Single(filtered.Employers!);
        Assert.Equal("Jumbo Supermarkt", filtered.Employers![0].EmployerName);
    }

    [Fact]
    public void Typical_cv_text_yields_work_education_license_and_certificate_proposals()
    {
        var cv = """
            Werkervaring
            Jumbo Supermarkt – kassamedewerker (2019-2021)
            Opleiding: MBO Retail niveau 3, 2018
            Rijbewijs B
            Certificaat BHV, 2022
            """;
        var extracted = new CvExtractedProfile(
            Employers: [new CandidateEmployerHistoryDto("Jumbo Supermarkt", Role: "kassamedewerker")],
            Educations: ["MBO"],
            DrivingLicenses: ["B"],
            Certificates: [new CandidateCertificateDto("BHV", 2022)]);

        var filtered = CvExtractionGrounding.Filter(extracted, cv);
        var keys = CvExtractionFieldKeys.ListProposalKeys(filtered);

        Assert.Contains(CvExtractionFieldKeys.Employers, keys);
        Assert.Contains(CvExtractionFieldKeys.Educations, keys);
        Assert.Contains(CvExtractionFieldKeys.DrivingLicenses, keys);
        Assert.Contains(CvExtractionFieldKeys.Certificates, keys);
    }
}
