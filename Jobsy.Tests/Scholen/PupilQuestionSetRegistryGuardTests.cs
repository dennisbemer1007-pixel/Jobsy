namespace Jobsy.Tests.Scholen;

/// <summary>
/// Pupil-facing Infrastructure must resolve defs via ForClass(SchoolClass), never Get(set)
/// with a guessed/other test. Web may call Get from the progress DTO's QuestionSet.
/// </summary>
public class PupilQuestionSetRegistryGuardTests
{
    [Fact]
    public void Infrastructure_scholen_does_not_call_registry_Get()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Jobsy.Infrastructure", "Scholen");
        Assert.True(Directory.Exists(dir), dir);

        var hits = new List<string>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            // Allow the registry type itself if it ever moves here; today it lives in Core.
            if (file.EndsWith("PupilQuestionSetRegistry.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (text.Contains("_registry.Get(", StringComparison.Ordinal)
                || text.Contains("registry.Get(", StringComparison.Ordinal))
            {
                hits.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.True(
            hits.Count == 0,
            "Infrastructure/Scholen must use registry.ForClass(schoolClass), not Get. Hits: "
            + string.Join(", ", hits));
    }

    private static string FindRepoRoot()
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
