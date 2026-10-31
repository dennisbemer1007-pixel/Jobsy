using System.Text;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Infrastructure.Scholen.Passport;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace Jobsy.Tests.Scholen;

public class ExportG78PassportSampleTests
{
    static ExportG78PassportSampleTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Export_g78_passport_sample_pdf_artifact()
    {
        var renderer = new PupilStoryRenderer();
        var builder = new PupilPassportPdfModelBuilder(renderer);
        var result = new PupilResult
        {
            PupilCodeId = Guid.NewGuid(),
            CompletedAtUtc = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc),
            HollandCode = "RI",
            DreamJobKey = "kok",
            RiasecScoresJson =
                """{"Realistic":86,"Investigative":74,"Artistic":48,"Social":61,"Enterprising":35,"Conventional":30}""",
            CultureScoresJson =
                """{"Collaboration":35,"PeopleFirst":75,"Informal":30,"Flexibility":65}""",
            ValuesScoresJson = "{}",
            CompetenceScoresJson = "{}",
            TopValue = "Connection"
        };
        var keys = PupilStoryTemplates.SelectKeys(result, ["sport", "natuur", "techniek"], PupilQuestionSet.Groep78);
        result.StoryKeysJson = PupilStoryTemplates.Serialize(keys);

        var vakMap = new Dictionary<string, object>();
        foreach (var key in PupilVakScores.AllSubjectKeys)
        {
            vakMap[key] = new { leuk = 4, goed = 4 };
        }

        vakMap["natuur-techniek"] = new { leuk = 5, goed = 5 };
        vakMap["rekenen"] = new { leuk = 5, goed = 5 };
        vakMap["gym"] = new { leuk = 5, goed = 5 };
        vakMap["spelling"] = new { leuk = 3, goed = 5 };

        var progress = new PupilProgress
        {
            PupilCodeId = result.PupilCodeId,
            LikesJson = """["sport","natuur","techniek"]""",
            DislikesJson = """["lang-stilzitten"]""",
            DreamJobKey = "kok",
            VakScoresJson = System.Text.Json.JsonSerializer.Serialize(vakMap)
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

        var model = builder.Build(code, cls, "Demo OBS", "28A-Z7K", null);
        model = model with
        {
            MirrorBody = PupilG78MirrorTemplate.Render(result, progress, PupilVakScores.Parse(progress.VakScoresJson), "nl")
        };

        var pdf = new PupilPassportPdfService().Render(model);
        Assert.True(pdf.Length > 4000);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.AsSpan(0, 4)));

        using var doc = PdfDocument.Open(pdf);
        Assert.Equal(5, doc.NumberOfPages);

        Directory.CreateDirectory("/opt/cursor/artifacts");
        File.WriteAllBytes("/opt/cursor/artifacts/g78-paspoort-bijlagen-sample.pdf", pdf);
    }
}
