using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class RoutesDocFreshnessTests
{
    [Fact]
    public void Docs_ROUTES_md_matches_blazor_pages()
    {
        var root = RepoRoot.Find();
        var path = Path.Combine(root, RoutesDocGenerator.RelativeDocPath);
        var generated = RoutesDocGenerator.Normalize(RoutesDocGenerator.Generate());

        var update = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_UPDATE_ROUTES_DOC"),
            "1",
            StringComparison.Ordinal);

        if (update || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, generated);
        }

        Assert.True(File.Exists(path), $"Missing {RoutesDocGenerator.RelativeDocPath}");
        var onDisk = RoutesDocGenerator.Normalize(File.ReadAllText(path));

        Assert.True(
            string.Equals(generated, onDisk, StringComparison.Ordinal),
            $"{RoutesDocGenerator.RelativeDocPath} is outdated. Regenerate with:\n" +
            "JOBSY_UPDATE_ROUTES_DOC=1 dotnet test Jobsy.Tests/Jobsy.Tests.csproj " +
            "--filter \"FullyQualifiedName~RoutesDocFreshness\"");
    }
}
