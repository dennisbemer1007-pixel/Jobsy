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
        var model = BuildModel("Alex");
        model = model with { Language = "en" };
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
        var model = BuildModel("Alex");
        model = model with { Language = "en" };
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
    }

    private PupilPassportPdfModel BuildModel(string? name)
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
            code, cls, "Demo College Lobsy", "K7Q-M2P", name, language: "en");
    }
}
