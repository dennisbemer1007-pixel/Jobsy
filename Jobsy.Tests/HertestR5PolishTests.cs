using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Tests;

public sealed class HertestR5PolishTests
{
    [Fact]
    public void SanitizeWhyForDetail_strips_confidence_percent()
    {
        var raw = "Past bij je voorkeuren. Betrouwbaarheid van deze match: ongeveer 88%.";
        var cleaned = CandidateFitDisplay.SanitizeWhyForDetail(raw);
        Assert.StartsWith("Past bij je voorkeuren", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain('%', cleaned!, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_redirects_vacancies_root_to_banenkaart()
    {
        var program = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/Program.cs"));
        Assert.Contains("MapGet(\"/vacancies\"", program, StringComparison.Ordinal);
        Assert.Contains("KbRoutes.Map", program, StringComparison.Ordinal);
    }

    private static string FindRoot()
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
