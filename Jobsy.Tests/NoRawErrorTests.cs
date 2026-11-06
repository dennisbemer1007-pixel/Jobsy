namespace Jobsy.Tests;

/// <summary>
/// Visitors never see <c>ex.Message</c>, a stack trace, or another raw exception detail
/// (public-pages 07, §0 shared rule). This is a source scan, not a render test, so it also
/// catches the string even if a code path currently can't throw.
/// </summary>
public class NoRawErrorTests
{
    [Theory]
    [InlineData("Jobsy.Web", "Components", "Pages", "Legal", "PrivacyData.razor")]
    [InlineData("Jobsy.Web", "Hosting", "PrivacyDataExportEndpoints.cs")]
    [InlineData("Jobsy.Web", "Components", "UnsubscribeDialog.razor")]
    public void Source_file_never_shows_ex_Message(params string[] relativeParts)
    {
        var path = Path.Combine(FindRepoRoot(), Path.Combine(relativeParts));
        var source = File.ReadAllText(path);

        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
