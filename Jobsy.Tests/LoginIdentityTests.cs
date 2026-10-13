using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class LoginIdentityTests
{
    [Theory]
    [InlineData("Twalieb", "twalieb@jobsy.local")]
    [InlineData("twalieb", "twalieb@jobsy.local")]
    [InlineData("  Twalieb  ", "twalieb@jobsy.local")]
    [InlineData("twalieb@jobsy.local", "twalieb@jobsy.local")]
    [InlineData("Kandidaat@Jobsy.Local", "kandidaat@jobsy.local")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_maps_bare_names_to_jobsy_local(string? input, string expected)
        => Assert.Equal(expected, LoginIdentity.Normalize(input));

    [Fact]
    public void Login_page_accepts_bare_username_in_email_field()
    {
        var root = FindRepoRoot();
        var login = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Login.razor"));
        Assert.Contains("type=\"text\"", login);
        Assert.Contains("inputmode=\"email\"", login);
        Assert.DoesNotContain("type=\"email\"", login);
        Assert.Contains("LoginIdentity.Normalize", File.ReadAllText(
            Path.Combine(root, "Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs")));
        Assert.Contains("LoginIdentity.Normalize", File.ReadAllText(
            Path.Combine(root, "Jobsy.Api/Controllers/AuthController.cs")));
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
