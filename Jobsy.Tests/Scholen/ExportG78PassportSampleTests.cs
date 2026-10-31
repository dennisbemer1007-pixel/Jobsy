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
                """{"realistic":86,"investigative":74,"artistic":48,"social":61,"enterprising":35,"conventional":30}""",
            CultureScoresJson =
                """{"collaboration":35,"peopleFirst":75,"informal":30,"flexibility":65}""",
            ValuesScoresJson =
                """{"autonomy":62,"connection":84,"achievement":71,"stability":48,"impact":66}""",
            CompetenceScoresJson = "{}",
            TopValue = "Connection"
        };
        var keys = PupilStoryTemplates.SelectKeys(result, ["sport", "natuur", "techniek"], PupilQuestionSet.Groep78);
        result.StoryKeysJson = PupilStoryTemplates.Serialize(keys);

        var vakMap = new Dictionary<string, object>();
        foreach (var key in PupilVakScores.AllSubjectKeys)
        {
            vakMap[key] = new { leuk = 2, goed = 2 };
        }

        // Mockup density: three strong, three growth, one good-but-not-favorite.
        vakMap["natuur-techniek"] = new { leuk = 5, goed = 5 };
        vakMap["rekenen"] = new { leuk = 5, goed = 5 };
        vakMap["gym"] = new { leuk = 5, goed = 5 };
        vakMap["geschiedenis"] = new { leuk = 5, goed = 3 };
        vakMap["tekenen"] = new { leuk = 5, goed = 3 };
        vakMap["muziek"] = new { leuk = 5, goed = 3 };
        vakMap["spelling"] = new { leuk = 2, goed = 5 };

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
            MirrorBody = PupilG78MirrorTemplate.Render(result, progress, PupilVakScores.Parse(progress.VakScoresJson), "nl"),
            G78BijlagenMockChrome = true
        };

        var pdf = new PupilPassportPdfService().Render(model);
        Assert.True(pdf.Length > 4000);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.AsSpan(0, 4)));

        using var doc = PdfDocument.Open(pdf);
        Assert.Equal(5, doc.NumberOfPages);

        Directory.CreateDirectory("/opt/cursor/artifacts");
        File.WriteAllBytes("/opt/cursor/artifacts/g78-paspoort-bijlagen-sample.pdf", pdf);

        WriteMockupComparisonPngs(pdf);
    }

    private static void WriteMockupComparisonPngs(byte[] samplePdf)
    {
        var mockPath = "/home/ubuntu/.cursor/projects/workspace/uploads/paspoort-bijlagen-mockup_5759.pdf";
        if (!File.Exists(mockPath))
        {
            return;
        }

        EnsurePythonCompareDeps();
        ExecutePythonComparison(samplePdf, mockPath);
        for (var p = 1; p <= 5; p++)
        {
            var path = $"/opt/cursor/artifacts/g78-bijlagen-compare-p{p}-100dpi.png";
            Assert.True(File.Exists(path), $"Missing comparison artifact: {path}");
        }
    }

    private static void EnsurePythonCompareDeps()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python3",
            ArgumentList = { "-m", "pip", "install", "--quiet", "pymupdf", "pillow" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        proc!.WaitForExit(180_000);
        Assert.Equal(0, proc.ExitCode);
    }

    private static object ExecutePythonComparison(byte[] samplePdf, string mockPath)
    {
        var samplePath = "/opt/cursor/artifacts/g78-paspoort-bijlagen-sample.pdf";
        File.WriteAllBytes(samplePath, samplePdf);
        var script = """
import sys
try:
    import pymupdf as fitz
except ImportError:
    import fitz
scale = 100 / 72
mat = fitz.Matrix(scale, scale)
mock = fitz.open(sys.argv[1])
sample = fitz.open(sys.argv[2])
from PIL import Image
import io
for i in range(min(5, mock.page_count, sample.page_count)):
    mp = mock[i].get_pixmap(matrix=mat)
    sp = sample[i].get_pixmap(matrix=mat)
    im_m = Image.open(io.BytesIO(mp.tobytes("png")))
    im_s = Image.open(io.BytesIO(sp.tobytes("png")))
    w = im_m.width + im_s.width
    h = max(im_m.height, im_s.height)
    combo = Image.new("RGB", (w, h), (255, 252, 250))
    combo.paste(im_m, (0, 0))
    combo.paste(im_s, (im_m.width, 0))
    out = f"/opt/cursor/artifacts/g78-bijlagen-compare-p{i+1}-100dpi.png"
    combo.save(out)
print("ok")
""";
        var scriptPath = "/tmp/g78-compare.py";
        File.WriteAllText(scriptPath, script);
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python3",
            ArgumentList = { scriptPath, mockPath, samplePath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        proc!.WaitForExit(120_000);
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(proc.StandardError.ReadToEnd());
        }

        return proc.StandardOutput.ReadToEnd();
    }
}
