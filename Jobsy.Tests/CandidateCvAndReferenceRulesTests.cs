using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CandidateCvAndReferenceRulesTests
{
    [Fact]
    public void Hard_requirements_block_when_references_missing()
    {
        var error = ApplicationRequirementRules.ValidateHardRequirements(
            null,
            null,
            null,
            [],
            [],
            0,
            minimumReferences: 2,
            candidateReferenceCount: 1);

        Assert.Contains("recensie", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Hard_requirements_pass_when_enough_complete_references()
    {
        var error = ApplicationRequirementRules.ValidateHardRequirements(
            null,
            null,
            null,
            [],
            [],
            0,
            minimumReferences: 1,
            candidateReferenceCount: 1);

        Assert.Null(error);
    }

    [Fact]
    public void Hard_requirements_ignore_references_when_vacancy_asks_none()
    {
        var error = ApplicationRequirementRules.ValidateHardRequirements(
            null,
            null,
            null,
            [],
            [],
            0,
            minimumReferences: null,
            candidateReferenceCount: 0);

        Assert.Null(error);
    }

    [Fact]
    public void Reference_is_complete_only_with_all_fields()
    {
        Assert.False(CandidateReferenceRules.IsComplete("Bakkerij", "Anna", "anna@test.nl", null));
        Assert.False(CandidateReferenceRules.IsComplete("Bakkerij", "Anna", "niet-email", "0612345678"));
        Assert.True(CandidateReferenceRules.IsComplete("Bakkerij", "Anna Jansen", "anna@test.nl", "0612345678"));
    }

    [Fact]
    public void Cv_file_rules_accept_pdf_and_docx()
    {
        Assert.True(CandidateCvFileRules.TryNormalize("cv.pdf", "application/pdf", 1200, out var pdfName, out var pdfType, out var pdfError));
        Assert.Null(pdfError);
        Assert.Equal("cv.pdf", pdfName);
        Assert.Equal(CandidateCvFileRules.PdfContentType, pdfType);

        Assert.True(CandidateCvFileRules.TryNormalize("cv.docx", CandidateCvFileRules.DocxContentType, 1200, out _, out var docxType, out _));
        Assert.Equal(CandidateCvFileRules.DocxContentType, docxType);

        Assert.False(CandidateCvFileRules.TryNormalize("photo.png", "image/png", 1200, out _, out _, out var bad));
        Assert.Contains("PDF", bad);
        Assert.False(CandidateCvFileRules.TryNormalize("huge.pdf", "application/pdf", CandidateCvFileRules.MaxBytes + 1, out _, out _, out var size));
        Assert.Contains("5 MB", size);
    }

    [Fact]
    public void Cv_merge_fills_only_empty_fields()
    {
        var existing = new CandidatePreferencesDto(
            Roles: ["horeca"],
            MaxTravelMinutes: 20,
            PreferredTransport: "Fiets",
            AboutMe: "Al ingevuld",
            Employers: [new CandidateEmployerHistoryDto("Café Bestaan")],
            Educations: ["HAVO"],
            Certificates: []);

        var extracted = new CvExtractedProfile(
            FirstName: "Ada",
            LastName: "Kandidaat",
            PhoneNumber: "0611111111",
            AboutMe: "Niet overschrijven",
            DrivingLicenses: ["B"],
            Educations: ["MBO"],
            Roles: ["retail"],
            Employers: [new CandidateEmployerHistoryDto("Café Bestaan"), new CandidateEmployerHistoryDto("Supermarkt Nieuw")],
            Certificates: [new CandidateCertificateDto("BHV", 2024)]);

        var merged = CvProfileMerge.Apply(null, null, null, existing, extracted);

        Assert.Equal("Ada", merged.FirstName);
        Assert.Equal("Kandidaat", merged.LastName);
        Assert.Equal("0611111111", merged.PhoneNumber);
        Assert.Equal("Al ingevuld", merged.Preferences.AboutMe);
        Assert.Contains("B", merged.Preferences.DrivingLicenses!);
        Assert.Contains("MBO", merged.Preferences.Educations!);
        Assert.Contains("retail", merged.Preferences.Roles);
        Assert.Contains("horeca", merged.Preferences.Roles);
        Assert.Equal(2, merged.Preferences.Employers!.Count);
        Assert.Contains(merged.FilledFields, f => f == "voornaam");
        Assert.DoesNotContain(merged.FilledFields, f => f == "over mij");
        Assert.Contains(merged.FilledFields, f => f == "werkervaring");
    }
}
