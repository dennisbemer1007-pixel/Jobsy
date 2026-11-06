namespace Jobsy.Tests;

/// <summary>
/// Quoted component attributes without <c>@</c> are string literals. A field name like
/// <c>_endStrength</c> then shows up on screen instead of the value.
/// </summary>
public class LiteralParameterBindingTests
{
    private static string RepoRoot => TestRepo.FindRoot();

    [Fact]
    public void String_parameters_do_not_bind_underscore_field_names_as_literals()
    {
        var root = Path.Combine(RepoRoot, "Jobsy.Web");
        var hits = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (var name in new[] { "StrengthFact", "WorkFact", "ValueFact", "Period" })
            {
                var token = $"{name}=\"_";
                if (text.Contains(token, StringComparison.Ordinal))
                {
                    hits.Add($"{Path.GetRelativePath(RepoRoot, file)} contains {token}");
                }
            }
        }

        Assert.True(hits.Count == 0, string.Join(Environment.NewLine, hits));
    }
}
