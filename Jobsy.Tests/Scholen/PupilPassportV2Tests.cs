using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Infrastructure.Scholen.Passport;
using Jobsy.Infrastructure.Services.Passport;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

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
        Assert.Contains("Lobsy bewaart geen namen", PupilPassportCopy.Get("LeerlingPassport.Footer.G78"), StringComparison.Ordinal);
        Assert.Contains("Geen cijfer", model.FooterPrivacy, StringComparison.Ordinal);
        Assert.Equal("Noor", model.DisplayNameOnPdf);
    }

    [Fact]
    public void Vo_side_job_availability_splits_days_and_hours()
    {
        var model = BuildVoModel(null);
        Assert.Equal("Za + wo-middag", model.SideJobAvailabilityLine);
        Assert.Equal(PupilPassportCopy.Get("LeerlingPassport.Vo.Info.SideJobHours"), model.SideJobInfoSubline);
    }

    [Fact]
    public void Vo_full_model_carries_own_words()
    {
        var model = BuildVoModel("Sara Bakker");
        Assert.Contains("zorg", model.OwnWords ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vo_passport_resolves_side_job_copy_not_raw_keys()
    {
        var model = BuildVoModel(null);
        Assert.NotNull(model.SideJobTypes);
        Assert.All(model.SideJobTypes!, j =>
        {
            Assert.DoesNotContain("LeerlingPassport.", j.Title, StringComparison.Ordinal);
            Assert.DoesNotContain("LeerlingPassport.", j.Subtitle, StringComparison.Ordinal);
            Assert.DoesNotContain("LeerlingPassport.", j.Why, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Vo_sectors_are_unique_titles()
    {
        var model = BuildVoModel(null);
        Assert.NotNull(model.SectorFits);
        var titles = model.SectorFits!.Select(s => s.Title).ToList();
        Assert.Equal(titles.Count, titles.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(model.SectorFits, s => Assert.DoesNotContain("Past bij hoe jij bent", string.Join(' ', s.Bullets), StringComparison.Ordinal));
    }

    [Fact]
    public void Pdf_renderers_contain_no_emoji_characters()
    {
        var dir = Path.Combine(RepoRoot(), "Jobsy.Infrastructure", "Scholen", "Passport");
        foreach (var file in Directory.GetFiles(dir, "PupilPassport*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.False(ContainsEmoji(text), $"Emoji found in {Path.GetFileName(file)}");
        }
    }

    [Fact]
    public void Copy_catalog_has_no_missing_side_job_keys()
    {
        foreach (var key in PupilVoSideJobCatalog.AllKeysForTests())
        {
            Assert.DoesNotContain("LeerlingPassport.", PupilPassportCopy.Get($"LeerlingPassport.Vo.SideJob.{key}.Title"), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Display_name_query_is_not_in_progress_entity()
    {
        var props = typeof(PupilProgress).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain(props, n => Regex.IsMatch(n, "(?i)display.?name|pdf.?name|passport.?name"));
    }

    [Fact]
    public void Student_yara_demo_renders_two_page_pdf()
    {
        var svc = StudentPdfService();
        var pdf = svc.RenderDemoPdf(sparse: false);
        Assert.True(pdf.Length > 8000);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.AsSpan(0, 4)));
        using var doc = PdfDocument.Open(pdf);
        Assert.Equal(2, doc.NumberOfPages);
    }

    [Fact]
    public void Student_yara_model_uses_student_layout_and_nursing_copy()
    {
        var model = PassportPdfModelBuilder.Build(StudentPassportDemoSeed.YaraFacts());
        Assert.True(model.StudentLayout);
        Assert.Equal(3, model.StageVacancyMatches.Count);
        Assert.True(model.SingleSectorFullWidth);
        Assert.Contains("Zorgzaam en rustig", model.Tagline ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("Samen & aardig", model.Tagline ?? "", StringComparison.Ordinal);
        Assert.Contains("Hbo Verpleegkunde", model.Chips[0].Text, StringComparison.Ordinal);
        Assert.Equal(4, model.Direction.Count);
        Assert.DoesNotContain("vaste ploeg", string.Join(' ', model.BestOutTips), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dit kwam naar voren in je test", string.Join(' ', model.StrengthCards.Select(c => c.Subtitle)), StringComparison.Ordinal);
    }

    [Fact]
    public void Writes_demo_artifacts_to_v3_store()
    {
        var outDir = "/cursor/stores/self/leerlingpaspoort/v3";
        try
        {
            Directory.CreateDirectory(outDir);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return;
        }
        var pupilSvc = new PupilPassportPdfService();
        var studentSvc = StudentPdfService();

        File.WriteAllBytes(Path.Combine(outDir, "noor-groep8.pdf"), pupilSvc.Render(BuildGroep78Model("Noor")));
        File.WriteAllBytes(Path.Combine(outDir, "groep8-sparse.pdf"), pupilSvc.Render(BuildGroep78Model(null)));
        File.WriteAllBytes(Path.Combine(outDir, "sara-havo4.pdf"), pupilSvc.Render(BuildVoModel("Sara Bakker")));
        File.WriteAllBytes(Path.Combine(outDir, "vo-sparse.pdf"), pupilSvc.Render(BuildVoModel(null)));

        ExportPng(pupilSvc, BuildGroep78Model("Noor"), Path.Combine(outDir, "noor-groep8.png"));
        ExportPng(pupilSvc, BuildGroep78Model(null), Path.Combine(outDir, "groep8-sparse.png"));
        ExportPng(pupilSvc, BuildVoModel("Sara Bakker"), Path.Combine(outDir, "sara-havo4.png"));
        ExportPng(pupilSvc, BuildVoModel(null), Path.Combine(outDir, "vo-sparse.png"));

        File.WriteAllBytes(Path.Combine(outDir, "yara-hbo.pdf"), studentSvc.RenderDemoPdf(false));
        File.WriteAllBytes(Path.Combine(outDir, "yara-hbo-sparse.pdf"), studentSvc.RenderDemoPdf(true));
        File.WriteAllBytes(Path.Combine(outDir, "yara-hbo-p1.png"), studentSvc.RenderDemoPagePng(1, 150, false));
        File.WriteAllBytes(Path.Combine(outDir, "yara-hbo-p2.png"), studentSvc.RenderDemoPagePng(2, 150, false));
        File.WriteAllBytes(Path.Combine(outDir, "yara-hbo-sparse-p1.png"), studentSvc.RenderDemoPagePng(1, 150, true));
    }

    private static void ExportPng(PupilPassportPdfService svc, PupilPassportPdfModel model, string pngPath)
    {
        var png = svc.RenderFirstPagePng(model, 150);
        File.WriteAllBytes(pngPath, png);
    }

    private static bool ContainsEmoji(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var code = char.IsSurrogatePair(text, i) ? char.ConvertToUtf32(text, i) : text[i];
            if (code is >= 0x2600 and <= 0x27BF or >= 0x1F300 and <= 0x1FAFF)
            {
                return true;
            }

            if (char.IsSurrogatePair(text, i))
            {
                i++;
            }
        }

        return false;
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
        progress.VoFavoriteSubjectsJson = """["biologie","tekenen","engels"]""";
        progress.VoProfileKey = "natuur-gezondheid";
        progress.VoSideJobAvailabilityJson = """["za","wo-middag","max-8"]""";
        progress.VoTravelModeKey = "fiets";
        progress.VoOwnWords = "Ik wil graag ontdekken of zorg echt bij me past.";
        progress.VoLearningGoalsJson = """["Meer biologie-praktijk","Rustig blijven in de klas"]""";
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

    private static StudentPassportPdfService StudentPdfService()
        => new(null!, new PassportPdfService(new FakeCompanySettings()), null!);

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
}
