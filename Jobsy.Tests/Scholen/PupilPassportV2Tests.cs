using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Infrastructure.Scholen.Passport;
using QuestPDF.Infrastructure;

namespace Jobsy.Tests.Scholen;

public class PupilPassportV2Tests
{
    private readonly PupilStoryRenderer _renderer = new();

    static PupilPassportV2Tests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Flag_off_uses_legacy_pdf_copy_constants()
    {
        var legacy = new PupilReportPdfService();
        var bytes = legacy.Render(MinimalLegacyModel());
        Assert.True(bytes.Length > 400);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes.AsSpan(0, 4)));
    }

    [Fact]
    public void Groep78_passport_has_no_stored_name_and_privacy_footer()
    {
        var model = BuildGroep78Model("Noor");
        var pdf = new PupilPassportPdfService().Render(model);
        Assert.True(pdf.Length > 2000);
        var src = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Infrastructure", "Scholen", "Passport", "PupilPassportGroep78PdfRenderer.cs"));
        Assert.Contains("Lobsy bewaart geen namen", PupilPassportCopy.Get("LeerlingPassport.Footer.G78"), StringComparison.Ordinal);
        Assert.Contains("Geen cijfer", model.FooterPrivacy, StringComparison.Ordinal);
        Assert.Contains("geen vacatures", PupilPassportCopy.Get("LeerlingPassport.Cards.Sub"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Noor", model.DisplayNameOnPdf);
        Assert.DoesNotContain("Blob", src, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vo_passport_side_jobs_are_catalog_not_vacancies()
    {
        var model = BuildVoModel(null);
        Assert.NotNull(model.SideJobTypes);
        Assert.All(model.SideJobTypes!, j => Assert.DoesNotContain("http", j.Why, StringComparison.OrdinalIgnoreCase));
        var pdf = new PupilPassportPdfService().Render(model);
        Assert.True(pdf.Length > 2000);
    }

    [Fact]
    public void Display_name_query_is_not_in_progress_entity()
    {
        var props = typeof(PupilProgress).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain(props, n => Regex.IsMatch(n, "(?i)display.?name|pdf.?name|passport.?name"));
    }

    [Fact]
    public void Writes_demo_pdfs_to_agent_store()
    {
        var outDir = "/cursor/stores/self/leerlingpaspoort";
        Directory.CreateDirectory(outDir);
        var svc = new PupilPassportPdfService();

        var groepNamed = svc.Render(BuildGroep78Model("Noor"));
        var groepSparse = svc.Render(BuildGroep78Model(null));
        var voNamed = svc.Render(BuildVoModel("Sara Bakker"));
        var voSparse = svc.Render(BuildVoModel(null));

        File.WriteAllBytes(Path.Combine(outDir, "noor-groep8.pdf"), groepNamed);
        File.WriteAllBytes(Path.Combine(outDir, "groep8-sparse.pdf"), groepSparse);
        File.WriteAllBytes(Path.Combine(outDir, "sara-havo4.pdf"), voNamed);
        File.WriteAllBytes(Path.Combine(outDir, "vo-sparse.pdf"), voSparse);

        ExportPng(BuildGroep78Model("Noor"), Path.Combine(outDir, "noor-groep8.png"));
        ExportPng(BuildVoModel("Sara Bakker"), Path.Combine(outDir, "sara-havo4.png"));
        ExportPng(BuildGroep78Model(null), Path.Combine(outDir, "groep8-sparse.png"));
        ExportPng(BuildVoModel(null), Path.Combine(outDir, "vo-sparse.png"));
    }

    private void ExportPng(PupilPassportPdfModel model, string pngPath)
    {
        var png = new PupilPassportPdfService().RenderFirstPagePng(model, 150);
        File.WriteAllBytes(pngPath, png);
    }

    private PupilPassportPdfModel BuildGroep78Model(string? name)
    {
        var result = FixtureResult();
        var progress = FixtureProgress(result.PupilCodeId);
        var builder = new PupilPassportPdfModelBuilder(_renderer);
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
        return builder.Build(code, cls, "OBS De Kompasroos (fictief)", "K7Q-M2P", name);
    }

    private PupilPassportPdfModel BuildVoModel(string? name)
    {
        var result = FixtureResult();
        result.HollandCode = "SI";
        var progress = FixtureProgress(result.PupilCodeId);
        progress.VoFavoriteSubjectsJson = """["biologie","tekenen"]""";
        progress.VoProfileKey = "natuur-gezondheid";
        progress.VoSideJobAvailabilityJson = """["za","max-8"]""";
        progress.VoTravelModeKey = "fiets";
        var builder = new PupilPassportPdfModelBuilder(_renderer);
        var code = new PupilCode
        {
            Id = result.PupilCodeId,
            Status = PupilCodeStatus.Completed,
            Result = result,
            Progress = progress
        };
        var cls = new SchoolClass
        {
            Name = "4B",
            Level = SchoolLevel.Havo,
            Year = 4,
            QuestionSet = PupilQuestionSet.Vo
        };
        return builder.Build(code, cls, "Amersfoort (school)", "K7Q-M2P", name);
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

    private static PupilReportPdfModel MinimalLegacyModel()
        => new(
            "School",
            "8A",
            "K7Q-M2P",
            new DateOnly(2026, 9, 1),
            "Story",
            [],
            [],
            [],
            [],
            null,
            [],
            null);

    private static string RepoRoot()
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
