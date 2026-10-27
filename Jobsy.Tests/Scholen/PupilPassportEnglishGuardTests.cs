using Jobsy.Core.Enums;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Tests.Scholen;

public sealed class PupilPassportEnglishGuardTests
{
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
