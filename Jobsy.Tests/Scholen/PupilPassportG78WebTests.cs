using Jobsy.Core.Enums;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Tests.Scholen;

public class PupilPassportG78WebTests
{
    private readonly PupilStoryRenderer _renderer = new();

    [Fact]
    public void Web_mapper_omits_world_fractions_and_percentages()
    {
        var model = BuildModel("Noor");
        var web = PupilPassportWebMapper.ToGroep78Web(model, enabled: true);
        var json = System.Text.Json.JsonSerializer.Serialize(web);
        Assert.DoesNotContain("%", json, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\d+/\d+", json);
        Assert.DoesNotContain("LeerlingPassport.Stamp", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_mapper_uses_phase_labels_not_numeric_route_index()
    {
        var model = BuildModel(null);
        var web = PupilPassportWebMapper.ToGroep78Web(model, enabled: true);
        Assert.NotNull(web.RouteSteps);
        Assert.All(web.RouteSteps!, s => Assert.False(int.TryParse(s.PhaseLabel, out _)));
    }

    [Fact]
    public void Mature_section_titles_in_copy()
    {
        Assert.Equal("Dit ben jij", PupilPassportCopy.Get("LeerlingPassport.Section.DitBenIk"));
        Assert.Equal("Dit vind ik leuk", PupilPassportCopy.Get("LeerlingPassport.Section.Likes"));
        Assert.DoesNotContain("!", PupilPassportCopy.Get("LeerlingPassport.Tip.dierenarts.2"), StringComparison.Ordinal);
    }

    private PupilPassportPdfModel BuildModel(string? name)
    {
        var result = FixtureResult();
        var progress = FixtureProgress(result.PupilCodeId);
        var code = new PupilCode
        {
            Id = result.PupilCodeId,
            Status = PupilCodeStatus.Completed,
            Result = result,
            Progress = progress
        };
        var cls = new SchoolClass
        {
            Name = "8A",
            Level = SchoolLevel.Groep78,
            Year = 8,
            QuestionSet = PupilQuestionSet.Groep78
        };
        return new PupilPassportPdfModelBuilder(_renderer).Build(
            code, cls, "OBS De Kompasroos (fictief)", "K7Q-M2P", name);
    }

    private static PupilProgress FixtureProgress(Guid codeId) => new()
    {
        PupilCodeId = codeId,
        LikesJson = """["dieren","tekenen","natuur"]""",
        DislikesJson = """["lang-stilzitten","druk-lawaai"]""",
        LikeOtherWord = "Paardrijden",
        DislikeOtherWord = "Spinnen",
        DreamJobKey = "dierenarts"
    };

    private static PupilResult FixtureResult()
    {
        var result = new PupilResult
        {
            PupilCodeId = Guid.NewGuid(),
            CompletedAtUtc = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc),
            HollandCode = "IS",
            DreamJobKey = "dierenarts"
        };
        var keys = PupilStoryTemplates.SelectKeys(result, ["dieren", "tekenen"], PupilQuestionSet.Groep78);
        result.StoryKeysJson = PupilStoryTemplates.Serialize(keys);
        return result;
    }
}
