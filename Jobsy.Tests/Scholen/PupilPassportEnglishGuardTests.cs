using Jobsy.Core.Enums;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Infrastructure.Scholen.Passport;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace Jobsy.Tests.Scholen;

public sealed class PupilPassportEnglishGuardTests
{
    static PupilPassportEnglishGuardTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private readonly PupilStoryRenderer _renderer = new();

    [Fact]
    public void English_g78_passport_web_has_no_obvious_dutch_leaks()
    {
        var model = BuildModel("Alex", "en");
        var web = PupilPassportWebMapper.ToGroep78Web(model, enabled: true);
        var json = System.Text.Json.JsonSerializer.Serialize(web);

        Assert.DoesNotContain("LeerlingPassport.", json, StringComparison.Ordinal);
        Assert.DoesNotContain("LeerlingStory.", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Groep ", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Check je droombaan", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Group ", json, StringComparison.Ordinal);
    }

    [Fact]
    public void English_g78_passport_pdf_has_no_dutch_leaks_or_raw_keys()
    {
        var model = BuildModel("Alex", "en");
        var bytes = new PupilPassportPdfService().Render(model);
        Assert.True(bytes.Length > 2000);
        using var doc = PdfDocument.Open(bytes);
        var text = string.Join(' ', doc.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));

        Assert.DoesNotContain("LeerlingPassport.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Dit heb je al", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Wat heb je nodig", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("groep 8", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("What you already have", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MY DNA PASSPORT", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Koraalrif", text, StringComparison.Ordinal);
        Assert.DoesNotContain("pagina 1/1", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\u2013", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\u2014", text, StringComparison.Ordinal);
        Assert.Contains("Chef", text, StringComparison.Ordinal);
        Assert.Contains("Coral reef", text, StringComparison.OrdinalIgnoreCase);

        foreach (var strength in model.Strengths)
        {
            Assert.Contains(strength.Title, text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(strength.Subtitle, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Dutch_g78_passport_pdf_has_no_raw_keys_or_english_leaks()
    {
        var model = BuildModel("Alex", "nl");
        var bytes = new PupilPassportPdfService().Render(model);
        using var doc = PdfDocument.Open(bytes);
        var text = string.Join(' ', doc.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));

        Assert.DoesNotContain("LeerlingPassport.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("LeerlingDroom.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MY DNA PASSPORT", text, StringComparison.Ordinal);
        Assert.Contains("Dit heb je al", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Koraal", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pagina 1/1", text, StringComparison.OrdinalIgnoreCase);

        foreach (var strength in model.Strengths)
        {
            Assert.Contains(strength.Title, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private PupilPassportPdfModel BuildModel(string? name, string language = "nl")
    {
        var result = new PupilResult
        {
            PupilCodeId = Guid.NewGuid(),
            CompletedAtUtc = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc),
            HollandCode = "IS",
            DreamJobKey = "kok",
            StoryKeysJson = PupilStoryTemplates.Serialize(
                PupilStoryTemplates.SelectKeys(
                    new PupilResult { HollandCode = "IS" },
                    ["techniek", "puzzels"],
                    PupilQuestionSet.Groep78))
        };
        var progress = new PupilProgress
        {
            PupilCodeId = result.PupilCodeId,
            LikesJson = """["techniek","puzzels"]""",
            DislikesJson = """["druk-lawaai"]""",
            DreamJobKey = "kok"
        };
        var code = new PupilCode
        {
            Id = result.PupilCodeId,
            Status = PupilCodeStatus.Completed,
            Result = result,
            Progress = progress
        };
        var cls = new SchoolClass
        {
            Name = "8B",
            Level = SchoolLevel.Groep78,
            Year = 8,
            QuestionSet = PupilQuestionSet.Groep78
        };
        return new PupilPassportPdfModelBuilder(_renderer).Build(
            code, cls, "Demo College Lobsy", "K7Q-M2P", name, language: language);
    }
}
