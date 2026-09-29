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
                        && t.Namespace.Contains("Scholen.Pupil", StringComparison.Ordinal)
                        && !t.IsAbstract
                        && t.IsClass)
            .ToList();

        // 01: no Pupil* services yet — vacuous pass when empty.
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
}
