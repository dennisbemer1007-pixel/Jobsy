namespace Jobsy.Tests.Scholen;

public sealed class PupilUiNoNumericProgressTests
{
    private static IEnumerable<string> LeerlingRazorPaths()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web", "Components", "Pages", "Leerling"));
        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories)
            : [];
    }

    [Fact]
    public void Leerling_pages_do_not_format_question_or_world_counters()
    {
        var forbidden = new[]
        {
            "Leerling.Reis.QuestionOf",
            "Leerling.Reis.WorldOf",
            "Culture[\"Leerling.Island.Progress\"]",
            "Culture[\"LeerlingDroom.Progress\"]",
            "ll-stars",
        };

        foreach (var path in LeerlingRazorPaths())
        {
            var text = File.ReadAllText(path);
            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, text, StringComparison.Ordinal);
            }
        }
    }
}
