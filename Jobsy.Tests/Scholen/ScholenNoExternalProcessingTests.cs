using System.Reflection;
using Jobsy.Infrastructure.Scholen;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Pupil-related Scholen services must not take external processing dependencies (D9).
/// Extended in 04/06.
/// </summary>
public class ScholenNoExternalProcessingTests
{
    private static readonly string[] ForbiddenTypeNameFragments =
    [
        "IOpenAi",
        "HttpClient",
        "IEmailSender",
        "ITrainingOffer",
        "IVacancy",
        "IAnalytics",
    ];

    [Fact]
    public void Pupil_namespace_services_have_no_forbidden_ctor_deps()
    {
        var asm = typeof(PupilCodeService).Assembly;
        var types = asm.GetTypes()
            .Where(t => t.Namespace is not null
                        && (t.Namespace.Contains("Scholen.Pupil", StringComparison.Ordinal)
                            || t.Name.Contains("PupilPortal", StringComparison.Ordinal)
                            || t.Name.Contains("PupilLogin", StringComparison.Ordinal)
                            || t.Name.Contains("PupilResult", StringComparison.Ordinal)
                            || t.Name.Contains("PupilQuestion", StringComparison.Ordinal)
                            || t.Name.Contains("PupilStory", StringComparison.Ordinal)
                            || t.Name.Contains("PupilDream", StringComparison.Ordinal)
                            || t.Name.Contains("PupilReportPdf", StringComparison.Ordinal))
                        && !t.IsAbstract
                        && t.IsClass)
            .ToList();

        Assert.NotEmpty(types);

        var offenders = new List<string>();
        foreach (var type in types)
        {
            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var p in ctor.GetParameters())
                {
                    var name = p.ParameterType.FullName ?? p.ParameterType.Name;
                    if (ForbiddenTypeNameFragments.Any(f => name.Contains(f, StringComparison.Ordinal)))
                    {
                        offenders.Add($"{type.Name}({name})");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    [Fact]
    public void Core_story_and_fit_types_do_not_reference_openai_or_vacancies()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string[] files =
        [
            Path.Combine(root, "Jobsy.Core", "Scholen", "PupilStoryRenderer.cs"),
            Path.Combine(root, "Jobsy.Core", "Scholen", "PupilDreamJobFit.cs"),
            Path.Combine(root, "Jobsy.Infrastructure", "Scholen", "PupilReportPdfService.cs"),
        ];
        string[] forbidden = ["OpenAi", "HttpClient", "TrainingOffers", "RoleFitCheck", "IVacancy", "Vacancy"];
        foreach (var file in files)
        {
            Assert.True(File.Exists(file), file);
            var text = File.ReadAllText(file);
            foreach (var f in forbidden)
            {
                Assert.DoesNotContain(f, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Pupil_razor_pages_do_not_reference_forbidden_components()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var dir = Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Leerling");
        Assert.True(Directory.Exists(dir), dir);
        string[] forbidden =
        [
            "BottomNav", "TrainingOffersBlock", "RoleFitCheck", "VacancyMap", "banenkaart", "Partner",
            "href=\"http"
        ];
        foreach (var file in Directory.EnumerateFiles(dir, "*.razor"))
        {
            var text = File.ReadAllText(file);
            foreach (var f in forbidden)
            {
                Assert.DoesNotContain(f, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
