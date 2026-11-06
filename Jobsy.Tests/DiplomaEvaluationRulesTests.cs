using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

public class DiplomaEvaluationRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 3);

    [Fact]
    public void Normalize_keeps_the_candidates_wording_and_does_not_copy_the_pick_list()
    {
        var ok = DiplomaEvaluationRules.TryNormalize(
            "Lisans İstanbul",
            "nuffic",
            "should be dropped",
            "  vergelijkbaar met hbo-bachelor, niet gelijkwaardig  ",
            "hbo-bachelor",
            new DateOnly(2024, 6, 1),
            "  IDW-99  ",
            Today,
            out var value,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(value);
        Assert.Equal("vergelijkbaar met hbo-bachelor, niet gelijkwaardig", value!.EquivalentLevelText);
        Assert.Equal("hbo-bachelor", value.EquivalentLevelCode);
        Assert.NotEqual(value.EquivalentLevelCode, value.EquivalentLevelText);
        Assert.Equal(DiplomaEvaluationRules.BodyNuffic, value.IssuingBody);
        Assert.Null(value.IssuingBodyOther);
        Assert.Equal("IDW-99", value.ReferenceNumber);
        Assert.False(DiplomaEvaluationRules.MayInfluenceMatchOrEducationLevel);
        Assert.False(EducationLevelLabels.CandidateMeetsRequirement([], "HBO"));
    }

    [Fact]
    public void Empty_level_is_rejected_even_when_a_pick_list_code_is_sent()
    {
        var ok = DiplomaEvaluationRules.TryNormalize(
            null, "sbb", null, "  ", "mbo-4", new DateOnly(2020, 1, 1), "SBB-1", Today, out _, out var error);
        Assert.False(ok);
        Assert.Equal("level_required", error);
    }

    [Fact]
    public void Other_body_requires_a_name_and_future_dates_fail()
    {
        Assert.False(DiplomaEvaluationRules.TryNormalize(
            null, "other", "  ", "mbo 3", null, new DateOnly(2020, 1, 1), "X", Today, out _, out var missing));
        Assert.Equal("other_name_required", missing);

        Assert.False(DiplomaEvaluationRules.TryNormalize(
            null, "other", "Gemeente Delft", "mbo 3", null, Today.AddDays(1), "X", Today, out _, out var future));
        Assert.Equal("date_future", future);
    }

    [Fact]
    public void Shared_fact_and_snapshot_omit_the_document()
    {
        var row = new CandidateDiplomaEvaluation
        {
            Id = Guid.NewGuid(),
            DiplomaTitle = "Lisans",
            IssuingBody = DiplomaEvaluationRules.BodyNuffic,
            EquivalentLevelText = "hbo-bachelor volgens de tekst",
            EquivalentLevelCode = "hbo-bachelor",
            EvaluationDate = new DateOnly(2024, 6, 1),
            ReferenceNumber = "IDW-99",
            DocumentFileName = "geheim-waardering.pdf",
            DocumentContentType = "application/pdf",
            DocumentContent = "%PDF-1.4"u8.ToArray(),
            DocumentSizeBytes = 8
        };

        var shared = DiplomaEvaluationRules.ToSharedFact(row);
        var json = JsonSerializer.Serialize(shared);
        Assert.DoesNotContain("geheim-waardering.pdf", json, StringComparison.Ordinal);
        Assert.DoesNotContain("DocumentFileName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DocumentContent", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%PDF", json, StringComparison.Ordinal);
        Assert.Equal("volgens waardering van Nuffic/SBB", DiplomaEvaluationRules.DutchAttribution(shared.IssuingBody, shared.IssuingBodyOther));

        var snapshot = DiplomaEvaluationRules.SerializeSnapshot([shared]);
        Assert.NotNull(snapshot);
        Assert.DoesNotContain("geheim", snapshot, StringComparison.Ordinal);
        var parsed = DiplomaEvaluationRules.ParseSnapshot(snapshot);
        Assert.Equal("hbo-bachelor volgens de tekst", parsed[0].EquivalentLevelText);
    }

    [Fact]
    public void Document_accepts_pdf_and_png_and_rejects_a_renamed_text_file()
    {
        var pdf = "%PDF-1.4\n"u8.ToArray();
        Assert.True(DiplomaEvaluationRules.TryNormalizeDocument("scan.pdf", "application/pdf", pdf, out var name, out var type, out _));
        Assert.Equal("application/pdf", type);
        Assert.EndsWith(".pdf", name, StringComparison.Ordinal);

        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 };
        Assert.True(DiplomaEvaluationRules.TryNormalizeDocument("foto.png", "image/png", png, out _, out var pngType, out _));
        Assert.Equal("image/png", pngType);

        var text = "hello"u8.ToArray();
        Assert.False(DiplomaEvaluationRules.TryNormalizeDocument("waardering.pdf", "application/pdf", text, out _, out _, out var error));
        Assert.Equal("file_type", error);
    }

    [Fact]
    public async Task Pdf_prints_the_stated_level_and_attribution_without_the_file_name()
    {
        var prefs = new CandidatePreferencesDto(["horeca"], 30, "Fiets", Educations: ["MBO"]);
        var model = LobsyCvModelFactory.FromLiveProfile(
            "Ada",
            "ada@test.local",
            null,
            false,
            prefs,
            null,
            null,
            DateTime.UtcNow,
            diplomaEvaluations:
            [
                new DiplomaEvaluationSharedFact(
                    "Lisans",
                    DiplomaEvaluationRules.BodySbb,
                    null,
                    "vergelijkbaar met mbo-4",
                    "mbo-4",
                    new DateOnly(2023, 4, 2),
                    "SBB-44")
            ]);

        Assert.Equal("vergelijkbaar met mbo-4", model.DiplomaEvaluations![0].EquivalentLevelText);
        Assert.Equal("volgens waardering van Nuffic/SBB", model.DiplomaEvaluations[0].Attribution);
        Assert.Equal(["MBO"], model.Educations);

        var pdf = await new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages()).RenderAsync(model);
        using var doc = PdfDocument.Open(pdf);
        var text = string.Join('\n', doc.GetPages().Select(p => p.Text));
        Assert.Contains("vergelijkbaar met mbo-4", text, StringComparison.Ordinal);
        Assert.Contains("volgens waardering van Nuffic/SBB", text, StringComparison.Ordinal);
        Assert.Contains("SBB-44", text, StringComparison.Ordinal);
        Assert.DoesNotContain("geheim", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".pdf", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Match_rules_do_not_reference_diploma_evaluations()
    {
        var root = FindRepoRoot();
        foreach (var relative in new[]
                 {
                     "Jobsy.Core/Rules/EducationLevelLabels.cs",
                     "Jobsy.Core/Rules/ProfileVacancyMatchCalculator.cs",
                     "Jobsy.Core/Rules/MatchProfileCompleteness.cs"
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.DoesNotContain("DiplomaEvaluation", text, StringComparison.Ordinal);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Jobsy.sln.");
    }

    private sealed class FakeCompanySettings : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "Test", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(PlatformCompanyUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);

        public byte[] GetBrandLogoPng() => [];

        public byte[] GetBrandWatermarkPng() => [];
    }

    private sealed class FakeMapImages : ICandidateMapImageService
    {
        public Task<byte[]?> RenderAsync(
            double latitude, double longitude, int width = 640, int height = 280, int zoom = 15,
            byte[]? markerLogoPng = null, CancellationToken cancellationToken = default)
            => Task.FromResult<byte[]?>(null);

        public Task<byte[]?> RenderWorkplaceReachAsync(
            double latitude, double longitude, double radiusMeters, int width = 640, int height = 280,
            byte[]? markerLogoPng = null, CancellationToken cancellationToken = default)
            => Task.FromResult<byte[]?>(null);
    }
}
